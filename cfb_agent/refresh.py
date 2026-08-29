"""Refresh: pull every reachable source into SQLite.

Partial failure is fine — the card generator works with whatever landed and
says what's missing. What is *not* fine is a silently wrong join, so every
name-keyed row that fails to resolve to a team id is counted and reported.
"""

import json
import statistics
from datetime import datetime, timezone
from typing import Optional

from . import config, db
from .http import FetchError
from .sources import cfbd, espn, oddsapi
from .teams import Registry


def refresh_week(season: int, week: int, odds_snapshot: bool = False,
                 mark_closing: bool = False) -> dict:
    """Returns a status dict: source name -> human-readable outcome."""
    status: dict[str, str] = {}
    now = datetime.now(timezone.utc).isoformat(timespec="seconds")

    try:
        reg = Registry.load(season)
        status["registry"] = f"ok: {len(reg.teams)} teams, {len(reg.lookup)} name keys"
    except FetchError as e:
        status["registry"] = f"FAILED: {e}"
        return status

    with db.connect() as conn:
        # --- games (ESPN scoreboard, keyless) --------------------------------
        espn_games: list[dict] = []
        try:
            espn_games = espn.fetch_week(season, week)
            for g in espn_games:
                _upsert_game(conn, season, week, g)
                for b in g["books"]:
                    _insert_line(conn, g["game_id"], b["book"], b["spread_home"],
                                 config.DEFAULT_SPREAD_PRICE, "espn", now)
            n_books = sum(len(g["books"]) for g in espn_games)
            status["espn:games"] = f"ok: {len(espn_games)} games, {n_books} book-lines"
        except FetchError as e:
            status["espn:games"] = f"skipped: {e}"

        # --- games (CFBD; fills in finals and anything ESPN's group missed) ---
        try:
            cfbd_games = cfbd.fetch_games(season, week)
            known = {g["game_id"] for g in espn_games}
            added = 0
            for g in cfbd_games:
                if g["game_id"] in known:
                    _update_score(conn, g)
                    continue
                # Only add games where at least one side is FBS; the rest are
                # noise we will never price.
                if "fbs" not in (g["home_class"], g["away_class"]):
                    continue
                _upsert_game(conn, season, week, g)
                added += 1
            status["cfbd:games"] = f"ok: {len(cfbd_games)} rows, {added} games ESPN missed"
        except FetchError as e:
            status["cfbd:games"] = f"skipped: {e}"

        # --- FPI ratings (keyless, id-keyed) ---------------------------------
        try:
            fpi = espn.fetch_fpi(season)
            _store_ratings(conn, season, week, "fpi", fpi)
            status["espn:fpi"] = f"ok: {len(fpi)} teams"
        except FetchError as e:
            status["espn:fpi"] = f"skipped: {e}"

        # --- SP+ ratings (CFBD) ----------------------------------------------
        sp_year = _sp_year(season, week)
        sp: dict[int, float] = {}
        try:
            sp, unmapped = cfbd.fetch_sp_ratings(sp_year, reg)
            _store_ratings(conn, season, week, "sp+", sp)
            note = f" [UNMAPPED: {', '.join(unmapped)}]" if unmapped else ""
            status["cfbd:sp+"] = f"ok: {len(sp)} teams (SP+ vintage {sp_year}){note}"
        except FetchError as e:
            status["cfbd:sp+"] = f"skipped: {e}"

        # --- talent priors for early-season blending (CFBD) -------------------
        if week < config.PRIOR_FADE_WEEK:
            try:
                talent, unmapped = cfbd.fetch_talent(season, reg)
                _store_ratings(conn, season, week, "talent", _talent_to_points(talent, sp))
                note = f" [UNMAPPED: {', '.join(unmapped)}]" if unmapped else ""
                status["cfbd:talent"] = f"ok: {len(talent)} teams{note}"
            except FetchError as e:
                status["cfbd:talent"] = f"skipped: {e}"

        # --- CFBD lines (id-joined; also carries opening numbers) -------------
        try:
            rows = cfbd.fetch_lines(season, week)
            matched = 0
            for r in rows:
                if _game_exists(conn, r["game_id"]):
                    _insert_line(conn, r["game_id"], r["book"], r["spread_home"],
                                 config.DEFAULT_SPREAD_PRICE, "cfbd", now)
                    matched += 1
            status["cfbd:lines"] = f"ok: {matched}/{len(rows)} book-lines joined by game id"
        except FetchError as e:
            status["cfbd:lines"] = f"skipped: {e}"

        # --- Odds API lines (name-keyed -> ids, then joined by team pair) ------
        try:
            rows, meta = oddsapi.fetch_spreads(season, week, reg, snapshot=odds_snapshot)
            pair_index = _pair_index(conn, season, week)
            matched, unjoined = 0, 0
            for r in rows:
                gid = pair_index.get((r["home_id"], r["away_id"]))
                if gid is None:
                    unjoined += 1
                    continue
                _insert_line(conn, gid, r["book"], r["spread_home"], r["price"], "oddsapi", now)
                matched += 1
            b = meta["budget"]
            unm = f" [UNMAPPED: {', '.join(meta['unmapped'])}]" if meta["unmapped"] else ""
            status["oddsapi:lines"] = (
                f"ok: {matched} book-lines joined ({unjoined} rows for games not on this "
                f"week's slate) — {meta['source']}, spent {meta['spent']} req; "
                f"month {b['used_this_month']}/{b['monthly_budget']}, "
                f"week snapshots {b['snapshots_this_week']}/{b['snapshots_allowed']}{unm}"
            )
        except oddsapi.BudgetExceeded as e:
            status["oddsapi:lines"] = f"budget-blocked: {e}"
        except FetchError as e:
            status["oddsapi:lines"] = f"skipped: {e}"

        if mark_closing:
            n = _mark_closing(conn, season, week)
            status["closing"] = f"ok: marked {n} lines as closing"

        conn.execute(
            "INSERT OR REPLACE INTO meta (key, value) VALUES (?, ?)",
            (f"refresh_status:{season}:{week}", json.dumps({"at": now, "status": status})),
        )

    return status


def last_status(season: int, week: int) -> Optional[dict]:
    """The source report from the most recent refresh of this week."""
    with db.connect() as conn:
        row = conn.execute(
            "SELECT value FROM meta WHERE key=?", (f"refresh_status:{season}:{week}",)
        ).fetchone()
    if not row:
        return None
    return json.loads(row["value"])["status"]


def _sp_year(season: int, week: int) -> int:
    """Which SP+ vintage to use for a given week.

    CFBD publishes preseason SP+ for the upcoming season in the summer, and it
    already folds in returning production, recruiting and portal movement. That
    is both knowable before week 1 and far more accurate than last season's
    final ratings, which describe a roster that has largely turned over. We use
    the current season's ratings whenever they exist and fall back to the prior
    season only if they do not.
    """
    if not config.USE_PRESEASON_SP:
        return season if week >= config.PRIOR_FADE_WEEK else season - 1
    return season


def _upsert_game(conn, season: int, week: int, g: dict) -> None:
    conn.execute(
        """INSERT INTO games (game_id, season, week, kickoff, home_id, away_id,
               home_team, away_team, neutral, home_score, away_score, completed)
           VALUES (?,?,?,?,?,?,?,?,?,?,?,?)
           ON CONFLICT(game_id) DO UPDATE SET
               kickoff=excluded.kickoff,
               home_score=excluded.home_score,
               away_score=excluded.away_score,
               completed=excluded.completed""",
        (g["game_id"], season, week, g["kickoff"], g["home_id"], g["away_id"],
         g["home_name"], g["away_name"], g["neutral"], g["home_score"],
         g["away_score"], g["completed"]),
    )


def _update_score(conn, g: dict) -> None:
    if g["home_score"] is None and not g["completed"]:
        return
    conn.execute(
        "UPDATE games SET home_score=?, away_score=?, completed=? WHERE game_id=?",
        (g["home_score"], g["away_score"], g["completed"], g["game_id"]),
    )


def _game_exists(conn, game_id: str) -> bool:
    return conn.execute("SELECT 1 FROM games WHERE game_id=?", (game_id,)).fetchone() is not None


def _pair_index(conn, season: int, week: int) -> dict[tuple[int, int], str]:
    rows = conn.execute(
        "SELECT game_id, home_id, away_id FROM games WHERE season=? AND week=?",
        (season, week),
    ).fetchall()
    return {(r["home_id"], r["away_id"]): r["game_id"] for r in rows}


def _insert_line(conn, game_id: str, book: str, spread_home: float,
                 price: Optional[int], source: str, now: str) -> None:
    conn.execute(
        """INSERT OR REPLACE INTO lines
               (game_id, book, spread_home, price, source, fetched_at)
           VALUES (?,?,?,?,?,?)""",
        (game_id, book, spread_home, price or config.DEFAULT_SPREAD_PRICE, source, now),
    )


def _store_ratings(conn, season: int, week: int, source: str, ratings: dict[int, float]) -> None:
    for team_id, rating in ratings.items():
        conn.execute(
            """INSERT OR REPLACE INTO ratings (season, week, team_id, source, rating)
               VALUES (?,?,?,?,?)""",
            (season, week, int(team_id), source, float(rating)),
        )


def _talent_to_points(talent: dict[int, float],
                      reference: Optional[dict[int, float]] = None) -> dict[int, float]:
    """Put the 247 talent composite on the same points-above-average scale as SP+.

    The old linear min/max rescale pinned whichever team happened to be last to
    exactly -25 and the leader to +25, which makes the prior's spread an
    artifact of the extremes rather than of the distribution. Standardizing and
    then scaling by a fixed points-per-standard-deviation keeps the prior
    commensurate with SP+ and stable from year to year.
    """
    if len(talent) < 3:
        return {}
    vals = list(talent.values())
    mean = statistics.fmean(vals)
    sd = statistics.pstdev(vals) or 1.0
    scale = config.TALENT_POINTS_PER_SD
    if reference and len(reference) >= 3:
        scale = statistics.pstdev(list(reference.values())) or scale
    return {t: (v - mean) / sd * scale for t, v in talent.items()}


def _mark_closing(conn, season: int, week: int) -> int:
    """Flag the last line seen for each (game, book) as that book's closing number."""
    conn.execute(
        """UPDATE lines SET is_closing=0
           WHERE game_id IN (SELECT game_id FROM games WHERE season=? AND week=?)""",
        (season, week),
    )
    cur = conn.execute(
        """UPDATE lines SET is_closing=1
           WHERE rowid IN (
               SELECT l.rowid FROM lines l
               JOIN games g ON g.game_id = l.game_id
               JOIN (SELECT game_id, book, MAX(fetched_at) AS ft
                       FROM lines GROUP BY game_id, book) x
                 ON x.game_id = l.game_id AND x.book = l.book AND x.ft = l.fetched_at
               WHERE g.season=? AND g.week=?)""",
        (season, week),
    )
    return cur.rowcount
