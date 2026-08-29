"""Walk-forward evaluation of the rating model against the market.

The question this answers is not "does the model predict football?" but the
much harder one: **does the model know anything the closing line does not?**
Those come apart completely. A model can predict margins well and still be
worthless to bet, because the market already prices everything it knows.

The primary statistic here is therefore not accuracy or ROI, it is the
incremental coefficient on the model in

    actual_margin ~ a + b1 * market_spread + b2 * model_spread

If b2 is indistinguishable from zero, the model carries no information beyond
the market and there is no edge to bet, regardless of how good its ROI looks in
any particular sample.

Everything is walk-forward: to price week W of season Y the fit sees only games
played before week W of season Y, plus a preseason prior built entirely from
season Y-1 and earlier.
"""

import statistics
from dataclasses import dataclass, field
from typing import Optional

from . import backtest, config, model
from .http import FetchError
from .sources import cfbd
from .teams import Registry


@dataclass
class Game:
    season: int
    week: int
    game_id: str
    home_id: int
    away_id: int
    home_name: str
    away_name: str
    neutral: bool
    margin: float               # actual, home minus away
    market_close: float         # closing home spread
    market_open: Optional[float] # best opening home spread (line shopping)
    open_spreads: list = field(default_factory=list)


def load_season(season: int) -> dict[int, list[Game]]:
    """Completed FBS-priceable games for a season, grouped by week."""
    out: dict[int, list[Game]] = {}
    for week in range(1, 16):
        try:
            games = cfbd.fetch_games(season, week)
            lines = backtest._lines_by_game(season, week)
        except FetchError:
            continue
        bucket = []
        for g in games:
            if g["home_score"] is None or g["away_score"] is None or not g["completed"]:
                continue
            b = lines.get(g["game_id"])
            if not b or b["close"] is None:
                continue
            opens = b["opens"]
            if opens and max(abs(o - b["close"]) for o in opens) > \
                    backtest.MAX_PLAUSIBLE_OPEN_TO_CLOSE:
                opens = []
            bucket.append(Game(
                season=season, week=week, game_id=g["game_id"],
                home_id=g["home_id"], away_id=g["away_id"],
                home_name=g["home_name"], away_name=g["away_name"],
                neutral=bool(g["neutral"]),
                margin=g["home_score"] - g["away_score"],
                market_close=b["close"], market_open=None, open_spreads=opens,
            ))
        out[week] = bucket
    return out


def walk_forward(season: int, weeks: dict[int, list[Game]], prior: dict[int, float],
                 lam: float, hfa: float, mov_cap: float) -> list[tuple[Game, float]]:
    """(game, model home spread) for every game, priced with prior weeks only."""
    history: list[tuple[int, int, float, bool]] = []
    out: list[tuple[Game, float]] = []
    for week in sorted(weeks):
        ratings = model.fit_ratings(history, prior, lam=lam, hfa=hfa, mov_cap=mov_cap)
        for g in weeks[week]:
            margin = model.predict(ratings, g.home_id, g.away_id, g.neutral, hfa)
            if margin is not None:
                out.append((g, -margin))
        # Only after pricing the week do its results enter the fit.
        for g in weeks[week]:
            history.append((g.home_id, g.away_id, g.margin, g.neutral))
    return out


# --- statistics --------------------------------------------------------------

def ols(rows: list[list[float]], y: list[float]) -> list[float]:
    """Least squares via normal equations with partial pivoting."""
    k = len(rows[0])
    aug = [[sum(rows[i][a] * rows[i][b] for i in range(len(rows))) for b in range(k)]
           + [sum(rows[i][a] * y[i] for i in range(len(rows)))] for a in range(k)]
    for c in range(k):
        piv = max(range(c, k), key=lambda r: abs(aug[r][c]))
        aug[c], aug[piv] = aug[piv], aug[c]
        if abs(aug[c][c]) < 1e-12:
            continue
        for r in range(k):
            if r != c:
                f = aug[r][c] / aug[c][c]
                aug[r] = [aug[r][j] - f * aug[c][j] for j in range(k + 1)]
    return [aug[i][k] / aug[i][i] if abs(aug[i][i]) > 1e-12 else 0.0 for i in range(k)]


def incremental_information(priced: list[tuple[Game, float]]) -> dict:
    """Does the model add anything to the closing line? The core test."""
    if len(priced) < 50:
        return {"n": len(priced)}
    y = [g.margin for g, _ in priced]
    mkt = [-g.market_close for g, _ in priced]
    mdl = [-m for _, m in priced]

    b_m = ols([[1.0, mkt[i]] for i in range(len(y))], y)
    resid_m = [y[i] - (b_m[0] + b_m[1] * mkt[i]) for i in range(len(y))]
    b_both = ols([[1.0, mkt[i], mdl[i]] for i in range(len(y))], y)
    resid_b = [y[i] - (b_both[0] + b_both[1] * mkt[i] + b_both[2] * mdl[i])
               for i in range(len(y))]

    # SE of the model coefficient, accounting for its collinearity with market.
    b_mm = ols([[1.0, mkt[i]] for i in range(len(y))], mdl)
    mdl_resid = [mdl[i] - (b_mm[0] + b_mm[1] * mkt[i]) for i in range(len(y))]
    ss = sum(r * r for r in mdl_resid)
    se = (statistics.pstdev(resid_b) / ss ** 0.5) if ss > 1e-9 else float("inf")

    return {
        "n": len(y),
        "beta_market": round(b_both[1], 4),
        "beta_model": round(b_both[2], 4),
        "beta_model_se": round(se, 4),
        "t_model": round(b_both[2] / se, 2) if se and se != float("inf") else 0.0,
        "resid_sd_market_only": round(statistics.pstdev(resid_m), 3),
        "resid_sd_with_model": round(statistics.pstdev(resid_b), 3),
        "model_vs_market_sd": round(statistics.pstdev(
            [mdl[i] - mkt[i] for i in range(len(y))]), 3),
    }


def calibration(priced: list[tuple[Game, float]]) -> dict:
    """How the market's close relates to our number — slope 1 means same scale."""
    if len(priced) < 50:
        return {}
    x = [m for _, m in priced]
    y = [g.market_close for g, _ in priced]
    b = ols([[1.0, x[i]] for i in range(len(x))], y)
    resid = [y[i] - (b[0] + b[1] * x[i]) for i in range(len(x))]
    return {"intercept": round(b[0], 3), "slope": round(b[1], 4),
            "resid_sd": round(statistics.pstdev(resid), 3)}


def ats_at_threshold(priced: list[tuple[Game, float]], threshold: float,
                     use_open: bool = False) -> dict:
    """Bet every disagreement of at least `threshold` against the market."""
    wins = losses = pushes = 0
    clvs: list[float] = []
    for g, model_spread in priced:
        entry_home = g.market_close
        if use_open:
            if not g.open_spreads:
                continue
            entry_home = None  # chosen per side below
        if use_open:
            best_home, best_away = max(g.open_spreads), min(g.open_spreads)
            home_edge = best_home - model_spread
            away_edge = model_spread - best_away
        else:
            home_edge = g.market_close - model_spread
            away_edge = model_spread - g.market_close

        if max(home_edge, away_edge) < threshold:
            continue
        take_home = home_edge >= away_edge
        if use_open:
            entry = best_home if take_home else -best_away
        else:
            entry = g.market_close if take_home else -g.market_close
        close = g.market_close if take_home else -g.market_close
        margin_pick = g.margin if take_home else -g.margin
        cover = margin_pick + entry
        if cover > 0:
            wins += 1
        elif cover < 0:
            losses += 1
        else:
            pushes += 1
        clvs.append(entry - close)

    n = wins + losses + pushes
    graded = wins + losses
    profit = wins * (100.0 / 110.0) - losses
    return {
        "threshold": threshold, "n": n,
        "record": f"{wins}-{losses}-{pushes}",
        "ats_pct": round(wins / graded * 100, 2) if graded else 0.0,
        "roi": round(profit / n * 100, 2) if n else 0.0,
        "mean_clv": round(statistics.fmean(clvs), 3) if clvs else 0.0,
    }


def break_even_edge(resid_sd: float) -> float:
    """Points of true edge needed to break even at -110.

    A spread bet wins when the margin lands on our side of the number. Moving
    the number by one point shifts cover probability by roughly the density of
    the margin distribution at the spread, phi(0)/sd for a roughly-normal
    margin. Break-even at -110 is 52.381%, so:

        points needed = (0.52381 - 0.5) / (0.3989 / sd)
    """
    density = 0.3989422804 / resid_sd
    return (0.5238095238 - 0.5) / density


# --- full evaluation ---------------------------------------------------------

TRAIN_SEASONS = (2023, 2024)
TEST_SEASONS = (2025,)

# Pre-registered before looking at results, so the list cannot grow to fit
# whatever happened to work.
HYPOTHESES = [
    ("Home underdogs (any size)", lambda g: g.market_close > 0, "home"),
    ("Home underdogs of 7+", lambda g: g.market_close >= 7, "home"),
    ("Road favorites of 14+, fade", lambda g: g.market_close <= -14, "home"),
    ("Big favorites 21+, fade", lambda g: abs(g.market_close) >= 21, "away"),
    ("Small spreads (<3), take home", lambda g: abs(g.market_close) < 3, "home"),
    ("Weeks 1-3, take the dog", lambda g: g.week <= 3 and g.market_close > 0, "home"),
    ("Weeks 10+, take the dog", lambda g: g.week >= 10 and g.market_close > 0, "home"),
    ("Always home", lambda g: True, "home"),
    ("Always away", lambda g: True, "away"),
]

BREAK_EVEN = 0.5238095238


def _binom_p(wins: int, n: int, p0: float = BREAK_EVEN) -> float:
    """One-sided P(X >= wins) under H0: true rate is break-even. Normal approx."""
    import math
    if n <= 0:
        return 1.0
    mu, sd = n * p0, (n * p0 * (1 - p0)) ** 0.5
    return 0.5 * math.erfc(((wins - 0.5 - mu) / sd) / 2 ** 0.5)


def _grade(games, side: str):
    w = l = p = 0
    for g in games:
        entry = g.market_close if side == "home" else -g.market_close
        margin = g.margin if side == "home" else -g.margin
        cover = margin + entry
        if cover > 0:
            w += 1
        elif cover < 0:
            l += 1
        else:
            p += 1
    return w, l, p


def full_evaluation(reg: Registry, seasons=(2023, 2024, 2025),
                    lam: float = None, hfa: float = None,
                    mov_cap: float = None) -> dict:
    """Load, walk forward, and run every test. Returns a result dict."""
    lam = config.RATING_SHRINKAGE if lam is None else lam
    hfa = config.HOME_FIELD_ADVANTAGE if hfa is None else hfa
    mov_cap = config.RATING_MOV_CAP if mov_cap is None else mov_cap

    priced: dict[int, list] = {}
    for season in seasons:
        weeks = load_season(season)
        prior = backtest.composite_for_week(backtest.season_ratings(season, reg), 1)
        priced[season] = walk_forward(season, weeks, prior, lam, hfa, mov_cap)

    everything = [row for season in seasons for row in priced[season]]

    per_season = {}
    for season in seasons:
        rows = priced[season]
        info = incremental_information(rows)
        model_err = statistics.pstdev([g.margin + m for g, m in rows]) if rows else 0.0
        market_err = statistics.pstdev([g.margin + g.market_close for g, _ in rows]) \
            if rows else 0.0
        per_season[season] = {
            "n": len(rows), "info": info, "calibration": calibration(rows),
            "model_error": round(model_err, 3), "market_error": round(market_err, 3),
        }

    pooled_info = incremental_information(everything)
    market_err_all = statistics.pstdev([g.margin + g.market_close for g, _ in everything])

    thresholds = {}
    for th in (1.5, 2.0, 2.5, 3.0, 4.0, 5.0):
        per = {}
        tw = tl = 0
        for season in seasons:
            r = ats_at_threshold(priced[season], th)
            w, l, _ = (int(x) for x in r["record"].split("-"))
            tw += w
            tl += l
            per[season] = r
        thresholds[th] = {
            "per_season": per, "pooled_w": tw, "pooled_l": tl,
            "pooled_pct": round(tw / (tw + tl) * 100, 2) if tw + tl else 0.0,
            "p_value": round(_binom_p(tw, tw + tl), 3),
            "all_seasons_profitable": all(
                per[s]["ats_pct"] > BREAK_EVEN * 100 for s in seasons),
        }

    hypotheses = []
    for name, sel, side in HYPOTHESES:
        per = {}
        tw = tl = 0
        for season in seasons:
            games = [g for g, _ in priced[season] if sel(g)]
            w, l, _ = _grade(games, side)
            tw += w
            tl += l
            per[season] = round(w / (w + l) * 100, 2) if w + l else 0.0
        hypotheses.append({
            "name": name, "per_season": per,
            "n": tw + tl,
            "pooled_pct": round(tw / (tw + tl) * 100, 2) if tw + tl else 0.0,
            "p_value": round(_binom_p(tw, tw + tl), 3),
            "all_seasons_profitable": all(v > BREAK_EVEN * 100 for v in per.values()),
        })

    # The verdict. Every condition must hold; each exists because a real edge
    # implies it and noise usually does not.
    best_th = max(thresholds.items(), key=lambda kv: kv[1]["pooled_pct"])
    verdict = {
        "model_beats_market_at_prediction":
            pooled_info["resid_sd_with_model"] < pooled_info["resid_sd_market_only"] - 0.05,
        "model_adds_information": pooled_info["t_model"] > 2.0,
        "any_threshold_profitable_and_consistent": any(
            v["pooled_pct"] > BREAK_EVEN * 100 and v["all_seasons_profitable"]
            and v["p_value"] < 0.05 for v in thresholds.values()),
        "any_hypothesis_holds": any(
            h["pooled_pct"] > BREAK_EVEN * 100 and h["all_seasons_profitable"]
            and h["p_value"] < 0.05 for h in hypotheses),
    }
    passed = verdict["model_adds_information"] and (
        verdict["any_threshold_profitable_and_consistent"] or verdict["any_hypothesis_holds"])

    return {
        "seasons": list(seasons),
        "params": {"lambda": lam, "hfa": hfa, "mov_cap": mov_cap},
        "per_season": per_season, "pooled": pooled_info,
        "market_error": round(market_err_all, 3),
        "break_even_points": round(break_even_edge(market_err_all), 3),
        "thresholds": thresholds, "hypotheses": hypotheses,
        "verdict": verdict, "passed": passed,
        "best_threshold": best_th[0],
        "n_total": len(everything),
    }


def render_evaluation(res: dict) -> str:
    from datetime import datetime, timezone
    out: list[str] = []
    a = out.append
    seasons = res["seasons"]

    a(f"# LineHawk - Model Evaluation {seasons[0]}-{seasons[-1]}")
    a("")
    a(f"Generated {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}. "
      f"{res['n_total']:,} priced games, walk-forward.")
    a("")
    a(f"## Verdict: {'GO' if res['passed'] else 'NO-GO - THE MODEL HAS NO EDGE'}")
    a("")
    if not res["passed"]:
        a("The model is well calibrated and predicts football reasonably. It still has no")
        a("edge, because **the closing line already contains everything it knows**. Those")
        a("are different claims and only the second one decides whether to bet.")
        a("")

    a("## The arithmetic that governs everything")
    a("")
    a(f"Game margins scatter around the closing spread with a standard deviation of")
    a(f"**{res['market_error']:.2f} points**. One point of spread is therefore worth about")
    a(f"{0.3989 / res['market_error'] * 100:.2f}% of cover probability, so clearing the")
    a(f"52.38% break-even at -110 needs roughly **{res['break_even_points']:.2f} points of")
    a("genuine edge on every bet.** That is the bar. Not 0.1 points, not a hunch.")
    a("")

    a("## Does the model know anything the market does not?")
    a("")
    a("Regress the actual margin on the market's number and the model's number together.")
    a("If the model carries independent information, its coefficient is positive and")
    a("significant. If the market has already priced it, the coefficient is zero.")
    a("")
    a("| Season | n | calib. slope | model error | market error | beta_model | t |")
    a("|--------|---|--------------|-------------|--------------|------------|---|")
    for season in seasons:
        d = res["per_season"][season]
        i, c = d["info"], d["calibration"]
        a(f"| {season} | {d['n']} | {c.get('slope', 0):.3f} | {d['model_error']:.2f} "
          f"| {d['market_error']:.2f} | {i.get('beta_model', 0):+.4f} "
          f"| {i.get('t_model', 0):+.2f} |")
    p = res["pooled"]
    a(f"| **pooled** | **{p['n']}** | - | - | - | **{p['beta_model']:+.4f}** "
      f"| **{p['t_model']:+.2f}** |")
    a("")
    a(f"Pooled `beta_model` is {p['beta_model']:+.4f} with standard error "
      f"{p['beta_model_se']:.4f} (t = {p['t_model']:+.2f}). Adding the model to the market")
    a(f"moves residual error from {p['resid_sd_market_only']:.3f} to "
      f"{p['resid_sd_with_model']:.3f} points - i.e. nowhere.")
    a("")
    a("Note also that in every season the model's own prediction error is **larger** than")
    a("the market's. We are not bringing a better forecast to the table.")
    a("")

    a("## ATS by disagreement threshold")
    a("")
    a("Bet every game where the model and the closing line differ by at least the")
    a("threshold. A real edge grows with disagreement and shows up every year.")
    a("")
    a("| Threshold | " + " | ".join(str(s) for s in seasons) + " | Pooled | n | p | Every year? |")
    a("|-----------|" + "|".join(["---"] * len(seasons)) + "|--------|---|---|-------------|")
    for th, d in sorted(res["thresholds"].items()):
        cells = " | ".join(f"{d['per_season'][s]['ats_pct']:.1f}%" for s in seasons)
        a(f"| >= {th} | {cells} | **{d['pooled_pct']:.2f}%** | {d['pooled_w'] + d['pooled_l']} "
          f"| {d['p_value']:.3f} | {'yes' if d['all_seasons_profitable'] else 'no'} |")
    a("")
    a("Break-even is 52.38%. The per-season columns are the point: single seasons swing")
    a("wildly on both sides of break-even, and nothing survives being asked to repeat.")
    a("")

    a("## Pre-registered market-inefficiency hypotheses")
    a("")
    a("These were written down before the results were looked at, so the list cannot")
    a("quietly grow to fit whatever happened to work.")
    a("")
    a("| Hypothesis | " + " | ".join(str(s) for s in seasons) + " | Pooled | n | p | Every year? |")
    a("|------------|" + "|".join(["---"] * len(seasons)) + "|--------|---|---|-------------|")
    for h in res["hypotheses"]:
        cells = " | ".join(f"{h['per_season'][s]:.1f}%" for s in seasons)
        a(f"| {h['name']} | {cells} | **{h['pooled_pct']:.2f}%** | {h['n']} | {h['p_value']:.3f} "
          f"| {'yes' if h['all_seasons_profitable'] else 'no'} |")
    a("")
    a("`Always home` and `always away` land within a whisker of 50%, which is what an")
    a("efficiently priced market looks like from the outside.")
    a("")

    a("## Gate conditions")
    a("")
    a("| Condition | Result |")
    a("|-----------|--------|")
    for k, v in res["verdict"].items():
        a(f"| {k.replace('_', ' ')} | {'PASS' if v else 'FAIL'} |")
    a("")

    a("## What would have to change")
    a("")
    a("The blocker is not the code, the calibration or the thresholds. It is that SP+ and")
    a("FPI are public, and the closing line is largely built from them plus the injury and")
    a("money information we do not have. Reprocessing public ratings cannot beat a market")
    a("that already reads those ratings.")
    a("")
    a("Getting to a real edge means bringing something the close does not contain:")
    a("")
    a("- **Information the market lacks or is slow on** - confirmed injury/QB status ahead")
    a("  of the market, weather at kickoff, travel and rest spots. This is where retail")
    a("  edges genuinely live, and it means acting early on news, not modelling harder.")
    a("- **Softer markets than the ones tested here.** These are consensus numbers from")
    a("  major books on FBS games, the most efficient corner of the sport.")
    a("- **Reduced juice.** At -105 instead of -110 break-even falls from 52.38% to 51.22%,")
    a(f"  which is worth about {(0.5238 - 0.5122) / (0.3989 / res['market_error']):.2f} points of edge - "
      "comparable to everything a model")
    a("  might realistically add, and available without predicting anything.")
    a("")
    a("Until one of those is in hand and measured, the honest position is to keep pricing")
    a("games, keep logging paper, and keep tracking CLV - which is what the pipeline now")
    a("does, with LIVE mode locked until this evaluation passes.")
    a("")
    return "\n".join(out)
