"""Configuration: API keys from env vars, model knobs, file locations."""

import os
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = REPO_ROOT / "data"
CACHE_DIR = DATA_DIR / "cache"
RAW_DIR = DATA_DIR / "cache" / "raw"
REPORTS_DIR = REPO_ROOT / "reports"
DB_PATH = DATA_DIR / "cipher.sqlite"
REGISTRY_PATH = DATA_DIR / "team_registry.json"
ODDS_BUDGET_PATH = DATA_DIR / "oddsapi_budget.json"

# --- API keys (all optional; the pipeline degrades gracefully) ---------------
# Free key: https://collegefootballdata.com/key  (SP+, priors, closing lines)
CFBD_API_KEY = os.environ.get("CFBD_API_KEY", "")
# Free tier: https://the-odds-api.com  (multi-book lines for line shopping)
ODDS_API_KEY = os.environ.get("ODDS_API_KEY", "")

# --- Season / model knobs ----------------------------------------------------
SEASON = int(os.environ.get("CFB_SEASON", "2026"))

# Home-field advantage in points, flat for every team. League-average HFA has
# drifted well below the old 3.0 folklore; 2.0 is where recent seasons sit.
# Deliberately not team-specific: per-team HFA is not supported by the sample
# sizes involved and is the kind of knob that overfits a backtest.
HOME_FIELD_ADVANTAGE = 2.0
NEUTRAL_SITE_HFA = 0.0

# Weeks 1-4 blend the season rating with talent-based priors; the blend weight
# on priors decays linearly to 0 by PRIOR_FADE_WEEK.
PRIOR_FADE_WEEK = 5

# CFBD publishes preseason SP+ for the upcoming season in the summer, already
# folding in returning production, recruiting and portal movement. It is both
# knowable before week 1 and far more accurate than the prior season's final
# ratings, which describe a roster that has largely turned over. Set False to
# fall back to prior-season SP+ for weeks 1-4.
USE_PRESEASON_SP = True

# In-season rating fit (cfb_agent/model.py). RATING_SHRINKAGE is how many games
# of evidence the preseason prior is worth; tuned on 2023-2024 and held fixed.
RATING_SHRINKAGE = 3.0
RATING_MOV_CAP = 35.0

# Fallback scale for the talent prior when no SP+ ratings are available to
# calibrate against (points per standard deviation of talent composite).
TALENT_POINTS_PER_SD = 13.5

# --- Bet qualification -------------------------------------------------------
# Minimum absolute edge (our spread vs market spread, in points) to bet at all.
# Early-season ratings carry more uncertainty, so weeks 1-3 demand more edge.
EARLY_WEEKS_THROUGH = 3
MIN_EDGE_EARLY = 2.5      # weeks 1-3
MIN_EDGE_LATE = 2.0       # week 4 onward

# Fixed tier thresholds; the bottom tier floats with the week's minimum.
TIER_A_EDGE = 4.5         # 3-unit play: model and market disagree hard
TIER_B_EDGE = 3.5         # 2-unit play


def min_edge_for_week(week: int) -> float:
    """Minimum edge in points required to make a play at all, by week."""
    return MIN_EDGE_EARLY if week <= EARLY_WEEKS_THROUGH else MIN_EDGE_LATE


def tiers_for_week(week: int) -> list[tuple[float, float]]:
    """[(min_edge_points, units)] checked top-down, for a given week."""
    return [
        (TIER_A_EDGE, 3.0),
        (TIER_B_EDGE, 2.0),
        (min_edge_for_week(week), 1.0),
    ]


# Standard vig assumed when a book doesn't report a price.
DEFAULT_SPREAD_PRICE = -110

# Cap on plays per week; forced selectivity beats volume.
MAX_PLAYS_PER_WEEK = 10

# Any play whose model number differs from the market by this much is treated
# as a suspected data bug, not an edge, and is quarantined off the card.
ABSURD_EDGE_POINTS = 7.0

# --- Live-money gating -------------------------------------------------------
# Weeks 1-3 are paper only, no exceptions. Real money additionally requires a
# passing evaluation gate, recorded in data/gate_status.json by
# `python -m cfb_agent evaluate`. Absent that file, everything stays PAPER:
# the system will not promote itself to live on its own authority.
PAPER_ONLY_THROUGH_WEEK = 3
GATE_STATUS_PATH = DATA_DIR / "gate_status.json"

# --- HTTP / budget -----------------------------------------------------------
# Cache TTL for HTTP responses, seconds.
CACHE_TTL = 6 * 3600

# The Odds API free tier is 500 requests/month. We spend at most three
# snapshots a week (Tue open, Fri night, Sat ~60min pre-kick).
ODDS_MONTHLY_BUDGET = 500
ODDS_SNAPSHOTS_PER_WEEK = 3
ODDS_SPORT_KEY = "americanfootball_ncaaf"


def ensure_dirs() -> None:
    for d in (DATA_DIR, CACHE_DIR, RAW_DIR, REPORTS_DIR):
        d.mkdir(parents=True, exist_ok=True)
