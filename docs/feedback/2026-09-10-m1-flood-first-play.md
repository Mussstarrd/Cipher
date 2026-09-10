# Owner playtest — Milestone 1 "The Flood" graybox, first play

**Date:** 2026-09-10
**Build:** branch `claude/game-studio-audit-pitch-nxaqhx` @ b3da41d + Unity-generated project files (this commit)
**Machine:** owner's Windows 11 laptop, Unity 6000.0.83f1 editor Play mode (Built-in RP, no vsync setting changed)
**Input:** Xbox controller (left stick = cursor, A = airstrike)
**Target density:** 1,000 agents (desktop default in `FloodBootstrap`)

## Owner notes (verbatim)

> airstrike does an AoE thing around my red ball. fps stays right between 950-1025. played and killed 1000 or so and let it run another 20 seconds

Follow-up when asked to confirm fps vs. the alive count:

> i was reading rhe fps correctly. i understand the metrics behind alive and breach (enemies on board vs got through exit). the fps is averaging right at 1000 it is a constant faster than human eye from 975-1050 while the living enemies are actively moving the track

## Engineering annotations

- Session length: ~1,000 kills plus a further ~20 s at steady state, so the flood reached its 1,000-agent top-up density.
- The "950-1025" figure: the HUD reads `fps N | alive N | breached N | kills N`. 950–1,025 is also exactly the band the *alive* count sits in once the trickle spawner saturates. Owner confirmed it is fps. **Result: ~1,000 fps (975–1,050) at 1,000 live agents, Editor Play mode, vsync off.** That is roughly 16× the 60 fps desktop bar with the swarm saturated, so the desktop density step to 2,000–4,000 agents is safe to try; the interesting ceiling on this machine is the sim's 30 Hz step cost, not rendering.
- "red ball": the strike cursor is authored orange (1, 0.65, 0.1). Reads as red to the owner under the current dark palette / Standard shader lighting — note for when the cursor gets real art (Trap: cursor must be unambiguous against the red flood).
- No exceptions, errors, or shader-missing warnings in Editor.log during the session.
- First open required **zero code fixes**: sim + game + editor assemblies compiled clean headlessly before the editor GUI was ever launched.

## Follow-ups

- [x] Confirm fps vs alive reading (owner) — confirmed ~1,000 fps.
- [ ] Airstrike feel: owner described *what* it does, not yet *how it feels* — ask directed questions next session (impact readability, cooldown, radius vs horde width).

## Addendum — first CI-built player (run 34487824831), same day

Owner ran `CipherDeadTurf.exe` from the CI artifact and sent two photos with:

> That's all it shows

Photos: a light-gray ground plane, the orange cursor sphere, HUD `fps 60 | alive 1000 | breached 888 | kills 621`, no agents, no walls.

- Diagnosis: instanced draws (agents, walls) were stripped from the player — `m_InstancingStripping` was "Strip Unused" and the instanced materials are created at runtime, so the stripper saw no user. Fixed by Keep All; Standard FORWARD variant count doubled in the build log (256 → 512 vp, 4096 → 8192 fp).
- `fps 60` in the player is vsync (QualitySettings default), not a perf regression.
- Kills still counted because the sim doesn't care whether anything is drawn — "preview never lies" cuts both ways: the HUD was truthful, the picture wasn't.
