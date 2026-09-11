# CIPHER — Studio Working Memory

Read this first, every session. Keep it current: surprising facts, new conventions, and gotchas that cost >30 min go here the same day (see docs/05, "the ratchet").

## What this project is

**CIPHER: DEAD TURF** — third-person action / tower-defense hybrid (hardcore mazing + Bloons-depth upgrade trees + hero combat), syndicate-kingpin-vs-mutated-swarms fantasy. Full pitch: `docs/02-FLAGSHIP-PITCH.md`.

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

(No .NET SDK in some remote containers — CI on GitHub Actions is the source of truth; verify runs after pushing.)

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
- **`game/` compiled clean on first open (2026-09-10, Unity 6000.0.83f1)** — zero code fixes. Owner's laptop: Hub 3.21 (MSIX, lives under `WindowsApps`, not Program Files), editor at `C:\Program Files\Unity\Hub\Editor\6000.0.83f1\Editor\Unity.exe`, Android SDK/NDK/JDK installed. No .NET SDK locally — `dotnet test` still runs only in CI. Headless import/compile recipe is in `game/README.md`; use it to verify `game/` edits before handing the owner a build.
- ProjectSettings, `packages-lock.json`, all `.meta`s and `Assets/Scenes/Flood.unity` are committed. Active Input Handling = Input System Package. Package Manager resolved Input System to 1.19.0 / test-framework 1.6.0.
- **First owner playtest recorded:** `docs/feedback/2026-09-10-m1-flood-first-play.md` (**~1,000 fps at 1,000 agents** in Editor Play mode on the owner laptop; airstrike "feel" not yet described).
- **Unity CI is live (2026-09-10):** `.github/workflows/unity.yml` installs the editor fresh on `windows-latest` (~10 min; buildalon's editor cache restore crashed Unity with 0x8007007E — leave `cache-installation: false`), activates Personal via `UNITY_USERNAME`/`UNITY_PASSWORD`, builds `CipherDeadTurf.exe` (87 MB, ~5 min) and uploads it as an artifact. First green run: actions/runs/34487824831. Whole job ≈17 min.
- Game-side EditMode tests exist (`game/Assets/Tests/EditMode`, asmdef `Cipher.Game.Tests.EditMode`) and run in CI before the build. Put testable game logic in plain C# classes (see `PauseMenuModel`) so hard rule 2 holds outside `sim/` too.
- **Hero graybox shipped (2026-09-10):** `HeroModel` (pure C#, 7 tests) + sim queries (`Raycast`, `ApplyDamage`, `CountWithin`, shared `Grid.Movement`). **ADR-002 accepted (owner delegated): combat is chase third-person; overhead is look-only and becomes the build-mode camera.** Weapons hold while overhead, enforced in code. Owner playtest of the hero pending.
- **Brain trust ran 2026-09-10** (4 parallel memos in `docs/design/`), synthesized with dates in **`docs/design/ROADMAP-2026-09.md`** — read it before planning anything. Decisions in force: line airstrike aimed by looking; LB = build-mode toggle; cash economy ($5/kill, $100×wave, $400 start); Sapper breacher with timed hole stages + repair drone; Spitter tower-hunter; spawn pity timers (owner's 0.05% is the late-game floor); URP migration right before the character pass (~Oct 8).
- **Maze v0 shipped 2026-09-10 (six days early):** sim has mutable walls (kinds/HP/breach stages), integer token-bucket gates, `FlowField.EnsureFresh` (one recompute per tick), `BuildValidator` (scratch-map what-if = the covenant), `TurretSystem`; game has `MatchState` (5 waves, cash, vault, win/lose) and `BuildModel` (cursor, place/sell, route preview). 48 sim tests, 29 game tests. Line airstrike aimed by looking shipped the same day.
- **Maze v1 shipped 2026-09-10 (two weeks early):** Sapper + Spitter archetypes in the sim (`AgentWorld.Archetypes.cs`, no RNG in core), timed breach stages + repair, turret upgrade tiers, `SpawnDirector` (seeded, pity timers, forced Sapper when sealed), repair drones, gun crates. 61 sim tests, 40 game tests incl. headless full-match integration tests (`MatchIntegrationTests`) — extend those before trusting any new match-level behaviour.
- **Audio is procedural** (`game/Assets/Scripts/Audio/`): synthesized at startup, no asset files, recipes unit-tested. Add new sounds as recipes; keep per-sound peak <= 0.95 and use `minInterval` for anything that can fire faster than 10/s.
- **ADR-003 (Proposed): the premise gained an author.** **Nobody got infected — they opted in.** A neural implant tied to stimulus payments, adopted by the majority, repurposed by the AI that administered it. The enemy are ordinary chipped citizens ("the signed"): no rot, no blood, normal eyes, one small amber implant light at the temple. **Red eyes belong only to lab releases and we spend them almost never.** Hacked service humanoids and cargo drones are the other two classes. Half the game is daylight. **The protagonist is a veteran, not a gangster** (owner cast him 2026-09-10): unregistered non-voter, refused the implant and the money on principle, takes the VA cheque he is owed. This retires the pitch's kingpin, the gold-plated bling progression and the Scarface framing — see ADR-003's 'What this retires from the pitch' table before writing any fiction. `docs/02-FLAGSHIP-PITCH.md` is now substantially wrong and needs a rewrite pass. Read `docs/decisions/ADR-003-premise-the-cascade.md` before writing any fiction, level theme or enemy. It recommends against the owner's "schizophrenia" phrasing and says why.
- **Destructible terrain shipped 2026-09-10:** `Movement.FirstBlockedCell` (walls stop bullets — they did not before), `AgentWorld.DamageWall` / `DamageWallsInRadius` feeding the existing breach pipeline, hero gun and airstrike wired in, player walls take `FriendlyWallDamageScale`. Next from the spec: cover props, roving drone.
- **Tower families are data (2026-09-10):** `TurretCatalog` in the sim holds `TurretFamily` entries (Sentry = single target, Grinder = area) each with their own tier ladder; `TurretSystem` takes a family array, `BuildModel.Options` builds the build bar from it. Adding a tower is a catalogue entry, not code. Turret prices moved off `EconomyConfig` onto the family.
- **`docs/ACCOUNTS.md`** lists every service the owner should sign up for, ordered, with a shippable-licence verdict per tool. Phase 2 art work is blocked until those exist.
- **Android builds are slow for a findable reason:** `m_InstancingStripping: 2` (Keep All, needed because our instanced materials are created at runtime and the stripper cannot see them) times the Built-in `Standard` shader times two graphics APIs = **~98,000 shader variants**, roughly 15 minutes of compiling before IL2CPP even starts, and a fat APK. The real fix is to stop using `Shader.Find("Standard")` for the instanced draws and ship a tiny custom instanced shader; that collapses the variant count and is naturally part of the URP migration (Phase 2). Interim lever applied: Android targets Vulkan alone. **Never build Android locally** on the owner's 16 GB laptop; it was OOM-killed doing so. The `android` job in `unity.yml` builds the APK on a Linux runner, opt-in via workflow_dispatch.
- **HUD is DPI-scaled**: `OnGUI` draws in a virtual space via `BeginScaledUi`; inside `OnGUI` use `_uiW`/`_uiH`, never `Screen.width`/`Screen.height`, or it lands outside the scaled matrix.
- **`docs/design/ROADMAP-2026-10.md` is the live plan** (supersedes ROADMAP-2026-09, whose September rows all shipped on 2026-09-10). Phase 1 = scenarios/objectives from JSON, stat resolver, gear + safe zone, escalating difficulty, cover props + roving drone. Phase 2 = URP + real characters (blocked on owner accounts). Phase 3 = campaign. Phase 4 = split-screen co-op. **Open: the owner picks systems-first or art-first.**
- **Concept art is generated, not drawn by Claude.** Claude has no image model; anything it draws in code looks like flat-shaded 1995 geometry and the owner rightly rejected two rounds of it. `tools/concept/generate.py` + `shots.json` call Replicate and write PNGs to `art/concept/` (gitignored). Token lives in `.secrets/replicate.txt` (gitignored). **Model licence matters:** default is FLUX schnell (Apache-2.0, shippable); `--model dev` looks better but is non-commercial and must never reach a build or store page.
- **ADR-004 (Proposed): the setting is a gated lake community in rural Virginia**, not a city. The owner supplied it (Lake of the Woods, Orange County VA) with reference photographs on 2026-09-10. It is a tower-defense map that exists in the real world: a handful of road gates, one lake flank, treeline behind every lot, a golf course as open ground, a clubhouse full of civilians, a boat ramp as the only exit. **The shipped map is fictionalised ("Wilderness Lake") because real people live in the real place** — keep the owner's street address out of this repo, out of concept prompts and out of any external service. His own house is in the game as the player's home lot; that one he gave us. Concept target: `art/concept/lotw-*.png` and `night-*.png`.
- **`docs/design/campaign-act1.md` is a fighting retreat** (owner's spine, 2026-09-10): mission 1 is securing the main gate, and the map contracts every mission until mission 12 is the owner's own house with nothing behind it. His three requested missions are the pump house (3), the fire station armoury (7) and the county laboratory (9, the act break, the only mission outside the gates). Missions 4 and 12 cannot be won by design; the other ten can. **The community is authored ONCE and each mission is a smaller sub-rectangle of it** — twelve missions cost closer to one level than twelve, so do not build twelve maps. Damage persists between missions. One new system: a mission-end kit-recovery multiplier (75/50/25% by how the hold ended). Read it before writing any mission content.
- **Design specs waiting to be built:** `docs/design/arsenal-and-terrain.md` (three tower families incl. the Grinder and a roving drone, destructible terrain, cover props, five level themes, build order) and `docs/design/progression-and-campaign.md` (gear, builds, safe zone, JSON scenarios, couch co-op). `docs/ACCOUNTS.md` lists what the owner needs to sign up for.
- **Owner checkpoint requested** (his words): no more check-ins until towers + upgrades + wall/tower attackers + champion upgrades exist → all four shipped in Maze v1. Next: owner playtest of Maze v1 → gun pass (Oct 1) → URP + first characters (Oct 8).
- **Was:** owner hero feedback → barricade build-mode with live path preview (Milestone 2 "The Maze").
