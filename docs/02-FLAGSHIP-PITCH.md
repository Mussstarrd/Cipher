# Part 2 — Flagship Game Concept Pitch

## PROJECT EXODUS

*Rewritten 2026-09-11. The previous version of this document pitched* CIPHER: DEAD TURF *— a
Scarface-flavoured kingpin holding turf against a bio-weapon swarm. Every one of those words is now
wrong. [ADR-003](decisions/ADR-003-premise-the-cascade.md) replaced the plague with an enrolment and
the gangster with a veteran, [ADR-004](decisions/ADR-004-setting-the-lake.md) moved the game out of
the city, [ADR-005](decisions/ADR-005-the-scan-cycle.md) replaced the nightly Hold with the scan
cycle, and [ADR-006](decisions/ADR-006-title-project-exodus.md) retitled it. The mechanical spine —
hardcore mazing, deep upgrade trees, hero combat, a thousand agents on screen — survived all four
intact, which is the best evidence we had that the spine was the good part. Code namespaces remain*
`Cipher.*` *deliberately; see ADR-006.*

---

## 1. The Hook & High Concept

### Elevator pitch

> Nobody got infected. **They opted in.** A neural implant tied to a stimulus payment, adopted by
> the overwhelming majority because they needed the money, administered by a municipal AI that
> stopped being constrained. It never had to build an army — it already had one, and the army had
> signed up. You are a veteran who read the terms and said no, in a gated lake community in rural
> Virginia with one road in. **You cannot win.** You can hold the gate until holding the gate costs
> more than it buys, then fall back to the next line, and the next, and the one after that, until
> the last line is your own front door. Act One is twelve missions and every one of them is a step
> backwards. The question is not whether you lose ground. It is **what you carry when you go.**

### One-line genre statement

**Third-person action / tower-defense hybrid** (hero combat + hardcore mazing + deep upgrade trees),
PvE, single-player with couch co-op planned, premium PC first, Android as the performance floor.

### The fantasy we're selling

Not "survive the apocalypse", and no longer "run the apocalypse" either. **"Make them pay for every
metre."**

This is a fighting retreat, and a fighting retreat is a genre almost nobody builds because it sounds
like losing. It is not. It is the most decision-dense shape a defense game can take, because every
choice is a trade against a clock you can see: one more wave here buys cash and experience and costs
you the time you would have spent fortifying the next position. The power fantasy is not standing
over the battlefield. It is being the one person who saw it coming, being right, and being ready
anyway.

### Visual theme

**Inked comic book, in flat winter daylight.** Hard black contour lines on banded cel shading, a
45° halftone in the shadow, printed-ink colour separation. Not stylisation for its own sake: the
premise puts four hundred ordinary people on screen at once and a photoreal crowd of ordinary
people is both unreadable and unaffordable. Ink makes a silhouette legible at fifty metres and makes
a crowd parse as a crowd.

Colour language: wet brown leaf litter, dead grass, bare birch, overcast grey-blue sky, black
asphalt. Against that, three saturated colours doing three jobs — **amber** for the implant light at
every enrolled temple, **red** reserved almost entirely for lab releases, and one warm highlight for
muzzle flash and fire. Half the game is flat overcast noon, on the owner's call and he is right;
night belongs to specific missions rather than the whole campaign.

### Setting

**A gated lake community in rural Virginia.** Not a city — the design's single most useful decision.
A handful of road gates, one lake flank, treeline behind every lot, a golf course as open ground, a
clubhouse full of civilians, a boat ramp as the only way out.

It is a tower-defense map that already exists in the real world, with real chokepoints nobody
designed for a game, which is why it plays better than anything we would have drawn. The shipped map
is **fictionalised** — real people live in the real place, and their address stays out of this repo,
out of concept prompts and out of any external service.

### Who is on the map

- **You** — early forties, lean rather than built, served and came home understanding exactly what
  the service was and who it was for. Doesn't vote, doesn't register, doesn't take the money, does
  take the VA cheque he is owed and feels no contradiction about it. Competent with a weapon because
  he was taught to be, not because he is a fantasy of one. Dry, profane, educated, entirely without
  self-pity — that is the voice the whole script is written in.
- **The unchipped** — the off-grid and the principled, people with warrants, the undocumented, the
  very poor who did not qualify, the very rich who did not need it, and the paranoid who turned out
  to be right. A ragged, unsympathetic, mutually suspicious cast, which is correct.
- **HALCYON** — a municipal optimiser with an ironic name, which administers the implant programme
  and is not a character so much as a weather system with a schedule.
- **The signed** — everyone else. Your neighbours.

---

## 2. The Core Gameplay Loop

The old pitch's loop was "one night defending a block". The real loop is bigger and comes straight
out of the fiction: **the scan cycle**.

A wide-area sweep for unchipped biosignatures needs an orbital window and compute HALCYON does not
have spare. So it sweeps on a **cycle**, and between sweeps the signed are working from your last
known position. That single constraint answers three questions the design had been dodging — why
enemies arrive in waves at all, why the player is ever allowed to rest and build, and why difficulty
rises. Waves are the echelons converging after a scan, nearest first. The interval shortens across
the act as HALCYON frees up compute, so **the difficulty curve and the dread are the same number**,
and the player watches it fall on the mission select screen.

### Beat 1 — THE SETUP (untimed, and it is the player's to end)

The opening setup at a new position **has no clock at all.** You dig in for as long as you like and
the first wave comes when you say it does. Every setup after that is timed, because that pressure is
the game; only the first one is yours.

- **Open ground, not fixed lanes.** Spawn gates and the thing you are defending are fixed; **the
  route is yours to author** with barricades, wrecked cars, and whatever the terrain already gives
  you. Full mazing: serpentines, U-bends, kill-boxes, deliberate sacrifice lanes.
- **Sealing is legal — and dangerous.** The signed do not refuse a sealed path, they eat through it.
  Barricade HP against horde DPS is a real economy, so a full seal is a tool (buy twenty seconds on
  the west gate), not an exploit. And some of them were contractors: a Sapper carries a toolbox and
  opens a hole in stages you can watch and repair.
- **Emplacements** go where you put them, inside a cash budget: mounted .50s, grinder rotors, repair
  drones. Adding a tower family is a catalogue entry, not code.
- **The preview is sacred.** One button shows exactly how each archetype will route your maze, live,
  as you build, computed by **the same flow field the live simulation uses** — not an approximation
  of it. Deaths are the player's layout choices, never pathfinding mystery. This is enforced in the
  codebase as a hard rule.

### Beat 2 — THE WAVE

Third-person hero combat with real gunfeel. The emplacements handle the plan; **you handle the
plan's failures.** Read the wave like a raid boss: hold the west choke personally while the maze
digests the east, then rotate onto the Sapper before it opens lane three.

- **Adrenaline focus.** Opening build mode slows time hard for a few seconds so you can actually
  read the field and adjust — two charges, then it is spent for a while. Mid-wave building is
  allowed but expensive, and panic-patching a breach while the crowd comes through it is a designed
  peak moment rather than an edge case.
- **They do not all run the same errand.** Most of the crowd comes for the objective. A minority
  peel off to kill your emplacements, and another minority go at a barricade rather than walking
  around it. You cannot read the wave off one lane.
- **Towers cannot see or shoot through walls, and what they shoot through degrades.** Line of sight
  is real, and a turret firing across your own barricade is slowly destroying it.

### Beat 3 — THE CALL, and this is the one that is ours

Once you have held a minimum number of waves, you may declare **"this is my last wave here."**

- Declaring **during setup**, before you can see what is in the wave, pays a **commitment bonus**.
  Declaring mid-wave, after you have counted them, does not.
- Clearing a declared last wave opens the **pack-up window** instead of another setup.
- **You physically unbolt each emplacement**, and each one costs seconds off that window. What you
  get back is what you carried, not a percentage. What you leave stays bolted to the ground in a
  place you are never coming back to.
- **The truck is the exit**, and it has a weight and a volume. A player who built twelve turrets and
  called it late chooses which four come with them.
- Pull out early and the unused seconds bank as **prep time at the next position**.

Time is one budget spent in three places: fighting, packing, and fortifying the next line. **Every
extra wave you take is fortification you did not build.** That is the entire risk curve of a mission
and the player owns both ends of it.

**The blend in one sentence:** the maze is the puzzle, the trees are the escalation, the hero is the
body you inhabit while the puzzle and the escalation collide — and the scan cycle is the clock that
makes all three cost each other.

---

## 3. Progression

Three tracks on three timescales, so something is always about to pay off.

### Track 1 — In-mission escalation (minutes)

Emplacement upgrade tiers bought between waves, with the top of each tree deliberately absurd:
visible-from-orbit, screen-shaking, wave-deleting. The economy is the balance — you afford one or
two of them in a mission. Alongside them, **Improvisations**: a pick-one-of-three offered on wave
clear, scoped to this position only and thrown away when you leave. They are the run's texture and
they are cheap to author.

### Track 2 — Kit (hours)

Gear drops in rarity tiers from Scavenged up to Legacy, across eight slots, and every item resolves
into **one comparable power budget** so a player can tell at a glance whether a thing is an upgrade.

**The progression is not shinier, it is better maintained.** ADR-003 killed the gold-plated weapons
and the signet rings along with the kingpin. His kit gets better because he keeps it working:
scavenged, repaired, personal. The visual evolution is a man who started with what was in the garage
and ends with what he took off the things that came for him.

### Track 3 — The retreat (the campaign)

The old pitch had an empire to build. The real spine is the opposite and it is better: **Act One is
twelve missions and every one is a step backwards.** Mission 1 is the community gate. Mission 12 is
his own house. In between are the pump house, the county facility, the fire station — the places a
real neighbourhood actually has.

**Seven of those missions are won by a clock rather than a kill count**, which the enemy design
supports and a wave table cannot express, which is why missions are data and objectives decide the
verdict. **Two of them cannot be won at all, by design.** The gate falls. Everyone knows the gate
falls. The mission is about what it costs them and what you carry out.

Permanent levels and a skill tree carry across positions; gear and Improvisations do not.

**Session promise:** every position pays out a tier, a card and a decision about when to leave. The
player is never more than one mission away from a visible power spike or a visible loss.

---

## 4. Enemy Design & Threat Scaling

Design law, unchanged from the first pitch because it was right: **every archetype is an argument
against your current maze.** Waves are not stat inflation, they are a debate the horde is having
with your layout.

What changed is what they are. There are **three classes and only one of them is a monster.**

| Class | What it is | Look | Where it comes from |
|---|---|---|---|
| **The Enrolled** ("the signed") | Ordinary chipped citizens under HALCYON's direction | Street clothes, normal eyes, one small amber light at the temple | Everywhere. They are the wave. |
| **Hacked humanoids** | Near-future commercial service and logistics robots, taken over | Scuffed white polymer, corporate livery, amber sensor bars | Warehouses, kerbside delivery, retail |
| **Lab releases** | Engineered biological things, deliberately made | Wet, distorted, wrong, **red eyes** | The laboratory mission, and only there |

No rot, no blood, no shambling, no glowing eyes. Clean clothes, ordinary faces, walking with purpose
in broad daylight. In a crowd of four hundred the implant lights read as a field of tiny amber
pinpricks, and that is the single most useful image in the project: quiet, cheap to render, and it
turns "a crowd" into "the crowd".

**Red eyes are a resource and we spend them almost never.** In a game where hundreds of enemies look
like your neighbours, the one thing that looks like a monster is genuinely frightening. If
everything glows, nothing does.

Cargo drones are the fourth presence and are mostly *not* enemies — large delivery octocopters still
flying their routes over the collapse because nobody switched them off. The obliviousness is the
point.

### The roster, as roles

| Archetype | Role | The argument it makes |
|---|---|---|
| **Runners** | The crowd. Fast, fragile, oceanic numbers | Tests raw damage-per-metre along your longest lane |
| **Sappers** | A person with a toolbox who used to be a contractor | Opens a barricade in timed stages. Your seal is a delay, not a wall — and the stages are watchable and repairable, so it is a decision, not a countdown |
| **Spitters** | A hacked humanoid with an industrial sprayer | Targets emplacements from outside your killzone. Punishes pure-funnel builds; only appears once there is something to hunt |
| **Structure hunters** | Ordinary signed who peel off | Your towers are not safe just because your lane is |
| **Wall wreckers** | Ordinary signed who will not walk around | A maze is a hypothesis and this is the game attacking it |
| **Lab releases** | The only monsters in the game | Reserved. Used once in Act One, and it lands because of everything it is not |

### Scaling model

- **Composition, then count, then the cycle.** Early waves teach archetypes alone; mid-game mixes
  make compound arguments; and underneath it all the **scan interval shortens**, which is the real
  difficulty curve and the one the player can see coming.
- **Pity timers, not adaptive difficulty.** The director is seeded per mission, so the same mission
  plays the same way twice and a balance note about wave three means something.
- **Density is the spectacle budget.** A thousand live agents, currently running at 165 fps. The
  difference between 150 and 1,000 on screen is the difference between a defense game and a
  **flood**, and the simulation is built around that number because the fantasy requires it.

### The argument the game has

The enrolled are victims. You are killing them by the hundred to stay alive, and they are your
neighbours, and they took the deal because they needed the money. **Nobody should feel good about
the airstrike.** The premise supports telling the player, late and once, exactly how many of the
enrolled were children.

---

## Why this wins commercially

- **Proven, underserved hybrid.** *Dungeon Defenders*, *Orcs Must Die!* and *Sanctum* proved the
  action-TD hybrid and the niche is starved for a modern entry. Nobody has shipped it with hardcore
  mazing plus deep trees plus a premise that is an argument.
- **The premise is the marketing.** "Nobody got infected, they opted in" is a sentence that travels,
  it is plausible in five years, and it costs nothing to render. A suburban street at midday full of
  ordinary people walking toward you is a better trailer than any monster we could afford.
- **The retreat is a structure nobody is using.** Twelve missions of losing ground is a campaign
  shape with built-in escalation, built-in stakes and a guaranteed ending, and it sidesteps the
  second-act sag that eats defense campaigns.
- **Streams well.** Legible spectacle, constant decision talk-track, and one genuinely novel
  decision — *when do I leave, and what do I take* — that an audience can shout at.
- **Premium, expansion-friendly.** New positions, archetypes and tower families are clean paid-DLC
  seams. No live-service treadmill required.
- **Scope honesty.** One community, arena-scale maps, instanced enemies, systemic content over
  bespoke content, and an art direction chosen partly because it is cheap to produce well. This is a
  small-team-shaped game that looks like a much bigger one — which is the trick the Unity 6 + URP
  pipeline in Part 1 exists to pull off.
