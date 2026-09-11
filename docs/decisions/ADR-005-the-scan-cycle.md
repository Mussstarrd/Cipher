# ADR-005 — The scan cycle: the player decides when a position is done

**Status:** Accepted (owner's system, ENG built it) · **Date:** 2026-09-10
**Builds on:** [ADR-003](ADR-003-premise-the-cascade.md) (the premise), [ADR-004](ADR-004-setting-the-lake.md) (the retreat)
**Supersedes:** the kit-recovery multiplier table in `docs/design/campaign-act1.md` §2, which was my
placeholder for exactly this and was worse.

## Context

The owner's words:

> The "skynet Ai devil" only has computational power or orbital lineup or something enough to be able to do
> a full 6G or 7G scan for unchipped targets once a day or whatever so maybe once a certain point is hit you
> can decide okay I want this to be my last wave for this level and I'm able to gain my experience and level
> up send resources to roll over or whatever and once that last chosen wave is cleared I have a set timer to
> pack up and Retreat back to the next level. I will have a set amount of time to reinforce based on whatever
> this computational power or orbital cycle thing dictates. seems like maybe more than one day in between
> Cycles would be better something to be able to give a realistic opportunity in the storyline to stealth up
> and trap up and fortify and rest and all of that

This answers three questions the design had been quietly dodging.

| Question we had no answer for | The scan cycle answers it |
|---|---|
| Why do enemies arrive in *waves* at all? | A scan is an event. One sweep, one converge. Waves are the echelons arriving, nearest first. |
| Why is the player ever allowed to rest and build? | Because between scans HALCYON does not know where they are. It is working from last known position. |
| Why does the difficulty rise? | The interval shortens as HALCYON frees up compute. The squeeze **is** the AI getting smarter, which was the owner's original premise. |

A tower-defence game whose wave timer is diegetic is rare, and getting it for free out of the fiction is
worth more than any balance change we could make.

## Decision

### 1. HALCYON cannot see continuously

A wide-area sweep for unchipped biosignatures needs an orbital window and compute it does not have spare.
It runs on a **cycle**, and between cycles the signed operate on stale information.

**Cycle length is per-position and shortens across the act.** Early on the player gets days. By the last
missions they get hours. Same number does the difficulty curve and the dread, and the player can watch it
fall on the mission select screen.

### 2. The player calls their own last wave

Once they have held a minimum number of waves, the player may declare **"this is my last wave here."**

- Clearing a declared last wave opens the **pack-up window** instead of another setup phase.
- Declaring **during setup**, before they can see what is in the wave, pays a **commitment bonus**. Declaring
  mid-wave once they have counted the horde does not.
- This is the entire risk curve of a mission and the player owns both ends of it. Take another wave for the
  cash and the experience, or leave with more of your kit and more time to dig in at the next line.

### 3. The pack-up window is how kit recovery actually works

My previous draft awarded a flat 75/50/25% of your build value based on how the mission ended. That was a
table pretending to be a decision. The real version:

- When the window opens you have a fixed number of seconds before the next scan.
- **You physically unbolt each emplacement**, and each one costs seconds off that window.
- What you get back is what you carried, not a percentage. What you leave stays bolted to the ground in a
  place you are never coming back to.
- You may **pull out early** and bank the unused time as prep at the next position.

So a player who built twelve turrets and called it late has to choose which four come with them. That is a
real decision made under a real clock, and it is the same decision the fiction is about.

### 4. Time is one budget, spent in three places

The cycle is a single clock. Fighting spends it, packing spends it, and **whatever is left is prep time at
the next position** to fortify, trap, rest and re-arm. Every extra wave taken is fortification not built.

This is what stops the obvious exploit of turtling at the final position from mission one: everything you own
is bolted to ground you are standing on, and the money and the time to build the last line come from having
held the earlier ones well.

### 5. The last mission has the call greyed out

Mission 12 is the only position in Act One with nothing behind it. The extract call is present, disabled, and
the reason it gives is **"there is nowhere to fall back to."**

Eleven missions train the player that they always get to decide when to leave. The twelfth takes it away
through a UI element they have used every mission. No cutscene does that as cheaply or as well.

## What shipped with this ADR

Built and tested the same day, in `game/Assets/Scripts/Match/`:

- `ScanCycleConfig` — cycle seconds, minimum waves before the call, pack-up window, per-emplacement unbolt
  cost, commitment bonus. Per-position tuning; the campaign layer shortens it mission by mission.
- `MatchPhase.Extraction` and `MatchPhase.Extracted`. **Extracted is neither a win nor a loss**, which is the
  whole point: leaving on schedule is the correct play, not a failure.
- `MatchState.DeclareLastWave()` with a typed `DeclareResult` (`Ok`, `TooEarly`, `AlreadyDeclared`,
  `NotFighting`, `NowhereToGo`), `TrySalvage()` with `SalvageResult`, `PullOutNow()`, `PrepSecondsRemaining`.
- 15 EditMode tests in `ScanCycleTests.cs`. 61 game tests green headless on 2026-09-11.
- Bound to **L** on the keyboard and **D-pad down** on the controller, with HUD state and refusal reasons.

## Consequences

| Area | Effect |
|---|---|
| **Core loop** | Missions stop having a fixed wave count. The wave table becomes an escalating supply the player draws from until they call it. |
| **Economy** | Salvage is now a per-emplacement transaction rather than an end-of-mission multiplier, so build decisions carry across missions. |
| **Campaign** | The interlude between missions becomes a real phase with a time budget. Fortify, trap, scavenge, rest. |
| **Difficulty tuning** | Largely collapses into one number per mission: cycle length. |
| **Scope risk** | The interlude invites a stealth/scavenging mode. **Not in Act One.** Flagged in the campaign doc as Act Two. |
| **`campaign-act1.md` §2** | Rewritten; the multiplier table is gone. |

## Open questions for the owner

1. **How long is a cycle, in fiction?** I have written it as roughly three days at the start of the act,
   falling to hours by mission 12. Three days buys the stealth-and-rest breathing room you asked for without
   making the community's collapse feel slow.
2. **Should the player see the countdown to the next scan the whole time**, or only once it is close? Always
   visible is more tense and less mysterious. My pick is always visible, because dread beats surprise here.
3. **Does experience carry the same way cash does?** You mentioned levelling and rolling resources over. Cash
   and salvage carry today. Say the word and experience/perks carry too, which turns the retreat into a
   progression curve rather than only an attrition one.
