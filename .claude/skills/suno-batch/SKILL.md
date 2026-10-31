---
name: suno-batch
description: Generate N varied Suno prompts for one instrumental lane for A/B testing, as a markdown table or JSON. Use when the user wants several Style-field variants, a test matrix, or a batch of prompts for a lane (connor-price, paak-pocket, jid-dark, phonk-experimental, hybrid).
---

# suno-batch

Produce N variants for one lane so the user can A/B in Suno. By default the batch holds the Lyrics field, Exclude field, arc and BPM constant and varies only the Style wording, seed and Weirdness, so each comparison isolates the Style text.

## Inputs

- lane (required), N (default 5), optional: BPM, key, mood, arc, constraints, `--varyArc` (cycle energy arcs across rows), `--bpm` (fix tempo; otherwise rows step around the lane default), format `table` (default) | `json` | `blocks`.

## Procedure

```bash
node forge.js --lane <lane> --batch <N> [--bpm n] [--mood "..."] [--arc <arc>] [--seed <base>] --format table
node forge.js --lane <lane> --batch <N> --format json --out batch.json
node forge.js --lane <lane> --batch <N> --format blocks --out batch.md   # full eight-section blocks per row
```

- Every row is QA-checked by the CLI (JSON output includes a `qa` object per row). Do not deliver a row with `qa.ok: false`.
- Table output lists: row, seed, BPM, key, arc, Weirdness, Style Influence, Style field, then the shared Lyrics and Exclude fields once underneath. Remind the user that model (`v6`), Custom mode, Instrumental ON, Max Mode ON and Variety OFF apply to every row.
- For a "safer vs weirder" ladder, pass `--weirdness` per row manually or let the default pattern run (lane default, -10, +10, -20, +20 cycling).
- For a tempo sweep, run once per BPM with the same seed and `--format table`, then concatenate.

## Delivering

Return the table or JSON verbatim, then two or three sentences: what dimension the batch varies, how to A/B (generate rows back to back with identical toggles; keep the pair whose drums feel most human; then feed the winner to `/suno-critique` for the next pass). Never suggest audio uploads or Audio Influence.
