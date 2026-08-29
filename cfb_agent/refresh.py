"""Refresh: pull every reachable source into SQLite. Partial failure is fine —
the card generator works with whatever landed and says what's missing.
"""

from datetime import datetime, timezone

from . import config, db
from .http import FetchError
from .sources import cfbd, espn, oddsapi


def refresh_week(season: int, week: int) -> dict:
    """Returns a status dict: source name -> 'ok: N rows' | 'skipped: reason'."""
    status: dict[str, str] = {}
    now = datetime.now(timezone.utc).isoformat(timespec="seconds")

    with db.connect() as conn:
        # --- games + ESPN BET line (keyless) --------------------------------
        try:
            games = espn.fetch_week(season, week)
            for g in games:
                conn.execute(
                    """INSERT INTO games (game_id, season, week, kickoff, home_team,
                           away_team, neutral, home_score, away_score, completed)
                       VALUES (?,?,?,?,?,?,?,?,?,?)
                       ON CONFLICT(game_id) DO UPDATE SET
                           home_score=excluded.home_score,
                           away_score=excluded.away_score,
                           completed=excluded.completed""",
                    (g["game_id"], season, week, g["kickoff"], g["home_team"],
                     g["away_team"], g["neutral"], g["home_score"], g["away_score"],
                     g["completed"]),
                )
                if g["spread_home"] is not None:
                    conn.execute(
                        "INSERT OR REPLACE INTO lines (game_id, book, spread_home, price, fetched_at) VALUES (?,?,?,?,?)",
                        (g["game_id"], g["book"], g["spread_home"], config.DEFAULT_SPREAD_PRICE, now),
                    )
            status["espn:games"] = f"ok: {len(games)} games"
        except FetchError as e:
            status["espn:games"] = f"skipped: {e}"

        # --- FPI ratings (keyless) ------------------------------------------
        try:
            fpi = espn.fetch_fpi(season)
            _store_ratings(conn, season, week, "fpi", fpi)
            status["espn:fpi"] = f"ok: {len(fpi)} teams"
        except FetchError as e:
            status["espn:fpi"] = f"skipped: {e}"

        # --- SP+ ratings (CFBD key) -----------------------------------------
        try:
            sp = cfbd.fetch_sp_ratings(season if week >= config.PRIOR_FADE_WEEK else season - 1)
            _store_ratings(conn, season, week, "sp+", sp)
            status["cfbd:sp+"] = f"ok: {len(sp)} teams"
        except FetchError as e:
            status["cfbd:sp+"] = f"skipped: {e}"

        # --- talent priors for early-season blending (CFBD key) -------------
        if week < config.PRIOR_FADE_WEEK:
            try:
                talent = cfbd.fetch_talent(season)
                _store_ratings(conn, season, week, "talent", _talent_to_points(talent))
                status["cfbd:talent"] = f"ok: {len(talent)} teams"
            except FetchError as e:
                status["cfbd:talent"] = f"skipped: {e}"

        # --- multi-book lines for line shopping (Odds API key) --------------
        try:
            rows = oddsapi.fetch_spreads()
            matched = _match_lines(conn, season, week, rows, now)
            status["oddsapi:lines"] = f"ok: {matched}/{len(rows)} book-lines matched"
        except FetchError as e:
            status["oddsapi:lines"] = f"skipped: {e}"

        # --- CFBD lines (also carries closing lines for CLV) ----------------
        try:
            rows = cfbd.fetch_lines(season, week)
            matched = _match_lines(conn, season, week, rows, now)
            status["cfbd:lines"] = f"ok: {matched}/{len(rows)} book-lines matched"
        except FetchError as e:
            status["cfbd:lines"] = f"skipped: {e}"

    return status


def _store_ratings(conn, season: int, week: int, source: str, ratings: dict[str, float]) -> None:
    for team, rating in ratings.items():
        conn.execute(
            "INSERT OR REPLACE INTO ratings (season, week, team, source, rating) VALUES (?,?,?,?,?)",
            (season, week, team, source, rating),
        )


def _talent_to_points(talent: dict[str, float]) -> dict[str, float]:
    """Rescale the 247 talent composite to a points-above-average scale so it
    can blend with SP+/FPI. Linear rescale to roughly [-25, +25]."""
    if not talent:
        return {}
    lo, hi = min(talent.values()), max(talent.values())
    span = (hi - lo) or 1.0
    return {t: (v - lo) / span * 50.0 - 25.0 for t, v in talent.items()}


def _match_lines(conn, season: int, week: int, rows: list[dict], now: str) -> int:
    """Attach book lines (keyed by team names) to games by fuzzy name match."""
    games = conn.execute(
        "SELECT game_id, home_team, away_team FROM games WHERE season=? AND week=?",
        (season, week),
    ).fetchall()
    index = {(_norm(g["home_team"]), _norm(g["away_team"])): g["game_id"] for g in games}
    matched = 0
    for row in rows:
        gid = index.get((_norm(row["home_team"]), _norm(row["away_team"])))
        if not gid:
            continue
        conn.execute(
            "INSERT OR REPLACE INTO lines (game_id, book, spread_home, price, fetched_at) VALUES (?,?,?,?,?)",
            (gid, row["book"], row["spread_home"], row.get("price", config.DEFAULT_SPREAD_PRICE), now),
        )
        matched += 1
    return matched


def _norm(name: str) -> str:
    """Normalize team names across sources ('Ohio State Buckeyes' ~ 'Ohio State')."""
    stop = {"university", "college"}
    words = [w for w in (name or "").lower().replace("&", "and").split() if w not in stop]
    return " ".join(words[:3])
