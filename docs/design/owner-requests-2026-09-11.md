# Owner requests, 2026-09-11 — the checkpoint list

**Status:** captured verbatim, being worked · **Author:** ENG

Recorded the moment they were given, so nothing is lost to a session cutoff. His words:

> Build should be lb button opens build wheel instead of tiny text prompts in top corner of the screen
> and you should be able to ooen build mode and time slows a good percent for a few seconds to give you
> a chance o actually absorb and adjust plan. Maybe it's like adrenaline focus or something you can open
> build mode for 2 cycles of this before your slowdown is depleted for a period of time.. At end of level
> it's weight/size based retrieval. The exit is a truck and I can bring whatll fit in it which is a
> threshold you can figure out with size and weight parameters and what not. Don't forget level ups and
> skill tree and shit. Towers should not be able to detect through walls. Mobs shouldn't project their
> path and they shouldn't all follow the same logic. Some should branch off and try to kill tower or
> blockade infra. Towers shouldn't be able to shoot through walls without the walls deteriorating.

Plus, from the message before it:

> beginning of level you should get as much time as you want to set up until you press A or something to start

## The list

| # | Request | Layer | State |
|---|---|---|---|
| 1 | Untimed opening setup, player starts the first wave | match | **done** |
| 2 | Inventory and gear | progression | **done** |
| 3 | Upgrade paths | progression | **done** (pick-one-of-three), tree in #8 |
| 4 | Truck extraction: what fits, by weight and size | progression + match | **done** |
| 5 | Adrenaline focus: build mode slows time, two charges | match | **done** |
| 6 | Turrets cannot see or shoot through walls | **sim** | **done** |
| 7 | Turret fire degrades any wall it passes through | **sim** | **done** |
| 8 | Levels and a skill tree | progression | **done** |
| 9 | Mobs must not all run the same logic; some branch to kill turrets or wreck infrastructure | **sim** | **done** |
| 10 | LB opens a build wheel instead of corner text | UI | **done** |

## Notes on the ones that changed an earlier decision

**#4 replaces the pack-up clock as the *limit*, not as the *mechanic*.** ADR-005 made extraction a timed
window in which each emplacement costs seconds to unbolt. The owner has now added a second, better
constraint: the exit is a truck and you take what fits. Both stay, and they do different jobs. Time says
*how many* you can get to. The truck says *what is worth carrying*, which is the more interesting decision
because a heavy turret and four light ones cost the same bed space, and that is a real choice.

**#8 does not delete #3.** The design memo argued for pick-one-of-three over a tree for v0 and the reasoning
still holds for *in-mission* upgrades: they are disposable, they land at the wave clear, and they need no
save file. The owner asked for levels and a tree as well, so the split is now explicit:

| Layer | Scope | Lives in |
|---|---|---|
| Improvisations, pick one of three | one mission, discarded after | `Improvisations.cs` |
| Levels and skill tree | permanent, across missions | `SkillTree.cs` |

That is the Hades split: boons die with the run, the Mirror is forever.

**#6, #7 and #9 are simulation changes, which means they are determinism-critical.** No RNG in the core,
fixed iteration order, and the state-hash test has to keep passing.

**#10's wheel is maths plus drawing, split on purpose.** `BuildWheel.cs` holds the selection rule and is
unit tested (twelve o'clock is option zero, it runs clockwise, the dead zone keeps the last pick rather than
clearing it when a thumb drifts back to centre). The OnGUI drawing is dumb and knows nothing about turrets.

**#9 is the most interesting request in the list.** A thousand agents that all path to the same goal is a
flow field, and a flow field is legible in a way that stops being frightening: the player learns the one
lane and holds it. Splitting intent at spawn means the wall you did not garrison is the one they choose,
and it makes the Sapper and Spitter feel like the sharp end of a spectrum rather than two special cases.

## What actually reaches the player

Recording this because a system that exists but never fires is not done:

- Intents are chosen in `SpawnDirector.DecideIntent`, not in the sim, and the bootstrap passes one on every
  ordinary spawn. Default shares are 10% hunters and 8% wreckers, and hunters only appear once there is an
  emplacement to hunt. **Most bodies still come for the objective**, because the aim is an unpredictable
  crowd, not a permanent siege of the turret line.
- A body tearing at an emplacement raises `StructureMauled`, which the bootstrap turns into turret damage,
  a sound, and a "TURRET TORN DOWN" alert.
- Adrenaline focus drives `Time.timeScale`, so the whole simulation slows with it, and it is ticked on
  unscaled time so four seconds of focus lasts four seconds.
- The wheel is bound to LB held (or Q on the keyboard): build mode opens, a focus charge is spent, and
  releasing commits the highlighted option.
