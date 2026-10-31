# suno-forge

A reusable toolkit that writes **Suno prompts for radio-ready hip hop instrumentals**. You pick a lane, tempo, key, mood and energy arc; it returns one copy-ready block: which Suno model to use, which toggles to set, the Style field, the Lyrics field (section tags and production cues, no words), the Exclude Styles field, slider settings, two alternate Style fields, and listening notes. Everything is text-steered: **no audio uploads, no Audio Influence, no reference clips**.

Verified against Suno as of **2026-10-31** (v6 / v6-wild / v6-mini; every earlier model was retired on 2026-09-09). See `reference/suno-field-spec.md` and re-verify with the `suno-researcher` agent when Suno ships something new.

## Lanes

| Lane | Sound (internal description only; no artist names go into prompts) | BPM (written) |
|---|---|---|
| `connor-price` | Bouncy, playful trap-pop: tight syncopated drums, bright synth plucks and bells, stop-start mutes, crisp clean mix | 130-160 half-time (default 140); `--bpm 105` for the straight bounce |
| `paak-pocket` | Live swung neo-soul funk hip hop: drums behind the beat, Rhodes and Wurlitzer, fingerstyle bass guitar, warm tape saturation | 85-100 (default 92) |
| `jid-dark` | Dense dark boom-bap/trap hybrid: pitched sample chops, odd fills and switch-ups, busy hi-hats, cinematic strings | 130-160 half-time (default 146) |
| `phonk-experimental` | Memphis-rooted original phonk: cowbell accents, distorted long-decay 808s, tape hiss, chopped and screwed sample textures, off-grid swing. Not drift phonk | 130-150 half-time (default 140) |
| `hybrid` | Phonk drums + neo-soul Rhodes/bass pocket + trap-pop brightness | 136-150 half-time (default 142) |

Custom blends: `--blend phonk-experimental,paak-pocket,connor-price` takes drums from the first lane, bass and harmony from the second, texture and mix from the third.

## The workflow

```
generate  ->  paste into Suno  ->  listen  ->  critique  ->  regenerate
```

1. **Generate.** Easiest: open **`app/index.html`** in a browser (double-click it; no server, no install). Pick a lane, press **Generate**, and copy each Suno field with its button. The same engine from the command line:
   ```bash
   node forge.js                                   # interactive
   node forge.js --lane paak-pocket --bpm 94 --key "D Dorian" --mood "sun-soaked" --arc build-release --seed 42 --qa
   node forge.js --config examples/config.hybrid.json --out my-block.md
   ```
   Or in Claude Code: `/suno-prompt paak-pocket, 94 BPM, D Dorian, sun-soaked, build-release`.

2. **Paste into Suno.** Open Create, switch to Custom (Advanced) mode, pick the model from section 1, set the toggles from section 2 (Instrumental ON, Max Mode ON, **Variety OFF**, no Voice/Persona, Duration Custom as listed), paste the three fields from sections 3-5 into Styles, Lyrics and Exclude Styles, set Weirdness and Style Influence from section 6, leave Audio Influence alone (it only appears if you upload audio, which this workflow never does). Generate.

3. **Listen** with section 8 open: does the hook land where planned, do the drums swing or sit on the grid, did anything hum, did the mix hold up after 2:30, did it stop at `[End]`? Use Replace Section for one bad region, Extend if only the back half drifted, Remaster on the keeper, Get Stems for a DAW polish.

4. **Critique.** `/suno-critique` with the prompt you used and what went wrong. It runs the QA checker, maps the symptom to a known failure mode, and returns a corrected block that changes as little as possible so the next A/B is informative.

5. **Regenerate.** Same seed plus the targeted change, or try variation A (safer) or B (weirder) from section 7. For a matrix, `/suno-batch` or:
   ```bash
   node forge.js --lane phonk-experimental --batch 6 --format table
   node forge.js --lane jid-dark --batch 4 --format json --out ab.json
   ```

## Rules the toolkit enforces

- No real artist names or "type beat / in the style of" phrasing (Suno silently rewrites such prompts).
- Instrumental by default: toggle ON, `[Instrumental]` first line, tags-only Lyrics, no parentheses, vocal terms in Exclude as bare nouns.
- Negatives only in Exclude Styles, never inline in Style.
- Verified limits (Style 1000, Lyrics 5000, Exclude 1000) with a ~500-char Style target; most important descriptors first.
- Radio structure: intro <= 8 bars, hook or drop by 0:45, 2:30-3:30, a contrast section, `[Outro]` then `[End]`.
- No audio references anywhere.

`node forge.js qa <file...>` checks any block (markdown or JSON) against all of these; `npm test` runs the engine and QA tests; `node scripts/build-examples.js` regenerates `examples/` and fails on any QA error; `node scripts/build-app.js` rebuilds `app/index.html` after any change to `lib/`.

## Repository map

```
CLAUDE.md                  project rules for Claude Code
app/index.html             one-page generator with a Generate button and Copy buttons (built from app/src by scripts/build-app.js)
forge.js                   CLI (interactive, flags, --config, --batch, --blend, qa, lanes, arcs, spec)
lib/spec.json              verified Suno facts (models, limits, sliders, Variety, Max Mode, features, banned names)
lib/lanes.json             lane vocabulary pools, section cues, sliders, exclusions, BPM and keys
lib/arrange.js             energy arcs, bars-to-seconds, radio-window planner, Lyrics renderer
lib/generate.js            Style and Exclude builders, variations, post-gen notes, block assembly
lib/qa.js                  QA checker and markdown block parser
lib/render.js              copy-block and batch-table renderers
reference/                 suno-field-spec, metatag-cheatsheet, vocabulary-bank, tempo-key-tables, exclusion-presets
research/                  dated, sourced research behind the reference docs (platform, metatags, artist vocabulary, radio conventions)
examples/                  one QA-passed block per lane plus the hybrid; JSON configs
.claude/skills/            suno-prompt, suno-critique, suno-batch
.claude/agents/            suno-researcher, groove-designer, style-writer, qa-checker
scripts/                   build-examples.js, test.js
```

## Energy arcs

`flat-bang` (hook first), `build-release` (classic lift), `slow-burn` (layers each hook), `stop-start` (mutes, drum-only break, drop), `beat-switch` (two-part). `node forge.js arcs` prints them; `reference/tempo-key-tables.md` has the section maps and a bars-to-seconds table.

## Known uncertainties

Suno publishes no character-limit table (limits are community-measured), the detailed mode is called Custom on help pages and Advanced in recent UI reports, and whether section tags are honored with the Instrumental toggle on rests on hands-on community reports rather than official documentation. Fallback if structure is ignored: toggle off, keep the tags-only Lyrics, "instrumental" in Style, vocal terms in Exclude. Details and sources: `research/01-suno-platform.md`.

Requires Node 18+. No dependencies.
