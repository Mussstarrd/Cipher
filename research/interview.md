# Owner interview

Status: **delegated** — 2026-10-31

The owner declined the question-and-answer interview and asked the studio to work it out
itself. The studio's working profile of the owner is therefore held in-session and is
**not** written to this repo. What follows is the product-level output the later phases
need: the pain-area hypotheses the studio is proceeding on, in the order it ranks them.

The owner can correct, reorder, or strike any line in one sentence and the studio
re-runs from there.

## Pain-area hypotheses

| # | Hypothesis | Why it ranks here |
|---|------------|-------------------|
| A | A personal trading-research workflow (screener thesis plus daily generated reports) has no durable home: nothing links a flagged setup to what was actually done to how it worked out, and nothing accumulates into a scorecard. | Daily use already exists; compounding is natural; honesty rule is satisfiable (record and compute, never predict). |
| B | Email is being used as a catch-all system of record and has stopped working as one. | Daily pain, but any fix needs Gmail OAuth and competes with mature products. |
| C | Household and school logistics arrive by email and are relayed by hand. | Weekly pain, shared with another adult, privacy-sensitive, crowded market. |
| D | Work-travel expense preparation. | Recurring but bounded; employer tooling likely mandatory. |
| E | A personal music-production catalog is scattered across folders and versions. | High delight, episodic use, low pain. |

## Constraints every candidate must respect

- Usable in five minutes a day from a laptop or Android phone browser.
- Local-first, zero required accounts, default configuration costs nothing.
- Brokerage data arrives by export or paste first; any live API is optional.
- Anything touching trading records and computes from the owner's own entries. It
  never promises returns or predicts prices.

## What "life-changing" means (working definition)

Open one local page each evening and know, in under five minutes, whether the system is
working, with that answer getting sharper every week.

## Owner's direct statement (2026-10-31, supersedes the hypotheses above)

The owner cancelled the first winner and said what he actually wants: a tool that
streamlines releasing the music he makes. The full quote is in `decisions/0006`. The
workflow he described, step by step:

1. Generate or finish a track in Suno and download it.
2. Master it in FL Studio (Fruity Loops).
3. Cut a 30-second clip from the track.
4. Write a prompt (he called it a "catchy hook generator") and generate an AI video clip
   in Kling to go with the beat.
5. Edit the audio clip and the video together.
6. Write tags and a description.
7. Upload to the platforms.

He also named the two shapes he would accept: a prompt generator for Kling, or a
step-by-step HTML helper that walks each track through the whole chain. The studio is
building the second, with the first inside it.

Constraints carried forward: Windows likely (FL Studio), Android phone, local-first,
no required accounts, honest copy (the tool organizes, generates text drafts from his
inputs, and runs local commands; it does not promise plays, streams, or income).
