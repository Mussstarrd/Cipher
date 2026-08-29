"""ESPN public JSON endpoints — keyless.

Provides: schedule + scores + a DraftKings spread per game, and FPI ratings.

Everything here is keyed by ESPN's integer ids — event id for games, team id
for teams — which are the same ids CFBD uses. Nothing in this module matches on
team names.
"""

from typing import Optional

from .. import http

SCOREBOARD = "https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard"
FPI = "https://site.web.api.espn.com/apis/fitt/v3/sports/football/college-football/powerindex"


def fetch_week(season: int, week: int) -> list[dict]:
    """All FBS-group games for a week, with any attached book spreads.

    Returns dicts: game_id, kickoff, home_id, away_id, home_name, away_name,
    neutral, home_score, away_score, completed, books [{book, spread_home}].
    """
    data = http.get_json(
        SCOREBOARD,
        params={"groups": 80, "dates": season, "seasontype": 2, "week": week, "limit": 400},
        ttl=900,
    )
    games = []
    for event in data.get("events", []):
        comp = (event.get("competitions") or [{}])[0]
        sides = {c.get("homeAway"): c for c in comp.get("competitors", [])}
        home, away = sides.get("home"), sides.get("away")
        if not home or not away:
            continue
        home_id, away_id = _team_id(home), _team_id(away)
        if home_id is None or away_id is None:
            continue
        status = (event.get("status") or {}).get("type", {})
        games.append({
            "game_id": str(event.get("id")),
            "kickoff": event.get("date"),
            "home_id": home_id,
            "away_id": away_id,
            "home_name": (home.get("team") or {}).get("displayName"),
            "away_name": (away.get("team") or {}).get("displayName"),
            "neutral": 1 if comp.get("neutralSite") else 0,
            "home_score": _int(home.get("score")),
            "away_score": _int(away.get("score")),
            "completed": 1 if status.get("completed") else 0,
            "books": _parse_books(comp.get("odds") or []),
        })
    return games


def _parse_books(odds_entries: list) -> list[dict]:
    out = []
    for odds in odds_entries:
        spread = _parse_home_spread(odds)
        if spread is None:
            continue
        out.append({
            "book": (odds.get("provider") or {}).get("name") or "ESPN",
            "spread_home": spread,
        })
    return out


def _parse_home_spread(odds: dict) -> Optional[float]:
    """Return the spread from the home team's perspective (negative = favored).

    ESPN's numeric `spread` is home-relative in the payloads we've seen, but
    that is undocumented and has historically flipped. `homeTeamOdds.favorite`
    is the authoritative disambiguator, so we use the magnitude of `spread`
    and take the sign from the favorite flag whenever it is present.
    """
    raw = odds.get("spread")
    if raw is None:
        return _from_details(odds)
    try:
        val = float(raw)
    except (TypeError, ValueError):
        return _from_details(odds)

    home_odds = odds.get("homeTeamOdds") or {}
    if "favorite" in home_odds and abs(val) > 0:
        return -abs(val) if home_odds.get("favorite") else abs(val)
    return val


def _from_details(odds: dict) -> Optional[float]:
    """Fall back to a details string like 'UGA -3.5' / 'EVEN'."""
    details = (odds.get("details") or "").strip()
    if details.upper() in {"EVEN", "PK", "PICK"}:
        return 0.0
    parts = details.split()
    if len(parts) != 2:
        return None
    abbr, num = parts
    try:
        val = float(num)
    except ValueError:
        return None
    home_abbr = ((odds.get("homeTeamOdds") or {}).get("team") or {}).get("abbreviation")
    away_abbr = ((odds.get("awayTeamOdds") or {}).get("team") or {}).get("abbreviation")
    if abbr == home_abbr:
        return val
    if abbr == away_abbr:
        return -val
    return None


def fetch_fpi(season: int) -> dict[int, float]:
    """ESPN team id -> FPI rating (points above an average team, neutral field)."""
    data = http.get_json(
        FPI, params={"region": "us", "lang": "en", "limit": 400, "season": season}, ttl=6 * 3600
    )
    out: dict[int, float] = {}
    for entry in data.get("teams", []):
        tid = _int((entry.get("team") or {}).get("id"))
        if tid is None:
            continue
        for cat in entry.get("categories", []):
            if cat.get("name") != "fpi":
                continue
            # `values` carries full precision; `totals` is the rounded display.
            vals = cat.get("values") or []
            if vals:
                try:
                    out[tid] = float(vals[0])
                except (TypeError, ValueError):
                    pass
            break
    return out


def _team_id(competitor: dict) -> Optional[int]:
    return _int((competitor.get("team") or {}).get("id"))


def _int(v) -> Optional[int]:
    try:
        return int(v)
    except (TypeError, ValueError):
        return None
