# The autoplay baseline

`-exodus-autoplay <seconds>` plays a position with nobody at the controller. It exists because
every headless run before it watched wave one and nothing else: with no player nothing dies, wave
one never clears, and wave two never starts. The whole back half of every mission had never
executed outside a human session, and neither had two archetypes — a Spitter needs a turret to
hunt, and a Sapper needs a wall worth breaching.

## What it is, and what it is not

It builds through the real `BuildModel`, so cost, legality and the seal rules all apply. It does
**not** shoot, dodge, repair, retreat, re-site a gun, or spend a skill point. The hero is one of the
largest damage sources in the game and the bot has switched it off.

So these numbers are a **floor**: what a position does to a defence that was built once and then
abandoned. A position that holds here is not proven easy. A position that collapses here has not
been proven broken. What the table is good for is **comparison between positions**, because every
one of them faces the identical incompetent defender.

## The table (2026-09-13, 3 turrets + 8 barricades per setup, 420s budget)

| # | Position | Outcome | At | Waves cleared |
|---|----------|---------|----|---------------|
| 1 | The Gate | held, stalemate | 420s | 0 |
| 2 | The Service Road | truck lost | 70s | 0 |
| 3 | The Pump House | truck lost | 52s | 0 |
| 4 | The Gate Falls | held, stalemate | 420s | 0 |
| 5 | The Fairway | truck lost | 42s, **1 kill** | 0 |
| 6 | Crowbar | truck lost | 116s | **1** |

Three shapes, and each one says something:

**Stalemate (1, 4).** The corridor holds — the bot's two turrets are never overrun — but it cannot
kill fast enough to clear a wave in seven minutes. These positions are not testing the player's
defence so much as their damage. That is a defensible thing for an opening position to do and a
much less defensible thing for position 4 to still be doing.

**Fast loss (2, 3, 5).** The Fairway dying in 42 seconds with a single kill is not a bug: it is the
authored spike, the one position with `pillarExceptions` on record, "an open fairway with nothing to
funnel with at all". It is behaving exactly as written. Positions 2 and 3 have no such excuse on
file and are the two worth a human's attention first.

**Crowbar (6)** is the only position in the campaign that cleared a wave against this bot, which was
the opposite of what I expected from a centred objective with six approaches. Worth keeping in mind
before anyone "fixes" its difficulty.

## The frozen wave (found 2026-09-13, partly fixed)

Funding the bot properly (`-exodus-autoplay-cash 6000 -exodus-autoplay-turrets 10`) so it could
build a defence that actually holds produced the worst result in the project so far, and it is not
a harness artifact:

> Crowbar, wave 1. Truck untouched at 30/30. Forty of forty-two down by t=60s. Then **six minutes
> in which the kill count moved by one.** Two bodies stood behind a barricade run no turret
> covered, the wave could not clear, and the mission could not end.

The designed answer to a player wall is the Sapper. It could not come. Sappers are issued only by
`SpawnDirector.Decide`, which runs **once per spawned body** — so the counter to a wall can only be
issued *while a wave is spawning*. A wall built during setup stops the wave; the stopped wave never
dies; no next wave means no spawn stream; and the Sapper that exists precisely to answer that wall
is never issued. **The wall had made itself uncounterable by working.**

`BreakStalledWave` closes that circle: a wave that has finished spawning, still has bodies up, and
has gone `StallSeconds` (14s) with no kill *and* no truck damage gets a Sapper sent from a main
gate. Capped at **three per wave** — it is a deadlock breaker, not a difficulty knob, and uncapped
it sends roughly twenty extra bodies across five minutes, which is a second wave nobody asked for.

**What this does not fix.** Progress resumed (kills went 40 → 49 where they had gone 40 → 40), but
the wave still did not clear. After the cap is spent the game logs a loud warning rather than
freezing quietly — and that warning now **names every body still up**, which finally produced a
diagnosis instead of another guess:

```
[Stall] id  2 Runner/HuntStructure at (79.1,56.5) hasPath=True  wall=None
[Stall] id 21 Runner/WreckWall     at (56.0,40.1) hasPath=True  wall=Barricade
[Stall] id 24 Runner/WreckWall     at (46.1,50.0) hasPath=True  wall=Barricade
[Stall] id 44 Sapper/Vault         at (43.0,42.0) hasPath=True  wall=None
```

**Every one of them has a path.** The freeze was never about sealed bodies, which is what the first
two attempts at it assumed. Three separate causes, in descending order of blame. **Two are now fixed** — and they turned out
to be the same bug wearing different clothes, which is the part worth remembering: *every
specialised behaviour in this sim returned `true` unconditionally and had no way to fail.* An
errand with no exit is a body that can never be resolved.

1. **FIXED — `StepStructureHunter` had no give-up.** It acquires the nearest turret within
   `HunterAcquireRange`, and while the distance exceeds `HunterContactRange` it steers and returns
   `true` — *every tick, forever*. A hunter with a barricade between it and the gun slides along
   the wall and never closes, never falls through to the objective, and never dies. This is the
   main offender and the next thing to fix.
2. **FIXED — wrecking was a career, not an errand.** `StepWallWrecker` returned `true` for as long
   as *any* wall sat within `WreckerSearchCells`, with no completion. On a position where the
   player has built fifty barricades, a wrecker demolishes the neighbourhood for the rest of the
   match and never once goes at the truck. The field was not stuck, it was **busy**. The errand now
   ends when the hole is open — go through it, that was the point — with a generous patience budget
   for the wrecker that is getting nowhere. Wave one went from never clearing to clearing, and
   kills over the same 420s went 41 → 108.
3. **OPEN — a Spitter and a Sapper that do not arrive.** With the first two fixed the freeze now
   happens in wave *two*, with exactly two bodies left:

   ```
   [Stall] id  44 Spitter/Vault at (78.0,64.8) hasPath=True
   [Stall] id 108 Sapper/Vault  at (36.1,52.1) hasPath=True
   ```

   Both pathed, both stationary, both at the ARCHETYPE layer rather than the intent layer, so the
   errand budget does not reach them. `StepSpitter` re-acquires the nearest turret every tick and
   approaches; with fourteen guns on the map one is always in range, so it can approach forever.
   It does have a wall fallback, but that only fires on *zero* movement — a body sliding
   tangentially along a barricade keeps moving and never trips it.

   Given three instances of one bug, the next fix should probably be structural: a no-progress
   watchdog in `AgentWorld.Step` that sends any body back to the objective when it has not changed
   cell for a while, rather than a fourth bespoke give-up. It needs care — a wrecker legitimately
   stands still for ~27s breaking a 200hp wall, and a Sapper stands still while planting — so the
   watchdog has to reset on *doing damage*, not only on moving.

A sim change did land alongside this diagnosis and is worth keeping on its own merits: a body whose
cell has no path now falls back to wrecking, because `DirectionAt` on an unreachable cell returns a
zero vector and such a body previously followed nothing at all. It has tests
(`SealedBodyTests`). **It does not fix the freeze**, because the frozen bodies were never sealed.

## Known blind spots

- **Sapper planting is still unobserved.** `-exodus-autoplay-cash` now lets the bot buy a real
  wall (52 barricades spanning four streets), and sappers do reach it — but none has ever planted,
  because `PlanSapper` only targets a wall whose breach SAVES WALKING, and the bot's four straight
  runs leave the diagonals open, so walking around stays cheap. That is the sim being right and the
  bot being a poor imitation of a player, who walls a choke rather than a compass rose. Mission 6's
  premise remains unverified by machine.
- **Sappers plant only when breaching SAVES WALKING** (`PlanSapper` scores walk-to-wall plus the far
  side's integration cost against the route already available). A barricade you can walk around is
  one no Sapper will look at. Crowbar's director chose three Sappers and every one of them targeted
  nothing, which is correct behaviour against a wall that blocks nothing.
- **Spitters only appeared in Crowbar**, because the other positions' `spitterFirstAt` lands after
  their wave-one spawn window closes. See CLAUDE.md on spawn windows: these thresholds are sampled
  on spawn events, never between waves.
