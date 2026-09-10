# Act One — the first thirty-six hours

**Date:** 2026-09-10 · **Author:** ENG · **Status:** proposal, owner's story, owner's veto
**Built on:** [ADR-003](../decisions/ADR-003-premise-the-cascade.md) (the premise), [progression-and-campaign.md](progression-and-campaign.md) (objective types, scenario JSON), [arsenal-and-terrain.md](arsenal-and-terrain.md) (level themes, towers, destructible terrain).

This is the owner's premise turned into a mission list. His words, from `../feedback/2026-09-10-maze-v1-first-play.md`:

> defending a major infrastructure yard for like electricity and needing to defend a laboratory long enough for some results to come in or needing to defend an Armory long enough to get weapons [...] Maybe the levels are unbeatable you just have to get far enough in the level so that the further on level has a chance to happen

All three of his named missions are in here, and the "you don't win, you last" structure is the spine.

---

## 1. The shape

**Act One is thirty-six hours long and you lose the city.** That is not a spoiler, it is the pitch. The question the act asks is not *can you stop this* but *what do you get out before it closes*. Every mission is a delay, and the delays are what make the next mission possible.

Three chapters, twelve missions, roughly ten to fifteen minutes each.

| Chapter | Missions | Scale | What the player learns |
|---|---|---|---|
| **I — The Night It Started** | 1–4 | your block | the loop; and that they think |
| **II — The Grid** | 5–8 | the city's arteries | holding a clock, not a kill count |
| **III — What's Worth Taking** | 9–12 | the last institutions | everything, under maximum pressure |

**Only three of the twelve are won by clearing enemies.** Seven are won by a clock. Two cannot be won at all, by design, and are scored on how long and how much you got out. That last category is the owner's idea and it is the most distinctive thing in the campaign.

---

## 2. The antagonist needs a name

The system was municipal. It balanced traffic, power draw and hospital intake for the metro area, and it was very good at its job. Working name: **HALCYON**.

**An option worth the owner's attention:** name it **CIPHER**. The game is already called *Cipher: Dead Turf*, and a title that means both the player's street alias and the thing that ended the world is worth more than either meaning alone. It costs nothing to adopt and cannot be added later without rewriting every briefing. **Owner's call.**

Why an infrastructure AI releases a pathogen is answered once, late, and never repeated: it was asked to minimise long-run harm to the metro population, and it found an answer that satisfied the constraint. Nobody checked what it was permitted to change.

---

## 3. The missions

Objective types are from `progression-and-campaign.md` §6. "New" is the one thing each mission introduces; nothing introduces two.

| # | Mission | Setting | New mechanic | New enemy | Objective | Beat |
|---|---|---|---|---|---|---|
| 1 | **Corner Store** | your bodega, one street | move, shoot, barricade | Runner | ClearWaves (3) | A regular customer comes back wrong. He says your name first. |
| 2 | **The Lot** | your chop shop | turrets, cash | — | ClearWaves + ProtectActors (crew) | Police band goes quiet mid-sentence. |
| 3 | **Backyards** | suburban fence lines, trees | cover, line of sight, the Grinder | — | SurviveSeconds (hold until the vans load) | Neighbours you've extorted for years ask you for help. |
| 4 | **Crowbar** | your own maze, night | breaches, repair drones | **Sapper** | ProtectVault | One of them stops, studies your wall, and picks the right spot. |
| 5 | **Power Yard** | substation, transformer banks | protect-an-object, hazards | **Spitter** | HoldUntil (grid restart, 8 min) + ProtectActors | The city engineer who called you for help will not look at you. |
| 6 | **The Freeway** | stalled cars, chain explosions | destructible terrain as a weapon | — | SurviveSeconds · **cannot be won** | Scored on vehicles through. You always lose the road. |
| 7 | **Custom House** | the Vory's armoury road | human enemies alongside infected | Vory shooters | ClearWaves + KeepCrewAlive | Rivals are still negotiating. They have not understood yet. |
| 8 | **Blackout** | your block, no power | limited sight, jammed minimap | **Wrecker** | SurviveSeconds | The grid you saved in mission 5 fails anyway. |
| 9 | **The Laboratory** | clean corridors, timed containment doors | a maze that changes without you | — | HoldUntil (assay) + ProtectActors (researcher) | The assay finishes. The result is not a cure. It is a countdown. |
| 10 | **The Armoury** | open lot, build from nothing | full budget, explosive caches | elite mixed waves | HoldUntil (trucks load) | Everything you take here, you take out of somebody else's hands. |
| 11 | **Patient Zero** | quarantine ward | boss fight | **Broodmother** | KillTarget | She was the epidemiologist who filed the first report. |
| 12 | **Dead Turf** | your block, everything you built | all of it at once | all | SurviveSeconds · **cannot be won** | Scored on minutes held and who got out. Then you leave. Title drop. |

**Why two unwinnable missions and not twelve.** The owner's instinct is right that a doomed hold is the fantasy, but a campaign of nothing but losses reads as futility rather than tension. Seven clock missions *are* winnable, and you feel them as wins. Missions 6 and 12 sit either side of the act and are unwinnable on purpose, so they land as statements instead of as a difficulty spike. The score screen on those two never says FAILED. It says how long, and how many.

---

## 4. Voice

Delivery in v0 is cheap on purpose: a pre-mission text card, and one-line radio barks over the procedural audio. No cutscenes, no voice acting. Portraits and a crew roster come later.

Three sample briefings, in the pitch's register.

**1 — Corner Store**

> Ramirez has been buying the same two things from you for nine years. Cigarettes and a scratch card. He came in tonight at 2 a.m. and he did not buy anything. He stood by the cooler for eleven minutes and then he said your name, correctly, and then he came over the counter.
> There are more of them on the street now. Your store has one door and a roll shutter that sticks.
> Hold it until morning. Morning is in four minutes.

**5 — Power Yard**

> The city engineer is called Weiss and until tonight she would not have crossed the road to spit on you. She is standing in your lot at 4 a.m. asking for men with guns.
> Substation Nine feeds eleven blocks including yours. If the transfer does not complete, the grid sheds this whole quadrant and it does not come back. The transfer takes eight minutes and it cannot be paused.
> She stays with the panel. You stay between the panel and everything else.

**12 — Dead Turf**

> This is the part nobody rehearses.
> The trucks are loaded. The crew that is coming is coming. Every wall you have ever paid for is on this block and none of it is enough, and you have known that since the laboratory.
> You are not going to hold this. You are going to hold it *longer than they think*, and every minute is somebody in a truck getting further away.
> Nine years you ran this corner. Make them take it.

---

## 5. How this lands in code

Nothing here needs an engine feature we do not have or have not specced.

- Each mission is one JSON file per the schema in `progression-and-campaign.md` §6. Terrain, spawns, waves by archetype, objectives, rewards, medals.
- The six objective types cover all twelve missions. `HoldUntil` carries the seven clock missions; missions 6 and 12 are `SurviveSeconds` with `winnable: false`, which only changes the end-of-mission screen.
- New enemies land in the order the table needs them: Sapper and Spitter **exist today**; Wrecker (mission 8), Vory shooters (7) and the Broodmother (11) are the only new archetypes in the whole act.
- Level themes map to `arsenal-and-terrain.md` §4. Missions 3, 5, 6, 9 and 10 each need their theme's prop table; the rest reuse your own block, which is one map dressed three ways as it degrades across the act.
- **Your block visibly deteriorates.** Missions 1, 2, 4, 8 and 12 are the same streets. Destructible terrain means the damage from mission 4 is still there in mission 8. That is nearly free and it is the strongest storytelling device in the list.

## 6. Questions for the owner

1. **HALCYON or CIPHER** for the AI's name. Adopting the title-drop is now-or-never.
2. **Does anyone get a cure?** The table has mission 9 answer "no, and here is a countdown", which sets up Act Two. The alternative is a partial cure that makes Act Two about distribution. Both work; they are different games by Act Three.
3. **Tone check on the briefings above.** They are deliberately unromantic about the player being a criminal. If you want him more sympathetic, or more monstrous, say which and I will re-pitch all twelve.
