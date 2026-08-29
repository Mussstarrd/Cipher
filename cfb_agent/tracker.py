"""Bet log + settlement + season tracking (W/L, ROI, CLV)."""

from datetime import datetime, timezone

from . import config, db
from .edges import Play


def log_plays(season: int, week: int, plays: list[Play]) -> int:
    """Record this week's card in the bet log (skips games already logged)."""
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
                """INSERT INTO bets (season, week, game_id, pick_team, pick_spread,
                       price, units, edge, placed_at)
                   VALUES (?,?,?,?,?,?,?,?,?)""",
                (season, week, p.game_id, p.pick_team, p.pick_spread,
                 p.price, p.units, p.edge, now),
            )
            logged += 1
    return logged


def settle(season: int, week: int) -> list[dict]:
    """Grade open bets against final scores; compute profit and CLV."""
    results = []
    with db.connect() as conn:
        rows = conn.execute(
            """SELECT b.*, g.home_team, g.away_team, g.home_score, g.away_score, g.completed
               FROM bets b JOIN games g ON b.game_id=g.game_id
               WHERE b.season=? AND b.week=? AND b.result IS NULL""",
            (season, week),
        ).fetchall()
        for b in rows:
            if not b["completed"] or b["home_score"] is None:
                continue
            margin_home = b["home_score"] - b["away_score"]
            pick_margin = margin_home if b["pick_team"] == b["home_team"] else -margin_home
            cover = pick_margin + b["pick_spread"]
            if cover > 0:
                result, profit = "win", b["units"] * _payout(b["price"])
            elif cover < 0:
                result, profit = "loss", -b["units"]
            else:
                result, profit = "push", 0.0

            closing = conn.execute(
                """SELECT spread_home FROM lines WHERE game_id=? ORDER BY fetched_at DESC LIMIT 1""",
                (b["game_id"],),
            ).fetchone()
            clv = None
            closing_spread = None
            if closing:
                closing_spread = (
                    closing["spread_home"] if b["pick_team"] == b["home_team"] else -closing["spread_home"]
                )
                # Positive CLV: we got a better number than the close.
                clv = b["pick_spread"] - closing_spread

            conn.execute(
                "UPDATE bets SET result=?, profit_units=?, closing_spread=?, clv_points=? WHERE bet_id=?",
                (result, profit, closing_spread, clv, b["bet_id"]),
            )
            results.append({"pick": f"{b['pick_team']} {b['pick_spread']:+g}",
                            "result": result, "profit": profit, "clv": clv})
    return results


def season_summary(season: int) -> dict:
    with db.connect() as conn:
        row = conn.execute(
            """SELECT COUNT(*) n,
                      SUM(result='win') w, SUM(result='loss') l, SUM(result='push') p,
                      SUM(profit_units) profit, SUM(units) staked,
                      AVG(clv_points) avg_clv
               FROM bets WHERE season=? AND result IS NOT NULL""",
            (season,),
        ).fetchone()
    n = row["n"] or 0
    staked = row["staked"] or 0.0
    return {
        "settled": n,
        "record": f"{row['w'] or 0}-{row['l'] or 0}-{row['p'] or 0}",
        "profit_units": round(row["profit"] or 0.0, 2),
        "roi": round((row["profit"] or 0.0) / staked * 100, 1) if staked else 0.0,
        "avg_clv_points": round(row["avg_clv"], 2) if row["avg_clv"] is not None else None,
    }


def _payout(price: int) -> float:
    """Profit per unit staked at American odds."""
    return 100.0 / abs(price) if price < 0 else price / 100.0
