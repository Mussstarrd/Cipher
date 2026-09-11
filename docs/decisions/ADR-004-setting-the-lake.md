# ADR-004 — The setting is a gated lake community, not a city

**Status:** Proposed (owner supplied the setting, ENG developed it) · **Date:** 2026-09-10
**Amends:** `docs/design/campaign-act1.md`, `docs/02-FLAGSHIP-PITCH.md`
**Builds on:** [ADR-003](ADR-003-premise-the-cascade.md)

## Context

The owner gave us the setting directly, along with reference photographs:

> That's the guy, this is my house and community. Lake of the woods VA and we need some
> dark gritty shit too I loved this render

Lake of the Woods is a private gated community in Orange County, Virginia: several thousand
homes on wooded lots, one large lake, a golf course, a clubhouse and pool, and a small number
of controlled road entrances. It sits next to the Wilderness battlefield.

Everything before this ADR assumed a generic city. That was a placeholder nobody had ever
argued for.

## Decision

**Act One happens inside one gated lake community in rural Virginia**, and the campaign is the
story of losing it one piece at a time.

### 1. Why this is a better setting than a city, on the merits

This is not a sentimental choice. A gated community is a **tower-defense map that already exists
in the real world**, and it fixes four problems the city setting had.

| Problem with the city | What the lake community does |
|---|---|
| Why can't the horde just come from everywhere? | It can't. There are a handful of road entrances and one water flank. **The geography is the maze.** |
| Why does the player defend *this* block? | Because there is a gate, and behind it is everyone. The premise stops needing an explanation. |
| Cities need thousands of unique buildings | A planned community is **a few house models rotated and re-dressed**, which is honest to the real place and enormously cheaper. |
| Where does the horde come from, physically? | The woods. Every lot backs onto treeline, so pressure comes from the trees and the roads at once, which is a far better read than "down the street". |

The lake is the second flank and the escape route in the same body of water. The golf course is
open killing ground with no cover for anyone. The clubhouse is the obvious civilian strongpoint.
None of that had to be invented.

### 2. The community is fictionalised, and the reason is not legal caution

The in-game community is **not named Lake of the Woods and not mapped house-for-house.** Working
name: **Wilderness Lake**, which keeps the battlefield reference the real place has.

Two reasons, in order of importance:

1. **Real people live there.** Shipping a horror game whose map is the actual street layout of a
   real neighbourhood puts strangers' homes in a game about being overrun. That is not ours to do.
2. It frees level design to move the golf course, tighten the gates and shorten the sightlines
   for play, which a faithful copy would forbid.

**What we keep exactly:** the geography type, the architecture, the tree cover, the road
hierarchy, the lake, the clubhouse, the golf course, the winter Virginia palette. The concept
pass in `art/concept/lotw-*.png` is the target and it works.

**The owner's own house is in the game** as the player's home lot, dressed from his reference,
because that one is his to give. Its street address appears nowhere in this repo.

### 3. What this does to the campaign

The twelve-mission structure in `campaign-act1.md` survives. The settings change and get better:

| Was | Becomes |
|---|---|
| Your bodega, one street | **Your house and driveway.** The garage is the safe zone, which it already was in the concept art. |
| Your chop shop | **Your street**, the four neighbours who are also unchipped |
| Suburban backyards | Unchanged. It was already this. |
| Power yard (mission 5) | **The community substation and pump house.** Lose it and the whole peninsula goes dark, which is mission 8. |
| The freeway, unwinnable | **The main gate.** You never hold the gate. You buy the time to fall back to the interior. |
| The Vory's armoury road | **The sheriff's substation / the gun club.** |
| The laboratory (mission 9) | A **county research facility** off the highway, the only mission outside the gates. |
| Dead Turf, the finale | **The clubhouse and the boat ramp.** You hold the ramp while the pontoons load. You are scored on boats away. |

That last one is the strongest ending the campaign has had. Everybody leaves by water, and the
thing you are defending is a queue.

### 3b. The act is a fighting retreat, and the map contracts

**Added 2026-09-10.** The owner supplied the campaign's spine the same day he supplied the setting:

> maybe the storyline starts with trying to secure the gate and we are progressively pushed further and
> further back until ultimately I'm at the house

This is now the structure of Act One and it is the single best structural idea the project has had. It
converts the setting from a backdrop into the story. Detail in `docs/design/campaign-act1.md`; what matters
at the ADR level is that it changes what we build:

- **The community is authored once and each mission is a smaller sub-rectangle of it.** Twelve missions cost
  closer to one level than to twelve. This is a large, real saving and it arrives for free with the fiction.
- **Every location is seen twice**, once held and once being given up, which doubles the value of every prop
  we make.
- **Damage persists across missions.** Already supported by the destructible-terrain work; now it is load
  bearing rather than a nice touch.
- **Kit does not follow the player.** A new mission-end recovery multiplier means a clean hold funds the next
  line and a ragged one does not. That is the mechanic that makes retreating cost something without ever
  showing the player a fail screen for doing what the plot requires.
- **The owner's house is the final mission**, not the first. It is the only location the player defends with
  nothing behind it.

### 4. Half in daylight is now doubly right

Winter Virginia overcast is the palette: bare hardwoods, pine, brown leaf litter, grey sky, old
snow. It is cheap to render, it is specific, and it looks like nothing else in the genre, which
is wall-to-wall night-time neon. The dark set exists for specific missions and lands harder for
being rationed.

## Consequences

| Area | Effect |
|---|---|
| **Art** | Prop and building budget drops hard. One house kit, four roof types, two garage types, dressed per lot. Trees carry the frame. |
| **Level design** | Maps are now derived from a real plan type rather than invented. Road hierarchy gives us natural chokepoints without hand-placed walls. |
| **Sim** | No change. Flow field, breaches and gates are setting-agnostic. Treeline as a spawn edge is already expressible. |
| **Tone** | The horror gets closer to home, which is the entire point of ADR-003. These are your neighbours and this is your cul-de-sac. |
| **Title** | "Dead Turf" gets worse here, not better. Turf reads gang, and this is not gang ground. Flagged again below. |

## Open questions for the owner

1. **Is "Wilderness Lake" the right fictional name**, or do you want the real one used? Your call,
   but my recommendation is the fictional one and the reason is the neighbours, not lawyers.
2. **The title.** Second ADR in a row where it comes up. The setting now suggests something about
   water, winter, gates or holding. Say the word and I will pitch ten.
3. **How much of your actual house do you want in it?** I can go as far as the floor plan if you
   send it, or stop at the exterior we already have.
