"""CollegeFootballData.com — the best free CFB data source (needs a free key).

Used for: SP+ ratings (primary rating input), preseason priors (team talent),
and per-book betting lines including closing lines for CLV.
"""

from .. import config, http

BASE = "https://api.collegefootballdata.com"


def _headers() -> dict:
    if not config.CFBD_API_KEY:
        raise http.FetchError("CFBD_API_KEY not set — get a free key at collegefootballdata.com/key")
    return {"Authorization": f"Bearer {config.CFBD_API_KEY}"}


def fetch_sp_ratings(year: int) -> dict[str, float]:
    """Team -> SP+ overall rating (points above average)."""
    data = http.get_json(f"{BASE}/ratings/sp", params={"year": year}, headers=_headers())
    return {
        row["team"]: float(row["rating"])
        for row in data
        if row.get("team") and row.get("rating") is not None
    }


def fetch_talent(year: int) -> dict[str, float]:
    """Team -> 247 talent composite. Used to build early-season priors."""
    data = http.get_json(f"{BASE}/talent", params={"year": year}, headers=_headers())
    return {row["school"]: float(row["talent"]) for row in data if row.get("school")}


def fetch_lines(year: int, week: int) -> list[dict]:
    """Per-book spreads for a week.

    Returns dicts: home_team, away_team, book, spread_home.
    CFBD's `spread` is from the home team's perspective already.
    """
    data = http.get_json(f"{BASE}/lines", params={"year": year, "week": week}, headers=_headers())
    out = []
    for game in data:
        for line in game.get("lines", []):
            if line.get("spread") is None:
                continue
            out.append(
                {
                    "home_team": game.get("homeTeam"),
                    "away_team": game.get("awayTeam"),
                    "book": line.get("provider", "unknown"),
                    "spread_home": float(line["spread"]),
                }
            )
    return out
