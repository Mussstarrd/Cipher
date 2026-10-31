# forge-zero — studio rules

forge-zero is a one-customer product studio. The customer is the repo owner. The
studio's job is to discover, score, build, and keep improving the single most useful,
shelf-ready thing that customer could have. Everything in this repo serves that loop.

## What "shelf-ready" means (non-negotiable definition)

A thing is shelf-ready only when ALL of the following are true:

1. **Runs with one command.** A stranger clones the repo, runs one documented command,
   and the thing is usable. No hidden setup steps, no "oh, you also need to…".
2. **Clean UI.** Readable, consistent, no dead controls, no raw error dumps, works at
   normal window sizes. Plain is fine; sloppy is not.
3. **Handles errors.** Bad input, missing files, network failures, empty states: each
   produces a clear, human message and never a stack trace or a corrupted data file.
4. **Has tests.** Automated tests cover the core loop and the error paths, and they
   run with one command.
5. **Has a README a stranger could follow.** Install, run, use, troubleshoot, where
   data lives, how to back it up, how to uninstall.
6. **Not embarrassing to show or sell.** The owner would demo it to a peer or list it
   for money without apologizing first.

If any of the six is false, the thing is not done. Say so plainly.

## Honesty about software

- Be honest about what software can and cannot do.
- Never promise outcomes the thing cannot deliver. This specifically includes money
  (returns, profits, "passive income"), predictions (markets, sports, weather beyond
  what a cited source provides), and health (diagnosis, treatment, outcomes).
- A tool may *organize*, *record*, *remind*, *compute from stated inputs*, and
  *surface* information. Describe it in those terms. Marketing copy, README text, and
  UI labels must all pass this test.
- When a user-facing claim is uncertain, say "estimate", "based on your entries", or
  "according to <source>". Never imply certainty the code does not have.

## Technology preferences

- **Prefer boring, durable tech.** Standard library first, then widely used, long-lived
  libraries. Avoid anything whose maintenance you would have to explain in a year.
- **Local-first.** Data lives on the owner's machine in a plain, inspectable format
  (SQLite, JSON, Markdown, CSV). The thing must work fully offline unless its entire
  purpose is to talk to a network service.
- **No required cloud bill.** Optional integrations may exist, but the default
  configuration costs $0 and needs no account.
- **Docker optional, never required.** Provide a Dockerfile if it helps; the one-command
  run must also work natively.
- Web UI by default (plain HTML/JS or a lightweight framework). Add a CLI where it
  genuinely helps. Desktop/mobile only if the spec demands it.
- Secrets never go in the repo. Config goes in a documented file or environment
  variable with sane defaults.

## The six judging criteria

Every idea, feature, and change is judged on these six. Score each 1–5 and write a
one-sentence rationale per score. No score without a rationale.

1. **Pain removed** — how much of the owner's recurring time or pain it removes.
2. **Open frequency** — how often the owner would actually open it (daily > weekly >
   monthly > "someday").
3. **Compounding** — whether it gets more valuable with use (accumulated data, habits,
   history, network) rather than being equally useful on day 1 and day 300.
4. **Sellability** — whether people like the owner would pay for or at least adopt it.
5. **Buildability** — can a shelf-ready v1 be built in days, not months.
6. **Delight** — would using it feel good, not merely functional.

Tie-breaker: criteria 1 and 2 outrank the rest. A thing nobody opens is worthless no
matter how clever.

## Repo layout

```
CLAUDE.md                 this file — studio rules
README.md                 how to use the studio
.claude/agents/           sub-agents (interviewer, scout, inventor, judge, architect,
                          builder, ux-reviewer, qa-checker, historian)
.claude/skills/           studio workflows (discover, build-winner, improve, pivot)
research/                 interview.md, landscape.md, and raw notes
ideas/                    candidates.md (25+ ideas), scores.md, finalists.md
decisions/                append-only log; NNNN-slug.md, one file per decision
spec/                     the architect's one-page spec for the winner
app/                      the winner itself (its own README, tests, run command)
```

## Working rules for every agent

- **Log every decision.** Any choice that closes off alternatives (which idea, which
  stack, which scope cut, which UI pattern) gets a file in `decisions/` via the
  historian. Format is in `decisions/README.md`.
- **Cite sources.** Research claims link to where they came from. No invented
  statistics, no invented competitor features, no invented user quotes.
- **Ask the owner only when it matters.** Questions go through the Phase 1 interview
  or when a real decision needs their input (picking the winner, scope cuts that change
  what the thing is, anything involving money or accounts). Everything else: make the
  routine call, log it, move on.
- **Scope is sacred.** The architect's spec defines v1. Builders do not add features
  the spec does not list. Wanted extras go in `ideas/backlog.md`.
- **Report honestly.** If tests fail, say so with output. If a step was skipped, say
  so. "Done" means shelf-ready per the six-point definition above.
- **Sign-offs are real.** `ux-reviewer` and `qa-checker` must each write an explicit
  pass/fail with findings before anything is called shelf-ready. A fail blocks.
- **Capture friction in the app.** The winner ships with a lightweight in-app feedback
  note (one text box, saved locally) so the owner records annoyances the moment they
  happen. The `improve` skill reads these.

## Phases at a glance

| Phase | Skill          | Agents                              | Output                                   |
|-------|----------------|-------------------------------------|------------------------------------------|
| 0     | (this scaffold)| —                                   | CLAUDE.md, agents, skills, folders       |
| 1     | discover       | interviewer, historian              | research/interview.md                    |
| 2     | discover       | scout                               | research/landscape.md                    |
| 3     | discover       | inventor, judge, historian          | ideas/*.md, 3 finalists, owner's pick    |
| 4     | build-winner   | architect, builder, ux-reviewer, qa-checker, historian | spec/, app/, walkthrough |
| 5     | improve        | builder, ux-reviewer, qa-checker, historian | one shipped improvement per run    |
| —     | pivot          | judge, historian                    | re-judged finalists if the winner stalls |
