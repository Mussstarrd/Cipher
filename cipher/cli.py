"""Command line entry point: ``python -m cipher <command>``.

Observe-only by default. There is no order-placing command in this build, and
that is deliberate -- the journal has to show a positive, calibrated edge over
the *market price* before execution is worth writing.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from .client import KalshiClient, KalshiError, parse_event
from .fees import DEFAULT_SCHEDULE, breakeven_probability
from .journal import Journal, summarise
from .model import Event
from .scanners import structural
from .signal import rank

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

    fees = sub.add_parser("fees", help="fee-adjusted breakeven table")
    fees.set_defaults(func=cmd_fees)

    calibrate = sub.add_parser("calibrate", help="score journalled signals")
    calibrate.add_argument("--journal", default="data/journal.jsonl")
    calibrate.set_defaults(func=cmd_calibrate)

    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    return args.func(args)
