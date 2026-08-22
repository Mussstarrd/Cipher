"""How many settled signals before you can tell edge from luck?

This is the question that actually decides whether the project is worth
continuing, and it is almost always answered by vibes. It does not have to be.

The market price is the null hypothesis. Buying NO at 92c is a bet that the true
probability is meaningfully above 92%; if it is exactly 92%, you break even
before fees and lose after them. So the test is:

    H0: the true win rate equals the price you paid (no edge)
    H1: the true win rate equals what the model claims

Both produce the same outcome most of the time -- that is precisely the problem
with high-probability contracts. Distinguishing 92% from 99.5% takes dozens of
settled trades, because a run of wins is exactly what H0 predicts too.

Everything here uses the exact binomial rather than a normal approximation: at
these sample sizes and these tail probabilities the approximation is poor in the
direction that flatters you.
"""

from __future__ import annotations

import math
from dataclasses import dataclass


def _binomial_cdf(k: int, n: int, p: float) -> float:
    """P(X <= k) for X ~ Binomial(n, p)."""
    if k < 0:
        return 0.0
    if k >= n:
        return 1.0
    return sum(math.comb(n, i) * p**i * (1 - p) ** (n - i) for i in range(k + 1))


@dataclass(frozen=True)
class Plan:
    """A decision rule you can commit to before placing the first trade."""

    trades: int
    max_losses: int
    false_positive_rate: float
    power: float
    model_probability: float
    market_probability: float

    @property
    def expected_losses_if_model_right(self) -> float:
        return self.trades * (1 - self.model_probability)

    @property
    def expected_losses_if_market_right(self) -> float:
        return self.trades * (1 - self.market_probability)

    def render(self) -> str:
        return "\n".join(
            [
                f"  run {self.trades} settled signals at ~{self.market_probability:.0%} book price",
                f"  conclude the edge is real only if losses <= {self.max_losses}",
                "",
                f"  if the model is right ({self.model_probability:.1%}): "
                f"expect {self.expected_losses_if_model_right:.1f} losses",
                f"  if the book is right  ({self.market_probability:.1%}): "
                f"expect {self.expected_losses_if_market_right:.1f} losses",
                "",
                f"  chance of fooling yourself (book right, rule passes): "
                f"{self.false_positive_rate:.1%}",
                f"  chance of detecting a real edge: {self.power:.1%}",
            ]
        )


def decision_rule(
    trades: int,
    model_probability: float,
    market_probability: float,
    alpha: float = 0.05,
) -> Plan:
    """Largest loss count that still rejects 'no edge' at the ``alpha`` level."""
    if not 0 < market_probability < model_probability < 1:
        raise ValueError("need 0 < market_probability < model_probability < 1")

    null_loss_rate = 1 - market_probability
    alt_loss_rate = 1 - model_probability

    max_losses = -1
    for k in range(trades + 1):
        if _binomial_cdf(k, trades, null_loss_rate) <= alpha:
            max_losses = k
        else:
            break

    return Plan(
        trades=trades,
        max_losses=max_losses,
        false_positive_rate=_binomial_cdf(max_losses, trades, null_loss_rate),
        power=_binomial_cdf(max_losses, trades, alt_loss_rate),
        model_probability=model_probability,
        market_probability=market_probability,
    )


def trades_needed(
    model_probability: float,
    market_probability: float,
    alpha: float = 0.05,
    target_power: float = 0.8,
    limit: int = 5000,
) -> Plan | None:
    """Smallest number of settled trades reaching ``target_power``."""
    for trades in range(1, limit + 1):
        plan = decision_rule(trades, model_probability, market_probability, alpha)
        if plan.max_losses >= 0 and plan.power >= target_power:
            return plan
    return None


def single_trade_verdict(model_probability: float, market_probability: float) -> str:
    """What one trade can tell you. Included because the answer is 'nothing'."""
    win_if_no_edge = market_probability
    return (
        f"One trade wins {win_if_no_edge:.0%} of the time even with no edge whatsoever.\n"
        f"A win is therefore not evidence of anything: it is the single most likely\n"
        f"outcome under both hypotheses. Only a loss is informative, and only weakly."
    )
