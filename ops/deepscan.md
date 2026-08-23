# The daily deep scan — the machine reflecting on itself

Jeffery's instruction, 2026-08-23: review "the entirety of the idea, the code,
the problems, the history, response, reaction, plans, fixes … basically self
reflection and absorption independently."

Two reflection loops exist and must not be confused. Hearth's 22:00 review
reflects on the FAMILY's day and writes memory/. This scan reflects on the
MACHINE — and writes ops/. It runs daily at 05:30 ET from the design session,
unattended. The 2-hour heartbeat is the mid-scan; this is the deep one.

## The questions, in order

1. **Did yesterday's promises hold?** Read the last deepscan's "watch" list,
   ops/status/ since then, and the backlog. Anything promised-and-quiet is the
   first finding — an assistant that drops its own commitments cannot be
   trusted with a family's.
2. **What actually broke or misfired?** Journal warnings in status blocks,
   failed wakes (expected-vs-fired line), review parse failures, rollbacks,
   push conflicts. For each: root cause named, not symptom restated.
3. **What is the family's behaviour saying?** Read memory/misses.md,
   corrections.md, and the shape (not the private content) of usage: which
   features get used, which got tried once and abandoned, where someone had to
   ask twice. An unused feature is a finding. Scratchpad content is NEVER read
   for this — volume at most, words never.
4. **Where is the code lying or drifting?** Skim the last 24h of diffs plus one
   rotating subsystem in full (memory → brain → server → loops/markets/weather →
   public → scripts, one per day). Look for: comments that no longer match
   behaviour, duplicated logic, error paths that swallow, guards that can never
   fire, state that grows without a sweeper.
5. **Is the idea still on course?** Reread CLAUDE.md and the backlog against
   what got built. Efficiency includes NOT building: name anything in flight
   that the family's actual behaviour says nobody needs.

## What it may do with the answers

- Write `ops/review/YYYY-MM-DD-deepscan.md`: findings, each with evidence and
  either a fix, a backlog item, or an explicit "watching, not acting".
- Update ops/backlog.md — add, reprioritise, or kill items, with reasons.
- Apply and push SMALL, safe fixes directly (the droplet applies them within
  15 minutes). Anything that touches attribution, rooms, memory-write rules,
  or money handling is proposed in the deepscan file instead, and waits for
  Jeffery.
- Message Jeffery ONLY when something needs his hands or his decision.
  A green scan is silent. Rule 6 applies to the engineers too.

## What it may never do

The NEVERs do not sleep and are not this scan's to reinterpret: nothing
outward, no real trades, no memory/ writes (that is the 22:00 review's or a
human's), no reading scratchpad content, no widening its own permissions. A
scan that concludes a NEVER is inefficient writes the argument down for
Jeffery and stops there.
