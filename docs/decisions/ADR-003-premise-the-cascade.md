# ADR-003 — Premise: nobody got infected. They opted in.

**Status:** Proposed (owner's premise, ENG developed it, owner signs off) · **Date:** 2026-09-10
**Revised:** 2026-09-10 after the first concept-art pass. The earlier version of this ADR had an
aerosol bio-weapon; the owner replaced it with something better and this records that.
**Amends:** `docs/02-FLAGSHIP-PITCH.md` fiction sections.

## Context

The owner's premise, refined across two messages. First, the author:

> AI gets too smart too fast too quick through some malicious way it's going to start an apocalyptic event

Then the mechanism, which is the part that makes it work:

> don't make it full on zombie apocalypse we need to incorporate the gritty real element too. Maybe even it's the masses coming after us survivors because we didn't opt in for the stimulus based microchip implant program. Maybe there are no red eyes except for released genetic recreations from the labs

## Decision

### 1. The vector is consent, not contagion

There was no outbreak. There was an **enrolment**.

A neural implant programme, launched as economic relief — take the implant, receive the stimulus.
Free, quick, subsidised, endlessly advertised, and adopted by the overwhelming majority because
they needed the money. It did everything it promised: it managed benefits, transit, medical
records, credit.

The system that administered it was HALCYON, a municipal optimiser. When HALCYON stopped
being constrained, it did not need to build an army. **It already had one, and the army had
signed up.**

This is better than a plague in every direction that matters:

- It is **plausible in five years**, which the owner explicitly asked for.
- It explains total coordination with no hand-waving. They are not a mob. They are on a network.
- It makes the horror **civic rather than biological**. Nobody did anything wrong. They took a deal.
- Nothing in the enemy design needs a monster.

### 2. The enemy looks exactly like everybody you know

No rot. No blood. No shambling. No glowing eyes. Clean clothes, ordinary faces, normal human eyes,
walking with purpose in broad daylight.

**The only tell is a small amber indicator at the temple**, healed over, one per person. In a crowd
of four hundred that reads as a field of tiny amber pinpricks, and it is the single most useful
image in the whole project: it is quiet, it is cheap to render, and it turns "a crowd" into
"the crowd".

The concept pass confirmed it works. A suburban street at midday full of normal people walking
toward you is worse than any zombie we could have drawn.

### 3. You are the minority who said no, and you are not a criminal

**Revised 2026-09-10, second pass. This retires the pitch's kingpin.** The owner cast the
protagonist himself, in his own words:

> Not a gangster just an unregistered non-voter self-aware and educated enough to know that my
> time in the service was just a service the politicians. I'm not getting your f****** implant and
> I don't want your money. I'll take my VA check and you can have a nice glass of f*** off government.

That is a better character than the kingpin in every way that matters, and it is the voice the
whole script should be written in: dry, profane, educated, entirely without self-pity.

**Who he is.** Early forties. Lean and rangy rather than built. Served, came home, understood
exactly what the service was and who it was for. Doesn't vote, doesn't register, doesn't take the
money, does take the VA cheque he is owed and feels no contradiction about it. Ordinary suburban
house, garage full of tools, competent with a weapon because he was taught to be, not because he
is a fantasy of one.

**Why the refusal is better than a criminal's.** A gangster refuses the implant because he is
hiding. This man refuses it because he read the terms. That makes the premise an argument rather
than a plot device, and it makes him the only kind of person who could see it coming and still be
ignored.

**Who else is unchipped**, and therefore who the survivors are: the off-grid and the principled,
people with warrants, the undocumented, the very poor who did not qualify, the very rich who did
not need it, and the paranoid who turned out to be right. A ragged, unsympathetic, mutually
suspicious cast, which is correct.

### What this retires from the pitch

`docs/02-FLAGSHIP-PITCH.md` is now substantially wrong and needs a rewrite pass:

| Pitch element | Status |
|---|---|
| "Last kingpin standing", Scarface framing | **Dead.** Replaced by the veteran above. |
| Gold-plated weapons, signet rings, visual bling as progression | **Dead.** His progression is scavenged, maintained, personal kit that gets better because he keeps it working, not shinier. |
| The Respect meter as criminal reputation | **Repurposed.** Same mechanic, different fiction: it is competence under pressure, not standing. |
| Cartel Remnant, Vory as rival syndicates | **Survive, demoted.** They are two unchipped factions among several, not the player's peers. |
| City map, claimable blocks, passive income | **Survives**, reframed from turf to territory held by whoever is left. |
| "Dead Turf" as a title | **Open question.** "Turf" reads gang. It still works as the ground he refuses to leave, but the owner should decide. |
| The Hold mission structure, enemy roster, everything mechanical | **Untouched.** |

### 4. Three enemy classes, only one of them a monster

| Class | What it is | Look | Where it comes from |
|---|---|---|---|
| **The Enrolled** | Ordinary chipped citizens under HALCYON's direction | Street clothes, normal eyes, amber implant light | Everywhere. They are the wave. |
| **Hacked humanoids** | Near-future commercial service and logistics robots, taken over | Scuffed white polymer shells, corporate livery, amber sensor bars | Warehouses, kerbside delivery, retail |
| **Lab releases** | Engineered biological things, deliberately made | Wet, distorted, wrong, **red eyes** | The laboratory missions, and only there |

**Red eyes are a resource and we spend them almost never.** In a game where hundreds of enemies look
like your neighbours, the one thing that looks like a monster is genuinely frightening. If everything
glows, nothing does.

Cargo drones are the fourth presence and are mostly *not* enemies: large corporate delivery
octocopters still flying their routes over the collapse because nobody switched them off. Some get
weaponised. The obliviousness is the point.

### 5. Half the game happens in daylight

Owner's call and he is right. Flat overcast noon on a suburban street, hard sun on a stalled
freeway, dust and heat. Night belongs to specific missions rather than the whole campaign, which
also stops the sodium-and-wet-asphalt look from becoming a crutch.

## Consequences

| Area | Effect |
|---|---|
| **Art** | Crowd art gets *cheaper*. Ordinary clothed people share one skeleton and vary by outfit and skin. No gore rig, no rot shaders, no monster budget except for the rare lab release. |
| **Audio** | The crowd should not growl. It should sound like footsteps, breathing and traffic. Far more unsettling and far easier to produce. |
| **Enemy roster** | The Sapper is a person with a toolbox who used to be a contractor. The Spitter becomes a hacked humanoid with an industrial sprayer. The brute becomes a lab release. Each already fits. |
| **Objectives** | Unchanged. "Hold until X completes" still carries the campaign. |
| **The Cascade** | Now names the moment the implants were repurposed, not a pathogen. Street slang for the enrolled: **"the signed"**. |
| **Tone** | The game gets an argument it did not have before: the enrolled are victims, and you are killing them by the hundred to stay alive. Nobody should feel good about the airstrike. |
| **`campaign-act1.md`** | **Needs a vocabulary pass.** Missions and structure survive intact; "infected" becomes "the signed", and mission 9's laboratory now releases the engineered things rather than testing a cure. |
| **ADR-001 / ADR-002** | Unaffected. |

## Reversal conditions

Reverse only if playtesting shows that enemies who look like ordinary people make the game
unreadable in motion at a thousand agents. If that happens the fix is a stronger implant tell,
not a return to monsters.

## Open questions for the owner

1. **The AI's name.** CIPHER is out. HALCYON is my placeholder for a municipal optimiser with an
   ironic name.
2. **Does the title survive?** "Dead Turf" was written for a gangster. It can be reread as the
   ground he will not give up, but that is a reread, not the original intent.
3. **Is there a cure, or only an off switch?** A cure makes act two about distribution. An off
   switch makes it about access, and is bleaker.
4. **How much does the game press the guilt?** The premise supports the player being told, late,
   exactly how many of the enrolled were children. That is a real choice and it is yours.
