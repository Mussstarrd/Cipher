"""Edge finder: our number vs the best market number, tiered by confidence.

Conventions (everything is from the home team's perspective until a pick is
made): market spread_home of -6.5 means the home team is favored by 6.5.
Our model spread is -predicted_margin. Edge is how many points of value the
pick side gets versus our number.
"""

from dataclasses import dataclass, field

from . import config, db, ratings


@dataclass
class Play:
    game_id: str
    kickoff: str
    home_team: str
    away_team: str
    neutral: bool
    predicted_margin: float          # home margin per model
    market_spread_home: float        # best available home spread
    best_book: str
    price: int
    pick_team: str
    pick_spread: float               # the number the pick side takes
    edge: float                      # points of value on the pick side
    units: float
    all_books: dict[str, float] = field(default_factory=dict)

    @property
    def tier(self) -> str:
        return {3.0: "A", 2.0: "B", 1.0: "C"}.get(self.units, "C")


def find_plays(season: int, week: int) -> tuple[list[Play], list[dict]]:
    """Returns (plays sorted by edge desc, all priced games for the appendix)."""
    comp = ratings.composite_ratings(season, week)

    with db.connect() as conn:
        games = conn.execute(
            "SELECT * FROM games WHERE season=? AND week=? AND completed=0",
            (season, week),
        ).fetchall()
        line_rows = conn.execute(
            """SELECT l.game_id, l.book, l.spread_home, l.price FROM lines l
               JOIN (SELECT game_id, book, MAX(fetched_at) AS ft FROM lines GROUP BY game_id, book) x
                 ON l.game_id=x.game_id AND l.book=x.book AND l.fetched_at=x.ft""",
        ).fetchall()

    lines_by_game: dict[str, list] = {}
    for r in line_rows:
        lines_by_game.setdefault(r["game_id"], []).append(r)

    plays: list[Play] = []
    priced: list[dict] = []
    for g in games:
        margin = ratings.predict_margin(comp, g["home_team"], g["away_team"], bool(g["neutral"]))
        books = lines_by_game.get(g["game_id"], [])
        if margin is None or not books:
            continue
        model_spread_home = -margin

        # Line shopping: for each side, the best number is the one giving the
        # pick the most points. Home wants the largest spread_home; away the
        # smallest (most negative).
        best_for_home = max(books, key=lambda b: b["spread_home"])
        best_for_away = min(books, key=lambda b: b["spread_home"])

        home_edge = best_for_home["spread_home"] - model_spread_home
        away_edge = model_spread_home - best_for_away["spread_home"]

        if home_edge >= away_edge:
            pick_team, edge, row = g["home_team"], home_edge, best_for_home
            pick_spread = row["spread_home"]
        else:
            pick_team, edge, row = g["away_team"], away_edge, best_for_away
            pick_spread = -row["spread_home"]

        priced.append(
            {
                "home_team": g["home_team"], "away_team": g["away_team"],
                "model_spread_home": model_spread_home,
                "market_spread_home": row["spread_home"], "edge": edge,
            }
        )

        units = _units_for_edge(edge)
        if units == 0:
            continue
        plays.append(
            Play(
                game_id=g["game_id"], kickoff=g["kickoff"] or "",
                home_team=g["home_team"], away_team=g["away_team"],
                neutral=bool(g["neutral"]), predicted_margin=margin,
                market_spread_home=row["spread_home"], best_book=row["book"],
                price=row["price"] or config.DEFAULT_SPREAD_PRICE,
                pick_team=pick_team, pick_spread=pick_spread, edge=edge,
                units=units,
                all_books={b["book"]: b["spread_home"] for b in books},
            )
        )

    plays.sort(key=lambda p: p.edge, reverse=True)
    return plays[: config.MAX_PLAYS_PER_WEEK], priced


def _units_for_edge(edge: float) -> float:
    for min_edge, units in config.TIERS:
        if edge >= min_edge:
            return units
    return 0.0
