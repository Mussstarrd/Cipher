# ADR-012 — The truck is in the middle

**Status:** Accepted (owner directive, 2026-09-12)
**Supersedes:** nothing. **Amends:** the layout assumption behind every shipped position.

## The directive

> "It just dawned on me. The thing that made Dungeon Defenders so awesome was the centralized
> protection point — the home base crystals were always like centered in the map. I feel as if our
> safety truck should maybe be the same."

## Context

The Gate placed the truck at **x = 124 on a 128-wide map** — hard against the back wall — with the
county road entering at x = 2. That is not a defended objective; it is a corridor with a prize at
the end, and it produced three problems we had been treating as separate bugs:

1. **One lane did all the work.** Fixed separately by assigning the wave across gates
   (`flankShare`), but the underlying shape was still a single axis of attack.
2. **Camping was optimal.** The player is one body with one gun. Against a single approach, standing
   in the pinch is simply the correct play, and no amount of enemy tuning changes that.
3. **The Brush Hog had nowhere to be.** A 2.4 m area weapon on a long straight lane is strictly
   worse than a Sentry, everywhere, forever.

An objective at the centre answers all three with geometry instead of rules, which is why Dungeon
Defenders' crystal works and why Kingdom Rush's Icewind Pass — enemies from north and south at
once — is remembered as the campaign's difficulty spike. Independent research commissioned the same
evening reached this conclusion from the other direction, before the owner raised it: multi-lane
convergence is the single most relevant pattern for a hybrid where the player is also a unit.

## Decision

**The goal sits at the centre of the position, and approaches converge on it from several sides.**

On The Gate, as of this ADR:

| | Before | After |
|---|---|---|
| Truck | (124, 48) — back wall | **(64, 48) — the crossroads** |
| Hero spawn | (112, 48) | (64, 44) |
| Main gates | county road (west) | county road (west) **+ the back road (east)** |
| Flank gates | the pool, clubhouse lawn | unchanged — now genuinely north and south of the goal |

Four approaches. With `flankShare` at 0.34 the wave divides roughly **33 / 33 / 17 / 17**.

## Consequences

**Intended, and large.** The truck is now about half as far from every spawn and takes traffic from
four directions instead of one. A capture run with **no turrets built** ended with the truck at
**4/25** and the player down. That is the mechanic working, not a balance failure: this is a tower
defence, and the answer to convergent pressure is emplacements. But it does mean **mission one's
numbers are now wrong until someone plays it**, and tuning them by intuition rather than by playing
would be guessing. Flagged, not fixed.

**The Brush Hog has a home, but a smaller one than this ADR first claimed.** "The centre of a
crossroads is a convergence point by definition" is true and was the wrong thing to be satisfied by.
Measured (`EveryPositionOwesTheAreaTowerAMergePoint`, 2026-09-13), The Gate's best merge is **two of
four gates, seven cells from the truck** — against the Service Road's forty-nine and the Pump House's
eleven with three of four. So centring the goal bought convergence AT the objective and almost none
before it: four approaches that stay four approaches until the last moment.

That is not a failure of ADR-012 — an area weapon at the truck is a real, correct last line — but it
does mean The Gate is currently the WEAKEST position for the area family of the three shipped, which
is a strange property for the tutorial-adjacent one. Recorded rather than patched: the fix is layout
(funnel two approaches together before they arrive) and layout on the opening position is worth an
owner's eye rather than another unattended edit.

**Camping is no longer a solution.** Any lane the player stands in is three lanes they are not
standing in. Their own presence becomes a budget, which is exactly the property the emplacement
budget already has.

**The preview covenant is untouched.** This is geometry and spawn assignment; nothing about the flow
field, `BuildValidator`, or hard rule 4 changes.

**Every other position inherited the question, and it is answered (2026-09-12).**
`act1-02-the-service-road` and `act1-03-the-pump-house` both placed their goals at 96% and 93%
across their maps, with every gate on the west edge — the same corridor The Gate was, and both with
`flankShare` unset at the 0.08 default, so their authored flanks were noise.

They were **not** centred, deliberately. `the-contracting-perimeter.md` already asks position 2 for
four approaches and position 3 for five, and both already had the gates; what they lacked was
traffic through them and a goal that the approaches could converge *on*. The goals moved in to **69%
and 65%** rather than to 50%: far enough from the wall that four gates genuinely surround them, still
biased along the direction of retreat, and different from The Gate's dead centre — because twelve
identical crossroads is the failure mode this ADR warns about two paragraphs down. Flank shares set
to 0.30 and 0.36, scaled to their approach counts.

**The written brief was wrong and is rewritten.** The Gate's brief told the player "there is one
way through this end of the community that is shorter than walking round... close it, and make them
take the long way." That was true of the corridor and is false of the crossroads: closing one lane
now sends the wave to the other three rather than the long way round. It is the first thing a player
reads, so a stale brief is not a cosmetic problem — it teaches the wrong game. Rewritten to teach the
shape instead: one gun, four ways in, build for the three you are not standing in.

**The chase camera now has buildings on every side, and it is fixed (2026-09-13).** Parked at the
back wall the truck had open ground behind it and the rig could swing freely. At the crossroads it is
surrounded, so the camera regularly ended up inside a house. Repro:
`-exodus-screenshot-yaw 0 -exodus-screenshot-pitch 12`.

The fix marches the hero→camera line through the grid and pulls the rig in short of any
`WallKind.Rock` cell, reusing `Movement.FirstBlockedCell` — the same march that decides whether a
turret can see a target, so "is something in the way" keeps one definition. A physics cast was never
available: the bought buildings are imported meshes with no colliders, so a `SphereCast` reports
clear straight through a house. Only Rock counts; a knee-high barricade must not yank the camera.

**The part that cost two increments is the skin, and it is about ROOFS, not walls.** A footprint is
the wall line, and Synty's eaves overhang it by the better part of a metre. At a 0.55 m skin the rig
sat at y=38.3 with the wall at 37.5 — correctly outside the footprint by every measure, and directly
underneath the roof, which renders as the dark inside of a building and looks exactly like the bug it
was meant to fix. The diagnostic insisted the maths was right while the picture insisted it was
wrong, **and both were telling the truth**. The skin is 1.8 m, which also covers the ink outline,
since an inverted hull stands off the geometry and the visible edge of a building is always further
out than the building.

The lesson is the diagnostic, not the number: `-exodus-cam-diag` prints wanted / allowed / target /
actual every 120 frames. Printing the target AND the rig's real position is what separated "the pull
is not happening" from "the pull is happening and is not far enough", which two builds of reasoning
had not.

## What this does NOT mean

**Not every position is a rosette.** Twelve identical crossroads would be as monotonous as twelve
corridors. The contracting perimeter's job is that each position takes away something different —
approaches, frontage, depth, sight, permanence. Centre-and-converge is the default shape to deviate
*from*, not a template to stamp.

**The lake and the treeline still bound the map.** A position on the shore cannot be attacked from
the water. "Centred" means centred in the ground the player can actually be flanked across, not
geometrically centred in the rectangle.

## Verification

591 EditMode tests green, including `ShippedScenarioTests.PropsNeverSealAGate` and the reachability
checks that would have caught a goal placed inside a building or behind a sealed gate.
