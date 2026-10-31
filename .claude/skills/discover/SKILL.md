---
name: discover
description: Runs Phases 1–3 of the forge-zero studio — owner interview, landscape research, 25+ candidate ideas, scoring, and three finalists for the owner to pick from. Use to start discovery or to resume it at whichever phase is incomplete.
---

# /discover — Phases 1 to 3

You are orchestrating discovery for the one-customer studio defined in `CLAUDE.md`.
Read `CLAUDE.md` first. Then determine where discovery stands and resume from there.

## Resume logic
Check, in order:
1. `research/interview.md` — if the status line is not "complete", run Phase 1.
2. `research/landscape.md` — if missing, run Phase 2.
3. `ideas/candidates.md` — if missing or under 25 ideas, run Phase 3a.
4. `ideas/scores.md` and `ideas/finalists.md` — if missing, run Phase 3b.
5. If finalists exist but no decision in `decisions/` records the owner's pick, present
   the finalists again and wait for the pick.

## Phase 1 — Interview (agent: `interviewer`)
- Delegate to the `interviewer` agent, or run its brief directly if a batch is already
  in flight and the owner has just answered.
- At most 12 questions, batches of 3–4. Record every batch to `research/interview.md`
  before asking the next one. Probe vague answers once.
- **Stop and wait for the owner after each batch.** This is the one place in the studio
  where blocking on the owner is correct.
- When all batches are answered, write the Interviewer's synthesis, set status to
  "complete", and have `historian` log "discovery began; pain areas identified".

## Phase 2 — Scout (agent: `scout`)
- Delegate to `scout` with the synthesis. It must web-search and cite.
- Output: `research/landscape.md`. Verify it has, per pain area: what exists, what
  people complain about, failed attempts, communities, what is missing.
- `historian` logs any constraints the landscape imposes (legal, API access, lock-in).

## Phase 3a — Invent (agent: `inventor`)
- Delegate to `inventor`. Verify `ideas/candidates.md` has at least 25 ideas, each with
  a pitch, a "why it might fail", band label, pain areas served, nearest existing thing.
- Verify the bands are actually spread (roughly 10 conservative / 10 adjacent / 5+ weird).
  If not, send it back for more.

## Phase 3b — Judge (agent: `judge`)
- Delegate to `judge`. Verify `ideas/scores.md` scores all candidates on all six criteria
  with a rationale per score, lists the dropped 15 with reasons, and stress-tests the
  top 10 with the six-month question.
- Verify `ideas/finalists.md` presents exactly three, side by side, with scores,
  tradeoffs, and a recommendation.

## Present to the owner
Show the three finalists side by side in chat: a compact scores table, two sentences
each, the key tradeoff, the judge's recommendation, and the one question they should ask
themselves. Then ask them to **pick one, or name one to mutate**.

- If they pick: `historian` logs the decision (decided by: owner). Tell them to run
  `/build-winner`.
- If they ask for a mutation: `inventor` produces 5 variants, `judge` re-scores just
  those against the other two finalists, present again. Log the mutation request.

## Rules
- Do not skip the owner's pick. Do not pick for them.
- Every phase's output file must exist and be checked before the next phase starts.
- Keep the owner informed in one or two lines when a phase completes.
