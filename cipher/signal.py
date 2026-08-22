"""The one object every scanner emits.

A signal is a *claim with a number attached*: this market, this side, this
probability, this price, this size, and -- critically -- what would have to be
true for the claim to be wrong. Everything gets journalled whether or not it is
traded, because the only way to find out if a scanner works is to score its
predictions after the fact.
"""

from __future__ import annotations

import hashlib
from dataclasses import dataclass, field, asdict
from datetime import datetime
from enum import Enum

from .fees import DEFAULT_SCHEDULE, expected_value_cents, kelly_fraction, schedule_for
from .model import utcnow


class Side(str, Enum):
    YES = "yes"
    NO = "no"


class Kind(str, Enum):
    """Why we believe the claim. Ordered from most to least trustworthy.

    ARBITRAGE       -- no forecast involved; the prices contradict each other.
    DETERMINISTIC   -- the resolution source already implies the outcome.
    MODEL           -- a quantitative model disagrees with the book.
    HEURISTIC       -- pattern/microstructure heuristic. Weakest. Paper only.
    """

    ARBITRAGE = "arbitrage"
    DETERMINISTIC = "deterministic"
    MODEL = "model"
    HEURISTIC = "heuristic"


TRUST_ORDER = {Kind.ARBITRAGE: 0, Kind.DETERMINISTIC: 1, Kind.MODEL: 2, Kind.HEURISTIC: 3}


@dataclass
class Signal:
    """A single actionable (or observable) claim."""

    ticker: str
    side: Side
    kind: Kind
    scanner: str
    price_cents: int
    probability: float
    contracts: int
    rationale: str
    # What would falsify this. Filled in by the scanner; read by a human before
    # any real money moves.
    invalidated_if: str = ""
    minutes_to_close: float | None = None
    created_at: datetime = field(default_factory=utcnow)
    extra: dict = field(default_factory=dict)
    # Set by arbitrage scanners: the profit locked in by executing *every* leg
    # of the basket, in cents. An individual leg of an arbitrage always looks
    # like a losing trade on its own -- you are buying YES at exactly its
    # implied probability and paying a fee -- so the per-leg EV computation
    # below is meaningless for these and this field replaces it.
    locked_profit_cents: float | None = None
    legs: int = 1

    @property
    def schedule(self):
        return schedule_for(self.ticker)

    @property
    def is_locked(self) -> bool:
        return self.kind is Kind.ARBITRAGE and self.locked_profit_cents is not None

    @property
    def expected_value_cents(self) -> float:
        """Expected profit attributable to this signal, after fees.

        For an arbitrage leg this is the basket's locked profit divided evenly
        across legs, so that the legs sum to the basket total and ranking is not
        skewed by leg count.
        """
        if self.is_locked:
            return self.locked_profit_cents / max(self.legs, 1)
        return expected_value_cents(
            self.probability, self.price_cents, self.contracts, self.schedule
        )

    @property
    def edge_cents(self) -> float:
        """Expected profit per contract, after fees, in cents.

        Fees are amortised over the *proposed order size*, not over a single
        contract. The distinction is large at the extremes: the per-order cent
        rounding makes one contract at 97c look like it costs a full cent in
        fees (0.2c of edge), while the same fee across 200 contracts is 0.02c
        each. Pricing the edge off a 1-contract fee understates it enough to
        suppress genuinely good signals.
        """
        if self.is_locked:
            return self.locked_profit_cents / max(self.contracts, 1)
        contracts = max(self.contracts, 1)
        return (
            expected_value_cents(self.probability, self.price_cents, contracts, self.schedule)
            / contracts
        )

    @property
    def kelly(self) -> float:
        # A locked arbitrage has no downside to size against, so Kelly does not
        # apply; sizing there is bounded by book depth and capital, not variance.
        if self.is_locked:
            return 0.0
        return kelly_fraction(self.probability, self.price_cents, self.contracts, self.schedule)

    @property
    def signal_id(self) -> str:
        """Stable id so repeated scans of the same condition dedupe in the journal."""
        raw = f"{self.ticker}|{self.side.value}|{self.scanner}|{self.price_cents}"
        return hashlib.sha1(raw.encode()).hexdigest()[:16]

    def to_dict(self) -> dict:
        d = asdict(self)
        d["side"] = self.side.value
        d["kind"] = self.kind.value
        d["created_at"] = self.created_at.isoformat()
        d["signal_id"] = self.signal_id
        d["edge_cents"] = round(self.edge_cents, 3)
        d["expected_value_cents"] = round(self.expected_value_cents, 3)
        d["kelly"] = round(self.kelly, 4)
        return d

    def __str__(self) -> str:
        mins = "" if self.minutes_to_close is None else f" [{_humanise(self.minutes_to_close)}]"
        return (
            f"{self.kind.value:<13} {self.ticker:<34} {self.side.value.upper():<3} "
            f"@{self.price_cents:>3}c  p={self.probability:.3f}  "
            f"edge={self.edge_cents:+.2f}c/ct  x{self.contracts}{mins}  {self.rationale}"
        )


def _humanise(minutes: float) -> str:
    """Compact time-to-close so a far-dated market does not print eight digits."""
    if minutes < 90:
        return f"{minutes:.0f}m"
    if minutes < 60 * 48:
        return f"{minutes / 60:.1f}h"
    return f"{minutes / 1440:.0f}d"


def rank(signals: list[Signal]) -> list[Signal]:
    """Most trustworthy kind first, then largest total expected value.

    Deliberately *not* sorted by edge alone: a 40c "edge" from a heuristic is a
    bug report, not an opportunity, and should never outrank a locked arbitrage.
    """
    return sorted(
        signals,
        key=lambda s: (TRUST_ORDER[s.kind], -s.expected_value_cents),
    )
