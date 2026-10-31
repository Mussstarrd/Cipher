# 0004 — Winner: Pipeline Scorecard (owner delegated the pick)

**Date:** 2026-10-31
**Phase:** 3
**Decided by:** owner (delegation) / judge (recommendation)
**Status:** accepted

## Context
Thirty candidates were scored (`ideas/scores.md`). Three finalists were presented
(`ideas/finalists.md`). The owner had earlier asked the studio to make its own calls
rather than ask for input, so the judge's recommendation stands as the pick.

## Options considered
- **A. Pipeline Scorecard** (merge of candidates 2 Signal Scorecard, 5 Report Vault,
  18 Setups Diff) — 24/30, six-month verdict Yes. Reports the owner's pipeline already
  generates land in a folder or are pasted in; the page shows each day's new and
  persisting flags, what was done about each, and a rolling per-signal scorecard.
  Zero input on a normal day.
- **B. Setup Ledger with reason tag** (1 + 14) — 22/30, Probably. Paste-driven ledger;
  asks for a daily habit.
- **C. Markdown Journal** (28) — 20/30, Probably. Two typed lines a day in a synced text
  file; phone-friendly; asks for a daily habit.

## Decision
Build A, staged: report vault and day view first, scorecard second. B's "reason tag" is
queued as the first post-v1 improvement (`ideas/backlog.md`). C is the fallback if the
pipeline cannot be pointed at a folder.

## Why
Criteria 1 and 2 dominate: the owner already produces the data every market day and has
said he wants low-input tools. A is the only finalist that is useful without a new habit.
Compounding is natural (every day adds to the scorecard). Honesty holds: the thing
records, organizes, and computes from the owner's own reports and fills; it predicts
nothing.

## Consequences
- The pipeline must write reports to a folder (or the owner pastes them). The app must
  also accept a plain documented format so the parser is not guessing forever.
- v1 scores taken trades from imported fills; untaken flags are tracked as flags, and any
  "what would have happened" figure is labelled as an estimate from entered prices only.
- Revisit at the first `/improve` if the owner has not pointed the pipeline at the
  folder within two weeks; that would indicate B or C was the right call.
