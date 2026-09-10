# ADR-002 — Camera model: chase third-person with a held tactical overhead

**Status:** Proposed (graybox test shipping) · **Date:** 2026-09-10 · **Owner sign-off:** pending — decided by playing, not by reading

## Context

Open Decision #1: the pitch promises overview mazing AND third-person gunfeel, and no doc said what the camera does. The register offered three options and asked for a graybox test of (b) full third-person + build-mode drone cam vs (c) zoom hybrid. The owner asked for a hero to play before the maze exists, which forces a provisional camera now.

## What ships in the hero graybox

Both candidates are in the same build behind one button, so the owner can flip between them mid-fight:

| Mode | How | Movement | Aim | Camera |
|---|---|---|---|---|
| **Chase** (default) | — | camera-relative (left stick) | hero faces where the camera looks; right stick / mouse orbits | behind and above the hero, follows |
| **Tactical** | hold **LB** / Tab | world-relative | twin-stick: right stick / mouse aims directly | high overhead, fixed north-up, follows the hero |

This is option (c) with "hold" instead of "toggle". If the owner never releases LB, it's (a). If the owner never presses LB, it's (b) minus the build-mode drone cam, which arrives with the maze milestone either way.

## Exit criterion for this ADR

After the owner has played the hero build:

- If **chase** is where the fun is and tactical is only used to "look around": accept (b). Build mode gets its own drone camera; combat stays over-the-shoulder.
- If the owner **fights from tactical**: the gunfeel fantasy is not landing from behind the shoulder at this map scale. Accept (a)-with-zoom and invest in twin-stick feel (aim assist, auto-face) instead of third-person shooter feel.
- If the owner **flips constantly**: accept (c) as designed and make the transition a first-class animation (fast, readable, with a shoulder-button hold so it never sticks in the wrong mode).

Whichever wins, M3's "one more wave" test must score the mode *transition*, not just the gun (per Open Decision #1).

## Consequences

- The tactical camera is north-up fixed yaw so barricade placement in the maze milestone reads like the pathing preview: same orientation every time.
- Chase mode owns hero facing, so a future aim-assist lives in the chase input path only.
- Map scale is untested against chase-mode legibility (Open Decision #1 warned option (b) needs smaller, ground-legible maps). The 64×48 flood arena is the first data point.
