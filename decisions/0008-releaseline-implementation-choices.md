# 0008 — ReleaseLine v1 implementation choices that closed doors

**Date:** 2026-10-31
**Phase:** 4
**Decided by:** builder (recorded by historian)
**Status:** accepted

## Context
Building `spec/SPEC.md` required several choices the spec left open. These are the ones a
future change would have to work around.

## Options considered
Listed per item in `app/NOTES.md`; only the decision and its cost are recorded here.

## Decision
- **Storage shape.** One SQLite file (WAL mode, explicit transactions, one connection per
  request). Stage state is a merged JSON blob per stage; drafts are one row per editable
  field as JSON `{label, text, ...}`; the assets table is filled only from verified
  paths. Settings live in `data/settings.json`, written atomically.
- **Path fields are strict.** A path that does not exist is rejected with a sentence and
  not saved, rather than saved with a warning.
- **Stage completion.** Cut and Assemble mark their own stage done on success; checklist
  stages need an explicit Mark done; Reopen is always available.
- **Prompt engine.** `random.Random(seed)` over vocab banks keyed by 12 genre families,
  10 mood categories, 3 energies, 7 presets, each bank at least 8 entries. Each variant
  carries its own shot list: first shot establishing, last shot a held frame, subject,
  setting, lighting, and style constant, camera and moment varying.
- **Assemble pipeline.** Normalize each shot (scale and pad to the aspect, 30 fps,
  H.264) → concat via a list file → mux with the audio clip (AAC 192k), using
  `-stream_loop -1` plus `-shortest` so the visual always matches the audio length.
- **Metadata.** Type-beat titles have no "[FREE]" prefix; licensing wording is the
  owner's call. Platform format reminders are generic to avoid quoting limits that go
  stale.
- **UI.** Hash routing, DOM built with an element helper (no innerHTML with user data),
  one expanded stage at a time, clipboard API with a fallback.

## Why
Buildability and durability: every choice avoids a dependency or a probe step that could
break on a stranger's machine. Honesty: strict paths and verified assets mean the app
never claims a file exists when it does not.

## Consequences
Changing the stage data shape later means a migration over JSON blobs. Prompt quality is
bounded by the banks until an optional model call is added (backlog item one). Revisit
the assemble pipeline if the owner wants captions or title cards (needs font handling).
