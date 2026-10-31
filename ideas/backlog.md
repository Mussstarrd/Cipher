# Backlog

Wanted, not in v1. Each line: what, why deferred, source.

## ReleaseLine (current winner, decision 0006)
- **Optional Claude API prompt generation** for Kling prompts and descriptions, behind an
  API key in Settings; template engine stays the default. Deferred: no required cloud
  bill in v1.
- **Automatic hook detection** (loudest/most repetitive 30 s) to suggest the clip start.
  Deferred: needs audio analysis beyond the standard library.
- **Burned-in title card / captions** on the final video. Deferred: font handling.
- **Loudness measurement** of the master (ffmpeg ebur128). Deferred: v1 keeps the
  master stage as a checklist.
- **Direct uploads** via platform APIs. Deferred: OAuth, terms, churn.

## Archived (Pipeline Scorecard, superseded by 0006)
- Reason tag on each flag; Robinhood CSV journal for strangers; live brokerage sync;
  phone-first quick entry.

## Added by the builder during the v1 build (not in spec, not built)
- **Archive button on the track page.** The API accepts `status: archived` and the
  Dashboard lists archived tracks, but there is no UI control. Deferred: not in spec §4.
- **Open folder button** (reveal `library/<slug>/` in Explorer/Finder). Deferred: needs
  per-OS subprocess calls; copying the path works for now.
- **Draft history** (keep earlier chosen prompts instead of replacing). Deferred: v1
  keeps the Drafts list short on purpose.
- **Per-track metadata edits survive Regenerate** (mark a metadata draft as chosen).
  Deferred: edits already persist; only Regenerate discards them.
- **Delete a track.** Deferred: archive covers the need without data loss.
