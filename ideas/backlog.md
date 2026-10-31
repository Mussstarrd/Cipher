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
