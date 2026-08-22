"""Resolver protocol: the part that actually knows something about the world.

A resolver answers one question for one family of markets: *given the current
state of the resolution source, what is the probability this contract settles
YES?* It also reports how stale its view of the source is, because a confident
probability derived from a five-minute-old observation is worse than useless.

The design rule that matters: a resolver reads the **same source the exchange
settles on**, never a proxy for it. A market that settles on the NWS daily
climate report for KNYC is not a market about the weather; it is a market about
what that specific product says. Trading it off a different forecast provider is
how you lose on a technicality.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from typing import Protocol

from ..model import Market, utcnow


@dataclass(frozen=True)
class Estimate:
    """A resolver's view of one market."""

    probability: float
    # How sure the resolver is that its own probability is right. Distinct from
    # ``probability``: a coin flip is p=0.5 with confidence 1.0.
    confidence: float
    source: str
    observed_at: datetime
    rationale: str
    deterministic: bool = False
    detail: dict = field(default_factory=dict)

    def staleness_seconds(self, now: datetime | None = None) -> float:
        return ((now or utcnow()) - self.observed_at).total_seconds()


class Resolver(Protocol):
    """Implemented once per resolution-source family."""

    name: str

    def handles(self, market: Market) -> bool:
        """True if this resolver understands the market's settlement rules."""
        ...

    def estimate(self, market: Market) -> Estimate | None:
        """Probability of YES, or None if the source cannot answer right now."""
        ...


class ResolverRegistry:
    """First resolver that claims a market wins."""

    def __init__(self, resolvers: list[Resolver] | None = None):
        self._resolvers: list[Resolver] = list(resolvers or [])

    def register(self, resolver: Resolver) -> None:
        self._resolvers.append(resolver)

    def estimate(self, market: Market) -> Estimate | None:
        for resolver in self._resolvers:
            if resolver.handles(market):
                return resolver.estimate(market)
        return None

    def __len__(self) -> int:
        return len(self._resolvers)
