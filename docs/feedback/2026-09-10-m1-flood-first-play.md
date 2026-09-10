# Owner playtest — Milestone 1 "The Flood" graybox, first play

**Date:** 2026-09-10
**Build:** branch `claude/game-studio-audit-pitch-nxaqhx` @ b3da41d + Unity-generated project files (this commit)
**Machine:** owner's Windows 11 laptop, Unity 6000.0.83f1 editor Play mode (Built-in RP, no vsync setting changed)
**Input:** Xbox controller (left stick = cursor, A = airstrike)
**Target density:** 1,000 agents (desktop default in `FloodBootstrap`)

## Owner notes (verbatim)

> airstrike does an AoE thing around my red ball. fps stays right between 950-1025. played and killed 1000 or so and let it run another 20 seconds

## Engineering annotations

- Session length: ~1,000 kills plus a further ~20 s at steady state, so the flood reached its 1,000-agent top-up density.
- The "950-1025" figure: the HUD reads `fps N | alive N | breached N | kills N`. 950–1,025 is also exactly the band the *alive* count sits in once the trickle spawner saturates. Asked the owner to re-read the first number; if confirmed as fps, this laptop is far above the PC target and the next density step (2,000–4,000) is safe to try. If it was the alive count, fps is still unrecorded. **Resolution below once the owner replies.**
- "red ball": the strike cursor is authored orange (1, 0.65, 0.1). Reads as red to the owner under the current dark palette / Standard shader lighting — note for when the cursor gets real art (Trap: cursor must be unambiguous against the red flood).
- No exceptions, errors, or shader-missing warnings in Editor.log during the session.
- First open required **zero code fixes**: sim + game + editor assemblies compiled clean headlessly before the editor GUI was ever launched.

## Follow-ups

- [ ] Confirm fps vs alive reading (owner).
- [ ] Airstrike feel: owner described *what* it does, not yet *how it feels* — ask directed questions next session (impact readability, cooldown, radius vs horde width).
