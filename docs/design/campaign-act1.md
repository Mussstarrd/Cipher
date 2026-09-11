# Act One — the fighting retreat

**Date:** 2026-09-10 · **Author:** ENG · **Status:** proposal, owner's story, owner's veto
**Restructured:** 2026-09-10 on the owner's spine. His words:

> maybe the storyline starts with trying to secure the gate and we are progressively pushed further and
> further back until ultimately I'm at the house

That is the act. Everything below is that sentence with mission numbers on it.

**Built on:** [ADR-003](../decisions/ADR-003-premise-the-cascade.md) (the protagonist is a veteran, the enemy
is "the signed"), [ADR-004](../decisions/ADR-004-setting-the-lake.md) (a gated lake community),
[progression-and-campaign.md](progression-and-campaign.md) (objective types, scenario JSON),
[arsenal-and-terrain.md](arsenal-and-terrain.md) (towers, destructible terrain).

The owner's three requested missions are all here: the power yard is mission 3, the armoury is 7, the
laboratory is 9. From his first playtest notes:

> defending a major infrastructure yard for like electricity and needing to defend a laboratory long enough
> for some results to come in or needing to defend an Armory long enough to get weapons [...] Maybe the
> levels are unbeatable you just have to get far enough

---

## 1. The shape: the map gets smaller every mission

**You never take ground back. You only decide how expensively you give it up.**

Mission 1 you are defending a gate on behalf of four thousand people. Mission 12 you are defending a garage
door on behalf of yourself. The road between those two things is one road, and the player drives down it
backwards for twelve missions.

| Chapter | Missions | What you are holding | What the player learns |
|---|---|---|---|
| **I — The Perimeter** | 1–4 | the whole community | the loop, and that the perimeter is a lie |
| **II — Falling Back** | 5–8 | the interior, the services | holding a clock, not a kill count |
| **III — The Last Street** | 9–12 | your street, then your lot | all of it, with nothing behind you |

**Why this is better than the version it replaces.** The previous draft had twelve missions in twelve places
and the player learned nothing about any of them. A contracting map means:

- **Every location appears twice.** Once when you hold it, once behind enemy lines while you fall back
  through it. The street you fortified in mission 2 is the approach road in mission 12.
- **The art budget collapses.** One community, dressed progressively worse. Not twelve arenas.
- **The tension is structural, not scripted.** The player can see the map shrinking on the mission select.
  Nobody has to narrate the stakes.
- **Losing is the verb.** The premise says the levels can be unbeatable and you just get far enough. A
  retreat makes that true *mechanically* instead of true *in two special missions*.

---

## 2. What "pushed back" costs you, in cash

This is the mechanic that makes the retreat hurt, and it is small to build.

When a mission ends you **abandon everything you built** on that map. How much of it you get back depends on
how the mission ended:

| How it ended | Kit recovered |
|---|---|
| Objective met with time to spare | 75% |
| Objective met, overrun at the end | 50% |
| Fell back under fire (the unwinnable missions) | 25% |
| — | |

So a clean hold funds the next line. A ragged one does not. The player is never punished with a fail
screen for retreating, because retreating is the plan. They are punished with a thinner wallet, which they
feel immediately and can do something about.

**This also answers the "why don't I just turtle at the house from mission 1" question**: because everything
you own is bolted to the ground you are standing on, and the money to build the last line comes from having
held the first ones well.

---

## 3. The missions

Objective types are from `progression-and-campaign.md` §6. "New" is the one thing each mission introduces;
nothing introduces two. Read the **Ground lost** column down the page: that is the whole story.

### Chapter I — The Perimeter

You still believe this is a thing that can be held.

| # | Mission | Where | New mechanic | New enemy | Objective | Ground lost |
|---|---|---|---|---|---|---|
| 1 | **The Gate** | the main entrance, night one | move, shoot, barricade | Runner | ClearWaves (3) | none yet |
| 2 | **The Service Road** | the back entrance nobody thought about | turrets, cash | — | ClearWaves + ProtectActors | the outer lots |
| 3 | **The Pump House** | substation and water plant | protect-an-object, hazards | **Spitter** | HoldUntil (transfer, 8 min) | the north shore |
| 4 | **The Gate Falls** | the main entrance again | terrain as a weapon | — | SurviveSeconds · **cannot be won** | **the perimeter** |

**Mission 1 beat.** Dave Kessler from across the street is on the wrong side of the gate at 2 a.m. and he
says your name, correctly, before he starts climbing.

**Mission 4 beat.** You are standing in the same spot as mission 1, with four times the kit, and it does not
matter. Scored on minutes bought and how many people got behind the inner line while you bought them.
The score screen does not say FAILED. It says how long, and how many.

### Chapter II — Falling Back

The perimeter is gone. You are holding the inside of your own neighbourhood.

| # | Mission | Where | New mechanic | New enemy | Objective | Ground lost |
|---|---|---|---|---|---|---|
| 5 | **The Fairway** | the golf course, first light, fog | scale | — | SurviveSeconds | the west course |
| 6 | **Crowbar** | your own maze, night | breaches, repair drones | **Sapper** | ProtectVault | the east streets |
| 7 | **The Fire Station** | the county truck bay, the club vault | full budget, explosive caches | elite mixed waves | HoldUntil (trucks load) | the commercial row |
| 8 | **Blackout** | the interior streets | limited sight, jammed minimap | **Wrecker** | SurviveSeconds | everything north of the clubhouse |

**Mission 5 is the scale mission.** Open fairway, no cover for anyone, ground fog, and they come out of it a
hundred metres wide. This is the shot in `art/concept/lotw-fairway-schnell-01.png` and it should be the first
time the player understands the number they are fighting.

**Mission 6 beat.** One of them stops, studies your wall, and picks the right spot. They are not a mob.

**Mission 8 beat.** The pump house you saved in mission 3 fails anyway, because HALCYON did not need to take
it, it only needed to wait.

### Chapter III — The Last Street

| # | Mission | Where | New mechanic | New enemy | Objective | Ground lost |
|---|---|---|---|---|---|---|
| 9 | **The Facility** | a county lab, outside the gates | a maze that changes without you | — | HoldUntil (assay) + ProtectActors | you are not there to stop it |
| 10 | **The Release** | the wooded back lots, night | boss fight | **lab release, red eyes** | KillTarget | the woods |
| 11 | **The Ramp** | the clubhouse and the boat ramp | evacuation under fire | — | HoldUntil (boats load) | the clubhouse |
| 12 | **Front Porch** | your house, your driveway, your garage | all of it at once | all | SurviveSeconds · **cannot be won** | — |

**Mission 9 is the only time you leave, and leaving is what loses the community.** The researcher has
something. You take a truck out through a gate you no longer control. The assay completes and there is no
cure, there is a countdown, and there is something in containment that does not stay in it. You come home to
a neighbourhood that fell while you were gone. This is the act break and it is the one mission that breaks
the retreat pattern, which is why it lands.

**Mission 10 spends the act's only monster.** Per ADR-003, red eyes are a resource. One release, one boss,
one time. Everything else you kill in twelve missions is a person who took a deal.

**Mission 11 is the goodbye.** Eleven pontoons, more people than eleven pontoons carry, and everyone on that
ramp has already done the arithmetic. You hold the ramp until the last boat is off the trailer. **You do not
get on it.** The mission is won. You have nowhere to go and you go home.

**Mission 12 is the whole game in one lot.** Your house, from the reference the owner gave us, with every
dollar of recovered kit bolted to it. The approach road is the street from mission 2. The treeline behind
the garage is the woods from mission 10. It cannot be won and the player has known that since mission 4.
Scored on minutes held. Title drop.

---

## 4. Where the two unwinnable missions sit, and why there

Missions **4** and **12**, and the placement is deliberate.

Mission 4 is early enough to **teach the grammar**: you can lose ground and still be winning. If the first
unwinnable mission arrives at the finale it reads as the designer cheating. Arriving at mission 4 it reads as
the premise telling the truth, and every clock mission after it is played by someone who understands what
they are actually buying.

Missions 1, 2, 3, 5, 6, 7, 8, 9, 10 and 11 are all winnable and all feel like wins. Ten out of twelve. The
act is not futile, it is expensive.

---

## 5. Voice

Delivery in v0 is cheap on purpose: a pre-mission text card, and one-line radio barks over the procedural
audio. No cutscenes, no voice acting.

The register is the owner's own, recorded verbatim in ADR-003: dry, profane, educated, entirely without
self-pity. He is not a hero and does not think he is owed anything except the cheque he already earned.

**1 — The Gate**

> There are two ways into this place and one of them is this gate, which is a boom barrier a determined
> ten-year-old could lift. For nine years that was fine, because the thing it was keeping out was
> solicitors.
> Kessler from number 40 is standing on the other side of it. He borrowed your pressure washer in April and
> brought it back clean, which is more than most. He has been standing there nine minutes.
> When you put the light on him, the only part of him that reacts is the little amber light behind his ear.
> Then he says your name. Correctly.
> Weld it shut. You have about four minutes.

**4 — The Gate Falls**

> Everything you have built is at this gate and it is not going to be enough, and the difference between you
> and the county is that you already know that.
> Behind you, eight hundred people are moving furniture into the clubhouse at a speed that suggests they do
> not know it yet.
> You are not holding the gate. You are selling it. Get the price up.

**12 — Front Porch**

> This is the part nobody rehearses.
> The boats are gone. The clubhouse is gone. The gate has been gone since Tuesday. What is left is a tan
> contemporary on a wooded lot with a two-car garage that does not lock properly, and you have spent four
> days turning it into the only thing standing between the treeline and a man who would not take their money.
> They are coming up the street you fortified in the first week. You know every yard of it, because you built
> it, and then you lost it.
> You did not vote for any of this.
> Make them take it anyway.

---

## 6. How this lands in code

Nothing here needs an engine feature we do not have or have not specced.

- Each mission is one JSON file per the schema in `progression-and-campaign.md` §6.
- The six objective types cover all twelve missions. `HoldUntil` carries the clock missions; 4 and 12 are
  `SurviveSeconds` with `winnable: false`, which only changes the end-of-mission screen.
- **The kit-recovery table in §2 is the one new system**, and it is a multiplier on the existing `Bank`
  applied at mission end. It needs a unit test per row and nothing else.
- New enemies land in the order the table needs them: Sapper and Spitter **exist today**; the Wrecker
  (mission 8) and the lab release (mission 10) are the only new archetypes in the whole act. The previous
  draft's "unchipped raiders" are cut — see §7.
- **Treeline spawns are already expressible**; a spawn edge is a spawn edge. What is new is that most maps
  have two of them, roads and woods, with different pressure curves.
- **One map, twelve cuts of it.** The community is authored once. Each mission is a sub-rectangle of it with
  its own spawn edges and its own props, and the sub-rectangles get smaller. The art and level cost of this
  act is far closer to one level than to twelve.
- **Damage persists between missions.** A breach from mission 6 is still open in mission 8, and mission 12's
  approach road carries the wreckage of missions 1 through 11. Nearly free, and the strongest storytelling
  device in the list.

## 7. Changes from the previous draft, and one cut

- The act is now a retreat rather than twelve locations. Owner's call, and it is the right one.
- **"The Cul-de-Sac" and the neighbour-defence mission are absorbed** into missions 2 and 12.
- **Unchipped raiders are cut.** In a retreat there is no room for a second faction, and making other
  survivors the enemy fights the mission 11 evacuation, which only works if the people on the ramp are worth
  saving. They can come back in Act Two.
- The laboratory moves from a routine mid-act mission to the act break, and now costs the player the
  community rather than just time.

## 8. Questions for the owner

1. **Does mission 12 end?** Options: you die, you are last seen still shooting, or it hard-cuts on a timer to
   Act Two with no answer. My pick is the hard cut, because it is the only one that does not close the door.
2. **Do the neighbours have names?** Kessler costs nothing and pays off for twelve missions. If you want real
   names from your street in there, that is yours to give and I will not put any in without you saying so.
3. **The title.** Third document in a row where it comes up. "Dead Turf" was written for a gangster holding a
   city block. This act ends on a man holding his own front door. Say the word and I will pitch ten.
