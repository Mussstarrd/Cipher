"""Composite power ratings and game pricing.

Every source rating is on a points-above-average scale, so a game prices as:

    predicted_margin(home) = rating(home) - rating(away) + HFA

Early season (weeks 1..PRIOR_FADE_WEEK-1) the composite blends in the talent
prior, fading it out linearly as real games accumulate.

Ratings are keyed by team id throughout.
"""

import statistics

from . import config, db

# Relative trust in each source when averaging.
SOURCE_WEIGHTS = {"sp+": 0.5, "fpi": 0.35, "talent": 0.15}

# Every source claims to be "points above an average team", but they are not
# actually on the same scale: for 2026 preseason, SP+ has sd 13.55 across FBS
# while FPI has sd 11.33 (0.84x) despite correlating 0.965 with it. Averaging
# them raw shrinks every rating gap by a few percent, which on a slate full of
# big home favorites reads as a systematic "always take the underdog" bias
# (measured: the market's home spread sat 1.0 pt beyond ours on average, and
# every qualifying play landed on the dog). So each source is put on the
# reference source's scale before blending.
REFERENCE_SOURCE = "sp+"

# A team needs at least one of these to be priceable at all; the talent prior
# alone is not enough to bet a game on.
CORE_SOURCES = ("sp+", "fpi")


def weights_for_week(week: int) -> dict[str, float]:
    weights = dict(SOURCE_WEIGHTS)
    if week >= config.PRIOR_FADE_WEEK:
        weights.pop("talent", None)
    else:
        fade = 1.0 - (week - 1) / (config.PRIOR_FADE_WEEK - 1)
        weights["talent"] *= fade
    return weights


def _scalers(by_team: dict[int, dict[str, float]]) -> dict[str, tuple[float, float]]:
    """source -> (shift, scale) mapping it onto the reference source's scale."""
    stats: dict[str, tuple[float, float]] = {}
    for source in SOURCE_WEIGHTS:
        vals = [p[source] for p in by_team.values() if source in p]
        if len(vals) >= 3:
            stats[source] = (statistics.fmean(vals), statistics.pstdev(vals) or 1.0)
    ref = stats.get(REFERENCE_SOURCE) or next(iter(stats.values()), None)
    if ref is None:
        return {}
    ref_mean, ref_sd = ref
    return {s: (ref_mean - (ref_sd / sd) * m, ref_sd / sd) for s, (m, sd) in stats.items()}


def normalized_components(season: int, week: int) -> dict[int, dict[str, float]]:
    """Rating components with every source mapped onto the reference scale."""
    by_team = rating_components(season, week)
    scalers = _scalers(by_team)
    out: dict[int, dict[str, float]] = {}
    for team_id, parts in by_team.items():
        rescaled = {}
        for source, value in parts.items():
            shift_scale = scalers.get(source)
            if shift_scale is None:
                rescaled[source] = value
            else:
                shift, scale = shift_scale
                rescaled[source] = shift + scale * value
        out[team_id] = rescaled
    return out


def rating_components(season: int, week: int) -> dict[int, dict[str, float]]:
    """team id -> {source: rating} for everything stored for this week."""
    with db.connect() as conn:
        rows = conn.execute(
            "SELECT team_id, source, rating FROM ratings WHERE season=? AND week=?",
            (season, week),
        ).fetchall()
    by_team: dict[int, dict[str, float]] = {}
    for r in rows:
        by_team.setdefault(int(r["team_id"]), {})[r["source"]] = r["rating"]
    return by_team


def composite_ratings(season: int, week: int) -> dict[int, float]:
    """team id -> composite rating. Uses whichever sources actually loaded."""
    weights = weights_for_week(week)
    out: dict[int, float] = {}
    for team_id, sources in normalized_components(season, week).items():
        # Refuse to rate a team off the talent prior alone — that is how FCS
        # opponents and roster-churn outliers sneak onto a card.
        if not any(s in sources for s in CORE_SOURCES):
            continue
        avail = {s: w for s, w in weights.items() if s in sources and w > 0}
        if not avail:
            continue
        total = sum(avail.values())
        out[team_id] = sum(sources[s] * w for s, w in avail.items()) / total
    return out


def predict_margin(ratings: dict[int, float], home_id: int, away_id: int,
                   neutral: bool) -> float | None:
    """Predicted home-team margin of victory; None if a team is unrated."""
    rh, ra = ratings.get(int(home_id)), ratings.get(int(away_id))
    if rh is None or ra is None:
        return None
    hfa = config.NEUTRAL_SITE_HFA if neutral else config.HOME_FIELD_ADVANTAGE
    return rh - ra + hfa


def top_teams(season: int, week: int, n: int = 15) -> list[tuple[int, float, dict[str, float]]]:
    """Highest-rated teams, with their component ratings. The sanity check:
    an implausible list means the mapping or the parsing is broken."""
    comp = composite_ratings(season, week)
    parts = normalized_components(season, week)
    ranked = sorted(comp.items(), key=lambda kv: kv[1], reverse=True)[:n]
    return [(tid, rating, parts.get(tid, {})) for tid, rating in ranked]
