"""Paths and defaults. Everything is relative to the repo root unless an
environment variable overrides it, so the same checkout works on the Lenovo
(Windows) and on a server."""
from __future__ import annotations

import os
from pathlib import Path

ROOT = Path(os.environ.get("ANOM_ROOT", Path(__file__).resolve().parent.parent))
INBOX = Path(os.environ.get("ANOM_INBOX", ROOT / "inbox"))
OUT = Path(os.environ.get("ANOM_OUT", ROOT / "out"))
CATALOG = Path(os.environ.get("ANOM_CATALOG", ROOT / "catalog" / "catalog.json"))
FFMPEG = os.environ.get("ANOM_FFMPEG", "ffmpeg")

# Rights defaults. Pro is the plan Jeff is on; renders made while subscribed
# carry commercial rights (Suno help centre, Oct 2026). The render date is
# asked for per beat because the file's mtime is the download, not the render.
SUNO_PLAN = os.environ.get("ANOM_SUNO_PLAN", "Pro")

AUDIO_EXT = {".wav", ".mp3", ".flac", ".m4a", ".aiff", ".aif", ".ogg"}
PREVIEW_SECONDS = 30.0

# Step 3b: the store. Prices are the defaults from proposals.md D.7; each one
# can be overridden with ANOM_PRICE_<TIER> (e.g. ANOM_PRICE_MP3=24.99).
# Exclusive has no list price; it is by offer.
BRAND = "Anom Culture"
STORE_URL = "beatstars.com/anomculture"


def _price(tier: str, default: float | None) -> float | None:
    v = os.environ.get(f"ANOM_PRICE_{tier.upper()}")
    return float(v) if v else default


PRICES: dict[str, float | None] = {
    "mp3": _price("mp3", 29.99),
    "wav": _price("wav", 49.99),
    "trackout": _price("trackout", 99.99),
    "unlimited": _price("unlimited", 199.99),
    "exclusive": _price("exclusive", None),
}
