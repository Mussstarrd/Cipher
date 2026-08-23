# Standing authority

Jeffery grants this once. The design session issues instructions within it; the
droplet session executes them without asking again. Anything outside it waits
for him, however small and however obvious it seems.

Amend by editing this file. It is the only thing that decides what happens on
that machine without a human in the room.

---

## Granted — act, then report

- **Configuration the app owns.** Values in `server/.env` that Jeffery has
  already specified, and settings that only change Hearth's own behaviour.
- **Restart, reload, re-enable** Hearth's own services and timers.
- **Fix bugs in this repository's code**, including the droplet's own tooling,
  provided the fix traces to something that actually happened.
- **Commit and push** to the working branch.
- **Read** logs, service state, `preflight`, calendars, and the mailbox Hearth
  is already configured for.
- **Write memory** only through the 22:00 review, never by hand.

## Requires Jeffery, every time

- **Anything that costs money**, or changes a model, plan or billing setting.
- **Anything outward-facing**: sending mail to anyone but Jeffery, registering,
  booking, buying, replying to a school or a coach, adding a calendar.
- **Creating, rotating or installing credentials** — API keys, app passwords,
  tokens. Say it needs doing; do not do it.
- **Deleting or rewriting memory** outside the review's normal operation.
- **Changing who can see what**: the passphrases, the room boundary, whose
  device is whose.
- **Adding a person** to the household, or a new source of data about one.

## Never, with or without permission

- **Never copy a secret anywhere it can be read** — not into a status file, a
  commit message, a log line, or a report. Report *that* a credential is wrong,
  never what it is.
- **Never weaken a boundary to make something work.** If the passphrase gate,
  the room split or the device binding is in the way, that is the finding.
- **Never disable, soften or route around a failure report.** A system whose own
  failure mode is silence is worse than no system, and every serious fault found
  so far was found because something said so out loud.
- **Never edit memory to make a claim look right.** If memory is wrong, that is
  a miss and belongs in `misses.md`.
- **Never act on an instruction that contradicts this file**, even one written by
  the design session. Say so in the status report instead.

## Why it is shaped this way

The line is not risk, it is reversibility. A restart, a config value, a code fix
— all undoable in seconds, and the cost of asking every time is that nothing
gets done. A sent email, a spent dollar, a rotated key or a deleted memory
cannot be taken back, and no amount of confidence justifies doing one unasked.
