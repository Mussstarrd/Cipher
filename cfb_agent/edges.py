"""Edge finder: our number vs the best market number, tiered by confidence.

Conventions (everything is from the home team's perspective until a pick is
made): market spread_home of -6.5 means the home team is favored by 6.5.
Our model spread is -predicted_margin. Edge is how many points of value the
pick side gets versus our number.
"""

from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Optional

from . import config, db, ratings
from .teams import Registry


@dataclass
class Play:
    game_id: str
    kickoff: str
    home_id: int
    away_id: int
    home_team: str
    away_team: str
    neutral: bool
    predicted_margin: float          # home margin per model
    market_spread_home: float        # best available home spread
    best_book: str
    price: int
    pick_team_id: int
    pick_team: str
    pick_spread: float               # the number the pick side takes
    edge: float                      # points of value on the pick side
    units: float
    tier: str
    consensus_spread_home: float     # median home spread across books
    n_books: int
    all_books: dict[str, float] = field(default_factory=dict)
    quarantine: Optional[str] = None  # set when this is presumed a data bug


def find_plays(season: int, week: int, reg: Optional[Registry] = None,
               now: Optional[datetime] = None
               ) -> tuple[list[Play], list[Play], list[dict]]:
    """Returns (plays, quarantined, all priced games).

    Only games that have not kicked off are priceable — a game in progress is
    still `completed=0` in the schedule feed, and its pregame spread is no
    longer a number anyone can take.

    `quarantined` holds plays whose model-vs-market gap is so large it is more
    likely a data defect than an edge; they are kept out of the card until a
    human has looked at them.
    """
    now = now or datetime.now(timezone.utc)
    comp = ratings.composite_ratings(season, week)
    tiers = config.tiers_for_week(week)

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
    quarantined: list[Play] = []
    priced: list[dict] = []

    for g in games:
        if not _is_upcoming(g["kickoff"], now):
            continue
        margin = ratings.predict_margin(comp, g["home_id"], g["away_id"], bool(g["neutral"]))
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
            pick_id, pick_name = g["home_id"], g["home_team"]
            edge, row = home_edge, best_for_home
            pick_spread = row["spread_home"]
        else:
            pick_id, pick_name = g["away_id"], g["away_team"]
            edge, row = away_edge, best_for_away
            pick_spread = -row["spread_home"]

        spreads = sorted(b["spread_home"] for b in books)
        consensus = _median(spreads)
        # Disagreement is measured against the consensus, not the shopped
        # number, so a single stale book can't inflate it.
        disagreement = abs(model_spread_home - consensus)

        priced.append({
            "game_id": g["game_id"],
            "home_team": g["home_team"], "away_team": g["away_team"],
            "model_spread_home": model_spread_home,
            "market_spread_home": row["spread_home"],
            "consensus_spread_home": consensus,
            "edge": edge, "n_books": len(books),
        })

        units, tier = _tier_for_edge(edge, tiers)
        if units == 0:
            continue

        play = Play(
            game_id=g["game_id"], kickoff=g["kickoff"] or "",
            home_id=g["home_id"], away_id=g["away_id"],
            home_team=g["home_team"], away_team=g["away_team"],
            neutral=bool(g["neutral"]), predicted_margin=margin,
            market_spread_home=row["spread_home"], best_book=row["book"],
            price=row["price"] or config.DEFAULT_SPREAD_PRICE,
            pick_team_id=pick_id, pick_team=pick_name, pick_spread=pick_spread,
            edge=edge, units=units, tier=tier,
            consensus_spread_home=consensus, n_books=len(books),
            all_books={b["book"]: b["spread_home"] for b in books},
        )
        if disagreement >= config.ABSURD_EDGE_POINTS:
            play.quarantine = (
                f"model disagrees with the {len(books)}-book consensus by "
                f"{disagreement:.1f} pts — presumed data defect until verified"
            )
            quarantined.append(play)
        else:
            plays.append(play)

    plays.sort(key=lambda p: p.edge, reverse=True)
    quarantined.sort(key=lambda p: p.edge, reverse=True)
    return plays[: config.MAX_PLAYS_PER_WEEK], quarantined, priced


def _is_upcoming(kickoff: Optional[str], now: datetime) -> bool:
    """True if the game has not started. An unparseable kickoff is not bettable."""
    if not kickoff:
        return False
    try:
        ko = datetime.fromisoformat(kickoff.replace("Z", "+00:00"))
    except ValueError:
        return False
    if ko.tzinfo is None:
        ko = ko.replace(tzinfo=timezone.utc)
    return ko > now


def _median(xs: list[float]) -> float:
    n = len(xs)
    if n == 0:
        return 0.0
    mid = n // 2
    return xs[mid] if n % 2 else (xs[mid - 1] + xs[mid]) / 2


def _tier_for_edge(edge: float, tiers: list[tuple[float, float]]) -> tuple[float, str]:
    for (min_edge, units), name in zip(tiers, ("A", "B", "C")):
        if edge >= min_edge:
            return units, name
    return 0.0, ""
