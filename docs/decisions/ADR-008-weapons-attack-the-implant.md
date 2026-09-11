# ADR-008: Weapons attack the implant, not the body

- **Status:** Accepted (owner directive, 2026-09-11)
- **Supersedes:** nothing. **Depends on:** ADR-003 (the premise), ADR-002 (chase third-person)
- **Owner's words, verbatim:**

  > "This all needs to be based on the microchip. The weapons need to be like a drone Shield drone
  > gun type thing that emits some sort of jammer or an over-the-wave malware that slowly decrypts
  > and kills the chip so once I shoot the person maybe they slowly deteriorate over two or three
  > seconds but can still attack with gradually decreasing strength over that time maybe the Drone
  > airstrike can just be like a big EMP that's a lot of realism"

## Context

ADR-003 settled the premise: nobody got infected, they **opted in**. The enemy are ordinary chipped
citizens — no rot, no blood, normal eyes, one small amber implant light at the temple. The
protagonist is a veteran who refused the implant and the money that came with it.

The mechanics never caught up with that. The hero carried an LMG, the turrets fired rounds, the
airstrike dropped bombs, and agents popped out of existence the instant their health reached zero.
Every one of those is a weapon aimed at a **person**, in a game whose whole authored idea is that
the people are not the enemy — the thing in their head is. The fiction and the verbs had been
quietly contradicting each other since ADR-003 was written, and the owner named it.

There is also a gameplay problem this fixes, which is why it is worth the churn. Three separate
complaints from the last two playtests are the same complaint:

- "the turrets are killing the mobs too easily... a single turret should be able to be overrun"
- "the zombies are attracted to me but when they get to me they just stand there"
- "a full line of people aren't able to destroy a single turret"

All three come from **instant death**. An agent that vanishes on its last point of health can never
finish the swing it started, never crest a turret's line, never make the player back off. The
pressure a crowd applies is not its damage per second; it is the fact that shooting one does not
immediately stop it.

## Decision

**Weapons carry malware, not kinetic energy.** They decrypt and kill the implant. The body is
collateral only in the sense that a person whose implant is dying stops being a threat.

Three consequences, in the order they matter:

### 1. Nothing dies instantly. Chips **fail**.

When an agent's chip integrity reaches zero it does not leave the simulation. It enters a **failing**
state for `SimConfig.FailSeconds` (2.5s by default) during which it:

- keeps walking, at a speed that decays toward a stumble,
- keeps attacking, at strength scaled by how much integrity is left,
- then drops.

`TotalKills` increments at the moment the chip breaks, not when the body drops, so cash and
experience pay on the player's action rather than 2.5 seconds after it.

This is the single biggest feel change in the game and it is deliberately the load-bearing one: it
makes a crowd dangerous without adding a single agent to it, which is exactly the
quality-over-quantity direction the owner asked for on 2026-09-11.

### 2. The hero's weapon is an **emitter**, and so are the turrets.

Hitscan mechanics are unchanged — a transmission is instantaneous, so the existing ray march is
already the correct model, and every line-of-sight rule keeps working unaltered. What changes is
what the thing *is*: a directed jammer that lances a single chip (Sentry), or a broadcast field
that degrades every chip in an area (the Grinder family, "Brush Hog").

A wall still stops it. That is not a physics claim — the community's walls are chain-link and stacked
timber, and what actually stops the carrier is line of sight to the implant.

### 3. The airstrike is an **EMP**, not a bomb.

The delivery vehicle was already a hijacked cargo drone (ADR-003's third enemy class, turned around).
It now drops an electromagnetic pulse down the line the player is looking at: everything chipped in
the corridor has its implant hard-failed at once. No fire, no smoke, no orange.

This also quietly fixes an economy problem — the owner called the airstrike overpowered, and an EMP
that starts a 2.5s failure in forty people is far less final than a bomb that deletes them, because
those forty keep walking and keep swinging for another two and a half seconds.

## What this retires

| Retired | Replaced by |
|---|---|
| "health" as a body-damage number | **chip integrity** — the same number, now meaning something |
| instant death at zero | a 2.5s **failing** state with decaying speed and decaying threat |
| bullet tracers | a transmitted lance with a bright head and a segmented body |
| the airstrike fireball | an EMP burst: white core, electric-blue shell, ground ring, arcs |
| "gun: LMG" in the HUD | the emitter's designation |

**What this does NOT retire:** the ray march, the wall damage model, turret line of sight, the
archetypes, the intent split, or any of the economy. This is a re-skin with one real mechanical
addition, and it was scoped that way on purpose — the systems layer is frozen under the live
roadmap, and the failing state is the one exception being spent here.

## Consequences worth watching

- **Wave clears now have a tail.** A wave is not clear until the last failing body drops, up to 2.5s
  after the last chip breaks. This is correct — it is the crowd finishing its swing — but anything
  that keys off `AliveCount` hitting zero now fires later than it used to.
- **Difficulty goes up, everywhere, at once.** Failing agents still deal damage. Turret and hero
  tuning will need a pass against real play, not against the old arithmetic.
- **The archetype-death inference gets less reliable.** The game infers Sapper and Spitter deaths
  from the living count falling, because the sim raises no per-archetype death event. That inference
  must move to the chip-break moment or it will misattribute. See `AgentWorld.Failing.cs`.
- **Red eyes stay forbidden.** ADR-003 spends them almost never, and a dying implant is not a reason
  to spend one. The tell is the amber temple light stuttering out.

## Alternatives considered

- **Keep bullets, re-skin the HUD text.** Cheapest, and it fails the owner's actual point: he asked
  for the mechanic ("slowly deteriorate... can still attack with gradually decreasing strength"),
  not the vocabulary.
- **A separate "stunned" state instead of decay.** A binary stun is a pause, and a paused crowd is
  less threatening than a degrading one, which is the opposite of what the playtests asked for.
- **Make the failure duration depend on which weapon broke the chip.** Tempting, and deferred: it
  multiplies the tuning surface before anyone has played a single wave of the basic version.
