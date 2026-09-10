# ADR-001 — Engine: Unity 6 (reversing the UE5 recommendation)

**Status:** Accepted · **Date:** 2026-09-10 · **Owner sign-off:** pending (flagged in status update; reversible cheaply *now*, expensively later)

## Context — what changed

Doc 01 recommended UE5. Two new product constraints arrived from the owner, plus one operational reality:

1. **The game must run on Android.**
2. **Primary input is an Xbox controller** (PC and Android alike).
3. **Development is AI-driven** (Claude as the engineering department, human as taste/direction), so the toolchain must be automatable headless from a CLI, with diffable assets.

## Why the constraints break the UE5 case

The UE5 recommendation rested on three pillars: **Nanite, Lumen, and Niagara GPU sims**. On Android, Nanite is unsupported, Lumen is impractical for our perf envelope, and GPU-sim VFX budgets shrink drastically. The pillars don't survive contact with the primary new platform — we'd be paying UE5's costs (heavy builds, binary `.uasset` content, editor-centric authoring) without its rewards.

Meanwhile the AI-driven pipeline inverts the tooling comparison:

| Requirement | Unity 6 | UE5 |
|---|---|---|
| Mobile track record & renderer (URP) | Excellent, battle-tested | Workable, second-class |
| Massive agent counts on mobile CPUs | **ECS/DOTS + Burst — best-in-class** | Mass Entity (desktop-oriented) |
| Assets diffable/authorable by AI | **Text YAML (force-text) — scenes, prefabs, materials are editable text** | Binary uassets, editor-centric |
| Headless CLI build/test (incl. Android) | `-batchmode` + game-ci, routine | Possible, heavier |
| Compile loop speed | Seconds (C#) | Minutes (C++) |
| Xbox controller on Android + PC | Input System, native support | Supported |
| Cost at our scale | Personal edition free < $200k revenue | 5% royalty > $1M |

## Decision

- **Unity 6 LTS, Universal Render Pipeline (URP)**, one codebase shipping **Android (primary perf floor)** and **PC/Steam (high-end target)**.
- **ECS/DOTS + Burst + Jobs** for the swarm sim hot path; conventional GameObjects for hero, UI, and low-count actors.
- **Unity Input System** with gamepad as the *first-designed* control scheme (touch is a fallback overlay, not the design center — this also keeps us console-ready).
- **IL2CPP** for Android release builds; `Asset Serialization: Force Text` from day one.
- The deterministic simulation core lives **outside UnityEngine** (pure C#, `sim/` in this repo) and is consumed by Unity via assembly definition — testable with plain `dotnet test`, engine-swappable, and it guarantees the pathing-preview-equals-live-sim covenant (Trap 5, doc 03).

## Consequences

- **Fidelity restatement:** "high graphical fidelity" now means *top-tier stylized-realistic mobile, scaling up handsomely on PC* — carried by density, VFX language, silhouette-strong art direction, and lighting mood, not by path-traced GI. The dark neon-and-thermite look survives; the Nanite rubble field does not.
- **Density targets:** 1,000+ live agents remains the PC target; Android targets **300–500 on-screen** (mid-tier device) with the same sim, scaled spawn budgets, and cheaper render path. Honest number, revisited against measurements.
- Docs 01–03 remain valid in architecture (flow fields, VAT-style instanced crowds, snapshot netcode, CI perf gates) — only the engine bindings change.
- Business note, flagged not decided: premium-priced games monetize poorly on Google Play; PC/Steam likely remains the revenue platform, with Android as reach/companion. Monetization design deferred.

## What would reverse this

Owner veto; or a hard requirement returning to cinematic desktop-only fidelity; or ECS/URP failing the Milestone-1 density gate on target hardware (measured, not vibed).
