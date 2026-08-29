"""Synthetic fixture data for demoing the pipeline offline.

Teams and lines are invented (fictional schools, DEMO_SEASON=1999) so demo
output can never be mistaken for a real betting card.
"""

import random
from datetime import datetime, timezone

from . import config, db

DEMO_SEASON = 1999

TEAMS = [
    "Ashford Tech", "Brookfield", "Cascade A&M", "Dunmore", "Eastvale",
    "Fairport", "Granite Ridge", "Harlow", "Ironwood", "Juniper",
    "Kestrel Bay", "Loxley", "Meridian Vale", "Northgate", "Oakhurst",
    "Pinecrest", "Quarry Hill", "Rosemont", "Silverlake", "Thornbury",
]

BOOKS = ["DemoBook A", "DemoBook B", "DemoBook C"]


def seed_demo_week(week: int = 1, seed: int = 7) -> int:
    """Create a synthetic slate: ratings, games, and noisy multi-book lines."""
    rng = random.Random(seed + week)
    now = datetime.now(timezone.utc).isoformat(timespec="seconds")

    true_ratings = {t: rng.gauss(0, 12) for t in TEAMS}

    with db.connect() as conn:
        # Model sees the truth plus noise, split across two "sources".
        for source, noise in (("sp+", 2.0), ("fpi", 3.0)):
            for t, r in true_ratings.items():
                conn.execute(
                    "INSERT OR REPLACE INTO ratings (season, week, team, source, rating) VALUES (?,?,?,?,?)",
                    (DEMO_SEASON, week, t, source, r + rng.gauss(0, noise)),
                )

        teams = TEAMS[:]
        rng.shuffle(teams)
        n_games = 0
        for i in range(0, len(teams), 2):
            home, away = teams[i], teams[i + 1]
            gid = f"demo-{week}-{i // 2}"
            conn.execute(
                """INSERT OR REPLACE INTO games (game_id, season, week, kickoff, home_team,
                       away_team, neutral, completed) VALUES (?,?,?,?,?,?,0,0)""",
                (gid, DEMO_SEASON, week, f"1999-09-0{(i % 7) + 1}T19:30Z", home, away),
            )
            # Market centers near truth with its own error, books shade around it.
            fair = -(true_ratings[home] - true_ratings[away] + config.HOME_FIELD_ADVANTAGE)
            market = fair + rng.gauss(0, 2.5)
            for book in BOOKS:
                shade = rng.choice([-1.0, -0.5, 0.0, 0.5, 1.0])
                spread = round((market + shade) * 2) / 2
                conn.execute(
                    "INSERT OR REPLACE INTO lines (game_id, book, spread_home, price, fetched_at) VALUES (?,?,?,?,?)",
                    (gid, book, spread, -110, now),
                )
            n_games += 1
    return n_games


def simulate_results(week: int = 1, seed: int = 7) -> int:
    """Fill in plausible final scores for the demo slate so settle/CLV can run."""
    rng = random.Random(seed * 1000 + week)
    with db.connect() as conn:
        games = conn.execute(
            "SELECT * FROM games WHERE season=? AND week=?", (DEMO_SEASON, week)
        ).fetchall()
        ratings = {
            r["team"]: r["rating"]
            for r in conn.execute(
                "SELECT team, rating FROM ratings WHERE season=? AND week=? AND source='sp+'",
                (DEMO_SEASON, week),
            )
        }
        for g in games:
            margin = (ratings.get(g["home_team"], 0) - ratings.get(g["away_team"], 0)
                      + config.HOME_FIELD_ADVANTAGE + rng.gauss(0, 14))
            base = rng.randint(17, 34)
            home = max(0, round(base + margin / 2))
            away = max(0, round(base - margin / 2))
            if home == away:
                home += 3
            conn.execute(
                "UPDATE games SET home_score=?, away_score=?, completed=1 WHERE game_id=?",
                (home, away, g["game_id"]),
            )
        return len(games)
