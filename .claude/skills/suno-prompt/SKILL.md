---
name: suno-prompt
description: Generate a copy-ready Suno prompt block for a radio-ready hip hop INSTRUMENTAL. Use when the user asks for a Suno prompt, beat prompt, Style field, or instrumental for one of the lanes (connor-price, paak-pocket, jid-dark, phonk-experimental, hybrid) or describes a hip hop instrumental they want Suno to make. Inputs: lane, BPM, key, mood, energy arc, extra constraints.
---

# suno-prompt

Produce ONE copy-ready block with eight labeled sections for a Suno hip hop instrumental. Follow `CLAUDE.md` rules: no artist names, instrumental by default, negatives only in Exclude Styles, verified limits, no audio references.

## Inputs

| Input | Values | Default |
|---|---|---|
| lane | `connor-price` / `paak-pocket` / `jid-dark` / `phonk-experimental` / `hybrid` (or `--blend a,b,c`) | ask if unclear; `hybrid` if the user describes a mix |
| BPM | number (see `reference/tempo-key-tables.md` per lane) | lane default |
| key | e.g. `F minor`, `D Dorian` | lane default |
| mood | comma words | none |
| energy arc | `flat-bang` / `build-release` / `slow-burn` / `stop-start` / `beat-switch` | lane default |
| constraints | extra must-have descriptors, extra Exclude terms, title, slider overrides | none |

Map free-text requests onto these. "Bouncy, bright, playful" -> connor-price. "Live drums, Rhodes, pocket" -> paak-pocket. "Dark, dense, chops, strings, switch-up" -> jid-dark. "Memphis, cowbell, tape, screwed" -> phonk-experimental. Two or more of those -> hybrid or `--blend`.

## Procedure

1. Run the generator; it is the source of truth for timing, limits and vocabulary:
   ```bash
   node forge.js --lane <lane> --bpm <n> --key "<key>" --mood "<words>" --arc <arc> [--constraints "..."] [--exclude "..."] [--seed <n>] --qa
   ```
   Use `--seed` so the user can reproduce the block. Use `--blend phonk-experimental,paak-pocket,connor-price` for custom hybrids (drums from the first, bass and harmony from the second, texture and mix from the third).
2. Read the QA footer. If it says FAIL, fix the input (never hand-edit around a limit) and re-run.
3. If the user asked for something the generator cannot express (a specific instrument, a named section idea), add it via `--constraints` (goes into Style after the instruments) or edit the Lyrics cues by hand, then re-validate with `node forge.js qa <file>` after saving the block to a file.
4. Return the block verbatim. Do not summarize it, do not strip the code fences, do not add commentary above it. Add at most two sentences after it (what to try first, what to listen for).

## Output contract (what the block must contain)

1. **MODEL** - `v6` by default and why; when to try `v6-wild` (the only experimental variant; defaults Variety Off); `v6-mini` as the Free-plan fallback. Never recommend a retired model (v4.x, v5, v5.5).
2. **MODE/TOGGLES** - Custom (Advanced) mode ON, Instrumental ON, Max Mode ON for anything over ~2:00, Variety OFF, Persona/Voices NO ("Songs with Voices cannot be instrumental"), Duration Custom = planned runtime + 10 s, Song Title.
3. **STYLE FIELD** - copy-ready, under 1000 chars (target ~350-500), "instrumental" in the first three tags, descriptors ordered genre -> mood -> drums/groove -> BPM + feel -> key -> bass -> harmony -> texture -> mood -> mix. No negatives. No artist names.
4. **LYRICS FIELD** - `[Instrumental]` first, `[Short Instrumental Intro - N bars: ...]`, then section tags with bar counts and production cues, no words, no parentheses, `[Outro ...]` then `[End]`. Intro <= 8 bars, first hook/drop by 0:45, runtime 2:30-3:30, at least one contrast section.
5. **EXCLUDE STYLES FIELD** - bare nouns, ~8 terms (core vocal guards + lane guards). Never "no X".
6. **SLIDERS** - Weirdness % and Style Influence % with one-line rationale each; Variety Off with the reason; Audio Influence "not used, no audio is uploaded".
7. **VARIATIONS** - A. Safer (fewer descriptors, Weirdness -20, Style Influence +10) and B. Weirder (wild-pool descriptors, Weirdness +15 capped at 70, Style Influence -10), each a complete copy-ready Style field.
8. **POST-GEN NOTES** - what to listen for (lane-specific), when to Extend vs Replace Section vs re-roll, Remaster, Get Stems (Auto Split 12 stems / 50 credits), the Variety-Off and Max-Mode checks.

## Hand-editing rules (if you must deviate from the generator)

- Pull descriptors only from `reference/vocabulary-bank.md`; never invent an artist comparison.
- Keep bracket cues descriptive and parenthesis-free; use the tag names rated Reliable in `reference/metatag-cheatsheet.md`.
- Re-run `node forge.js qa <saved-block.md>` after any edit. A block that fails QA is not deliverable.

## Example invocations

```bash
node forge.js --lane paak-pocket --bpm 94 --key "D Dorian" --mood "sun-soaked, late-night" --arc build-release --seed 42 --qa
node forge.js --lane phonk-experimental --bpm 138 --mood "haunted, woozy" --arc stop-start --exclude "siren" --seed 7 --qa
node forge.js --blend phonk-experimental,paak-pocket,connor-price --bpm 142 --seed 3 --qa
node forge.js --config examples/config.hybrid.json --qa
```
