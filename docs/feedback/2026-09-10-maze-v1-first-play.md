# Owner playtest — Maze v1 (Sappers, Spitters, drones, upgrades, crates, sound), first play

**Date:** 2026-09-10
**Build:** `16cd38a` preview (Maze v1 + procedural sound), Windows player, Xbox controller

## Owner notes (verbatim)

> ok ive played and its functioning and if i try to build a wall of turrets they will eat a wall down range and flank me... so now we need to figure out next steps. character builds and power up paths and refining sound effects, adding graphics models, a story line, everything that turns this from a concept of a cool toy, to a cmarketable and playable action strategy tower defense crawler etc. id like if items could drop and an inventory for gear and maybe a safe zone in between rounds or every third round or something where i can organize gear and sell uneeded shit so i can have armor and shit that can upgrade over time. these levels can be scenario based like warcraft if that helps so that levels can be nbeaten and progress can be made. eventually id like to have couch co-op. sping up whatever powers may be needed to get a plan on all of these refinements and the best strategy for path forward. use SOLID principles and make sure testing, QA, is maintained. let me knwo what accoutns i may need to make to get you repositories for ai genewration for graphics or backgrounds or sounds or droplet or AWS resources, wahjtever makes sense

## Engineering annotations

- **"functioning"**: Maze v1 loop confirmed end to end on the owner's machine with sound.
- **"wall of turrets → they eat a wall down range and flank me"**: the Sapper doing its job (breach where the shortcut is biggest, which is rarely where the guns are). Usability gap to close soon: the player only *hears* the siren; there is no off-screen indicator pointing at the Sapper or the breach cell from the chase camera. The breach memo specified an off-screen HUD arrow; it has not been built yet.
- **Owner's next-phase asks** (the toy → product list):
  1. character builds and power-up paths;
  2. refined sound effects;
  3. graphics models;
  4. a storyline;
  5. item drops, an inventory for gear, armor that upgrades over time;
  6. a safe zone between rounds (or every third round) to organise gear and sell;
  7. scenario-based levels (Warcraft-style) that can be beaten, with progress;
  8. couch co-op eventually;
  9. process: SOLID, testing and QA maintained;
  10. a list of accounts to create for AI generation (graphics, backgrounds, sounds) and cloud resources.
- Brain trust #2 launched against this list; outputs in `docs/design/`, synthesis in `docs/design/ROADMAP-2026-10.md`, accounts checklist in `docs/ACCOUNTS.md`.
