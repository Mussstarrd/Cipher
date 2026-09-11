# Authoring a mission

**For:** the owner, and anyone changing what a position plays like without rebuilding the game.
**Files:** `game/Assets/Resources/Scenarios/*.json`. One file per position. The filename **is** the id.

A mission is a text file. Change a wave table, move a gate, retune the economy, add a guardhouse —
save, run. Nothing is compiled. Three ship today: `act1-01-the-gate.json`,
`act1-02-the-service-road.json` and `act1-03-the-pump-house.json`. Copy one and edit it; the pump
house is the one with actors and a custom scan cycle in it.

## The shortest possible mission

```json
{
  "schema": 1,
  "id": "my-mission",
  "displayName": "My Mission",
  "map": { "width": 64, "height": 48 },
  "heroSpawn": { "x": 58, "y": 24 },
  "spawnCells": [ { "x": 1, "y": 24, "gate": "west" } ],
  "vault": { "x": 62, "y": 24, "hp": 25 },
  "waves": [ { "setupSeconds": 25, "count": 150, "spawnPerSecond": 8 } ],
  "objectives": [ { "type": "ClearWaves", "count": 1 } ]
}
```

Everything else has a default. Save it as `my-mission.json` and it is a level.

## The reader is strict, and that is on purpose

It will refuse the file rather than load a mission that plays subtly wrong, and every message names
the exact place:

```
$.waves[2].spawnPerSecond: expected a number, found string
$.map.walls[1].rect: [30,0,4,4] falls outside the 32x32 map
$.objectives: ClearWaves asks for 7 waves but only 5 are defined, so the objective can never complete
$.spawnCells: no route from the gate at (1,24) to the vault at (62,24); the walls seal it off
$.waves[0].mix: shares sum to 0.7, they must sum to 1
$.teir: unknown field 'teir'. Known fields here: schema, id, displayName, tier, ...
```

**A misspelled field is an error, not a shrug.** A silently ignored key is how you spend an hour
wondering why the wave table did nothing.

There is no fallback level. If the file is broken the game says so and stops.

## What you can set

### Identity

| Field | Meaning |
|---|---|
| `id` | Must match the filename. |
| `displayName` | Shown on the objectives panel. |
| `brief` | One or two sentences, in the protagonist's voice. |
| `tier` | Rough difficulty band. Content-facing only for now. |
| `next` | The mission file to load when you fall back from here. Omit it and the authored chain stops. |
| `lastStand` | `true` = there is nowhere behind this position. Greys out the extract call entirely, so it can only be held or lost. In Act One this is mission 12 and nothing else. **Not the same as omitting `next`** — one is design, the other is how much content is written. |

### The map

```json
"map": {
  "width": 64, "height": 48,
  "walls": [
    { "rect": [16, 0, 1, 6],  "kind": "Wall" },
    { "rect": [16, 13, 1, 35], "kind": "Wall" }
  ]
}
```

`rect` is `[x, y, width, height]` in cells. `kind` is `Wall` (breachable — sappers can open it),
`Static` (never breachable) or, rarely, `Barricade`/`Structure`. Default is `Wall`.

A wall with a gap in it is two rects. **Stagger the gaps** — that is what makes a route long. The
Gate's three fence lines have their gaps low, high, low, which is why it is a level and not a
corridor. All three gaps on the same row makes a straight line and you have a hallway.

Size is limited to 24–512 a side.

### Where everyone starts and what they want

```json
"heroSpawn":  { "x": 56, "y": 24 },
"spawnCells": [ { "x": 1, "y": 12, "gate": "fairway-north" } ],
"vault":      { "x": 62, "y": 24, "hp": 25 }
```

Every gate must have a route to the vault, and the loader checks it with the same flow field the
game uses. Name your gates: it costs nothing and a brief that says "they'll come up the fairway"
means something.

### Waves

```json
"waves": [
  { "setupSeconds": 25, "count": 150, "spawnPerSecond": 8,
    "mix": { "Runner": 0.98, "Spitter": 0.02 } }
]
```

`setupSeconds` is the build phase **before** that wave. The first one is ignored in practice: the
opening setup at a new position has no clock and starts when you press start.

`mix` shares must sum to 1. It is read and kept but the spawn director is still global rather than
per-wave, so it does not yet steer what actually spawns — write it truthfully anyway, because the
day the director goes per-wave it should not mean editing twelve mission files.

### Money and the director

```json
"economy": { "startCash": 400, "cashPerKill": 5, "waveClearBonusPerWave": 100 },
"director": { "seed": 20260911, "sapperFirstAt": 45, "spitterFirstAt": 40,
              "hunterShare": 0.10, "wreckerShare": 0.08 }
```

`seed` is what makes a mission play the same way twice, which is what makes a note about wave three
worth writing down. Change the seed and you get a different but equally repeatable mission.

`hunterShare` is the fraction of ordinary bodies that peel off to kill emplacements; `wreckerShare`
the fraction that attack a barricade instead of walking around it. Together they cannot exceed 1.

### What winning means

```json
"objectives": [
  { "type": "ClearWaves", "count": 2 },
  { "type": "ProtectVault", "minHp": 1 }
]
```

All objectives must complete to finish; any failure loses it.

| Type | Parameters | Goal or condition | Notes |
|---|---|---|---|
| `ClearWaves` | `count` | goal | Cannot exceed the number of waves you defined. |
| `SurviveSeconds` | `seconds` | goal | The clock missions. |
| `HoldUntil` | `actorId` | goal | A Process actor finishes its work. Fails if the thing is destroyed. |
| `ProtectVault` | `minHp` | **condition** | |
| `ProtectActors` | `minAlive` | **condition** | Counts Structures and Processes. |
| `KeepCrewAlive` | `minAlive` | **condition** | Counts Crew. |

A **condition** is something you hold, not a task you finish. It can only fail or stay pending, and
the mission never waits on it — a mission whose only objective is a condition could never be won.
Every mission needs at least one goal.

### Actors: the things a mission is fought over

```json
"actors": [
  { "id": "transfer", "kind": "Process", "x": 63, "y": 28, "hp": 260,
    "durationSeconds": 300, "requiresHeroWithin": 0 },
  { "id": "transformer-a", "kind": "Structure", "x": 60, "y": 23, "hp": 180 },
  { "id": "dave", "kind": "Crew", "x": 61, "y": 30, "hp": 60 }
]
```

| Kind | What it is |
|---|---|
| `Process` | Runs a clock. `durationSeconds` is the work; `requiresHeroWithin` (cells) means it only runs while you are standing there — set 0 and it runs unattended. |
| `Structure` | A transformer, a pump, a well head. It has HP and that is all. |
| `Crew` | A person. Takes damage three times as fast, because people do. |

Enemies standing near an actor wreck it, and how fast depends on how many. **Actors do not block
movement** — nothing an objective owns is allowed to change where the horde walks.

Work on an attended Process **pauses** when you leave; it does not reset. You can be pushed off it
and come back.

### The scan cycle, per position

```json
"cycle": { "cycleSeconds": 900, "minWavesBeforeExtract": 2,
           "extractSeconds": 90, "prepDollarsPerSecond": 0.9 }
```

This is the difficulty curve and the fiction in one number. HALCYON frees up compute as the act goes
on, so `cycleSeconds` should **shorten** mission by mission — days early, hours by the end. Whatever
is left of it when you pull out becomes materials at the next position, at `prepDollarsPerSecond`.

A mission with a long `HoldUntil` needs a long cycle, or there is nothing left to fall back with.

**Completing the objectives does not end the mission.** It opens the pack-up window, unless
`lastStand` is set. That is the point of the scan cycle: leaving is a thing you do under a clock,
carrying what fits.

### Scenery

```json
"props": [
  { "kind": "Pillar",      "x": 16.5, "y": 5.4 },
  { "kind": "Guardhouse",  "x": 19.0, "y": 14.5, "yaw": 200 },
  { "kind": "BoomBarrier", "x": 17.4, "y": 6.2,  "yaw": 90 },
  { "kind": "JerseyBarrier", "x": 19.5, "y": 8.0, "yaw": 90 }
]
```

Decoration only — none of it blocks movement or takes damage, so put it wherever it looks right.
Positions are in cells and may be fractional. `yaw` is degrees.

## Rules of thumb

- **Two waves is a short mission, five is a long one.** The player chooses where to stop inside
  that; the wave table is the ceiling, not the length.
- **Count roughly doubles across a mission.** 150 → 250 → 350 → 500 → 700 is The Gate.
- **`spawnPerSecond` is the pressure dial**, and it matters more than `count`. Same 500 bodies at 8
  a second is a grind; at 20 a second it is a crisis.
- **Start cash buys about four things.** $400 is two turrets, or one turret and a lot of wall.
- **Give the fall-back position a harder table than the one before it**, because the player arrives
  with materials, levels and a shorter cycle.
- **A `HoldUntil` should be most of the mission, not all of it.** Five minutes of transfer inside a
  fifteen-minute cycle leaves the player a real choice about how many extra waves to take.

## Checking your work

Every file in `Resources/Scenarios` is loaded by the test suite, which asserts it parses, that its
id matches its filename, that any `next` names a file that exists, and that it has at least one
objective that can ever complete. So a broken mission fails CI rather than a playtest.

```
Unity.exe -batchmode -nographics -projectPath game -runTests -testPlatform EditMode -testResults out.xml
```
