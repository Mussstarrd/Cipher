# How to build a living, idle, learning, group assistant

Four properties, four different problems. Most products get one or two and the
result feels like a chatbot with a calendar bolted on.

- **Living** — it exists between conversations
- **Idle** — it does work when nobody is talking to it
- **Learning** — what it knows compounds instead of resetting
- **Group** — several people share one of it

Five foundations. Everything else is a detail.

---

## 1. Memory is a repository, not a database

Plain text files, versioned in git, tiered by lifespan, with a scheduled
compaction pass.

A database is the obvious choice and the wrong one here, for four reasons that
all matter more than query speed:

- **Diffable.** You can see exactly what it learned, when, and what changed. A
  row that silently updated tells you nothing.
- **Auditable by the family.** Anyone can read every word held about them. That
  is not a nice-to-have — it is the only honest answer to "what does it know
  about me", and the first time someone feels watched by it, it is dead.
- **Portable.** No vendor, no migration, no export feature to build.
- **Mergeable.** Text merges. Application state does not.

The tiering is what keeps it viable forever: raw daily logs are written freely
and never read again after their day; only the compacted layers load into a
session. A household produces under 2 MB of text a year, and the *loaded*
working set stays a few thousand tokens indefinitely. **Storage size is never
the constraint. Skipping compaction is.**

## 2. The heartbeat lives outside the conversation

The single thing that separates living from responsive is that something fires
when nobody is talking. Three kinds of wake, and the third is the one nobody
builds:

- **Scheduled** — the check-ins, the nightly review, weekly compaction.
- **Event** — mail arrives, a photo lands, a webhook fires.
- **Deadline-derived** — the agent reads its own rhythms and open loops, works
  out what is about to matter, and schedules its *own* next wake for it.

Cron gives you a system that runs on time. The third gives you one that seems to
be paying attention, because it wakes for reasons that came from what it knows
rather than from a fixed table.

## 3. The event log is the truth; the state files are a projection

This is the fix for the whole class of bug where two writers clobber each other.

With several humans and an agent all writing, **nobody edits state directly.**
Everyone appends immutable events — *Jeffery said this*, *Hearth learned that*,
*a form came in*. The nightly review folds the log into the readable state files.

Three things fall out for free:

- Concurrent writes stop being a problem: appends do not conflict.
- The audit trail is the storage format, not an extra feature.
- Rebuilding state after a bad inference is a replay, not a repair.

The rule that follows: **never publish state assembled only from a local copy.**
Read what is live, treat it as authoritative, merge, then write.

## 4. Channels are adapters; the brain must not know they exist

Email, SMS, a PWA, a shared page — each is a thin adapter that turns messages
into events and events into messages. The assistant never knows which one it is
talking through.

This is why the delivery surface is the last mile and the cheapest thing to
change, and why it should never be chosen first. It also means the universal
channel and the good channel can coexist: email reaches the grandparent who will
never install anything, the app reaches everyone else, and both land in the same
log.

## 5. Learning has to be adversarial towards itself

Recording is not learning. A system that only accumulates gets fatter and more
confident, which is worse than forgetting. Four mechanisms:

- **Provenance on everything.** *Told* is a hypothesis. *Observed n times* is a
  belief. Never let the first silently become the second.
- **Corrections outrank inference, permanently.** A human saying "no" beats any
  amount of self-observation, forever, with no decay.
- **Decay for the unconfirmed.** A belief that was expected to show up and did
  not gets weaker, not repeated. Without this you get a system that is
  confidently wrong for years.
- **A pass that hunts its own errors.** The nightly review asks *what did I get
  wrong today*, not *what happened today*. That one word is the difference.

---

## The constraint nobody wants to hear

**Trust is the product.** The thing that makes this valuable — that it holds
years of a family's life in detail — is exactly what makes it dangerous when it
is wrong, and unrecoverable when it leaks. One missed appointment and everyone
reverts to the old system permanently, and correctly.

So the order is: reliable, then honest about what it does not know, then useful,
then clever. A system that is right four days in five is worse than none,
because everything still has to be checked.

## Concrete stack

| Layer | Choice | Why |
| --- | --- | --- |
| Heartbeat + host | Managed Agents with scheduled deployments | Fires sessions on a cron and hosts the container; no scheduler to run or keep alive |
| Isolation | One container and one memory store per household | Clean tenancy from day one, not retrofitted |
| Memory | A git repo per household | See §1 |
| Writes | Append-only event log, state as projection | See §3 |
| Channels | Email first, PWA second, SMS via a number if ever needed | Email is the only channel everyone already has |
| Models | The strongest model for the nightly review and anything reasoning-heavy; a cheap fast one for routine lookups | The review is where the compounding happens — do not economise there |

## What today's prototype is missing

Named honestly, because the gap between this document and `app/` is the roadmap:

1. **Nothing fires.** The four check-ins do not run unattended. This is the
   single largest gap and no amount of compaction fixes it.
2. **State is a projection with no log behind it.** §3 is unimplemented.
3. **Republishing overwrites.** Mitigated by a rule, not by architecture.
4. **No identity.** Verified against contract 0.2.14: this account can declare
   only `artifact`, `downloads`, `mcp` and `self`. No `user`, `db`, `room` or
   `assets` — so no in-app camera and no knowing who is typing until it runs
   somewhere else.
