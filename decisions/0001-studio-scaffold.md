# 0001 — Scaffold the studio as agents + skills + plain folders

**Date:** 2026-10-31
**Phase:** 0
**Decided by:** historian (per owner's brief)
**Status:** accepted

## Context
The owner asked for infrastructure that discovers, scores, builds, and improves a single
product, with them as the only customer. The structure had to be followable by a stranger
and durable across many Claude Code sessions.

## Options considered
- Single long prompt file — simplest, but phases blur and nothing is re-runnable.
- Agents + skills + plain Markdown folders — each phase re-runnable, every role has a
  written brief, all state is inspectable files in git.
- Custom orchestration scripts — more machinery than the problem needs right now.

## Decision
Agents in `.claude/agents/`, workflows in `.claude/skills/`, state in `research/`,
`ideas/`, `decisions/`, `spec/`, `app/`. Decision log is append-only Markdown.

## Why
Boring, durable, local-first, zero cloud bill — the same rules the studio applies to the
things it builds. Git is the database.

## Consequences
Easy to re-run any phase. Agents have no shared memory beyond the files, so every agent
brief says exactly which files it reads and writes. Revisit if the number of decisions
grows past what a flat folder can carry (~100).
