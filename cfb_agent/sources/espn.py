"""ESPN public JSON endpoints — keyless.

Provides: schedule + scores + ESPN BET spread per game, and FPI ratings.
Endpoint shapes are undocumented and occasionally shift; parsers here are
defensive and skip records they can't read rather than crashing the run.
"""

from typing import Optional

from .. import http

SCOREBOARD = "https://site.api.espn.com/apis/site/v2/sports/football/college-football/scoreboard"
FPI = "https://site.web.api.espn.com/apis/fitt/v3/sports/football/college-football/powerindex"


def fetch_week(season: int, week: int) -> list[dict]:
    """All FBS games for a week, each with any ESPN BET home spread attached.

    Returns dicts: game_id, kickoff, home_team, away_team, neutral,
    home_score, away_score, completed, spread_home (or None), book.
    """
    data = http.get_json(
        SCOREBOARD,
        params={"groups": 80, "dates": season, "seasontype": 2, "week": week, "limit": 400},
    )
    games = []
    for event in data.get("events", []):
        comp = (event.get("competitions") or [{}])[0]
        sides = {c.get("homeAway"): c for c in comp.get("competitors", [])}
        home, away = sides.get("home"), sides.get("away")
        if not home or not away:
            continue
        status = (event.get("status") or {}).get("type", {})
        game = {
            "game_id": str(event.get("id")),
            "kickoff": event.get("date"),
            "home_team": home.get("team", {}).get("displayName"),
            "away_team": away.get("team", {}).get("displayName"),
            "neutral": 1 if comp.get("neutralSite") else 0,
            "home_score": _int(home.get("score")),
            "away_score": _int(away.get("score")),
            "completed": 1 if status.get("completed") else 0,
            "spread_home": None,
            "book": None,
        }
        odds = (comp.get("odds") or [{}])[0]
        spread = _parse_home_spread(odds, home, away)
        if spread is not None:
            game["spread_home"] = spread
            game["book"] = (odds.get("provider") or {}).get("name", "ESPN BET")
        games.append(game)
    return games


def _parse_home_spread(odds: dict, home: dict, away: dict) -> Optional[float]:
    """Return the spread from the home team's perspective (negative = favored)."""
    raw = odds.get("spread")
    if raw is None:
        # Fall back to details like "UGA -3.5".
        details = odds.get("details") or ""
        parts = details.split()
        if len(parts) != 2:
            return None
        abbr, num = parts
        try:
            val = float(num)
        except ValueError:
            return None
        home_abbr = home.get("team", {}).get("abbreviation")
        away_abbr = away.get("team", {}).get("abbreviation")
        if abbr == home_abbr:
            return val
        if abbr == away_abbr:
            return -val
        return None
    try:
        val = float(raw)
    except (TypeError, ValueError):
        return None
    # ESPN's numeric spread is favorite-relative in some payloads; the
    # homeTeamOdds.favorite flag disambiguates when present.
    home_odds = odds.get("homeTeamOdds") or {}
    if "favorite" in home_odds:
        return -abs(val) if home_odds.get("favorite") else abs(val)
    return val


def fetch_fpi(season: int) -> dict[str, float]:
    """Team display name -> FPI rating (points above average team)."""
    data = http.get_json(FPI, params={"region": "us", "lang": "en", "limit": 400, "season": season})
    out: dict[str, float] = {}
    for entry in data.get("teams", []):
        name = (entry.get("team") or {}).get("displayName")
        rating = None
        for cat in entry.get("categories", []):
            if cat.get("name") == "fpi" and cat.get("totals"):
                try:
                    rating = float(cat["totals"][0])
                except (ValueError, TypeError, IndexError):
                    rating = None
        if name and rating is not None:
            out[name] = rating
    return out


def _int(v) -> Optional[int]:
    try:
        return int(v)
    except (TypeError, ValueError):
        return None
