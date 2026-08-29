"""SQLite persistence for games, market lines, ratings, and the bet log.

Teams are stored as integer ids (the shared ESPN/CFBD id) everywhere that
matters for joining. Display names are carried alongside for reporting only —
nothing joins on them.
"""

import sqlite3
from contextlib import contextmanager

from . import config

SCHEMA_VERSION = 2

SCHEMA = """
CREATE TABLE IF NOT EXISTS games (
    game_id    TEXT PRIMARY KEY,          -- ESPN event id (CFBD uses the same id)
    season     INTEGER NOT NULL,
    week       INTEGER NOT NULL,
    kickoff    TEXT,                      -- ISO timestamp
    home_id    INTEGER NOT NULL,
    away_id    INTEGER NOT NULL,
    home_team  TEXT NOT NULL,             -- display only
    away_team  TEXT NOT NULL,             -- display only
    neutral    INTEGER DEFAULT 0,
    home_score INTEGER,
    away_score INTEGER,
    completed  INTEGER DEFAULT 0
);
CREATE INDEX IF NOT EXISTS idx_games_week ON games(season, week);

CREATE TABLE IF NOT EXISTS lines (
    game_id     TEXT NOT NULL,
    book        TEXT NOT NULL,
    spread_home REAL NOT NULL,            -- home team spread (negative = home favored)
    price       INTEGER DEFAULT -110,
    source      TEXT NOT NULL DEFAULT '', -- which feed delivered it
    fetched_at  TEXT NOT NULL,
    is_closing  INTEGER DEFAULT 0,
    PRIMARY KEY (game_id, book, fetched_at)
);
CREATE INDEX IF NOT EXISTS idx_lines_game ON lines(game_id);

CREATE TABLE IF NOT EXISTS ratings (
    season  INTEGER NOT NULL,
    week    INTEGER NOT NULL,
    team_id INTEGER NOT NULL,
    source  TEXT NOT NULL,                -- 'sp+', 'fpi', 'talent'
    rating  REAL NOT NULL,
    PRIMARY KEY (season, week, team_id, source)
);

CREATE TABLE IF NOT EXISTS bets (
    bet_id      INTEGER PRIMARY KEY AUTOINCREMENT,
    season      INTEGER NOT NULL,
    week        INTEGER NOT NULL,
    game_id     TEXT NOT NULL,
    pick_team_id INTEGER NOT NULL,
    pick_team   TEXT NOT NULL,            -- display only
    pick_spread REAL NOT NULL,            -- the number we took, from pick_team's view
    book        TEXT,
    price       INTEGER DEFAULT -110,
    units       REAL NOT NULL,
    edge        REAL NOT NULL,
    mode        TEXT NOT NULL DEFAULT 'PAPER',   -- 'PAPER' | 'LIVE'
    placed_at   TEXT NOT NULL,
    result      TEXT,                     -- 'win' | 'loss' | 'push' | NULL (open)
    profit_units REAL,
    closing_spread REAL,                  -- pick_team's closing number, for CLV
    clv_points  REAL
);

CREATE TABLE IF NOT EXISTS meta (
    key   TEXT PRIMARY KEY,
    value TEXT
);
"""


@contextmanager
def connect():
    config.ensure_dirs()
    conn = sqlite3.connect(config.DB_PATH)
    conn.row_factory = sqlite3.Row
    conn.executescript(SCHEMA)
    conn.execute(
        "INSERT OR REPLACE INTO meta (key, value) VALUES ('schema_version', ?)",
        (str(SCHEMA_VERSION),),
    )
    try:
        yield conn
        conn.commit()
    finally:
        conn.close()
