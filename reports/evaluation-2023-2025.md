# LineHawk - Model Evaluation 2023-2025

Generated 2026-08-29 17:55 UTC. 2,208 priced games, walk-forward.

## Verdict: NO-GO - THE MODEL HAS NO EDGE

The model is well calibrated and predicts football reasonably. It still has no
edge, because **the closing line already contains everything it knows**. Those
are different claims and only the second one decides whether to bet.

## The arithmetic that governs everything

Game margins scatter around the closing spread with a standard deviation of
**15.23 points**. One point of spread is therefore worth about
2.62% of cover probability, so clearing the
52.38% break-even at -110 needs roughly **0.91 points of
genuine edge on every bet.** That is the bar. Not 0.1 points, not a hunch.

## Does the model know anything the market does not?

Regress the actual margin on the market's number and the model's number together.
If the model carries independent information, its coefficient is positive and
significant. If the market has already priced it, the coefficient is zero.

| Season | n | calib. slope | model error | market error | beta_model | t |
|--------|---|--------------|-------------|--------------|------------|---|
| 2023 | 729 | 0.939 | 15.68 | 15.05 | -0.1008 | -0.72 |
| 2024 | 740 | 0.956 | 16.17 | 15.54 | -0.0691 | -0.50 |
| 2025 | 739 | 0.979 | 15.49 | 15.09 | +0.1323 | +0.92 |
| **pooled** | **2208** | - | - | - | **-0.0177** | **-0.22** |

Pooled `beta_model` is -0.0177 with standard error 0.0812 (t = -0.22). Adding the model to the market
moves residual error from 15.231 to 15.231 points - i.e. nowhere.

Note also that in every season the model's own prediction error is **larger** than
the market's. We are not bringing a better forecast to the table.

## ATS by disagreement threshold

Bet every game where the model and the closing line differ by at least the
threshold. A real edge grows with disagreement and shows up every year.

| Threshold | 2023 | 2024 | 2025 | Pooled | n | p | Every year? |
|-----------|---|---|---|--------|---|---|-------------|
| >= 1.5 | 47.7% | 46.7% | 53.4% | **49.27%** | 1443 | 0.992 | no |
| >= 2.0 | 48.6% | 45.8% | 53.8% | **49.47%** | 1225 | 0.981 | no |
| >= 2.5 | 47.5% | 46.4% | 53.9% | **49.29%** | 1055 | 0.979 | no |
| >= 3.0 | 47.4% | 47.4% | 54.7% | **49.83%** | 893 | 0.940 | no |
| >= 4.0 | 44.7% | 45.3% | 54.9% | **48.30%** | 646 | 0.983 | no |
| >= 5.0 | 48.9% | 43.4% | 52.2% | **48.10%** | 420 | 0.965 | no |

Break-even is 52.38%. The per-season columns are the point: single seasons swing
wildly on both sides of break-even, and nothing survives being asked to repeat.

## The one thing that does work, and why it is still not enough

Regressing the line's movement `(close - open)` on our disagreement with the open
`(model - open)` gives a coefficient of **+0.0780** (SE 0.0115, t = **+6.78**, n = 2,196).

That is highly significant and it is a real result: **the market does drift toward
our number after the open.** The opening line carries error, our number sees part of
it, and betting the open captures some of that drift as closing line value.

The trap is measuring that value naively. Taking the *best* of several opening
numbers and comparing it to a *median* close pays us for shopping whichever side we
choose, so a coin flip books positive CLV too. Subtracting the coin flip leaves the
part actually attributable to the model:

| Disagreement | n | CLV as shopped | CLV no shopping | Coin-flip placebo | **Real edge** | ATS at open |
|--------------|---|----------------|-----------------|-------------------|---------------|-------------|
| >= 2.0 pts | 1466 | +1.174 | +0.326 | +0.864 | **+0.310** | 52.39% +/- 1.31 |
| >= 3.0 pts | 1065 | +1.341 | +0.402 | +0.933 | **+0.407** | 51.83% +/- 1.53 |
| >= 4.0 pts | 755 | +1.563 | +0.534 | +0.957 | **+0.605** | 51.79% +/- 1.82 |
| >= 5.0 pts | 504 | +1.872 | +0.710 | +1.345 | **+0.527** | 53.37% +/- 2.23 |

So the genuine edge runs **+0.31 to +0.60 points** - real,
consistent, and growing with the size of the disagreement, which is what a true
effect looks like rather than a fluke. It is also **less than the 0.91 points
the vig demands.** The table below is deliberately generous: it uses the *best* of
the four thresholds (+0.60), so the real picture is somewhat worse than
what it shows.

| Price | Break-even | Edge needed | Our expected win% | Expected ROI |
|-------|-----------|-------------|-------------------|--------------|
| -102 | 50.50% | 0.19 pts | 51.58% | **+2.16%** |
| -105 | 51.22% | 0.47 pts | 51.58% | **+0.71%** |
| -110 | 52.38% | 0.91 pts | 51.58% | **-1.52%** |

This is the whole project in one table. The engine is not worthless and it is not a
winner: it finds about half the edge it needs. The measured ATS at opening numbers
agrees - every threshold lands within one standard error of break-even.

## Pre-registered market-inefficiency hypotheses

These were written down before the results were looked at, so the list cannot
quietly grow to fit whatever happened to work.

| Hypothesis | 2023 | 2024 | 2025 | Pooled | n | p | Every year? |
|------------|---|---|---|--------|---|---|-------------|
| Home underdogs (any size) | 49.4% | 51.7% | 50.2% | **50.43%** | 811 | 0.874 | no |
| Home underdogs of 7+ | 46.6% | 51.8% | 49.3% | **49.26%** | 408 | 0.905 | no |
| Road favorites of 14+, fade | 49.7% | 48.7% | 47.8% | **48.75%** | 480 | 0.949 | no |
| Big favorites 21+, fade | 53.5% | 47.6% | 55.3% | **52.12%** | 307 | 0.559 | no |
| Small spreads (<3), take home | 49.0% | 48.1% | 53.8% | **50.15%** | 343 | 0.811 | no |
| Weeks 1-3, take the dog | 53.1% | 50.0% | 55.6% | **52.94%** | 136 | 0.482 | no |
| Weeks 10+, take the dog | 53.1% | 53.1% | 45.5% | **50.46%** | 323 | 0.772 | no |
| Always home | 49.3% | 50.0% | 50.9% | **50.07%** | 2161 | 0.985 | no |
| Always away | 50.7% | 50.0% | 49.1% | **49.93%** | 2161 | 0.989 | no |

`Always home` and `always away` land within a whisker of 50%, which is what an
efficiently priced market looks like from the outside.

## Gate conditions

| Condition | Result |
|-----------|--------|
| model beats market at prediction | FAIL |
| model adds information | FAIL |
| any threshold profitable and consistent | FAIL |
| any hypothesis holds | FAIL |

## What would have to change

The blocker is not the code, the calibration or the thresholds. It is that SP+ and
FPI are public, and the closing line is largely built from them plus the injury and
money information we do not have. Reprocessing public ratings cannot beat a market
that already reads those ratings.

Getting to a real edge means bringing something the close does not contain:

- **Information the market lacks or is slow on** - confirmed injury/QB status ahead
  of the market, weather at kickoff, travel and rest spots. This is where retail
  edges genuinely live, and it means acting early on news, not modelling harder.
- **Softer markets than the ones tested here.** These are consensus numbers from
  major books on FBS games, the most efficient corner of the sport.
- **Reduced juice.** At -105 instead of -110 break-even falls from 52.38% to 51.22%,
  which is worth about 0.44 points of edge - comparable to everything a model
  might realistically add, and available without predicting anything.

Until one of those is in hand and measured, the honest position is to keep pricing
games, keep logging paper, and keep tracking CLV - which is what the pipeline now
does, with LIVE mode locked until this evaluation passes.
