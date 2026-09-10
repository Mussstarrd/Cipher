# Cipher

Studio flagship project. Working title: **CIPHER: DEAD TURF** — a third-person action / tower-defense hybrid (hardcore mazing + Bloons-depth upgrade trees + Dungeon Defenders-style hero combat) set in a quarantined city ruled by syndicates and overrun by mutated swarms.

## Founding documents

| Doc | Contents |
|---|---|
| [01 — Tech Stack Audit](docs/01-TECH-STACK-AUDIT.md) | Engine decision (UE5 + Mass Entity + VAT horde rendering), AI/generative tooling reality check, infrastructure & backend architecture |
| [02 — Flagship Pitch](docs/02-FLAGSHIP-PITCH.md) | High concept, core gameplay loop, progression & dopamine systems, enemy design and threat scaling |
| [03 — Day-1 Execution Blueprint](docs/03-DAY1-EXECUTION-BLUEPRINT.md) | The three 30-day MVP milestones and the eight technical feasibility traps with decisions installed now |

## Headline decisions

- **Engine:** Unreal Engine 5 (5.5+). Mass Entity for the swarm sim, vertex-animation-texture instancing for horde rendering, Niagara GPU VFX, Nanite for environments only, Lumen within a hard frame budget.
- **AI tooling:** agentic coding assistants and generative 3D (Tripo/Meshy/Rodin-class) adopted for prototyping and clutter; text-to-game generators rejected; zero LLM inference at runtime.
- **Infra:** GitHub Actions + self-hosted Hetzner build runners, nightly cook + performance gate, EOS listen-server co-op first, cheap dedicated servers later.
- **First proof:** 1,000 enemies at 60fps in graybox within 10 days, or the plan changes.
