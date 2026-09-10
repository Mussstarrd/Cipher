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

- **Xbox controller:** left stick moves the cursor, **A** drops an airstrike. Mouse + left click works without one.
- **HUD** (top-left): `fps N | alive N | breached N | kills N` — desktop tops up toward 1,000 agents.
- Commit any new `.meta` files Unity generates (including inside `../sim/src/Cipher.Sim/`).

### Headless first-open / CI recipe

Everything above can be reproduced without touching the GUI (this is how the first open was done on 2026-09-10 — zero code fixes were needed):

```
Unity.exe -batchmode -nographics -projectPath game -quit -logFile import.log
Unity.exe -batchmode -nographics -projectPath game -executeMethod Cipher.Game.Editor.FirstOpenSetup.CreateFloodScene -quit -logFile setup.log
```

`FirstOpenSetup` (in `Assets/Editor/`, also under the **Cipher** menu) creates `Assets/Scenes/Flood.unity` if missing and registers it in Build Settings. Idempotent.

## CI license (one-time owner setup)

`.github/workflows/unity.yml` builds a Windows executable in game-ci Docker images and uploads it as an artifact. It skips itself until three repo secrets exist:

| Secret | Value |
| --- | --- |
| `UNITY_LICENSE` | full contents of `Unity_v6000.x.ulf` (from https://license.unity3d.com/manual, fed the `.alf` produced by `Unity.exe -batchmode -nographics -createManualActivationFile`) |
| `UNITY_EMAIL` | Unity ID email |
| `UNITY_PASSWORD` | Unity ID password |

Set them with `gh secret set NAME < file` / `gh secret set NAME` (prompts). The `.ulf` is tied to the machine that made the `.alf`; regenerate both if it stops activating. Personal licenses need re-issuing roughly yearly.

## Known first-APK caveats (handled when we get there)

- The `Standard` shader is referenced by code (`Shader.Find`); device builds need it in *Project Settings → Graphics → Always Included Shaders* or it strips to magenta. Editor Play mode is unaffected.
- Android build settings (IL2CPP/ARM64, landscape, target SDK) get committed as ProjectSettings once the editor has generated them.

## Layout

- `Packages/manifest.json` — dependencies; the sim core mounts as a local package (`com.cipher.sim`)
- `Assets/Scripts/Cipher.Game.asmdef` — game assembly; references `Cipher.Sim` + Input System
- `Assets/Scripts/Bootstrap/FloodBootstrap.cs` — Milestone 1 entry point (procedural scene, fixed-tick sim loop, instanced rendering, input)
- `Assets/Editor/FirstOpenSetup.cs` — headless scene creation / Build Settings registration
- `Assets/Scenes/Flood.unity` — empty scene; the bootstrap populates it at Play
