# Owner playtest — Hero graybox, first play

**Date:** 2026-09-10
**Build:** local preview of `549a7f6` (hero + ADR-002 chase-only combat), Windows player
**Input:** Xbox controller

## Owner notes (verbatim)

> fantastic and i love the view. we should complicate the maze a bit and have like a .05% spawn rate of a "smart" enemy that starts like, building a demo kit or wall breach or something that'll break a hole in a wall that'll start letting enemies be attracted to path of least resistance and hole in wall starts as hole big enough for 1 zombie per second and progressively gets worse until the wall collapses there or i go buy and initiate a repair module or drone or whatever. also, the air strike would be better with a little more ditance. the health bar and enemy kill and spawn and stuff is working. cool stuff. now we'll need to be able to aim that further out blast so we arent just blindly facing and pressing the y button. we are going to need to figure out the tower building portion as well. maybe kills add currency and then build towers that maybe a different .05% are attracted to to attack and kill. ingest those ideas, sping up a brain trust to research and decide best paths forward and lets keep moving forward! let me know when I have something cooler to play and give me an idea of when we may even see some skins on these characters so i can manage expactations

## Engineering annotations

- **Camera (ADR-002):** "i love the view" — chase third-person confirmed by the owner after the fact. ADR-002 stands.
- **Working as intended:** HP drain from contact, kill counting, spawner top-up, airstrike firing.
- **Asks extracted:**
  1. *Breacher archetype* (~0.05% of spawns): plants a demo kit on a wall; a hole opens sized for ~1 runner/s and widens over time until the wall segment collapses; the horde re-routes through it ("path of least resistance" — which is exactly what the flow field does the moment a cell opens); countered by a purchasable repair module/drone. Maps onto the blueprint's **Wrecker** (wave 4) and the M2 "barricade HP + horde eat-through" line, with the owner's twist that it is a *rare, targeted* saboteur rather than a brute.
  2. *Airstrike*: more distance, and **aimable** — not "face and press Y".
  3. *Economy + towers*: kills → currency; place towers; a second rare archetype (~0.05%) that hunts towers. Maps onto blueprint M2 (one turret type) + the pitch's **Spitter** (targets structures).
  4. *Brain trust*: owner explicitly asked for parallel research/design agents to decide paths forward.
  5. *Expectation-setting*: when do capsules become characters/skins?
- Brain-trust outputs land in `docs/design/`; synthesis + timeline in `docs/design/ROADMAP-2026-09.md`.
