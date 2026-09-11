# Arsenal & terrain — tower families, roving drones, destructible environment, level themes

**Date:** 2026-09-10 · **Author:** ENG · **Status:** proposal, owner veto
**Source:** owner asks in `docs/feedback/2026-09-10-maze-v1-first-play.md`. Fiction per `../decisions/ADR-003-premise-the-cascade.md`.
**Siblings:** `progression-and-campaign.md` (gear, builds, scenarios, co-op), `../ACCOUNTS.md` (tooling).

## TL;DR

- Three tower families, not a list: **Line** (single target, range), **Grinder** (short-range sustained area), **Roving** (a drone that patrols a route you draw). Each answers a different failure, so choosing between them is a decision rather than a budget check.
- **Player weapons damage terrain.** Environment walls take full damage, your own barricades take a quarter. Wall hit points already exist; the airstrike widening the hole the horde is pouring through is the best tension in the design and it costs almost nothing to build.
- **Cover objects** (trees, stalled cars, transformers) are one-cell destructible props that block line of sight but not movement — the thing that makes a backyard read differently from a freeway.
- Level themes are maze topologies, not skins: substation, laboratory, armoury, backyards, stalled freeway.

---

## 1. Tower families

Today there is one turret (Sentry .50: range 10, 60 dps, targets the runner nearest the vault). It is a good default and a boring only-choice. Three families, each with the existing two-tier upgrade ladder:

| Family | v0 unit | Cost | Range | Damage | Targets | The failure it answers |
|---|---|---|---|---|---|---|
| **Line** | Sentry .50 (exists) | $150 | 10 | 60 dps single | first toward vault | leakers down a long lane |
| **Grinder** | Brush Hog | $120 | 2.2 (all round) | 22 dps to **everything** in radius, no target cap | n/a | the crowd that walks past your single-target gun |
| **Roving** | Cartel Gunship | $260 | 6 while moving | 35 dps single | nearest on its route | a hole you cannot predict |

**Grinder** is the owner's "one that spins to do AoE of people passing it". Cheap, short, and it wants to be *inside* the maze at a corner where the crowd bunches, which teaches maze shape. It is deliberately terrible in the open.

**Roving** is the owner's "roving attack drone, kind of like the airplanes in Bloons". Design:

- You place **2 to 4 waypoints** in build mode; the drone loops them at 6 cells/s. Waypoints are free to move between waves, cost a small fee mid-wave.
- It flies, so it ignores walls and can cross a breach. That is the point: it is the only defence that can be redeployed to a surprise.
- It cannot be attacked by Runners; **Spitters can shoot it down** while it is within their 9 cells. So it is not a free answer.
- Deterministic: position is a pure function of route length and elapsed sim time, so it needs no pathfinding and stays hash-stable.

**Upgrade ladders** reuse `TurretTier` (data, so tiers can grow without code):

- Sentry: *Twin .50* (damage ×1.6) → *Overwatch* (range +4). Exists.
- Rotor: *Barbed Drum* (damage ×1.7) → *Wide Throw* (radius +1.0).
- Gunship: *Door Gunner* (damage ×1.6) → *Second Bird* (a second drone on the same route, half damage).

**Kill criterion for the family split:** in a 5-mission run the owner builds at least one of each family without being told to, and can say in one sentence what each is for. If two families collapse into "the good one", cut to two and rebalance.

---

## 2. Destructible terrain

Owner: *"the walls that are part of the environment if the horde can eat through them maybe my AoE should damage them or my gunshots so that over time the environment deteriorates."*

Yes. `GridMap` already stores wall hit points, and the breach system already turns zero hit points into a widening hole. Wire player damage into the same pipe:

| Source | Damage to map walls | Damage to your barricades | Note |
|---|---|---|---|
| Airstrike bomb | 120 per bomb | 30 | six bombs will open a wall you did not want opened |
| Hero gunfire (direct hit) | 3 per round | 0 | ~36 dps of chip; the wall behind a firefight erodes over a minute |
| Grinder / Sentry fire | 0 | 0 | your own guns never eat your maze |
| Spitter acid | 15 | 15 | already hits structures; now hits walls too |

Reaching zero hit points advances one breach stage, exactly as the Sapper's kit does. The whole feature is one damage call plus tuning, and the existing tests already cover what happens next.

**Why quarter damage on your own barricades rather than none:** none is safer, and boring. A quarter means a long fight at your own wall costs you something without a stray round ruining a run. Show a small "friendly fire" tick on the wall's health bar the first three times it happens, then stop teaching.

**Degenerate case to watch:** the player learns to demolish their own maze with the airstrike to reshape it for free. Fix if seen: sell already refunds, so make friendly demolition refund nothing. Pre-committed.

---

## 3. Cover props

One-cell objects that are **not** walls: they block line of sight and gunfire, but agents path around them freely and they have low hit points.

- **Tree** (backyards) — 60 hp, blocks shots, burns for 6 s when hit by an airstrike, dealing area damage.
- **Stalled car** (freeway) — 120 hp, blocks shots, **explodes** at zero for 60 damage in a 3-cell radius. Chain reactions on a packed freeway are the set piece.
- **Transformer** (substation) — 100 hp, blocks shots, arcs on death: 40 damage in a 2-cell radius, briefly slows survivors.

These give each theme its own feel without new systems: they are a `WallKind` variant with `blocksMovement = false`, plus a death effect. They also make the hero's positioning matter, which the open arena currently does not.

---

## 4. Level themes

Each theme is a maze topology and a hazard, not a texture swap:

| Theme | Topology | Hazard / prop | Teaches |
|---|---|---|---|
| **Substation** (act 1 opener) | wide lanes, fenced compounds, one high-value transformer bank to protect | transformers arc | protect-an-object objectives |
| **Backyards** | dense, many short fences you can breach or shoot through, tree lines | trees burn | line of sight, short sightlines, the Grinder |
| **Stalled freeway** | two long corridors with car clumps, few build sites | cars chain-explode | funnelling, and that terrain is a weapon |
| **Laboratory** | tight interior grid, containment doors that open on a timer | sealed rooms flood | reacting to a maze that changes without you |
| **Armoury** | open lot, wall you build yourself almost from scratch, ammo caches | caches explode | pure mazing, high budget |

The current 64×48 serpentine arena stays as the tutorial/testbed and becomes the first JSON scenario (see `progression-and-campaign.md` §6).

---

## 5. Build order

Each row is independently shippable and testable; nothing here is a rewrite.

1. **Destructible terrain** (½ day) — one damage call, tuning, 4 tests. Biggest feel change per hour of work.
2. **Grinder tower** (½ day) — new `TurretFamily` data + area damage using the existing radial query. 4 tests.
3. **Cover props** (1 day) — non-blocking `WallKind`, death effects, line-of-sight check in `Raycast`. 6 tests.
4. **Roving drone** (1–1.5 days) — waypoint placement UI in build mode, deterministic route walker, Spitter targeting. 6 tests.
5. **Theme data** (ongoing) — each theme is a scenario JSON plus a prop table once scenarios land.

## Risks

- **Tower family bloat**: three families with two tiers each is nine things to balance before any of it has art. Mitigation is the kill criterion above: cut to two if one is never chosen.
- **Destructible terrain versus the preview covenant**: the build preview promises truthful routing for the *current* wall state. Player-caused wall damage is a dynamic change like a breach, so it is telegraphed the same way (health bar, then the existing breach alert). No new covenant wording needed.
- **Roving drones trivialise breaches** if they are cheap. $260 and Spitter counterplay is the first guess; expect to raise the price.
