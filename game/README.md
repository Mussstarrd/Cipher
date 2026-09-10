# CIPHER — Unity project (Milestone 1: "The Flood")

Graybox density prototype: the deterministic sim from `../sim` rendered as an
instanced horde, with an Xbox-controller-driven airstrike cursor. The whole
scene is built procedurally at Play — no scene authoring needed.

## Opening the project

Project files (`ProjectSettings/`, `packages-lock.json`, `.meta`s, `Assets/Scenes/Flood.unity`)
are committed, so an open is just: Unity Hub → **Add → `game/`** → open with **Unity 6000.0.83f1**
(or the newest 6000.0.x LTS patch; accept the upgrade prompt). Active Input Handling is already
set to *Input System Package*. Press **Play** in `Flood.unity` — the bootstrap builds the whole
graybox at runtime.

- **Xbox controller (combat):** left stick move, right stick look, **RT** fire, **Y** airstrike on the orange line where you are looking (6-24 cells), **View** start the wave early, **Menu** pause, **A** get back up / run it back.
- **Build mode:** tap **LB** to enter (overhead camera, weapons holstered). Left stick moves the grid cursor, **A** places (hold and sweep to paint), **X** sells, **RB** or d-pad left/right switches Barricade $20 / Sentry .50 $150 / Repair Drone $150, **Y** upgrades the turret under the cursor (Twin .50 $120, then Overwatch $200), **B** or LB leaves. Hold a direction and the cursor accelerates through four speeds (7 to 46 cells/s) so crossing the arena is quick. Green dots are where each of the five spawns will path; red means that spawn cannot reach the vault (a full seal, allowed but you were warned).
- **Keyboard / mouse:** WASD, mouse look, LMB fire, RMB or Q airstrike, Tab build (arrows, Space place, X sell, Q next item), Enter start wave, Esc pause.
- **The loop:** five waves march from the left edge to the green vault on the right. Every runner that reaches the vault takes one point off it; at zero you lose. Kills from any source pay $5, clearing wave N pays $100 x N, you start with $400. Selling refunds 100% between waves, 50% x remaining health mid-wave. You are the gold capsule: runners chew you on contact, LMG kills in two taps, airstrike walks six bombs down a 14x4 line.
- **Sappers** (tall orange) walk to the wall that shortcuts most, plant for 4 s, and open a hole that widens every 20 s until the wall collapses (traffic through it speeds that up). A pulsing orange marker floats over the wall they picked. Kill them on the walk, or drop a **Repair Drone** on the hole and stand within 8 cells while it works (one stage per 4 s). If you seal the maze completely, a Sapper is forced.
- **Spitters** (squat green) go for turrets: they close to 9 cells and lob acid every 1.5 s. Turrets out-range them but do not prioritise them; you do.
- **Minimap** bottom-right: walls, barricades, orange breaches, blue turrets, the green vault, you in yellow, and heat where the horde is thickest (Sappers and Spitters get their own pixel colour). **RS click** (pad) or **N** hides it.
- **Repair drones snap**: put the cursor within 3 cells of a breach and the drone drops on the breach itself, so you never have to hit one exact cell.
- **Sound** is fully procedural (synthesized at startup from `Assets/Scripts/Audio/SoundRecipes.cs`: no audio files). Gunfire, hits and kills, airstrike call-in / whistle / bombs, turret fire (positional), build clicks, Sapper siren and breach countdown, breach and collapse impacts, Spitter blip, pickups, wave horn, wave-clear and win/lose stings, hurt and down, a wind bed and a horde rumble that swells with how many runners are near you. Pause menu: **Y** (pad) or **M** (keyboard) toggles sound.
- **Gun crates** (spinning yellow) appear every 45 s on your side of the arena and last 30 s. Walk over one: LMG Mk2, Mk3, then Gold-plated.
- **HUD** (top-left): fps / alive / breached / kills by source; cash, wave, phase and vault; HP and airstrike bars; build-mode item, cost and placement message.
- Commit any new `.meta` files Unity generates (including inside `../sim/src/Cipher.Sim/`).

### Headless first-open / CI recipe

Everything above can be reproduced without touching the GUI (this is how the first open was done on 2026-09-10 — zero code fixes were needed):

```
Unity.exe -batchmode -nographics -projectPath game -quit -logFile import.log
Unity.exe -batchmode -nographics -projectPath game -executeMethod Cipher.Game.Editor.FirstOpenSetup.CreateFloodScene -quit -logFile setup.log
```

`FirstOpenSetup` (in `Assets/Editor/`, also under the **Cipher** menu) creates `Assets/Scenes/Flood.unity` if missing and registers it in Build Settings. Idempotent.

## CI license (one-time owner setup)

`.github/workflows/unity.yml` installs the editor on a Windows runner, activates a **Unity Personal** license, runs `Cipher.Game.Editor.CiBuild.BuildWindows`, and uploads the result as an artifact. It skips itself until two repo secrets exist:

| Secret | Value |
| --- | --- |
| `UNITY_USERNAME` | Unity ID email |
| `UNITY_PASSWORD` | Unity ID password |

Set them with `gh secret set UNITY_USERNAME` / `gh secret set UNITY_PASSWORD` (each prompts for the value). **Unity no longer offers manual `.alf`/`.ulf` activation for Personal seats** (the manual page now says "not eligible to activate your license offline"), which is why the game-ci `UNITY_LICENSE` route is not used. If the Unity ID has two-factor auth enabled, activation in CI will fail — use an ID without it, or a dedicated CI Unity ID.

`CiBuild` also has a `BuildAndroid` entry point (menu **Cipher/Build/Android APK**); the workflow matrix grows an Android row once the first APK is validated on a device.

## Known first-APK caveats (handled when we get there)

- The `Standard` shader is referenced by code (`Shader.Find`), so it is pinned in *Project Settings → Graphics → Always Included Shaders* (fileID 46 in `GraphicsSettings.asset`). Remove it and every player build renders magenta while Editor Play mode looks fine.
- Instancing variants are pinned too (`m_InstancingStripping: 2`, Keep All). The agents and walls are drawn with `Graphics.DrawMeshInstanced` using materials created at runtime, which the build's variant stripper cannot see — with the default "Strip Unused" the first CI build showed only the ground and cursor.
- Android build settings (IL2CPP/ARM64, landscape, target SDK) get committed as ProjectSettings once the editor has generated them.

## Layout

- `Packages/manifest.json` — dependencies; the sim core mounts as a local package (`com.cipher.sim`)
- `Assets/Scripts/Cipher.Game.asmdef` — game assembly; references `Cipher.Sim` + Input System
- `Assets/Scripts/Bootstrap/FloodBootstrap.cs` — Milestone 1 entry point (procedural scene, fixed-tick sim loop, instanced rendering, input, pause menu)
- `Assets/Scripts/Match/MatchRules.cs` — cash, wave table, vault, win/lose (pure C#, tested)
- `Assets/Scripts/Match/Directors.cs` — SpawnDirector (seeded archetype rolls, pity timers, forced Sapper on seal), RepairDrone, gun tiers and crate pickups (pure C#, tested)
- `Assets/Scripts/Build/BuildModel.cs` — build cursor, place/sell/refund, route preview via the sim BuildValidator (pure C#, tested)
- `Assets/Scripts/Hero/HeroModel.cs` — the Enforcer as pure state (movement via the shared `Movement` wall rule, hitscan via `AgentWorld.Raycast`, contact damage, airstrike); unit-tested
- `Assets/Scripts/UI/PauseMenuModel.cs` — pause menu state, no UnityEngine, unit-tested
- `Assets/Scripts/Audio/` — `Waveforms` (pure DSP, tested), `SoundRecipes` (one recipe per sound, tested for level/length/NaN), `SoundBank` (Unity clips, pooled 2D/3D sources, rate limits, ambience)
- `Assets/Tests/EditMode/` — EditMode tests (`Cipher.Game.Tests.EditMode`); CI runs them before the build. Locally: `Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults out.xml`
- `Assets/Editor/FirstOpenSetup.cs` — headless scene creation / Build Settings registration
- `Assets/Editor/CiBuild.cs` — player build entry points used by CI (`-executeMethod … -buildPath <dir>`)
- `Assets/Scenes/Flood.unity` — empty scene; the bootstrap populates it at Play
