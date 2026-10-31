---
name: build-winner
description: Runs Phase 4 of the forge-zero studio — architect writes the one-page spec, builder implements it to shelf-ready standard, ux-reviewer and qa-checker both sign off, and the owner gets a working app, tests, README, and screen-by-screen walkthrough. Use after the owner has picked a finalist.
---

# /build-winner — Phase 4

Read `CLAUDE.md` first. Find the owner's pick in the latest `decisions/` entry and in
`ideas/finalists.md`. If there is no recorded pick, stop and tell the owner to run
`/discover`.

## Step 1 — Spec (agent: `architect`)
- Delegate to `architect`. Output: `spec/SPEC.md`, one page, all ten sections from the
  architect's brief (users, core loop, data model, UI sketch, CLI, stack, out of scope,
  app-specific shelf-ready checklist, risks).
- Read it. Check: local-first, no cloud bill, one run command, one test command, the
  in-app feedback note is in the UI sketch, the honesty rules hold in the name and
  description.
- Show the owner the spec summary (name, core loop, stack, top three scope cuts) in a few
  lines. **Only block for their input if a scope cut changes what the thing fundamentally
  is.** Otherwise proceed.
- `historian` logs the stack choice and the three largest scope cuts.

## Step 2 — Build (agent: `builder`)
- Delegate to `builder` with the spec. It builds into `app/`: skeleton, core loop,
  error paths, tests, in-app feedback note, README, `WALKTHROUGH.md`.
- Builder must run the tests and the app and report what it saw. If tests fail, it says
  so; you do not proceed to review with failing tests.

## Step 3 — Sign-off (agents: `ux-reviewer`, `qa-checker`, in parallel)
- Launch both. Each writes `app/reviews/ux-NN.md` or `app/reviews/qa-NN.md` with an
  explicit PASS or FAIL.
- If either FAILs: send blocking findings to `builder`, then re-run **that reviewer**
  (new file, incremented NN). Loop until both PASS. Log in `decisions/` any FAIL that
  changed the design, not routine bug fixes.
- Both must PASS. There is no override.

## Step 4 — Deliver
Report to the owner, in this order:
1. The one command to run it and the one command to test it.
2. Test results (counts, verbatim summary line).
3. The screen-by-screen walkthrough (from `app/WALKTHROUGH.md`, condensed).
4. Where their data lives and how to back it up.
5. What is out of scope for v1 (so they know it was heard) and where the backlog is.
6. The review verdicts with links to the review files.
7. "Run `/improve` once you have used it for a few days."

## Rules
- Spec is the contract. If the builder wants to add something, it goes in
  `ideas/backlog.md`.
- Shelf-ready per `CLAUDE.md`'s six points, or it is not done. Say which points are
  unmet if you must stop early.
- Commit at each step boundary with a clear message.
