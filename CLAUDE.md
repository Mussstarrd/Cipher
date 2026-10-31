# suno-forge

Suno prompt generator for radio-ready hip hop **instrumentals**. A reusable toolkit: lane data, an arrangement engine, a QA checker, a CLI, Claude Code skills and sub-agents, and verified reference docs. Not a one-off script.

## Project rules (non-negotiable)

1. **Never use real artist names in generated prompts.** Lane names (`connor-price`, `paak-pocket`, `jid-dark`, `phonk-experimental`, `hybrid`) are internal labels only. Suno silently rewrites any prompt containing an artist name ("We don't reference artists directly...") and you lose control of the whole Style field. Translate every sound into genre / drum / groove / instrument / texture / mood / mix descriptors from `reference/vocabulary-bank.md`. `lib/spec.json` holds the banned-name list the QA checker enforces; "type beat", "in the style of", "sounds like" are banned phrases too.
2. **Always output copy-ready blocks.** Every generated prompt is one block with eight labeled sections (MODEL, MODE/TOGGLES, STYLE FIELD, LYRICS FIELD, EXCLUDE STYLES FIELD, SLIDERS, VARIATIONS, POST-GEN NOTES). The three Suno fields sit in their own fenced code blocks so they paste clean.
3. **Respect verified character limits.** Style 1000, Lyrics 5000, Exclude Styles 1000, title 80 (see `reference/suno-field-spec.md`; limits are community-measured on v6 since Suno publishes none). Keep the Style field near the ~500-character soft target. **Front-load**: Suno weights early tokens more heavily, so order descriptors genre -> mood -> drums/groove -> tempo/key -> bass -> harmony/instruments -> texture -> mix/master.
4. **Default to instrumental.** Custom (Advanced) mode, Instrumental toggle ON, Variety OFF, Lyrics field holds bracketed section/production tags only (zero sung words, zero parentheses: Suno sings parenthesized text), leading `[Instrumental]` line, `[End]` last. Exclude Styles always lists vocal terms as bare nouns.
5. **Negatives go in the Exclude Styles field, never inline in the Style field.** Inline negatives ("no vocals") are ignored on v6 and can pull the unwanted thing in. Exclude terms are bare nouns ("vocals"), never negated ("no vocals"). Default list is ~8 terms (core vocal guards + up to 4 lane guards); trim to 4 if Suno ignores it.
6. **No audio references, ever.** Never suggest uploading clips, Audio Influence, Cover, Add Vocals/Add Instrumental, or any reference track. All steering comes from the Style, Lyrics and Exclude fields plus the Weirdness, Style Influence and Variety controls. The QA checker rejects any copy-field mention of these and any prose that recommends them; the only allowed form is a statement that Audio Influence is not used.
7. **Radio structure.** Runtime 2:30-3:30, intro <= 8 bars, first hook/drop by 0:45 (hook-first arcs land it by ~0:15), at least one contrast section (break / bridge / drop / beat switch), hard stop via `[Outro]` + `[End]`. Half-time lanes (trap, trap-pop, phonk, written 130-160 BPM) use 16-written-bar hooks and 16-bar verses.
8. **Current Suno facts only.** As of 2026-09-09 the only models are **v6, v6-wild, v6-mini**; everything earlier is retired. Max Mode ON for anything over ~2:00. If a Suno fact might have changed, run the `suno-researcher` agent before trusting `lib/spec.json`, and update `spec.json` + `reference/suno-field-spec.md` together.

## Layout

| Path | What |
|---|---|
| `forge.js` | CLI: interactive, flags, `--config file.json`, `--batch N`, `--blend a,b,c`, `qa <files>`, `lanes`, `arcs`, `spec` |
| `lib/spec.json` | Verified Suno facts: models, limits, sliders, Variety, Max Mode, features, banned names, exclude presets |
| `lib/lanes.json` | Per-lane descriptor pools (priority-ordered), cues per section, sliders, exclusions, BPM/keys, listen-for notes |
| `lib/arrange.js` | Energy arcs, bars-to-seconds, radio-window planner, Lyrics renderer |
| `lib/generate.js` | Style/Exclude builders, variations, post-gen notes, block assembly (seeded, reproducible) |
| `lib/qa.js` | QA checker (limits, names, audio refs, instrumental, negatives, structure, sliders, model) + markdown block parser |
| `lib/render.js` | Copy block and batch table renderers |
| `reference/` | Field spec, metatag cheat sheet, vocabulary bank, tempo/key tables, exclusion presets |
| `research/` | Dated, sourced research the reference docs are built from |
| `examples/` | One generated block per lane plus the hybrid, all QA-passed, plus JSON configs |
| `.claude/skills/` | `suno-prompt`, `suno-critique`, `suno-batch` |
| `.claude/agents/` | `suno-researcher`, `groove-designer`, `style-writer`, `qa-checker` |
| `scripts/` | `build-examples.js`, `test.js` |

## Workflow

```
node forge.js --lane <lane> --bpm <n> --mood "<words>" --arc <arc> [--seed n] [--qa]
node forge.js qa examples/*.md          # validate blocks
node scripts/build-examples.js          # regenerate examples/ (deterministic seeds)
npm test                                # engine + QA tests
```

Generate -> paste the three fields into Suno (Custom mode, Instrumental ON, Variety OFF, Max Mode ON, sliders as listed) -> listen -> `/suno-critique` with what went wrong -> regenerate. See `README.md`.

## Editing conventions

- Descriptor pools in `lib/lanes.json` are priority-ordered; the first item is always selected, later items are sampled with a front bias. Put the most characteristic phrase first.
- Never add a vocal-implying descriptor to a pool ("soulful", "choir", "chant", "anthemic", "gospel", "vocal chops", "R&B", "talkbox", "ad-lib"): they cause humming leaks.
- Never add a word that community testing reports as destabilizing ("complex drums", "aggressive", "crescendo", "ethereal", "lush", "shimmering", "realistic") unless the lane needs it; prefer "slight"/"subtle" modifiers.
- Cues in `lanes.json` are plain descriptive phrases inside the bracket; no parentheses, no sung words, no mix-operation tags as standalone brackets (`[filter sweep]` alone is unreliable).
- Any change to `lib/spec.json` must cite a source in `research/01-suno-platform.md` and be mirrored in `reference/suno-field-spec.md`.
- Run `npm test` and `node forge.js qa examples/*.md` before committing. Regenerate examples if `lanes.json` or `arrange.js` changed.
