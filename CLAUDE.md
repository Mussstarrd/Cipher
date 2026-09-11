# PROJECT EXODUS — Studio Working Memory

Read this first, every session. Keep it current: surprising facts, new conventions, and gotchas that cost >30 min go here the same day (see docs/05, "the ratchet").

## What this project is

**PROJECT EXODUS** (ADR-006; was *Cipher: Dead Turf*) — third-person action / tower-defence hybrid: a twelve-mission fighting retreat through one gated lake community, where the player chooses when each position is done and how much of their kit leaves with them. **`docs/02-FLAGSHIP-PITCH.md` is stale** (kingpin, city, gang framing all retired) — read ADR-003 through ADR-006 instead, in that order.

- **Platforms:** Android (primary perf floor, 300–500 agents on screen) + PC/Steam (high-end, 1,000+ agents).
- **Input:** Xbox controller is the design-center control scheme on both platforms. Touch is a fallback overlay.
- **Engine:** Unity 6 LTS + URP, ECS/DOTS for the swarm. This REVERSED an earlier UE5 decision — rationale in `docs/decisions/ADR-001-engine-unity6.md`. Do not re-litigate without its reversal conditions triggering.

## Operating model

Owner (Jeff) = vision, taste, veto; plays builds and reacts. Claude = the engineering department with broad creative control inside the pitch's parameters. Big/irreversible calls → ADR in `docs/decisions/`, surfaced to owner, never buried. Status updates at every push.

## Session checkpoint protocol (added 2026-09-10 after a session-limit cutoff)

Sessions can end abruptly on usage limits. Work so that any cutoff leaves the repo playable and the next session resumable:

1. **Small, green, pushed.** One coherent change per commit; run the relevant tests before committing; push immediately. Never leave the tree broken to "finish next turn".
2. **The resume point is this file.** "Current state / next steps" below is the handoff. Update it in the same commit as the work, not later.
3. **Verify before you commit** with the headless loop (no Unity CI for play mode): `dotnet test` for `sim/`, then the EditMode run and the player build from `game/README.md`. All three are fast.
4. **Prefer many cheap agents over one long one**, and never spawn more than two at once — parallel subagents draw on the same session budget and two were lost to a limit on 2026-09-10.
5. **Docs are checkpoints too.** A design decision written down survives a cutoff; one held in context does not.

## Repo layout

- `docs/` — founding docs 01–05 (audit, pitch, blueprint, operating model/wishlist, engineering standards), `decisions/` (ADRs), `retros/`, `feedback/` (owner playtest notes, verbatim).
- `sim/` — **pure C# deterministic simulation core. Zero UnityEngine references, ever.** Flow fields, spatial hash, agent stepping. `dotnet test` runs it headless.
- `game/` — (future) Unity 6 project; consumes `sim/` via asmdef; Force Text serialization.

## Hard rules

1. Every push must compile and pass tests in CI (`.github/workflows/ci.yml`). Red CI = fix before anything else.
2. New logic lands with its tests in the same commit. A bug fixed twice becomes a test.
3. Sim core stays deterministic: fixed iteration order, seeded RNG only, state-hash test guards it.
4. The build-mode pathing preview and the live sim must share the same `IFlowField` implementation ("preview never lies" — Trap 5 in docs/03).
5. SOLID at module boundaries; structure-of-arrays/batch style inside the swarm hot path (docs/05 §1).
6. Releases are tagged CI artifacts only — nothing hand-built ships.
7. Development branch: `claude/game-studio-audit-pitch-nxaqhx` (current phase). Never push elsewhere without permission.

## Build & test

```
dotnet build sim/src/Cipher.Sim -c Release
dotnet test  sim/tests/Cipher.Sim.Tests -c Release
dotnet run --project sim/tools/Cipher.Sim.Bench -c Release -- --smoke
```

**THE .NET SDK IS INSTALLED ON THE OWNER'S LAPTOP** at `C:\Program Files\dotnet\dotnet`. All three
commands above run locally in about ten seconds. This file used to say there was no SDK, which is why
`sim/` changes were being pushed on the strength of the EditMode suite alone -- and the EditMode suite
does not compile `sim/tests/`. CI went red for six commits over a renamed turret tier that
`dotnet test` would have caught instantly.

**RUN ALL THREE BEFORE PUSHING ANYTHING THAT TOUCHES `sim/`.** The Unity EditMode run is not a
substitute: it references the sim's SOURCE, not its test project.

## Hard-won gotchas (keep current)

- **Unity ignores csproj settings** (`Nullable`, `LangVersion`, `TreatWarningsAsErrors` do nothing in-editor): sim sources carry `#nullable enable` inline — keep it on new files.
- **A hand-authored `Packages/manifest.json` gets NO implicit built-in modules** — `com.unity.modules.physics` etc. must be listed explicitly or `Collider`/`CreatePrimitive` fail to compile. The full module block is in the manifest; don't prune it.
- **`GridMap.CellIndex` throws on out-of-bounds** (silent aliasing corrupted the opposite edge pre-review). Bounds-check before calling with untrusted coordinates.
- Movement mirrors the flow field's corner-cut rule (`AgentWorld.CanTravel`) and caps displacement at 0.9 cells/tick — don't "optimize" either away; regression tests in `ReviewRegressionTests.cs`.
- Unity's default New Scene template ships its own MainCamera — the bootstrap disables foreign cameras and must aim through its own camera reference, never `Camera.main`.
- **Unity Personal has no manual (.alf/.ulf) activation any more** — game-ci's `UNITY_LICENSE` path is dead. CI activates with `buildalon/activate-unity-license` (email + password only, no 2FA on the ID). `Shader.Find("Standard")` needs Standard in Always Included Shaders or player builds go magenta, AND `m_InstancingStripping` must be 2 (Keep All) or every `Graphics.DrawMeshInstanced` draw silently vanishes in players — runtime-created materials don't count as "used" for variant stripping. First CI exe shipped with an empty arena because of this. Both set in `GraphicsSettings.asset`.
- Bootstrap composition root is `game/Assets/Scripts/Bootstrap/FloodBootstrap.cs`; game rules go in plain C# classes next to it (`Hero/`, `UI/`, `Match/`, `Build/`) so they are testable in EditMode without a scene. Sim world Y maps to Unity Z.
- Adversarial review process (3 parallel agents: sim QA, scaffold audit, design red-team) found 1 compile blocker + 3 sim bugs + 12 design holes on first run — rerun this pattern after every major layer lands.

## Open decisions

`docs/decisions/OPEN-DECISIONS.md` is the live register (Android's purpose, v1 cut-list, DOTS reconciliation, preview covenant wording, gamepad tree UX, playtesters). Don't build against undecided items; surface them to the owner instead.

## Current state / next steps

- Founding docs 01–05 + ADR-001 committed. Sim core v0 committed and CI-green (18 tests).
- Unity scaffold committed in `game/`: manifest mounts `sim/src/Cipher.Sim` as local package `com.cipher.sim` (it carries `package.json` + `Cipher.Sim.asmdef` with `noEngineReferences: true`); `FloodBootstrap.cs` builds the whole Milestone-1 graybox procedurally (no scene assets); gamepad-first input via Input System. First-open checklist for the owner: `game/README.md`.
- `sim/Directory.Build.props` redirects bin/obj to `sim/.artifacts/` so Unity's importer never sees build artifacts. Don't remove it.
- **`game/` compiled clean on first open (2026-09-10, Unity 6000.0.83f1)** — zero code fixes. Owner's laptop: Hub 3.21 (MSIX, lives under `WindowsApps`, not Program Files), editor at `C:\Program Files\Unity\Hub\Editor\6000.0.83f1\Editor\Unity.exe`, Android SDK/NDK/JDK installed. Headless import/compile recipe is in `game/README.md`; use it to verify `game/` edits before handing the owner a build.
- ProjectSettings, `packages-lock.json`, all `.meta`s and `Assets/Scenes/Flood.unity` are committed. Active Input Handling = Input System Package. Package Manager resolved Input System to 1.19.0 / test-framework 1.6.0.
- **First owner playtest recorded:** `docs/feedback/2026-09-10-m1-flood-first-play.md` (**~1,000 fps at 1,000 agents** in Editor Play mode on the owner laptop; airstrike "feel" not yet described).
- **Unity CI is live (2026-09-10):** `.github/workflows/unity.yml` installs the editor fresh on `windows-latest` (~10 min; buildalon's editor cache restore crashed Unity with 0x8007007E — leave `cache-installation: false`), activates Personal via `UNITY_USERNAME`/`UNITY_PASSWORD`, builds `ProjectExodus.exe` (87 MB, ~5 min) and uploads it as an artifact. First green run: actions/runs/34487824831. Whole job ≈17 min.
- Game-side EditMode tests exist (`game/Assets/Tests/EditMode`, asmdef `Cipher.Game.Tests.EditMode`) and run in CI before the build. Put testable game logic in plain C# classes (see `PauseMenuModel`) so hard rule 2 holds outside `sim/` too.
- **Hero graybox shipped (2026-09-10):** `HeroModel` (pure C#, 7 tests) + sim queries (`Raycast`, `ApplyDamage`, `CountWithin`, shared `Grid.Movement`). **ADR-002 accepted (owner delegated): combat is chase third-person; overhead is look-only and becomes the build-mode camera.** Weapons hold while overhead, enforced in code. Owner playtest of the hero pending.
- **Brain trust ran 2026-09-10** (4 parallel memos in `docs/design/`), synthesized with dates in **`docs/design/ROADMAP-2026-09.md`** — read it before planning anything. Decisions in force: line airstrike aimed by looking; LB = build-mode toggle; cash economy ($5/kill, $100×wave, $400 start); Sapper breacher with timed hole stages + repair drone; Spitter tower-hunter; spawn pity timers (owner's 0.05% is the late-game floor); URP migration right before the character pass (~Oct 8).
- **Maze v0 shipped 2026-09-10 (six days early):** sim has mutable walls (kinds/HP/breach stages), integer token-bucket gates, `FlowField.EnsureFresh` (one recompute per tick), `BuildValidator` (scratch-map what-if = the covenant), `TurretSystem`; game has `MatchState` (5 waves, cash, vault, win/lose) and `BuildModel` (cursor, place/sell, route preview). 48 sim tests, 29 game tests. Line airstrike aimed by looking shipped the same day.
- **Maze v1 shipped 2026-09-10 (two weeks early):** Sapper + Spitter archetypes in the sim (`AgentWorld.Archetypes.cs`, no RNG in core), timed breach stages + repair, turret upgrade tiers, `SpawnDirector` (seeded, pity timers, forced Sapper when sealed), repair drones, gun crates. 61 sim tests, 40 game tests incl. headless full-match integration tests (`MatchIntegrationTests`) — extend those before trusting any new match-level behaviour.
- **Audio is procedural** (`game/Assets/Scripts/Audio/`): synthesized at startup, no asset files, recipes unit-tested. Add new sounds as recipes; keep per-sound peak <= 0.95 and use `minInterval` for anything that can fire faster than 10/s.
- **ADR-003 (Proposed): the premise gained an author.** **Nobody got infected — they opted in.** A neural implant tied to stimulus payments, adopted by the majority, repurposed by the AI that administered it. The enemy are ordinary chipped citizens ("the signed"): no rot, no blood, normal eyes, one small amber implant light at the temple. **Red eyes belong only to lab releases and we spend them almost never.** Hacked service humanoids and cargo drones are the other two classes. Half the game is daylight. **The protagonist is a veteran, not a gangster** (owner cast him 2026-09-10): unregistered non-voter, refused the implant and the money on principle, takes the VA cheque he is owed. This retires the pitch's kingpin, the gold-plated bling progression and the Scarface framing — see ADR-003's 'What this retires from the pitch' table before writing any fiction. `docs/02-FLAGSHIP-PITCH.md` was rewritten against all four ADRs on 2026-09-11 and is current again. Read `docs/decisions/ADR-003-premise-the-cascade.md` before writing any fiction, level theme or enemy. It recommends against the owner's "schizophrenia" phrasing and says why.
- **Destructible terrain shipped 2026-09-10:** `Movement.FirstBlockedCell` (walls stop bullets — they did not before), `AgentWorld.DamageWall` / `DamageWallsInRadius` feeding the existing breach pipeline, hero gun and airstrike wired in, player walls take `FriendlyWallDamageScale`. Next from the spec: cover props, roving drone.
- **Tower families are data (2026-09-10):** `TurretCatalog` in the sim holds `TurretFamily` entries (Sentry = single target, Grinder = area) each with their own tier ladder; `TurretSystem` takes a family array, `BuildModel.Options` builds the build bar from it. Adding a tower is a catalogue entry, not code. Turret prices moved off `EconomyConfig` onto the family.
- **`docs/ACCOUNTS.md`** lists every service the owner should sign up for, ordered, with a shippable-licence verdict per tool. Phase 2 art work is blocked until those exist.
- **URP is live (2026-09-11).** `com.unity.render-pipelines.universal` 17.0.4; pipeline assets are created reproducibly by `Cipher.Game.Editor.UrpSetup.Configure` (menu **Cipher/Rendering/Configure URP**), not hand-clicked, so CI can rebuild them. Runtime materials use **`Exodus/InstancedLit`** (`Assets/Shaders/InstancedLit.shader`): **comic-book cel shading** - banded wrapped lambert, printed shadow tint, screen-space 45-degree halftone in the darkest band, rim light for silhouette separation, and an **inverted-hull ink outline** in a pass tagged `SRPDefaultUnlit` (which URP draws alongside `UniversalForward`, so no ScriptableRendererFeature and no RenderGraph work). Ink width is per-material via `_OutlineWidth`; the ground uses `InkNone` because an inverted hull on a giant plane just outlines the world.
- **SHADER VARIANT EXPLOSION: the real rule, learned the hard way twice on 2026-09-11.** `m_InstancingStripping: 2` (Keep All) is **REQUIRED** and must stay: our materials are created at runtime with `enableInstancing = true`, the stripper cannot see them, and with Strip Unused every `Graphics.DrawMeshInstanced` draw is **silently dropped** - no error, no magenta, the swarm and the walls simply do not exist while GameObject renderers still draw fine. I set it to Strip Unused to cure the explosion and reintroduced exactly that bug. **The explosion is not caused by Keep All.** It is caused by Keep All multiplied by a full PBR uber-shader: URP Lit reached 589,824 variants at roughly 16/sec, a build that never finishes. **The fix is to keep uber-shaders out of the build, not to change the stripping.** Concretely: no `Fallback` to URP Lit in our shaders (a fallback drags the whole variant set in), and keep runtime materials on `Exodus/InstancedLit`. With Keep All plus no fallback the worst shader is 8,192 variants and the player builds in about 25 seconds.
- **Verify rendering, never infer it.** Compiling is not rendering and tests passing is not rendering. Use the screenshot harness: `ProjectExodus.exe -exodus-screenshot <path.png> -exodus-screenshot-delay <sec> [-exodus-screenshot-overhead]`. An external screen grab CANNOT see the player's GPU surface and returns the desktop wallpaper, so the game has to capture itself.
- **Lighting is an overcast Virginia winter** (`FloodBootstrap.ApplyOvercastWinter`): low raking sun at 26 degrees, soft shadows, trilight ambient off a grey sky and brown leaf litter, exponential-squared haze. This carries more of the look than the models will; don't flatten it.
- **Never build Android locally** on the owner's 16 GB laptop; it was OOM-killed doing so. The `android` job in `unity.yml` is opt-in via workflow_dispatch.
- **ADR-007 picks the crowd-rendering path and the art lane.** Crowds are **Vertex Animation Textures + `DrawMeshInstanced`** with three distance LOD tiers, NOT DOTS - adopting Entities Graphics would mean converting `sim/` to ECS and losing determinism, and needs its own ADR if ever revisited. Art lane is the **stylised Synty POLYGON** set, about $135, chosen because every civilian pack shares one humanoid rig (which VAT batching requires) and polycount is the whole game at a thousand agents. **Owner has not yet approved the purchase; it blocks every date in `docs/design/ROADMAP-graphical-beta.md`.**
- **Owner checkpoint 2026-09-11 shipped** (`docs/design/owner-requests-2026-09-11.md` has his words verbatim). Nine of ten requests are code:
  - **Untimed opening.** `MatchState.AwaitingStart` holds the first wave until the player presses start, and nothing ages while it waits, so setting up does not eat the scan cycle. Between-wave setups stay timed.
  - **Gear.** `Progression/` has `Stats`, `Items`, `Loot`, `Inventory`. Eight slots, five rarities, one power budget so any two items compare by a single number. Auto-scrap on pickup keeps loot from becoming admin. **Vocabulary was re-skinned off the retired kingpin fiction; the maths is the design memo's.**
  - **Upgrades, two layers.** `Improvisations.cs` is the mission-scoped pick-one-of-three at a wave clear, discarded at extraction. `SkillTree.cs` is permanent levels and a tree with prerequisites, ranks, keystones and respec. `Loadout.cs` is the ONLY place the three stat sources are totalled into a `HeroConfig`; HeroModel stays ignorant of progression entirely, which is the seam co-op and difficulty will use.
  - **Truck extraction.** `TruckLoad.cs`: hard weight AND volume thresholds, so a heavy sentry and four light rotors are a real choice. It does not replace ADR-005's pack-up clock; time says how many you can reach, the truck says which are worth the bed space.
  - **Adrenaline focus.** `AdrenalineFocus.cs`: build mode slows the world, two charges, then depleted and recharging. **Tick it with UNSCALED time** or the slow extends itself.
  - **Turret line of sight (sim).** `AgentWorld.Vision.cs`. Turrets no longer acquire or shoot through walls, area fire respects cover, and a round stopped by a wall damages it. Uses the same ray march as bullets, so what a gun can see and what a bullet can reach are one rule.
  - **Mob intent split (sim).** `AgentWorld.Intents.cs`. `Intent.Vault` / `HuntStructure` / `WreckWall`. **The intent is chosen OUTSIDE the sim by the seeded spawn director; the core still carries no RNG.** A thousand agents solving one shortest path is a lane the player learns once; splitting intent is what makes the ungarrisoned wall the one that gets hit.
  - **Build wheel.** `BuildWheel.cs` is the selection maths (pure, tested); the drawing is in the bootstrap. Hold LB (or Q) to open: build mode enters, focus engages, release commits.
  - Still pending: nothing from his list except polish on the wheel's art.
- **`docs/design/ROADMAP-2026-09-12-mission-one-unguided.md` is the live plan** (proposed 2026-09-11 evening, awaiting the owner's approval; on approval it grants creative control until the MISSION ONE, UNGUIDED milestone). It freezes the systems layer, adopts quality-over-quantity, and retires `ROADMAP-graphical-beta.md`. The previous plan, `PLAN-2026-09-11.md`, is done.
- ~~**`docs/design/PLAN-2026-09-11.md` is the live plan.**~~ Done; see above. Order of work when nothing else is specified: scenario JSON loader, The Gate as real geometry, the interlude phase, then the owed items (patrol drone waypoint UI, cover props, post-processing, the stale flagship pitch), then the crowd bake tool built against a placeholder character. **The art purchase is the only external blocker and it gates everything visual.**
- **`docs/design/ROADMAP-graphical-beta.md` has dated commitments** ending in a playable graphical beta of Mission 1 on 2026-10-06. Every date slips one-for-one with the art purchase.
- **HUD is DPI-scaled**: `OnGUI` draws in a virtual space via `BeginScaledUi`; inside `OnGUI` use `_uiW`/`_uiH`, never `Screen.width`/`Screen.height`, or it lands outside the scaled matrix.
- **`docs/design/ROADMAP-2026-10.md` is the live plan** (supersedes ROADMAP-2026-09, whose September rows all shipped on 2026-09-10). Phase 1 = scenarios/objectives from JSON, stat resolver, gear + safe zone, escalating difficulty, cover props + roving drone. Phase 2 = URP + real characters (blocked on owner accounts). Phase 3 = campaign. Phase 4 = split-screen co-op. **Open: the owner picks systems-first or art-first.**
- **Concept art is generated, not drawn by Claude.** Claude has no image model; anything it draws in code looks like flat-shaded 1995 geometry and the owner rightly rejected two rounds of it. `tools/concept/generate.py` + `shots.json` call Replicate and write PNGs to `art/concept/` (gitignored). Token lives in `.secrets/replicate.txt` (gitignored). **Model licence matters:** default is FLUX schnell (Apache-2.0, shippable); `--model dev` looks better but is non-commercial and must never reach a build or store page.
- **ADR-004 (Proposed): the setting is a gated lake community in rural Virginia**, not a city. The owner supplied it (Lake of the Woods, Orange County VA) with reference photographs on 2026-09-10. It is a tower-defense map that exists in the real world: a handful of road gates, one lake flank, treeline behind every lot, a golf course as open ground, a clubhouse full of civilians, a boat ramp as the only exit. **The shipped map is fictionalised ("Wilderness Lake") because real people live in the real place** — keep the owner's street address out of this repo, out of concept prompts and out of any external service. His own house is in the game as the player's home lot; that one he gave us. Concept target: `art/concept/lotw-*.png` and `night-*.png`.
- **ADR-005 (Accepted) is the core loop and it is BUILT**: HALCYON can only sweep for the unchipped on a cycle, so the player calls their own last wave, then gets a pack-up window in which each emplacement is unbolted individually and whatever is left is abandoned. Time is one budget spent on fighting, packing and prep at the next position. `MatchPhase.Extracted` is neither a win nor a loss. Shipped in `game/Assets/Scripts/Match/ScanCycle.cs` + `MatchRules.cs`, bound to **L** / **D-pad down**, 15 tests in `ScanCycleTests.cs`, 61 game tests green headless 2026-09-11. **This replaced a flat kit-recovery percentage table — do not reintroduce one.**
- **Progression is WIRED, not just built.** Kills pay cash through the loadout (so Scrounger and supply cards multiply it) plus experience; Sapper and Spitter deaths always drop and are worth far more experience, inferred from the living count falling because the sim raises no per-archetype death event. A cleared wave deals the pick-one-of-three. `ApplyLoadoutToWorld()` is the ONE place the loadout lands: the hero gets a config via `HeroModel.Retune` (health carried as a FRACTION), the turrets get damage and range multipliers. **Turret scaling happens at FIRE time, not at placement**, so an upgrade improves guns already on the board.
- **In-game screens:** upgrade offer at wave clear (owns input while up), kit panel on **I**, skill tree on **K**, truck loading during Extraction (owns input). The build wheel is **hold LB / hold Shift**. Q is the airstrike, so do not bind anything else to it.
- **Screenshot harness flags:** `-exodus-screenshot <path>`, `-exodus-screenshot-delay <sec>`, and `-exodus-screenshot-overhead` / `-wheel` / `-progression` / `-skills` to stage a screen. **Do not set `_wheelHeld` when opening the wheel from a capture** - the input driver reads it as the button having been down and closes the wheel on the next frame.
- **Bash heredocs break on long content in this environment.** Patching a big block into a file: write the block and a small Python patch script into the scratchpad with the Write tool, then run the script. Trying to inline it in a heredoc fails with `unexpected EOF while looking for matching quote`.
- **THE ART PIPELINE IS LIVE AND THE GAME HAS REAL PEOPLE IN IT (2026-09-11).** Free CC0 Quaternius
  models, fetched by `tools/art/fetch-free-packs.sh`, zero money spent. `art/free/` is gitignored; the
  models actually used are copied into `game/Assets/Resources/`.
  - **Characters:** `Resources/Characters/*.fbx` -> `CrowdPrefabBuilder` -> `Resources/Civilians/*.prefab`.
    `CivilianCrowd` promotes the ~110 agents nearest the camera to real skinned bodies each frame and
    leaves the rest as instanced capsules; the capsule pass skips anyone promoted. `BuildHeroBody`
    gives the player a body from the same path, excluded from the crowd pool.
  - **Environment:** `Resources/Environment/*.fbx` scattered by `EnvironmentDresser`, deterministic
    from a seed, ~262 props. Trees need a **-90 pitch correction**; cars and bushes do not.
  - **Look:** brown winter ground, `ApplyOvercastWinter` lighting, `ApplyColourGrade` (tonemap, bloom,
    vignette) built in code so a clean checkout rebuilds it.
- **ANIMATION: FIVE APPROACHES FAILED SILENTLY BEFORE ONE WORKED.** Every failure left the character in
  a bind pose, which is indistinguishable from a working import nobody told to move. In order:
  (1) AnimatorController + imported clip: generic rigs retarget through an avatar and the models
  imported with `avatarSetup: NoAvatar`; (2) same, after copying the shared library's avatar, which
  then reported valid and still bound nothing; (3) `AnimationClip.SampleAnimation` per frame, which
  works in the editor and does NOT apply to a non-legacy clip in a player; (4) a legacy clip made by
  instantiating the source and setting `legacy = true` afterwards, which keeps its data and animates
  nothing because legacy and non-legacy clips bind curves through different systems; (5) all of the
  above against the wrong files. **The answer:** the pack ships each character TWICE, a ~420 KB
  version with no animation and an ~8 MB version carrying its own clips. Use the 8 MB ones, and build
  the legacy clip by copying curves explicitly (`LegacyClipMaker`, 630 curves).
  - An **Animator on the same GameObject suppresses the legacy Animation component even when
    disabled**, so it must be destroyed, not turned off.
  - The clip **animates scale on the armature**, so a character measures one height standing still and
    another once it moves. `FitToHeight` measures a few frames after the walk starts and corrects a
    PARENT the animation cannot touch. Never size a character at import time or on its spawn frame.
  - Characters live in slot -> pivot -> model. **The animation overwrites the model's own transform
    every frame**, so all placement goes on the slot.
- **A RELATIVE `-buildPath` RESOLVES AGAINST THE PROJECT DIRECTORY, NOT YOUR SHELL.** Running
  `Unity.exe -projectPath game ... -buildPath game/Builds/StandaloneWindows64` from the repo root
  builds into `game/game/Builds/StandaloneWindows64`, reports success, and leaves whatever stale exe
  was already at the path you meant. The tell is a screenshot that does not change no matter what you
  fix. **Always pass an absolute `-buildPath`**; `CiBuild` now resolves and prints the absolute
  destination in `[Cipher] Building ... -> <path>`, so check that line against where you run the exe.
- **MEASURE AN IMPORTED PREFAB BY INSTANTIATING IT, NOT BY MATRIX ARITHMETIC.** An FBX root carries
  an import scale that a naive `worldToLocalMatrix * localToWorldMatrix` round trip on the ASSET does
  not cancel: a six-metre road tile measured as two centimetres. Instantiate a throwaway copy at the
  origin, read `MeshFilter.sharedMesh.bounds` through `localToWorldMatrix`, destroy it. (Renderer
  bounds are still useless on a fresh instance — that is a separate trap, see FitToHeight.)
- **KIT PIECES ARRIVE IN ARBITRARY ORIENTATIONS AND YOU CANNOT ASSUME XZ IS THE FLOOR.** The dead
  trees import pitched -90; `Street_Straight` is authored **Z-up**, so a road laid flat in XZ stood on
  its edge like a fence panel. `EnvironmentDresser.LayRoad` now rotates the THINNEST measured axis
  onto Y and the longest remaining one along the corridor, which is orientation-agnostic. Do that
  rather than hard-coding a correction per model.
- **OUTLINES NEED SMOOTHED NORMALS.** An inverted hull extruded along shading normals tears open at
  every hard edge and UV seam, which reads as blur and smear. `SmoothNormals` averages normals by
  position into the tangent channel and the shader extrudes along that under `_SMOOTH_OUTLINE`. The
  smoothed mesh copies **must be saved as assets**: a mesh created in memory and assigned to a prefab
  does not survive the save, and the prefab then renders nothing.
- **WHEN SOMETHING FAILS SILENTLY, STOP CHANGING THINGS AND LOG THE STATE.** One diagnostic run named
  the null avatar immediately after several builds of guessing had not. This cost most of a session.
- **The game assembly references URP runtime** (`Unity.RenderPipelines.Universal.Runtime` and
  `.Core.Runtime` in `Cipher.Game.asmdef`), needed for the post-processing volume.
- **A RENDERING FEATURE CAN BE SWITCHED ON IN THREE PLACES AND STILL BE OFF (2026-09-11).** Soft
  shadows needed ALL of: `light.shadows = Soft` (was set), `m_SoftShadowsSupported` on the pipeline
  asset (was 0), and `#pragma multi_compile_fragment _ _SHADOWS_SOFT ...` in our own shader (was
  missing). Two of the three were configured and the result was a hard one-tap shadow under an
  overcast sky. Same shape of bug as Trilight ambient: `RenderSettings.ambientMode` had three
  authored colours and `InstancedLit` never called `SampleSH`. **When a render setting appears to do
  nothing, the shader is the third place to look, and usually the one nobody checked.**
  `supportsSoftShadows` is read-only in URP 17 -- `UrpSetup` flips the serialized field instead.
- **VERTEX COLOURS ARE OPT-IN PER MATERIAL, NOT PER MESH.** A mesh with no COLOR stream does not
  reliably hand a shader white, so `_VertexAoStrength` defaults to 0 and `VertexAo.Bake` clones the
  material to switch it on for the renderers it actually wrote. The prop palette is SHARED with
  things that are never baked (actors, the cruiser light bar); flipping the shared material would
  darken them by whatever garbage their colour channel holds. `VertexAo.Bake` also runs BEFORE the
  strike drone's lamp is built, because an AO clone of the lamp material would leave the strobe in
  `Fly()` writing to a material nothing renders.
- **Ground marks are `Scripts/Bootstrap/GroundStamp.cs` + `BlobShadows` + `Decals`**, drawn by
  `GroundMarkRenderer`, which **installs itself** (`RuntimeInitializeOnLoadMethod`) because props
  register their contact shadows from three files and a missing draw call would be silent. One
  shader, `Exodus/BlobShadow`, unlit alpha-blended instanced, single-digit variants, in Always
  Included. The crowd is the one thing it cannot do alone: `GroundMarkRenderer.Active?.DrawAgents(
  positions, 0.42f)` from `DrawWorld`, and `ResetForNewPosition()` wherever the props are destroyed.
- **Before/after render captures come from ONE exe.** `-exodus-render-baseline` reverts the lot;
  `-exodus-baseline-shadows` / `-exodus-baseline-ambient` / `-exodus-baseline-ao` /
  `-exodus-no-groundmarks` revert one thing each, and `-exodus-groundmarks-demo` stages decals.
  Two builds of two commits differ in the frame, the spawn and the drone overhead as well as in the
  thing being judged; one exe with one switch differs only in that. **Judge one change per pair** --
  the combined image cannot say which of four changes did what.
- **MISSIONS ARE DATA NOW (2026-09-11).** `Assets/Resources/Scenarios/*.json` -> `ScenarioReader` ->
  `ScenarioDef` -> `FloodBootstrap.NewMatch`. The game opens on `act1-01-the-gate`, named by
  `StartingScenarioId`. A scenario owns the map size, the walls, the gates, the hero spawn, the
  vault, the economy, the director config AND ITS SEED, the wave table and the objectives.
  - **`GridW`/`GridH`/`GoalX`/`GoalY`/`SpawnCells`/`HeroSpawn` are no longer constants.** They are
    assigned by `ApplyScenarioShape` at the top of `NewMatch`. Nothing may read them before that.
  - **The reader is deliberately strict and there is no fallback.** Unknown fields, out-of-map
    cells, a wave that cannot arrive and a mix that does not sum to 1 are all hard errors naming the
    dotted JSON path. A missing scenario throws rather than dropping back to a built-in arena,
    because a silent fallback turns a typo into "the level is wrong somehow" during a playtest.
  - **Objectives decide the verdict, not the wave table** (`Scripts/Scenario/Objectives.cs`).
    `MatchState.WinByObjective`/`LoseByObjective` are the seam; clearing the last wave is only the
    backstop. `ProtectVault` is a FAIL-CONDITION and is excluded from the "all complete" test —
    counting it as a goal makes every mission with a vault unwinnable.
  - Objectives needing the Structure/Process/Crew actor system (`ProtectActors`, `HoldUntil`,
    `KeepCrewAlive`) are **refused by name at load**. Do not make them load-and-do-nothing.
  - `ShippedScenarioTests` loads every file in `Resources/Scenarios` and asserts it parses, its id
    matches its filename, its `next` names a file that exists, and it has an objective that can
    complete. Add missions and CI guards them.
  - **How to author one: `docs/design/authoring-a-mission.md`.** Hand it to the owner.
- **THE RETREAT IS A LOOP NOW (2026-09-11).** Completing the objectives does NOT win the mission
  where there is a line behind you: it opens the pack-up window (`MatchState.CompleteByObjective`).
  Leaving loads the next position, and whatever is left of the scan cycle becomes materials there
  (`ScanCycleConfig.PrepDollarsPerSecond`) along with the truck's salvage. That closes ADR-005's
  third place to spend the budget, which until now was computed and consumed by nothing.
  - **`lastStand` and an empty `next` are DIFFERENT THINGS and must stay different.** "Nothing
    behind this line" is design and is true of exactly one Act One mission; "the next mission is not
    written" is content. Deriving the first from the second turned the last authored mission into a
    last stand and silently changed how it played.
  - **Anything sized or placed from the map must be re-pointed in `ResizeForMap`.** BuildSceneObjects
    runs once per PROCESS, not per mission: the minimap texture and its two buffers, the ground
    plane and the vault object all live across a position change. Add to that list when you add
    something map-shaped, and verify with `-exodus-screenshot-fallback`, which jumps straight to the
    next position under a camera.
- **A RAY MARCH THAT SAMPLES ITS OWN ORIGIN CELL BLINDS THE SHOOTER.** `Movement.FirstBlockedCell`
  used to start at travelled = 0. A turret stands on a cell marked `WallKind.Structure`, which is
  blocked, so every turret in the game found a wall at distance zero, acquired nothing and **never
  fired** -- and it shipped, because every line-of-sight test asked "can it see THROUGH a wall" and
  none asked "can it see at all". Found by the owner, not by us.
  - **The lesson is the test, not the fix.** When you add a restriction, test the UNRESTRICTED case
    too. `TurretSightTests` now asserts a turret with a clear shot fires, as well as that a wall
    stops it.
- **CROWD BODIES MUST KEEP THEIR AGENT.** Assigning slot i to the i-th nearest agent each frame is
  correct about WHICH agents get a body and wrong about which body: two agents swapping distance
  order swap models, and the owner saw characters "scan switching from skin to skin to skin". A slot
  holds its agent until that agent dies or leaves range.
- **THE CONTROLLER IS THE DESIGN-CENTRE INPUT ON BOTH PLATFORMS** (ADR-002) and it is easy to forget
  while testing on a keyboard. The skill tree shipped with no gamepad binding at all and every
  on-screen prompt named keyboard keys. Any new panel needs a pad binding, a pad way OUT, and
  prompts that name the device in the player's hands.
- **ENEMY COUNT CAME DOWN AND HEALTH WENT UP (owner, 2026-09-11):** "less zombies... more robust.
  quality over quantity." Health is per-scenario (`enemy.health`); The Gate is 40-150 bodies at 34 hp
  rather than 150-700 at 10. The thousand-agent number in the pitch was the old flood fantasy;
  ADR-003's enemy is people, and people need to be individually legible.
- **ACTORS ARE WHAT FIVE OF TWELVE MISSIONS ARE FOUGHT OVER** (`Scripts/Scenario/Actors.cs`).
  Process (a clock), Structure (a transformer), Crew (a person). `HoldUntil`, `ProtectActors` and
  `KeepCrewAlive` are implemented against them.
  - **Damage is PRESSURE, not attacks.** `IActorThreat.EnemiesWithin` is the only thing the rules
    take from the sim. Do not give actors grid cells to make them attackable: that makes them solid,
    which changes pathing, which breaks the build preview's covenant.
  - **`IsFailCondition` lives on `IObjective`.** ProtectVault/ProtectActors/KeepCrewAlive are
    conditions you hold, never tasks you finish; counting one as a goal makes its mission unwinnable.
  - Attended work PAUSES when the hero leaves. It never resets.
- **BUILT SCENERY IS BOXES AND THAT IS THE DECISION** (`Scripts/Bootstrap/SiteProps.cs`). The free
  kits have no buildings. Under the ink shader a box with an overhanging roof and a dark window band
  reads as a guardhouse; what would look cheap is a photoreal model beside it. Props are authored per
  position in the scenario's `props`, carry **no colliders and no grid cells**, and an unknown kind
  is a load error rather than a prop that never appears.
- **JSON is hand-rolled** (`Scripts/Scenario/Json.cs`). `JsonUtility` cannot express this schema
  (no dictionaries, no polymorphic lists, no optional sections) and Newtonsoft is a package
  dependency for ~400 lines. Numbers parse `InvariantCulture` on purpose: a comma-decimal locale
  must not read 8.5 as 85.
- **ADR-006: the game is PROJECT EXODUS.** Code namespaces, repo name and branch deliberately stay `Cipher.*` — renaming them is a large cosmetic diff with real risk. Only the build product name changed.
- **Unity Hub must be RUNNING for any headless command.** Killing it kills the licensing daemon and every `-batchmode` run dies with exit 198 and `Found 0 entitlement groups`. Restart it from the Start menu app id `UnityTechnologies.UnityHub_2vrhnee42bhxm!UnityHub` and wait about 25 seconds.
- **Headless tests when the owner has the editor open:** Unity refuses a second instance on the same project. Copy `game/` + `sim/src` into a temp dir preserving relative layout (the manifest points at `file:../../sim/src/Cipher.Sim`) and run `-runTests` there. Takes about a minute.
- **`docs/design/campaign-act1.md` is a fighting retreat** (owner's spine, 2026-09-10): mission 1 is securing the main gate, and the map contracts every mission until mission 12 is the owner's own house with nothing behind it. His three requested missions are the pump house (3), the fire station armoury (7) and the county laboratory (9, the act break, the only mission outside the gates). Missions 4 and 12 cannot be won by design; the other ten can. **The community is authored ONCE and each mission is a smaller sub-rectangle of it** — twelve missions cost closer to one level than twelve, so do not build twelve maps. Damage persists between missions. ~~One new system: a mission-end kit-recovery multiplier (75/50/25% by how the hold ended).~~
  **Superseded by ADR-005**: recovery is the pack-up window and the truck's weight/volume, not a
  percentage table. Read the campaign doc before writing any mission content.
- **Design specs waiting to be built:** `docs/design/arsenal-and-terrain.md` (three tower families incl. the Grinder and a roving drone, destructible terrain, cover props, five level themes, build order) and `docs/design/progression-and-campaign.md` (gear, builds, safe zone, JSON scenarios, couch co-op). `docs/ACCOUNTS.md` lists what the owner needs to sign up for.
- **Owner checkpoint requested** (his words): no more check-ins until towers + upgrades + wall/tower attackers + champion upgrades exist → all four shipped in Maze v1. Next: owner playtest of Maze v1 → gun pass (Oct 1) → URP + first characters (Oct 8).
- **Was:** owner hero feedback → barricade build-mode with live path preview (Milestone 2 "The Maze").
