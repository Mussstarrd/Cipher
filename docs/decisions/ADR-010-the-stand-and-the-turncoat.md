# ADR-010: The stand is loud on purpose, and you can turn their machines

- **Status:** Accepted (owner directive, 2026-09-12)
- **Depends on:** ADR-003 (the three enemy classes), ADR-005 (the scan cycle), ADR-008 (signal
  weapons), ADR-009 (the truck, and energy as the thing that draws them)
- **Amends:** ADR-009's open question, which is now closed.

## 1. The question ADR-009 left open, and the owner's answer

ADR-009 established that artificial energy draws them, and left one thing dangling: if that is true,
why would the player ever switch anything on? It read as a stealth mechanic waiting to happen, and
the ADR flagged it as the strongest unbuilt lever in the game.

The owner answered it, and the answer is better than the question:

> "energy attracts them, energy turrets, each level is a stand and we are knowingly and actively
> using energy knowing that we are going to have to knock them back enough to pack up and retreat to
> another hiding spot out of their current alert zone. it's a segway from mission to mission.
> 'All right guys we got a fight, we got to use a whole bunch of energy and a whole bunch of beat
> the crap out of them, and then we got to get out of town.'"

**There is no stealth option and there was never supposed to be one.** A position is a decision to be
loud. You turn everything on, you hold, you hurt them enough to buy the gap, and then the noise you
made is the reason you cannot stay. That is not a cost bolted onto the loop — **it is the reason the
loop is a loop**, and it retroactively explains the one thing ADR-005 never justified: why leaving is
a decision the player makes rather than a failure state.

### What this changes

| Was | Is |
|---|---|
| "going quiet is an available strategy" | **it is not.** The stand is loud by definition |
| retreat = you were losing | retreat = the bill for the noise, paid on purpose |
| the scan is a timer | the scan is **their alert zone closing on the racket you just made** |
| emplacements cost money | emplacements cost money **and heat** |

**The alert zone is the between-mission fiction.** You do not retreat to safety; you retreat *out of
their current alert zone*, which is why the next position is a fresh fight two days later
(ADR-009's timeline) rather than the same fight continued.

### What is worth building, and what is not

The temptation is a heat meter. **Resist it for now.** The loop already expresses this: the scan
clock is visible, emplacements already pull the crowd, and packing up already costs the clock. A
number on the HUD would be a fourth resource competing with cash, time and truck space, and the
owner has not asked for one.

What IS worth building, in order:

1. **Say it.** The player has never been told any of this. One line in the mission brief and one in
   the pack-up screen. Cheap, and it converts an unexplained rule into the premise.
2. **Make the noise visible where it already exists.** Emplacements draw the crowd (ADR-009); the
   player cannot currently see that a gun is *why* they are being flanked.
3. **Only then** consider making the cycle length respond to how much was switched on. That is the
   real version of the mechanic and it needs play data first.

## 2. The turncoat drone

The owner's second directive, same message:

> "It would be cool if I could buy a turing that is not a turret it is actually a roaming drone that
> works on hacking humanoids to turn them against the microchip humans"

and, in the same breath, the thing that makes it possible:

> "all I'm seeing is humans I'm thinking that like 30% of them should be those humanoid robots"

**ADR-003 always named three enemy classes and only ever shipped one.** The hacked service humanoids
have existed in the fiction since the premise was written and have never appeared on screen. Putting
them in a wave and then letting the player *take them back* is the best use anyone has found for
them, and it is thematically exact: the whole war is about who owns which machine. HALCYON hacked
them; you hack them back.

### The design

A **buildable, roaming, non-shooting emplacement**. It is not a turret and must not feel like one:

- It **moves**, unlike everything else the player places. It is the only mobile thing they own.
- It **carries no weapon.** Its output is conversion, not damage.
- It targets **hacked humanoids only.** It cannot touch a chipped person — they are a human being
  with a chip in their head, and taking them over would be a different game and a much darker one.
  **This limit is the design, not a simplification**: it makes the humanoid share of a wave into a
  resource the player reads, and it means a wave with few machines in it is a wave the drone is bad
  against. That is a reason to look at a crowd and think.
- A converted humanoid **fights for you** until it is destroyed. It does not path home, it does not
  need managing, and it is not a unit the player commands — it is a machine that has changed sides.

### Why this is not cheap to build, and what it costs

**The simulation has never had an agent fight another agent.** Every mechanic to date is
crowd-versus-structure or crowd-versus-hero. Conversion means:

- an allegiance flag on an agent, which the flow field and every targeting query must respect;
- turrets and the hero must not shoot their own converts, which touches the same "two queries, two
  answers" trap the outside review already caught once;
- the converted have to pick targets, which is the first enemy-selects-enemy code in `sim/`;
- and all of it must stay deterministic, with no RNG in the core.

That is an afternoon of design and a day of careful work, not a quick win. It is worth it: it is the
first genuinely new *verb* the player has been given since the airstrike, and unlike a fourth turret
family it changes how a crowd is read rather than how much damage is dealt.

**Open, and not to be guessed at:**
- Does a convert count as a kill for the economy? (Recommendation: no. It is worth more than a kill.)
- Does it survive the pack-up, or is it abandoned like an emplacement? (Recommendation: abandoned —
  it is a machine you borrowed, and ADR-005 says what is left behind is left behind.)
- Can the drone be destroyed, and does the player lose the converts with it? (Recommendation: yes and
  no respectively — the hack holds, which is what makes the drone worth protecting rather than
  babysitting.)

## Consequences

- **ADR-009's "not built, deliberately" note is superseded.** The answer is above; the heat meter it
  imagined is explicitly deferred in favour of telling the player the rule first.
- **Roughly 30% of a wave becomes machines**, which changes the crowd's silhouette, its read at
  distance, and the value of every area weapon. Balance will move.
- **Missions with few humanoids make the drone a bad buy**, and that is correct. The build bar should
  never contain a thing that is right in every mission.
- The drone is the first thing the player owns that can be *somewhere else*. Whatever UI it needs is
  new — the patrol-drone waypoint UI has been on the owed list since the first roadmap and is now
  load-bearing rather than polish.
