# Breach & Repair — the "smart" saboteur (design memo)

**Date:** 2026-09-10 · **Status:** proposal for owner review · **Source ask:** `docs/feedback/2026-09-10-hero-first-play.md` item 1 · **Scope:** design only, no code in this change.

## 1. TL;DR

- Ship **one new archetype, the Sapper**: a rare, telegraphed unit that walks the normal flow field, stops at the first player wall whose far side is a big shortcut, plants a kit, and opens a staged hole (1/s → 3/s → collapse over 40 s).
- The hole is expressed as **field cost + a per-cell admission gate**, so the pathing preview stays honest by construction (Trap 5) and only agents whose detour exceeds the hole's cost divert — not the whole horde at stage 1.
- Counterplay ladder: kill the Sapper (18–25 s walk), shoot the kit (4 s plant + 20 s stage 1), or buy a **consumable Repair Drone** the hero must escort.
- Spawn rule: **scripted first Sapper at T+45 s**, then 1 per 200 spawns with ≥60 s spacing, max one active breach. The owner's 0.05% becomes the late-game floor, not the v0 rate.
- Wrecker stays a separate archetype and ships **after** the Sapper; Spitter's roster slot is absorbed into the same "structure damage → field cost" system later.

## 2. Mapping onto Wrecker and Spitter

The pitch has two anti-maze units: the **Spitter** (T2, ranged structure damage) and the **Wrecker** (T4, ignores the field). The owner's ask is a third grammar: *rare, targeted, slow-burn, repairable* — closer to the Spitter's job than the Wrecker's. Orcs Must Die's Kobold Sappers are the precedent: barricade-targeting units colour-flagged on the map so players prioritise them ([OMD3 barricade discussion](https://steamcommunity.com/app/1522820/discussions/0/5056966106384834836/)).

**Decision: two archetypes, not one, and the Sapper ships first.**

| | Sapper (new, v0) | Wrecker (blueprint M2, next) |
|---|---|---|
| Movement | normal flow field — **no new movement class** | ignores field, straight line — new traversal class + second field query mode |
| Effect on map | staged cost/gate mutation on one wall cell | barricade HP damage on contact along its line |
| Sim cost to build | archetype flag + a `BreachSystem` on top of `GridMap.SetCost/SetBlocked` | new field-bypass steering, per-cell HP, plus everything the Sapper needs |
| Counter | kill it, shoot kit, repair | hero DPS only |

The Sapper is cheaper (it only *uses* `GridMap.Version` + `FlowField.Compute`, which M2 needs anyway), it is the owner's explicit ask, and it validates the repair loop the Wrecker also needs. The Spitter later becomes "a common Sapper that damages from range": same wall data, no kit.

## 3. Graybox v0 spec

**Spawn rule.** The graybox tops up to 1,000 agents at the kill rate (~3–6/s), so 0.05% ≈ one Sapper every 5–8 minutes: the owner would never see one, and players cannot learn a threat they meet once an hour. v0 rule:

- Wave/session script forces Sapper #1 at **T+45 s** (visible by minute 1, breach resolving by minute 2 of a 3-minute session).
- Thereafter **1 per 200 spawns (0.5%)**, drawn from a seeded stream passed into the sim (see §5), with **≥60 s spacing** and a cap of **1 active breach + 1 walking Sapper**. Expected 2–3 sightings per 3-minute session.
- Late-game wave tables can drop this to the owner's 0.05% once other pressures exist.

**Target selection.** No second flow field. Each tick the Sapper checks its four orthogonal neighbours: if neighbour `(x+dx, y+dy)` is a **Barricade-class** wall (not Bedrock) and `IntegrationCostAt(x+2dx, y+2dy) ≤ IntegrationCostAt(x, y) − 12`, it stops and plants there. That is "the wall between it and the goal that saves the most", from data the field already has, so the choice is deterministic and the preview can draw it. The graybox's three serpentine walls are flagged Barricade-class so the owner sees this before build mode exists; arena boundaries are Bedrock and never targeted.

**Timeline (seconds after the Sapper reaches the wall).**

| Phase | Duration | Map state | Kit |
|---|---|---|---|
| Plant | 4 s | wall intact | kit visible, 40 HP, shootable |
| Stage 1 | 20 s | cell unblocked, cost 40, gate 1 agent/s | kit 40 HP |
| Stage 2 | 20 s | cost 15, gate 3 agents/s | kit 40 HP |
| Collapse | — | cell + both orthogonal neighbours become floor, cost 1, no gate | kit gone |

Walk time on the 64×48 arena at 3 cells/s is ~18–25 s, so first ping to any route change is **≥ 22 s** (Open Decision #5: "you will always see it coming"). Sapper HP **60** (runners 10; hero LMG 72 DPS → ~1 s of fire). The difficulty is *finding* it from the chase camera, not killing it.

**Throughput model.** Cost alone cannot express "1 per second": one open cell at 3 cells/s passes ~5 agents/s. Decision: **both levers**. Cost decides *who diverts* (preview-visible); a per-cell **admission gate** (integer token bucket, agents served in index order) decides *how many pass*. Agents without a token are wall-blocked by `Movement.CanTravel` this tick and queue.

**Telegraph.** On spawn: taller orange-emissive capsule with a backpack, off-screen HUD arrow labelled "SAPPER", looping drill-whine panned to it. On plant: siren, pulsing wall cell, countdown to stage 1. Each stage: wall cracks/lowers, bass hit. The LB peek shows the breach cell in its current cost colour.

**Counterplay ladder.**

1. Kill the Sapper in transit (free, 18–25 s window).
2. Shoot the kit during plant (no hole) or stage 1/2 (hole **freezes** at its current stage; still needs repair to close).
3. **Repair Drone** — consumable, bought with kill currency (placeholder **150**; a 3-minute session yields ~600 at 1/kill). Spawns at the hero, flies 6 cells/s to the nearest breach, repairs one stage per 4 s (collapse → sealed 12 s) **only while the hero is within 8 cells**. Invulnerable in v0. A placed auto-repair emplacement is v1. Dungeon Defenders uses the same "repairs cost the build currency" tension ([DD2 defenses](https://wiki.dungeondefenders2.com/wiki/Defenses)).

## 4. Emergent-behaviour check

**Flow field re-route.** Opening a cell bumps `GridMap.Version`; a full `FlowField.Compute` (3,072 cells, <1 ms) re-routes *every* agent whose route through the hole is cheaper. With a naive "cost 1 when open", 1,000 agents would park at a 1/s gate: the killzone empties and the game becomes "shoot the queue". They Are Billions shows the failure mode — one gap and "100% of zombies rush that gap" ([TAB tips](https://www.neoseeker.com/they-are-billions/guide/Tips_and_Tricks)). We do want attraction; we do not want *total* attraction at stage 1. **Decision: stage cost is the divert filter** — at cost 40 only agents saving >40 cells of maze divert (in the graybox, roughly the bottom third of spawns at the first wall); at 15 most divert; at collapse everyone does. Whole-horde attraction, scaled by stage, and the *only* rule the preview can draw truthfully; a "nearby agents only" rule is a second pathing behaviour the preview cannot show — rejected. Flow-field literature agrees: congestion is handled by cost, not per-agent exceptions ([Emerson, Game AI Pro ch. 23](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter23_Crowd_Pathfinding_and_Steering_Using_Flow_Field_Tiles.pdf)).

**Degenerate strategies (Open Decision #8 format).**

| Strategy | Exit criterion (M2 test) | Pre-committed fallback |
|---|---|---|
| Full-seal, camp the breach | A full-seal build with hero camping must still lose legibly by wave 5 (#5 already requires this) | Target choice adds a seeded tie-break among all qualifying walls, weighted *away* from the hero's cell; 2 Sappers/wave from wave 3 |
| Ignore breaches | An ignored collapse must cost ≥30% vault HP within 60 s | Collapse cascades one extra cell per 20 s; collapsed cells cannot be re-barricaded mid-wave |
| Repair spam | Testers buying ≥3 drones/wave without leaving the killzone | Price ×1.5 per purchase within a wave; one drone alive at a time |

## 5. Implementation plan by layer

**Sim core (`sim/src/Cipher.Sim`, no UnityEngine).**
- `GridMap`: add `WallClass` per cell (None/Bedrock/Barricade) and `BreachStage`; a `SetBreachStage(x, y, stage)` that applies the blocked/cost table above and bumps `Version`. Per-cell cost already exists.
- `BreachSystem` (new): active breach record (cell, stage, timer, kit HP), gate token buckets in fixed-point ints, `DamageKit`, `Repair(dt, heroPos)`. Owns all stage transitions.
- `AgentWorld`: `byte[] _archetype`, `_plantTimer`; `Spawn(pos, health, archetype)`; Sapper branch in `Step` (shortcut test → plant). Gate check in the movement resolve. `StateHash` must mix archetype, breach stage, and gate tokens.
- Field recompute trigger: the tick driver recomputes when `ComputedForMapVersion != map.Version` (full recompute is fine at 64×48; incremental stays a later optimisation).
- **Determinism:** no RNG in the core. The spawn decision comes from an `ISpawnStream` (seeded xorshift) supplied by the caller; the sim only consumes integers.

**Game layer (`game/`).** Sapper and kit meshes, wall crack stages (colour + height), HUD off-screen arrow + countdown, three audio stubs, D-pad-down purchase input, drone sphere with escort-radius ring, preview overlay colouring breach cells by current cost.

**Tests that must exist (same commit as the logic).**
1. `Sapper_SameSeedSameSpawnTicks` — two worlds, identical streams, identical Sapper spawn ticks and StateHash.
2. `Sapper_PlantsAtFirstWallWhoseFarSideSavesAtLeastThreshold` — serpentine map, known plant cell.
3. `Sapper_NeverTargetsBedrock` — boundary walls untouched.
4. `Breach_StageOneAdmitsAtMostOneAgentPerSecond` — 100 agents queued, ≤30 pass in 30 s.
5. `Breach_TimelineTransitionsAt4_24_44Seconds`.
6. `Breach_KitDestroyedDuringPlant_LeavesWallIntact`; `_DuringStage1_FreezesStage`.
7. `Breach_OpeningRecomputesFieldAndDivertsOnlySavingsAboveCost` — agent at far detour diverts, agent near the gap does not.
8. `Repair_RequiresHeroWithinEscortRadius_ThenRestoresBlocked`.
9. `Preview_MatchesLiveRoutingThroughStagedBreach` — Trap 5 divergence test with a breach at each stage.
10. `StateHash_ChangesWithBreachStageAndGateTokens`.

## 6. Risks and v0 cuts

**Risks.** (a) Mosh pit: queued agents stack on the wall side; the 0.9-cell cap and gate-as-blocked rule guard tunnelling. (b) Chase camera misses the Sapper — the off-screen ping is the feature, not polish. (c) Cost thresholds (40/15/12) are guesses; tune against the preview. (d) "Rare = 0.05%" vs visibility first.

**Cut from v0.** Placed auto-repair emplacement; Spitter ranged acid; Sappers attacking emplacements (that is the *second* 0.05% ask, a separate memo); more than one simultaneous breach; breaching Bedrock; killable drone; incremental field recompute; any art beyond coloured primitives.

## Questions for the owner

1. **Escort or fire-and-forget?** The drone repairing only while you stand within 8 cells pulls you out of your killzone — is that the tension you want, or should money alone fix a wall?
2. **Can city walls ever be breached**, or only barricades you built? (Decides whether the Sapper can change the level, not just your maze.)
3. **Silhouette:** a mutated *person* with a backpack (reads "smart", human-made kit) or a Bloom creature that grows the breach organically?
