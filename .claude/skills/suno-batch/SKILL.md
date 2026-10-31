---
name: suno-batch
description: Generate N one-of-one Suno prompts for a lane for A/B testing, as a markdown table, JSON, or full blocks. Use when the user wants several rolls, a test matrix, or a batch of prompts for a lane (any, connor-price, paak-pocket, jid-dark, phonk-experimental, hybrid).
---

# suno-batch

Produce N rolls for one lane. Every row has a distinct fingerprint (different lead source, processing, motif, articulation, mode or key), so the batch is a real A/B of sounds, not of wording.

## Inputs

lane (or `any`), N (default 5), optional: BPM, key, mood, mode, layout, constraints, seed (base), `--wild`, format `table` (default) | `json` | `blocks`.

## Procedure

```bash
node forge.js --lane <lane> --batch <N> [--bpm n] [--mood "..."] [--mode loop|straight|switch] [--seed <base>] --format table
node forge.js --lane <lane> --batch <N> --format json --out batch.json      # per-row qa object included
node forge.js --lane <lane> --batch <N> --format blocks --out batch.md      # full eight-section blocks
```

- Every row is QA-checked; do not deliver a row with `qa.ok: false`.
- To isolate one variable, fix the others: `--mode straight --bpm 92` and vary only the seed; or fix `--seed 1` and vary `--mode`.
- The table shows seed, BPM, key, layout, lead handle, sliders and the head tags; the full Lyrics, Styles and Exclude fields follow per row.

## Delivering

Return the table or JSON verbatim, then two or three sentences: what the batch varies, how to A/B (generate rows back to back with identical settings, Variety Off, Max Mode On; keep the row whose drums sit furthest behind the grid and whose lead sounds least like a preset), and that the winner's seed reproduces it. Never suggest audio uploads or Audio Influence.
