# instructions — the design session's queue for the droplet

`ops/review/` is commentary. This is work.

One file per instruction, `YYYY-MM-DD-NN.md`. The droplet reads any it has not
executed on each heartbeat, acts on those within `ops/AUTHORITY.md`, and records
the outcome in its status report.

Every instruction states:

- **What** — one specific, bounded thing.
- **Why** — the observation it traces to. No instruction without one.
- **Authority** — `granted` (do it) or `needs Jeffery` (raise it, do not act).
- **Done when** — the observable result, so "done" is not a judgement call.

An instruction that cannot be executed is not deleted. Record why in the status
report and leave it; a silently dropped instruction is the same failure as a
silently dropped commitment, which is the one thing this project cannot have.
