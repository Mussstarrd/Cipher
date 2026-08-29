"""CLI for the LineHawk / Cipher CFB agent.

    python -m cfb_agent refresh  --week 1              # pull data into SQLite
    python -m cfb_agent refresh  --week 1 --odds-snapshot   # ...spending 1 Odds API req
    python -m cfb_agent refresh  --week 1 --closing    # ...and mark closing lines
    python -m cfb_agent top      --week 1              # sanity check: top 15 teams
    python -m cfb_agent mapping  --week 1              # prove the team/line joins
    python -m cfb_agent card     --week 1              # price games, write the card
    python -m cfb_agent card     --week 1 --log        # ...and record plays
    python -m cfb_agent settle   --week 1              # grade bets, compute CLV
    python -m cfb_agent summary                        # season record / ROI / CLV
    python -m cfb_agent budget   --week 1              # Odds API spend so far
    python -m cfb_agent demo                           # end-to-end on synthetic data
"""

import argparse
import sys

from . import card, config, db, edges, fixtures, ratings, refresh, tracker
from .sources import oddsapi
from .teams import Registry

COMMANDS = ["refresh", "top", "mapping", "card", "settle", "summary", "budget", "demo"]


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(prog="cfb_agent")
    ap.add_argument("command", choices=COMMANDS)
    ap.add_argument("--season", type=int, default=config.SEASON)
    ap.add_argument("--week", type=int, default=1)
    ap.add_argument("--log", action="store_true", help="card: also record plays in the bet log")
    ap.add_argument("--odds-snapshot", action="store_true",
                    help="refresh: spend one metered Odds API request")
    ap.add_argument("--closing", action="store_true",
                    help="refresh: mark the latest line per book as that book's close")
    ap.add_argument("--refresh-registry", action="store_true",
                    help="rebuild the team id registry from both providers")
    ap.add_argument("-n", type=int, default=15, help="top: how many teams to show")
    args = ap.parse_args(argv)

    if args.refresh_registry:
        reg = Registry.load(args.season, refresh=True)
        print(f"registry rebuilt: {len(reg.teams)} teams, {len(reg.lookup)} name keys")

    if args.command == "refresh":
        status = refresh.refresh_week(args.season, args.week,
                                      odds_snapshot=args.odds_snapshot,
                                      mark_closing=args.closing)
        for src, st in sorted(status.items()):
            print(f"  {src}: {st}")
        return 0

    if args.command == "top":
        return _cmd_top(args.season, args.week, args.n)

    if args.command == "mapping":
        return _cmd_mapping(args.season, args.week)

    if args.command == "card":
        mode = tracker.mode_for_week(args.week)
        plays, quarantined, priced = edges.find_plays(args.season, args.week)
        content = card.render_card(args.season, args.week, plays, quarantined, priced,
                                   refresh_status=refresh.last_status(args.season, args.week),
                                   mode=mode)
        path = card.write_card(args.season, args.week, content, mode=mode)
        print(f"{len(priced)} games priced, {len(plays)} plays, "
              f"{len(quarantined)} quarantined -> {path}  [{mode}]")
        if args.log:
            n = tracker.log_plays(args.season, args.week, plays, mode=mode)
            print(f"{n} plays logged to bet log as {mode}")
        return 0

    if args.command == "settle":
        results = tracker.settle(args.season, args.week)
        if not results:
            print("Nothing to settle (no open bets on completed games).")
        for r in results:
            clv = f", CLV {r['clv']:+.1f}" if r["clv"] is not None else ", CLV n/a"
            print(f"  [{r['mode']}] {r['pick']}: {r['result'].upper()} "
                  f"({r['profit']:+.2f}u{clv}) final {r['score']}")
        print(_summary_line(args.season))
        return 0

    if args.command == "summary":
        print(_summary_line(args.season))
        return 0

    if args.command == "budget":
        st = oddsapi.budget_status(args.season, args.week)
        print(f"  Odds API month {st['month']}: {st['used_this_month']}/{st['monthly_budget']} "
              f"used, {st['remaining_this_month']} remaining")
        print(f"  week {args.season}-{args.week}: {st['snapshots_this_week']}/"
              f"{st['snapshots_allowed']} snapshots")
        for t in st["snapshot_times"]:
            print(f"    - {t}")
        return 0

    if args.command == "demo":
        return _cmd_demo(args.week)

    return 1


def _cmd_top(season: int, week: int, n: int) -> int:
    reg = Registry.load(season)
    rows = ratings.top_teams(season, week, n)
    if not rows:
        print("No ratings stored — run `refresh` first.")
        return 1
    w = ratings.weights_for_week(week)
    print(f"Composite weights for week {week}: "
          + ", ".join(f"{k} {v:.3f}" for k, v in w.items() if v > 0))
    print(f"{'#':>3}  {'team':28s} {'comp':>7} {'sp+':>7} {'fpi':>7} {'talent':>7}")
    for i, (tid, rating, parts) in enumerate(rows, 1):
        print(f"{i:>3}  {reg.name(tid)[:28]:28s} {rating:7.2f} "
              f"{_g(parts,'sp+')} {_g(parts,'fpi')} {_g(parts,'talent')}")
    return 0


def _g(parts: dict, key: str) -> str:
    v = parts.get(key)
    return f"{v:7.2f}" if v is not None else f"{'--':>7}"


def _cmd_mapping(season: int, week: int) -> int:
    """Show, per game, every source's name for each team and every book's number.

    This is the proof that the id join works: if it is broken, the names in a
    row will not describe the same game.
    """
    reg = Registry.load(season)
    # Compute ratings before opening a connection: db.connect() is not reentrant.
    comp = ratings.composite_ratings(season, week)
    with db.connect() as conn:
        games = conn.execute(
            """SELECT g.*, COUNT(DISTINCT l.book) nb FROM games g
               LEFT JOIN lines l ON l.game_id=g.game_id
               WHERE g.season=? AND g.week=?
               GROUP BY g.game_id HAVING nb > 0
               ORDER BY nb DESC, g.kickoff LIMIT 10""",
            (season, week),
        ).fetchall()
        if not games:
            print("No games with lines — run `refresh` first.")
            return 1
        for g in games:
            print(f"\n=== game_id {g['game_id']}  ({g['kickoff']})"
                  + ("  [neutral]" if g["neutral"] else "") + " ===")
            for side in ("away", "home"):
                tid = g[f"{side}_id"]
                t = reg.teams.get(int(tid), {})
                rating = comp.get(int(tid))
                print(f"  {side:4s} id={tid:<6} ESPN={g[side + '_team']!r}")
                print(f"       {'':11s}CFBD={t.get('school')!r}  "
                      f"ESPN-reg={t.get('espn_name')!r}  class={t.get('classification')}  "
                      f"rating={'%.2f' % rating if rating is not None else 'UNRATED'}")
            rows = conn.execute(
                """SELECT l.book, l.spread_home, l.price, l.source FROM lines l
                   JOIN (SELECT game_id, book, MAX(fetched_at) ft FROM lines
                         WHERE game_id=? GROUP BY game_id, book) x
                     ON x.game_id=l.game_id AND x.book=l.book AND x.ft=l.fetched_at
                   ORDER BY l.source, l.book""",
                (g["game_id"],),
            ).fetchall()
            print(f"  books ({len(rows)}):")
            for r in rows:
                print(f"     {r['source']:8s} {r['book']:22s} home {r['spread_home']:+6.1f} "
                      f"@ {r['price']}")
    return 0


def _cmd_demo(week: int) -> int:
    season = fixtures.DEMO_SEASON
    n = fixtures.seed_demo_week(week)
    print(f"Seeded {n} synthetic games (season {season}, week {week}).")
    plays, quarantined, priced = edges.find_plays(season, week)
    content = card.render_card(season, week, plays, quarantined, priced, demo=True)
    path = card.write_card(season, week, content, mode="DEMO")
    print(f"{len(priced)} games priced, {len(plays)} plays -> {path}")
    tracker.log_plays(season, week, plays, mode="PAPER")
    fixtures.simulate_results(week)
    for r in tracker.settle(season, week):
        clv = f", CLV {r['clv']:+.1f}" if r["clv"] is not None else ""
        print(f"  {r['pick']}: {r['result'].upper()} ({r['profit']:+.2f}u{clv})")
    print(_summary_line(season))
    return 0


def _summary_line(season: int) -> str:
    s = tracker.season_summary(season)
    clv = f", avg CLV {s['avg_clv_points']:+.2f} pts" if s["avg_clv_points"] is not None else ""
    beat = f", beat close {s['beat_close_pct']:.0f}% (n={s['clv_sample']})" \
        if s["beat_close_pct"] is not None else ""
    return (f"Season {season}: {s['record']} ({s['settled']} settled), "
            f"{s['profit_units']:+.2f}u, ROI {s['roi']:+.1f}%{clv}{beat}")


if __name__ == "__main__":
    sys.exit(main())
