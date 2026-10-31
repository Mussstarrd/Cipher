---
name: pivot
description: Re-runs judging for the forge-zero studio when the winner has stalled — the owner stopped opening it, the core loop did not compound, or a constraint killed it. Diagnoses why, re-scores the remaining finalists plus any new candidates, and presents a recommendation to keep, mutate, or replace. Logs the decision.
---

# /pivot — re-judge when the winner stalls

Read `CLAUDE.md`, `spec/SPEC.md`, `ideas/finalists.md`, `ideas/scores.md`,
`research/friction-log.md`, the in-app feedback file, and all `decisions/` entries.

## Step 1 — Diagnose the stall
Ask the owner one batch of questions (at most 3):
- When did you last open it, and what did you open instead?
- Was the problem the idea, the execution, or your life changing?
- Is there anything in it you would miss if it vanished?
Record the answers in `research/friction-log.md` under a "Pivot" heading with the date.

Then write your own one-paragraph diagnosis, classifying the stall as one of:
- **Execution stall** — right idea, the build got in the way (fixable by `/improve`).
- **Idea stall** — nobody opens it on a random Tuesday; the six-month question was
  answered "no" by reality.
- **Constraint stall** — something external (platform, legal, data access) broke it.
- **Life stall** — the owner's situation changed and the pain is gone or different.

If it is an execution stall, say so, recommend `/improve`, log it, and stop.

## Step 2 — Re-judge (agent: `judge`)
- Hand `judge` the diagnosis, the two unpicked finalists, and the top 10 from
  `ideas/scores.md`. If the diagnosis is a life stall, first have `interviewer` ask up
  to 4 refresh questions and update `research/interview.md` (new dated section), and have
  `inventor` add 5–10 new candidates grounded in the new answers.
- `judge` re-scores with what has been learned, re-asks the six-month question with the
  stall as evidence, and writes `ideas/finalists-pivot-NN.md`.

## Step 3 — Present
Show the owner three options side by side, always including **"keep the current winner
and fix X"** as one of them even if the judge ranks it last. Scores, tradeoffs,
recommendation, and what each option costs in days.

Ask them to pick. **Wait.**

## Step 4 — Log and route
- `historian` logs the stall diagnosis and the pick. If the winner is replaced, mark the
  original pick's decision entry "superseded by NNNN".
- Keep: run `/improve`. Mutate: `inventor` mutates, back to Step 2. Replace: archive
  `app/` to `archive/<name>/`, keep the data, run `/build-winner`.

## Rules
- Do not throw away a working app lightly. Archive, never delete; the data belongs to
  the owner.
- Be honest in the diagnosis. If the studio built the wrong thing, say so.
