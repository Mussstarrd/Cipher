"""Compare a resolver's probability against the book and size the difference.

This is the second tier: unlike the structural scanners, it *is* making a claim
about the world, so it comes with guard rails the arbitrage scanners do not
need. In order of how much money each one saves:

* **Staleness.** An estimate derived from an observation older than
  ``max_staleness_seconds`` is discarded outright. Near expiry, a stale source
  is not a slightly worse signal -- it is a signal pointing the wrong way.
* **Edge floor.** The disagreement has to clear fees *and* a margin, because the
  model is not exact and the book is not stupid.
* **Confidence haircut.** The probability is shrunk toward the market price by
  ``1 - confidence``. A resolver that is unsure ends up agreeing with the book,
  which is the correct default.
* **Extremes.** Probabilities are clamped away from 0 and 1. Nothing this system
  can observe justifies certainty, and an unclamped 1.0 makes Kelly demand the
  whole bankroll.
"""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime

from ..fees import expected_value_cents, schedule_for
from ..model import Market
from ..resolvers.base import Estimate
from ..signal import Kind, Side, Signal

SCANNER = "disagreement"

# No estimate is ever allowed to be certain.
PROBABILITY_CLAMP = 0.005


@dataclass(frozen=True)
class DisagreementConfig:
    min_edge_cents: float = 2.0
    max_staleness_seconds: float = 20.0
    min_confidence: float = 0.6
    max_contracts: int = 200
    # Cheap contracts have terrible fee-adjusted risk/reward and are usually
    # where a wrong model looks most attractive. Skip the lottery-ticket end.
    min_price_cents: int = 5
    max_price_cents: int = 99
    # Staleness allowance for *deterministic* estimates. Much longer on purpose:
    # a deterministic claim rests on a quantity that cannot move against you --
    # an observed daily maximum is a valid lower bound an hour later, because a
    # day's max never falls. Age degrades a model's guess; it does not degrade a
    # monotone fact. Set to None to apply the ordinary limit to everything.
    deterministic_staleness_seconds: float | None = None

    def staleness_limit(self, deterministic: bool) -> float:
        if deterministic and self.deterministic_staleness_seconds is not None:
            return self.deterministic_staleness_seconds
        return self.max_staleness_seconds


# Presets, because the right staleness window is a property of the source, not
# of the scanner. Getting this wrong in either direction is expensive: too tight
# and hourly sources never fire at all; too loose and a fast-moving source gets
# traded on yesterday's news.
CRYPTO = DisagreementConfig(max_staleness_seconds=20.0, min_edge_cents=2.0)
WEATHER = DisagreementConfig(
    # NWS observations arrive roughly hourly, so a 20s window rejects every
    # estimate the weather resolver will ever produce.
    max_staleness_seconds=75 * 60,
    # A monotone bound stays true all evening.
    deterministic_staleness_seconds=10 * 3600,
    min_edge_cents=2.0,
    min_confidence=0.6,
)


def _haircut(probability: float, market_price: float, confidence: float) -> float:
    """Shrink toward the book in proportion to the resolver's own uncertainty."""
    blended = confidence * probability + (1.0 - confidence) * market_price
    return min(1.0 - PROBABILITY_CLAMP, max(PROBABILITY_CLAMP, blended))


def scan(
    market: Market,
    estimate: Estimate,
    config: DisagreementConfig | None = None,
    now: datetime | None = None,
) -> list[Signal]:
    """Emit at most one signal for the cheaper of the two sides.

    ``now`` overrides the clock so a scan can be replayed against a recorded
    day at the timestamps it actually happened.
    """
    config = config or DisagreementConfig()

    if estimate.confidence < config.min_confidence:
        return []
    staleness = estimate.staleness_seconds(now)
    if staleness > config.staleness_limit(estimate.deterministic):
        return []

    quote = market.quote
    schedule = schedule_for(market.ticker)
    kind = Kind.DETERMINISTIC if estimate.deterministic else Kind.MODEL
    signals: list[Signal] = []

    for side, ask, implied in (
        (Side.YES, quote.yes_ask, estimate.probability),
        (Side.NO, quote.no_ask, 1.0 - estimate.probability),
    ):
        if ask is None or not config.min_price_cents <= ask <= config.max_price_cents:
            continue

        probability = _haircut(implied, ask / 100, estimate.confidence)
        edge = (
            expected_value_cents(probability, ask, config.max_contracts, schedule)
            / config.max_contracts
        )
        if edge < config.min_edge_cents:
            continue

        signals.append(
            Signal(
                ticker=market.ticker,
                side=side,
                kind=kind,
                scanner=f"{SCANNER}.{estimate.source}",
                price_cents=ask,
                probability=probability,
                contracts=config.max_contracts,
                rationale=(
                    f"{estimate.source} implies {implied:.1%} vs book {ask}c "
                    f"({estimate.rationale})"
                ),
                invalidated_if=(
                    f"the source moves, or the observation ages past "
                    f"{config.staleness_limit(estimate.deterministic):.0f}s "
                    f"(currently {staleness:.1f}s)"
                ),
                minutes_to_close=market.minutes_to_close(),
                extra={
                    "raw_probability": round(implied, 4),
                    "confidence": estimate.confidence,
                    "staleness_seconds": round(staleness, 2),
                    "source_detail": estimate.detail,
                },
            )
        )

    # Both sides firing means the book is crossed or the estimate is nonsense;
    # either way it is not a directional opportunity.
    return signals if len(signals) == 1 else []
