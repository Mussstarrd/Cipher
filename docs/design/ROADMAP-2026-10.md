# Roadmap — from toy to game (October 2026)

**Date:** 2026-09-10 · **Author:** ENG · **Supersedes:** `ROADMAP-2026-09.md` (all of its September rows shipped on 2026-09-10)

**Inputs:** `../decisions/ADR-003-premise-the-cascade.md` (the premise), `progression-and-campaign.md` (gear, builds, scenarios, co-op), `arsenal-and-terrain.md` (towers, destructible terrain, level themes), `../ACCOUNTS.md` (tooling), `../feedback/2026-09-10-maze-v1-first-play.md` (the owner's list).

## Where we actually are

The loop is real and the owner's verdict is *"It's fun"* — but also *"I was able to deploy a bunch of towers and easily beat the level."* That sentence is the whole roadmap. What exists is a **sandbox**: one arena, one difficulty, no reason to come back. Everything below turns it into a **game**: a reason to play the next mission, and a reason for the next mission to be harder.

Shipped and playable today: hero with an aimable line airstrike, build mode with an honest pathing preview, cash economy, two tower families with upgrade ladders, Sappers that breach walls, Spitters that hunt turrets, repair drones, gun crates, destructible terrain, a minimap, procedural sound, 5-wave win/lose.

## The one decision that shapes the order

For one engineer, **art and systems are an either/or**, and the owner has now asked about graphics twice. So the two credible orders are:

**Order A — systems first (ENG recommends).** Scenarios, objectives, gear and the safe zone land first, so when the art arrives it is dressing a real game. Risk: the owner looks at capsules for another two weeks.

**Order B — art first.** URP migration and the first real characters land first, so it stops looking like a prototype and can be shown to people. Risk: two weeks of work that makes a sandbox prettier without making it deeper, and the gear/scenario architecture lands on top of art rather than under it.

**ENG recommends A, but weakly.** The tiebreaker is what the owner wants the build *for*. If the next milestone is "show a friend", B wins outright. **This is the owner's call and the rest of this document assumes A; say the word and the phases swap.**

## Phase 1 — it becomes a game (target: ~1 week)

The single highest-value change: **missions come from data and end on objectives**, not a hard-coded five-wave list.

| # | Work | Why it is first | Owner sees |
|---|---|---|---|
| 1.1 | **Scenario JSON + `IObjective`** — today's arena becomes the first scenario file; objectives compose (hold N seconds, protect X, survive, eliminate) | Everything else hangs off it. Missions become content, not code. Per ADR-003 most missions are *held*, not cleared. | Missions with different win conditions, and a mission he can actually beat |
| 1.2 | **Stat resolver** — kills the `GunTiers` mutate-in-place hack; stats resolve from sources (base + gear + power-ups) | Blocks gear, blocks builds, blocks co-op. Cheap now, expensive later. | Nothing directly (it is plumbing) |
| 1.3 | **Item drops + inventory + safe zone** — 8 slots, 5 rarities, sell for Scrip between missions | The owner's ask, and the reason to play mission 2 | Loot, a stash screen, armour that upgrades |
| 1.4 | **Difficulty that escalates** with scenario index | Answers "easily beat the level" | Missions that fight back |
| 1.5 | **Cover props + roving drone** (from `arsenal-and-terrain.md`) | Trees and stalled cars are what make a backyard read differently from a freeway; the drone is the owner's ask, twice | Terrain that matters, a patrolling gunship |

**Exit criterion:** the owner plays three different scenarios back to back, loses one, and can say what he would change about his loadout.

## Phase 2 — it stops looking like a prototype (target: ~2 weeks after Phase 1)

This is the honest answer to *"when are we going to start incorporating some design skins like environmental background textures and stuff like that."*

| # | Work | Effort |
|---|---|---|
| 2.1 | **URP migration** — must happen before any shader or animation work; custom shaders do not port | 1–2 days |
| 2.2 | **Vertex-animation pipeline** — bake a rigged zombie's run/attack/die to a texture, draw 1,000 of them instanced | 3–5 days |
| 2.3 | **First real characters** — one infected model with variants, one rigged hero with a weapon | 2–3 days + asset sourcing |
| 2.4 | **Environment pass** — ground materials, wall/prop meshes per theme, lighting and colour grade per level | 3–5 days |

**Blocked on the owner:** asset sources need accounts (`../ACCOUNTS.md`). Nothing in Phase 2 starts until there is a licence-clean place to get a rigged humanoid and a zombie.

**What "first real characters" honestly means:** placeholder-quality, license-clean assets that read correctly at gameplay distance. Not the pitch's earned cosmetic tiers — those need a commissioned artist and remain months out.

## Phase 3 — the campaign (target: ~3 weeks after Phase 2)

Act 1 of ADR-003: 12–15 missions across substation, backyards, freeway, laboratory, armoury. Pre-mission briefing cards and radio barks (no cutscenes, no voice acting). City map as mission select. Faction rivals. Heat modifiers for replaying a beaten block.

## Phase 4 — couch co-op

Split-screen, two players, PC only (rendering the flood twice is not an Android budget). Per `progression-and-campaign.md` §8 the blocking assumptions are: a single `_hero` field, a build cursor fused to one player, a `Bank` with no owner, a camera rig built once in `Awake`, and every proximity query that takes "the hero position". **None of that gets harder if we keep writing rule classes as pure C# with injected state — which is the current discipline — so no co-op work happens before Phase 3.**

## Decisions waiting on the owner

Nothing below blocks Phase 1; defaults are in force.

1. **Order A or B** (systems first or art first). The only genuinely important one.
2. **ADR-003:** does the kingpin identity stay, is "the Cascade" the infection's name, and how bleak is the ending? See that ADR's three questions.
3. **Accounts** in `../ACCOUNTS.md` — Phase 2 is blocked without them.
4. Standing defaults from the September roadmap that were never answered and are still live: repair drone requires the hero nearby (yes), only player barricades and map walls are breachable (yes), airstrike on a cooldown rather than a kill-charged meter (yes), kills-only income (yes).
