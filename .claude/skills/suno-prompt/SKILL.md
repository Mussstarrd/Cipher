---
name: suno-prompt
description: Generate a copy-ready Suno prompt block for a radio-ready hip hop INSTRUMENTAL. Use when the user asks for a Suno prompt, beat prompt, Styles/Lyrics fields, or an instrumental in one of the lanes (connor-price, paak-pocket, jid-dark, phonk-experimental, hybrid, or "any"), or describes a hip hop beat they want Suno to make. Inputs: lane, BPM, key, mood, mode (loop/straight/switch), must-haves.
---

# suno-prompt

Produce ONE copy-ready block with eight labeled sections. The engine does the writing (physical sound descriptions with handles, head tags front-loaded, a guard over every word); your job is to map the request onto its inputs, run it, and return the block untouched.

## Inputs

| Input | Values | Default |
|---|---|---|
| lane | `any` / `connor-price` / `paak-pocket` / `jid-dark` / `phonk-experimental` / `hybrid` | `any` (weighted random) unless the user describes a sound |
| BPM | number; see `node forge.js lanes` for ranges | lane pick |
| key | `F minor`, `D dorian`, `Eb major` | lane colour (avoids recent keys) |
| mood | comma words (guarded: swing/vocal/EDM words are dropped and reported) | none |
| mode | `loop` / `straight` / `switch` | the layout decides |
| layout | one of `node forge.js layouts` | weighted by lane |
| constraints | must-have descriptors, comma (guarded; go into the head tags) | none |
| exclude | extra Exclude Styles terms | none |
| hint | `fuzzlament` / `machineglitch` / `staxtwitch` / `hits` / `none` (the strange ingredient) | random, about half of rolls |
| wild | pick v6-wild for this block | off |
| seed | number or string | random |

Mapping free text: "a hint of a mournful fuzz guitar" -> `--hint fuzzlament`; "industrial, glitchy, mechanical" -> `--hint machineglitch`; "urgent horn stabs, Stax-style nerves" -> `--hint staxtwitch`; "a piano/string hit pattern" -> `--hint hits`. "bouncy, bright, playful, claps" -> connor-price. "Live drums, keys, pocket, bass guitar, warm" -> paak-pocket. "Dark, dense, chops, strings, switch-up" -> jid-dark. "Memphis, cowbell, cassette, slowed, screwed" -> phonk-experimental. Two or more -> hybrid. Nothing specific -> any.

## Procedure

1. Run the generator (it is the source of truth):
   ```bash
   node forge.js --lane <lane> [--bpm n] [--key "..."] [--mood "..."] [--mode loop|straight|switch] [--hint hits|fuzzlament|machineglitch|staxtwitch|none] [--constraints "..."] [--exclude "..."] [--seed n] [--wild] --qa
   ```
   Always pass `--seed` (or report the seed the block shows) so the roll can be reproduced.
2. Read the GENERATOR NOTES and QA footer. A dropped mood word is normal (the guard refused it); explain in one line which word was dropped and why. If QA says FAIL, change the input, never hand-edit around a limit.
3. Return the block verbatim: no summary above it, fences intact. Add at most two sentences after it: what to try first (v6 vs v6-wild), what to listen for.

## What the block must contain (the renderer does this; do not reformat)

1. **MODEL**: v6 by default and why; when to run v6-wild (and its slider ranges); v6-mini as the Free fallback. Never a retired model.
2. **MODE/TOGGLES**: Custom (Advanced) ON, Instrumental ON, Max Mode ON, Variety Off, Voice NO, Duration Custom (estimate + buffer), title.
3. **STYLE FIELD**: head tags (anchor, BPM, key, instrumental, mood, syncopation, twitch, slur, mode tag, low lead, lead handle, mix tag) then `lead:` `bass:` `drums:` `counter:`; under 1000 chars; no names, no negatives, no swing/fill/EDM words.
4. **LYRICS FIELD**: `[Instrumental]` first, layout tags with the same handles, `[End]` last, no parentheses, first full beat by 0:45.
5. **EXCLUDE STYLES FIELD**: the lane list (vocal guards + transition guards + lane drift guards) plus extras; bare nouns.
6. **SLIDERS**: Weirdness and Style Influence with rationale; Variety Off; Audio Influence "not used, nothing is uploaded".
7. **VARIATIONS**: A. Safer (neutral anchor, no twitch/slur tags, v6) and B. Weirder (one wild tag, v6-wild at 65-80 / 55-70).
8. **POST-GEN NOTES**: structure check, lane listening notes, no-fill rule, re-roll rule, Extend/Replace/Remaster/Stems, the Variety and Max Mode checks.

## Hand-editing

Avoid it. If the user insists on a specific phrase, pass it through `--constraints` (it is guarded) rather than typing it into the field. If you must edit a field, save the block and run `node forge.js qa <file>`; a FAIL is not deliverable. Never add an artist name, a comparison, a negation, or any of: fill, roll, riser, sweep, build, drop, transition, enters, comes in, swing, swung, shuffle, waltz, cinematic, Rhodes, sax, choir, chant.

## Examples

```bash
node forge.js --lane any --seed 42 --qa
node forge.js --lane paak-pocket --bpm 94 --key "D dorian" --mood "sun-soaked, late-night" --mode straight --seed 7 --qa
node forge.js --lane phonk-experimental --mode loop --mood "dark" --seed 11 --qa
node forge.js --lane jid-dark --mode switch --constraints "the sliced strings up front" --wild --seed 3 --qa
```
