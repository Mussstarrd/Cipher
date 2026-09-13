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

**The Brush Hog finally has a home.** The centre of a crossroads is a convergence point by
definition. Every later position owes the area family the same courtesy.

**Camping is no longer a solution.** Any lane the player stands in is three lanes they are not
standing in. Their own presence becomes a budget, which is exactly the property the emplacement
budget already has.

**The preview covenant is untouched.** This is geometry and spawn assignment; nothing about the flow
field, `BuildValidator`, or hard rule 4 changes.

**Every other position inherits the question.** `act1-02-the-service-road` and
`act1-03-the-pump-house` still place their goals the old way. They are not wrong yet — a fighting
retreat can legitimately include a position with its back to something — but a *default* of
back-wall placement is now a bug, and `docs/design/the-contracting-perimeter.md` should be re-read
against this before position 4 is authored.

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
