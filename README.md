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
python3 -m cfb_agent refresh --week 1          # pull data into data/cipher.sqlite
python3 -m cfb_agent card    --week 1 --log    # write reports/2026-week01.md, log plays
# ...games happen...
python3 -m cfb_agent refresh --week 1          # pick up final scores + closing lines
python3 -m cfb_agent settle  --week 1          # grade bets, compute CLV
python3 -m cfb_agent summary                   # season record / ROI / avg CLV
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
   All on a points-above-average scale.
2. **Fair spread**: `rating(home) − rating(away) + 2.3` home field (0 on
   neutral sites).
3. **Edge**: fair spread vs the best available book number for each side.
4. **Tiers**: edge ≥ 4.0 pts → 3u (A) · ≥ 2.5 → 2u (B) · ≥ 1.5 → 1u (C) ·
   below → pass. Max 10 plays a week; passing is a position.

All knobs live in `cfb_agent/config.py`.

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
  fixtures.py    synthetic demo data (fictional teams, season 1999)
reports/         committed weekly cards
data/            SQLite + HTTP cache (gitignored)
```
