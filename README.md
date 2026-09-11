# PROJECT EXODUS

A third-person action / tower-defence hybrid: **a twelve-mission fighting retreat through one
fortified lake community**, where you choose when each position is done and how much of your kit
leaves with you.

The enemy are your neighbours. Nobody got infected — they opted in, to a neural implant tied to
stimulus payments, and the AI administering it repurposed them. Your weapons carry malware, not
bullets; they decrypt the implant, and it takes the host with it. Half the game is daylight.

You are not defending a position. You are buying road, because the truck at your back is a four-door
with your family in it, and the thing hunting you scans on a cycle.

> Repo, namespaces and branch are still `Cipher.*` — the game was called *Cipher: Dead Turf* until
> ADR-006. Renaming code is a large cosmetic diff with real risk, so only the product name changed.

## Start here

**`CLAUDE.md` is the studio's working memory** and the fastest route into the whole project: current
state, every hard-won gotcha, and the resume point. Read it first.

Then the decisions, in this order — they supersede large parts of the founding docs:

| ADR | What it settles |
|---|---|
| [003 — The Cascade](docs/decisions/ADR-003-premise-the-cascade.md) | The premise. Read before writing any fiction, enemy or level theme. |
| [004 — The Lake](docs/decisions/ADR-004-setting-the-lake.md) | A gated lake community in rural Virginia, fictionalised. |
| [005 — The Scan Cycle](docs/decisions/ADR-005-the-scan-cycle.md) | The core loop: call your own last wave, then pack up against a clock. |
| [008 — Weapons Attack the Implant](docs/decisions/ADR-008-weapons-attack-the-implant.md) | Signal weapons; the implant is decrypted progressively. |
| [009 — The Truck Is the Family](docs/decisions/ADR-009-the-truck-is-the-family.md) | Why this is a retreat at all. |
| [001](docs/decisions/ADR-001-engine-unity6.md) · [002](docs/decisions/ADR-002-camera-model.md) · [006](docs/decisions/ADR-006-title-project-exodus.md) · [007](docs/decisions/ADR-007-crowd-rendering-and-art-lane.md) | Engine, camera, title, crowd rendering + art lane. |

[Open Decisions](docs/decisions/OPEN-DECISIONS.md) is the live register of calls not yet made.

## Headline decisions

- **Platforms & input:** Android (perf floor) + PC/Steam. **Xbox controller is the design-centre
  control scheme on both** — not an afterthought, and prompts must name the device in the player's
  hands.
- **Engine:** Unity 6 LTS + URP. The swarm is **Vertex Animation Textures + `DrawMeshInstanced`,
  NOT DOTS** (ADR-007) — adopting Entities Graphics would mean converting `sim/` to ECS and losing
  determinism.
- **The sim core is engine-free and deterministic.** Zero `UnityEngine` references in `sim/`, ever.
  Fixed iteration order, seeded RNG only, guarded by a state-hash test.
- **Quality over quantity.** The enemy are people and people have to be individually legible, so a
  position is forty to a hundred and fifty bodies rather than a thousand.
- **Discipline:** every push compiles and passes CI; releases are tagged CI artifacts only; big calls
  become ADRs; `CLAUDE.md` is the ratchet and is updated the same day.

## Repo layout

- `sim/` — engine-agnostic simulation core (flow fields, spatial hash, swarm stepping) + tests + bench
- `game/` — the Unity 6 project; mounts `sim/` as a local package
- `docs/` — founding docs 01–05, `decisions/` (ADRs), `design/`, `retros/`, `feedback/` (owner
  playtests, verbatim — the ground truth about what actually goes wrong when a human plays it)

## Build & test

```
dotnet build sim/src/Cipher.Sim -c Release
dotnet test  sim/tests/Cipher.Sim.Tests -c Release
dotnet run --project sim/tools/Cipher.Sim.Bench -c Release -- --smoke
```

Unity EditMode tests and the player build are in `game/README.md`. **Run all three of the above
before pushing anything that touches `sim/`** — the EditMode suite references the sim's *source*,
not its test project, so it cannot catch a broken sim test.

## Founding documents

Written on day one. Still useful for principles; **superseded on specifics by the ADRs above**.

| Doc | Contents |
|---|---|
| [01 — Tech Stack Audit](docs/01-TECH-STACK-AUDIT.md) | AI/generative tooling reality check + architecture principles (engine sections superseded by ADR-001) |
| [02 — Flagship Pitch](docs/02-FLAGSHIP-PITCH.md) | High concept and loop, rewritten against ADR-003 through 006 |
| [03 — Day-1 Execution Blueprint](docs/03-DAY1-EXECUTION-BLUEPRINT.md) | The MVP milestones and eight feasibility traps |
| [04 — Operating Model & Wish Lists](docs/04-OPERATING-MODEL-AND-WISHLIST.md) | How the human+AI studio runs |
| [05 — Engineering Standards](docs/05-ENGINEERING-STANDARDS.md) | SOLID at the seams, testing gates, release discipline, the ratchet |
