# Part 3 — The Day-1 Execution Blueprint

*The next 30 days exist to answer one question: **is the core loop fun at density?** Everything below is scoped to produce that answer with the fewest lines of code and zero art debt. Graybox everything; the only "juice" allowed is the cheap juice that changes the answer (camera shake, hitstop, sound).*

---

## 1. Core MVP Milestones — the first 3 systems (30 days)

Three prototypes, built in this order because each de-risks the next. All in one UE5 project from day one (no throwaway side projects — the horde renderer *is* the foundation).

### Milestone 1 — "The Flood" (Days 1–10)
**1,000+ enemies moving and dying at 60fps, graybox.**

- Mass Entity swarm: archetype fragments (position, velocity, HP, state), flow-field-driven movement (see traps below), capsule-free collision via spatial hash grid.
- VAT-instanced rendering (AnimToTexture): 3 animations (run, attack, die), 2 LODs, one archetype mesh. Deliberately ugly.
- Kill pipeline: damage application in batch, death → ragdoll-free VAT death anim + Niagara gib burst, corpse fade.
- A debug wave-spawner spraying 1,000 runners at a target across open ground.
- **Exit criteria (hard numbers, measured on our min-spec target GPU, not a dev box):** 1,000 live agents + 200 simultaneous deaths ≤ 16.6ms frame, sim ≤ 4ms, and it *feels* like a flood, not a parade.

**Why first:** everything in this game is downstream of density. If we can't hit this, the pitch changes; better to know by day 10.

### Milestone 2 — "The Maze" (Days 11–20)
**Build mode + pathing that respects the player.**

- Grid-snapped barricade placement (one barricade type, one emplacement type: a turret cube that shoots), placement validation, refund.
- Flow-field regeneration on build/destroy (incremental, budgeted — see traps), barricade HP + horde eat-through behavior, Wrecker archetype stub that ignores the field and beelines.
- **The pathing preview**: live route visualization per archetype while in build mode. This is a milestone deliverable, not polish — the mazing covenant *is* the feature.
- Wave table driven by a DataTable: 5 waves, runners + a Wrecker at wave 4.
- **Exit criteria:** a designer (or any of us) can build a serpentine, watch the preview, run 5 waves, lose to a Wrecker breach, rebuild, and win — with zero pathing surprises ("it went *where*?" = bug, full stop).

### Milestone 3 — "The Gun" (Days 21–30)
**The hero inside the machine.**

- Third-person character: sprint, slide, one hitscan weapon with real feedback (hitstop on kills, tracer, decal, camera kick, kill-confirm sound), one grenade (Niagara explosion + radial batch damage into the swarm), one ultimate stub (airstrike line — the trailer moment, graybox version).
- Hero damage integrates with the batch damage pipeline from M1 (the gun kills 30 runners in a sweep and the frame doesn't hiccup).
- Mid-wave build toggle (combat-price markup).
- **Exit criteria — the only one that matters:** the **"one more wave" test.** Five people play the 5-wave graybox. If nobody unpromptedly says some version of "run it back," the loop has a design problem no amount of engineering will fix, and we iterate *here* before writing another system.

**Explicitly OUT of the 30 days:** co-op/netcode (architecture is net-*ready* — commands and batch events, no gameplay logic in rendering — but nothing replicates yet), meta progression, upgrade trees (one flat turret is enough to feel the loop), all final art, all factions. Day 31–60 adds upgrade tiers + a second and third enemy archetype — that's when the Bloons layer gets validated.

---

## 2. Technical Feasibility Traps — guard rails, installed now

Ranked by (probability × cost-when-late). Each has an owner-decision baked in *today* so nobody "discovers" these in month 6.

### Trap 1 — Per-agent pathfinding at 1,000+ units
**The killer.** A* per agent per repath = seconds of CPU. **Decision: flow fields + local steering, never per-agent global search for swarm units.**
- One flow field per (goal × archetype-traversal-class), computed on the build grid; agents sample the field and apply local avoidance (boid-lite separation via the spatial hash).
- Field regen on barricade change is **incremental and amortized** (dirty-region recompute, budget ≤ 2ms/frame spread over frames; enemies tolerate a stale field for 100ms without visible stupidity).
- Elites/humans (dozens, not hundreds) may use real navmesh A* + behavior — two-tier AI by design.
- Corpse-mound soft terrain and eat-through both express as field cost updates — one system, many features.

### Trap 2 — Niagara overdraw & VFX death-spiral
GPU particles are cheap until 200 deaths/frame each spawn a gib burst over a fire in smoke — then overdraw murders the GPU. **Decision: a global VFX budget manager from M1.** Effect pooling, per-frame spawn caps with priority (player-caused > ultimate > ambient), aggressive culling of sub-8-pixel effects, LOD'd emitters, and *one* shared gib system fed by death events rather than per-death spawns. Measured in the CI perf gate (Part 1) with a max-density replay.

### Trap 3 — Animation & skinning cost
Skeletal meshes on the horde = dead on arrival; that's why VAT is a day-1 decision, not an optimization. The *residual* trap: **elite/boss count creep.** Every "make it skeletal, it's special" request adds a real budget line. **Decision: hard cap of 12 skeletal enemies live (elites + bosses + humans), enforced by the spawner; UE5 Animation Budget Allocator on whatever remains.**

### Trap 4 — Networked physics & replication fantasy
Two rules that must be laws before any netcode exists, because retrofitting them is a rewrite:
1. **Nothing physics-simulated is gameplay-authoritative.** Chaos debris, ragdolls, gibs = cosmetic, client-local, never replicated.
2. **The swarm never replicates as actors.** Server-authoritative sim + compressed snapshot stream (Part 1). Enforced early by keeping the sim deterministic-friendly (fixed-tick sim step, no gameplay reads from render state) even in single-player.

### Trap 5 — Pathing-preview honesty drift
Subtle and brand-fatal: sim behavior evolves (corpse mounds, enrage speed, field staleness) and the build-mode preview quietly stops telling the truth — players experience it as "the game cheated." **Decision: the preview runs the *same* field + steering code path as the live sim (a query mode, not a parallel implementation), and CI includes a preview-vs-sim divergence test on fixed layouts.**

### Trap 6 — UE5 marquee-feature performance debt
Lumen + Nanite + VSM defaults are tuned for cinematic scenes, not 1,000 moving instances under 12 dynamic lights of muzzle flash. **Decision: perf budget sheet from day 1** (sim 4ms / render 8ms / VFX 2ms / UI+misc 2ms at min-spec 60fps), the nightly CI perf gate holds it, and every marquee feature has a named fallback (Lumen→baked+DFAO, VSM→CSM) that we *keep working* rather than "port later." Density games ship or die on the p95 frame time during the worst wave.

### Trap 7 — Content-scaling debt in the trees
A Bloons-depth system (say 12 emplacements × 3 paths × 5 tiers) is ~180 upgrades of design, VFX, and balance — a *content* mountain teams discover in month 8. **Decision: upgrades are data (DataTables/DataAssets) composing a small library of effect primitives (stat mod, on-kill trigger, aura, projectile swap) from the very first turret.** New tiers become design entries, not engineering tickets. The M2 turret ships as data even though it's one turret.

### Trap 8 — Save/economy integrity in co-op
Client-authoritative saves + co-op progression = duped-currency lobbies at launch. **Decision:** single-player saves stay local and simple; the moment persistent progression meets co-op (Phase 2), progression writes go through the Nakama service. Architected as a boundary now (all economy mutations flow through one interface), costing us ~zero today.

---

## The 30-day scoreboard

| Day | Proof |
|---|---|
| 10 | 1,000 enemies, 60fps, it feels like a flood |
| 20 | Mazing works, preview never lies, a Wrecker can ruin your night |
| 30 | Five graybox waves pass the "one more wave" test with a gun in your hands |

If all three light up green, we have a game and a pipeline, and month two is upgrades, archetypes, and the first real art target. If any goes red, we found the truth for the price of 30 days — which is the entire point of this blueprint.
