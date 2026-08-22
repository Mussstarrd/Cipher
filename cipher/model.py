"""Normalised market-data types.

Kept deliberately thin and immutable: every scanner takes these and returns
signals, so the scanners can be unit-tested against fixtures without a network.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone


def utcnow() -> datetime:
    return datetime.now(timezone.utc)


@dataclass(frozen=True)
class Quote:
    """Top of book for one market, in cents.

    Kalshi quotes YES and NO separately. ``yes_ask`` is what you pay to buy YES;
    ``no_ask`` is what you pay to buy NO. They are not required to sum to 100 --
    when they sum to less than 100 net of fees, that is a locked arbitrage, which
    is exactly what ``scanners.structural`` looks for.
    """

    yes_bid: int | None = None
    yes_ask: int | None = None
    no_bid: int | None = None
    no_ask: int | None = None
    yes_bid_size: int = 0
    yes_ask_size: int = 0
    no_bid_size: int = 0
    no_ask_size: int = 0

    @property
    def yes_mid(self) -> float | None:
        if self.yes_bid is None or self.yes_ask is None:
            return None
        return (self.yes_bid + self.yes_ask) / 2

    @property
    def spread(self) -> int | None:
        if self.yes_bid is None or self.yes_ask is None:
            return None
        return self.yes_ask - self.yes_bid


@dataclass(frozen=True)
class Market:
    """A single binary contract."""

    ticker: str
    event_ticker: str
    title: str
    close_time: datetime
    quote: Quote = field(default_factory=Quote)
    status: str = "active"
    volume: int = 0
    open_interest: int = 0
    # Populated for strike-ladder markets so structural scanners can reason about
    # ordering without parsing titles.
    strike_type: str | None = None  # "greater", "less", "between", "custom"
    floor_strike: float | None = None
    cap_strike: float | None = None

    def minutes_to_close(self, now: datetime | None = None) -> float:
        now = now or utcnow()
        return (self.close_time - now).total_seconds() / 60.0

    @property
    def series(self) -> str:
        return self.ticker.split("-", 1)[0].upper()


@dataclass(frozen=True)
class Event:
    """A group of markets that share a resolution source and, often, a partition.

    ``mutually_exclusive`` is set by Kalshi for events whose markets form an
    exhaustive partition (exactly one resolves YES). That flag is the licence to
    run the sum-to-100 arbitrage; without it, summing brackets is meaningless.
    """

    event_ticker: str
    title: str
    markets: tuple[Market, ...] = ()
    mutually_exclusive: bool = False
    category: str | None = None
