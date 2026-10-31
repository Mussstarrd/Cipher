"""Tracks, stages, assets, drafts, posts and feedback. All writes are transactional."""

from __future__ import annotations

import json
import re
import unicodedata
from pathlib import Path

from . import NotFound, UserError
from .db import now, one, rows, transaction
from .settings import library_root

STAGE_KEYS = ["download", "master", "clip", "prompt", "video", "assemble", "meta", "upload"]

STAGE_TITLES = {
    "download": "Download from Suno",
    "master": "Master in FL Studio",
    "clip": "Cut the 30-second clip",
    "prompt": "Video prompt",
    "video": "Generate video in Kling",
    "assemble": "Edit together",
    "meta": "Tags and description",
    "upload": "Upload",
}

NEXT_ACTION = {
    "download": "Download the track from Suno into source/ and record the path.",
    "master": "Master it in FL Studio, export WAV + MP3 into master/, record the path.",
    "clip": "Pick the hook start time and cut the clip.",
    "prompt": "Generate a video prompt and pick one.",
    "video": "Generate the shots in Kling and save them into video/.",
    "assemble": "Assemble the shots with the audio clip.",
    "meta": "Generate titles, descriptions and tags.",
    "upload": "Upload to each enabled platform and paste the posted URLs.",
}

LIBRARY_FOLDERS = ["source", "master", "clip", "video", "final"]
ENERGIES = ("low", "mid", "high")
STATUSES = ("active", "released", "archived")
ASSET_KINDS = ("source", "master", "clip", "video", "final")
DRAFT_KINDS = ("prompt", "shotlist", "title", "description", "tags", "hashtags")

# Stage data fields that hold a file path, and the asset kind recorded for them.
PATH_FIELDS = {
    "download": {"path": "source"},
    "master": {"path": "master"},
    "clip": {"wav": "clip", "mp3": "clip"},
    "assemble": {"final": "final"},
}


def slugify(title: str) -> str:
    text = unicodedata.normalize("NFKD", title).encode("ascii", "ignore").decode()
    text = re.sub(r"[^a-zA-Z0-9]+", "-", text).strip("-").lower()
    return text[:60].strip("-") or "track"


def unique_slug(conn, title: str) -> str:
    base = slugify(title)
    slug, n = base, 2
    while one(conn.execute("SELECT id FROM tracks WHERE slug = ?", (slug,))):
        slug = f"{base}-{n}"
        n += 1
    return slug


def track_dir(settings: dict, slug: str, base: Path | None = None) -> Path:
    return library_root(settings, base) / slug


def ensure_library(settings: dict, slug: str, base: Path | None = None) -> Path:
    root = track_dir(settings, slug, base)
    try:
        for name in LIBRARY_FOLDERS:
            (root / name).mkdir(parents=True, exist_ok=True)
    except OSError as exc:
        raise UserError(
            f"Could not create the library folder {root} ({exc.strerror or exc}). "
            "Check the library root in Settings."
        )
    return root


def _clean_fields(fields: dict, settings: dict, creating: bool) -> dict:
    clean = {}
    text_keys = ("title", "artist", "genre", "mood", "bpm", "key", "hook", "suno_url", "notes")
    for k in text_keys:
        if k in fields and fields[k] is not None:
            clean[k] = str(fields[k]).strip()
    if creating:
        if not clean.get("title"):
            raise UserError("Give the track a title. Everything else is optional.")
        clean.setdefault("artist", settings.get("artist", ""))
    elif "title" in clean and not clean["title"]:
        raise UserError("The title cannot be empty.")
    if len(clean.get("title", "")) > 200:
        raise UserError("Keep the title under 200 characters.")
    if "bpm" in clean and clean["bpm"]:
        if not re.fullmatch(r"\d{2,3}(\.\d+)?", clean["bpm"]):
            raise UserError("BPM should be a number like 140, or left blank.")
    if "suno_url" in clean and clean["suno_url"]:
        if not re.match(r"https?://", clean["suno_url"]):
            raise UserError("The Suno link should start with http:// or https://, or be blank.")
    if "energy" in fields and fields["energy"] is not None:
        e = str(fields["energy"]).strip().lower() or "mid"
        if e not in ENERGIES:
            raise UserError("Energy must be low, mid or high.")
        clean["energy"] = e
    if "status" in fields and fields["status"] is not None:
        s = str(fields["status"]).strip().lower()
        if s not in STATUSES:
            raise UserError("Status must be active, released or archived.")
        clean["status"] = s
    return clean


def create_track(conn, settings: dict, fields: dict, base: Path | None = None) -> dict:
    clean = _clean_fields(fields, settings, creating=True)
    clean.setdefault("energy", "mid")
    slug = unique_slug(conn, clean["title"])
    ensure_library(settings, slug, base)
    ts = now()
    with transaction(conn):
        cur = conn.execute(
            "INSERT INTO tracks(slug, title, artist, genre, mood, energy, bpm, key, hook, "
            "suno_url, notes, status, created_at, updated_at) VALUES "
            "(?,?,?,?,?,?,?,?,?,?,?,'active',?,?)",
            (
                slug, clean["title"], clean.get("artist", ""), clean.get("genre", ""),
                clean.get("mood", ""), clean["energy"], clean.get("bpm", ""),
                clean.get("key", ""), clean.get("hook", ""), clean.get("suno_url", ""),
                clean.get("notes", ""), ts, ts,
            ),
        )
        track_id = cur.lastrowid
        for key in STAGE_KEYS:
            conn.execute(
                "INSERT INTO stages(track_id, key, state, data) VALUES (?, ?, 'todo', '{}')",
                (track_id, key),
            )
    return get_track(conn, track_id)


def get_track(conn, ident) -> dict:
    """Find by numeric id or slug. Raises NotFound with a sentence."""
    ident_s = str(ident).strip()
    row = None
    if ident_s.isdigit():
        row = one(conn.execute("SELECT * FROM tracks WHERE id = ?", (int(ident_s),)))
    if row is None:
        row = one(conn.execute("SELECT * FROM tracks WHERE slug = ?", (ident_s,)))
    if row is None:
        raise NotFound(f"No track called '{ident_s}'. Run 'list' or open the Dashboard to see your tracks.")
    return row


def update_track(conn, track_id: int, fields: dict, settings: dict) -> dict:
    clean = _clean_fields(fields, settings, creating=False)
    if not clean:
        raise UserError("Nothing to update.")
    sets = ", ".join(f"{k} = ?" for k in clean)
    with transaction(conn):
        conn.execute(
            f"UPDATE tracks SET {sets}, updated_at = ? WHERE id = ?",
            (*clean.values(), now(), track_id),
        )
    return get_track(conn, track_id)


def touch(conn, track_id: int) -> None:
    conn.execute("UPDATE tracks SET updated_at = ? WHERE id = ?", (now(), track_id))


def get_stages(conn, track_id: int) -> list[dict]:
    found = {r["key"]: r for r in rows(conn.execute(
        "SELECT * FROM stages WHERE track_id = ?", (track_id,)))}
    out = []
    for key in STAGE_KEYS:
        r = found.get(key) or {"key": key, "state": "todo", "data": "{}", "done_at": None}
        try:
            data = json.loads(r.get("data") or "{}")
        except ValueError:
            data = {}
        out.append({
            "key": key, "title": STAGE_TITLES[key], "state": r["state"],
            "data": data if isinstance(data, dict) else {}, "done_at": r.get("done_at"),
        })
    return out


def current_stage_key(stages: list[dict]) -> str | None:
    for s in stages:
        if s["state"] == "todo":
            return s["key"]
    return None


def _check_path_fields(key: str, data: dict) -> list[tuple[str, str]]:
    """Return (kind, path) pairs for path fields in data, after verifying they exist."""
    found = []
    for field, kind in PATH_FIELDS.get(key, {}).items():
        if field in data and data[field] is not None:
            raw = str(data[field]).strip()
            data[field] = raw
            if not raw:
                continue
            p = Path(raw).expanduser()
            if not p.is_file():
                raise UserError(f"File not found: {raw}. Check the path and try again.")
            found.append((kind, str(p)))
    if key == "video" and isinstance(data.get("shots"), list):
        cleaned = []
        for raw in data["shots"]:
            raw = str(raw or "").strip()
            if not raw:
                continue
            p = Path(raw).expanduser()
            if not p.is_file():
                raise UserError(f"File not found: {raw}. Check the path and try again.")
            cleaned.append(str(p))
            found.append(("video", str(p)))
        data["shots"] = cleaned
    return found


def update_stage(conn, track_id: int, key: str, state: str | None = None,
                 data: dict | None = None, verify_paths: bool = True) -> dict:
    """Merge data into the stage and/or set its state. Returns the stage."""
    if key not in STAGE_KEYS:
        raise UserError(f"'{key}' is not a stage. Stages are: {', '.join(STAGE_KEYS)}.")
    if state is not None and state not in ("todo", "done", "skipped"):
        raise UserError("A stage can be todo, done or skipped.")
    if data is not None and not isinstance(data, dict):
        raise UserError("Stage data must be an object of fields.")
    current = next(s for s in get_stages(conn, track_id) if s["key"] == key)
    merged = dict(current["data"])
    assets = []
    if data:
        data = dict(data)
        assets = _check_path_fields(key, data) if verify_paths else []
        merged.update(data)
    new_state = state or current["state"]
    done_at = current["done_at"]
    if state in ("done", "skipped"):
        done_at = now()
    elif state == "todo":
        done_at = None
    with transaction(conn):
        conn.execute(
            "UPDATE stages SET state = ?, data = ?, done_at = ? WHERE track_id = ? AND key = ?",
            (new_state, json.dumps(merged), done_at, track_id, key),
        )
        for kind, path in assets:
            conn.execute(
                "INSERT OR IGNORE INTO assets(track_id, kind, path, created_at) VALUES (?,?,?,?)",
                (track_id, kind, path, now()),
            )
        touch(conn, track_id)
    return next(s for s in get_stages(conn, track_id) if s["key"] == key)


def record_asset(conn, track_id: int, kind: str, path: str) -> None:
    if kind not in ASSET_KINDS:
        raise UserError(f"Unknown asset kind '{kind}'.")
    with transaction(conn):
        conn.execute(
            "INSERT OR IGNORE INTO assets(track_id, kind, path, created_at) VALUES (?,?,?,?)",
            (track_id, kind, str(path), now()),
        )
        touch(conn, track_id)


def assets_report(conn, track: dict, settings: dict, base: Path | None = None) -> dict:
    """Recorded assets (flagged if missing) plus what is actually in each library folder."""
    recorded = rows(conn.execute(
        "SELECT * FROM assets WHERE track_id = ? ORDER BY id", (track["id"],)))
    for a in recorded:
        a["exists"] = Path(a["path"]).is_file()
    root = track_dir(settings, track["slug"], base)
    on_disk = {}
    for name in LIBRARY_FOLDERS:
        folder = root / name
        files = []
        if folder.is_dir():
            try:
                files = sorted(p.name for p in folder.iterdir() if p.is_file())
            except OSError:
                files = []
        on_disk[name] = files
    return {"root": str(root), "root_exists": root.is_dir(), "recorded": recorded,
            "on_disk": on_disk}


def list_tracks(conn) -> list[dict]:
    out = []
    for t in rows(conn.execute("SELECT * FROM tracks ORDER BY updated_at DESC, id DESC")):
        stages = get_stages(conn, t["id"])
        key = current_stage_key(stages)
        t["current_stage"] = key
        t["current_stage_title"] = STAGE_TITLES[key] if key else None
        t["next_action"] = (
            NEXT_ACTION[key] if key and t["status"] == "active"
            else "Released." if t["status"] == "released"
            else "Archived." if t["status"] == "archived"
            else "All stages done. Press Release when the posts are up."
        )
        t["done_count"] = sum(1 for s in stages if s["state"] != "todo")
        out.append(t)
    return out


def dashboard(conn) -> dict:
    tracks = list_tracks(conn)
    groups = {key: [] for key in STAGE_KEYS}
    groups["finished"] = []
    released, archived = [], []
    for t in tracks:
        if t["status"] == "released":
            t["posts"] = posts_for(conn, t["id"])
            released.append(t)
        elif t["status"] == "archived":
            archived.append(t)
        elif t["current_stage"]:
            groups[t["current_stage"]].append(t)
        else:
            groups["finished"].append(t)
    return {
        "groups": [{"key": k, "title": STAGE_TITLES.get(k, "Ready to release"),
                    "tracks": v} for k, v in groups.items()],
        "released": released,
        "archived": archived,
        "counts": {"active": sum(1 for t in tracks if t["status"] == "active"),
                   "released": len(released), "archived": len(archived),
                   "total": len(tracks)},
    }


# ---- drafts -------------------------------------------------------------------

def save_draft(conn, track_id: int, kind: str, content, chosen: bool = False,
               seed: int | None = None) -> dict:
    if kind not in DRAFT_KINDS:
        raise UserError(f"Unknown draft kind '{kind}'.")
    text = content if isinstance(content, str) else json.dumps(content)
    with transaction(conn):
        cur = conn.execute(
            "INSERT INTO drafts(track_id, kind, content, chosen, seed, created_at) "
            "VALUES (?,?,?,?,?,?)", (track_id, kind, text, 1 if chosen else 0, seed, now()))
        touch(conn, track_id)
    return one(conn.execute("SELECT * FROM drafts WHERE id = ?", (cur.lastrowid,)))


def replace_drafts(conn, track_id: int, kind: str, contents: list, seed=None,
                   chosen: bool = False) -> None:
    """Replace the unchosen drafts of one kind. Chosen drafts are kept."""
    with transaction(conn):
        conn.execute("DELETE FROM drafts WHERE track_id = ? AND kind = ? AND chosen = 0",
                     (track_id, kind))
        for c in contents:
            text = c if isinstance(c, str) else json.dumps(c)
            conn.execute(
                "INSERT INTO drafts(track_id, kind, content, chosen, seed, created_at) "
                "VALUES (?,?,?,?,?,?)", (track_id, kind, text, 1 if chosen else 0, seed, now()))
        touch(conn, track_id)


def update_draft(conn, track_id: int, draft_id: int, content: str | None = None,
                 chosen: bool | None = None) -> dict:
    d = one(conn.execute("SELECT * FROM drafts WHERE id = ? AND track_id = ?",
                         (draft_id, track_id)))
    if d is None:
        raise NotFound("That draft no longer exists. Generate again.")
    with transaction(conn):
        if content is not None:
            conn.execute("UPDATE drafts SET content = ? WHERE id = ?", (str(content), draft_id))
        if chosen is not None:
            conn.execute("UPDATE drafts SET chosen = ? WHERE id = ?", (1 if chosen else 0, draft_id))
        touch(conn, track_id)
    return one(conn.execute("SELECT * FROM drafts WHERE id = ?", (draft_id,)))


def drafts_for(conn, track_id: int) -> list[dict]:
    return rows(conn.execute(
        "SELECT * FROM drafts WHERE track_id = ? ORDER BY chosen DESC, kind, id", (track_id,)))


# ---- platforms and posts ------------------------------------------------------

def platforms(conn, enabled_only: bool = False) -> list[dict]:
    sql = "SELECT * FROM platforms" + (" WHERE enabled = 1" if enabled_only else "") + " ORDER BY id"
    return rows(conn.execute(sql))


def update_platforms(conn, items: list) -> list[dict]:
    if not isinstance(items, list):
        raise UserError("Platforms must be a list.")
    with transaction(conn):
        for it in items:
            if not isinstance(it, dict) or "id" not in it:
                raise UserError("Each platform needs an id.")
            sets, vals = [], []
            if "enabled" in it:
                sets.append("enabled = ?"); vals.append(1 if it["enabled"] else 0)
            if "upload_url" in it:
                url = str(it["upload_url"] or "").strip()
                if url and not re.match(r"https?://", url):
                    raise UserError(f"The upload URL for platform {it['id']} should start with http:// or https://.")
                sets.append("upload_url = ?"); vals.append(url)
            if "notes" in it:
                sets.append("notes = ?"); vals.append(str(it["notes"] or ""))
            if sets:
                conn.execute(f"UPDATE platforms SET {', '.join(sets)} WHERE id = ?",
                             (*vals, int(it["id"])))
    return platforms(conn)


def posts_for(conn, track_id: int) -> list[dict]:
    return rows(conn.execute(
        "SELECT posts.*, platforms.name AS platform FROM posts JOIN platforms "
        "ON platforms.id = posts.platform_id WHERE track_id = ? ORDER BY platforms.id",
        (track_id,)))


def set_post(conn, track_id: int, platform_id: int, url: str) -> dict:
    """Record (or clear) the posted URL for a platform; release if all enabled have URLs."""
    url = str(url or "").strip()
    if url and not re.match(r"https?://", url):
        raise UserError("A posted URL should start with http:// or https://.")
    if not one(conn.execute("SELECT id FROM platforms WHERE id = ?", (platform_id,))):
        raise NotFound("That platform does not exist. Check Settings.")
    with transaction(conn):
        if url:
            conn.execute(
                "INSERT INTO posts(track_id, platform_id, url, posted_at) VALUES (?,?,?,?) "
                "ON CONFLICT(track_id, platform_id) DO UPDATE SET url = excluded.url, "
                "posted_at = excluded.posted_at", (track_id, platform_id, url, now()))
        else:
            conn.execute("DELETE FROM posts WHERE track_id = ? AND platform_id = ?",
                         (track_id, platform_id))
        touch(conn, track_id)
    released = maybe_release(conn, track_id)
    return {"posts": posts_for(conn, track_id), "released": released}


def maybe_release(conn, track_id: int) -> bool:
    enabled = platforms(conn, enabled_only=True)
    have = {p["platform_id"] for p in posts_for(conn, track_id) if p["url"]}
    if enabled and all(p["id"] in have for p in enabled):
        release(conn, track_id)
        return True
    return False


def release(conn, track_id: int) -> dict:
    with transaction(conn):
        conn.execute("UPDATE tracks SET status = 'released', updated_at = ? WHERE id = ?",
                     (now(), track_id))
        conn.execute(
            "UPDATE stages SET state = 'done', done_at = ? WHERE track_id = ? AND key = 'upload' "
            "AND state = 'todo'", (now(), track_id))
    return get_track(conn, track_id)


# ---- feedback -----------------------------------------------------------------

def add_feedback(conn, text: str, screen: str, base: Path) -> dict:
    text = str(text or "").strip()
    if not text:
        raise UserError("Write a sentence or two first, then send.")
    screen = str(screen or "").strip()[:80]
    ts = now()
    with transaction(conn):
        cur = conn.execute("INSERT INTO feedback(created_at, screen, text) VALUES (?,?,?)",
                           (ts, screen, text))
    md = base / "feedback.md"
    try:
        new = not md.exists()
        with md.open("a", encoding="utf-8") as fh:
            if new:
                fh.write("# ReleaseLine feedback\n\n")
            fh.write(f"## {ts} — {screen or 'unknown screen'}\n\n{text}\n\n")
    except OSError as exc:
        raise UserError(f"Saved to the database, but could not append to {md}: {exc.strerror or exc}.")
    return {"id": cur.lastrowid, "created_at": ts, "screen": screen, "text": text}


def list_feedback(conn) -> list[dict]:
    return rows(conn.execute("SELECT * FROM feedback ORDER BY id DESC"))
