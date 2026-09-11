# Owner playtest — The Gate, 2026-09-11

Second real playtest, first one of the graphical build. **Verbatim, in his order**, because a
playtest note that has been tidied up is a playtest note that has been half-thrown-away.

> i played a bit. the characters are always making walking movements and they are all in sync. the
> character skins are very quickly like scan switching from skin to skin to skin. the turrets arent
> shooting. the map is super clustered with our fake ass walls blocking up everything. Maybe the
> zombies are hunting me and my escape truck or whatever the protection point is (green protect box
> at my side of the map). maybe we need less zombies and have them be more robust. quality over
> quantity. also, the graphics are alright but if us paying 50 bucks or wahtever makes the
> difference between this looking like state of decay 1 and a legt re-envisioning of dungeon
> defenders, lets do it right. the skills and inventory pop ups i dont really understand and i cant
> get to my inventory or skills or any ofd that shit on controller there are only instructions on
> screen for keyboard keys and thep oint of this is to use controller primarily. can cop cars have
> flashing lights? am i getting ahead of myself? I just want to make sure your T's are crossed and
> I's dotted in the roadmap

## Triage

Ordered by how much each one ruins the game, not by how hard it is.

| # | What he saw | What it is | Severity |
|---|---|---|---|
| 1 | **Turrets aren't shooting** | A tower-defense game whose towers do not fire. Nothing else on this list matters next to it. | **Blocker** |
| 2 | **Skins scan-switch from skin to skin** | Crowd LOD identity churn. Bodies are assigned to the nearest N agents *by distance order, every frame*, so a body slot changes occupant constantly and the character model at a given spot flickers between people. My bug, introduced with the crowd. | **Blocker** |
| 3 | **Can't reach inventory or skills on a controller; prompts are keyboard-only** | ADR-002 and the pitch both say the controller is the design-centre input on both platforms, and half the game is unreachable on one. | **Blocker** |
| 4 | **Everyone is always walking, and in sync** | Two bugs wearing one coat: the walk clip plays whether or not the agent is moving, and the desync only varies phase and speed, not the clip. | High |
| 5 | **Map is cluttered with walls that block everything** | Mine, from this morning. I lengthened the fence lines to force a serpentine and overshot: 35-cell walls on a 48-cell map is a corridor, not a choice. | High |
| 6 | **Unclear what the green box is or what is being defended** | The vault has no label, no marker and no explanation. He guessed "my escape truck or whatever the protection point is", which is the game failing to state its own objective. | High |
| 7 | **Fewer, more robust enemies — quality over quantity** | A design call, and it is his to make. Worth taking seriously rather than defending the thousand-agent number. | Design |
| 8 | **Would pay ~$50 if it is the difference between State of Decay 1 and a real Dungeon Defenders re-envisioning** | A budget decision, offered. Needs a straight answer, not a shopping list. | Decision |
| 9 | **Can cop cars have flashing lights?** | Yes. Cheap, and exactly the kind of detail that makes a scene feel authored. | Small |

## What he is actually asking with "am I getting ahead of myself?"

No. Every item above is either a bug I introduced or a gap in something already decided. The one
genuinely forward-looking item is the $50, and the honest answer belongs in the roadmap rather than
in a purchase.

## Notes for whoever reads this later

- **"Quality over quantity" is a real design signal and it points somewhere specific.** The
  thousand-agent number came from the pitch's flood fantasy, and the premise ADR-003 replaced it
  with is *people*, not a tide. Fewer, tougher, more individually legible enemies suits the amber
  implant light, the crowd art and the performance budget better than a thousand capsules do.
  ADR-007's VAT crowd work gets cheaper too.
- **The wall clutter is a lesson about authoring, not about walls.** Staggering the gaps was right;
  making the walls nearly map-height to force it was not. A serpentine should come from where the
  gaps are, not from how much wall there is.
