# 0006 — Pivot: owner cancelled Pipeline Scorecard; winner is a music release pipeline helper

**Date:** 2026-10-31
**Phase:** pivot
**Decided by:** owner
**Status:** accepted (supersedes 0004 and 0005)

## Context
The owner stopped the Phase 4 build and stated directly what he wants: something that
streamlines putting out the music he is making. His words, lightly cleaned up for
transcription errors:

> "I want something that streamlines me being able to put out the music I'm making.
> Whether it's a catchy hook generator for a prompt that I put into Kling to make video
> clips for my beats, or just a step-by-step HTML helper to get my tracks from download
> from Suno, to master in Fruity Loops, to cut a 30-second clip, to generate the AI
> video to go with the clip, to edit them both together, to giving tags and
> descriptions, to uploading, etc."

This is the first direct interview answer and it outranks every inference in Phase 1–3.
The stall was an idea stall: the studio weighted the trading signal (which was visible in
connected tooling) over the music signal (which was visible only as files).

## Options considered
- Finish the scorecard anyway: ignores a direct, specific statement from the only customer.
- Re-run the full invent/judge cycle: the owner has already described the product; a
  second round of 25 ideas would be ceremony.
- Spec and build the described product directly, keeping the studio rules.

## Decision
Build the described product directly. The partial scorecard code (untracked, never
committed) was deleted; its spec is archived at `archive/spec-pipeline-scorecard/`.
Research and ideas files stay as history.

## Why
Criteria 1 and 2 by the owner's own account: this is the thing he does and wants to do
more of. Pain area E in the hypotheses was under-ranked.

## Consequences
Phase 1 inference risk is now largely gone for this product. Revisit if, at the first
`/improve`, the owner reports he has not released a track with it.
