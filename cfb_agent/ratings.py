"""Composite power ratings and game pricing.

Every source rating is on a points-above-average scale, so a game prices as:

    predicted_margin(home) = rating(home) - rating(away) + HFA

Early season (weeks 1..PRIOR_FADE_WEEK-1) the composite blends in the talent
prior, fading it out linearly as real games accumulate.
"""

from . import config, db

# Relative trust in each source when averaging.
SOURCE_WEIGHTS = {"sp+": 0.5, "fpi": 0.35, "talent": 0.15}


def composite_ratings(season: int, week: int) -> dict[str, float]:
    """Team -> composite rating. Uses whichever sources actually loaded."""
    with db.connect() as conn:
        rows = conn.execute(
            "SELECT team, source, rating FROM ratings WHERE season=? AND week=?",
            (season, week),
        ).fetchall()

    by_team: dict[str, dict[str, float]] = {}
    for r in rows:
        by_team.setdefault(r["team"], {})[r["source"]] = r["rating"]

    weights = dict(SOURCE_WEIGHTS)
    if week >= config.PRIOR_FADE_WEEK:
        weights.pop("talent", None)
    else:
        # Scale the talent prior down as the season progresses.
        fade = 1.0 - (week - 1) / (config.PRIOR_FADE_WEEK - 1)
        weights["talent"] *= fade

    out: dict[str, float] = {}
    for team, sources in by_team.items():
        avail = {s: w for s, w in weights.items() if s in sources}
        if not avail:
            continue
        total = sum(avail.values())
        out[team] = sum(sources[s] * w for s, w in avail.items()) / total
    return out


def predict_margin(ratings: dict[str, float], home: str, away: str, neutral: bool) -> float | None:
    """Predicted home-team margin of victory; None if a team is unrated."""
    rh, ra = ratings.get(home), ratings.get(away)
    if rh is None or ra is None:
        return None
    hfa = config.NEUTRAL_SITE_HFA if neutral else config.HOME_FIELD_ADVANTAGE
    return rh - ra + hfa
