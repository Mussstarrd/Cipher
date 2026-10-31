"""SQLite storage. Schema is created on first use; every write runs inside a transaction."""

from __future__ import annotations

import sqlite3
from contextlib import contextmanager
from datetime import datetime, timezone
from pathlib import Path

from . import UserError
from .settings import data_dir

SCHEMA = """
CREATE TABLE IF NOT EXISTS tracks (
    id INTEGER PRIMARY KEY,
    slug TEXT NOT NULL UNIQUE,
    title TEXT NOT NULL,
    artist TEXT NOT NULL DEFAULT '',
    genre TEXT NOT NULL DEFAULT '',
    mood TEXT NOT NULL DEFAULT '',
    energy TEXT NOT NULL DEFAULT 'mid',
    bpm TEXT NOT NULL DEFAULT '',
    key TEXT NOT NULL DEFAULT '',
    hook TEXT NOT NULL DEFAULT '',
    suno_url TEXT NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT '',
    status TEXT NOT NULL DEFAULT 'active',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS stages (
    id INTEGER PRIMARY KEY,
    track_id INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
    key TEXT NOT NULL,
    state TEXT NOT NULL DEFAULT 'todo',
    data TEXT NOT NULL DEFAULT '{}',
    done_at TEXT,
    UNIQUE(track_id, key)
);
CREATE TABLE IF NOT EXISTS assets (
    id INTEGER PRIMARY KEY,
    track_id INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
    kind TEXT NOT NULL,
    path TEXT NOT NULL,
    created_at TEXT NOT NULL,
    UNIQUE(track_id, kind, path)
);
CREATE TABLE IF NOT EXISTS drafts (
    id INTEGER PRIMARY KEY,
    track_id INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
    kind TEXT NOT NULL,
    content TEXT NOT NULL,
    chosen INTEGER NOT NULL DEFAULT 0,
    seed INTEGER,
    created_at TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS platforms (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    enabled INTEGER NOT NULL DEFAULT 1,
    upload_url TEXT NOT NULL DEFAULT '',
    notes TEXT NOT NULL DEFAULT ''
);
CREATE TABLE IF NOT EXISTS posts (
    id INTEGER PRIMARY KEY,
    track_id INTEGER NOT NULL REFERENCES tracks(id) ON DELETE CASCADE,
    platform_id INTEGER NOT NULL REFERENCES platforms(id) ON DELETE CASCADE,
    url TEXT NOT NULL DEFAULT '',
    posted_at TEXT,
    UNIQUE(track_id, platform_id)
);
CREATE TABLE IF NOT EXISTS feedback (
    id INTEGER PRIMARY KEY,
    created_at TEXT NOT NULL,
    screen TEXT NOT NULL DEFAULT '',
    text TEXT NOT NULL
);
"""

# Format reminders are deliberately generic: platforms change their limits, so the
# note points at the upload page rather than quoting numbers that go stale.
DEFAULT_PLATFORMS = [
    ("YouTube", "https://studio.youtube.com/",
     "MP4 (H.264 + AAC). Vertical 9:16 is treated as a Short; 16:9 as a regular video. "
     "Title, description and tags come from the Tags stage."),
    ("TikTok", "https://www.tiktok.com/upload",
     "MP4, vertical 9:16 works best. Paste the TikTok hashtag set into the caption."),
    ("Instagram Reels", "https://www.instagram.com/",
     "MP4, vertical 9:16. Use the Instagram hashtag set. Check the current length limit "
     "on the upload screen."),
    ("SoundCloud", "https://soundcloud.com/upload",
     "Audio only: upload the mastered WAV or MP3 320 from the Master stage, plus square "
     "artwork."),
    ("Distributor", "",
     "Set your distributor's upload page URL in Settings. Usually wants the mastered WAV, "
     "square artwork, genre, mood and the explicit flag from the distributor note."),
]


def now() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat()


def db_path(base: Path | None = None) -> Path:
    return (base or data_dir()) / "releaseline.db"


# Database files this process created from nothing (first run, or the file was deleted
# while the app was running). The Dashboard uses this to say so when library/ still has
# track folders that no longer belong to any track.
_CREATED: set = set()


def was_created_by_us(base: Path | None = None) -> bool:
    return str(db_path(base)) in _CREATED


def connect(base: Path | None = None) -> sqlite3.Connection:
    """Open (and initialize) the database. Problems become one sentence with the path."""
    path = db_path(base)
    try:
        path.parent.mkdir(parents=True, exist_ok=True)
        if not path.exists():
            _CREATED.add(str(path))
        conn = sqlite3.connect(str(path), isolation_level=None, timeout=10)
    except (sqlite3.Error, OSError) as exc:
        raise UserError(
            f"The database at {path} could not be opened ({exc}). Check that the folder "
            "exists and is writable, or restore data/ from a backup."
        )
    try:
        conn.row_factory = sqlite3.Row
        conn.execute("PRAGMA foreign_keys = ON")
        conn.execute("PRAGMA journal_mode = WAL")
        init(conn)
        return conn
    except (sqlite3.Error, OSError) as exc:
        conn.close()
        raise UserError(
            f"The database at {path} could not be opened ({exc}). Check that the folder "
            "exists and is writable, or restore data/ from a backup."
        )


def init(conn: sqlite3.Connection) -> None:
    conn.executescript(SCHEMA)  # runs outside any transaction; each statement is idempotent
    with transaction(conn):
        for name, url, notes in DEFAULT_PLATFORMS:
            conn.execute(
                "INSERT OR IGNORE INTO platforms(name, enabled, upload_url, notes) "
                "VALUES (?, 1, ?, ?)",
                (name, url, notes),
            )


@contextmanager
def transaction(conn: sqlite3.Connection):
    """BEGIN IMMEDIATE ... COMMIT, rolling back on any exception."""
    conn.execute("BEGIN IMMEDIATE")
    try:
        yield conn
    except BaseException:
        conn.execute("ROLLBACK")
        raise
    else:
        conn.execute("COMMIT")


def rows(cursor) -> list[dict]:
    return [dict(r) for r in cursor.fetchall()]


def one(cursor) -> dict | None:
    r = cursor.fetchone()
    return dict(r) if r is not None else None
