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

## 3. v1 cut-list (OWNER approval of ENG draft) — before any metagame code

Draft position: v1 = Bloom-only combat (no rival-faction squad AI — it's a full squad-shooter discipline), one hero kit (Enforcer), city map as mission-select + passive income only, lieutenants as named crew buffs (permadeath flavor, no roguelite layer), co-op cut from v1 and architected-for as post-launch expansion, faction combat as paid-DLC seam. Genre statement edited to match.

## 4. Pure C# sim vs DOTS/Burst (ENG, owner informed) — reconcile ADR-001's claim

ADR-001 justified Unity partly via DOTS, but the built architecture is a pure C# engine-free core (deliberately — it's what makes headless testing and the preview covenant work). Plan: keep the pure core, benchmark on the actual phone via adb this month; if the phone soak fails budgets, graduate hot subsystems behind the existing interfaces. ADR-001 gets amended either way.

## 5. Preview covenant, precise wording (ENG) — with Milestone 2

"The preview never lies" is impossible under eat-through/corpse-mounds/Wreckers. New covenant: *truthful for current field state; every dynamic re-route gets a loud telegraph — "you will always see it coming."* M2 exit criteria gain a degenerate-strategy check: a full-seal turtle must lose legibly by wave 5.

## 6. Upgrade-tree UX on gamepad (ENG experiment, month 2) — with kill criterion

Radial per-emplacement menus, D-pad path picks, hold-to-confirm T5s; tested live at 8+ emplacements. Kill criterion: owner upgrades 3 emplacements mid-wave without dying or misclicking. Pre-authorized fallback: cut crosspaths to strict path-commit.

## 7. Playtester recruitment (OWNER) — before day 30

The "one more wave" gate specifies five players; the studio has one. Need 3–5 friends with controllers lined up for the first graybox build, or the gate is decoration.

## 8. Design-trap gates for every future layer (ENG process rule — adopted now)

Each major design layer (trees, camera, seal economy, empire loop) ships with a written exit criterion and pre-committed fallback *before* the layer is built — the doc-03 trap format applied to design, so the plan can kill a boring feature as reliably as it catches a slow frame.
