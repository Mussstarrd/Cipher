# ADR-002 — Camera model: chase third-person for combat, overhead for looking and building

**Status:** Accepted · **Date:** 2026-09-10 · **Owner sign-off:** delegated to ENG by the owner ("you decide which would be best") on 2026-09-10, before the hero playtest

## Context

Open Decision #1: the pitch promises overview mazing AND third-person gunfeel, and no doc said what the camera does. The register offered three options and asked for a graybox test of (b) full third-person + build-mode drone cam vs (c) zoom hybrid. The owner asked for a hero to play before the maze exists, which forces a provisional camera now.

## What ships in the hero graybox

Both candidates are in the same build behind one button, so the owner can flip between them mid-fight:

| Mode | How | Movement | Aim | Camera |
|---|---|---|---|---|
| **Chase** (default) | — | camera-relative (left stick) | hero faces where the camera looks; right stick / mouse orbits | behind and above the hero, follows |
| **Tactical** | hold **LB** / Tab | world-relative | twin-stick: right stick / mouse aims directly | high overhead, fixed north-up, follows the hero |

This is option (c) with "hold" instead of "toggle". If the owner never releases LB, it's (a). If the owner never presses LB, it's (b) minus the build-mode drone cam, which arrives with the maze milestone either way.

## Decision

**Option (b), the Orcs Must Die split.** Combat is over-the-shoulder chase, full stop. The overhead camera exists for situational awareness (hold LB to peek) and becomes the build-mode camera in the maze milestone. **Weapons hold while overhead**, enforced in code, so the "one more wave" test measures the camera we actually ship combat on.

Why this and not the twin-stick hybrid:

1. The pitch's fantasy sentence is *walk out the front door with a gold-plated LMG and clear the street yourself*. That only lands behind the shoulder.
2. The two proven hero-plus-towers games (Orcs Must Die, Dungeon Defenders) use exactly this split; there is no shipped hit that fights from the overview at this density.
3. Xbox controller is the design centre. Over-the-shoulder with aim assist is the known-good gamepad shooter pattern; twin-stick aiming from 40 units up into a thousand runners is imprecise and reads as a mobile game.
4. Android perf floor: the chase frustum holds far fewer agents per frame than the overhead, which is the cheapest density win we have.

What it commits us to:

- **Aim assist** (soft target magnetism, capped) lives in the chase input path. Budgeted for the M3 gun pass.
- **Arenas must be ground-legible from the shoulder**: tighter blocks, taller landmarks, lane colouring. The 64×48 flood field is a density test, not a level.
- **Build mode = the overhead rig + a build cursor**, north-up fixed yaw so it reads like the pathing preview every time. Entering build mode is a mode toggle, not a hold.
- **Mid-wave building** (M3) happens from overhead with the weapons holstered, which is the combat-price tension the blueprint wants anyway.

## Reversal conditions

Reverse to the twin-stick hybrid only if, at the M3 "one more wave" test, players with controllers cannot land the gun (miss rate stays above ~60% after aim assist) or consistently report they cannot read the maze during fights even with legible arenas. Both are measurable in the build.

## Original exit criterion (kept for the record)

Before the owner delegated the call, the plan was to let the playtest decide:

- If **chase** is where the fun is and tactical is only used to "look around": accept (b). Build mode gets its own drone camera; combat stays over-the-shoulder.
- If the owner **fights from tactical**: the gunfeel fantasy is not landing from behind the shoulder at this map scale. Accept (a)-with-zoom and invest in twin-stick feel (aim assist, auto-face) instead of third-person shooter feel.
- If the owner **flips constantly**: accept (c) as designed and make the transition a first-class animation (fast, readable, with a shoulder-button hold so it never sticks in the wrong mode).

Whichever wins, M3's "one more wave" test must score the mode *transition*, not just the gun (per Open Decision #1).

## Consequences

- The tactical camera is north-up fixed yaw so barricade placement in the maze milestone reads like the pathing preview: same orientation every time.
- Chase mode owns hero facing, so a future aim-assist lives in the chase input path only.
- Map scale is untested against chase-mode legibility (Open Decision #1 warned option (b) needs smaller, ground-legible maps). The 64×48 flood arena is the first data point.
