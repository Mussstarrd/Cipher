# Decision log

Append-only. One file per decision, named `NNNN-short-slug.md`, numbered in order.
Never edit a past decision; if it is reversed, write a new one that references it.

Template:

```markdown
# NNNN — Title

**Date:** YYYY-MM-DD
**Phase:** 0–5 or pivot
**Decided by:** owner | agent-name
**Status:** accepted | superseded by NNNN

## Context
What question had to be answered and why now.

## Options considered
- Option A — one line on its pros and cons
- Option B — ...

## Decision
What was chosen.

## Why
The reasoning, including which of the six criteria drove it.

## Consequences
What this makes easier, what it makes harder, what to revisit and when.
```
