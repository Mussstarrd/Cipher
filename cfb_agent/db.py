"""SQLite persistence for games, market lines, ratings, and the bet log."""

import sqlite3
from contextlib import contextmanager

from . import config

SCHEMA = """
CREATE TABLE IF NOT EXISTS games (
    game_id    TEXT PRIMARY KEY,          -- source id (ESPN event id or synthetic)
    season     INTEGER NOT NULL,
    week       INTEGER NOT NULL,
    kickoff    TEXT,                      -- ISO timestamp
    home_team  TEXT NOT NULL,
    away_team  TEXT NOT NULL,
    neutral    INTEGER DEFAULT 0,
    home_score INTEGER,
    away_score INTEGER,
    completed  INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS lines (
    game_id     TEXT NOT NULL,
    book        TEXT NOT NULL,
    spread_home REAL NOT NULL,            -- home team spread (negative = home favored)
    price       INTEGER DEFAULT -110,
    fetched_at  TEXT NOT NULL,
    is_closing  INTEGER DEFAULT 0,
    PRIMARY KEY (game_id, book, fetched_at)
);

CREATE TABLE IF NOT EXISTS ratings (
    season  INTEGER NOT NULL,
    week    INTEGER NOT NULL,
    team    TEXT NOT NULL,
    source  TEXT NOT NULL,                -- 'sp+', 'fpi', 'prior', 'composite'
    rating  REAL NOT NULL,
    PRIMARY KEY (season, week, team, source)
);

CREATE TABLE IF NOT EXISTS bets (
    bet_id      INTEGER PRIMARY KEY AUTOINCREMENT,
    season      INTEGER NOT NULL,
    week        INTEGER NOT NULL,
    game_id     TEXT NOT NULL,
    pick_team   TEXT NOT NULL,
    pick_spread REAL NOT NULL,            -- the number we took, from pick_team's view
    price       INTEGER DEFAULT -110,
    units       REAL NOT NULL,
    edge        REAL NOT NULL,
    placed_at   TEXT NOT NULL,
    result      TEXT,                     -- 'win' | 'loss' | 'push' | NULL (open)
    profit_units REAL,
    closing_spread REAL,                  -- pick_team's closing number, for CLV
    clv_points  REAL
);
"""


@contextmanager
def connect():
    config.ensure_dirs()
    conn = sqlite3.connect(config.DB_PATH)
    conn.row_factory = sqlite3.Row
    conn.executescript(SCHEMA)
    try:
        yield conn
        conn.commit()
    finally:
        conn.close()
