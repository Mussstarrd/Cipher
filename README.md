# Cipher CFB

A college football spread-betting research agent. Every week it pulls games,
power ratings, and market lines; prices every FBS matchup; and writes a
markdown card of plays where its number disagrees with the market by enough
to matter — with confidence tiers, line shopping, a bet log, and honest
season tracking (ROI and closing line value).

Zero dependencies — Python 3.10+ standard library only.

## Quickstart

```bash
# See the whole pipeline run on clearly-synthetic fixture data:
python3 -m cfb_agent demo

# The real weekly flow:
python -m cfb_agent refresh --week 1 --odds-snapshot  # pull data (spends 1 Odds API req)
python -m cfb_agent top     --week 1           # sanity check the ratings
python -m cfb_agent mapping --week 1           # prove the team/line joins
python -m cfb_agent card    --week 1 --log     # write the card, log plays
# ...games happen...
python -m cfb_agent refresh --week 1 --closing # final scores + mark closing lines
python -m cfb_agent settle  --week 1           # grade bets, compute CLV
python -m cfb_agent summary                    # season record / ROI / avg CLV
```

## Data sources

| Source | Needs | Provides |
|--------|-------|----------|
| ESPN public JSON | nothing | schedule, scores, ESPN BET spread, FPI ratings |
| [CollegeFootballData](https://collegefootballdata.com/key) | free key in `CFBD_API_KEY` | SP+ ratings, talent priors, per-book + closing lines |
| [The Odds API](https://the-odds-api.com) | free key in `ODDS_API_KEY` | live multi-book spreads for line shopping |

The pipeline degrades gracefully: with no keys at all it still runs on ESPN
data alone (FPI ratings + one book's line). Each card lists which sources
actually loaded.

### Running inside Claude Code on the web

Remote sessions route traffic through a network policy. For `refresh` to work
there, the environment's allowed domains must include:

```
site.api.espn.com
site.web.api.espn.com
api.collegefootballdata.com
api.the-odds-api.com
```

Configure this (and the `CFBD_API_KEY` / `ODDS_API_KEY` env vars) in the
environment settings — see the
[Claude Code on the web docs](https://code.claude.com/docs/en/claude-code-on-the-web).

## How it prices a game

1. **Composite rating** per team: weighted blend of SP+ (0.50), FPI (0.35),
   and — in weeks 1–4 only, fading out — a talent-composite prior (0.15).
   These sources are *not* natively on the same scale (2026 preseason: SP+ has
   sd 13.55 across FBS, FPI 11.33), so each is mapped onto a common scale
   before blending. Skipping that step compresses every rating gap and biases
   the card toward underdogs.
2. **Fair spread**: `rating(home) − rating(away) + 2.0` home field (0 on
   neutral sites), flat for every team.
3. **Edge**: fair spread vs the best available book number for each side.
4. **Minimum edge**: 2.5 pts in weeks 1–3, 2.0 from week 4 — early-season
   ratings carry more uncertainty.
5. **Tiers**: edge ≥ 4.5 → 3u (A) · ≥ 3.5 → 2u (B) · ≥ the week's minimum
   → 1u (C) · below → pass. Max 10 plays a week; passing is a position.
6. **Quarantine**: any play disagreeing with the multi-book consensus by 7+
   points is held off the card as a presumed data defect until inspected.

All knobs live in `cfb_agent/config.py`.

## Team identity

Team-name matching is the single biggest source of silent, catastrophic error
in a model like this: a fuzzy match of "Miami" onto Miami (OH) does not fail
loudly, it prices the wrong team and produces a large, confident, wrong edge.

So there is no fuzzy matching anywhere. **CFBD team ids and ESPN team ids are
the same namespace** (verified: all 138 FBS teams, and 667 teams overall), and
CFBD's `/games` and `/lines` rows are keyed by the ESPN event id — so the
schedule, ESPN's odds, FPI and CFBD's lines all join on integers. The two feeds
that arrive name-keyed (CFBD ratings/talent, The Odds API) resolve through
`cfb_agent/teams.py` by exact-after-canonicalization lookup plus an explicit
alias table. An unrecognized name raises `UnmappedTeam`; the row is dropped and
reported, never guessed.

`python -m cfb_agent mapping --week N` prints, for ten games, every source's
name for both teams and every book's number — the proof the joins are right.

## Betting posture

Weeks 1–3 are **paper only**, and those cards are labelled `PAPER`. Real money
additionally requires a passing evaluation gate:

```bash
python -m cfb_agent evaluate --seasons 2023,2024,2025
```

`mode_for_week` needs *both* locks open, and the gate defaults to closed, so a
missing or unreadable `data/gate_status.json` can never be mistaken for a pass.
The system will not promote itself to live money on its own authority.

**As of the 2023–2025 evaluation the gate FAILS** — see
`reports/evaluation-2023-2025.md`. The finding is worth stating precisely,
because it is not "the model is broken":

- The model is well calibrated (slope ~0.98 against the market) and predicts
  football reasonably. It is still a **worse** margin forecaster than the
  closing line in all three seasons (15.5–16.2 vs 15.1–15.5 points).
- Regressing actual margin on the market's number *and* ours gives
  `beta_model = -0.018` (t = −0.22) over 2,208 walk-forward games. The closing
  line already contains everything the model knows.
- One real effect does survive: the market **drifts toward our number** after
  the open (t = +6.78). Betting openers captures roughly **+0.3 to +0.6 points**
  of genuine edge, once the line-shopping artifact is subtracted out via a
  coin-flip placebo.
- But margins scatter around the close with SD 15.2 points, so a point of
  spread is worth 2.62% of cover probability and break-even at −110 demands
  **0.91 points**. The engine finds about half the edge it needs: roughly
  −1.5% ROI at −110, break-even around −105.

Single seasons are not evidence. At the 2.5-point threshold the model graded
47.5% / 46.4% / 53.9% across 2023 / 2024 / 2025 — read on 2025 alone it looks
like a winning system, pooled it is 49.29%. The evaluator therefore requires a
result to hold in *every* season and to clear significance, and its hypotheses
are pre-registered so the list cannot grow to fit whatever happened to work.

## The Odds API budget

The free tier is 500 requests/month, so the binding constraint is snapshot
count. `cfb_agent/sources/oddsapi.py` enforces it rather than trusting the
caller: scoped to `americanfootball_ncaaf` / `us` / `spreads` (1 request per
snapshot), capped at 3 snapshots per week (Tue open, Fri night, Sat ~60 min
pre-kick), counted in `data/oddsapi_budget.json`, with every raw response
archived under `data/cache/raw/`. `refresh` reads the archive and spends
nothing unless you pass `--odds-snapshot`. Check spend with
`python -m cfb_agent budget --week N`.

## The honesty rules

- Break-even at −110 is **52.38%**. Anything that can't clear that is noise.
- **Closing line value is the scoreboard.** Consistently beating the closing
  line predicts long-term profit; a hot 3-week record does not. `settle`
  computes CLV on every bet automatically.
- 1 unit = 1% of bankroll. A full card risks single-digit percent, ever.
- This is a research tool, not financial advice. Bet only where it's legal
  for you, only with money you can afford to lose.

## Layout

```
cfb_agent/
  config.py      knobs: HFA, tiers, thresholds, keys
  http.py        cached stdlib HTTP client
  db.py          SQLite schema (games, lines, ratings, bets)
  sources/       espn.py · cfbd.py · oddsapi.py
  refresh.py     pull all reachable sources into the DB
  ratings.py     composite ratings -> predicted margins
  edges.py       edge finder, line shopping, tiering
  card.py        weekly markdown card
  tracker.py     bet log, settlement, ROI + CLV
  teams.py       team id registry + strict name resolution (no fuzzy matching)
  backtest.py    look-ahead-free historical backtest; the real-money gate
  fixtures.py    synthetic demo data (fictional teams, season 1999)
reports/         committed weekly cards
data/            SQLite + HTTP cache (gitignored)
```
