# Owner playtest — Maze v0 (Milestone 2), first play

**Date:** 2026-09-10
**Build:** `f54c82b` (local preview + CI artifact), Windows player, Xbox controller

## Owner notes (verbatim)

> Well it's working but it seems like right now at this point if I do close the loop it break breaks . but pretty cool man keep it going and we don't have to check point again until I'm able to build towers and possibly upgrade the towers and we have people trying to destroy our walls and Towers we'll get another shot there I guess maybe my Champion gets upgrades maybe a gun upgrade spawns on the map every now and again I don't know keep rolling

## What the photo shows

HUD: `fps 60 | alive 500 | breached 0 | kills 750 (you 750, turrets 0)`, `$1970  Wave 4/5  WAVE 500/500 spawned  Vault 25/25`, build mode on Barricade, message **SEALED — they will chew through here**. A hand-built spiral of barricades around the vault approach, the whole wave (500 runners) parked in columns on the far side of the first map wall with no route. Zero turrets built; all 750 kills are the hero's.

## Engineering annotations

- **"if I close the loop it breaks"**: confirmed degenerate case from Open Decision #5 / ROADMAP #3. A full seal leaves every runner with a zero direction vector; they stand still, `alive` never reaches 0, the wave never clears. The Sapper (ROADMAP #4) is the designed answer: a sealed spawn *forces* a Sapper spawn, which breaches the nearest wall between the stuck horde and the vault. Until it lands the seal is a soft-lock. Interim guard shipped with the Sapper work: forced Sapper on seal, plus the vault being reachable is not required for a wave to end if the *only* remaining runners are stuck (no — decided against: stuck runners must be dealt with, that is the point).
- **Turrets went unbuilt** ("turrets 0"). RB-to-cycle was not discovered or not wanted. Action: HUD lists every item with the selected one highlighted and the switch key, and d-pad left/right also cycles.
- **Difficulty**: $1970 banked at wave 4 with a hero-only defence says kill income is generous once the maze is long. Leave until turrets/upgrades exist to spend it on.
- **Owner asks for the next checkpoint** (his words: no more check-ins until these exist):
  1. build towers (exists; make discoverable) **and upgrade them**;
  2. enemies that destroy walls and towers (Sapper + Spitter);
  3. champion upgrades: a gun upgrade that spawns on the map every now and again.
- Camera/controls: no complaints; overhead build camera and cursor were used to draw a precise spiral.
