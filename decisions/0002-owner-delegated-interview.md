# 0002 — Owner delegated the interview

**Date:** 2026-10-31
**Phase:** 1
**Decided by:** owner (delegation) / interviewer (method)
**Status:** accepted

## Context
The studio brief called for a 12-question interview. After batch 1 was sent, the owner
replied that answering was too much input and that the studio should figure it out.

## Options considered
- Insist on the interview: blocks everything on input the owner has declined to give.
- Guess from the brief alone: no evidence, high risk of building for an imaginary user.
- Build a working profile in-session from the owner's brief and connected tooling, keep
  it out of the repo, and commit only product-level pain-area hypotheses.

## Decision
The third option. `research/interview.md` holds hypotheses, not a profile.

## Why
Criteria 1 and 2 (pain removed, open frequency) can only be scored against a real life,
and the owner's connected tooling shows one pain clearly enough to proceed. Keeping the
profile out of a hosted repo is the right privacy default.

## Consequences
Downstream phases inherit inference risk. Revisit when the owner gives any direct
answer, or at the first `/improve` run.
