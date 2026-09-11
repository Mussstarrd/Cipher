# Open Decisions Register

*From the first adversarial design review (2026-09-10). Each becomes an ADR when decided. Items marked **OWNER** need Jeff's call; items marked **ENG** are Claude's to draft, with owner veto. Ordered by urgency.*

## 1. Camera model — DECIDED 2026-09-10 → ADR-002 (Accepted)

The pitch promises overview mazing AND third-person combat; no doc says what the camera does, and the current graybox uses a third thing (fixed overhead). Options:
- **(a) Tactical camera, hero as controlled unit** — cheapest, matches graybox, weakens gunfeel fantasy.
- **(b) Full third-person + build-mode drone cam** — the Orcs Must Die model; requires smaller, ground-legible maps.
- **(c) Zoom hybrid** (over-the-shoulder ⇄ overhead on a shoulder-button hold).
ENG recommendation to be drafted as ADR-002 with a graybox test of (b) vs (c); M3's exit test must score the mode *transition*, not just the gun.

**Decided:** option (b). Combat is chase third-person; overhead is look-only now and the build-mode camera in M2. Owner delegated the call to ENG; reversal conditions are in ADR-002.

## 2. What is Android FOR (OWNER) — one paragraph, decides half the budgets

Premium + controller-required on Google Play is a tiny market; the same ADR that made Android the perf floor admits PC is the revenue platform. If Android's honest role is "owner plays nightly builds on the couch" (legitimate!), it becomes a dev/test target: PC becomes the design floor, and Android density is whatever falls out of the soak test. Engine choice is unaffected either way.

## 3. v1 cut-list — ~~RETIRED 2026-09-12~~, rewritten against the ADRs

**The old entry was retired kingpin fiction end to end** and an outside review flagged it: it
proposed "Bloom-only combat", "rival-faction squad AI", "one hero kit (Enforcer)", "city map as
mission-select" and "lieutenants as named crew buffs". ADR-003 killed the syndicate framing,
ADR-004 replaced the city with a lake community, and ADR-006 renamed the game. Left standing it
poisoned the one file `CLAUDE.md` calls the live register.

**The live cut-list question is now this:** v1 is Act One — twelve missions of one authored
community, contracting each time (`docs/design/campaign-act1.md`). What is *in* is the retreat
loop (ADR-005), signal weapons (ADR-008), gear + skills, and couch co-op **architected for but cut**.
What is genuinely undecided:

- **Energy as a signature the player manages.** ADR-009 established that artificial energy draws
  them; running more guns should measurably heat up a position and going quiet should be a real
  choice with a real cost. It is the strongest lever the fiction has handed us and it is a new
  system. **Surfaced to the owner, not built.**
- **Whether missions 4 and 12 need a code seam.** `campaign-act1.md` says both are unwinnable by
  design and that the score screen "does not say FAILED — it says how long, and how many".
  `MatchPhase` has `Won`, `Lost` and `Extracted` and nothing else. Decide before mission 4 is
  authored, not after.
- **What the Grinder family is for**, now that emplacements are noise rather than just damage.

## 4. Pure C# sim vs DOTS/Burst (ENG, owner informed) — reconcile ADR-001's claim

ADR-001 justified Unity partly via DOTS, but the built architecture is a pure C# engine-free core (deliberately — it's what makes headless testing and the preview covenant work). Plan: keep the pure core, benchmark on the actual phone via adb this month; if the phone soak fails budgets, graduate hot subsystems behind the existing interfaces. ADR-001 gets amended either way.

## 5. Preview covenant, precise wording (ENG) — with Milestone 2

"The preview never lies" is impossible under eat-through/corpse-mounds/Wreckers. New covenant: *truthful for current field state; every dynamic re-route gets a loud telegraph — "you will always see it coming."* M2 exit criteria gain a degenerate-strategy check: a full-seal turtle must lose legibly by wave 5.

## 6. Upgrade-tree UX on gamepad (ENG experiment, month 2) — with kill criterion

Radial per-emplacement menus, D-pad path picks, hold-to-confirm T5s; tested live at 8+ emplacements. Kill criterion: owner upgrades 3 emplacements mid-wave without dying or misclicking. Pre-authorized fallback: cut crosspaths to strict path-commit.

## 7. Playtester recruitment (OWNER) — **NOW THE ONLY BLOCKER ON THE CURRENT MILESTONE**

The "one more wave" gate specifies five players; the studio has one. Need 3–5 friends with
controllers lined up, or the gate is decoration.

**Escalated 2026-09-12.** The MISSION ONE, UNGUIDED milestone has four acceptance criteria and
three of them are now met in code. The fourth — *can a stranger say what killed them* — cannot be
answered from the repository by anybody, and an outside review named one specific question worth
instrumenting: **does a player understand that someone who keeps walking after being shot is already
dead?** That is the riskiest unknown in the build. Two people who have never seen it, a controller,
and no explanation from the owner.

## 8. Design-trap gates for every future layer (ENG process rule — adopted now)

Each major design layer (trees, camera, seal economy, empire loop) ships with a written exit criterion and pre-committed fallback *before* the layer is built — the doc-03 trap format applied to design, so the plan can kill a boring feature as reliably as it catches a slow frame.
