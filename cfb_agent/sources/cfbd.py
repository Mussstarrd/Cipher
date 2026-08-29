"""CollegeFootballData.com — the best free CFB data source (needs a free key).

Used for: SP+ ratings (primary rating input), preseason talent priors, and
per-book betting lines including closing lines for CLV.

Lines and games come back keyed by the ESPN event id, so they join to the
schedule on an integer with no name matching. Ratings and talent are keyed by
CFBD's `school` string and are resolved through the team registry, which fails
loudly rather than guessing.
"""

from typing import Optional

from .. import config, http
from ..teams import Registry

BASE = "https://api.collegefootballdata.com"

# /ratings/sp includes a synthetic row carrying league-wide means. It is not a
# team and must never be stored as one.
NON_TEAM_ROWS = {"nationalAverages"}


def _headers() -> dict:
    if not config.CFBD_API_KEY:
        raise http.FetchError("CFBD_API_KEY not set — get a free key at collegefootballdata.com/key")
    return {"Authorization": f"Bearer {config.CFBD_API_KEY}"}


def fetch_sp_ratings(year: int, reg: Registry) -> tuple[dict[int, float], list[str]]:
    """team id -> SP+ overall rating. Returns (ratings, unmapped team names)."""
    data = http.get_json(f"{BASE}/ratings/sp", params={"year": year}, headers=_headers())
    return _by_team_id(data, "team", "rating", reg)


def fetch_talent(year: int, reg: Registry) -> tuple[dict[int, float], list[str]]:
    """team id -> 247 talent composite. Returns (talent, unmapped team names)."""
    data = http.get_json(f"{BASE}/talent", params={"year": year}, headers=_headers())
    return _by_team_id(data, "team", "talent", reg)


def _by_team_id(rows, name_key: str, value_key: str, reg: Registry):
    out: dict[int, float] = {}
    unmapped: list[str] = []
    for row in rows:
        name = row.get(name_key) or row.get("school")
        if not name or name in NON_TEAM_ROWS:
            continue
        if row.get(value_key) is None:
            continue
        tid = reg.try_resolve(name)
        if tid is None:
            unmapped.append(name)
            continue
        try:
            out[tid] = float(row[value_key])
        except (TypeError, ValueError):
            continue
    return out, sorted(set(unmapped))


def fetch_lines(year: int, week: int) -> list[dict]:
    """Per-book spreads for a week, keyed by ESPN event id.

    Returns dicts: game_id, home_id, away_id, book, spread_home, spread_open.
    CFBD's `spread` is already from the home team's perspective.
    """
    data = http.get_json(f"{BASE}/lines", params={"year": year, "week": week}, headers=_headers())
    out = []
    for game in data:
        gid = game.get("id")
        if gid is None:
            continue
        for line in game.get("lines") or []:
            spread = line.get("spread")
            if spread is None:
                continue
            try:
                spread = float(spread)
            except (TypeError, ValueError):
                continue
            out.append({
                "game_id": str(gid),
                "home_id": _int(game.get("homeTeamId")),
                "away_id": _int(game.get("awayTeamId")),
                "book": line.get("provider") or "unknown",
                "spread_home": spread,
                "spread_open": _float(line.get("spreadOpen")),
            })
    return out


def fetch_games(year: int, week: int, season_type: str = "regular") -> list[dict]:
    """Schedule + results for a week, keyed by ESPN event id.

    Used by the backtest (which has no ESPN scoreboard for past weeks) and by
    the morning-after refresh to pick up final scores.
    """
    data = http.get_json(
        f"{BASE}/games",
        params={"year": year, "week": week, "seasonType": season_type},
        headers=_headers(),
        ttl=1800,
    )
    out = []
    for g in data:
        gid = g.get("id")
        home_id, away_id = _int(g.get("homeId")), _int(g.get("awayId"))
        if gid is None or home_id is None or away_id is None:
            continue
        out.append({
            "game_id": str(gid),
            "season": g.get("season"),
            "week": g.get("week"),
            "kickoff": g.get("startDate"),
            "home_id": home_id,
            "away_id": away_id,
            "home_name": g.get("homeTeam"),
            "away_name": g.get("awayTeam"),
            "home_class": g.get("homeClassification"),
            "away_class": g.get("awayClassification"),
            "neutral": 1 if g.get("neutralSite") else 0,
            "home_score": _int(g.get("homePoints")),
            "away_score": _int(g.get("awayPoints")),
            "completed": 1 if g.get("completed") else 0,
        })
    return out


def _int(v) -> Optional[int]:
    try:
        return int(v)
    except (TypeError, ValueError):
        return None


def _float(v) -> Optional[float]:
    try:
        return float(v)
    except (TypeError, ValueError):
        return None
