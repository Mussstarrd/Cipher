"""Settings stored as JSON in <data dir>/settings.json. Written atomically."""

from __future__ import annotations

import json
import os
import tempfile
from pathlib import Path

from . import UserError

DEFAULTS = {
    "artist": "",
    "library_root": "",
    "ffmpeg_path": "",
    "aspect": "9:16",
    "shot_len": 5,
    "clip_len": 30,
    "fade": 0.5,
}

ASPECTS = ("9:16", "16:9", "1:1")
SHOT_LENS = (5, 10)


def data_dir() -> Path:
    """Where everything lives. RELEASELINE_DATA or ./data (relative to the cwd)."""
    return Path(os.environ.get("RELEASELINE_DATA") or "data").expanduser().resolve()


def settings_path(base: Path | None = None) -> Path:
    return (base or data_dir()) / "settings.json"


def load(base: Path | None = None) -> dict:
    """Return settings merged over defaults. A corrupt file is a sentence, not a crash."""
    path = settings_path(base)
    merged = dict(DEFAULTS)
    if not path.exists():
        return merged
    try:
        raw = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError) as exc:
        raise UserError(
            f"The settings file at {path} could not be read ({exc}). "
            "Fix it or delete it to go back to defaults."
        )
    if not isinstance(raw, dict):
        raise UserError(f"The settings file at {path} should contain a JSON object.")
    merged.update({k: v for k, v in raw.items() if k in DEFAULTS})
    return merged


def validate(values: dict) -> dict:
    """Coerce and check incoming values. Returns a clean dict of only known keys."""
    clean = {}
    for key in DEFAULTS:
        if key not in values:
            continue
        val = values[key]
        if key in ("artist", "library_root", "ffmpeg_path"):
            clean[key] = str(val or "").strip()
        elif key == "aspect":
            if val not in ASPECTS:
                raise UserError("Aspect ratio must be one of 9:16, 16:9 or 1:1.")
            clean[key] = val
        elif key == "shot_len":
            try:
                n = int(val)
            except (TypeError, ValueError):
                raise UserError("Shot length must be 5 or 10 seconds.")
            if n not in SHOT_LENS:
                raise UserError("Shot length must be 5 or 10 seconds.")
            clean[key] = n
        elif key == "clip_len":
            try:
                n = float(val)
            except (TypeError, ValueError):
                raise UserError("Clip length must be a number of seconds.")
            if not 3 <= n <= 600:
                raise UserError("Clip length must be between 3 and 600 seconds.")
            clean[key] = int(n) if n == int(n) else n
        elif key == "fade":
            try:
                f = float(val)
            except (TypeError, ValueError):
                raise UserError("Fade must be a number of seconds, for example 0.5.")
            if not 0 <= f <= 10:
                raise UserError("Fade must be between 0 and 10 seconds.")
            clean[key] = f
    return clean


def save(values: dict, base: Path | None = None) -> dict:
    """Merge validated values into the file and return the new settings."""
    current = load(base)
    current.update(validate(values))
    path = settings_path(base)
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        fd, tmp = tempfile.mkstemp(prefix="settings-", suffix=".tmp", dir=str(path.parent))
        with os.fdopen(fd, "w", encoding="utf-8") as fh:
            json.dump(current, fh, indent=2)
        os.replace(tmp, path)
    except OSError as exc:
        raise UserError(f"Could not write settings to {path}: {exc.strerror or exc}.")
    return current


def library_root(settings: dict, base: Path | None = None) -> Path:
    """Library folder: Settings value (relative to the data dir if not absolute) or data/library."""
    base = base or data_dir()
    raw = (settings.get("library_root") or "").strip()
    if not raw:
        return base / "library"
    p = Path(raw).expanduser()
    if not p.is_absolute():
        p = base / p
    return p
