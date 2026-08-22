"""Terminal-barrier pricing for markets on a continuously traded underlying.

Kalshi's hourly and daily crypto/index markets ("will BTC be above $113,000 at
5pm ET") are not forecasting problems in the usual sense. With a live spot price
and an estimate of short-horizon volatility they are a closed-form calculation,
and the only question is whether the book agrees with it.

This is where a scanner has a genuine, defensible reason to disagree with the
market: near expiry the probability surface gets very steep, so a book that is a
minute stale on spot can be tens of cents wrong. That is a plumbing race, not a
prediction, and it is winnable if your data path is faster than the marginal
quoter's.

Two cautions baked into the code:

* ``terminal_probability`` prices "above X *at* time T". A market that resolves
  on whether the price *ever touches* X is a different (roughly double the)
  probability -- see ``touch_probability``. Reading the settlement rules wrong
  here is the single most expensive mistake available in this module.
* Volatility is estimated from recent realised returns, which systematically
  understates risk right before scheduled events. ``vol_floor`` exists so the
  model cannot talk itself into a 99.9% when a CPI print lands in ten minutes.
"""

from __future__ import annotations

import math
from dataclasses import dataclass

SECONDS_PER_YEAR = 365.0 * 24 * 3600


def _norm_cdf(x: float) -> float:
    return 0.5 * (1.0 + math.erf(x / math.sqrt(2.0)))


def realised_volatility(prices: list[float], periods_per_year: float) -> float:
    """Annualised volatility from a series of equally spaced prices.

    ``periods_per_year`` is the sampling rate: for one-minute bars that is
    365*24*60. Uses log returns and the population standard deviation, which is
    close enough at the sample sizes involved and does not blow up on n=2.
    """
    if len(prices) < 3:
        raise ValueError("need at least 3 prices to estimate volatility")
    if any(p <= 0 for p in prices):
        raise ValueError("prices must be positive")

    returns = [math.log(b / a) for a, b in zip(prices, prices[1:])]
    mean = sum(returns) / len(returns)
    variance = sum((r - mean) ** 2 for r in returns) / len(returns)
    return math.sqrt(variance * periods_per_year)


@dataclass(frozen=True)
class BarrierInputs:
    spot: float
    strike: float
    seconds_to_expiry: float
    annual_volatility: float
    # Below this the model refuses to produce extreme probabilities. Protects
    # against a quiet ten-minute window convincing the estimator that nothing
    # can happen in the next ten.
    vol_floor: float = 0.15

    @property
    def tau(self) -> float:
        return max(self.seconds_to_expiry, 0.0) / SECONDS_PER_YEAR

    @property
    def sigma(self) -> float:
        return max(self.annual_volatility, self.vol_floor)


def terminal_probability(inputs: BarrierInputs) -> float:
    """P(spot at expiry > strike) under driftless geometric Brownian motion.

    Driftless is the right default at these horizons: any drift you could
    estimate from recent data is swamped by the noise term, and assuming zero
    keeps the model from inheriting a trend-following bias.
    """
    if inputs.spot <= 0 or inputs.strike <= 0:
        raise ValueError("spot and strike must be positive")
    if inputs.tau <= 0:
        return 1.0 if inputs.spot > inputs.strike else 0.0

    sigma, tau = inputs.sigma, inputs.tau
    d2 = (math.log(inputs.spot / inputs.strike) - 0.5 * sigma**2 * tau) / (sigma * math.sqrt(tau))
    return _norm_cdf(d2)


def touch_probability(inputs: BarrierInputs) -> float:
    """P(spot touches the strike at any point before expiry).

    For a market that resolves on *ever* reaching a level, not on where the price
    sits at the bell. Roughly twice the terminal probability for an out-of-the-
    money barrier -- the reflection-principle result. Getting this confused with
    ``terminal_probability`` is a systematic, one-directional error, so the
    resolver must be told which rule the market uses.
    """
    if inputs.tau <= 0:
        return 1.0 if inputs.spot >= inputs.strike else 0.0

    sigma, tau = inputs.sigma, inputs.tau
    if inputs.spot >= inputs.strike:
        return 1.0

    # Driftless reflection principle on log-price.
    x = math.log(inputs.strike / inputs.spot)
    return min(1.0, 2.0 * (1.0 - _norm_cdf(x / (sigma * math.sqrt(tau)))))


def implied_move_to_flip(inputs: BarrierInputs) -> float:
    """How far spot must move for the outcome to change, as a fraction of spot.

    A blunt sanity check to put in front of a human before any order: "this
    market pays unless BTC moves 0.8% in four minutes". If that number is small,
    the model's 97% is not the whole story.
    """
    if inputs.spot <= 0:
        raise ValueError("spot must be positive")
    return abs(inputs.strike - inputs.spot) / inputs.spot
