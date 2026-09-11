# ADR-009: The truck is the family, and the scan is why you cannot stay

- **Status:** Accepted (owner directive, 2026-09-11)
- **Depends on:** ADR-003 (the premise), ADR-004 (the setting), ADR-005 (the scan cycle and pack-up)
- **Owner's words, verbatim:**

  > "I'm thinking this takes place some time after the initial Exodus so the Lake of the Woods has
  > had some time to be fortified. We quickly realized that any source of energy that was not
  > organically created attracted these things but we're at a point that they're able to hyper scan
  > once a set period of time where they can detect even the energy we are giving off organically as
  > humans that's where these new missions start couple days in between each other. the reason for
  > falling back it's because the truck is a four-door and the back seat has my 2-year-old and
  > 9-year-old in it. My wife is in there as well maybe in between rounds she comes out with my
  > 9-year-old to do repairs."

## Context

Three things in this game were mechanically finished and narratively unexplained, and the owner has
just explained all three with one idea.

1. **Why is there a scan cycle at all?** ADR-005 built the whole loop on it — the player calls their
   own last wave, then packs up against a clock — and justified it with a hand-wave about HALCYON
   sweeping for the unchipped periodically. Serviceable. Not a reason anyone would *feel*.
2. **Why do they go for the turrets?** The owner asked for it on 2026-09-11 ("if they run by a tower
   they should try to attack the tower to clear their path") and we built it as pure aggression.
   It worked and it meant nothing.
3. **Why fall back at all?** This is the one that mattered and the one nobody had answered. A
   fighting retreat where the player is a lone veteran holding positions is a sequence of levels. If
   he can fight, he can hold. The retreat was a structure with no motive under it.

## Decision

### The timeline: this is *after* the Exodus, and the community is dug in

Missions 1–12 are not the fall of the community. The fall already happened; this is months later.
Lake of the Woods was fortified because it is *defensible* — a handful of road gates, one lake
flank, treeline behind every lot (ADR-004). People made it work. The walls in the game are
**built**, not improvised in the moment, and they should read that way.

**Missions are a couple of days apart**, not a continuous night. That is why damage persists between
positions (already in `docs/design/campaign-act1.md`) and why the player arrives at each position
with what survived the last one, plus whatever the days in between bought.

### The escalation: they hunt energy, and they have learned to hear us

The survivable rule, the one the community was built on: **anything giving off energy that was not
organically created draws them.** Generators, radios, powered tools, a turret's emitter. You can
live indefinitely if you stay quiet. That is what "fortified" bought.

What has changed — and what starts the campaign — is that HALCYON now runs a **hyper-scan** on a
fixed cycle, and the hyper-scan reads *organic* signature too. The bioelectric noise of a living
unchipped human. There is no going quiet enough any more. There is only the gap between sweeps.

**This is the scan cycle, and it is now the same object seen from two sides:**

- The cycle clock the player watches is the time until the next hyper-scan.
- Every emplacement they run is a beacon *inside* that window, pulling the crowd toward it.

So the two mechanics that already exist stop being arbitrary: turret aggression is turret *noise*,
and the pack-up window is the last quiet stretch before something looks directly at you.

### The reason to fall back: the truck is a four-door and the back seat is full

**The player's family is in the truck.** Wife in the front, a two-year-old and a nine-year-old in
the back. That is the entire motive for the retreat, and it reframes three systems at no code cost:

| Was | Is |
|---|---|
| "the vault" — abstract objective to protect | **the truck** — his wife and both children are in it |
| pack-up = recovering kit for the next level | **loading what will fit around two car seats** |
| kit left behind = an economic loss | **the price of leaving while everyone is still breathing** |

It also answers the unanswerable question. He does not fall back because he is losing. He falls back
because a four-door truck can only cover so much road in the gap between hyper-scans, and every
position he holds is time spent not moving. **Holding is not the goal; holding is what he spends to
buy the next stretch of road.** That is exactly the budget ADR-005 built and never gave a reason for.

### Between waves, the wife and the nine-year-old get out and repair

The owner's own image, and the best one in the message. In the setup window, two people leave the
truck and work on the emplacements. The nine-year-old helps. They are in the open while they do it.

This is not a new system — `ActorKind.Crew` and the `KeepCrewAlive` objective were built on
2026-09-11 and have never been used by a shipped mission. They are exactly this. What it adds is
**tension in the quiet part of the loop**, which is currently the part where nothing is at stake:
the setup window is where the player is safest, and it is about to be where the two people he cannot
replace are standing outside the vehicle.

It also makes the between-wave decision real. Repairs are faster with them out. They are only out
because he decided they should be.

## What this retires

- **The word "vault"** in everything the player reads. It stays in code identifiers (`VaultHp`,
  `ProtectVault`, the scenario JSON key) for exactly the reason ADR-006 kept the `Cipher.*`
  namespaces: renaming them is a large cosmetic diff with real risk and zero player benefit.
  **Player-facing strings change; identifiers do not.**
- **"Everything you have not carried out yet"** as the truck's subtitle. Wrong now, and it was
  always the weaker reading.
- Any framing of the retreat as a losing streak. He is not being pushed back. He is leaving on a
  schedule, and the schedule is set by something that scans.

## The one thing this implies that we have NOT built

**Energy as a signature the player manages.** If artificial energy draws them, then the player's own
emplacements should measurably pull the crowd — more guns running means a hotter position — and
going quiet should be a real, available choice with a real cost.

That is a genuinely new system and the live roadmap freezes the systems layer, so it is **not being
built on this commit**. It is recorded here because it is the most promising lever the fiction has
handed us: it turns "how many turrets can I afford" into "how much noise can I afford", which is a
better question and one that already has the crowd behaviour to support it. Surfaced to the owner
rather than buried. See the roadmap for where it lands.

## A note on the family, and why they are fictionalised

ADR-004 already set this precedent for the place: the community is real, real people live there, so
the shipped map is "Wilderness Lake" and **the owner's street address never enters this repo, a
concept prompt, or any external service**. His own house is in the game because he gave it to us.

The same rule applies to the four people in the truck. They are in the game because he put them
there. They are written as **roles, not identities** — no names, no likenesses, no details beyond
what the fiction needs — and nothing about them goes to an external service either. If the owner
later wants them named, that is his call to make explicitly, not a default we drift into.

## Consequences worth watching

- **The truck's HP bar is now a family.** That is a large tonal change to a number that currently
  reads like a health bar on a crate. It needs to look like what it is, and "0/25" is not it.
- **Failing the truck should not be a normal outcome.** ADR-005 made `Extracted` neither a win nor a
  loss. Losing the truck must stay rare and must land hard; if it becomes routine, the motive is
  cheapened and the player stops reading it as anything but a fail state.
- **Mission 12 is the owner's own house with nothing behind it** (`campaign-act1.md`). With this ADR
  in place, that mission's meaning changes completely, and mission 4 — one of the two that cannot be
  won by design — needs re-reading against it before it is authored.
- **Do not let the children become a mechanic.** The two-year-old never leaves the truck and never
  becomes a timer, an escort or a fetch objective. The nine-year-old helps with repairs because the
  owner said so; that is the whole of it.
