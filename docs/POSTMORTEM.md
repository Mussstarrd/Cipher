# LineHawk — post-mortem

**Status: ARCHIVED. The model does not have an edge. Do not bet it.**

Closed 2026-08-30, one day after the first live data ever ran through it.

---

## The verdict in one line

The model finds **+0.392 points** of genuine edge. Beating the vig at −110
requires **0.91 points**. It is real, it is well measured, and it is about half
of what it needs to be.

## What this cost, and what it avoided

The whole thing cost a day, two metered Odds API requests, and no betting losses
at all. The counterfactual is the version where the first card looked plausible
and got bet: at 51.04% against a 52.38% break-even, that is roughly −2.5% of
turnover, indefinitely, and it would have taken several seasons of results to
distinguish from bad luck.

A negative result found in a day is the cheap outcome. The expensive one is the
same result found slowly, in money.

## What was actually established

Not "it didn't work" — five specific, quantified findings:

1. **The closing line already contains everything the model knows.** Regressing
   actual margin on the market's number and ours together gives
   `beta_model = −0.018, SE 0.081, t = −0.22` over 2,208 walk-forward games.
   Adding the model to the closing line moves residual error from 15.231 to
   15.231 points.

2. **Our forecast is worse than the market's**, in every season tested
   (15.5–16.2 points vs 15.1–15.5).

3. **One real effect exists.** The market drifts toward our number after the
   open: `+0.0780, SE 0.0115, t = +6.78`. Opening lines carry error and we see
   part of it. This is worth +0.392 points once the line-shopping premium is
   subtracted out.

4. **The measurement framework is correct.** Over five seasons and 1,603 plays,
   the placebo-subtracted edge predicts a 51.04% win rate and the observed rate
   is 51.03%. Theory and observation agree to two decimal places.

5. **The market is efficient at this level.** "Always home" grades 50.07%,
   "always away" 49.93%. Nine pre-registered inefficiency hypotheses all fail.

## The six lessons

### 1. Compute the required effect size before building anything

The original plan specified tier thresholds (4.5 / 3.5 / 2.5 points) and a CLV
gate (+0.35) in detail. Nobody had computed that **margins scatter around the
close with SD 15.2 points, so break-even at −110 demands 0.91 points of edge.**

That one number reframes the entire project, and it was available on day zero
from public data. Had it been computed first, the obvious question would have
been "what plausibly gets us 0.91 points?" — and the honest answer, for a blend
of public ratings, is "nothing".

**Transferable:** before building a predictive system, compute the effect size
required for it to matter. Then ask whether the proposed inputs could plausibly
produce it. This is a ten-minute calculation that governs everything downstream.

### 2. Run the null hypothesis through your own success criteria

The specified gate was "≥250 plays with mean CLV > +0.35". The model scored
+0.365 and **passed**.

A coin flip, on the same games with the same entry method, scored **+0.675**.

The gate was not measuring model skill. It was measuring the premium for taking
the best of several opening numbers against a median close — something you get
for shopping, regardless of which side you pick. Any gate a placebo passes is
not a gate.

**Transferable:** whatever criterion decides your project, run a random or
trivial baseline through it first. If the baseline passes, the criterion is
broken, and you were about to be fooled by it.

### 3. Patterns found by inspection must be confirmed on data that did not
generate them

Two things looked real in 2023–2025:

- weeks 1–4 were negative (−0.24 edge, 48.2% ATS, consistent across all three
  seasons, and mechanistically explicable — the in-season fit has no data yet)
- Power-5 vs Power-5 in weeks 5+ was the best cell (+0.83 edge, 55.76% ATS)

Both were tested on 2021–2022, which had never been used to form them. **Both
failed.** Weeks 1–4 flipped sign to +0.538. The 55.76% cell graded 51.35%.

They were found after inspecting roughly fourteen segments, which is exactly the
condition under which two of them look significant by chance. The mechanistic
story for weeks 1–4 was persuasive and still wrong — a plausible causal
explanation is not evidence, it is the thing that makes noise feel like signal.

**Transferable:** hold out data specifically for confirming patterns you find by
looking. Count how many things you looked at. A good explanation for a spurious
pattern is a hazard, not a comfort.

### 4. The dangerous moments are the ones that look like success

Three separate times this project produced a result that could have been
shipped:

- The first card put **all ten plays on the underdog**. That is not ten edges,
  it is one systematic bias — FPI and SP+ are not on the same scale (sd 11.33 vs
  13.55), and blending them raw compressed every rating gap by about a point.
- The 2025 season alone graded **53.9%** at the 2.5-point threshold. Pooled
  across three seasons it was 49.29%.
- The CLV gate **passed** at +0.365, on the strength of four corrupt opening
  numbers; before filtering them it read +0.498.

Each was caught by asking "why is this good?" with the same energy as "why is
this broken?".

**Transferable:** investigate favourable results at least as hard as unfavourable
ones. Errors that flatter you do not announce themselves, and every one of these
was found by being suspicious of good news.

### 5. If your inputs are public, your edge is your processing advantage — which
is roughly zero

SP+ and FPI are published. The closing line is built by people who read them,
plus injury and money information we do not have. Reprocessing public ratings
cannot beat a market that already reads those ratings.

This is not a statement about effort or sophistication. A better model of the
same public inputs converges toward the market's number, which is the definition
of having no edge.

**Transferable:** locate your informational advantage before modelling. If you
cannot name something you know that the price does not, there is no edge to find
and the modelling is decoration.

### 6. Choose a metric that converges before you run out of time

At 2% ROI over 300 bets a season, the expected result is **+6 units against a
standard deviation of ~17 units**. You cannot distinguish a real edge from
nothing by results — not in a season, not in three.

Closing line value has SD ~2 points, so 300 bets give SE ≈ 0.115 points and a
+0.4-point effect shows up at t ≈ 3.5 **within one season**. Roughly an order of
magnitude faster.

**Transferable:** when the outcome you care about is noisy, find the
lower-variance leading indicator and gate on that instead. Otherwise you are
running an experiment that cannot conclude.

## What transfers if this is ever revisited

The infrastructure is market-agnostic and it is the good part:

- **`cfb_agent/teams.py`** — the team registry. CFBD and ESPN share an id
  namespace (138/138 FBS, 667 overall) and CFBD's `/games` and `/lines` are keyed
  by ESPN event id, so everything joins on integers. Name resolution is exact
  with an explicit alias table and fails loudly. This solved the single biggest
  source of silent catastrophic error and would be needed again immediately.
- **`cfb_agent/evaluate.py`** — walk-forward harness, incremental-information
  test, placebo construction, per-season consistency, pre-registered hypotheses.
  Sport-agnostic. This is what makes a verdict trustworthy.
- **`cfb_agent/model.py`** — prior-shrunk ridge ratings from results. Genuinely
  good at what it does; calibration slope 0.98 against the market, residual 4.6.
  It is a good rating system that happens not to be a profitable one.
- **`cfb_agent/backtest.py`** — the no-look-ahead construction, and the
  documented trap that CFBD's `/ratings/sp?year=Y&week=W` ignores `week` and
  returns end-of-season values.

## What would have to be true to restart

Not "a better model". Specifically one of:

1. **An informational edge** — something known before the market prices it.
   Confirmed injury or QB status, weather, or a genuinely faster news pipeline.
   The drift result (t = +6.78) shows openers are where softness lives.
2. **Exchange-level pricing.** At +0.392 points the strategy wins 51.04%, which
   is −2.55% at −110, −0.34% at −105, and **+2.08% at −100**. The edge is real
   and only monetises where commission replaces the spread. Whether such prices
   are obtainable at size on CFB sides is an open question.
3. **A softer market.** CFB sides against major books is the most efficient
   corner of the sport. Totals, first-half lines, player props and smaller
   conferences carry lower limits and thinner competition — and the same harness
   would evaluate them.

Absent one of those, the honest position is the one recorded here.

## Closing note

The engine works. It prices every FBS game, joins four data sources without a
single mismatched team, refuses to bet games in progress, quarantines its own
implausible outputs, and locks itself out of live money until an evaluation it
cannot fake says otherwise.

It is a well-built instrument that returned a clear reading, and the reading was
that there is nothing here to bet. That is a real answer to a real question, and
it was obtained before it cost anything. Filed accordingly.
