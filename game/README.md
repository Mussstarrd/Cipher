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
- `Assets/Scripts/Bootstrap/FloodBootstrap.cs` — Milestone 1 entry point (procedural scene, fixed-tick sim loop, instanced rendering, input)
- `Assets/Editor/FirstOpenSetup.cs` — headless scene creation / Build Settings registration
- `Assets/Editor/CiBuild.cs` — player build entry points used by CI (`-executeMethod … -buildPath <dir>`)
- `Assets/Scenes/Flood.unity` — empty scene; the bootstrap populates it at Play
