# 0005 — Stack: Python stdlib only; three largest v1 scope cuts

**Date:** 2026-10-31
**Phase:** 4
**Decided by:** architect
**Status:** accepted

## Context
`spec/SPEC.md` needed a stack that keeps "runs with one command" true on a stranger's
machine a year from now, and a v1 boundary tight enough to build in days.

## Options considered
- Flask/FastAPI + a JS framework: familiar, but adds a dependency install step and a
  build step, both of which rot.
- Python 3.10+ standard library only (http.server, sqlite3, json, csv, email,
  html.parser, unittest) with plain HTML/CSS/JS served from the app: zero installs, no
  build, fully offline.
- Node/Electron: heavier, and a desktop shell is not needed for a local page.

## Decision
Standard library only. Run: `python3 -m scorecard serve`. Test:
`python3 -m unittest discover -s tests`. Data in `./data` (override with
`SCORECARD_DATA`), port 8459 (override with `SCORECARD_PORT`).

Scope cuts for v1:
1. **No live brokerage API and no price fetching.** Outcomes come only from the
   Robinhood CSV export and manual marks (per decision 0003).
2. **No IMAP or email reading.** The owner saves, forwards, or pastes report files; the
   intended path is a one-line change so the pipeline also writes JSON Lines.
3. **No dollar P&L and no reason tag.** v1 reports counts per signal only; options
   multipliers and fees make dollars easy to get wrong. The reason tag is the first
   post-v1 improvement (`ideas/backlog.md`).

## Why
Buildability (criterion 5) and the shelf-ready rule "runs with one command". Honesty:
counts from logged reports and fills are defensible; dollar figures and anything that
reads as a return claim are not, yet.

## Consequences
Front end stays deliberately plain. Any future dependency must justify itself against
"would a stranger's `python3 -m scorecard serve` still work in a year". Revisit if the
owner asks for dollar P&L or charts in `/improve`.
