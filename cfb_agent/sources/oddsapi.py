"""The Odds API — multi-book live spreads for line shopping (needs a key).

The free tier is 500 requests/month and one request returns every book for
every game, so the constraint is snapshot *count*, not data volume. This module
enforces the budget itself rather than trusting the caller:

  * scoped strictly to sport=americanfootball_ncaaf, regions=us, markets=spreads
    (cost is markets x regions = 1 request per snapshot);
  * at most ODDS_SNAPSHOTS_PER_WEEK snapshots per (season, week) — the intended
    cadence is Tue open, Fri night, Sat ~60 minutes before kickoff;
  * a persistent counter in data/oddsapi_budget.json tracking spend per month,
    reconciled against the API's own x-requests-remaining header;
  * every raw response archived under data/cache/raw/ so a paid-for snapshot
    never has to be bought twice.

`fetch_spreads` returns cached data and spends nothing unless `snapshot=True`.
"""

import json
from datetime import datetime, timezone
from typing import Optional

from .. import config, http
from ..teams import Registry

BASE = f"https://api.the-odds-api.com/v4/sports/{config.ODDS_SPORT_KEY}/odds"
PARAMS = {"regions": "us", "markets": "spreads", "oddsFormat": "american"}


class BudgetExceeded(RuntimeError):
    """Refusing to spend a metered request. Not a fetch failure."""


# --- budget ledger -----------------------------------------------------------

def _load_ledger() -> dict:
    config.ensure_dirs()
    if config.ODDS_BUDGET_PATH.exists():
        try:
            return json.loads(config.ODDS_BUDGET_PATH.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            pass
    return {"months": {}, "weeks": {}}


def _save_ledger(ledger: dict) -> None:
    config.ODDS_BUDGET_PATH.write_text(json.dumps(ledger, indent=1, sort_keys=True), encoding="utf-8")


def budget_status(season: int, week: int) -> dict:
    """What we've spent, without spending anything."""
    ledger = _load_ledger()
    month = datetime.now(timezone.utc).strftime("%Y-%m")
    wk = f"{season}-{week}"
    snaps = ledger.get("weeks", {}).get(wk, [])
    used = ledger.get("months", {}).get(month, 0)
    return {
        "month": month,
        "used_this_month": used,
        "monthly_budget": config.ODDS_MONTHLY_BUDGET,
        "remaining_this_month": config.ODDS_MONTHLY_BUDGET - used,
        "snapshots_this_week": len(snaps),
        "snapshots_allowed": config.ODDS_SNAPSHOTS_PER_WEEK,
        "snapshot_times": snaps,
        "api_remaining": ledger.get("api_remaining"),
    }


def _record(season: int, week: int, api_remaining: Optional[str]) -> None:
    ledger = _load_ledger()
    month = datetime.now(timezone.utc).strftime("%Y-%m")
    ledger.setdefault("months", {})[month] = ledger.get("months", {}).get(month, 0) + 1
    wk = f"{season}-{week}"
    ledger.setdefault("weeks", {}).setdefault(wk, []).append(
        datetime.now(timezone.utc).isoformat(timespec="seconds")
    )
    if api_remaining is not None:
        ledger["api_remaining"] = api_remaining
    _save_ledger(ledger)


# --- fetch -------------------------------------------------------------------

def fetch_spreads(season: int, week: int, reg: Registry,
                  snapshot: bool = False) -> tuple[list[dict], dict]:
    """Current spreads across books, resolved to team ids.

    Returns (rows, meta). Rows: home_id, away_id, book, spread_home, price,
    commence_time. `meta` reports budget state and any unmapped names.

    With snapshot=False this reads the last archived response and spends no
    request. With snapshot=True it spends exactly one, subject to the budget.
    """
    if not config.ODDS_API_KEY:
        raise http.FetchError("ODDS_API_KEY not set — get a free key at the-odds-api.com")

    meta = {"budget": budget_status(season, week), "spent": 0, "unmapped": [], "source": None}

    if snapshot:
        status = meta["budget"]
        if status["snapshots_this_week"] >= config.ODDS_SNAPSHOTS_PER_WEEK:
            raise BudgetExceeded(
                f"already took {status['snapshots_this_week']} Odds API snapshots for "
                f"{season} week {week} (limit {config.ODDS_SNAPSHOTS_PER_WEEK})"
            )
        if status["remaining_this_month"] <= 0:
            raise BudgetExceeded(
                f"monthly Odds API budget exhausted ({status['used_this_month']}/"
                f"{config.ODDS_MONTHLY_BUDGET} for {status['month']})"
            )
        data = http.get_json(
            BASE, params={"apiKey": config.ODDS_API_KEY, **PARAMS},
            ttl=0, archive_as="oddsapi_ncaaf_spreads",
        )
        _record(season, week, None)
        meta["spent"] = 1
        meta["source"] = "live snapshot"
        meta["budget"] = budget_status(season, week)
    else:
        data = _latest_archive()
        if data is None:
            raise http.FetchError(
                "no archived Odds API snapshot yet — run with --odds-snapshot to spend one"
            )
        meta["source"] = "archived snapshot"

    rows, unmapped = _parse(data, reg)
    meta["unmapped"] = unmapped
    meta["games"] = len(data)
    return rows, meta


def _latest_archive():
    files = sorted(config.RAW_DIR.glob("oddsapi_ncaaf_spreads_*.json"))
    if not files:
        return None
    return json.loads(files[-1].read_text(encoding="utf-8"))


def latest_archive_time() -> Optional[str]:
    files = sorted(config.RAW_DIR.glob("oddsapi_ncaaf_spreads_*.json"))
    if not files:
        return None
    return files[-1].stem.rsplit("_", 1)[-1]


def _parse(data: list, reg: Registry) -> tuple[list[dict], list[str]]:
    out: list[dict] = []
    unmapped: set[str] = set()
    for game in data:
        home, away = game.get("home_team"), game.get("away_team")
        home_id, away_id = reg.try_resolve(home or ""), reg.try_resolve(away or "")
        if home_id is None or away_id is None:
            # Never guess. An unresolved name drops the game and is reported.
            if home_id is None and home:
                unmapped.add(home)
            if away_id is None and away:
                unmapped.add(away)
            continue
        for book in game.get("bookmakers", []):
            for market in book.get("markets", []):
                if market.get("key") != "spreads":
                    continue
                for outcome in market.get("outcomes", []):
                    # Match the outcome to the home side by id, not by string.
                    if reg.try_resolve(outcome.get("name") or "") != home_id:
                        continue
                    if outcome.get("point") is None:
                        continue
                    out.append({
                        "game_id": None,
                        "home_id": home_id,
                        "away_id": away_id,
                        "book": book.get("title") or book.get("key") or "unknown",
                        "spread_home": float(outcome["point"]),
                        "price": int(outcome.get("price", config.DEFAULT_SPREAD_PRICE)),
                        "commence_time": game.get("commence_time"),
                    })
    return out, sorted(unmapped)
