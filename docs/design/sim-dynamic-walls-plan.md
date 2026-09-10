# Sim: Dynamic Walls — barricades, breaches, re-routing

*Engineering plan, 2026-09-10. Scope: Milestone 2 "The Maze" + the owner's breacher ask (docs/feedback/2026-09-10-hero-first-play.md). Plan only; no code changes in this commit.*

## 0. Measured baseline (this machine, .NET 8, Release)

| Grid | Cells | `FlowField.Compute` warm | Note |
|---|---|---|---|
| 64x48 (game) | 3,072 | **0.38 ms** | first call 7 ms = JIT; pre-warm at boot |
| 64x64 (bench) | 4,096 | 0.36 ms | |
| 128x96 | 12,288 | 1.20 ms | |
| 256x192 | 49,152 | 4.80 ms | |

`AgentWorld.Step`, 1,000 agents on the 64x48 serpentine: **0.99 ms/tick**. Cost is ~0.12 us/cell on PC; assume 3-5x on a mid-tier Android (IL2CPP, Cortex-A7x) → ~1.5-2 ms per full recompute there.

## 1. GridMap data model

Today `GridMap` holds `byte[] _cost` (>= 1, already used by Dijkstra as a multiplier) and `bool[] _blocked`, with `Version` bumped on any change. Extend, keeping SoA:

```csharp
public enum WallKind : byte { None, Rock /*indestructible*/, Wall /*map, breachable*/, Barricade /*player*/ }
public enum BreachStage : byte { Intact, Cracked /*1 agent/s*/, Broken /*3 agent/s*/, Collapsed /*open*/ }

private readonly byte[]   _kind;        // WallKind
private readonly ushort[] _hp;          // 0 when kind == None
private readonly byte[]   _stage;       // BreachStage
private readonly int[]    _gateBudget;  // milli-tokens; only read for cells with stage Cracked/Broken
private readonly int[]    _passCount;   // agents that crossed this cell since breach (drives widening)

public bool IsBlocked(int x, int y) => _kind[i] != None && _stage[i] == Intact;   // Cracked/Broken/Collapsed are passable
public bool IsGate(int x, int y)    => _stage[i] == Cracked || _stage[i] == Broken;
public void PlaceBarricade(x, y, ushort hp)   // Version++, kind=Barricade, stage=Intact
public void Breach(x, y)                       // stage++ ; sets cost per stage (12 / 4 / 1); Version++
public void Repair(x, y)                       // stage=Intact, hp=max, passCount=0; Version++
public ulong StateHash()                       // kind, hp, stage, passCount — mixed into AgentWorld.StateHash
```

A breach is a *cell state*, not geometry: `Cracked` and `Broken` cells are passable to the flow field (so the horde is attracted) but carry a high traversal cost and a throughput gate; `Collapsed` is an ordinary open cell. Cost-by-stage is the routing lever: at cost 12 only agents for whom the hole saves > ~12 cells of walking divert (the nearby ones — "attracted to the path of least resistance" without the entire horde U-turning at the far end of the maze); at `Collapsed` the whole map re-plans through it.

### Modelling "1 agent per second"

| Option | Throughput control | Determinism | Feel |
|---|---|---|---|
| (a) Gate cell, N admits/tick, queue | exact, tunable | index-order admit is deterministic | scrum at the hole, trickle out — what the owner described |
| (b) High-cost cell only | none (cost shapes *who* diverts, not *how many pass*) | trivial | once they arrive it is a floodgate |
| (c) 1-cell-wide gap + separation | ~5/s at MoveSpeed 3, SepRadius 0.6; not tunable | fine | fine visually, 5x too leaky |

**Recommendation: (a) for throughput + (b)'s cost for attraction.** The gate is a per-cell integer token bucket refilled each tick (`+ rate * 1000 / 30` milli-tokens, capped at 1000): an agent may *enter* a gate cell from a non-gate cell only if it can spend 1000. The check lives in `Movement.CanTravel`'s caller (`AgentWorld.Step`) and is skipped entirely when `map.GateCount == 0`, so the hot loop pays one branch in the common case. Agents refused entry get `ResolveWalls`' slide/stay, so they bunch at the mouth — exactly the crowd-pressure visual we want, and it costs nothing extra. Integer tokens, fixed iteration order → bit-exact. Widening is **traffic-driven**: `_passCount` advances the stage (Cracked → Broken at 20 passes, → Collapsed at 60), so killing the breacher during its telegraph window prevents the hole, but once open the horde chews it wider on its own and only a repair stops it (the owner's "progressively gets worse until I go buy a repair").

Hard rule on re-blocking: `PlaceBarricade`/`Repair` on a cell any living agent occupies is **refused** (`PlacementResult.Occupied`) rather than entombing the agent — the standard TD rule, and cheaper than an eviction pass.

## 2. Flow-field regeneration

**Decision: full recompute, coalesced to at most once per tick, triggered by `map.Version != field.ComputedForMapVersion`.** `AgentWorld.Step` calls `_flowField.EnsureFresh()` first thing (the interface gains that one method). Any number of mutations in a tick cost one 0.38 ms compute. Breach stages and barricade placements are discrete, seconds-apart events, and eat-through HP loss does not bump `Version` until a stage boundary is crossed, so realistic churn is < 5 recomputes/s — ~2 ms/s on PC, ~10 ms/s on Android, out of a 4 ms/tick budget. The worst case (a change every tick) is 30 x 0.38 = 11 ms/s (PC) / ~60 ms/s (Android, 1.5%-of-budget-per-tick, 50% at worst) — survivable, and coalescing makes it the ceiling.

**Incremental becomes necessary when a full compute exceeds ~1 ms on the phone**, i.e. above ~8-10k cells (a 128x64 map), or if a future feature mutates costs continuously (corpse mounds updating every tick). Both are outside M2. When it lands, the goal-rooted Dijkstra tree we already have is exactly the structure D* Lite / LPA* repair ([Koenig & Likhachev](http://idm-lab.org/bib/abstracts/papers/aaai02b.pdf)): only locally-inconsistent cells re-expand, cost proportional to the changed region. `FlowField.Compute`'s doc-comment already reserves this; the API does not change. Cheaper interim wins if needed first: split `BakeDirections` out to a lazy pass, and run `Compute` on a worker thread with double-buffered arrays (swap at tick start — the field is immutable during a tick).

## 3. Preview covenant (Open Decision #5)

The preview must run the live code path, never a lookalike. Proposed API:

```csharp
// GridMap
public void CopyTo(GridMap scratch);                // Array.Copy of the SoA columns; 3,072 cells = microseconds

// FlowField
void EnsureFresh();                                 // added to IFlowField
public bool DirectionsEqual(FlowField other);       // test/assert helper

// Sim-side validator (no UnityEngine): the preview AND the commit path both call it.
public readonly struct PlacementResult { Ok, Occupied, SealsSpawn, NotBuildable }
public static PlacementResult Validate(GridMap live, AgentWorld world, FlowField scratchField, GridMap scratchMap,
                                       ReadOnlySpan<(int x,int y)> cells, ReadOnlySpan<(int x,int y)> spawns)
{
    live.CopyTo(scratchMap);
    foreach (var c in cells) { if (world.CountWithin(GridMap.CellCenter(c.x,c.y), 0.5f) > 0) return Occupied; scratchMap.PlaceBarricade(c.x, c.y, hp); }
    scratchField.Compute(live goal);                // same class, same Dijkstra, same corner rule
    foreach (var s in spawns) if (!scratchField.HasPath(s.x, s.y)) return SealsSpawn;
    return Ok;
}
```

Build mode holds one `scratchMap` + `scratchField` (allocated once). Every cursor move: `Validate` (0.4 ms) and the overlay draws `scratchField.DirectionAt` arrows — a *what-if* that is literally the live algorithm on a copy of the live map. On confirm, the same cells are applied to the live map; the next tick's `EnsureFresh` recomputes and the field is cell-for-cell identical to the scratch one (guarded by test 5.1). "Preview never lies" therefore means: **truthful for the current wall state**. Dynamic changes are telegraphed, not predicted:

- `BreachStarted(cell, etaSeconds)` fires when a breacher begins working (T = 6 s default) — the kill window and the on-screen warning.
- `FieldChanged(version, changedCells)` after any recompute: `FlowField` keeps the previous `_direction` buffer and emits the indices whose direction changed; the tactical overlay pulses them for a second. Costs one 3,072-cell compare per recompute.

The M2 degenerate-strategy check (full-seal turtle must lose legibly by wave 5) falls out of `SealsSpawn` being *allowed* for barricades but making every wall a breacher target: a breacher spawn is forced when no spawn has a path.

## 4. Archetypes in AgentWorld without virtual dispatch

Add `byte[] _archetype`, `byte[] _state`, `int[] _targetCell`, `float[] _workTimer`, and a small `List<int> _breachers`. The main loop stays branch-light; breachers run in a second pass.

```csharp
public enum Archetype : byte { Runner = 0, Breacher = 1 }
enum BreacherState : byte { Follow, Beeline, Work }

public void Step(float dt)
{
    _flowField.EnsureFresh();
    RebuildHashIfDirty();
    _map.RefillGates(dt);                       // no-op when GateCount == 0
    for (int i = 0; i < Count; i++)
    {
        if (!_alive[i] || _archetype[i] != (byte)Archetype.Runner) continue;
        StepRunner(i, dt);                      // existing body; + TryEnterGate before ResolveWalls
    }
    for (int k = 0; k < _breachers.Count; k++) // fixed order; list compacted when a breacher dies
    {
        int i = _breachers[k];
        if (!_alive[i]) continue;
        switch ((BreacherState)_state[i])
        {
            case Follow:  StepRunner(i, dt); if (CellDistance(i, _targetCell[i]) <= _config.BreacherBeelineCells) _state[i] = Beeline; break;
            case Beeline: SteerToward(i, StandCellFor(_targetCell[i]), dt); if (AtStandCell(i)) { _state[i] = Work; _events.Add(BreachStarted(cell, _config.BreachWorkSeconds)); } break;
            case Work:    _workTimer[i] += dt; if (_workTimer[i] >= _config.BreachWorkSeconds) { _map.Breach(x, y); _state[i] = Follow; } break;
        }
    }
    _hashDirty = true;
}

// Public API the game layer needs
public int  SpawnBreacher(Vec2 position, float health);           // target chosen inside the core (see §5)
public int  SpawnFromTable(Vec2 position, SpawnTable table);      // rolls archetype with the core RNG
public ReadOnlySpan<SimEvent> DrainEvents();                      // BreachStarted, BreachOpened, StageChanged, WallCollapsed, FieldChanged
public PlacementResult PlaceBarricade(ReadOnlySpan<(int,int)> cells);
public PlacementResult RepairWall(int x, int y);
```

`StepRunner` is the existing body extracted verbatim; `SteerToward` reuses `Movement.ResolveWalls` (the shared wall rule, so a breacher cannot corner-cut either). Breachers "ignore the field" only in `Beeline`, within N cells of their wall. Cap: `MaxLiveBreachers = 8` enforced in `SpawnFromTable`.

## 5. Determinism

Randomness enters in exactly two places — the archetype roll (0.05% per spawn) and tie-breaking among target walls — and both stay inside the core:

```csharp
public struct XorShift64 { ulong _s; public XorShift64(ulong seed) { _s = seed == 0 ? 0x9E3779B97F4A7C15 : seed; }
    public ulong Next() { _s ^= _s << 13; _s ^= _s >> 7; _s ^= _s << 17; return _s; }
    public int NextInt(int maxExclusive) => (int)(Next() % (ulong)maxExclusive); }
```

`SimConfig.Seed` seeds one `XorShift64` owned by `AgentWorld`; its state is mixed into `StateHash`, as is `GridMap.StateHash()` and the new per-agent arrays. Target selection is mostly *not* random: scan wall cells whose two orthogonal neighbours are both reachable and take the one with the largest `|Integration(a) - Integration(b)|` — the biggest shortcut, which is what makes it the "smart" enemy — with the RNG only breaking ties among the top 3. The scan is O(cells) per breacher spawn (rare). The game layer never rolls dice for the sim: `SpawnTrickle` switches to `SpawnFromTable`.

Tests to add (`sim/tests/Cipher.Sim.Tests/DynamicWallTests.cs`, `BreacherTests.cs`):

1. `Preview_ScratchField_EqualsLiveField_AfterCommit` — validate on scratch, commit to live, `EnsureFresh`, assert directions and integration equal cell-for-cell (the covenant test; also runs in CI on 3 fixed layouts).
2. `Placement_RefusedWhenCellOccupied_AndAgentNeverEntombed` — agents in walls = 0 across 300 ticks of random-but-seeded building.
3. `Placement_ThatSealsSpawn_ReportsSealsSpawn_ButBreacherIsForced`.
4. `Gate_AdmitsAtMostRatePerSecond` — 200 agents pressed against a Cracked cell; count crossings over 10 s within ±1 of rate.
5. `Gate_Throttle_IsBitExact_AcrossRuns` — hash equality with a gate under load.
6. `Breach_OpensHole_AgentsReroute_Repair_AgentsRerouteBack` — three-phase route-length assertion (`IntegrationCostAt` at spawn falls, then rises back to the pre-breach value).
7. `Breach_WidensWithTraffic_ThenCollapses_ToPlainOpenCell` — stage transitions at exact pass counts; collapsed cell has cost 1 and no gate.
8. `Breacher_Killed_DuringTelegraph_LeavesWallIntact`.
9. `Breacher_TargetsLargestShortcutWall_Deterministically` — two seeds, same map → same target when no tie.
10. `SeededRng_SameSeed_SameArchetypeSequence_DifferentSeed_Differs`.
11. `StateHash_CoversWallState_AndRng` — breach one cell, no agent moves, hash changes.
12. `FieldRecompute_CoalescesToOnePerTick` — 50 mutations in one tick → `Compute` call count 1 (counting subclass/test double via `IFlowField`).
13. `Movement_And_Field_AgreeOnGateCells` — extends `Directions_NeverPointIntoBlockedCells` to Cracked/Broken/Collapsed stages.

## 6. Performance guardrails and cuts

Guardrails: field recompute ≤ 1/tick (coalesced), pre-warmed at boot; gate logic gated by `GateCount == 0`; breachers ≤ 8 live, target scan only at spawn; zero steady-state allocation (events in a pooled list drained per tick); `FieldChanged` diff only when someone is subscribed. Bench gains a `--maze` scenario: 1,000 agents, a breach at tick 90, a repair at tick 240, reporting recompute count and ms — and the CI perf floor covers it.

**Cut from this pass:** incremental/D* Lite regen (measured unnecessary at 3k cells); per-archetype flow fields and multiple goals; agent eviction on re-block (refusal instead); sub-cell hole geometry (a hole is a cell); breacher-driven widening after opening (traffic does it); wall deformation visuals beyond a scale change; DOTS/Burst (Open Decision #4 — nothing here needs it at 1,000 agents / 1 ms).

## References

- [Emerson, "Crowd Pathfinding and Steering Using Flow Field Tiles", Game AI Pro (Supreme Commander 2)](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter23_Crowd_Pathfinding_and_Steering_Using_Flow_Field_Tiles.pdf) — cost fields + integration fields, player-painted pathable terrain.
- [Red Blob Games, "Flow Field Pathfinding for Tower Defense"](https://www.redblobgames.com/pathfinding/tower-defense/) — the one-search-serves-all-agents model this core already uses.
- [Koenig & Likhachev, "D* Lite" (AAAI 2002)](http://idm-lab.org/bib/abstracts/papers/aaai02b.pdf) — the incremental repair we defer to until grids exceed ~10k cells.
