"""The Odds API — multi-book live spreads for line shopping (needs a key).

Free tier is 500 requests/month; one call returns every book for every game,
so a weekly refresh costs ~1-2 requests.
"""

from .. import config, http

BASE = "https://api.the-odds-api.com/v4/sports/americanfootball_ncaaf/odds"


def fetch_spreads() -> list[dict]:
    """Current spreads across books.

    Returns dicts: home_team, away_team, book, spread_home, price.
    """
    if not config.ODDS_API_KEY:
        raise http.FetchError("ODDS_API_KEY not set — get a free key at the-odds-api.com")
    data = http.get_json(
        BASE,
        params={
            "apiKey": config.ODDS_API_KEY,
            "regions": "us",
            "markets": "spreads",
            "oddsFormat": "american",
        },
        ttl=1800,  # lines move; keep this cache short
    )
    out = []
    for game in data:
        home, away = game.get("home_team"), game.get("away_team")
        for book in game.get("bookmakers", []):
            for market in book.get("markets", []):
                if market.get("key") != "spreads":
                    continue
                for outcome in market.get("outcomes", []):
                    if outcome.get("name") != home or outcome.get("point") is None:
                        continue
                    out.append(
                        {
                            "home_team": home,
                            "away_team": away,
                            "book": book.get("title", book.get("key", "unknown")),
                            "spread_home": float(outcome["point"]),
                            "price": int(outcome.get("price", config.DEFAULT_SPREAD_PRICE)),
                        }
                    )
    return out
