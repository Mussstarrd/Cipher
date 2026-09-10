# Design memo — Aimable airstrike, economy + towers v0, tower-hunter archetype

**Date:** 2026-09-10 · **Author:** ENG/design (brain-trust track 2 of 3) · **Status:** proposal, owner veto
**Inputs:** [owner notes](../feedback/2026-09-10-hero-first-play.md), [pitch](../02-FLAGSHIP-PITCH.md), [blueprint](../03-DAY1-EXECUTION-BLUEPRINT.md) (M2/M3, Traps 5/7), [ADR-002](../decisions/ADR-002-camera-model.md), [Open Decisions](../decisions/OPEN-DECISIONS.md) #3 #5 #6, `HeroModel.cs` / `FloodBootstrap.cs`.
**Sibling memo:** `breach-and-repair.md` owns the breacher, eat-through and repair; referenced, not duplicated.

## TL;DR

1. **Airstrike = aim by looking.** Marker rides the camera ray to the ground (min 6 / max 24 cells); tap Y fires, hold Y previews and fires on release. Requires RS-Y camera pitch, which the M3 gun pass needs anyway.
2. **The strike becomes a line**, 14×4 cells along the look axis, six bombs walking far→near, 1.2 s inbound delay, 8 s cooldown. That is the pitch's "street erupts in a line".
3. **Cash** is the currency: $5 per kill from any source, $100×wave clear bonus, $400 start. Barricade $20 / 200 HP; Sentry .50 turret $150 / 300 HP / range 10 / 60 dps / targets "first". Full seals are legal and loudly flagged, never refused.
4. **Build mode = LB toggle** into the overhead rig; LS steps a grid cursor (8→16 cells/s), A place (hold to paint), X sell, RB/LT cycle, D-pad quick-select, B exit. Kill criterion: 6 barricades + 2 turrets in <30 s, no misplacement.
5. **Spitter** (tower-hunter): 0.5 % of spawns plus a 40 s pity timer, prefers turrets within 12 cells, lobs acid from 9 cells at 10 dps. Turrets do not prioritise it by default — the hero must.

---

## A. Aimable, longer airstrike

ADR-002 constraint: chase camera, hero faces where the camera looks. Today the camera sits at distance 9 / height 5 with fixed pitch and the marker is glued 8 cells ahead. Owner verdict: too short, blind.

### Options (score 1–5; higher is better)

| Scheme | Precision @15–25 cells | Time-to-fire under pressure | Thumb contention with orbit | Marker readability from chase cam | KB/M parity |
|---|---|---|---|---|---|
| (1) Hold Y → RS slides marker in a range disc, release fires, B cancels | 4 | 2 (1.0–1.5 s of stick travel at 12 cells/s) | 2 (camera freezes while held) | 3 (off-centre, far) | 4 |
| (2) Marker at the camera-ray ground hit, tap Y, range clamp | 3 (pitch→distance is non-linear: 0.7°/cell at 25) | 5 (tap) | 5 (aim = look) | 4 (screen-centre) | 5 |
| (3) LT zoom + RS grenade arc | 4 | 3 (two inputs) | 4 | 4 | 4 |

(3) collides with LT = ADS in the M3 gun pass. (1) has the best raw precision but freezes the camera mid-fight. **(2) wins 4 of 5 columns; hold-to-preview and a small snap close its precision gap.**

### Recommendation: (2) with hold-preview

- **RS-Y = camera pitch** (new): −10° to +55°, 90°/s at full deflection, response exponent 1.6, default 20° down.
- **Marker** = ground hit of the screen-centre ray, clamped to **6 ≤ range ≤ 24 cells** (gun range is 25: strike only what you can shoot). Ray misses ground → marker at 24 along facing.
- **Visible whenever ready**, as the full 14×4 strip; while charging, a 2-cell outline dot so "tap" is never blind.
- **Tap Y** (<150 ms) fires now. **Hold Y** locks the marker, tints it red, fires on release; **B** cancels at no cooldown cost. Mouse: RMB, same semantics.
- **Snap:** 3+ agents within 2 cells of the centre pull the strip 1 cell toward their centroid; never rotates the line.
- **Cooldown 8 s** (from 6; 56 cells covered vs. 38). Respect-meter charge replaces the timer when the meter ships.

### Telegraph (the trailer moment)

- **Shape:** line 14 × 4, along the look axis, centred on the marker: six bombs at 2.33-cell spacing, 50 damage each in radius 2.0. Parallel to the lane, so runners charging you stay inside it through the delay (3.6 cells covered in 1.2 s at 3 cells/s).
- **Delay:** 1.2 s to first impact, then bombs walk **far→near** at 80 ms intervals, so the horde runs *into* the explosions.
- **During the delay:** orange strip decal, smoke flare at the marker, "INBOUND" bark. The Bloom does **not** react in v0 (mindless is the fantasy). The hero takes 25 damage inside a bomb radius: min range 6 is a guard rail, not immunity. No structure damage in v0.
- Reference: Helldivers 2's Eagle strikes are beacon → fixed call-in delay → linear pattern relative to the throw axis, and read at distance because both are constant ([Eagle wiki](https://helldivers.wiki.gg/wiki/Eagle_Gas_Airstrike), [Eagle tips](https://www.thegamer.com/eagle-stratagem-tips-helldivers-2/)).

### Tests

- `AirstrikeMarker_ClampsBetweenMinAndMaxRange` — ray hits at 3 and at 40 cells produce 6 and 24.
- `AirstrikeMarker_RayMissesGround_FallsBackToMaxRangeAlongFacing`.
- `AirstrikeLine_KillsAlongLookAxis_SparesAcross` — agents 6 cells along the axis die, agents 3 cells across survive.
- `AirstrikeDelay_ImpactsOnScheduledTicks_Deterministic` — pending strike is sim state, resolved by tick count, hash-stable.
- `AirstrikeHold_CancelWithB_NoCooldownSpent`.
- `Airstrike_HeroInsideRadius_TakesSelfDamage`.

### Risks / cut list

Pitch control might make the gun feel worse before aim assist lands (mitigate: pitch only affects the marker until M3 tunes ADS). Cut first: snap magnetism, then hold-preview (tap-only still meets the ask).

---

## B. Economy + towers v0

### Options considered

Income: (a) per kill only; (b) per wave only; (c) kill + wave bonus. (b) makes hero kills worthless, (a) makes wave 1 a grind: **(c)**. Refund: Dungeon Defenders pays 100 % inside the same build phase and health-scaled after ([Sell Defense](https://dungeondefenders.fandom.com/wiki/Sell_Defense)); Orcs Must Die only sells between waves ([OMD traps](https://orcsmustdie.wiki.gg/wiki/Traps)). We take DD's rule: mid-wave selling stays a real decision.

### Numbers

| Item | Value |
|---|---|
| Currency | **Cash ($)**, money-green, count-up ticks. Rackets (passive block income) are Track 3 and do not exist in v0. |
| Income | $5 per kill, any source (hero, turret, airstrike). $15 bounty on a Spitter. Wave clear: $100 × wave index. No passive income. |
| Starting bank | $400 (10 barricades + 1 turret, or 20 barricades). |
| Barricade | $20, 1 cell, 200 HP. Refund 100 % in setup; mid-wave 50 % × health fraction. |
| Sentry .50 (the "turret cube") | $150, 1 cell, **blocks pathing like a barricade**, 300 HP. Range 10, 20 damage/shot, 3 shots/s = 60 dps (6 runners/s). Refund as barricade. |
| Targeting | **"First"**: lowest remaining flow-field distance to the vault among agents in range; ties → lowest agent id (determinism). |
| Combat-price markup (M3) | ×1.5 while a wave is live; refunds unchanged. |
| Wave 1 sizing | ~60 runners → ~$400 income, so wave 2 setup affords a second turret. |

### Placement rules

Grid-snapped, 1 cell. Refused: occupied cells, spawn gates plus a 2-cell apron, the vault, the hero's cell, any cell containing an agent (no wall-crushing). **Full seals are allowed** ("blocking is legal — and dangerous"). Detection is free: the flow field already reports unreachable gates after every rebuild. When a gate is unreachable, build mode paints its *chew route* in red (least-HP path; mechanics in `breach-and-repair.md`) and the HUD stamps "SEALED — they will chew through here". That is Open Decision #5's covenant made concrete: truthful for the current field, loud telegraph for what changes it. The M2 "turtle loses legibly by wave 5" check is enforced by the Spitter and the breacher, not by refusing placement.

### Upgrade hook (Trap 7)

Ships in data from the first turret, with nothing purchasable in v0:

```
EmplacementDef { id, displayName, cost, footprint, hp,
  base: { range, damage, fireInterval, targeting },
  paths[3]: { name, tiers[5]: { cost, effects[] } } }
Effect = StatMod(stat, add, mul) | OnKillTrigger(kind, value) | Aura(radius, stat, mul) | ProjectileSwap(id)
```

A validator encodes the Bloons crosspath rule: two paths max, only one past tier 2 ([Crosspathing](https://bloons.fandom.com/wiki/Crosspathing)). The .50's pitch paths (Sustained Fire / Heavy Loads / Crew Doctrine) exist as named, empty tier lists; one test applies a synthetic StatMod to prove the pipeline.

### Build-mode controller UX (overhead, north-up)

- **Enter/exit:** **tap LB toggles** build mode (ADR-002: toggle, not hold); the peek-hold is retired, build mode *is* the peek. Camera lerps overhead in 0.25 s, weapons holster, preview on. **B** or LB exits. KB/M: Tab.
- **Cursor:** LS steps an integer-cell cursor: first step immediate, repeat at 8 cells/s, ramping to 16 after 0.6 s held; deadzone 0.2. Starts on the hero's cell; camera follows with a 4-cell deadband. RS-Y zooms view height 18–40 cells; RS-X unused (fixed yaw matches the preview).
- **A** place; **hold A + move** paints barricades along the cursor path (what makes 6 walls in 30 s possible). **X** sell under cursor. **RB / LT** cycle item. **D-pad:** up barricade, right turret, down reserved for repair (breach memo). **Menu** pause. Mouse: free cell cursor, LMB/RMB, scroll, 1/2 hotkeys.
- **Feedback:** ghost turns red with a one-word reason ("OCCUPIED", "GATE", "BODIES"); bank flashes red when short. Placement is a sim command applied next tick, never from the render layer.
- **Mid-wave (M3):** same controls, ×1.5 prices; the hero is still a body in the street and takes contact damage while you build.
- **Kill criterion (Open Decision #6 adapted):** from chase mode, the owner places 6 barricades and 2 turrets in **<30 s with zero misplacements** (a sell within 3 s of placing counts as one), 2 of 3 attempts, stopwatch in the build. **Fallback:** confirm-on-release if paint fails; a free analogue cursor with 0.5-cell snap if the stepped cursor fails.

### Tests

- `Bank_KillFromAnySourcePaysFive_WaveClearPaysHundredTimesWave`.
- `Placement_RefusesOccupiedGateApronVaultAndAgentCells`.
- `Placement_FullSealAllowed_MarksGateSealedAndExposesChewRoute`.
- `Refund_FullInSetup_HalfTimesHealthMidWave`.
- `Turret_TargetsFirstByFieldDistance_TiesByLowestId`.
- `EmplacementDef_LoadsFromData_CrosspathValidatorRejectsThirdPath`.
- `StateHash_CoversStructuresBankAndPendingStrikes`.

### Risks / cut list

$5/kill at 300–500 agents on screen may inflate; the wave bonus is the knob, not the kill price. Cut order: paint-placement → zoom → mid-wave building (keep it setup-only until M3 proves the loop).

---

## C. Tower-hunter archetype: the Spitter

The owner's "different .05 %" is the pitch's Spitter. 0.05 % of a 60-runner wave is 0.03 spawns; the rule below keeps it rare *and* guaranteed on screen.

### Numbers

| Rule | v0 value |
|---|---|
| Spawn | 1 in 200 spawns (0.5 %) **plus** a pity timer: first at 40 s of live-wave time, then one per 45 s minimum. Max 3 alive. A 3-minute session sees 3–5. |
| Body | 60 HP (10 hero rounds, 1 s of turret fire), 2.4 cells/s, 1.6× runner scale, acid-green rim light. |
| Targeting | Every 15 ticks: nearest **turret** within 12 cells; else nearest barricade within 12; else follow the field like a runner. |
| Attack | Halts at 9 cells; one glob per 1.5 s, 1.0 s flight, 15 damage, 1.5-cell splash (hero takes 10 in it). 10 dps: a turret dies in 30 s alone, 10 s with three. |
| Destruction | Turret at 0 HP: cell opens, field regens, wreck decal, no refund, HUD "SENTRY LOST" with lane arrow. Selling it damaged pays the health-scaled refund: the "pull back or hold" decision. |
| Telegraph | 1.0 s wind-up on acquire: green tether to the target, screech, target outline pulses. Globs are 0.6-cell spheres with trails, readable at 25 cells from the chase cam. |

**Why the turret does not simply kill it:** range 9 sits inside turret range 10, but the turret targets "first" and the Spitter hangs back behind the runner front, so the turret keeps mowing runners while being melted. The archetype's argument: *your DPS needs an escort.*

### Counterplay

1. **Placement depth** — a turret 10+ cells behind the front barricade is unreachable until the Spitter walks the maze past the guns.
2. **Hero prioritisation** — 60 HP, $15 bounty, slow; the tether says where to look.
3. **Later:** overwatch — a second turret on "Strong" targeting, or Crew Doctrine target-calling (data hook in B).

### Split with the breacher

Breacher (sibling memo): melee, opens a widening hole that **re-routes** the flow; punishes a maze with no repair budget. Spitter: ranged, prefers **turrets**, kills **damage output** not routing; punishes a maze with no hero attention and no overwatch. v0 Spitter hits barricades only when no turret is within 12 cells, so the two never contest the same wall. Together the pure funnel fails from two directions: the wall breaks or the guns go quiet.

### Tests

- `Spitter_PityTimer_GuaranteesOneBy40sAndOnePer45s`.
- `Spitter_PrefersNearestTurretOverNearerBarricade`.
- `Spitter_NoStructureInRange_FollowsField`.
- `Spitter_HaltsAtNineCells_FiresEvery45Ticks_Deterministic`.
- `Turret_DestroyedAtZeroHp_OpensCellAndRegensField`.
- `Spitter_StateHashStableAcrossRuns`.

### Risks / cut list

Lobbed projectiles are a new sim primitive (ballistic arc, splash); if it slips, ship hitscan acid with the same numbers and the arc as render-only. Cut order: splash → hero damage → wind-up tether (never cut the tether last; it is the counterplay).

---

## Questions for the owner

1. Airstrike charge: keep an 8 s timer for v0, or start the Respect meter now (roughly 25 kills per charge) so the ultimate feels earned from day one?
2. Cash comes only from kills and wave bonuses in v0 — no passive racket income until the empire layer. Acceptable for the "one more wave" test?
3. LB becomes the build-mode toggle and the look-only peek disappears. If you want to keep peeking without holstering, say so and it moves to the View button.
