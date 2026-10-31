# forge-zero

A one-customer product studio that runs inside Claude Code.

forge-zero does not start with an app idea. It starts with an interview, researches the
landscape, generates and scores dozens of candidates, narrows to three finalists, builds
the one the owner picks to a shelf-ready standard, and then keeps improving it.

## How to use it

Open this repo in Claude Code and run the skills in order:

| Command          | What it does                                                             |
|------------------|--------------------------------------------------------------------------|
| `/discover`      | Phases 1–3: interview → landscape research → 25+ ideas → 3 finalists    |
| `/build-winner`  | Phase 4: spec → build → UX and QA sign-off → walkthrough                  |
| `/improve`       | Phase 5: asks what annoyed you, proposes three changes, ships the one you pick |
| `/pivot`         | Re-runs judging when the winner has stalled                              |

Rules the studio follows are in `CLAUDE.md`. Every decision and its reasoning is logged
in `decisions/`.

## Layout

- `research/` — interview answers and landscape research
- `ideas/` — candidate ideas, scores, finalists, backlog
- `decisions/` — append-only decision log
- `spec/` — the one-page spec for the winner
- `app/` — the winner (has its own README and one-command run)
