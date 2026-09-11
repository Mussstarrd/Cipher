# ROADMAP — Mission One, Unguided

**Status:** proposed to the owner 2026-09-11 evening; **the live plan once he approves.** Supersedes
`PLAN-2026-09-11.md` (which is done) and retires `ROADMAP-graphical-beta.md` (built around a purchase
that was never needed — do not read it as current).
**Authors:** the engineer, an outside game-director brainstorm (memo in this session), and three owner
playtests in `docs/feedback/2026-09-11-*.md`.
**Scope of authority:** on approval the owner grants full creative control until the milestone below
passes. Nothing outside §5 gets built before it does.

---

## 1. Diagnosis

The gap is not fidelity. It is **response**. The game does not react to the player, and every note from
three playtests reduces to one of two sentences: *"I can't see what's happening"* or *"it doesn't push
back."* Verified in the code this evening, not inferred:

- `Loadout.EquipFromPack` / `SellFromPack` are tested and **called from nowhere**. Loot auto-equips or
  auto-scraps on kill behind a 3.5-second toast. Gear has never existed in the world and the player has
  never touched it. *"I don't understand where I'm picking up gear from"* is literally accurate.
- `death.anim` and `hit.anim` are imported and **not loaded** (`BodyClips`, FloodBootstrap.cs:1857).
  Enemies die by vanishing.
- `_InstanceColor` is plumbed through the shader and **written by nothing**. There is no hit flash, no
  camera shake, no FOV change, no recoil, no decal, no damage number.
- The **legacy orange blast cylinder is still drawn** (FloodBootstrap.cs:3070) on top of the new
  four-stage explosion. The owner has reported "orange cylinders" twice. He was right both times.
- The **amber implant light** — the one image ADR-003's premise is built on — is rendered nowhere
  except on the strike drone.
- 165 of 255 game tests guard machinery the owner has said three times he cannot see. A tested system
  with no screen is not half-done; it is zero-done plus maintenance.

**The engineering is no longer the bottleneck. Feedback loops are.**

## 2. The milestone

### MISSION ONE, UNGUIDED

Hand a controller to someone who has never seen this project. Say only *"play this."* Within twenty
minutes, with no coaching, on video, they:

1. reach Extraction on The Gate and arrive at The Service Road;
2. can afterwards say **what they were defending, what killed them, and why they left when they did**;
3. equip one piece of gear **on purpose**;
4. say one unprompted positive thing about how it looks.

Two outside players. Pass = both hit all four. Fix only what they trip on; then ship the build as the
first tagged release.

**Why this size.** Smaller ("polish the graphics") has no fail condition, and creative control without a
fail condition is how a solo project builds systems forever — which is what has been happening. Bigger
(Act One, co-op, Android, the VAT crowd) multiplies content against an interface nobody can read: eleven
more missions of illegibility. Mission 1 already exists as data; everything left is response,
readability and finish — which is exactly the work that makes missions 2–12 cheap. And it is the only
milestone **judged by someone who is not the owner**, which is what he needs before granting a long
leash.

## 3. Rules in force until the milestone

1. **The systems layer is frozen.** No new tower family, archetype, objective type, or progression
   subsystem. The one exception is healing, which reuses `PickupSystem` verbatim.
2. **A system lands with the screen that shows it, or it doesn't land.** Sibling to hard rule 2.
3. **Quality over quantity, adopted.** Target 40–150 enemies per wave that stagger, animate and die
   visibly. Keep the sim's thousand-agent capability; stop designing toward the number. The pitch's
   "density is the spectacle budget" line gets rewritten.
4. **No money before Phase D.** The building-kit question is asked once, at the D checkpoint, with two
   screenshots, and the owner decides with his eyes.
5. **Every phase ends with a build and a screenshot to the owner.** Every commit is small, green
   (all three local checks), and pushed.
6. **Honest estimate: three weeks**, not "about a week." The response layer is the entire gap.

## 4. Graphics polish — ranked by impact ÷ days

Constraint: cel/ink, primitives + generated textures, CC0 models, one engineer, no artist.

| # | Change | Why it reads as polish | Days |
|---|---|---|---|
| 1 | **Delete the legacy blast cylinder** | It is drawn on top of the real explosion. Highest ratio in this document. | 0.05 |
| 2 | **Amber implant light** — emissive quad on the head bone for promoted bodies, additive dot on the capsule tier | The only art that carries authored meaning. Turns "grey capsules" into *the crowd*. | 1 |
| 3 | **Camera work** — trauma-decay shake on fire/blast/damage, FOV 60→66 sprint / →54 aim, positional spring so the rig lags and settles | The cheapest expensive-feeling change that exists. Costs nothing at any agent count. | 1.5 |
| 4 | **Grounding** — soft dark disc under every agent and prop on the existing instanced disc path | One light, no AO, nothing touches the floor. In an ink direction blob shadows read as *drawing*, not as a cheat. Real SSAO needs a depth-normals pass the shader lacks; skip it. | 1 |
| 5 | **Hit response as ink** — two-frame white flash via `_InstanceColor`, generated radial ink-spatter billboard at the impact, `death.anim` with the crowd slot held for its length, `hit.anim` on non-fatal damage, `punch.anim` in contact | Nothing reads more amateur than enemies blinking out. | 1.5 |
| 6 | **Ground decals** — scorch rings, drag marks, dark patches where bodies fell; projected quads, generated alpha, ZWrite off | The battlefield accumulates a history. It is what most says "someone played here." | 1 |
| 7 | **Vertex-colour AO on the box props** — corner/base darkening baked into the colour channel at build time, multiplied in the shader | Turns boxes into made things at zero runtime cost. | 1.5 |
| 8 | **Far-crowd silhouette tier** — beyond ~45 m swap the capsule for a camera-facing quad with a generated 4-frame flat-ink walk; fall back to a low-poly instanced humanoid if the quads read wrong from the tactical camera | At distance this style *is* silhouette. Buys headroom to raise the promoted count near the camera. | 2 |
| 9 | Two free fixes: `m_SoftShadowsSupported: 0` silently downgrades the soft shadows the lighting asks for; `AmbientMode.Trilight` is inert because `InstancedLit` never samples SH | Both are on and doing nothing. | 0.25 |

**When to buy a building kit.** `Resources/Environment` has no buildings at all; the guardhouse is
boxes. Buy when **both** hold: (a) a stranger has played the slice and the complaint is *"the buildings
look fake"* rather than *"I can't tell what's happening"* — money spent first buys a prettier confusing
game; (b) the free CC0 shelf has been checked, since the entire current pipeline came off it for
nothing. Non-negotiables for whatever is bought: **modular** pieces, not sealed house props (the
campaign re-cuts one community twelve times and needs clubhouse and fire-station interiors); **flat
colour or atlas materials at Quaternius poly density** — one normal-mapped PBR house next to a
Quaternius tree kills the direction instantly; **hard edges and clean silhouettes**, because the
inverted-hull outline turns high-frequency geometry into mush; **one vendor covering suburban plus
civic/utility**, so missions 3, 7 and 11 look like the same county. Rig is irrelevant; buildings do not
animate.

## 5. Legibility & feel — in order

1. **Make gear a thing you touch.** Notable kills drop a visible object with a rarity-coloured ink
   glyph and a beam. Walk over it to take it. Wire `EquipFromPack`/`SellFromPack` to a cursor in the
   kit screen. Show one number and an arrow. Auto-scrap becomes a visible *"+12 scrip"*. This one
   change answers three of his complaints.
2. **Enemy hit and death** (row 5 above). Plus ~40 ms of *render-only* hitstop on a kill — never the
   sim; determinism.
3. **The screens are a comic page, not an RPG list.** Hard 3 px black border, off-white paper fill,
   halftone in the gutters, one amber accent, unequal boxes like a page layout. Rule: *every screen
   shows a picture of the thing, never a list of it.*
   - **Kit** = an ink line-art paper doll with eight slots on leader lines; equipped vs candidate side
     by side with a large ±.
   - **Skills** = a *dossier*, not a node graph: columns of stamped boxes, taken ones stamped amber,
     locked ones in faint pencil.
   - **Truck** = a side elevation of the bed you slot boxes into, two ink gauges for weight and volume,
     and **what gets left behind drawn with equal weight** — the decision is about loss, not capacity.
   All generated textures and GUI rects; no art files.
4. **Say what's coming and what it cost.** Wave start: one card in the protagonist's voice naming the
   composition, with arrows on the minimap for spawn edges. Wave clear: killed / earned / time spent /
   cycle remaining. Extraction: *"You held 6 waves. That bought 4 minutes of setup at the Service
   Road."* Today the briefing is `WAVE 12/40 spawned`.
5. **Health, healing, damage direction.** Aid kits via `PickupSystem` (which today spawns only gun
   crates), slow out-of-combat regen, a screen-edge ink wedge for damage direction, a vignette driven
   by `HealthFraction`. He has never said "that killed me" because he cannot tell.
6. **Objectives as world markers.** Clamped screen-edge chevrons with metres on the vault, actors and
   truck. The vault names itself in world space, permanently.
7. **Device-aware glyphs everywhere.** Never a keyboard key when a pad is present.

## 6. Sequence

| Phase | Exit criterion | Contents | Days |
|---|---|---|---|
| **A — Response** | A kill looks and sounds like a kill. | Delete legacy blast cylinder; the two free render fixes; hit flash + ink spatter; death/hit/punch wired, slot held; render-only hitstop; camera trauma / FOV / spring; hero muzzle flash and shell. | 4–6 |
| **B — The player can see themselves** | A stranger states what they defend, what attacks, what hurt them. | Damage direction + health vignette; aid kit + regen; world-space objective markers; wave-start card, wave-clear summary, after-action sentence; device-aware glyphs. | 4–5 |
| **C — The comic page** | The owner opens the kit screen and does not say "gibberish." | World gear drops; equip/sell wired; kit, skills and truck rebuilt in the page language; truck shows the bed and what stays. | 5–7 |
| **D — Finish the picture** | A harness screenshot could go on a store page. | Amber implant light; grounding discs; decals; vertex AO; far silhouette tier. **Checkpoint: the building-kit question, with two screenshots.** | 5–6 |
| **E — Stranger test** | The acceptance test passes on video. | Two outside players, no coaching. Fix only what they trip on. Tag the release. | 3–4 |

Each phase ends with a build in `game/Builds/` and screenshots sent to the owner.

**Cut or deferred past the milestone:** VAT crowd (silhouette tier makes it less urgent and the owner
asked for fewer agents); couch co-op; Android; authoring missions 4–12; the lab release; more concept
art; DOTS; any new tower family, archetype or progression subsystem; the actors' own UI beyond the
objective panel.

**Immediately after the milestone:** author **mission 4 — The Gate Falls**, the first unwinnable
mission. Twelve missions of fighting retreat is a superb structure and also twelve missions of
unwinnable-mission UX that has never been tested once. If *"you lost and that is correct"* does not
read, the act shape is wrong, and that has to be learned at mission 4, not mission 11.

## 7. Risks, and where we disagree

- **The engineer over-builds systems relative to legibility.** Agreed, and the freeze in §3 is the fix.
  The test counts are the evidence.
- **"About a week" was wrong.** It covered the level, not the response layer. Three weeks, said up
  front; a missed self-set date costs more trust than an honest longer one.
- **The far-crowd silhouette quads may read wrong from the pitched tactical camera.** The engineer's
  reservation; the expert thinks the style is already silhouette at that range. Resolution: try quads
  first, keep the instanced low-poly humanoid as the fallback, decide from a screenshot.
- **The cop-car lights class of request is garnish, and the meal is not cooked.** Keep taking them —
  they are how the owner says the world feels unauthored — but they do not move phases.
- **Money.** The engineer told the owner animation did not need it and was right. Buildings are a
  specific, defensible gap. Do not spend during A–C; present at D and let him decide with his eyes.

## 8. What the owner is being asked to approve

1. The milestone as written in §2, judged by two outside players on video.
2. The systems freeze in §3, including "quality over quantity" as a stated design pillar.
3. Three weeks as the honest estimate.
4. The building-kit purchase deferred to the Phase D checkpoint.
5. He finds the two outside playtesters for Phase E (the open item in `OPEN-DECISIONS.md`).
