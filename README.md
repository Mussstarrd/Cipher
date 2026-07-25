# CIPHER

A prompt-engineering engine for AI music generators — Suno first. CIPHER produces hit-oriented rap prompt packages with a specific sonic identity (emo/experimental phonk core, dense-lyricism rap lineage, T.I.-swagger baseline), not generic rule-compliant prompts.

Every Suno behavior encoded in the engine traces to [`docs/suno-research.md`](docs/suno-research.md) — a sourced, confidence-labeled research doc on Suno's real control surfaces (style field, lyrics bracket tags, exclude field, weirdness/style-influence sliders), current as of July 2026. Don't add Suno facts to the code without adding them to the research doc first.

## What it does

Given a dominant artist DNA (plus optional accent DNAs and bans), the engine emits a ready-to-paste Suno package:

- **Style field text** — comma-phrase format, genre front-loaded, groove always present, artist names never emitted (Suno blocks them; the DNA library translates artists into subgenre + era + region + production descriptors instead)
- **Exclude field text** — capped at 5 terms, and every ban is paired with a competing positive injected into the style field (the exclude field is soft and leaks; crowding out wins)
- **Lyrics scaffold** — hook-first hit structure (`[Chorus]` first, `[Outro]`→`[End]` close, ~2:30–2:50 line budgets), beat-switch variant via `[Breakdown]`/`[Build]`/`[Drop]`, ad-lib parentheticals, flow guidance from the dominant DNA
- **Slider recommendations** — per build type (faithful / balanced / fusion)

## Usage

```bash
npm install
npm run cipher -- --list-artists
npm run cipher -- --dominant xxxtentacion --accent travis-scott:0.4
npm run cipher -- --dominant ti --accent weezer:0.35 --template hook-first-beat-switch
npm run cipher -- --dominant juice-wrld --ban saxophone --ban edm
npm test
```

## Project phases

1. **Research** (done) — verify Suno prompting mechanics empirically → `docs/suno-research.md`
2. **Core engine** (this code) — deterministic, testable TypeScript library + CLI
3. **Validate & iterate** — run generated prompts through Suno for real; tune the descriptor library against actual audio. Only after that: app shell / LLM layer.

## Layout

```
docs/suno-research.md   Phase 1 research — the source of truth for engine constants
src/types.ts            Data models (style slots, DNA, profiles, package)
src/profiles.ts         Per-model field budgets (v4.5-all / v5 / v5.5)
src/artists.ts          Artist-DNA descriptor library (the heart of the project)
src/grooves.ts          Three rhythmic vocabularies (anti-homogenization)
src/fusion.ts           Dominant + accent weighting
src/exclusion.ts        Two-tier kill list + negative-to-positive inversion
src/style.ts            Style-field assembly with priority-aware trimming
src/structure.ts        Hit-structure templates → lyrics scaffold
src/sliders.ts          Slider presets per build type
src/engine.ts           Orchestration → SunoPackage
src/cli.ts              Thin CLI
```
