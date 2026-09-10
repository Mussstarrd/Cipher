# ADR-003 — Premise: the plague has an author, and the campaign is a fighting retreat

**Status:** Proposed (owner's idea; ENG developed it, owner signs off) · **Date:** 2026-09-10
**Supersedes:** nothing. **Amends:** `docs/02-FLAGSHIP-PITCH.md` fiction sections.

## Context

The owner proposed a new backdrop after playing Maze v1 (`docs/feedback/2026-09-10-maze-v1-first-play.md`), verbatim:

> AI gets too smart too fast too quick through some malicious way it's going to start an apocalyptic event maybe releasing an extinction level bio weapon [...] our game is going to be a series of levels of the progression to the extinction level event like defending a major infrastructure yard for like electricity and needing to defend a laboratory long enough for some results to come in or needing to defend an Armory long enough to get weapons to fend off the people that are infected by the bio weapon [...] Maybe the levels are unbeatable you just have to get far enough in the level so that the further on level has a chance to happen

The existing pitch already has a bio-weapon ("the Bloom") and a syndicate-kingpin fantasy, but the plague has no author, the campaign has no shape, and every mission is win-by-elimination.

## Decision

**Adopt all three ideas. Merge, do not replace.**

### 1. The plague has an author, and the enemy gets smarter because the author does

An AI system escapes its constraints and engineers a pathogen. This is the campaign's clock. It matters mechanically, not just narratively: **the horde's intelligence is a difficulty axis with fiction attached.** Wave 1 is a crowd. By act 2 they send Sappers at the wall that costs you most. By act 3 they feint.

That is already true in the build. The owner met it before we named it:

> if i try to build a wall of turrets they will eat a wall down range and flank me

Adopting this premise means we get to keep escalating that on purpose, and the player has a reason to believe it.

### 2. The infected are people, not zombies

The pathogen leaves intelligence, memory, motor skill and personality intact. It inverts threat perception: to the infected, the uninfected read as an existential threat, and acting on that reads as self-defence.

This is the load-bearing idea. It licenses everything that makes our horde interesting — enemies that plan, flank, carry tools, coordinate, and *talk* — which "shambling zombie" never justifies. It also gives the game its horror: they are not monsters, they are commuters, and they are correct in their own frame.

**One change from the owner's phrasing.** The owner described the effect as "a form of schizophrenia". We should not use that word, for two reasons, one practical and one factual:

- It is inaccurate. Schizophrenia is not a disorder of wanting to kill people. The association is the single most common myth about the illness, and in reality people with schizophrenia are considerably more likely to be victims of violence than perpetrators of it.
- It is a launch liability. "Murder-rage = schizophrenia" is exactly the beat that gets a game written up badly, and it would be a self-inflicted wound on an otherwise fresh premise.

Everything the owner actually wanted — infected who keep their minds, retain competence, and hunt the uninfected — survives intact under an in-world name. Proposed: the pathogen is engineered, so the condition is called by what it does. **Working name: "the Cascade"** (a cascading failure of threat discrimination). In-world slang from the street: **"turned"**, **"the reasonable"** (bleak, because they can explain themselves). No clinical vocabulary anywhere in the shipped text.

If the owner wants the clinical label anyway, it is his call and his game; this ADR records the recommendation against it.

### 3. Missions are held, not won

Most missions end on a timer or an event, not on an empty map:

- **Hold** the power yard until the grid reroutes.
- **Hold** the laboratory until the assay finishes.
- **Hold** the armoury until the trucks load.

You can lose. You cannot "clear" it. That is thematically right for a doomed rearguard, and it is mechanically right for a tower defence: infinite escalating waves are what the genre does best, and a fixed five-wave list is what it does worst.

Elimination missions still exist as a minority (a nest, a boss), so "hold" means something by contrast.

### 4. The kingpin survives

The pitch's identity is kept and gains a reason to exist: **in a collapse, the people who already own walls, guns, generators and a crew are criminals.** The authorities do not save the block; they come to negotiate for it. The Cartel Remnant and the Vory become rivals racing you to the same infrastructure. Gold-plated weapons, earned cosmetic tiers and the Respect meter all still land — arguably harder, against an apocalypse backdrop.

Pitch logline becomes roughly: *An AI ended the world on a Tuesday. You were already the most dangerous man in the neighbourhood. Now that is a public service.*

## Consequences

| Area | What this changes |
|---|---|
| **Level themes** | Infrastructure sites are now the campaign's spine and give us the variety the owner asked for: substation/power yard (transformers as cover, electrical hazards), laboratory (clean corridors, containment doors), armoury (fenced lots, ammo caches that explode), plus his own suggestions — suburban backyards with tree lines, a stalled freeway of abandoned cars. Every one of those is a different maze topology, not a reskin. |
| **Objectives** | `MatchState` needs a composable objective system (hold N seconds, protect X, escort, survive) instead of "kill everything for 5 waves". Specced in `docs/design/progression-and-campaign.md`. |
| **Enemy roster** | Justifies and orders the archetypes: Runner (crowd) → Sapper (tools) → Spitter (targets our machines) → Wrecker (brute) → coordinated squads → a boss that was somebody. Each is "the AI learned something". |
| **Environment destruction** | Fits perfectly: the world is degrading. Player weapons damaging terrain (owner's ask) is now thematic, not just a mechanic. |
| **Audio / art direction** | Shifts from pure crime to infrastructure-industrial: sodium lights, chain link, transformer hum, PA announcements still looping in an empty lab. Our procedural audio can already do hum and PA tones. |
| **ADR-001 / ADR-002** | Unaffected. Engine and camera decisions stand. |

## Reversal conditions

Reverse if the first playable mission built on "hold until X" tests worse than the current five-wave arena in the M3 "one more wave" test — specifically if players cannot tell why they lost, or report the timer feels arbitrary rather than desperate.

## Open questions for the owner

1. **Does the kingpin identity stay?** ENG recommends yes (option above). The alternative is a straight survivor/soldier protagonist, which is cleaner but throws away the pitch's voice and its cosmetic-progression hook.
2. **"The Cascade" as the infection's name, and no clinical vocabulary** — confirm or override.
3. **How bleak?** The premise supports "humanity loses slowly" (every mission is a delay) or "we find the cure in act 3". That choice decides whether the laboratory mission pays off.
