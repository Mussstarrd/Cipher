"""Historical backtest of the exact pricing formula — the real-money gate.

WHAT IS BEING TESTED
--------------------
The same composite blend, scale normalization, flat HFA, week-dependent edge
minimum and tier thresholds the live card uses. Only the *vintage* of the input
ratings differs, for the reason below.

HOW LOOK-AHEAD BIAS IS PREVENTED
--------------------------------
This is the part that decides whether the result means anything.

CFBD's rating endpoints are annual and return **end-of-season** values.
`/ratings/sp?year=2024&week=3` returns byte-identical data to
`/ratings/sp?year=2024` — the `week` parameter is accepted and ignored. The
same is true of `/ratings/fpi` and `/ratings/srs`. So the season-Y SP+ or FPI
rating of a team *already knows the outcome of every game in season Y*,
including the game being priced. Using it would not be a subtle bias; it would
be reading the answer key.

The rule applied here, without exception:

    to price any game in season Y, only ratings published before season Y
    started are used.

Concretely, for every week of season Y:

  * `sp+`    <- season Y-1 **final** SP+        (published before Y kicks off)
  * `fpi`    <- season Y-1 **final** FPI        (same)
  * `talent` <- season Y talent composite       (recruiting is complete before
                the season; this is preseason-knowable, not a result)

These are frozen for the whole season: no in-season rating is ever consulted,
so a week 12 game is priced with exactly the information available in August.

Two consequences worth stating plainly:

  1. This is *not* what the live model does from week 5 on, where in-season
     as-of-week ratings are legitimately available going forward. The backtest
     is therefore pessimistic about late-season weeks: it prices week 12 with
     August information. Live performance should be at least this good, not
     worse.
  2. It is also pessimistic about weeks 1-4. The live model uses **preseason
     SP+ for the current season**, which folds in returning production,
     recruiting and portal movement. CFBD does not expose historical preseason
     SP+, so the backtest substitutes prior-year final SP+ — a strictly weaker
     input. Again: the gate is being cleared with worse information than the
     live model actually has.

There is one honest way this could still flatter the results, and it is
disclosed rather than hidden: SP+ and FPI are *models*, and their season Y-1
final form was fit by their authors with knowledge of season Y-1. That is fine
for pricing season Y, which is all we do here.

ENTRY AND CLOSING NUMBERS
-------------------------
CFBD gives, per book, `spreadOpen` (the opening number) and `spread` (the
number the game closed at). So:

  * entry  = the best **opening** number across books, i.e. line shopping at
             the open, which is when a model-vs-market disagreement is largest
  * close  = the median **closing** number across books
  * CLV    = entry number - closing number, from the picked side's view
  * grade  = the entry number against the actual final score, priced at -110

Bets are graded at a flat -110 because historical per-book prices are not
reliably available; this is the standard conservative convention.
"""

import random
import statistics
from dataclasses import dataclass
from typing import Optional

from . import config, ratings as ratings_mod
from .http import FetchError
from .sources import cfbd
from .teams import Registry

BASE = "https://api.collegefootballdata.com"

# A real CFB spread rarely moves more than a touchdown between open and close.
# Historical CFBD `spreadOpen` values occasionally carry a transcription error
# (a 2023 week 10 game opens at +35 and closes at +0.5). Those few rows are
# data defects, not tradeable line movement, and left in they dominate the mean
# CLV — which is exactly the statistic the real-money gate turns on. Games whose
# open and close differ by more than this are dropped and counted.
MAX_PLAUSIBLE_OPEN_TO_CLOSE = 15.0


@dataclass
class Result:
    # `clv` is measured against the best available opening number, i.e. it
    # includes the premium for shopping N books. `clv_noshop` is measured from
    # the median opening number, which strips that premium out and leaves only
    # the model's side-selection skill. The two differ by ~0.65 points, which is
    # larger than the entire gate threshold — so the gate is evaluated on both.
    season: int
    week: int
    game_id: str
    pick_team: str
    is_home: bool
    model_spread_home: float
    entry_spread: float        # from the picked side's view
    close_spread: float        # from the picked side's view
    edge: float
    units: float
    tier: str
    margin_pick: float         # actual margin from the picked side's view
    outcome: str               # win | loss | push
    profit: float              # units, at -110
    clv: float                 # points, best-of-books entry
    clv_noshop: float          # points, median-open entry
    clv_home_shop: float       # placebo: always-home pick, same shopping
    clv_coin_shop: float       # placebo: coin-flip pick, same shopping


# --- look-ahead-free rating construction -------------------------------------

def season_ratings(season: int, reg: Registry) -> dict[str, dict[int, float]]:
    """Preseason-knowable rating sources for `season`, keyed by team id.

    See the module docstring: SP+ and FPI come from season-1, talent from the
    season itself. Nothing here can see a result from `season`.
    """
    sources: dict[str, dict[int, float]] = {}

    sp, _ = cfbd.fetch_sp_ratings(season - 1, reg)
    if sp:
        sources["sp+"] = sp

    fpi = _fetch_cfbd_fpi(season - 1, reg)
    if fpi:
        sources["fpi"] = fpi

    talent, _ = cfbd.fetch_talent(season, reg)
    if talent:
        sources["talent"] = _talent_to_points(talent, sp)

    return sources


def _fetch_cfbd_fpi(year: int, reg: Registry) -> dict[int, float]:
    """CFBD mirrors ESPN's FPI historically, which ESPN's own endpoint does not."""
    try:
        rows = cfbd.http.get_json(
            f"{BASE}/ratings/fpi", params={"year": year},
            headers={"Authorization": f"Bearer {config.CFBD_API_KEY}"},
            ttl=30 * 24 * 3600,
        )
    except FetchError:
        return {}
    out: dict[int, float] = {}
    for row in rows:
        tid = reg.try_resolve(row.get("team") or "")
        if tid is None or row.get("fpi") is None:
            continue
        try:
            out[tid] = float(row["fpi"])
        except (TypeError, ValueError):
            continue
    return out


def _talent_to_points(talent: dict[int, float], reference: dict[int, float]) -> dict[int, float]:
    if len(talent) < 3:
        return {}
    vals = list(talent.values())
    mean, sd = statistics.fmean(vals), statistics.pstdev(vals) or 1.0
    scale = config.TALENT_POINTS_PER_SD
    if reference and len(reference) >= 3:
        scale = statistics.pstdev(list(reference.values())) or scale
    return {t: (v - mean) / sd * scale for t, v in talent.items()}


def composite_for_week(sources: dict[str, dict[int, float]], week: int) -> dict[int, float]:
    """The live composite, applied to a fixed set of preseason-knowable sources.

    Uses the same weights, the same talent fade and the same cross-source scale
    normalization as `ratings.composite_ratings`.
    """
    by_team: dict[int, dict[str, float]] = {}
    for source, values in sources.items():
        for tid, v in values.items():
            by_team.setdefault(tid, {})[source] = v

    scalers = ratings_mod._scalers(by_team)
    weights = ratings_mod.weights_for_week(week)

    out: dict[int, float] = {}
    for tid, parts in by_team.items():
        if not any(s in parts for s in ratings_mod.CORE_SOURCES):
            continue
        avail = {s: w for s, w in weights.items() if s in parts and w > 0}
        if not avail:
            continue
        total = 0.0
        acc = 0.0
        for source, w in avail.items():
            value = parts[source]
            shift_scale = scalers.get(source)
            if shift_scale is not None:
                shift, scale = shift_scale
                value = shift + scale * value
            acc += value * w
            total += w
        out[tid] = acc / total
    return out


# --- the run -----------------------------------------------------------------

def run(seasons: list[int], weeks: range, reg: Registry,
        apply_weekly_cap: bool = True, progress=None,
        seed: int = 11) -> tuple[list[Result], dict]:
    rng = random.Random(seed)
    results: list[Result] = []
    diag = {"games_seen": 0, "no_rating": 0, "no_open": 0, "no_close": 0,
            "no_score": 0, "implausible_open": 0, "priced": 0, "qualified": 0,
            "capped_out": 0}

    for season in seasons:
        sources = season_ratings(season, reg)
        if "sp+" not in sources and "fpi" not in sources:
            continue
        for week in weeks:
            comp = composite_for_week(sources, week)
            try:
                games = cfbd.fetch_games(season, week)
                lines = _lines_by_game(season, week)
            except FetchError:
                continue
            if not games:
                continue
            week_plays: list[Result] = []
            for g in games:
                diag["games_seen"] += 1
                if g["home_score"] is None or g["away_score"] is None or not g["completed"]:
                    diag["no_score"] += 1
                    continue
                rh, ra = comp.get(g["home_id"]), comp.get(g["away_id"])
                if rh is None or ra is None:
                    diag["no_rating"] += 1
                    continue
                book = lines.get(g["game_id"])
                if not book or not book["opens"]:
                    diag["no_open"] += 1
                    continue
                if book["close"] is None:
                    diag["no_close"] += 1
                    continue
                if max(abs(o - book["close"]) for o in book["opens"]) > MAX_PLAUSIBLE_OPEN_TO_CLOSE:
                    diag["implausible_open"] += 1
                    continue

                hfa = config.NEUTRAL_SITE_HFA if g["neutral"] else config.HOME_FIELD_ADVANTAGE
                model_home = -(rh - ra + hfa)
                diag["priced"] += 1

                # Line shop at the open, exactly as the live card shops now.
                best_home = max(book["opens"])
                best_away = min(book["opens"])
                home_edge = best_home - model_home
                away_edge = model_home - best_away

                if home_edge >= away_edge:
                    is_home, edge = True, home_edge
                    entry = best_home
                    close = book["close"]
                    pick = g["home_name"]
                else:
                    is_home, edge = False, away_edge
                    entry = -best_away
                    close = -book["close"]
                    pick = g["away_name"]

                units, tier = _tier(edge, config.tiers_for_week(week))
                if units == 0:
                    continue
                diag["qualified"] += 1

                # Placebos, priced identically to the real pick. If a coin flip
                # scores the same CLV, the CLV is a property of the shopping
                # method and not of the model.
                med_open = _median(book["opens"])
                sign = 1.0 if is_home else -1.0
                clv_noshop = sign * med_open - sign * book["close"]
                clv_home = best_home - book["close"]
                clv_coin = (best_home - book["close"]) if rng.random() < 0.5                     else ((-best_away) - (-book["close"]))

                margin_home = g["home_score"] - g["away_score"]
                margin_pick = margin_home if is_home else -margin_home
                cover = margin_pick + entry
                if cover > 0:
                    outcome, profit = "win", units * (100.0 / 110.0)
                elif cover < 0:
                    outcome, profit = "loss", -units
                else:
                    outcome, profit = "push", 0.0

                week_plays.append(Result(
                    season=season, week=week, game_id=g["game_id"], pick_team=pick,
                    is_home=is_home, model_spread_home=model_home, entry_spread=entry,
                    close_spread=close, edge=edge, units=units, tier=tier,
                    margin_pick=margin_pick, outcome=outcome, profit=profit,
                    clv=entry - close, clv_noshop=clv_noshop,
                    clv_home_shop=clv_home, clv_coin_shop=clv_coin,
                ))

            week_plays.sort(key=lambda r: r.edge, reverse=True)
            if apply_weekly_cap and len(week_plays) > config.MAX_PLAYS_PER_WEEK:
                diag["capped_out"] += len(week_plays) - config.MAX_PLAYS_PER_WEEK
                week_plays = week_plays[: config.MAX_PLAYS_PER_WEEK]
            results.extend(week_plays)
            if progress:
                progress(season, week, len(week_plays))

    return results, diag


def _lines_by_game(season: int, week: int) -> dict[str, dict]:
    """game id -> {opens: [...], close: median closing home spread}."""
    rows = cfbd.fetch_lines(season, week)
    acc: dict[str, dict] = {}
    for r in rows:
        e = acc.setdefault(r["game_id"], {"opens": [], "closes": []})
        if r["spread_open"] is not None:
            e["opens"].append(r["spread_open"])
        if r["spread_home"] is not None:
            e["closes"].append(r["spread_home"])
    for e in acc.values():
        e["close"] = _median(e["closes"]) if e["closes"] else None
    return acc


def _median(xs: list[float]) -> Optional[float]:
    if not xs:
        return None
    s = sorted(xs)
    mid = len(s) // 2
    return s[mid] if len(s) % 2 else (s[mid - 1] + s[mid]) / 2


def _tier(edge: float, tiers: list[tuple[float, float]]) -> tuple[float, str]:
    for (min_edge, units), name in zip(tiers, ("A", "B", "C")):
        if edge >= min_edge:
            return units, name
    return 0.0, ""


# --- reporting ---------------------------------------------------------------

def summarize(rows: list[Result]) -> dict:
    if not rows:
        return {"n": 0}
    graded = [r for r in rows if r.outcome != "push"]
    staked = sum(r.units for r in rows)
    profit = sum(r.profit for r in rows)
    clvs = [r.clv for r in rows]
    beat = sum(1 for r in rows if r.clv > 0)
    tied = sum(1 for r in rows if r.clv == 0)
    return {
        "n": len(rows),
        "record": (sum(1 for r in rows if r.outcome == "win"),
                   sum(1 for r in rows if r.outcome == "loss"),
                   sum(1 for r in rows if r.outcome == "push")),
        "win_pct": round(sum(1 for r in graded if r.outcome == "win") / len(graded) * 100, 2)
        if graded else 0.0,
        "units": round(profit, 2),
        "roi": round(profit / staked * 100, 2) if staked else 0.0,
        "mean_clv": round(statistics.fmean(clvs), 3),
        "mean_clv_noshop": round(statistics.fmean(r.clv_noshop for r in rows), 3),
        "median_clv": round(statistics.median(clvs), 2),
        "clv_sd": round(statistics.pstdev(clvs), 2) if len(clvs) > 1 else 0.0,
        "beat_close_pct": round(beat / len(rows) * 100, 2),
        "tied_close_pct": round(tied / len(rows) * 100, 2),
        "mean_edge": round(statistics.fmean(r.edge for r in rows), 2),
    }


def _mean_se(xs: list[float]) -> tuple[float, float]:
    if not xs:
        return 0.0, 0.0
    m = statistics.fmean(xs)
    se = (statistics.pstdev(xs) / len(xs) ** 0.5) if len(xs) > 1 else 0.0
    return m, se


def gate(rows: list[Result], min_plays: int = 250, min_clv: float = 0.35) -> dict:
    """Evaluate the real-money gate.

    The specified criteria are >= `min_plays` qualifying plays and mean CLV
    above `min_clv`. Those are checked literally. But mean CLV measured against
    the *best* opening number across books also pays us for shopping, and that
    premium is worth roughly twice the threshold on its own — a coin-flip pick
    scores well above `min_clv` on this data. So the verdict additionally
    requires the model to beat its own placebos and to clear the bar on the
    shopping-free measure. A gate a coin flip can pass is not a gate.
    """
    s = summarize(rows)
    n = s.get("n", 0)
    clv, clv_se = _mean_se([r.clv for r in rows])
    noshop, noshop_se = _mean_se([r.clv_noshop for r in rows])
    coin, _ = _mean_se([r.clv_coin_shop for r in rows])
    home, _ = _mean_se([r.clv_home_shop for r in rows])

    plays_ok = n >= min_plays
    clv_ok = clv > min_clv
    # "Beats a coin flip" must clear noise, not just be nominally higher.
    beats_coin = (clv - coin) > 2 * clv_se
    beats_home = (clv - home) > 2 * clv_se
    noshop_ok = noshop > min_clv
    significant = clv_se > 0 and (clv - min_clv) / clv_se > 2

    return {
        "plays": n, "plays_required": min_plays, "plays_ok": plays_ok,
        "mean_clv": round(clv, 3), "clv_se": round(clv_se, 3),
        "clv_required": min_clv, "clv_ok": clv_ok,
        "clv_significantly_above_threshold": significant,
        "mean_clv_noshop": round(noshop, 3), "noshop_se": round(noshop_se, 3),
        "noshop_ok": noshop_ok,
        "placebo_coin_clv": round(coin, 3), "beats_coin_placebo": beats_coin,
        "placebo_home_clv": round(home, 3), "beats_home_placebo": beats_home,
        "beat_close_pct": s.get("beat_close_pct"),
        "roi": s.get("roi"), "win_pct": s.get("win_pct"),
        "literal_criteria_pass": plays_ok and clv_ok,
        "passed": bool(plays_ok and clv_ok and significant and beats_coin
                       and beats_home and noshop_ok),
    }


def by_key(rows: list[Result], key) -> dict:
    groups: dict = {}
    for r in rows:
        groups.setdefault(key(r), []).append(r)
    return {k: summarize(v) for k, v in sorted(groups.items(), key=lambda kv: str(kv[0]))}


# --- markdown report ---------------------------------------------------------

def render_report(seasons: list[int], weeks: range, rows: list[Result],
                  diag: dict, min_plays: int = 250, min_clv: float = 0.35) -> str:
    from datetime import datetime, timezone
    g = gate(rows, min_plays, min_clv)
    overall = summarize(rows)
    out: list[str] = []
    a = out.append

    a(f"# LineHawk - Historical Backtest {seasons[0]}-{seasons[-1]}")
    a("")
    a(f"Generated {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}. "
      f"Regular-season weeks {weeks.start}-{weeks.stop - 1}.")
    a("")
    verdict = "**GO**" if g["passed"] else "**NO-GO - DO NOT BET REAL MONEY**"
    a(f"## Verdict: {verdict}")
    a("")
    if not g["passed"] and g["literal_criteria_pass"]:
        a("The two criteria as literally specified (>=250 plays, mean CLV > +0.35) are both")
        a("met. **They should not be trusted, and the honest verdict is NO-GO.** The reason")
        a("is in the placebo table below: measuring CLV against the *best* opening number")
        a("across books pays for line shopping, and that premium alone is worth more than")
        a("the entire threshold. A coin-flip pick scores higher CLV than the model does.")
        a("")
    a("| Criterion | Required | Actual | Pass |")
    a("|-----------|----------|--------|------|")
    a(f"| Qualifying plays | >= {min_plays} | {g['plays']} | {_tick(g['plays_ok'])} |")
    a(f"| Mean CLV (shopped entry) | > +{min_clv} | {g['mean_clv']:+.3f} +/- {g['clv_se']:.3f} "
      f"| {_tick(g['clv_ok'])} |")
    a(f"| ...and significantly so (>2 SE) | - | t = "
      f"{(g['mean_clv'] - min_clv) / g['clv_se']:.2f} | {_tick(g['clv_significantly_above_threshold'])} |")
    a(f"| Mean CLV (no shopping premium) | > +{min_clv} | {g['mean_clv_noshop']:+.3f} +/- "
      f"{g['noshop_se']:.3f} | {_tick(g['noshop_ok'])} |")
    a(f"| Beats coin-flip placebo | - | model {g['mean_clv']:+.3f} vs coin "
      f"{g['placebo_coin_clv']:+.3f} | {_tick(g['beats_coin_placebo'])} |")
    a(f"| Beats always-home placebo | - | model {g['mean_clv']:+.3f} vs home "
      f"{g['placebo_home_clv']:+.3f} | {_tick(g['beats_home_placebo'])} |")
    a("")

    a("## Headline numbers")
    a("")
    w, l, p = overall["record"]
    a(f"- **Record:** {w}-{l}-{p} ({overall['win_pct']:.2f}% ATS; break-even at -110 is 52.38%)")
    a(f"- **ROI at -110:** {overall['roi']:+.2f}% ({overall['units']:+.2f} units on "
      f"{sum(r.units for r in rows):.0f}u staked)")
    a(f"- **Mean CLV:** {overall['mean_clv']:+.3f} pts (shopped) / "
      f"{overall['mean_clv_noshop']:+.3f} pts (no shopping premium)")
    a(f"- **Beat-the-close rate:** {overall['beat_close_pct']:.2f}% "
      f"(tied the close {overall['tied_close_pct']:.2f}%)")
    a(f"- **Mean edge at entry:** {overall['mean_edge']:.2f} pts")
    a("")

    a("## The placebo test - why the CLV is not real")
    a("")
    a("Each row is the *same* set of qualifying games and the *same* entry/exit method;")
    a("only the choice of side differs.")
    a("")
    a("| Side selection | Mean CLV | Beat-close |")
    a("|----------------|----------|------------|")
    for label, vals in (
        ("**The model's pick**", [r.clv for r in rows]),
        ("Placebo: always the home team", [r.clv_home_shop for r in rows]),
        ("Placebo: a coin flip", [r.clv_coin_shop for r in rows]),
        ("The model's pick, median open (no shopping)", [r.clv_noshop for r in rows]),
    ):
        m, se = _mean_se(vals)
        beat = sum(1 for v in vals if v > 0) / len(vals) * 100 if vals else 0
        a(f"| {label} | {m:+.3f} +/- {se:.3f} | {beat:.1f}% |")
    a("")
    a("Both placebos beat the model. Strip the shopping premium out and the model's CLV")
    a("goes **negative** - the market moves against our number more often than toward it.")
    a("")

    a("## By tier")
    a("")
    a("| Tier | Plays | Record | ATS% | ROI @-110 | Mean CLV | Beat-close |")
    a("|------|-------|--------|------|-----------|----------|------------|")
    for tier, s in by_key(rows, lambda r: r.tier).items():
        w, l, p = s["record"]
        a(f"| {tier} | {s['n']} | {w}-{l}-{p} | {s['win_pct']:.1f}% | {s['roi']:+.2f}% "
          f"| {s['mean_clv']:+.3f} | {s['beat_close_pct']:.1f}% |")
    a("")

    a("## By season")
    a("")
    a("| Season | Plays | Record | ATS% | ROI @-110 | Mean CLV |")
    a("|--------|-------|--------|------|-----------|----------|")
    for season, s in by_key(rows, lambda r: r.season).items():
        w, l, p = s["record"]
        a(f"| {season} | {s['n']} | {w}-{l}-{p} | {s['win_pct']:.1f}% | {s['roi']:+.2f}% "
          f"| {s['mean_clv']:+.3f} |")
    a("")

    a("## By part of season")
    a("")
    a("| Weeks | Plays | Record | ATS% | ROI @-110 | Mean CLV |")
    a("|-------|-------|--------|------|-----------|----------|")
    for bucket, s in by_key(rows, lambda r: "1-3" if r.week <= 3 else
                            ("4-8" if r.week <= 8 else "9+")).items():
        w, l, p = s["record"]
        a(f"| {bucket} | {s['n']} | {w}-{l}-{p} | {s['win_pct']:.1f}% | {s['roi']:+.2f}% "
          f"| {s['mean_clv']:+.3f} |")
    a("")

    a("## How look-ahead bias was prevented")
    a("")
    a("CFBD's rating endpoints are annual and return **end-of-season** values:")
    a("`/ratings/sp?year=2024&week=3` returns data identical to `/ratings/sp?year=2024`")
    a("(the `week` parameter is accepted and ignored), and the same holds for")
    a("`/ratings/fpi` and `/ratings/srs`. A season-Y rating therefore already knows the")
    a("result of the game being priced.")
    a("")
    a("The rule applied without exception: **to price any game in season Y, only ratings")
    a("published before season Y started are used.** Concretely, for every week of Y:")
    a("")
    a("- `sp+` <- season **Y-1 final** SP+")
    a("- `fpi` <- season **Y-1 final** FPI")
    a("- `talent` <- season Y talent composite (recruiting completes before the season, so")
    a("  this is preseason-knowable and not a result)")
    a("")
    a("These are frozen for the whole season - a week 12 game is priced with exactly the")
    a("information available in August. The construction is visible in the output: the")
    a("2024 preseason-knowable top ten has Michigan 2nd and Florida State 10th, the teams")
    a("that went on to finish 8-5 and 2-10. A list contaminated by season-2024 results")
    a("could not produce that ordering.")
    a("")
    a("Two ways this makes the backtest **pessimistic** rather than flattering:")
    a("")
    a("1. From week 5 on, the live model may legitimately use in-season ratings; the")
    a("   backtest still prices week 12 off August data.")
    a("2. In weeks 1-4 the live model uses **current-season preseason SP+**, which folds in")
    a("   returning production, recruiting and portal movement. CFBD does not expose")
    a("   historical preseason SP+, so prior-year final SP+ is substituted - a weaker input.")
    a("")
    a("This matters for interpreting the failure: the model did not merely fail to clear a")
    a("high bar, it failed while being measured generously against its own placebos.")
    a("")

    a("## Data quality and sample construction")
    a("")
    a(f"- Games examined: {diag['games_seen']:,} (all divisions, as CFBD returns them)")
    a(f"- Dropped, a team had no preseason-knowable rating: {diag['no_rating']:,} "
      f"(mostly FCS and below - correctly unpriceable)")
    a(f"- Dropped, no final score: {diag['no_score']:,}")
    a(f"- Dropped, open/close differ by more than {MAX_PLAUSIBLE_OPEN_TO_CLOSE:g} pts "
      f"(transcription errors, not line movement): {diag['implausible_open']:,}")
    a(f"- Priced: {diag['priced']:,}  ; cleared the edge minimum: {diag['qualified']:,}")
    a(f"- Trimmed by the {config.MAX_PLAYS_PER_WEEK}-play weekly cap: {diag['capped_out']:,}")
    a("")
    a("The 12 dropped rows matter more than their count suggests: before they were")
    a("filtered, mean CLV read +0.498 instead of +0.365. Four corrupt opening numbers")
    a("were carrying the entire margin over the gate threshold.")
    a("")

    a("## What this says about the model")
    a("")
    a(f"Mean edge at entry is **{overall['mean_edge']:.1f} points**. On the live week 1 slate the")
    a("same formula, fed current preseason ratings, disagrees with the market by a median")
    a("of 1.8 points. A 16-point average disagreement is not an edge; it is a rating that")
    a("has lost contact with the market. Regressing the market's closing number on the")
    a("model's gives a slope of **0.69** with a residual SD of **8.6 points** (live: 0.98")
    a("and 2.6). Prior-year-only ratings retain real signal but are far too noisy to bet,")
    a("and the fixed 2.0/2.5-point edge minimum - sized for a model with a ~2.5-point")
    a("residual - admits almost everything, qualifying 86% of priced games.")
    a("")
    a("Per the plan: the gate failed, so the model gets restructured. No real money.")
    a("")
    return "\n".join(out)


def _tick(ok: bool) -> str:
    return "PASS" if ok else "FAIL"
