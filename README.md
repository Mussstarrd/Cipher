# Cipher

Studio flagship project. Working title: **CIPHER: DEAD TURF** — a third-person action / tower-defense hybrid (hardcore mazing + Bloons-depth upgrade trees + Dungeon Defenders-style hero combat) set in a quarantined city ruled by syndicates and overrun by mutated swarms.

## Founding documents

| Doc | Contents |
|---|---|
| [01 — Tech Stack Audit](docs/01-TECH-STACK-AUDIT.md) | Engine decision (UE5 + Mass Entity + VAT horde rendering), AI/generative tooling reality check, infrastructure & backend architecture |
| [02 — Flagship Pitch](docs/02-FLAGSHIP-PITCH.md) | High concept, core gameplay loop, progression & dopamine systems, enemy design and threat scaling |
| [03 — Day-1 Execution Blueprint](docs/03-DAY1-EXECUTION-BLUEPRINT.md) | The three 30-day MVP milestones and the eight technical feasibility traps with decisions installed now |
| [04 — Operating Model & Wish Lists](docs/04-OPERATING-MODEL-AND-WISHLIST.md) | How the human+AI studio runs; tiered subscription/hardware wish lists |
| [05 — Engineering Standards](docs/05-ENGINEERING-STANDARDS.md) | SOLID-at-the-seams architecture, testing gates, release discipline, the self-improving ratchet |
| [ADR-001 — Engine: Unity 6](docs/decisions/ADR-001-engine-unity6.md) | Reverses the UE5 call: Android + Xbox controller + AI-driven pipeline → Unity 6 / URP / ECS |

## Headline decisions

- **Platforms & input:** Android (primary perf floor) + PC/Steam (high-end), Xbox controller as the design-center control scheme on both.
- **Engine:** Unity 6 LTS + URP; ECS/DOTS for the swarm; deterministic engine-free sim core in `sim/` (pure C#, tested headless with `dotnet test`).
- **AI tooling:** agentic coding assistants and generative 3D (Tripo/Meshy/Rodin-class) adopted for prototyping and clutter; text-to-game generators rejected; zero LLM inference at runtime.
- **Discipline:** every push compiles + tests in CI (`.github/workflows/ci.yml`); releases are tagged CI artifacts only; decisions live in ADRs; `CLAUDE.md` is the studio's working memory.
- **First proof:** the flood — max agent density at 60fps in graybox, measured, before anything else matters.

## Repo layout

- `sim/` — engine-agnostic simulation core (flow fields, spatial hash, swarm stepping) + tests + benchmark
- `game/` — Unity 6 project (coming next)
- `docs/` — founding docs, `decisions/` (ADRs), `retros/`, `feedback/`
