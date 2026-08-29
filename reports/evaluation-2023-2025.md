# LineHawk - Model Evaluation 2023-2025

Generated 2026-08-29 17:51 UTC. 2,208 priced games, walk-forward.

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
