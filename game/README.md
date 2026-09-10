# CIPHER — Unity project (Milestone 1: "The Flood")

Graybox density prototype: the deterministic sim from `../sim` rendered as an
instanced horde, with an Xbox-controller-driven airstrike cursor. The whole
scene is built procedurally at Play — no scene authoring needed.

## First open (owner checklist, ~30 min mostly download time)

1. Install **Unity Hub**, then **Unity 6 LTS (6000.0.x)** with the **Android Build Support** module (IL2CPP + SDK/NDK boxes checked). If Hub offers a slightly different 6000.0 patch than `ProjectSettings/ProjectVersion.txt`, accept the upgrade prompt.
2. In Hub: **Add → `game/` folder** of this repo, open it. First import takes a few minutes (packages restore automatically, including the sim core from `../sim/src/Cipher.Sim`).
3. If prompted to **enable the new Input System backend and restart the editor — click Yes.** (If not prompted: Edit → Project Settings → Player → Active Input Handling → *Input System Package*.)
4. `File → New Scene` (pick the **Empty** template if offered; the Basic template's default camera gets auto-disabled by the bootstrap either way), then press **Play**. You should see a dark arena, a red flood snaking an S-route, and an orange cursor.
5. Plug in / pair the Xbox controller: **left stick** moves the cursor, **A** drops an airstrike. No controller? Mouse + left click works.
6. **Report back:** the FPS number in the top-left at steady state (it targets 1,000 agents on desktop), and how the strike *feels*.
7. Commit everything Unity generated: the `.meta` files (including inside `../sim/src/Cipher.Sim/`), `game/Packages/packages-lock.json`, and `game/Assets/Scenes/` if you saved a scene — they're asset identity cards and belong in git.

## Known first-APK caveats (handled when we get there)

- The `Standard` shader is referenced by code (`Shader.Find`); device builds need it in *Project Settings → Graphics → Always Included Shaders* or it strips to magenta. Editor Play mode is unaffected.
- Android build settings (IL2CPP/ARM64, landscape, target SDK) get committed as ProjectSettings once the editor has generated them.

## Layout

- `Packages/manifest.json` — dependencies; the sim core mounts as a local package (`com.cipher.sim`)
- `Assets/Scripts/Cipher.Game.asmdef` — game assembly; references `Cipher.Sim` + Input System
- `Assets/Scripts/Bootstrap/FloodBootstrap.cs` — Milestone 1 entry point (procedural scene, fixed-tick sim loop, instanced rendering, input)
