"""Kalshi fee model.

Everything downstream is fee-aware by construction. On Kalshi the fee is the
difference between a strategy that works and one that quietly bleeds, because
the fee is charged on notional-ish terms while the payoff on a near-certain
contract is only a few cents.

Kalshi's published trading fee for a taker order is::

    fee = ceil_to_cent(coefficient * contracts * P * (1 - P))

with ``P`` the price in dollars and ``coefficient`` 0.07 on most series (some
series use a different coefficient; see ``FeeSchedule``). Note the shape: the
fee is maximised at P = 0.50 (1.75c/contract at 0.07) and collapses toward the
extremes (0.33c/contract at P = 0.95). That asymmetry is not incidental -- it
is why the only economically sensible edge on this venue tends to live in
high-probability contracts, and why a 50/50 "prediction" needs a very large
edge to clear costs.

Maker orders that rest and get filled are free on most series today. Because
that changes, both numbers live in ``FeeSchedule`` rather than in the code.

ALWAYS re-check the live fee schedule before trading real size; these are
defaults, not gospel.
"""

from __future__ import annotations

import math
from dataclasses import dataclass

CENTS_PER_DOLLAR = 100


def _ceil_cent(dollars: float) -> int:
    """Round a dollar amount up to the next whole cent, returning cents."""
    # Guard against binary float noise turning 0.0300000001 into 4 cents.
    return int(math.ceil(round(dollars * CENTS_PER_DOLLAR, 6)))


@dataclass(frozen=True)
class FeeSchedule:
    """Fee parameters for one series.

    Attributes:
        taker_coefficient: multiplier in the ``c * n * P * (1-P)`` taker fee.
        maker_coefficient: same shape, applied to resting orders that fill.
        settlement_fee_cents: per-contract fee charged at settlement, if any.
    """

    taker_coefficient: float = 0.07
    maker_coefficient: float = 0.0
    settlement_fee_cents: int = 0

    def taker_fee_cents(self, price_cents: int, contracts: int = 1) -> int:
        """Fee in cents for crossing the spread on ``contracts`` at ``price_cents``."""
        _validate_price(price_cents)
        if contracts <= 0:
            return 0
        p = price_cents / CENTS_PER_DOLLAR
        return _ceil_cent(self.taker_coefficient * contracts * p * (1.0 - p))

    def maker_fee_cents(self, price_cents: int, contracts: int = 1) -> int:
        """Fee in cents for a resting order that gets filled."""
        _validate_price(price_cents)
        if contracts <= 0 or self.maker_coefficient == 0.0:
            return 0
        p = price_cents / CENTS_PER_DOLLAR
        return _ceil_cent(self.maker_coefficient * contracts * p * (1.0 - p))

    def round_trip_cost_cents(self, price_cents: int, contracts: int = 1) -> int:
        """Taker in, held to settlement. The usual case for a scanner signal."""
        return (
            self.taker_fee_cents(price_cents, contracts)
            + self.settlement_fee_cents * contracts
        )


DEFAULT_SCHEDULE = FeeSchedule()

# Series that deviate from the default get an entry here. Keyed by the series
# ticker prefix (the part before the first dash in a market ticker).
SERIES_OVERRIDES: dict[str, FeeSchedule] = {}


def schedule_for(ticker: str) -> FeeSchedule:
    """Fee schedule for a market ticker, e.g. ``KXBTCD-25AUG2217-T113999.99``."""
    series = ticker.split("-", 1)[0].upper()
    return SERIES_OVERRIDES.get(series, DEFAULT_SCHEDULE)


def _validate_price(price_cents: int) -> None:
    if not 0 <= price_cents <= 100:
        raise ValueError(f"price must be 0..100 cents, got {price_cents}")


def breakeven_probability(
    price_cents: int,
    contracts: int = 1,
    schedule: FeeSchedule = DEFAULT_SCHEDULE,
) -> float:
    """Minimum true probability at which buying YES at ``price_cents`` is +EV.

    This is the number people skip and then wonder where the money went. Because
    the fee is rounded up to the whole cent *per order*, the breakeven depends on
    order size: one contract at 99c can never be +EV (99c + a 1c minimum fee is
    the full payout), while 500 contracts at 99c break even around 99.1%.
    """
    _validate_price(price_cents)
    if contracts <= 0:
        raise ValueError("contracts must be positive")
    cost = price_cents * contracts + schedule.round_trip_cost_cents(price_cents, contracts)
    return cost / (CENTS_PER_DOLLAR * contracts)


def expected_value_cents(
    probability: float,
    price_cents: int,
    contracts: int = 1,
    schedule: FeeSchedule = DEFAULT_SCHEDULE,
) -> float:
    """Expected profit in cents from buying ``contracts`` YES at ``price_cents``."""
    _validate_probability(probability)
    _validate_price(price_cents)
    fees = schedule.round_trip_cost_cents(price_cents, contracts)
    gross = probability * (CENTS_PER_DOLLAR - price_cents) * contracts
    loss = (1.0 - probability) * price_cents * contracts
    return gross - loss - fees


def kelly_fraction(
    probability: float,
    price_cents: int,
    contracts: int = 1,
    schedule: FeeSchedule = DEFAULT_SCHEDULE,
) -> float:
    """Full-Kelly bankroll fraction for a YES buy. Clamped to [0, 1].

    Use a fraction of this (``KELLY_MULTIPLIER`` in the config) -- full Kelly
    assumes your probability is correct, and the whole premise of this project is
    that it usually is not.
    """
    _validate_probability(probability)
    _validate_price(price_cents)
    fee = schedule.round_trip_cost_cents(price_cents, contracts) / contracts
    win = CENTS_PER_DOLLAR - price_cents - fee
    lose = price_cents + fee
    if win <= 0:
        return 0.0
    f = (probability * win - (1.0 - probability) * lose) / win
    return max(0.0, min(1.0, f))


def _validate_probability(probability: float) -> None:
    if not 0.0 <= probability <= 1.0:
        raise ValueError(f"probability must be 0..1, got {probability}")
