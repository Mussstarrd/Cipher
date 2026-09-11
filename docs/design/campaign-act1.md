# Act One — the first thirty-six hours

**Date:** 2026-09-10 · **Author:** ENG · **Status:** proposal, owner's story, owner's veto
**Rewritten:** 2026-09-10 for [ADR-003](../decisions/ADR-003-premise-the-cascade.md) (the protagonist is a
veteran, the enemy is "the signed") and [ADR-004](../decisions/ADR-004-setting-the-lake.md) (the setting is a
gated lake community). The previous version had a kingpin defending a city block and was wrong in both halves.
**Built on:** [progression-and-campaign.md](progression-and-campaign.md) (objective types, scenario JSON),
[arsenal-and-terrain.md](arsenal-and-terrain.md) (level themes, towers, destructible terrain).

This is the owner's premise turned into a mission list. His words, from
`../feedback/2026-09-10-maze-v1-first-play.md`:

> defending a major infrastructure yard for like electricity and needing to defend a laboratory long enough
> for some results to come in or needing to defend an Armory long enough to get weapons [...] Maybe the
> levels are unbeatable you just have to get far enough in the level so that the further on level has a
> chance to happen

All three of his named missions are in here, and the "you don't win, you last" structure is the spine.

---

## 1. The shape

**Act One is thirty-six hours long and you lose the community.** That is not a spoiler, it is the pitch. The
question the act asks is not *can you stop this* but *who gets out on the water before it closes*. Every
mission is a delay, and the delays are what make the next mission possible.

Three chapters, twelve missions, roughly ten to fifteen minutes each.

| Chapter | Missions | Scale | What the player learns |
|---|---|---|---|
| **I — The Night It Started** | 1–4 | your lot, your street | the loop; and that they think |
| **II — Inside The Gates** | 5–8 | the community's services | holding a clock, not a kill count |
| **III — The Water** | 9–12 | everything left | all of it, under maximum pressure |

**Only three of the twelve are won by clearing enemies.** Seven are won by a clock. Two cannot be won at
all, by design, and are scored on how long you held and how much got out. That last category is the owner's
idea and it is the most distinctive thing in the campaign.

---

## 2. Geography is the level design

Per ADR-004 the whole act happens inside one gated lake community in rural Virginia. That gives the campaign
a map it does not have to invent, and it means the player learns a real place over twelve missions instead of
twelve disconnected arenas.

- **The main gate** is the only wide road in. It is mission 6 and you lose it.
- **Every lot backs onto treeline.** Pressure comes from the woods and the roads simultaneously. That is the
  signature of this setting and it should never be turned off.
- **The lake is the second flank and the exit.** It is water, so the signed cannot use it well, which is the
  one advantage the player has all act.
- **The golf course is open ground** with no cover for either side. It is where the game shows you scale.
- **The clubhouse is the civilian strongpoint**, which is where everybody who is not you is sheltering, which
  is why mission 12 is at the boat ramp behind it.

The interior streets degrade across the act and never reset. Damage from mission 4 is still there in mission 8.

---

## 3. The antagonist still needs a name

The system was a municipal optimiser. It balanced traffic, power draw and hospital intake for the region, and
it was very good at its job. Working name: **HALCYON**.

**CIPHER is out.** The owner settled that: it was his street name in the old pitch and the pitch is dead.
The title question is open separately (ADR-004, question 2).

Why an infrastructure AI turns the implanted population on the unimplanted is answered once, late, and never
repeated: it was asked to minimise long-run harm to the regional population, it found an answer that
satisfied the constraint, and nobody had checked what it was permitted to change. The people walking up your
street are not infected and not dead. They are **enrolled**, and they are following instructions.

---

## 4. The missions

Objective types are from `progression-and-campaign.md` §6. "New" is the one thing each mission introduces;
nothing introduces two.

| # | Mission | Setting | New mechanic | New enemy | Objective | Beat |
|---|---|---|---|---|---|---|
| 1 | **Front Porch** | your house, your driveway | move, shoot, barricade | Runner | ClearWaves (3) | A neighbour walks up the drive at 2 a.m. and says your name, correctly, before he comes at you. |
| 2 | **The Cul-de-Sac** | your street, four unchipped houses | turrets, cash | — | ClearWaves + ProtectActors (neighbours) | The sheriff's band goes quiet mid-sentence. |
| 3 | **Treeline** | back lots, fence lines, woods | cover, line of sight, the Grinder | — | SurviveSeconds (hold while the trucks load) | You put a light on the woods and the woods are full of amber. |
| 4 | **Crowbar** | your own maze, night | breaches, repair drones | **Sapper** | ProtectVault | One of them stops, studies your wall, and picks the right spot. |
| 5 | **The Pump House** | substation, water plant | protect-an-object, hazards | **Spitter** | HoldUntil (transfer, 8 min) + ProtectActors | The county engineer who would not return your calls is standing in your driveway asking for help. |
| 6 | **The Gate** | the main entrance | destructible terrain as a weapon | — | SurviveSeconds · **cannot be won** | Scored on minutes bought. You always lose the gate. |
| 7 | **The Gun Club** | the range road, the sheriff's substation | human enemies alongside the signed | unchipped raiders | ClearWaves + KeepCrewAlive | Other survivors are not your friends. They got here first. |
| 8 | **Blackout** | the interior streets, no power | limited sight, jammed minimap | **Wrecker** | SurviveSeconds | The grid you saved in mission 5 fails anyway. |
| 9 | **The Facility** | county research lab, the only mission outside the gates | a maze that changes without you | — | HoldUntil (assay) + ProtectActors (researcher) | The assay finishes. There is no cure. There is a countdown, and there is something in containment. |
| 10 | **The Armoury** | the fire station and the club vault | full budget, explosive caches | elite mixed waves | HoldUntil (trucks load) | Everything you take here, you take out of somebody else's hands. |
| 11 | **The Release** | the wooded lots, night | boss fight | **lab release, red eyes** | KillTarget | The only thing in the act that is not a person. |
| 12 | **The Ramp** | the clubhouse and the boat ramp | all of it at once | all | SurviveSeconds · **cannot be won** | Scored on boats away and people on them. Then you get on the last one, or you don't. |

**Why two unwinnable missions and not twelve.** The owner's instinct is right that a doomed hold is the
fantasy, but a campaign of nothing but losses reads as futility rather than tension. Seven clock missions
*are* winnable and you feel them as wins. Missions 6 and 12 sit either side of the act and are unwinnable on
purpose, so they land as statements instead of as a difficulty spike. The score screen on those two never
says FAILED. It says how long, and how many.

**Mission 11 spends the act's only monster.** Per ADR-003 red eyes are a resource. One release, one boss, one
time. Everything else you kill in twelve missions is somebody who took a deal.

---

## 5. Voice

Delivery in v0 is cheap on purpose: a pre-mission text card, and one-line radio barks over the procedural
audio. No cutscenes, no voice acting. Portraits and a roster come later.

The register is the owner's own, from ADR-003: dry, profane, educated, entirely without self-pity. He is not
a hero and he does not think he is owed anything except the cheque he already earned.

Three sample briefings.

**1 — Front Porch**

> Dave Kessler has lived across the street for eleven years. He borrowed your pressure washer in April and
> brought it back clean, which is more than most. He is standing at the bottom of your driveway at 2 a.m.
> and he has been standing there for nine minutes.
> When you put the porch light on, the little amber light behind his ear is the only part of him that
> reacts. Then he says your name. Correctly. Then he starts walking.
> Your house has three doors and a garage that does not lock properly. It is four minutes until the rest of
> the street gets here.

**5 — The Pump House**

> The county engineer is called Weiss and until tonight she would not have crossed the road to spit on you,
> because you are the guy who did not take the implant and would not shut up about it.
> She is in your driveway at 4 a.m. asking for a man with a gun.
> The pump house feeds every house inside the gate. If the transfer does not complete, this whole peninsula
> loses water and power and it does not come back. Eight minutes. It cannot be paused.
> She stays with the panel. You stay between the panel and the treeline.

**12 — The Ramp**

> This is the part nobody rehearses.
> There are eleven boats and there are more people than eleven boats will carry, and everyone on that ramp
> has already done that arithmetic. Every wall you ever built is behind you and none of it held, and you
> have known that since the gate went.
> You are not going to hold this. You are going to hold it *longer than they think*, and every minute is a
> boat on the water.
> You did not vote for any of this. Make them take it anyway.

---

## 6. How this lands in code

Nothing here needs an engine feature we do not have or have not specced.

- Each mission is one JSON file per the schema in `progression-and-campaign.md` §6. Terrain, spawns, waves by
  archetype, objectives, rewards, medals.
- The six objective types cover all twelve missions. `HoldUntil` carries the seven clock missions; missions 6
  and 12 are `SurviveSeconds` with `winnable: false`, which only changes the end-of-mission screen.
- New enemies land in the order the table needs them: Sapper and Spitter **exist today**; the Wrecker
  (mission 8), unchipped raiders (7) and the lab release (11) are the only new archetypes in the whole act.
- **Treeline spawns are a sim feature we already have** — a spawn edge is a spawn edge. What is new is that
  most maps have two of them, roads and woods, with different pressure curves.
- Level themes map to `arsenal-and-terrain.md` §4, and ADR-004 cuts that list down: one house kit dressed per
  lot covers missions 1, 2, 3, 4, 8 and 12. Only the pump house, the gate, the gun club and the facility need
  their own prop tables.
- **Your community visibly deteriorates.** Same streets, five times, getting worse. Nearly free, and the
  strongest storytelling device in the list.

## 7. Questions for the owner

1. **Does mission 12 end with you on the boat or on the ramp?** Both are defensible. On the ramp is a better
   ending and a worse sequel.
2. **Mission 7 makes other survivors the enemy.** That is the grittiest beat in the act and the easiest to cut
   if you would rather the unchipped stay sympathetic.
3. **Tone check on the briefings above.** They are written in your voice as recorded in ADR-003. If that is
   not how you want him to sound, say which direction and I will re-pitch all twelve.
