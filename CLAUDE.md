# CIPHER — Studio Working Memory

Read this first, every session. Keep it current: surprising facts, new conventions, and gotchas that cost >30 min go here the same day (see docs/05, "the ratchet").

## What this project is

**CIPHER: DEAD TURF** — third-person action / tower-defense hybrid (hardcore mazing + Bloons-depth upgrade trees + hero combat), syndicate-kingpin-vs-mutated-swarms fantasy. Full pitch: `docs/02-FLAGSHIP-PITCH.md`.

- **Platforms:** Android (primary perf floor, 300–500 agents on screen) + PC/Steam (high-end, 1,000+ agents).
- **Input:** Xbox controller is the design-center control scheme on both platforms. Touch is a fallback overlay.
- **Engine:** Unity 6 LTS + URP, ECS/DOTS for the swarm. This REVERSED an earlier UE5 decision — rationale in `docs/decisions/ADR-001-engine-unity6.md`. Do not re-litigate without its reversal conditions triggering.

## Operating model

Owner (Jeff) = vision, taste, veto; plays builds and reacts. Claude = the engineering department with broad creative control inside the pitch's parameters. Big/irreversible calls → ADR in `docs/decisions/`, surfaced to owner, never buried. Status updates at every push.

## Repo layout

- `docs/` — founding docs 01–05 (audit, pitch, blueprint, operating model/wishlist, engineering standards), `decisions/` (ADRs), `retros/`, `feedback/` (owner playtest notes, verbatim).
- `sim/` — **pure C# deterministic simulation core. Zero UnityEngine references, ever.** Flow fields, spatial hash, agent stepping. `dotnet test` runs it headless.
- `game/` — (future) Unity 6 project; consumes `sim/` via asmdef; Force Text serialization.

## Hard rules

1. Every push must compile and pass tests in CI (`.github/workflows/ci.yml`). Red CI = fix before anything else.
2. New logic lands with its tests in the same commit. A bug fixed twice becomes a test.
3. Sim core stays deterministic: fixed iteration order, seeded RNG only, state-hash test guards it.
4. The build-mode pathing preview and the live sim must share the same `IFlowField` implementation ("preview never lies" — Trap 5 in docs/03).
5. SOLID at module boundaries; structure-of-arrays/batch style inside the swarm hot path (docs/05 §1).
6. Releases are tagged CI artifacts only — nothing hand-built ships.
7. Development branch: `claude/game-studio-audit-pitch-nxaqhx` (current phase). Never push elsewhere without permission.

## Build & test

```
dotnet build sim/src/Cipher.Sim -c Release
dotnet test  sim/tests/Cipher.Sim.Tests -c Release
dotnet run --project sim/tools/Cipher.Sim.Bench -c Release -- --smoke
```

(No .NET SDK in some remote containers — CI on GitHub Actions is the source of truth; verify runs after pushing.)

## Current state / next steps

- Founding docs 01–05 + ADR-001 committed.
- Sim core v0 committed: GridMap, FlowField (Dijkstra + direction field), SpatialHash, AgentWorld (SoA, separation steering, radial damage), tests + bench.
- **Next:** Unity 6 project scaffold in `game/` (owner's laptop or CI), controller-first input map, sim↔Unity adapter rendering agents as instanced quads → Milestone 1 "The Flood" per docs/03.
