# The Hearth app

The family channel. One page, shared, no backend.

**Live:** https://claude.ai/code/artifact/b6da8b8b-568c-49e6-b679-3e790542475d

## What it is

A single self-contained HTML file. It holds its own state in a JSON block and
saves changes by **republishing itself** as a new version — every open view
reloads to it. No database, no server, no hosting bill.

- **Channel** — Hearth's four daily check-ins plus family messages.
  Hearth speaks in serif, the family in sans; you can tell who is talking
  without reading a name.
- **This week** — the recurring shape, today highlighted. Seasonal items that
  are coming round again. Open loops with a checkbox.
- **Teach Hearth something** — the intake, as an in-app flow rather than an
  interview. It never ends: everything added is marked *told*, and only
  observation over the following weeks turns a guess into something relied on.

## Honest limits of this version

- **Writers need a Claude account.** Viewing is a link; writing is not. This is
  the adoption wall, and it is the reason the real version is a PWA.
- **No identity capability**, so the page cannot tell who is typing. Everyone
  picks their name; it is remembered per device.
- **Hearth posts on schedule, not instantly.** Check-ins are written into the
  channel at 07:00 / 12:00 / 17:00 / 22:00. Nobody needs an instant reply from a
  household assistant, so this is a real v0 rather than a mock.
- The status chip never lies about which of these is happening: *saved for
  everyone*, *this device only*, or *read only*.

## Seed data

Real, from Jeffery: Abby's dance Saturdays 08:30, Aiden's soccer Tuesdays and
Thursdays 17:30–18:30. The fall soccer registration flag is live because it is
late August and that is exactly the case this product exists for.

## What the real version needs

An installable PWA: accounts, push, real-time, one container per family. That
removes the account wall and the schedule-only posting. Everything above the
delivery layer — memory, rhythms, check-ins, paper-trail — carries over unchanged.
