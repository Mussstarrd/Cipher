# Roadmap — September/October 2026 (brain-trust synthesis)

**Date:** 2026-09-10 · **Author:** ENG · **Inputs:** four parallel brain-trust memos in this folder, written the same day against the owner's hero playtest notes (`docs/feedback/2026-09-10-hero-first-play.md`).

| Memo | Owns |
|---|---|
| [breach-and-repair.md](breach-and-repair.md) | the "smart" wall-breacher (Sapper), hole stages, repair drone |
| [sim-dynamic-walls-plan.md](sim-dynamic-walls-plan.md) | how the deterministic sim supports mutable walls, gates, re-routing, preview honesty (measured: full flow-field recompute = 0.38 ms) |
| [economy-towers-and-aiming.md](economy-towers-and-aiming.md) | aimable line airstrike, cash economy, barricade + turret, build-mode controller UX, tower-hunting Spitter |
| [art-pipeline-and-skins.md](art-pipeline-and-skins.md) | rendering path for animated runners, asset sources, what "skins" means, honest dates |

## Decisions taken (ENG, owner veto)

Where memos disagreed, this is the call:

1. **Airstrike: aim by looking, and it becomes a line.** Marker sits where the camera looks at the ground, clamped 6–24 cells. Tap Y fires; hold Y previews and fires on release; B cancels. Six bombs walk the 14×4 line far→near after a 1.2 s inbound delay; 8 s cooldown. This needs camera pitch on the right stick, which the gun pass needs anyway. Ships **first** because it is the smallest change with the biggest feel delta.
2. **LB becomes a tap-toggle into build mode** (overhead rig). The hold-to-peek from the hero graybox is retired; ADR-002 already said build mode is a toggle.
3. **Cash economy**: $5 per kill from any source, $100 × wave number on wave clear, $400 start, no passive income yet. Barricade $20 / 200 HP. Sentry .50 turret $150 / 300 HP / range 10 / 60 dps / targets "first". Refund 100 % in setup, 50 % × remaining health mid-wave. Full seals are **allowed and loudly labelled** ("SEALED — they will chew through here"), never refused.
4. **Breacher = the Sapper**, a rare, telegraphed unit that walks the normal field, picks the first player wall whose far side is a big shortcut, plants a kit (4 s), then the hole widens on a **timer** (owner's words: "progressively gets worse until the wall collapses"): stage 1 admits ~1 runner/s for 20 s, stage 2 ~3/s for 20 s, then collapse. Traffic through the hole shaves time off the stages, but time alone is enough — sealing and camping cannot stall it. Throughput is a per-cell admission gate; attraction is field cost, so the preview stays honest (only agents whose detour beats the hole's cost divert at stage 1; everyone at collapse).
5. **Counterplay ladder**: kill the Sapper on its 18–25 s walk (60 HP, ~1 s of LMG), shoot the kit (40 HP, freezes the stage), or buy a **Repair Drone** ($150, one stage per 4 s, works only while the hero is within 8 cells — leaving the front line is the cost).
6. **Spawn rates**: the owner's 0.05 % is the late-game floor, not the v0 rate. At graybox kill rates 0.05 % would show one Sapper every 5–8 minutes and he would never see it. v0: scripted first Sapper at T+45 s, then 1 per 200 spawns with ≥60 s spacing, one active breach at a time. Spitter: 0.5 % plus a pity timer (first at 40 s, then at least one per 45 s, max 3 alive).
7. **Two rare archetypes with different jobs**: Sapper re-routes (walls); Spitter kills DPS (turrets: nearest within 12 cells, lobs acid from 9 cells at 10 dps, so the 10-range turret out-ranges it but ignores it under "first" targeting — the hero has to rotate). The blueprint's Wrecker brute ships after both.
8. **Determinism stays absolute**: all randomness (Sapper/Spitter spawn picks, tie-breaks) comes from a seeded xorshift inside the sim, folded into the state hash. The build preview uses a scratch copy of the live field running the identical compute. Full flow-field recompute on change, coalesced to once per tick (0.38 ms on PC, est. 1.5–2 ms on a mid phone); incremental algorithms are not needed below ~10k cells.
9. **URP migration happens right before art lands, not before gameplay.** It is 1–2 days with four procedural materials; the memo confirms custom shaders (VAT) do not port, so it must precede the character pass.

## Build order and dates

Today is Thursday 2026-09-10. Dates are what the owner can *play*, assuming one engineer, gameplay first, cosmetics ~30 % once M2 lands. Each row is a CI-built exe on the Desktop.

| When | Build | What the owner will see |
|---|---|---|
| **Fri Sep 11** | Hero 0.2 | Aimable line airstrike: look, tap Y, the street erupts in a line where you looked. Right stick tilts the camera. |
| **Thu Sep 17** | **Maze v0 (Milestone 2)** | LB into build mode; place barricades and a turret with cash from kills; live pathing preview that never lies; 5-wave table with win/lose; full-seal warning. |
| **Thu Sep 24** | Maze v1 | The Sapper breaches a wall, the hole widens, the horde pours through, you shoot the kit or buy a drone; Spitters hunt your turret. |
| **Wed Oct 1** | Gun pass (start of Milestone 3) | Aim assist, hit feedback, sprint; "one more wave" test build. **Owner action:** line up 3–5 friends with controllers (Open Decision #7). |
| **Thu Oct 8** | **First characters** | URP done; a real (placeholder) zombie runner with run/attack/die animations at 1,000 count, and a rigged hero holding a gun. Still one look each. |
| **Late Oct** | Art direction pass | Hero with 2 swappable outfits, runners in 2 variants, colour tints per instance. |
| **~Feb 2027** | Skins proper | Earned cosmetic tiers per the pitch (tracksuit → armor-weave → gold-plated). Long pole is commissioned art and the campaign-milestone system, not rendering. |

What would slip these: a Unity bug that only shows on the owner's machine (no Unity CI for play-mode yet, only builds), VAT on Android needing a real phone soak, and taste loops on art. The first two are engineering risk I own; the third is a budget conversation (~$2–8k of outsourced art for the skins row).

## Questions for the owner (answer whenever; defaults are in force meanwhile)

1. **Repair drone: escort or fire-and-forget?** Default: works only while you are within 8 cells, so repairing costs you front-line time.
2. **Can city walls ever be breached, or only what the player builds?** Default v0: player barricades and the graybox serpentine walls; the arena boundary never.
3. **Airstrike charge: cooldown timer or the pitch's Respect meter (charged by kills)?** Default v0: 8 s cooldown; Respect meter arrives with the gun pass.
4. **Is kills-only income right for v0**, or do you want a passive racket trickle from day one? Default: kills and wave-clear only.
