# ADR-006 — The game is called PROJECT EXODUS

**Status:** Accepted (owner's call) · **Date:** 2026-09-10
**Closes:** the open title question raised in ADR-003, ADR-004 and `campaign-act1.md`.
**Retires:** *CIPHER: DEAD TURF*.

## Context

The title had been flagged as wrong in three consecutive documents and for the same reason each time. *Dead
Turf* was written for a kingpin holding a city block. ADR-003 retired the kingpin. ADR-004 moved the ground
from a block to a gated lake community. By the time the campaign became a fighting retreat ending at the
protagonist's own front door, the word "turf" was describing a game we are no longer making.

*CIPHER* had already been struck: it was the protagonist's street alias in the old pitch, and the owner's
objection to it was flat.

> My street name is not Cypher for the record

The owner supplied the replacement:

> Project Exodus

## Decision

**The game is PROJECT EXODUS.** Adopted as given, no qualifier, no subtitle.

### Why it is right

- **It names what the player actually does.** Twelve missions of choosing when to leave and what to carry.
  The verb of this game is departure, and no other candidate title had a verb at all.
- **It survives the premise.** Nothing in it depends on crime, a city, or a protagonist archetype, which is
  what killed the last two titles.
- **It lands on the finale.** Mission 11 is an evacuation by water and mission 12 is the man who stayed. A
  title meaning "the departure" over an ending about the one who did not depart is the good kind of irony,
  and it is earned rather than decorative.
- **"Project" reads institutional**, which is the register of the whole fiction: a stimulus programme, a
  municipal optimiser, an enrolment. The horror in this game is administrative.

### Whose project is it? Leave it ambiguous, on purpose

Three readings are live and the game should support all three rather than settle it:

| Reading | Whose word it is |
|---|---|
| The community's evacuation plan, printed and pinned to the clubhouse wall | the survivors' |
| The name on the enrolment programme's own paperwork | the government's |
| HALCYON's internal designation for what it is doing to the unchipped | the AI's |

The third is the coldest and should be the last one revealed. A player who learns in act two that the phrase
on the clubhouse wall was copied from a document they were never meant to read gets the premise re-landed for
free.

## Consequences

| Area | Effect |
|---|---|
| **Docs** | `docs/02-FLAGSHIP-PITCH.md` was already scheduled for a rewrite under ADR-003. It now also gets the name. All other docs updated in this commit. |
| **Code namespaces** | **Unchanged.** `Cipher.Sim`, `Cipher.Game`, the repo name and the branch stay as they are. Renaming namespaces, asmdefs, the local package id and every `.meta` would be a large, risky, entirely cosmetic diff. Revisit only if the project goes public. |
| **Build output** | `CipherDeadTurf.exe` becomes `ProjectExodus.exe` in `CiBuild`. Cheap and player-visible, so it changes now. |
| **Store/marketing** | Nothing exists yet, so there is nothing to migrate. |

## Reversal conditions

Reverse only if a trademark search finds a conflict in games. "Exodus" is a common word with prior art in
other media; *Project Exodus* as a game title should be checked before any public announcement, and that
check is not urgent at this stage.
