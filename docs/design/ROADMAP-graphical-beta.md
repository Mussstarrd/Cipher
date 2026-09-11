# Road to a playable graphical beta level

**Date:** 2026-09-11 · **Author:** ENG · **Status:** plan, dates are commitments
**Answers:** the owner's question, *"when am I going to be able to play an actual graphical beta level"*
**Governed by:** [ADR-007](../decisions/ADR-007-crowd-rendering-and-art-lane.md)

## The honest position

Everything under the hood is further along than the picture is. The simulation runs a thousand agents at a
thousand frames per second, the breach and repair systems work, towers are a data catalogue, the economy and
the scan-cycle loop are built and tested. What is missing is **models**. Not shaders, not lighting, not
performance headroom. Models.

That is a supply problem with a known answer and a known price, not an open research problem.

## The target

**One playable level that looks like the concept art's world**: Mission 1, The Gate. Brick entry pillars, a
guardhouse, a boom barrier, welded scrap across the road, bare hardwoods and pines crowding both sides,
overcast winter light, and a crowd of ordinary clothed people walking up the road at you.

Playable end to end with the scan-cycle loop: hold, call your last wave, pack up, leave.

## Schedule

| Date | Milestone | Blocked on |
|---|---|---|
| **2026-09-11** | ✅ URP live, instanced shader, overcast winter lighting, Android variant explosion fixed | done |
| **2026-09-12** | Art packs purchased, roughly $135 | **owner** |
| **2026-09-18** | The Gate dressed: real houses, trees, road, gate, props. Agents still capsules. | packs |
| **2026-09-25** | VAT bake pipeline + crowd LOD. **Real people walking at you.** | packs |
| **2026-10-02** | Hero model and animation, the house, full environment dressing pass | — |
| **2026-10-06** | **Playable graphical beta of Mission 1** | — |

**The only external blocker is the purchase**, and it is about $135. Every date after it slips one-for-one
with it.

## What "graphical beta" will and will not mean

**Will:** a coherent, directed, recognisable world. Winter Virginia light, a real street, real houses, real
trees, a crowd of clothed people with an implant light at the temple, the hero as a character rather than a
capsule, the full loop playable.

**Will not:** match the photoreal concept renders. Those are a target for palette, staging, weather and mood,
not for model fidelity. See ADR-007 decision 2 — that is the trade, and it is the owner's to overrule.

## Why not photoreal

Because the crowd is the game. A thousand agents on screen forces low polycounts and one shared skeleton, and
the stylised lane gives us both for about $135 and three weeks. The photoreal route fragments across vendors
and rigs, costs an order of magnitude more, and would still need the same VAT work on top.

**The lighting is doing more work than the models here**, and the lighting shipped today.

## After the beta

Not part of this plan; listed so the beta is not mistaken for the finish line.

- Missions 2 through 12 dressed from the same kit, which is why ADR-004's contracting map matters.
- The interlude phase between positions: fortify, trap, scavenge, rest.
- Hacked humanoids and cargo drones as enemy classes.
- Couch co-op.
