# How to hand the art over

**For:** the owner · **Date:** 2026-09-11
**Why:** he asked how to give ENG access to the art. Short answer: buy it, put the files on the laptop,
say where. No account access, no password, no card details are needed or wanted.

## What to buy

The list and the licence verdicts are in [ADR-007](decisions/ADR-007-crowd-rendering-and-art-lane.md).
Roughly **$135** for four packs in one coherent style. One style matters more than breadth: mixing a
photoreal pack with a stylised one is the single biggest way this goes wrong, and under the comic shader the
stylised lane reads as drawn art rather than as a budget shortfall.

## Route A — Synty direct (simplest)

1. Buy at the Synty store with any account. The packs download as `.unitypackage` files.
2. Save them anywhere on this laptop. A folder like `Downloads/synty` is ideal.
3. **Close the Unity editor** if it is open. Unity refuses a second instance on one project and the import
   will fail with a lock error.
4. Tell ENG the folder.

That is the whole handover. ENG runs `tools/art/import-packs.sh <folder>`, which imports each package
headlessly, in sorted order, logging to `.artifacts/art-import/`.

## Route B — Unity Asset Store

Works too, and has one extra click because Asset Store packages arrive through the editor rather than as a
file you download directly.

1. Buy on the Unity Asset Store **signed in as the Unity ID already on this machine** (the same one CI
   activates with). Buying on a different account means the packs are not visible to this editor.
2. Open the project in Unity, then **Window → Package Manager → My Assets**, and press **Download** on each
   pack. Download only; there is no need to press Import.
3. Close the editor and tell ENG.

Downloaded packages land in `%APPDATA%/Unity/Asset Store-5.x/<Publisher>/<Category>/<Name>.unitypackage`,
and the same import script handles them.

## What ENG does NOT need

- **No password.** The Unity ID password is already a CI secret and should be rotated at some point anyway;
  nothing about importing art needs it again.
- **No card details.** Ever, for any reason.
- **No new account.** Everything here runs on what already exists.

## The free tier, available right now

These need no purchase and no permission, and ENG can pull them at any time:

| Source | Licence | Good for |
|---|---|---|
| Poly Haven | CC0, no attribution | Props, HDRIs |
| Kenney | CC0, no attribution | Greybox props, road and barrier kits |
| Quaternius | Commercial use, no attribution | Stylised props, vehicles, some characters |

They will not carry the game on their own, which is why the purchase still matters, but they are genuinely
useful for filling a street and they cost nothing.

## After the files land

ENG imports, then in order: environment kit into The Gate, the crowd bake pipeline, the hero, a dressing
pass. Roughly three weeks to a playable graphical beta of Mission 1, per
[PLAN-2026-09-11.md](design/PLAN-2026-09-11.md). Every day the purchase waits, that date moves a day.

**One warning worth repeating:** never let a non-commercial asset into the project. FLUX dev concept renders
and any Sketchfab model under an editorial licence are reference only. The shipping rule is simple: if the
licence does not clearly permit commercial use, it does not go in `game/`.
