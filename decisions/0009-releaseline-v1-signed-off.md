# 0009 — ReleaseLine v1 signed off as shelf-ready

**Date:** 2026-10-31
**Phase:** 4
**Decided by:** ux-reviewer and qa-checker (recorded by historian)
**Status:** accepted

## Context
Round 1 reviews (`app/reviews/ux-01.md`, `app/reviews/qa-01.md`) both failed on the same
blocker: cutting a clip with a start past the end of the audio wrote an empty file and
marked the stage done. UX also failed on buttons stuck in their busy labels. The builder
fixed both and thirteen non-blocking items (`app/NOTES.md` items 16–22).

## Options considered
- Ship with the round-1 state: violates CLAUDE.md criterion 3 (never a corrupted file).
- Fix blockers only: cheap non-blockers would have shipped as known friction.
- Fix blockers plus the cheap non-blockers, re-review both: chosen.

## Decision
`app/reviews/ux-02.md` PASS, `app/reviews/qa-02.md` PASS. ReleaseLine v1 is shelf-ready
per the six-point definition. Remaining non-blocking findings go to `ideas/backlog.md`.

## Why
All six shelf-ready points verified by someone other than the builder: one-command cold
start with zero deviations; UI clicked through at 1280×720 and 390 px with zero page
errors; every provoked error is a sentence; 77 tests, 0 skipped with ffmpeg present and
clean skips without it; README and walkthrough match the real strings; no banned words.

## Consequences
First `/improve` candidates are already queued. Revisit the "partially corrupt DB gives a
generic 500" finding if the owner ever sees it.
