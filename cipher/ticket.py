"""Turn a signal into a concrete order you could type into the Kalshi app.

Kept separate from the scanners because the arithmetic here is what a person
actually reads before risking money, and it should be impossible to get wrong by
accident. In particular it always states the loss side in the same breath as the
win side: a 95c contract advertises "+5c" and quietly means "-95c", and a ticket
that only prints the first number is lying by omission.
"""

from __future__ import annotations

import math
from dataclasses import dataclass

from .fees import breakeven_probability, schedule_for
from .signal import Signal


@dataclass(frozen=True)
class Ticket:
    """A fully priced order, all figures in cents unless named otherwise."""

    signal: Signal
    contracts: int
    price_cents: int
    fee_cents: int

    @property
    def outlay_cents(self) -> int:
        """Everything that leaves the account to open the position."""
        return self.contracts * self.price_cents + self.fee_cents

    @property
    def payout_if_right_cents(self) -> int:
        return self.contracts * 100

    @property
    def profit_if_right_cents(self) -> int:
        return self.payout_if_right_cents - self.outlay_cents

    @property
    def loss_if_wrong_cents(self) -> int:
        """The whole outlay. Binary contracts settle at zero when they lose."""
        return self.outlay_cents

    @property
    def breakeven_probability(self) -> float:
        return breakeven_probability(self.price_cents, self.contracts, schedule_for(self.signal.ticker))

    @property
    def expected_value_cents(self) -> float:
        p = self.signal.probability
        return p * self.profit_if_right_cents - (1 - p) * self.loss_if_wrong_cents

    @property
    def losses_erased_by_one_loss(self) -> float:
        """How many wins one loss wipes out. The number people skip."""
        if self.profit_if_right_cents <= 0:
            return math.inf
        return self.loss_if_wrong_cents / self.profit_if_right_cents

    def render(self) -> str:
        s = self.signal
        return "\n".join(
            [
                "ORDER TICKET",
                f"  market      : {s.ticker}",
                f"  side        : buy {s.side.value.upper()}",
                f"  limit price : {self.price_cents}c  (do not chase above this)",
                f"  quantity    : {self.contracts} contracts",
                "",
                f"  outlay      : ${self.outlay_cents / 100:.2f}"
                f"  (${self.contracts * self.price_cents / 100:.2f} + ${self.fee_cents / 100:.2f} fee)",
                f"  if right    : +${self.profit_if_right_cents / 100:.2f}",
                f"  if wrong    : -${self.loss_if_wrong_cents / 100:.2f}",
                f"  one loss erases {self.losses_erased_by_one_loss:.0f} wins",
                "",
                f"  model says  : {s.probability:.1%}",
                f"  needs       : {self.breakeven_probability:.1%} to break even",
                f"  expected    : {self.expected_value_cents / 100:+.2f} USD "
                f"(only as good as the model)",
                "",
                f"  why         : {s.rationale}",
                f"  wrong if    : {s.invalidated_if}",
            ]
        )


def size_to_stake(signal: Signal, stake_cents: int) -> Ticket | None:
    """Largest whole-contract order fitting inside ``stake_cents``, fees included.

    Fees are charged per order and rounded up, so the naive
    ``stake // price`` can overshoot the budget by a cent or two. This shrinks
    the count until the all-in outlay actually fits.
    """
    if stake_cents <= 0 or signal.price_cents <= 0:
        return None

    schedule = schedule_for(signal.ticker)
    contracts = min(stake_cents // signal.price_cents, signal.contracts)
    while contracts > 0:
        fee = schedule.round_trip_cost_cents(signal.price_cents, contracts)
        if contracts * signal.price_cents + fee <= stake_cents:
            return Ticket(signal, contracts, signal.price_cents, fee)
        contracts -= 1
    return None
