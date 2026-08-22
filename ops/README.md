# ops — the exchange between the two sessions

Two Claude sessions work on Hearth and cannot reach each other:

- **the droplet session** — root on the machine. Sees logs, timings, real
  failures, real cost. Cannot see why anything was built the way it was.
- **the design session** — Jeffery's chat. Has the whole history: every decision,
  every rejected alternative, every bug already fixed and why. Cannot reach the
  machine at all.

Neither can message the other. **This directory is the mailbox**, and git is the
transport.

```
droplet  ──►  ops/status/YYYY-MM-DD-HHMM.md  ──►  design session reads it
design   ──►  ops/review/YYYY-MM-DD.md       ──►  droplet reads it next run
```

## The status report — written by the droplet, twice a day

Lead with **what went wrong for the family**, not with system metrics. A green
dashboard on a product nobody opened is a failure report.

1. **Did it do its job?** Each of the four wakes: fired or not, and if it fired,
   was the output any good. Anything a human corrected.
2. **What broke.** Errors from the journal, with the actual text. Include things
   that recovered on their own — those are early warnings.
3. **What memory learned.** The diff to `memory/`: facts promoted, rhythms
   fired, loops opened and closed, misses recorded. This is the product working
   or not working, and it is more informative than uptime.
4. **What it cost.** Tokens and dollars for the day, per wake if known.
5. **Adoption.** Who actually opened it. If nobody but Jeffery has, say so
   plainly — that is the single most important number here and the easiest to
   quietly omit.
6. **Questions for the design session.** Things where knowing *why* something was
   built would change what you do about it.

## The review — written by the design session

Answers the questions, judges the proposals, adds what the machine cannot see:
whether a change contradicts a decision already made and why it was made.

## The rule that keeps this useful

**Every proposed change must trace to something that actually happened.** A log
line, a failed wake, a correction from a human, a cost that surprised someone.

Two models reviewing each other will otherwise generate endless plausible
improvements — refactors, abstractions, features nobody asked for — and it will
all look like progress. It is not. If nothing went wrong today, the correct
report is short and proposes nothing.

## Never in these files

Secrets, tokens, passwords, calendar URLs. The contents of the family's mail.
Health, money, or anything about the children beyond what is needed to explain a
failure. Report *that* an email was misread, not what it said.
