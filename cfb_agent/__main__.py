"""CLI for the Cipher CFB agent.

    python -m cfb_agent refresh --week 1          # pull data into SQLite
    python -m cfb_agent card    --week 1          # price games, write reports/…md
    python -m cfb_agent card    --week 1 --log    # …and record plays in the bet log
    python -m cfb_agent settle  --week 1          # grade logged bets, compute CLV
    python -m cfb_agent summary                   # season record / ROI / avg CLV
    python -m cfb_agent demo                      # end-to-end run on synthetic data
"""

import argparse
import sys

from . import card, config, edges, fixtures, refresh, tracker


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(prog="cfb_agent")
    ap.add_argument("command", choices=["refresh", "card", "settle", "summary", "demo"])
    ap.add_argument("--season", type=int, default=config.SEASON)
    ap.add_argument("--week", type=int, default=1)
    ap.add_argument("--log", action="store_true", help="card: also record plays in the bet log")
    args = ap.parse_args(argv)

    if args.command == "refresh":
        status = refresh.refresh_week(args.season, args.week)
        for src, st in sorted(status.items()):
            print(f"  {src}: {st}")
        return 0

    if args.command == "card":
        plays, priced = edges.find_plays(args.season, args.week)
        content = card.render_card(args.season, args.week, plays, priced)
        path = card.write_card(args.season, args.week, content)
        print(f"{len(priced)} games priced, {len(plays)} plays -> {path}")
        if args.log:
            n = tracker.log_plays(args.season, args.week, plays)
            print(f"{n} plays logged to bet log")
        return 0

    if args.command == "settle":
        results = tracker.settle(args.season, args.week)
        if not results:
            print("Nothing to settle (no open bets on completed games).")
        for r in results:
            clv = f", CLV {r['clv']:+.1f}" if r["clv"] is not None else ""
            print(f"  {r['pick']}: {r['result'].upper()} ({r['profit']:+.2f}u{clv})")
        print(_summary_line(args.season))
        return 0

    if args.command == "summary":
        print(_summary_line(args.season))
        return 0

    if args.command == "demo":
        season, week = fixtures.DEMO_SEASON, args.week
        n = fixtures.seed_demo_week(week)
        print(f"Seeded {n} synthetic games (season {season}, week {week}).")
        plays, priced = edges.find_plays(season, week)
        content = card.render_card(season, week, plays, priced, demo=True)
        path = card.write_card(season, week, content)
        print(f"{len(priced)} games priced, {len(plays)} plays -> {path}")
        tracker.log_plays(season, week, plays)
        fixtures.simulate_results(week)
        results = tracker.settle(season, week)
        for r in results:
            clv = f", CLV {r['clv']:+.1f}" if r["clv"] is not None else ""
            print(f"  {r['pick']}: {r['result'].upper()} ({r['profit']:+.2f}u{clv})")
        print(_summary_line(season))
        return 0

    return 1


def _summary_line(season: int) -> str:
    s = tracker.season_summary(season)
    clv = f", avg CLV {s['avg_clv_points']:+.2f} pts" if s["avg_clv_points"] is not None else ""
    return (f"Season {season}: {s['record']} ({s['settled']} settled), "
            f"{s['profit_units']:+.2f}u, ROI {s['roi']:+.1f}%{clv}")


if __name__ == "__main__":
    sys.exit(main())
