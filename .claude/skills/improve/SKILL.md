---
name: improve
description: Runs Phase 5 of the forge-zero studio — asks the owner what annoyed them since last time (and reads the in-app feedback notes), proposes the three highest-leverage changes, implements the one they pick to shelf-ready standard with UX and QA sign-off, and logs the decision. Run it whenever the owner has used the app for a while.
---

# /improve — Phase 5, one iteration

Read `CLAUDE.md`, `spec/SPEC.md`, the latest `decisions/` entries, and `ideas/backlog.md`
if it exists. If `app/` does not exist, stop and tell the owner to run `/build-winner`.

## Step 1 — Gather friction
1. Read the in-app feedback file (location is in `spec/SPEC.md`; default
   `app/data/feedback.md`). Summarize the entries since the last `/improve` decision.
2. Ask the owner, in one message: **"What annoyed you since last time?"** Offer the
   feedback-note summary as a prompt, and ask them to add anything it misses. Also ask
   how often they actually opened it, in rough terms. Wait for their answer.
3. Record their answer in `research/friction-log.md` with the date (append-only).

## Step 2 — Propose three changes
From the friction, the backlog, and your own reading of the app, propose exactly **three**
changes. For each:
- What changes, in two sentences.
- Which friction it removes (quote the owner).
- Score on the six `CLAUDE.md` criteria, briefly.
- Estimated size: hours or days.
- What it risks breaking.
Rank them and recommend one. Prefer changes that raise open frequency or compounding over
pure polish, unless the owner's friction says otherwise. Ask the owner to pick one.
**Wait for the pick.**

## Step 3 — Ship the pick
1. `historian` logs: the pick, the two deferred (which go to `ideas/backlog.md`), and
   why.
2. If the change alters the spec, `architect` updates `spec/SPEC.md` (a new dated
   section, not a silent rewrite) before the builder starts.
3. `builder` implements it, including tests and README updates.
4. `ux-reviewer` and `qa-checker` sign off (new review files). Loop fixes until both PASS.
5. Update `app/WALKTHROUGH.md` if a screen changed.

## Step 4 — Report
Tell the owner: what changed, how to see it, test results, what was deferred and where
it is logged. One short message.

## Rules
- One change per run. Resist bundling. If two are trivial, still pick one; the other
  waits.
- The feedback note itself must keep working after every change. QA checks it.
- Shelf-ready standard applies to the change, not just the original build.
- Commit with a message naming the friction it removes.
