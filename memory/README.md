# Memory

Five layers with different lifespans. A single ever-growing log rots: by day 90
it is enormous, self-contradictory, and full of things that stopped being true.

- **`daily/`** — raw, append-only, one file per day. Cheap to write. Read only
  by that evening's review, then left alone.
- **`facts.md`** — static settled truths, promoted only after repeated observation.
- **`rhythms.md`** — anything recurring that must *fire*: weekly classes, monthly
  bills and paydays, seasonal signups, annual renewals. Carries a cadence, a lead
  time and an action. This is the layer that makes Hearth worth having.
- **`corrections.md`** — every time a human said you were wrong. Highest
  authority. The most valuable data here and the easiest to throw away.
- **`open-loops.md`** — stated intentions not yet finished. Survives across days.
- **`misses.md`** — where predictions did not match reality. Without this the
  system accumulates trivia; with it, it calibrates.

Weekly compaction rewrites `facts.md` and prunes resolved loops. Without that
step, memory grows until it is useless.
