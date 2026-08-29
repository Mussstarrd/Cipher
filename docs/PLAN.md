# LineHawk — where the project stands and what to do next

Written 2026-08-29, after the week 1 paper card and the 2023–2025 evaluation.
Self-contained on purpose: it should be readable by someone (or something) with
no prior context on the repo.

---

## 1. The governing arithmetic

Everything below is downstream of one number.

College football game margins scatter around the closing spread with a standard
deviation of **15.2 points**. So one point of spread is worth about **2.62% of
cover probability**, and clearing the 52.38% break-even at −110 requires

> **0.91 points of genuine edge on every bet.**

That is the bar. Not "a good model" — 0.91 points. Most of the intuition people
carry about sports betting breaks on this number, because a model can be
visibly, satisfyingly *right* about football and still be nowhere near it.

## 2. What was measured

All figures walk-forward, 2,208 priced games over 2023–2025, hyperparameters
tuned on 2023–24 and frozen for 2025.

**The model has no information the closing line lacks.** Regressing actual
margin on the market's number and ours together:

```
beta_model = -0.018   SE 0.081   t = -0.22
```

Adding the model to the closing line moves residual error from 15.231 to
15.231 points. In all three seasons our own prediction error is *worse* than
the market's (15.5–16.2 vs 15.1–15.5). We do not have a better forecast.

**But the market drifts toward our number after the open.** Regressing
`(close − open)` on `(model − open)`:

```
coefficient = +0.0780   SE 0.0115   t = +6.78
```

That is real and highly significant. Opening lines carry error, our number sees
part of it, and betting openers converts some of that drift into value.

**Sizing that edge honestly.** Taking the best of several opening numbers and
comparing to a median close pays for line shopping whichever side you take — a
coin flip books positive CLV on that measure. Subtracting a coin-flip placebo
from the same games isolates the model's contribution:

| Disagreement | n | as-shopped | coin placebo | **real edge** | ATS at open |
|---|---|---|---|---|---|
| ≥ 2.0 pts | 1466 | +1.174 | +0.864 | **+0.310** | 52.39% ±1.31 |
| ≥ 3.0 pts | 1065 | +1.341 | +0.933 | **+0.407** | 51.83% ±1.53 |
| ≥ 4.0 pts | 755 | +1.563 | +0.957 | **+0.605** | 51.79% ±1.82 |
| ≥ 5.0 pts | 504 | +1.872 | +1.345 | **+0.527** | 53.37% ±2.23 |

**+0.3 to +0.6 points of real edge against a 0.91-point bar.** The engine finds
roughly half the edge it needs. Measured ATS agrees: every threshold lands
within one standard error of break-even.

| Price | Break-even | Edge needed | Expected ROI at +0.60 |
|---|---|---|---|
| −110 | 52.38% | 0.91 pts | **−1.52%** |
| −105 | 51.22% | 0.47 pts | **+0.71%** |
| −102 | 50.50% | 0.19 pts | **+2.16%** |

## 3. Segmentation: where the edge lives

Exploratory, at the ≥3.0-point threshold. Treated as hypothesis-generating.

| Segment | n | real edge | ATS |
|---|---|---|---|
| Weeks 1–4 | 353 | **−0.24** | 48.16% |
| Weeks 5–8 | 321 | +0.43 | 52.65% |
| Weeks 9–15 | 391 | +0.65 | 54.48% |
| P5 v P5, weeks 5+ | 382 | **+0.83** | 55.76% |
| non-P5 v P5, weeks 5+ | 330 | +0.47 | 51.21% |
| non-P5 v P5, weeks 1–4 | 243 | −0.26 | 46.91% |

Two different evidential standards apply here:

**The negative result is solid.** Weeks 1–4 grade below break-even in all three
seasons (44.3%, 47.0%, 52.9%) and the reason is mechanical, not statistical: the
in-season fit has almost no data yet, so the rating is essentially the preseason
prior, which is public information the market already holds. We should predict
this a priori, and we do.

**The positive result is not established.** Weeks 9–15 grade 61.2%, 50.0%, 52.8%
across 2023/24/25 — the pooled 54.48% is carried by 2023, and p = 0.218. The
P5-weeks-5+ cell is 55.76% ± 2.25, about 1.3 SE above break-even, from n = 382
after inspecting ~14 segments. That is suggestive, not proven.

So: **we know where not to bet. We do not yet know that we can bet anywhere.**

## 4. The strategic read

The gap to profitability is ~0.3–0.5 points. Price alone is worth 0.45 points
(−110 → −105). **The cheapest path to positive expectation is execution, not a
better model** — and execution carries no modelling risk.

This reframes the roadmap. The original upgrade queue (QB-continuity returning
production, win-totals priors, situational HFA) is aimed at improving the
*preseason prior*, which is exactly the regime — weeks 1–4 — we now have
evidence we should not bet at all.

## 5. Plan

### P0 — Execution (certain value, no modelling risk)

1. **Reduced juice.** −110 → −105 is worth 0.45 points, as much as the entire
   model edge, and requires predicting nothing. This is the single highest-value
   action available. Requires book access that offers it.
2. **Bet the earliest number.** Market drift (t = +6.78) is our only proven
   signal, and it is fully consumed by the close. Move the betting moment to the
   earliest available number rather than Saturday pre-kick.
3. **Maximise books quoted.** Shopping is worth real points; the current Odds
   API snapshot returns 9 books on major games.

### P1 — Expand the evidence base (converts "suggestive" to "known")

The segment results are limited by sample, not by method. The harness already
exists; it needs more data.

4. **Extend open-based analysis to 2021–2025** (5 seasons, ~67% more data).
   `spreadOpen` exists from 2021 and not before.
5. **Extend close-based analysis to 2014–2025** (12 seasons) for segment
   structure. **Caveat that must be handled:** pre-2021 CFBD line providers are
   `consensus`, `numberfire` and `teamrankings` — the latter two are projection
   sites, not sportsbooks. Using them as "the market" would corrupt the result.
   Restrict to `consensus` and validate it behaves like a market.
6. **Pre-register the two segment hypotheses** (weeks ≥5; P5-v-P5) and test them
   on the expanded sample. They were found by inspection, so they must be
   confirmed out-of-sample or dropped.

### P2 — Model work, ranked by expected value

7. **Exclude weeks 1–4 from betting.** Supported now.
8. **Opponent-adjusted efficiency instead of raw margin.** The current fit
   regresses on final margin; SP+ uses opponent-adjusted EPA with garbage-time
   filtering. CFBD exposes advanced box scores free. This is the largest genuine
   modelling upgrade available and directly attacks the "our forecast is worse
   than the market's" finding.
9. **Situational factors** — rest differential, travel distance, altitude.
   Cheap to add, plausibly not fully priced.
10. **QB-continuity returning production.** Improves the preseason prior.
    Deliberately demoted: it improves the weeks we have decided not to bet.

### P3 — Validation

11. **Track 2026 in paper, weeks 5+, with CLV as the primary metric.**

    This is not a preference, it is forced by variance. At 2% ROI and 300 bets a
    season the expected result is **+6 units against a standard deviation of ~17
    units** — you cannot distinguish a real edge from nothing by results in one
    season, or three. CLV has SD ~2 points, so 300 bets give SE ≈ 0.115 points
    and a +0.4-point edge shows up at t ≈ 3.5 **in a single season**.

    CLV converges roughly an order of magnitude faster than ROI. It is the only
    metric that can validate this in usable time, which is why it is the gate.

### P4 — Gates and kill criteria

12. Real money requires, on the expanded sample: mean real edge (placebo-
    subtracted) **> 0.6 points** in the target segment, holding in a **majority
    of seasons**, plus live paper CLV **> +0.4 points over ≥250 plays**.
13. **Kill criterion.** If the expanded backtest puts the weeks-5+ P5 edge below
    0.5 points, the modelling path is finished. The honest response is to bet
    nothing, or to move to a market where the bar is lower.

---

## UPDATE, same day: P1 executed. The segments do not replicate.

Two things were done immediately, because they were needed under every option.

### The pre-2021 data concern was wrong

I flagged above that `numberfire` and `teamrankings` are projection sites and
might corrupt older data. That was worth checking and it is **not true of this
feed**. Regressing margin on each provider's number over 2016/2018/2020:

| provider | n | slope | intercept | residual |
|---|---|---|---|---|
| consensus | 1642 | 0.973 | −0.42 | 15.89 |
| numberfire | 1551 | 0.980 | −0.40 | 15.98 |
| teamrankings | 1542 | 0.980 | −0.39 | 15.99 |
| Caesars | 902 | 0.976 | −0.45 | 15.59 |

All behave like markets, and pairwise they agree to within **0.52–0.77 points**
— book-to-book noise, not model-vs-market divergence. They are republishing
market lines. Pre-2021 closing data is usable.

### The segment hypotheses failed out-of-sample

The weeks-5+ and P5-v-P5 patterns were found on 2023–2025, so 2021–2022 is
clean out-of-sample data. Tested at the 3.0-point threshold:

| Hypothesis | n | real edge | ATS | p |
|---|---|---|---|---|
| H1 weeks 1–4 are negative | 205 | **+0.538** | 49.27% | 0.832 |
| H2 weeks 5+ are positive | 333 | +0.266 | 49.55% | 0.862 |
| H3 P5 v P5 weeks 5+ is best | 148 | +0.296 | **51.35%** | 0.630 |

**All three fail.** H1's sign flips outright. H3 — the cell that graded 55.76%
in-sample — grades 51.35% out-of-sample, below break-even. The segmentation was
noise, exactly as the "found after inspecting ~14 segments" caveat warned.

### The five-season pooled estimate

With 2021–2022 added, at the 3.0-point threshold:

| | value |
|---|---|
| n | **1,603** |
| Real edge (placebo-subtracted) | **+0.392 pts** |
| Predicted win% from that edge | **51.04%** |
| **Observed ATS** | **51.03%** |
| Break-even at −110 | 52.38% |
| Expected ROI at −110 | **−2.55%** |
| Expected ROI at −105 | **−0.34%** |

Per-season ATS: 47.8 / 51.5 / 51.9 / 50.3 / 53.2.

The predicted and observed win rates agree **to two decimal places**. That is
strong evidence the measurement framework is correct, and it settles the size of
the effect: the edge is real, it is about +0.4 points, and it is smaller than
the vig at any retail price.

### What this changes

The plan above ordered model work behind execution. That was right, but too
gentle. The five-season number says the model edge is **+0.4 points against a
0.91-point bar**, and no subset of games escapes it. Sections P1 and P2 as
written are now largely answered:

- P1 is **done**, and the answer is negative. More data will not help; the
  effect is measured tightly (n = 1,603) and it is too small.
- P2's segment-targeted work is **dead** — there is no segment to target.
- P2's remaining live item is opponent-adjusted EPA (item 8), which is the only
  proposal that could plausibly move the *size* of the edge rather than
  reallocate it. Everything else in the queue is now a distraction.

### The one direction the numbers actually point to

At +0.392 points the strategy wins 51.04%. Break-even by price:

| Price | Break-even | Result at 51.04% |
|---|---|---|
| −110 | 52.38% | −2.55% |
| −105 | 51.22% | −0.34% |
| −102 | 50.50% | +1.06% |
| −100 (exchange) | 50.00% | **+2.08%** |

**The edge only monetises at exchange-level pricing.** Betting exchanges and
low-hold books (Prophet X, Sporttrade, Circa-style reduced juice) charge
commission on winnings rather than a 4.5% spread. At 1–2% commission this
strategy is marginally positive; at −110 it is reliably negative.

That is a much more concrete conclusion than "improve the model". The question
is no longer *can we predict better* — five seasons say we cannot, by enough —
but *can we bet at a price where +0.4 points is enough*.


## 6. The question this plan cannot answer

**Is a 1–3% ROI edge on CFB sides worth the effort?**

That is the realistic ceiling from public ratings against a market built from
those same ratings. It is a genuine edge and a poor income. At 1 unit per bet
and 300 bets, it is a few units a season, swamped by variance in any given year.

If the goal is a rigorous research project with a small real edge, this plan
gets there. If the goal is meaningful return, the honest answer is that CFB
sides against major books is the most efficient corner of the sport, and the
scope should change — softer markets (totals, first-half, player props, smaller
conferences) where the bar is lower and the competition thinner. That is a
strategy decision, not a technical one.

## 7. Specific questions for outside review

1. Is the placebo-subtraction the right way to size CLV, or does subtracting a
   coin flip's shopping premium over-correct? What is the standard treatment?
2. The market-drift coefficient is +0.078 with t = 6.78 — small but very
   significant. Is capturing opening-line error a durable edge, or does it
   disappear once you are betting into it at size?
3. Is opponent-adjusted EPA likely to close a 0.5-point gap against a market
   that already prices SP+, or is that gap structurally unbridgeable with public
   data? (Five seasons now put our edge at +0.392 pts against a 0.91-pt bar.)
4. Are we wrong to demote the preseason-prior upgrades? The argument is that
   they improve weeks 1–4, which the evidence says not to bet — but a better
   prior also improves weeks 5–8 through the shrinkage term.
5. Given break-even needs 0.91 points and price is worth 0.45, is there any
   argument for pursuing the model at all versus pure execution optimisation?
6. The five-season estimate (+0.392 pts, 51.04% predicted vs 51.03% observed,
   n = 1,603) says this only clears at exchange-level pricing. Is that a real
   route — are exchange/low-hold prices actually obtainable at usable size on
   CFB sides — or is the honest read that CFB sides are simply closed to us?
