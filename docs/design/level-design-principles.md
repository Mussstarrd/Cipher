# Level design principles

Written 2026-09-12, after the owner opened the map layout for redesign: *"the map layout does not
need to stay the same... let's go ahead and put the energy in to properly build levels that are not
only aesthetically pleasing but also strategically appealing."*

This is the doctrine. `docs/design/the-contracting-perimeter.md` is the twelve-position ramp and
stays in force; this document says what makes any one of those positions *good*.

---

## 1. The diagnosis: why The Gate played as one corridor

CLAUDE.md already recorded the symptom — *"Only ONE pinch gets used, because one flow field means
one cheapest path"* — and attributed it to the flow field. **That was wrong, and it cost us a
mission's worth of layout work aimed at the wrong thing.**

One vector field computed from the goal is the standard, correct implementation of this genre
(Red Blob Games' tower-defence pathfinding article describes exactly what we built). The field
picking one cheapest route is not a defect. The defect was that **the wave was never assigned
anywhere else**.

`DirectorConfig.FlankShare` defaulted to **0.08** and no scenario overrode it. The Gate authored
three real gates — the county road, a gap in the pool fence, the clubhouse back lawn — and then sent
92% of every wave through the front door. Eight percent is noise, not a second front.

**The rule that follows:** a second route exists only if the wave is *assigned* to it. Kingdom Rush's
Icewind Pass does not hope the pathfinder discovers its northern lane; it spawns half the wave up
there. Route variety is a **spawn-table** decision, never a pathfinding one.

This leaves the preview covenant (hard rule 4) completely intact, which matters — it was never the
thing in the way.

---

## 2. Pillars

**P1 — Walls shape fights; they are not a maze puzzle.**
There is a genuine, unsettled split in the genre: *Defender's Quest* deliberately refused mazing
because routing and shooting compete for the same attention, while the Desktop-TD lineage treats
routing as the whole skill. **Our player is also a body on the ground with a gun, so the choice is
already made for us.** Barricades are cover and funnels, in the Orcs Must Die sense. Nobody should
ever be rewarded for building a serpentine.

*Do not quietly reintroduce full mazing.* If it ever looks tempting, it needs an ADR.

**P2 — Every position has at least two live fronts.**
Not two routes on the map — two routes that *receive traffic in the same wave*. This is also the
only reliable anti-camping tool we have: the player is one body with one gun, so a second front
converts their own presence into a budget they must spend. Terrain tricks do not do this; spawn
assignment does.

**P3 — Every position contains at least one convergence point.**
The Brush Hog is `FireMode.Area` with a **2.4 m** reach. On a long straight lane it is strictly worse
than a Sentry, everywhere, all game — and a tower family that is never the right answer is a
documented sign of a badly balanced tower defence. Every map owes the area family one place where
lanes merge, double back, or funnel, and that place should be obvious to a player who has walked it
once.

**P4 — The approach must be visible from where you are asked to build.**
Not "leading lines" — the Level Design Book argues convincingly that those are folklore, since any
corridor produces perspective convergence whether you meant it or not. The real test is
informational: **standing on a buildable slot, can you see what is coming down the lane it covers?**
If you cannot, the slot is a guess, and guessing is not strategy.

**P5 — Flat ground makes placement obvious, and obvious is boring.**
Elevation changes, turns and line-of-sight breaks are what turn "where do I put the Sentry" into a
decision. A porch roof, a retaining wall, a two-storey house with a reachable upper floor: each one
creates a real trade against the flat slot beside it. A flat two-lane street — which is what The Gate
currently is — is the textbook boring case.

**P6 — Two or three landmarks per position, visible from the buildable footprint.**
A landmark only works if it contrasts with what is *immediately around it*. The water tower, the
clubhouse, the boat ramp, a collapsed carport. The goal is that players name the lanes themselves —
"the water-tower side" — because a map players can name is a map they can plan on.

---

## 3. Escalating twelve positions through layout, not statistics

The owner's enemy-count direction ("less zombies... more robust — quality over quantity") already
rules out difficulty-by-inflation. Layout levers, in the order we should spend them:

1. **Shrink the defensible footprint.** Free narrative alignment: a retreat *means* less ground each
   time. Track buildable slots per position and make the number trend down.
2. **Add approach vectors.** One lane, then two, then two plus a rear opened by a wrecked wall.
3. **Degrade chokepoint quality rather than buffing enemies.** Later positions get *shorter*
   engagement lanes — less time-on-target per turret — and partial sightline obstruction, so part of
   the approach is hidden even though it is the same neighbourhood.
4. **Reopen familiar ground.** By position 6 or 7 the player believes they know this place. A road
   they barricaded in position 2 gets blown open in position 9. Fair, because the shared flow field
   telegraphs it; cruel, because they had stopped watching it.
5. **Spend the time budget.** ADR-005's one budget across fight, pack-up and prep is already a
   difficulty knob that touches no geometry at all.
6. **New terrain rules, not new enemy numbers.** A lake edge machines will cross and people will not
   changes every waterside position afterwards.
7. **A breather.** Kingdom Rush puts one at level 5 and a deliberately brutal spike at level 8. A
   retreat with no let-up reads as a grind rather than a tightening.

---

## 4. Traps to stay out of

- **A lane that is obviously better than the others.** The whole point of P2.
- **Sealing the map.** Walls are buildable and destructible, so a placement that severs the last
  legal route must be refused (`BuildValidator` is the seam, and `ShippedScenarioTests.PropsNeverSealAGate`
  already guards the authored case). Verify the *player-built* case the same way.
- **Juggling** — selling and rebuilding a wall mid-wave to flip which route is cheapest, herding the
  whole crowd back and forth. The genre is split on whether this is skill or exploit. Our pack-up
  economy discourages it between waves; confirm it cannot be done *during* one.
- **Silent reroutes.** Recomputing the field when a wall drops makes every enemy on the field turn at
  once. That is a tactic, but it reads as a glitch unless it is telegraphed.
- **Two new ideas in one position.** Stagger new terrain rules, new intents and new turrets.
- **Yard clutter that buries the sightline.** Easy to introduce by accident once a residential street
  gets dressed. Police it against P4.

---

## 5. Status

**Done (2026-09-12):**
- `flankShare` and `flankPaceScale` are authorable per scenario, validated 0..1, and
  **The Gate is set to 0.34** — roughly a third of every wave now arrives through the pool fence or
  the clubhouse lawn instead of the front door. P2, on the position that needed it most.

**Next, in order:**
- Audit all three shipped positions against P3 (a convergence point for the Brush Hog) and P4
  (sightline from every buildable slot).
- Elevation (P5): nothing in the game is currently above ground level except roofs nobody can reach.
- Landmarks (P6): the water tower does not exist yet.
- Confirm the player-built full-seal case is refused.

## Sources

Kingdom Rush campaign level design and Defender's Quest's fixed-path rationale (Game Developer);
Red Blob Games on flow-field tower defence; Orcs Must Die blocking strategies (Ludus Novus); the
Level Design Book on composition; Maul Tactics' mazing guide for the degenerate cases.
