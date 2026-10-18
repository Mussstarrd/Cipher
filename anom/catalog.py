"""The catalogue: one JSON file, the single source of truth for every beat.

Keyed by beat id. Audio never lives in git; this file does. Every writer
goes through `upsert` so the schema stays in one place.
"""
from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path

from anom.config import CATALOG

SCHEMA_VERSION = 1


def _now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def slugify(name: str) -> str:
    s = re.sub(r"[^a-z0-9]+", "-", name.lower()).strip("-")
    return s or "beat"


def beat_id(stem: str, sha1: str) -> str:
    return f"{slugify(stem)}-{sha1[:6]}"


def load(path: Path = CATALOG) -> dict:
    if not path.exists():
        return {"schema": SCHEMA_VERSION, "beats": {}}
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def save(data: dict, path: Path = CATALOG) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(".tmp")
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, sort_keys=True)
        f.write("\n")
    tmp.replace(path)


def new_entry(bid: str, source: Path, analysis: dict, suno_plan: str, render_date: str | None) -> dict:
    return {
        "id": bid,
        "status": "analyzed",            # analyzed -> packaged -> listed -> promoted
        "source_file": source.name,
        "added_at": _now(),
        "analysis": analysis,
        "rights": {
            "generator": "Suno",
            "plan": suno_plan,
            "render_date": render_date,  # ask the owner; file mtime is the download
            "download_date": datetime.fromtimestamp(source.stat().st_mtime, timezone.utc).date().isoformat(),
            "song_id": None,
            "commercial_ok": suno_plan.lower() in {"pro", "premier"},
        },
        "package": {},                   # filled by step 3b
        "videos": [],                    # filled by step 3c
        "posts": [],                     # filled by step 3d
        "metrics": {},                   # filled by step 3e
        "anomaly": None,                 # one line, named in every post; owner or 3b fills it
    }


def upsert(data: dict, entry: dict) -> dict:
    beats = data.setdefault("beats", {})
    existing = beats.get(entry["id"])
    if existing:
        existing.update({k: v for k, v in entry.items() if k in ("analysis", "source_file")})
        existing["updated_at"] = _now()
    else:
        beats[entry["id"]] = entry
    return data


def find_by_sha1(data: dict, sha1: str) -> dict | None:
    for e in data.get("beats", {}).values():
        if e.get("analysis", {}).get("sha1") == sha1:
            return e
    return None
