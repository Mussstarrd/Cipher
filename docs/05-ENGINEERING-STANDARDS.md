# Part 5 — Engineering Standards

*Binding for all code in this repo, human- or AI-authored. Enforced by CI, not by memory.*

## 1. Architecture: SOLID at the seams, data-oriented in the core

- **Ports & adapters.** The deterministic game simulation (`sim/`) is pure C# with **zero UnityEngine references** — enforced by target framework and, later, Unity asmdef boundaries. Unity is an adapter: rendering, input, audio subscribe to the sim; they never reach inside it.
- **SOLID where variation lives:** interfaces at module boundaries (`IFlowField` so the build-mode preview and the live sim share one implementation — the "preview never lies" covenant), constructor injection everywhere, no singletons/service locators, no static mutable state.
- **Data-oriented where performance lives:** the swarm hot path uses structure-of-arrays and batch operations, not per-agent objects. Textbook per-entity OOP in a 1,000-agent loop is cache poison; SOLID governs the *boundaries around* the hot loop, never bloats its inside. This tension is resolved explicitly, per module, in code review.
- **Determinism is a feature:** fixed-tick simulation, fixed iteration order, seeded RNG only. Guarded by a state-hash regression test. This is what makes replays, netcode snapshots, and headless balance sims cheap later.

## 2. Testing — the gate, not the garnish

| Layer | Runs | Gate |
|---|---|---|
| Sim unit tests (`dotnet test`, no Unity needed) | Every push, <1 min | Merge-blocking |
| Sim benchmark smoke (`Cipher.Sim.Bench --smoke`) | Every push | Merge-blocking (must complete) |
| Sim perf thresholds (steps/sec floors for 1k/5k agents) | Nightly on fixed hardware | Regression alarm |
| Unity EditMode/PlayMode tests (game-ci) | Every push once Unity project exists | Merge-blocking |
| Android device smoke (boot + scripted wave via adb) | Nightly once first APK exists | Regression alarm |
| Preview-vs-sim divergence test | Every push once build mode exists | Merge-blocking |

Rules: new logic lands **with** its tests in the same commit. A bug fixed twice becomes a test. No test may depend on wall-clock time, network, or execution order.

## 3. Release discipline

- `main` is protected: PRs only, CI green required. Feature work in branches.
- **A "release" is a tagged CI artifact** (APK + PC build) produced by the pipeline from a green commit — never a file built on somebody's machine and uploaded by hand.
- Every release tag carries its changelog and the CI run that birthed it. Rollback = re-point to the previous tag.
- Version scheme: `0.MILESTONE.PATCH` until content-complete, then semver.

## 4. The ratchet (how the pipeline learns)

1. Any failure class that escapes to a human twice → a permanent automated guard (test, analyzer rule, CI check). The guard ships in the same PR as the second fix.
2. Any manual step performed three times → scripted.
3. Any surprising fact that cost >30 min to learn → written into `CLAUDE.md` or an ADR the same day.
4. Milestone retros in `docs/retros/` end with concrete automation actions, which become issues immediately.

## 5. Code conventions (C#)

- `TreatWarningsAsErrors` + nullable reference types enabled everywhere.
- Public APIs documented with a sentence of *why*, not a restatement of the signature.
- Naming: domain vocabulary from the pitch docs (Hold, Wave, Emplacement, Archetype) — code and design speak one language.
- No dead code, no commented-out code, no TODOs without an issue number.
