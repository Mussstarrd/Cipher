"""Configuration: API keys from env vars, model knobs, file locations."""

import os
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = REPO_ROOT / "data"
CACHE_DIR = DATA_DIR / "cache"
REPORTS_DIR = REPO_ROOT / "reports"
DB_PATH = DATA_DIR / "cipher.sqlite"

# --- API keys (all optional; the pipeline degrades gracefully) ---------------
# Free key: https://collegefootballdata.com/key  (SP+, priors, closing lines)
CFBD_API_KEY = os.environ.get("CFBD_API_KEY", "")
# Free tier: https://the-odds-api.com  (multi-book lines for line shopping)
ODDS_API_KEY = os.environ.get("ODDS_API_KEY", "")

# --- Season / model knobs ----------------------------------------------------
SEASON = int(os.environ.get("CFB_SEASON", "2026"))

# Home-field advantage in points. League-average HFA has drifted down from the
# old 3.0 folklore; ~2.3 is closer to what recent seasons support.
HOME_FIELD_ADVANTAGE = 2.3
NEUTRAL_SITE_HFA = 0.0

# Weeks 1-4 blend last season's rating with talent-based priors; the blend
# weight on priors decays linearly to 0 by PRIOR_FADE_WEEK.
PRIOR_FADE_WEEK = 5

# Minimum absolute edge (our spread vs market spread, in points) to bet at all.
MIN_EDGE_POINTS = 1.5

# Confidence tiers: (min_edge_points, units). Checked top-down.
TIERS = [
    (4.0, 3.0),   # 3-unit play: model and market disagree hard
    (2.5, 2.0),   # 2-unit play
    (MIN_EDGE_POINTS, 1.0),  # 1-unit play
]

# Standard vig assumed when a book doesn't report a price.
DEFAULT_SPREAD_PRICE = -110

# Cap on plays per week; forced selectivity beats volume.
MAX_PLAYS_PER_WEEK = 10

# Cache TTL for HTTP responses, seconds.
CACHE_TTL = 6 * 3600


def ensure_dirs() -> None:
    for d in (DATA_DIR, CACHE_DIR, REPORTS_DIR):
        d.mkdir(parents=True, exist_ok=True)
