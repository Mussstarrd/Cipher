# ADR-011: The Collectors — what the lab let out

- **Status:** Accepted (owner directive, 2026-09-12)
- **Depends on:** ADR-003 (the premise, and the one class it deliberately held in reserve),
  ADR-004 (the setting), ADR-008 (the implant is decrypted progressively)
- **Spends:** ADR-003's red eyes. This is what they were being saved for.

## The owner's words

> "we need to have boss mobs too. remember our first renderings that were favorited were of a dud
> that looked like the butcher from diablo all huge belly and like 3 times the size of a person and
> shit. DC area man, they let out some super genetic freak killer fuckers that are rounding up and
> or eliminating problem people like me"

## Context

**ADR-003 wrote the door and this walks through it.** It names the signed (ordinary chipped
citizens), hacked service humanoids, cargo drones — and then says one more thing, in a single line
that has been sitting unspent since the premise was written:

> *Red eyes belong only to lab releases and we spend them almost never.*

There has never been a lab release in the game. There has also never been a boss. Those are the same
gap, and the owner has closed it with the piece the fiction was missing: **the government did not
only administer the implant. It made something for the people who refused it.**

That is the sharpest idea anyone has had about this world since the truck. The whole premise rests on
a protagonist who **opted out** — unregistered, non-voting, refused the chip and the money on
principle (ADR-003). Until now that was backstory. A thing purpose-built to collect people exactly
like him makes it the reason he is being hunted personally, and it converts "I am a survivor" into
"I am on a list."

Northern Virginia is forty miles from the District. The lake community is not remote; it is
**commutable**, which is why it filled with federal retirees and contractors in the first place, and
which is exactly why something released from a facility inside the Beltway reaches it.

## Decision

### What they are

**Collectors.** Not infected, not chipped, not a machine — engineered, and the only enemy in the game
that was *built on purpose to do this*. The community's word for them is deliberately bureaucratic
and that is the horror: an AI that administered stimulus payments does not send a monster after the
people who did not sign. It sends a **collections process**.

Physically they are the owner's own reference, which the very first concept pass landed and he
picked out: enormous, distended, roughly **three times a person's mass** and a head taller than the
tallest civilian. Heavy rather than fast. **Red eyes**, per ADR-003, and this is the almost-never
that clause was reserving.

### What makes them a boss, mechanically

Three things, and none of them is "more health with a bigger model".

**1. They come for the player, not the truck.** Every other enemy in the game walks a flow field to
the goal and fights what gets in the way. A Collector ignores the objective, ignores emplacements it
is not obstructed by, and walks at the hero. **This is the first enemy the player cannot solve by
building.** A fortification is irrelevant to something that was sent for *you* — the player has to
leave the position they spent the whole mission preparing, which is the most interesting thing a
boss can ask of a tower-defence player.

**2. They are HARDENED, so the decrypt barely works.** ADR-008's model is one pulse = a death
sentence in forty seconds, and hits stack. A Collector's implant — and it has one, it is lab
hardware, which is why the arsenal is not simply useless — resists: **a hit adds a fraction of the
drain it would add to a citizen.** The passive kill never arrives in a useful time, so every point of
integrity has to be taken off by direct fire. That inverts the tactic the player has just learned and
makes the fight about *sustained aim under pressure* rather than about spreading infection.

**3. They do not stumble until very late.** ADR-008's frailty band makes a body come apart over the
last third of its bar. A Collector holds its speed and its swing almost to zero, so there is no
comfortable stretch at the end where it stops being dangerous.

### What they are NOT

- **Not common.** One or two in a position, late, announced. The moment a boss becomes routine it is
  just an enemy with a bigger number.
- **Not a damage sponge.** If the fight is "hold the trigger for ninety seconds" the design has
  failed. It should be won by repositioning, by spending the EMP well, and by deciding what to give
  up while it walks through the middle of your emplacements.
- **Not chipped citizens, and this matters morally.** Everything else in this game is a neighbour who
  signed a form. A Collector is the only enemy in PROJECT EXODUS that the player is unambiguously
  right to destroy, and the fiction should let that land rather than flattening the difference.

### Open, and deliberately not guessed at

- **Does the EMP work properly on them?** Recommendation: yes, and it is their counter — an
  electromagnetic pulse is not malware and does not care how well the payload is encrypted. That
  gives the airstrike a job it does not currently have and a reason to save it.
- **Do they appear in mission one?** Recommendation: no. Mission one is the tutorial the stranger
  test is measuring. First appearance should be authored, not random.
- **Do the turrets aggro them?** Recommendation: a Collector damaged by an emplacement should turn on
  it, or a player will simply hide behind guns and the "you cannot build your way out" point is lost.

## Consequences

- **`Archetype` gains a fourth member**, which the outside review's lesson applies to directly: when
  a restriction or a category is added, every query that switches on it must be re-read. `CountAlive`,
  the spawn director's caps and pity timers, the archetype-break counters and the loot table all
  switch on archetype today.
- **The economy will move.** A Collector is worth far more than a citizen and takes far longer; if it
  pays per-kill like anything else it is worth almost nothing per second of effort.
- **Mission 9 is the county laboratory** (`campaign-act1.md`), the act break and the only mission
  outside the gates. It is now obvious what is in it, and that mission should be authored *after*
  Collectors exist rather than before.
- **The crowd renderer has never drawn anything that is not roughly person-sized.** Three times the
  mass is a different silhouette, a different LOD budget and a different contact radius.
