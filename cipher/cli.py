"""Command line entry point: ``python -m cipher <command>``.

Observe-only by default. There is no order-placing command in this build, and
that is deliberate -- the journal has to show a positive, calibrated edge over
the *market price* before execution is worth writing.
"""

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime
from pathlib import Path

from .client import KalshiClient, KalshiError, parse_event, parse_market
from .fees import DEFAULT_SCHEDULE, breakeven_probability
from .journal import Journal, summarise
from .model import Event
from .power import single_trade_verdict, trades_needed
from .resolvers import stations
from .resolvers.weather import WeatherResolver, parse_observations
from .scanners import disagreement, structural
from .signal import rank
from .ticket import size_to_stake

FIXTURES = Path(__file__).parent.parent / "tests" / "fixtures"


def _print_signals(signals) -> None:
    if not signals:
        print("no signals")
        return
    for signal in rank(signals):
        print(signal)
    print(f"\n{len(signals)} signal(s)")


def cmd_scan(args) -> int:
    """Pull live events and run the structural scanners over them."""
    client = KalshiClient(base_url=args.base_url)
    try:
        events = client.events(status="open", max_pages=args.max_pages)
    except KalshiError as exc:
        print(f"kalshi request failed: {exc}", file=sys.stderr)
        if exc.body:
            print(exc.body[:500], file=sys.stderr)
        return 2

    signals = []
    for event in events:
        if args.category and event.category != args.category:
            continue
        signals.extend(structural.scan_event(event))

    print(f"scanned {len(events)} events")
    _print_signals(signals)

    if args.journal:
        journal = Journal(args.journal)
        for signal in signals:
            journal.record(signal)
    return 0


def cmd_demo(args) -> int:
    """Run the scanners over bundled fixtures. No network required."""
    path = Path(args.fixture) if args.fixture else FIXTURES / "events.json"
    raw = json.loads(path.read_text())
    events: list[Event] = [parse_event(e) for e in raw["events"]]

    signals = []
    for event in events:
        found = structural.scan_event(event)
        print(f"\n{event.event_ticker}: {event.title}")
        for market in event.markets:
            q = market.quote
            print(
                f"   {market.ticker:<32} yes {q.yes_bid or '-':>3}/{q.yes_ask or '-':<3}"
                f"  no {q.no_bid or '-':>3}/{q.no_ask or '-':<3}"
            )
        print(f"   -> {len(found)} signal(s)")
        signals.extend(found)

    print()
    _print_signals(signals)
    return 0


def _weather_signals(markets, resolver, now=None):
    """Run the weather resolver and scanner, returning signals plus a per-market log.

    The log matters more than it looks: when a live run prints "no signals" the
    first question is always *why*, and the answers ("still before peak",
    "sitting on a rounding boundary", "the book already agrees") are all
    legitimate and all mean different things.
    """
    signals, log = [], []
    for market in markets:
        if not resolver.handles(market):
            log.append((market.ticker, "unmapped series or unparseable strike"))
            continue
        estimate = resolver.estimate(market, now=now)
        if estimate is None:
            log.append((market.ticker, "no estimate: before peak, no obs, or on a rounding edge"))
            continue
        found = disagreement.scan(market, estimate, disagreement.WEATHER, now=now)
        signals.extend(found)
        note = f"p={estimate.probability:.3f} conf={estimate.confidence:.2f}"
        if estimate.deterministic:
            note += " [settled]"
        if not estimate.detail.get("station_verified"):
            note += " [station UNVERIFIED -- confidence capped]"
        log.append((market.ticker, note + (f" -> {len(found)} signal" if found else " -> no edge")))
    return signals, log


def cmd_weather(args) -> int:
    """Scan daily high-temperature markets against live NWS observations."""
    if args.verify:
        series, station_id = args.verify.split(":", 1)
        confirmed = stations.verify(series, station_id)
        print(f"verified {series} -> {confirmed.station_id} ({confirmed.label})\n")

    now = None
    if args.fixture:
        payload = json.loads(Path(args.fixture).read_text())
        now = datetime.fromisoformat(payload["now"].replace("Z", "+00:00"))
        observations = parse_observations(payload["observations"])
        markets = [parse_market(m) for m in payload["markets"]]
        resolver = WeatherResolver(fetcher=lambda station_id, *, since, **_: observations)
        print(f"fixture: {args.fixture} (as of {now.isoformat()})\n")
    else:
        client = KalshiClient(base_url=args.base_url)
        try:
            markets = client.markets(status="open", series_ticker=args.series)
        except KalshiError as exc:
            print(f"kalshi request failed: {exc}", file=sys.stderr)
            return 2
        resolver = WeatherResolver()
        print(f"{args.series}: {len(markets)} open markets\n")

    try:
        signals, log = _weather_signals(markets, resolver, now=now)
    except Exception as exc:  # network or parse failure from NWS
        print(f"resolver failed: {exc}", file=sys.stderr)
        return 2

    for ticker, note in log:
        print(f"  {ticker:<30} {note}")
    print()

    if not signals:
        print("no signals: nothing is far enough from the book to be worth trading")
        if any("UNVERIFIED" in note for _, note in log):
            print(
                "note: every station here is unverified, which caps confidence and\n"
                "      suppresses signals by design. Read the series rulebook, confirm\n"
                "      the settlement station, then re-run with --verify SERIES:STATION."
            )
        return 0

    journal = Journal(args.journal) if args.journal else None
    for signal in rank(signals):
        ticket = size_to_stake(signal, int(round(args.stake * 100)))
        if ticket is None:
            print(f"{signal.ticker}: stake too small for one contract at {signal.price_cents}c")
            continue
        print(ticket.render())
        print(f"  settles     : after the NWS daily climate report for that station")
        print()
        if journal:
            journal.record(signal, traded=False)

    if journal:
        print(f"journalled to {journal.path} (traded=False -- set it yourself if you fill)")
    return 0


def cmd_fees(args) -> int:
    """Print the fee-adjusted breakeven table. The reality check."""
    sizes = [1, 50, 500]
    print(f"{'price':>6} {'fee/ct':>8}   " + "  ".join(f"be@{n:<5}" for n in sizes))
    for price in (5, 10, 25, 50, 75, 90, 95, 97, 99):
        fee_per_ct = DEFAULT_SCHEDULE.taker_fee_cents(price, 500) / 500
        cells = "  ".join(f"{breakeven_probability(price, n):<8.4f}" for n in sizes)
        print(f"{price:>5}c {fee_per_ct:>7.3f}c   {cells}")
    print(
        "\nbe@N = true probability needed to break even buying N contracts at that price.\n"
        "Note the 1-contract column: the per-order cent rounding makes small orders\n"
        "unprofitable at any price above ~97c regardless of how right you are."
    )
    return 0


def cmd_power(args) -> int:
    """How many settled signals before edge is distinguishable from luck."""
    print(single_trade_verdict(args.model, args.market))
    print()
    plan = trades_needed(args.model, args.market, alpha=args.alpha, target_power=args.target_power)
    if plan is None:
        print("no sample size reaches that power; the two hypotheses are too close")
        return 1
    print(plan.render())
    return 0


def cmd_calibrate(args) -> int:
    """Score everything the journal has seen."""
    journal = Journal(args.journal)
    rows = journal.read()
    if not rows:
        print(f"journal {journal.path} is empty")
        return 0

    summary = summarise(rows)
    print(f"signals recorded : {summary['signals']}")
    print(f"settled          : {summary['settled']}")
    if summary["settled"]:
        print(f"hit rate         : {summary['hit_rate']:.1%}")
        print(f"brier (model)    : {summary['brier_model']:.4f}")
        print(f"brier (market)   : {summary['brier_market']:.4f}   <- must beat this")
        print(f"realised pnl     : {summary['pnl_cents'] / 100:+.2f} USD")
        print("\ncalibration:")
        for row in summary["calibration"]:
            print(
                f"  {row['bucket']:>10}  n={row['n']:<5} "
                f"predicted={row['predicted']:.3f}  realised={row['realised']:.3f}"
            )
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="cipher", description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    scan = sub.add_parser("scan", help="live structural scan (needs network)")
    scan.add_argument("--base-url", default="https://api.elections.kalshi.com/trade-api/v2")
    scan.add_argument("--category", help="only scan events in this category")
    scan.add_argument("--max-pages", type=int, default=10)
    scan.add_argument("--journal", help="append signals to this journal file")
    scan.set_defaults(func=cmd_scan)

    demo = sub.add_parser("demo", help="run scanners over fixtures, offline")
    demo.add_argument("--fixture", help="path to an events JSON fixture")
    demo.set_defaults(func=cmd_demo)

    weather = sub.add_parser("weather", help="scan daily high-temperature markets")
    weather.add_argument("--series", default="KXHIGHNY", help="Kalshi series ticker")
    weather.add_argument("--stake", type=float, default=20.0, help="budget in USD")
    weather.add_argument("--fixture", help="offline fixture instead of live data")
    weather.add_argument("--journal", help="append signals to this journal file")
    weather.add_argument("--base-url", default="https://api.elections.kalshi.com/trade-api/v2")
    weather.add_argument(
        "--verify", metavar="SERIES:STATION",
        help="confirm a series' station after reading the rulebook, e.g. KXHIGHNY:KNYC",
    )
    weather.set_defaults(func=cmd_weather)

    fees = sub.add_parser("fees", help="fee-adjusted breakeven table")
    fees.set_defaults(func=cmd_fees)

    power = sub.add_parser("power", help="sample size needed to prove an edge")
    power.add_argument("--model", type=float, default=0.995, help="probability the model claims")
    power.add_argument("--market", type=float, default=0.92, help="probability the book implies")
    power.add_argument("--alpha", type=float, default=0.05)
    power.add_argument("--target-power", type=float, default=0.8)
    power.set_defaults(func=cmd_power)

    calibrate = sub.add_parser("calibrate", help="score journalled signals")
    calibrate.add_argument("--journal", default="data/journal.jsonl")
    calibrate.set_defaults(func=cmd_calibrate)

    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    return args.func(args)
