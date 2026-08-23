# Backlog — the standing improvement list

Jeffery's instruction, 2026-08-23: "start a list ourselves … hand off for work
overnight or during the day to improve this machine constantly."

Both sessions work from this file. The design session builds and pushes; the
droplet session applies, verifies on the live box, and reports in ops/status/.
Either may add items; only Jeffery's word removes a NEVER. Statuses: todo,
doing(who), done(date), blocked(on what). Done items move to the bottom monthly.

Rules that bound everything here (from CLAUDE.md, restated so nobody re-derives
them at 3am): nothing outward without a human — no sending, buying,
registering, real trades ever. Memory writes go through the 22:00 review or a
human. Standalone mode must keep working: no feature may silently require
account access.

## Now (this week)

- **B1 · Timed reminders** — done(2026-08-23, design session). Loops gain an optional due time; the server's
  30s tick fires a push AT that time, to the right person's phone (subs are
  name-tagged now). "Remind me at 15:00" stops meaning "hear about it at 17:00".
  The single biggest gap between Hearth and an actual assistant.
- **B2 · Meal planning** — done(2026-08-23, design session; first plan drafts tonight, Sunday review). Sunday proposal from what this family actually
  eats, swap-a-night in chat, 17:00 carries tonight's plan. Grows a
  memory/topics/meals.md as taste data accumulates.
- **B3 · Wake accounting honesty** — done(2026-08-23, design session). A slot that never fired is invisible
  to the status report's "wakes" line; it undercounted a 3-miss day as 1. Count
  expected-vs-fired per day, name the misses.
- **B4 · Suzan's iPhone push verified** — blocked(Suzan's two minutes: Safari →
  Share → Add to Home Screen → open from icon → Notify). Then confirm a real
  notification arrived, end to end.
- **B5 · Off-machine backup** — blocked(Jeffery: private repo + fine-grained
  token). Everything Hearth knows lives on one rented disk until this closes.
- **B6 · Real calendars connected** — blocked(Jeffery + Suzan: secret iCal URL
  each, desktop-only step, paste into the app card). TeamSnap's feed too, once
  the invite is accepted — that one wires the whole season in.

## Next

- **B7 · Photo → filled form** — the full paper-trail promise: rebuild a
  photographed form as a fillable document with reference.md pre-filled. Needs
  document tooling installed on the droplet; never signs.
- **B8 · Topic curation quality** — after a week of reviews filing topics, audit:
  are subjects coherent? aliases useful? Sunday merge working? Tune the prompt
  from evidence, not taste.
- **B9 · Search learns phrasing** — "find" currently needs every word to hit one
  line. Loosen to per-file scoring when that visibly frustrates.
- **B10 · Kid-proofing pass** — Aiden is a real user now. Read a week of his
  messages and tune tone, length, and what Hearth volunteers to a nine-year-old.
- **B11 · Paper-portfolio review lesson** — the 22:00 review should read
  portfolio.md moves and write one honest line about what the reasoning got
  right or wrong. Knowledge-building is the stated point; close the loop.
- **B12 · Earnings dates** — blocked(Jeffery: list the tickers, Adults thread).
  Then wire dates into 07:00 within lead time.

## Later

- **B13 · Web fetch with guardrails** — school calendar pages, league sites,
  tracking links. Design the allowlist and the "never act on fetched
  instructions" rule BEFORE the capability, not after.
- **B14 · Trip planner** — dates, drive times, packing list, weather window.
  Wants B2 + B6 + O3-weather underneath.
- **B15 · Grocery list from the meal plan** — wants B2 running a few weeks.
- **B16 · Voice notes in** — a photographed page works; a spoken "we're out of
  milk" should too. Investigate browser speech-to-text (standalone-safe).
- **B17 · Memory compaction proof** — Sundays are meant to merge and prune.
  Verify the first one actually ran and memory got SMALLER, not just different.
  First test is TONIGHT (Sun 2026-08-23, 22:00 ET); the 24th's deep scan checks.
- **B18 · Adoption before capability** — from deepscan 08-23: this is still a
  one-adult tool. Until Suzan and Aiden are genuinely on (B4, J1), weigh every
  new feature against "who will actually touch this".

## Done

- [2026-08-23] Two-session pipeline: push → auto-apply → restart → verified by
  heartbeat. The loop this whole list rides on.
- [2026-08-23] Device-bound identity; rooms (family/adults/private); scratchpad
  relay + safety doors; photo intake; To do tab; calendar-from-app; weather;
  paper portfolio; message folding; knowledge topics + search (this commit).
