"""As-of-week power ratings fit from results, shrunk toward a preseason prior.

Why this exists
---------------
CFBD's rating endpoints are annual and return end-of-season values, so there is
no way to ask them "what was SP+ in week 6 of 2024?" — the answer they give
already knows how 2024 ended. The backtest's first cut therefore had to freeze
prior-year ratings for a whole season, which left an 8.6-point residual against
the market and no edge at all.

This module fits ratings directly from game results, using only games played
*before* the week being priced, so every rating is genuinely knowable at the
time. Ratings are shrunk toward a preseason prior, which is what makes the
thing work in September: with two games played the prior dominates, and by
November the results do.

The fit
-------
For each played game with home h, away a, and margin m (home minus away):

    m ~ r_h - r_a + hfa

minimising, over all played games,

    sum (m - (r_h - r_a + hfa))^2  +  lam * sum (r_i - prior_i)^2

`lam` is the shrinkage: it behaves like "how many games of evidence the prior is
worth". Solved by Gauss-Seidel coordinate descent, which for a few hundred teams
converges in well under a second and needs no linear-algebra dependency:

    r_i = (sum of per-game targets + lam * prior_i) / (games_played_i + lam)

Margins are capped before fitting. A 63-7 result says a team is good; it does
not say it is eight touchdowns better than its opponent, and uncapped blowouts
drag ratings around badly.
"""

from typing import Iterable, Optional

# Defaults; the evaluator tunes these on training seasons only.
DEFAULT_LAMBDA = 6.0
DEFAULT_MOV_CAP = 24.0
DEFAULT_HFA = 2.0
DEFAULT_ITERS = 60


def cap_margin(margin: float, cap: float) -> float:
    """Squash blowouts. Linear to `cap`, then heavily damped beyond it."""
    if abs(margin) <= cap:
        return margin
    sign = 1.0 if margin > 0 else -1.0
    return sign * (cap + (abs(margin) - cap) * 0.25)


def fit_ratings(
    games: Iterable[tuple[int, int, float, bool]],
    prior: dict[int, float],
    lam: float = DEFAULT_LAMBDA,
    hfa: float = DEFAULT_HFA,
    mov_cap: float = DEFAULT_MOV_CAP,
    iters: int = DEFAULT_ITERS,
) -> dict[int, float]:
    """Fit team ratings from played games, shrunk toward `prior`.

    `games` yields (home_id, away_id, home_margin, neutral). Only teams that
    appear in `prior` are rated — an opponent with no preseason rating (an FCS
    team) still contributes to its opponent's rating through the prior-shrunk
    fit, but is not itself returned as a bettable rating.
    """
    # Per-team game lists, so coordinate descent is a cheap local update.
    played: dict[int, list[tuple[int, float, float]]] = {}
    for home, away, margin, neutral in games:
        m = cap_margin(margin, mov_cap)
        h = 0.0 if neutral else hfa
        played.setdefault(home, []).append((away, m, h))
        played.setdefault(away, []).append((home, -m, -h))

    teams = set(prior) | set(played)
    # A team with no prior (FCS) is anchored at the worst prior seen, which is
    # closer to the truth than 0 (an average FBS team) and stops cupcake wins
    # from inflating ratings.
    floor = min(prior.values()) if prior else 0.0
    ratings: dict[int, float] = {t: prior.get(t, floor) for t in teams}
    anchors: dict[int, float] = dict(ratings)

    for _ in range(iters):
        delta = 0.0
        for team in teams:
            rows = played.get(team, ())
            total = lam * anchors[team]
            for opp, margin, home_edge in rows:
                # m = r_team - r_opp + home_edge  =>  r_team = m + r_opp - home_edge
                total += margin + ratings[opp] - home_edge
            new = total / (len(rows) + lam)
            delta = max(delta, abs(new - ratings[team]))
            ratings[team] = new
        if delta < 1e-4:
            break

    # Re-centre on the rated (prior-bearing) population so the scale stays
    # "points above an average FBS team".
    rated = [t for t in teams if t in prior]
    if rated:
        mean = sum(ratings[t] for t in rated) / len(rated)
        for t in ratings:
            ratings[t] -= mean
    return {t: ratings[t] for t in rated}


def predict(ratings: dict[int, float], home_id: int, away_id: int,
            neutral: bool, hfa: float = DEFAULT_HFA) -> Optional[float]:
    rh, ra = ratings.get(int(home_id)), ratings.get(int(away_id))
    if rh is None or ra is None:
        return None
    return rh - ra + (0.0 if neutral else hfa)
