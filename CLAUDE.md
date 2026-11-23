# suno-forge

Suno prompt generator for radio-ready hip hop **instrumentals**. A reusable toolkit: a sound-design grammar with a guard, five lanes, lyrics layouts, a seeded engine, a QA checker, a one-button page, a CLI, Claude Code skills and sub-agents, and verified reference docs. Not a one-off script. Read `reference/grammar-and-guard.md` before touching vocabulary.

## Project rules (non-negotiable)

1. **Never use real artist names in generated prompts.** Lane ids (`connor-price`, `paak-pocket`, `jid-dark`, `phonk-experimental`, `hybrid`) are internal labels; the records behind them are named only in README and research. Suno silently rewrites a prompt with a name and you lose the whole field. `lib/guard.js` holds the names list and bans "type beat", "in the style of" and comparisons to a capitalised name.
2. **Always output copy-ready blocks.** One block, eight labeled sections (MODEL, MODE/TOGGLES, STYLE FIELD, LYRICS FIELD, EXCLUDE STYLES FIELD, SLIDERS, VARIATIONS, POST-GEN NOTES); the three Suno fields in their own fenced code blocks. The page shows the fields in Suno's order (Lyrics, Styles, Exclude Styles, sliders) with a Copy button each.
3. **Respect verified limits and front-load.** Style 1000, Lyrics 5000, Exclude Styles 1000, title 80 (community-measured on v6; Suno publishes none). The Style head tags (~260 chars) carry the identity and are never cut; the lead and drums lines always survive the cut order.
4. **Default to instrumental.** Custom (Advanced) mode, Instrumental ON, Variety OFF, Max Mode ON, `[Instrumental]` first line, `[End]` last, bracket tags only, zero parentheses (Suno sings them), "instrumental" in the head tags, Exclude Styles starts with vocals and lists humming.
5. **Negatives go in the Exclude Styles field, never inline.** "no X" plants X. Say the positive ("dry", "hard cuts only, straight in on beat 1") and put the ban in Exclude as bare nouns.
6. **No audio references, ever.** No uploads, Audio Influence, Cover, Add Vocals/Add Instrumental or reference clips anywhere. All steering is the Style, Lyrics and Exclude fields plus Weirdness, Style Influence and Variety. The QA checker rejects any copy-field mention and any prose that recommends them; the only allowed form is a statement that Audio Influence is not used.
7. **Radio structure.** First full beat (Hook or Loop) by 0:45, layouts run 2:30-3:30 at lane tempos, `[End]` last, Duration Custom set to the estimate plus a buffer. Half-time lanes (written 120+ BPM) double hook bars.
8. **No transitions, no fillers, no swing words.** Fill, roll, riser, sweep, build, drop, transition, "comes in", "enters", "bar 4", waltz, shuffle, swing, swung, time signatures never appear in a prompt (the guard enforces it over every grammar string). Groove is said physically: "the 808 stumbles into three on beat 3", "the snare a hair late". The drums line always carries an anti-transition phrase. "Every N bars" is capped at one per roll, zero in loop mode.
9. **Every sound is a physical description with a handle**, never a label; see `reference/grammar-and-guard.md`. Vocal-implying, EDM, film-score and elevator-jazz words are banned (voice, cinematic, strings-family names, Rhodes, sax, smooth, lo-fi outside the phonk lane); each lane ALLOWs the few words it needs and DENYs the words that pull it off course.
10. **Current Suno facts only.** As of 2026-09-09 the only models are **v6, v6-wild, v6-mini**. v6 at Weirdness 40-55 / Style Influence 75-85; v6-wild at 65-80 / 55-70 for the weird lane. If a Suno fact might have changed, run the `suno-researcher` agent, then update `lib/spec.json` and `reference/suno-field-spec.md` together.

## Layout

| Path | What |
|---|---|
| `app/index.html` | One-button page (Generate, Copy per field in Suno's order, pinned paste bar, memory). Built; source is `app/src/index.template.html` + `scripts/build-app.js` |
| `forge.js` | CLI: interactive, flags, `--lane any`, `--mode`, `--layout`, `--config`, `--batch`, `qa <files>`, `lanes`, `layouts`, `spec` |
| `lib/guard.js` | Word-class rules, names list, EVERY cap; `check(text, allow, deny)` |
| `lib/grammar.js` | Shared slots: lead/bass/twitch/groove/switch/colour/counter roles/head tags/anti-transition/guest families |
| `lib/lanes.js` | The five lanes: anchors, families (physical sources with handles), drums, bass, tempo, key colour, layout weights, sliders, exclude, allow/deny |
| `lib/layouts.js` | Lyrics layouts with placeholders, limits, fallbacks, nominal bars, runtime estimate |
| `lib/theory.js` | Keys and chord loops (minor-heavy; Dorian/soul loops for the live lane) |
| `lib/engine.js` | Seeded roll: builds lead/counter/bass/drums/switch/rhythm, Style field with cut order, lyrics fill, variations, fingerprint, warnings |
| `lib/qa.js` | QA checker (limits, guard, audio refs, instrumental, structure, EVERY cap, switch axis, sliders, models) + markdown parser |
| `lib/render.js`, `lib/arrange.js` | Block/batch renderers; bars-to-seconds helpers |
| `lib/spec.json` | Verified Suno facts (models, limits, controls, features, audio-ref terms) |
| `reference/` | grammar-and-guard, suno-field-spec, metatag-cheatsheet, vocabulary-bank, tempo-key-tables, exclusion-presets |
| `research/` | Dated, sourced research; `research/handoff/` is the previous generator's pass-down and source (the render-tested rules) |
| `examples/` | One QA-passed block per lane plus the hybrid; JSON configs |
| `.claude/skills/` | `suno-prompt`, `suno-critique`, `suno-batch` |
| `.claude/agents/` | `suno-researcher`, `groove-designer`, `style-writer`, `qa-checker` |
| `scripts/` | `build-examples.js`, `build-app.js`, `audit.js`, `test.js` |

## Workflow

```
node forge.js --lane any --seed 42 --qa         # one roll, QA appended
node forge.js qa examples/*.md                  # validate blocks
node scripts/build-examples.js                  # regenerate examples/ (deterministic seeds)
node scripts/build-app.js                       # rebuild app/index.html after any lib/ change
node scripts/audit.js                           # phrase shares, length histograms, filler scan
npm test                                        # guard walk + thousands of seeds
```

Generate -> paste into Suno (Lyrics, Styles, Exclude Styles, sliders) -> listen -> `/suno-critique` -> regenerate. See `README.md`.

## Editing conventions

- Add vocabulary as physical descriptions with handles, in the right family, with that family's processing words. Never add a one-phrase label.
- Every new string must pass `npm test` (the guard walk). If a word is needed by one lane only, add it to that lane's ALLOW; if a word hurts one lane only, add it to that lane's DENY.
- Never add a phrase with fill/roll/riser/sweep/build/drop/transition/enters/comes in/bar 4; never waltz/shuffle/swing/swung; never a negation; never a vocal-implying word.
- When the ear finds a problem in a rendered take, turn it into a guard rule, a clash rule or a vocabulary change, and add a test.
- Any change to `lib/spec.json` must cite a source in `research/01-suno-platform.md` and be mirrored in `reference/suno-field-spec.md`.
- Run `npm test` and `node forge.js qa examples/*.md` before committing. Regenerate examples and the app if anything in `lib/` changed.
