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
