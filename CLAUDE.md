# CIPHER — Studio Working Memory

Read this first, every session. Keep it current: surprising facts, new conventions, and gotchas that cost >30 min go here the same day (see docs/05, "the ratchet").

## What this project is

**CIPHER: DEAD TURF** — third-person action / tower-defense hybrid (hardcore mazing + Bloons-depth upgrade trees + hero combat), syndicate-kingpin-vs-mutated-swarms fantasy. Full pitch: `docs/02-FLAGSHIP-PITCH.md`.

- **Platforms:** Android (primary perf floor, 300–500 agents on screen) + PC/Steam (high-end, 1,000+ agents).
- **Input:** Xbox controller is the design-center control scheme on both platforms. Touch is a fallback overlay.
- **Engine:** Unity 6 LTS + URP, ECS/DOTS for the swarm. This REVERSED an earlier UE5 decision — rationale in `docs/decisions/ADR-001-engine-unity6.md`. Do not re-litigate without its reversal conditions triggering.

## Operating model

Owner (Jeff) = vision, taste, veto; plays builds and reacts. Claude = the engineering department with broad creative control inside the pitch's parameters. Big/irreversible calls → ADR in `docs/decisions/`, surfaced to owner, never buried. Status updates at every push.

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
- Bootstrap composition root is `game/Assets/Scripts/Bootstrap/FloodBootstrap.cs`; game rules go in plain C# classes next to it (`Hero/`, `UI/`) so they are testable in EditMode without a scene. Sim world Y maps to Unity Z.
- Adversarial review process (3 parallel agents: sim QA, scaffold audit, design red-team) found 1 compile blocker + 3 sim bugs + 12 design holes on first run — rerun this pattern after every major layer lands.

## Open decisions

`docs/decisions/OPEN-DECISIONS.md` is the live register (camera model, Android's purpose, v1 cut-list, DOTS reconciliation, preview covenant wording, gamepad tree UX, playtesters). Don't build against undecided items; surface them to the owner instead.

## Current state / next steps

- Founding docs 01–05 + ADR-001 committed. Sim core v0 committed and CI-green (18 tests).
- Unity scaffold committed in `game/`: manifest mounts `sim/src/Cipher.Sim` as local package `com.cipher.sim` (it carries `package.json` + `Cipher.Sim.asmdef` with `noEngineReferences: true`); `FloodBootstrap.cs` builds the whole Milestone-1 graybox procedurally (no scene assets); gamepad-first input via Input System. First-open checklist for the owner: `game/README.md`.
- `sim/Directory.Build.props` redirects bin/obj to `sim/.artifacts/` so Unity's importer never sees build artifacts. Don't remove it.
- **`game/` compiled clean on first open (2026-09-10, Unity 6000.0.83f1)** — zero code fixes. Owner's laptop: Hub 3.21 (MSIX, lives under `WindowsApps`, not Program Files), editor at `C:\Program Files\Unity\Hub\Editor\6000.0.83f1\Editor\Unity.exe`, Android SDK/NDK/JDK installed. No .NET SDK locally — `dotnet test` still runs only in CI. Headless import/compile recipe is in `game/README.md`; use it to verify `game/` edits before handing the owner a build.
- ProjectSettings, `packages-lock.json`, all `.meta`s and `Assets/Scenes/Flood.unity` are committed. Active Input Handling = Input System Package. Package Manager resolved Input System to 1.19.0 / test-framework 1.6.0.
- **First owner playtest recorded:** `docs/feedback/2026-09-10-m1-flood-first-play.md` (**~1,000 fps at 1,000 agents** in Editor Play mode on the owner laptop; airstrike "feel" not yet described).
- **Unity CI is live (2026-09-10):** `.github/workflows/unity.yml` installs the editor fresh on `windows-latest` (~10 min; buildalon's editor cache restore crashed Unity with 0x8007007E — leave `cache-installation: false`), activates Personal via `UNITY_USERNAME`/`UNITY_PASSWORD`, builds `CipherDeadTurf.exe` (87 MB, ~5 min) and uploads it as an artifact. First green run: actions/runs/34487824831. Whole job ≈17 min.
- Game-side EditMode tests exist (`game/Assets/Tests/EditMode`, asmdef `Cipher.Game.Tests.EditMode`) and run in CI before the build. Put testable game logic in plain C# classes (see `PauseMenuModel`) so hard rule 2 holds outside `sim/` too.
- **Hero graybox shipped (2026-09-10):** `HeroModel` (pure C#, 7 tests) + sim queries (`Raycast`, `ApplyDamage`, `CountWithin`, shared `Grid.Movement`). Chase cam default, hold LB = tactical overhead — both candidates from Open Decision #1 in one build; **ADR-002 (Proposed)** says which playtest outcome commits us to what. Owner playtest of the hero pending.
- **Next engineering:** owner hero feedback → camera decision (ADR-002 accepted) → barricade build-mode with live path preview (Milestone 2 "The Maze").
