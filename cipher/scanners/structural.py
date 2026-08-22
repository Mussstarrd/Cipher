"""Structural scanners: mispricings you can prove without forecasting anything.

These are the scanners worth building first. They make no claim about the world
-- only that a set of prices on the same exchange contradict each other by more
than the fees. If one of these fires and the fill goes through, the profit is
locked at trade time, so their hit rate is a property of the execution stack
rather than of anyone's judgement about politics or the weather.

Three families:

1. ``scan_yes_no_cross``   -- YES ask + NO ask < 100 on the same market.
2. ``scan_partition_sum``  -- an exhaustive bracket ladder whose YES asks sum to
                              less than 100 (or whose YES bids sum to more).
3. ``scan_ladder_monotonicity`` -- a "greater than X" ladder where a higher
                              strike trades above a lower one, which is
                              impossible: P(X > 110) <= P(X > 100), always.

All three are size-aware and fee-aware. The size on a locked arbitrage is
whatever the *thinnest* leg supports, because a partial fill converts a riskless
trade into a naked position -- the most common way this kind of scanner loses
money in practice.
"""

from __future__ import annotations

from ..fees import schedule_for
from ..model import Event, Market
from ..signal import Kind, Side, Signal

SCANNER_YES_NO = "structural.yes_no_cross"
SCANNER_PARTITION = "structural.partition_sum"
SCANNER_LADDER = "structural.ladder_monotonicity"

# Below this the edge is inside the noise of a one-cent tick and a partial fill.
MIN_EDGE_CENTS = 1
# 0 size means "unknown" from the /markets endpoint; assume a token size so the
# signal surfaces for a depth check rather than being silently dropped.
ASSUMED_SIZE_WHEN_UNKNOWN = 1


def _size(reported: int) -> int:
    return reported if reported > 0 else ASSUMED_SIZE_WHEN_UNKNOWN


def scan_yes_no_cross(market: Market) -> list[Signal]:
    """YES and NO both cheap enough that owning one of each pays more than it costs.

    Buying one YES and one NO guarantees exactly 100c at settlement -- one side
    resolves in the money and the other at zero. If the two asks plus fees come
    to less than 100c, that is free money.
    """
    quote = market.quote
    if quote.yes_ask is None or quote.no_ask is None:
        return []

    schedule = schedule_for(market.ticker)
    size = min(_size(quote.yes_ask_size), _size(quote.no_ask_size))
    cost = quote.yes_ask + quote.no_ask
    fees = schedule.round_trip_cost_cents(quote.yes_ask, size) + schedule.round_trip_cost_cents(
        quote.no_ask, size
    )
    profit_cents = (100 - cost) * size - fees
    if profit_cents < MIN_EDGE_CENTS:
        return []

    per_contract = profit_cents / size
    rationale = (
        f"YES {quote.yes_ask}c + NO {quote.no_ask}c = {cost}c < 100c; "
        f"locks {per_contract:.2f}c/pair after fees"
    )
    invalidated = "either leg moves or thins out before both fills confirm"
    # Emitted as a pair: both legs must fill or neither should be sent.
    return [
        Signal(
            ticker=market.ticker,
            side=Side.YES,
            kind=Kind.ARBITRAGE,
            scanner=SCANNER_YES_NO,
            price_cents=quote.yes_ask,
            probability=quote.yes_ask / 100,
            contracts=size,
            rationale=rationale,
            invalidated_if=invalidated,
            minutes_to_close=market.minutes_to_close(),
            locked_profit_cents=profit_cents,
            legs=2,
            extra={"leg": "1/2"},
        ),
        Signal(
            ticker=market.ticker,
            side=Side.NO,
            kind=Kind.ARBITRAGE,
            scanner=SCANNER_YES_NO,
            price_cents=quote.no_ask,
            probability=quote.no_ask / 100,
            contracts=size,
            rationale=rationale,
            invalidated_if=invalidated,
            minutes_to_close=market.minutes_to_close(),
            locked_profit_cents=profit_cents,
            legs=2,
            extra={"leg": "2/2"},
        ),
    ]


def scan_partition_sum(event: Event) -> list[Signal]:
    """An exhaustive set of brackets that does not add up to 100c.

    Only valid when the exchange flags the event mutually exclusive *and*
    exhaustive -- exactly one market resolves YES. Then a YES in every bracket
    pays exactly 100c, so if the asks sum below 100c net of fees the basket is a
    locked win. The mirror case (bids summing above 100c) is the same trade in
    reverse, sold rather than bought.
    """
    if not event.mutually_exclusive or len(event.markets) < 2:
        return []

    asks = [(m, m.quote.yes_ask, m.quote.yes_ask_size) for m in event.markets]
    if any(price is None for _, price, _ in asks):
        return []

    size = min(_size(s) for _, _, s in asks)
    total = sum(price for _, price, _ in asks)
    fees = sum(schedule_for(m.ticker).round_trip_cost_cents(price, size) for m, price, _ in asks)
    profit_cents = (100 - total) * size - fees
    if profit_cents < MIN_EDGE_CENTS:
        return []

    per_basket = profit_cents / size
    rationale = (
        f"{len(asks)} exhaustive brackets sum to {total}c < 100c; "
        f"locks {per_basket:.2f}c/basket after fees"
    )
    return [
        Signal(
            ticker=m.ticker,
            side=Side.YES,
            kind=Kind.ARBITRAGE,
            scanner=SCANNER_PARTITION,
            price_cents=price,
            probability=price / 100,
            contracts=size,
            rationale=rationale,
            invalidated_if=(
                "any leg fails to fill (a partial basket is a naked directional bet), "
                "or the event is not truly exhaustive"
            ),
            minutes_to_close=m.minutes_to_close(),
            locked_profit_cents=profit_cents,
            legs=len(asks),
            extra={"event_ticker": event.event_ticker, "leg": f"{i + 1}/{len(asks)}"},
        )
        for i, (m, price, _) in enumerate(asks)
    ]


def _ladder(markets: list[Market]) -> list[Market]:
    """Markets forming a 'greater than X' ladder, sorted by ascending strike."""
    rungs = [
        m
        for m in markets
        if m.strike_type in ("greater", "greater_or_equal") and m.floor_strike is not None
    ]
    return sorted(rungs, key=lambda m: m.floor_strike)


def scan_ladder_monotonicity(event: Event) -> list[Signal]:
    """A higher strike quoted above a lower strike on the same ladder.

    P(X > high) can never exceed P(X > low). So if you can buy YES on the high
    strike for less than you can sell YES on the low strike, the spread is a
    riskless credit: whenever the high pays, the low pays too.
    """
    rungs = _ladder(list(event.markets))
    signals: list[Signal] = []

    for i, low in enumerate(rungs):
        for high in rungs[i + 1 :]:
            low_bid, high_ask = low.quote.yes_bid, high.quote.yes_ask
            if low_bid is None or high_ask is None or high_ask >= low_bid:
                continue

            size = min(_size(low.quote.yes_bid_size), _size(high.quote.yes_ask_size))
            credit = (low_bid - high_ask) * size
            fees = schedule_for(low.ticker).round_trip_cost_cents(
                low_bid, size
            ) + schedule_for(high.ticker).round_trip_cost_cents(high_ask, size)
            profit_cents = credit - fees
            if profit_cents < MIN_EDGE_CENTS:
                continue

            rationale = (
                f"buy >{high.floor_strike:g} at {high_ask}c, sell >{low.floor_strike:g} "
                f"at {low_bid}c; the higher strike cannot be more likely"
            )
            invalidated = (
                "the two markets do not share a resolution source/time, "
                "or the strike direction was misparsed"
            )
            signals.extend(
                [
                    Signal(
                        ticker=high.ticker,
                        side=Side.YES,
                        kind=Kind.ARBITRAGE,
                        scanner=SCANNER_LADDER,
                        price_cents=high_ask,
                        probability=high_ask / 100,
                        contracts=size,
                        rationale=rationale,
                        invalidated_if=invalidated,
                        minutes_to_close=high.minutes_to_close(),
                        locked_profit_cents=profit_cents,
                        legs=2,
                        extra={"leg": "buy"},
                    ),
                    Signal(
                        ticker=low.ticker,
                        side=Side.NO,
                        kind=Kind.ARBITRAGE,
                        scanner=SCANNER_LADDER,
                        price_cents=100 - low_bid,
                        probability=(100 - low_bid) / 100,
                        contracts=size,
                        rationale=rationale,
                        invalidated_if=invalidated,
                        minutes_to_close=low.minutes_to_close(),
                        locked_profit_cents=profit_cents,
                        legs=2,
                        extra={"leg": "sell"},
                    ),
                ]
            )
    return signals


def scan_event(event: Event) -> list[Signal]:
    """Every structural scanner over one event."""
    signals: list[Signal] = []
    for market in event.markets:
        signals.extend(scan_yes_no_cross(market))
    signals.extend(scan_partition_sum(event))
    signals.extend(scan_ladder_monotonicity(event))
    return signals
