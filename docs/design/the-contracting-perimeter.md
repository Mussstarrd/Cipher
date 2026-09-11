# The contracting perimeter — how Act One gets harder to funnel

**Date:** 2026-09-11 · **Author:** ENG · **Status:** proposal, owner's veto
**Built on:** [`campaign-act1.md`](campaign-act1.md) (the twelve-mission spine),
[ADR-004](../decisions/ADR-004-setting-the-lake.md) (the place),
[ADR-005](../decisions/ADR-005-the-scan-cycle.md) (the clock),
[ADR-009](../decisions/ADR-009-the-truck-is-the-family.md) (why you leave)

---

## 1. The owner asked for this, in these words

2026-09-11, after playing mission two for the first time:

> "after I hit a button it loaded the service road map which here I do like that it looks like there's
> more Avenues of approach for the people to come so it seems like each time we do need to make sure
> that these are getting progressively more and more challenging to direct the flow of traffic prior
> to the truck being overrun we're going to have to strategically make it so these levels Force our
> perimeter closer and closer into us until we're literally in our own garage remember that"

Three separate requirements are in that sentence and they are not the same requirement:

1. **More avenues of approach**, mission over mission.
2. **Progressively harder to direct the flow of traffic** — which is not the same as "more enemies",
   and is the thing tower-defence difficulty is actually made of.
3. **The perimeter closes in** until mission twelve is his own garage.

`campaign-act1.md` already promises the third. This document is the other two, made concrete enough
to author against, plus the one number each mission is tuned by.

**He also said "I do like that" about the service road** — and at the time the service road was a
brown field with one road across it. What he liked was the *shape* of the spawn list: four ways in
instead of three. That instinct is exactly right and it is what this ramp is built on.

---

## 2. The metric: frontage, not map size

"The map contracts" is a good sentence and a bad specification, because mission two's grid
(144 × 112) is **larger** than mission one's (128 × 96) and always will be — the back of the
community is physically bigger than the front gate. Contraction is about ground the player must
hold, not cells the engine allocates.

So Act One is tuned on three numbers per position:

| Number | What it is | Why it is the one that matters |
|---|---|---|
| **Approaches** | ways in the player has to cover at all — road gates plus interior flank spawns | This is the owner's "avenues". Two gates and two flanks is four decisions, not one. |
| **Frontage** | total cells of gap across every approach, after the terrain has done its work | A 6-cell gate costs a handful of barricades. Forty cells of open fairway cannot be walled at all and must be fought. |
| **Depth** | cells from the outermost approach to the truck | Depth is reaction time. It is the thing that collapses in Chapter III and it is what makes mission twelve mission twelve. |

**The rule that makes it a ramp:** *approaches go up, depth comes down, and frontage per approach
goes up faster than the player's income does.* Any two of those moving is a harder mission. All
three moving is the act.

### The shape of the curve

```
approaches   3  4  5  6  |  5  6  6  7  |  7  5  6  8
frontage    12 14 22 40  | 60 34 30 46  | 38 26 44 30
depth      126 136 120 96 | 110 92 86 74 | 70 62 58 34
              I           |      II      |     III
```

Chapter II's mission 5 is the deliberate spike: an open fairway with nothing to funnel with at all.
Chapter III's depth column is the story — by mission twelve the outermost approach is thirty-four
cells from the truck, which is about eight seconds of running.

---

## 3. Mission by mission

"Terrain does for free" is the funnelling the player gets without spending a dollar. "Terrain stops
doing" is what got taken away since the last position, and it is the sentence that makes each
mission feel like a step backwards even when the player is winning.

### Chapter I — The Perimeter

| # | Position | Approaches | Frontage | Buildable ground | Terrain does for free | Terrain stops doing |
|---|---|---|---|---|---|---|
| **1** | **The Gate** | **3** — one road gate, two interior flanks (the pool, the clubhouse lawn) | **12** — a single 12-cell gate gap | Wide. The whole lane between the pool fence and the house rows. | Almost everything. The clubhouse, two streets of houses, the pool fence and the rec centre's L-plan leave **two pinches, four and five cells wide**. One barricade line across one pinch is a working maze. | — (this is the baseline) |
| **2** | **The Service Road** | **4** — **two** road gates (the cattle gate, the washed-out boundary fence) plus two interior flanks | **14** across two gates, 6 and 8 | Wide but **split in two**. Anything spent at one gate is not spent at the other. | Two rows of outer-lot houses and the maintenance depot. But the gardens between the houses are **six to eight cells**, and there are **seven of them**. | **One line no longer covers the position.** The gate did the work last time; here there are two, they are forty cells apart, and the ground between the houses is walkable. |
| **3** | **The Pump House** | **5** — two roads, the shore, two flanks | **22** | Narrowing. The well field itself is off-limits to building. | The substation fence and the transformer bays. | **Hazard ground.** Some of the best firing positions are now inside the spill radius, so the ground that helps you also hurts you. |
| **4** | **The Gate Falls** | **6** — every way into the front of the community at once | **40** | The same ground as mission 1, **plus whatever you built there in mission 1, still standing and still damaged**. | Nothing new. The same pinches — and the player knows them. | **The pinches stop being enough.** Six approaches against two pinches is arithmetic the player can do, which is the point: *cannot be won*, scored on minutes bought. |

**Mission 2 is the one that teaches the ramp**, and it has to be read directly against mission 1.
The Gate is a corridor: block the pinch and they take the long way. The Service Road is a *field with
houses on it*: every house is cover and every gap between houses is a lane. The player's first
instinct — one wall line at the gate — is correct and insufficient, and finding that out in mission
two is the cheapest possible lesson.

### Chapter II — Falling Back

| # | Position | Approaches | Frontage | Buildable ground | Terrain does for free | Terrain stops doing |
|---|---|---|---|---|---|---|
| **5** | **The Fairway** | **5** | **60** — the widest in the act | Enormous and worthless: you cannot wall sixty cells. | **Nothing.** Open fairway, ground fog, no cover for anybody. | **All of it.** This is the scale mission and it is deliberately the one position where terrain is not a tool. The answer is guns and the scan clock, not a maze. |
| **6** | **Crowbar** | **6** | **34** | Your own maze from mission 5's salvage, in a street grid. | The street grid — narrow, and it is genuinely good ground. | **Walls stop being permanent.** Sappers debut against player walls; the ground still funnels, but only while it holds. |
| **7** | **The Fire Station** | **6** | **30** | The tightest and best in the act: a truck bay, a forecourt, two approach roads. | The apparatus bay and the commercial row. The best free funnelling since mission 1. | Nothing — **mission 7 is the breather**, and it is the only one. A ramp with no step back down reads as a slope. |
| **8** | **Blackout** | **7** | **46** | Large, and you cannot see most of it. | The interior streets, in principle. | **Sight.** The minimap is jammed and the streets are dark, so terrain that funnels perfectly well is terrain the player cannot read. Same ground, worse information. |

### Chapter III — The Last Street

| # | Position | Approaches | Frontage | Buildable ground | Terrain does for free | Terrain stops doing |
|---|---|---|---|---|---|---|
| **9** | **The Facility** | **7** | **38** | A lab compound that **changes between waves** without the player touching it. | Shutters, bays and a service yard. | **Permanence.** A maze built in wave one is a different maze in wave three. The ground is good and it will not stay put. |
| **10** | **The Release** | **5** | **26** | Wooded back lots: narrow, broken, full of cover. | The treeline — the best cover in the act. | **Cover cuts both ways.** The one thing you cannot see coming is the one thing that matters. |
| **11** | **The Ramp** | **6** | **44** | The clubhouse apron and the boat ramp. | The clubhouse itself, a genuine fortress. | **Freedom to place.** The evacuation lanes must stay open — every cell you wall is a cell the pontoons' queue cannot use. |
| **12** | **Front Porch** | **8** | **30** | One lot. A driveway, a garage, a treeline, a street. | The house. Your house. | **Depth.** Thirty-four cells from the outermost approach to the truck, and the extract call is greyed out. There is no long way round for them any more, because there is nothing left to go round. |

---

## 4. The four levers, and the order to reach for them

Every row above is one or more of these. They are listed in the order they should be spent, because
each is cheaper to author than the one after it and each is easier for the player to read.

1. **Add an approach.** The cheapest lever in the game: one entry in `spawnCells`. It is also the
   one the owner asked for by name, and it has no downside except that the last two missions have
   nowhere left to put one.
2. **Widen the gaps.** Push the buildings apart. Mission 1's pinches are four and five cells;
   mission 2's are six to eight. The player's barricade budget is roughly fixed per position, so
   widening the gaps is a difficulty dial that never touches a wave table.
3. **Take away permanence.** Sappers (6), changing geometry (9), lanes that must stay open (11).
   The maze still works — it just stops staying built.
4. **Take away depth.** Reserved for Chapter III, because it is the only lever that cannot be
   undone and it is the one that means *we are nearly home*.

**Enemy count is not on this list, and that is deliberate.** The owner's other standing instruction
is "less zombies... more robust — quality over quantity" (2026-09-11, already in the scenarios as
per-position `enemy.health`). A map that is harder to funnel is harder at the same body count, which
is exactly what we want: the difficulty should be legible as *geometry*, not as a bigger number.

---

## 5. What shipped with this document

**`act1-02-the-service-road` is authored to the mission-2 row above.** Before: seven wall rects,
eleven props, all of them gate furniture, and a single 14-cell gap in the boundary fence that both
west spawns had to funnel through — so the map that was *supposed* to be the "more avenues" mission
was mechanically **more** funnelled than mission one. After:

| | before | after |
|---|---|---|
| road gates through the boundary fence | 1 (both west spawns used it) | **2** — the cattle gate (6 cells) and the washed-out fence (8) |
| interior flank spawns | 2 | 2 |
| props | 11 | **77** |
| community buildings (houses, depot, bleachers) | **0** | **16** |
| interior gaps of 6–8 cells between buildings | 0 | **7** |
| scan cycle | inherited the default, 600 s | **560 s**, authored, on the ramp |

Nine of the thirteen houses are two rows of outer lots with the gardens between them left walkable; the
`CommunityCentre` is the maintenance depot the road exists to serve; the cattle gate has its
guardhouse, boom and jersey barriers, and the washed-out fence has brush and pallets where somebody
gave up trying to close it.

### One new guard, because this was authorable-by-accident

`ScenarioReader.Validate` refuses a mission whose **walls** seal a gate. Props became solid on
2026-09-12 and the reader's probe never learned about them, so **a building across the only corridor
loaded perfectly clean** and showed up as a wave that spawns and then mills about — no error, no
crash, a level that is "wrong somehow" in the middle of a playtest.

`ShippedScenarioTests.PropsNeverSealAGate` is the reader's probe with `PropCatalog` applied on top,
under the same rules the bootstrap uses. It runs on every mission file in CI. Authoring a position
with real architecture is now something a person can do without holding the whole flow field in
their head, which is the precondition for any of the rows above being built.

---

## 6. Two things the owner should rule on

1. **Mission 3 is off the cycle ramp.** ADR-005 says the scan cycle shortens across the act. Mission
   1 runs on the 600-second default and mission 2 now authors 560 — but `act1-03-the-pump-house`
   authors **900**, which is longer than either. Either the pump house is a deliberate breather and
   should say so, or it is a number nobody revisited. My read is the second; the fix is one line.
2. **Mission 4 and the cost of "cannot be won".** Mission 4 is the same ground as mission 1 with six
   approaches instead of three, and `campaign-act1.md` scores it on minutes bought. That works only
   if the player arrives already believing they can hold it. If mission 2 and 3 have taught them
   that four approaches beat one wall line, mission 4 may read as expected rather than as a shock.
   Worth playing in order before it is authored.
