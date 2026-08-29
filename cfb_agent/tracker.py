"""Bet log + settlement + season tracking (W/L, ROI, CLV).

Every row carries a mode: PAPER rows are research and never represent money at
risk. Weeks 1-3 are PAPER by construction (config.PAPER_ONLY_THROUGH_WEEK).
"""

import json
from datetime import datetime, timezone
from typing import Optional

from . import config, db
from .edges import Play

KEY_NUMBERS = (3.0, 7.0)


def gate_status() -> dict:
    """The recorded evaluation verdict. Missing or unreadable means not passed."""
    try:
        return json.loads(config.GATE_STATUS_PATH.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {"passed": False, "reason": "no evaluation has been recorded"}


def mode_for_week(week: int) -> str:
    """PAPER unless the week is late enough AND the evaluation gate has passed.

    Two independent locks. The week rule is a calendar guard; the gate is an
    evidence guard. Either one alone keeps the card on paper, and the gate
    defaults to closed, so a missing or stale evaluation cannot be mistaken for
    a passing one.
    """
    if week <= config.PAPER_ONLY_THROUGH_WEEK:
        return "PAPER"
    return "LIVE" if gate_status().get("passed") else "PAPER"


def mode_explanation(week: int) -> str:
    if week <= config.PAPER_ONLY_THROUGH_WEEK:
        return f"weeks 1-{config.PAPER_ONLY_THROUGH_WEEK} are paper only"
    g = gate_status()
    if g.get("passed"):
        return "evaluation gate passed"
    return f"evaluation gate NOT passed ({g.get('reason', 'unknown')})"


def log_plays(season: int, week: int, plays: list[Play],
              mode: Optional[str] = None) -> int:
    """Record this week's card in the bet log (skips games already logged)."""
    mode = mode or mode_for_week(week)
    now = datetime.now(timezone.utc).isoformat(timespec="seconds")
    logged = 0
    with db.connect() as conn:
        for p in plays:
            exists = conn.execute(
                "SELECT 1 FROM bets WHERE season=? AND week=? AND game_id=?",
                (season, week, p.game_id),
            ).fetchone()
            if exists:
                continue
            conn.execute(
                """INSERT INTO bets (season, week, game_id, pick_team_id, pick_team,
                       pick_spread, book, price, units, edge, mode, placed_at)
                   VALUES (?,?,?,?,?,?,?,?,?,?,?,?)""",
                (season, week, p.game_id, p.pick_team_id, p.pick_team, p.pick_spread,
                 p.best_book, p.price, p.units, p.edge, mode, now),
            )
            logged += 1
    return logged


def closing_consensus(conn, game_id: str) -> Optional[float]:
    """Median closing home spread across books.

    Prefers lines explicitly flagged as closing; otherwise falls back to the
    latest number seen for each book. A single book is a quote, not a close.
    """
    rows = conn.execute(
        "SELECT book, spread_home FROM lines WHERE game_id=? AND is_closing=1",
        (game_id,),
    ).fetchall()
    if not rows:
        rows = conn.execute(
            """SELECT l.book, l.spread_home FROM lines l
               JOIN (SELECT game_id, book, MAX(fetched_at) AS ft
                       FROM lines WHERE game_id=? GROUP BY game_id, book) x
                 ON x.game_id=l.game_id AND x.book=l.book AND x.ft=l.fetched_at""",
            (game_id,),
        ).fetchall()
    spreads = sorted(r["spread_home"] for r in rows)
    if not spreads:
        return None
    mid = len(spreads) // 2
    return spreads[mid] if len(spreads) % 2 else (spreads[mid - 1] + spreads[mid]) / 2


def settle(season: int, week: int) -> list[dict]:
    """Grade open bets against final scores; compute profit and CLV."""
    results = []
    with db.connect() as conn:
        rows = conn.execute(
            """SELECT b.*, g.home_id, g.away_id, g.home_score, g.away_score, g.completed
               FROM bets b JOIN games g ON b.game_id=g.game_id
               WHERE b.season=? AND b.week=? AND b.result IS NULL""",
            (season, week),
        ).fetchall()
        for b in rows:
            if not b["completed"] or b["home_score"] is None or b["away_score"] is None:
                continue
            margin_home = b["home_score"] - b["away_score"]
            is_home = int(b["pick_team_id"]) == int(b["home_id"])
            pick_margin = margin_home if is_home else -margin_home
            cover = pick_margin + b["pick_spread"]
            if cover > 0:
                result, profit = "win", b["units"] * _payout(b["price"])
            elif cover < 0:
                result, profit = "loss", -b["units"]
            else:
                result, profit = "push", 0.0

            closing_home = closing_consensus(conn, b["game_id"])
            closing_spread = clv = None
            if closing_home is not None:
                closing_spread = closing_home if is_home else -closing_home
                # Positive CLV: we took a better number than the close.
                clv = b["pick_spread"] - closing_spread

            conn.execute(
                "UPDATE bets SET result=?, profit_units=?, closing_spread=?, clv_points=? WHERE bet_id=?",
                (result, profit, closing_spread, clv, b["bet_id"]),
            )
            results.append({
                "pick": f"{b['pick_team']} {b['pick_spread']:+g}",
                "mode": b["mode"], "result": result, "profit": profit,
                "clv": clv, "closing": closing_spread,
                "score": f"{b['away_score']}-{b['home_score']}",
            })
    return results


def season_summary(season: int, mode: Optional[str] = None) -> dict:
    where = "season=? AND result IS NOT NULL"
    params: list = [season]
    if mode:
        where += " AND mode=?"
        params.append(mode)
    with db.connect() as conn:
        row = conn.execute(
            f"""SELECT COUNT(*) n,
                       SUM(result='win') w, SUM(result='loss') l, SUM(result='push') p,
                       SUM(profit_units) profit, SUM(units) staked,
                       AVG(clv_points) avg_clv,
                       SUM(clv_points > 0) beat, SUM(clv_points IS NOT NULL) with_clv
                FROM bets WHERE {where}""",
            params,
        ).fetchone()
    n = row["n"] or 0
    staked = row["staked"] or 0.0
    with_clv = row["with_clv"] or 0
    return {
        "settled": n,
        "record": f"{row['w'] or 0}-{row['l'] or 0}-{row['p'] or 0}",
        "profit_units": round(row["profit"] or 0.0, 2),
        "roi": round((row["profit"] or 0.0) / staked * 100, 1) if staked else 0.0,
        "avg_clv_points": round(row["avg_clv"], 2) if row["avg_clv"] is not None else None,
        "beat_close_pct": round((row["beat"] or 0) / with_clv * 100, 1) if with_clv else None,
        "clv_sample": with_clv,
    }


def _payout(price: int) -> float:
    """Profit per unit staked at American odds."""
    return 100.0 / abs(price) if price < 0 else price / 100.0
