# Design memo — Progression, items, the safe zone, and scenarios as data

**Date:** 2026-09-10 · **Author:** ENG/design · **Status:** proposal, owner veto
**Inputs:** [owner playtest](../feedback/2026-09-10-maze-v1-first-play.md) (the "toy → product" list), [pitch](../02-FLAGSHIP-PITCH.md) §3, [blueprint](../03-DAY1-EXECUTION-BLUEPRINT.md) Traps 7/8, [Open Decisions](../decisions/OPEN-DECISIONS.md) #3 #6 #8, [ADR-002](../decisions/ADR-002-camera-model.md), [roadmap](ROADMAP-2026-09.md), [economy memo](economy-towers-and-aiming.md). Code read: `HeroModel.cs`, `MatchRules.cs`, `Directors.cs`, `BuildModel.cs`, `TurretSystem.cs`, `FloodBootstrap.cs`.
**Premise:** the runaway-AI bio-weapon story (ADR being written in parallel by the lead — treated as settled here). The horde is people who plan, flank and use tools; most missions are survived, not cleared.

---

## 1. TL;DR

1. **Three progression layers, each shippable alone**: in-match (crates, turret tiers — exist today), run-scoped gear (drops in a mission, equipped in the safe zone), account-scoped persistence (armour tiers, unlocks, perks, cosmetics). Gear **survives a mission loss**; in-match cash and turret tiers **do not**.
2. **Two currencies, deliberately non-fungible.** Cash ($) is intra-mission and is deleted at mission end. **Scrip** comes from selling gear in the safe zone and buys account-scoped upgrades. Gear can never be sold into the maze budget, so $5/kill keeps its weight.
3. **One hero (Enforcer), three build paths** (Trigger / Ordnance / Doctrine), delivered in v0 as a **Hades-style pick-1-of-3 "Contract" at each wave clear** — not a Bloons tree. The tree is v0.2, gated on Open Decision #6.
4. **The safe zone is between missions, plus one scripted mid-mission Stash Run** declared per scenario (`safeZoneAfterWaves`). Setup phases stay 20–25 s and stay a *build* phase.
5. **Missions become JSON.** `IObjective` composes 2–3 per mission, including non-elimination objectives (`SurviveSeconds`, `ProtectActors`, `HoldUntil`). The smallest first step is ~120 lines: make today's hard-coded five waves the first scenario file and assert it round-trips.
6. **Couch co-op is split-screen, not shared-screen** — ADR-002's over-the-shoulder camera cannot be shared. Five singular assumptions in today's code must die now, while they are one-liners.

---

## 2. Progression stack

| Layer | Scope | Earned by | Survives a mission **loss**? | Survives a **win**? |
|---|---|---|---|---|
| **A. In-match power** | one mission | cash → barricades, turret tiers; gun crates (`PickupSystem`) | **No** | **No** |
| **B. Run-scoped gear** | one mission, then banked | item drops from kills; guaranteed drops from Sappers/Spitters; mission reward | **Yes** — items already picked up are kept | Yes |
| **C. Account** | forever | Scrip (from selling gear), mission medals, campaign milestones | Yes | Yes |

**Why gear survives a loss.** Risk of Rain 2 discards *everything* on death — items, gold, XP — keeping only challenge unlocks ([RoR2 items](https://riskofrain2.fandom.com/wiki/Items)). Correct for a 20-minute roguelike run, wrong for us: the owner asked for "armor and shit that can upgrade over time", and our missions are 10–15 minutes of hand-built maze. Losing wave 5 of the Power Yard and keeping nothing would read as theft. Dungeon Defenders is the closer reference — loot drops in the mission and the Tavern between missions is where you equip, upgrade and sell it ([Tavern](https://dungeondefenders.wiki.gg/wiki/Tavern), [Tavernkeep](https://dungeondefenders.fandom.com/wiki/Tavernkeep)) — and Deep Rock Galactic makes the split explicit: per-run upgrade cards versus semi-permanent meta upgrades bought between missions ([Overclocks](https://deeprockgalactic.wiki.gg/wiki/Weapon_Overclocks)).

**Why layer A stays disposable.** It is the Bloons covenant already in the code: the ~$9,750 a five-wave mission pays out buys one or two turret ladders and then evaporates. If cash persisted, mission 8 would start solved. Hades draws the same line — boons die with the run, the Mirror of Night is permanent ([Mirror of Night](https://hades.fandom.com/wiki/Mirror_of_Night)).

Ship order: **B before C.** Layer B is playable alone; layer C is a save file plus a shop over it.

---

## 3. Item model

**Slots (8).** `Weapon`, `Vest`, `Helm`, `Gloves`, `Boots`, `Charm ×2`, `Crew Token`. Diablo 4 ships 10 across armour/jewellery/weapons ([Equipment](https://diablo4.wiki.fextralife.com/Equipment)); 8 is the same shape minus the slots that exist only to be a second ring. `Crew Token` is the premise hook: a named lieutenant's tag, so permadeath destroys an item.

**Rarity (5), affix counts 0/1/2/3/4:** `Street` → `Corner` → `Connected` → `Capo` → `Kingpin`. Rarity multiplier `rM` = 1.00 / 1.25 / 1.55 / 1.90 / 2.30.

**Affixes** reuse the pitch's modifier vocabulary as named affix families, so the words the pitch sells are the words on the item card:

| Affix | Effect | Family |
|---|---|---|
| **Laundered** | +N% cash per kill | economy |
| **Cold-Blooded** | kills refund M% of airstrike cooldown | tempo |
| **Made Man** | once per wave, survive a lethal hit at 1 HP (30 s lockout) | defence, Kingpin-only |
| Cut With | +N% gun damage | offence |
| On Retainer | +N% turret damage within 8 cells | crew |
| Bulletproof | flat −N damage per contact tick | defence |
| Runner | +N% move speed | mobility |

**Item level.** `ilvl = 4 × missionTier + waveIndex` (tier-1 mission, wave 3 → ilvl 7). Tiers 1–6 across the campaign give ilvl 4–28.

**Power budget — the comparison formula.** Every item's affixes are rolled against one budget, so two items compare by one number:

```
Budget(item)     = slotWeight × (4 + 1.6 × ilvl) × rM
slotWeight:        Weapon 1.00 | Vest 0.80 | Helm/Gloves/Boots 0.50 | Charm 0.35 | CrewToken 0.35
PowerScore(item) = Σ (affix magnitude × statWeight)
statWeight:        +1% gun damage 1.0 | +1% fire rate 1.2 | +1% max HP 0.8 | +1 flat armour 4.0
                   +1% cash/kill 0.5  | −1% airstrike cd 1.1 | +1% turret damage 0.7
```

The roller spends `Budget` across the affix count with ±12% jitter per affix, so `PowerScore ≈ Budget` by construction. The UI shows `PowerScore` as **"Weight"** plus a per-slot delta. Worked example: a `Capo` Vest at ilvl 12 → `0.80 × (4 + 19.2) × 1.90 = 35.3` weight, spent as +18% max HP (14.4) + 3 armour (12.0) + 17% Laundered (8.5).

**Drop sources and rates**, tuned so a five-wave mission yields ~10–12 keepers — one safe-zone visit is a real sort, not a chore:

| Source | Rate | Quality floor |
|---|---|---|
| Runner kill | 0.25% | Street |
| **Sapper kill** | **100%** | **Connected** (rarity ≥ 2) |
| **Spitter kill** | **100%** | **Corner** (rarity ≥ 1) |
| Wave clear | 1 item, floor `min(waveIndex, 3)` | — |
| Mission reward | scenario-declared (§6) | scenario |

At today's counts (1,950 runners, ~3 Sappers, ~4 Spitters) that is ~5 + 7 + 5 ≈ 17 rolls, of which ~12 survive the auto-junk filter (§5). Rarity roll for un-floored drops: 62 / 24 / 10 / 3.5 / 0.5%. Making the two rare archetypes *guaranteed* drops is the point: they are the units the maze-v1 playtest showed the owner actually hunting.

**Sell value is Scrip, never Cash:** `Scrip = round(2.5 × ilvl × rM)`. A Street ilvl-5 pays 13; a Kingpin ilvl-20 pays 115. Dumping nine junk items from one mission ≈ 150–220 Scrip; an account armour tier costs 400–1,200, so a tier is 3–6 missions — which matches the pitch's "every session a build-defining item".

**Explicitly not in v0:** crafting, re-rolling, set bonuses, sockets. Those are the Trap 7 content mountain wearing a different hat.

---

## 4. Character builds

**Recommendation: one hero (the Enforcer) with three build paths.** This matches the Open Decision #3 draft cut-list ("one hero kit"), and it is honest about cost: three heroes means three weapon feels, three animation sets and three balance passes — the expensive half of a hero. Three paths on one body is three data tables.

| Path | Fantasy | Signature | Scales off |
|---|---|---|---|
| **Trigger** | you are the DPS | gun damage / fire rate, execution refunds | Weapon, `Cut With`, `Cold-Blooded` |
| **Ordnance** | you delete lanes | airstrike cooldown/width, thermite, self-damage immunity | Charms, `Cold-Blooded` |
| **Doctrine** | the crew is the weapon | turret damage/range aura, faster repair drones, extra Crew Token | `Crew Token`, `On Retainer` |

**Delivery: Hades-style pick-1-of-3 at wave clear ("Contracts"), not a Bloons tree — for v0.**

| | Pick-1-of-3 at wave clear | Static tree (Bloons) |
|---|---|---|
| Content needed to feel deep | ~24 cards | ~45 nodes + tree layout |
| UI cost | one card row, A to pick | full gamepad tree nav (Open Decision #6, **unproven**) |
| Variety within a mission | high | none |
| Build identity across missions | weak | strong |
| Save / respec code | none | required |
| Fits a two-week v0 | **yes** | no |

Cards are tagged to a path; taking three of one path unlocks that path's capstone card in the pool. That is tree-like commitment with zero tree UI, and it directly feeds the "one more wave" test because the reward lands exactly at the moment the player is deciding whether to keep going.

**Kill criterion (Open Decision #8 format):** across three missions, the owner can name his build unprompted afterwards ("I went Trigger with the cash affixes") in ≥2 of 3, and picks the same path's card ≥50% of the time when it is offered. **Pre-committed fallback if the picks read as noise:** collapse to a single path committed at mission start with fixed per-wave unlocks — 40 lines, same data.

---

## 5. The safe zone

**What happens there:** equip / compare / sell gear; spend Scrip on account upgrades (armour tiers, weapon unlocks, perks); pick the next mission off the city map; read the story beat. Untimed, no enemies. Diegetically it is the syndicate's own block — the pitch's turf, Dungeon Defenders' Tavern.

**Cadence — argued.** Today's setup phase is 20–25 s and is *fully occupied* by building.

- **Every wave, inside setup.** Rejected: five inventory visits per mission, each interrupting the 25 s the player needs for barricades. It attacks the "one more wave" test directly.
- **Every third wave, inside setup.** Rejected for the same reason at lower frequency, and it makes the setup timer mean two things.
- **Between missions, plus one scenario-declared mid-mission Stash Run.** **Recommended.** The scenario names the waves after which it happens (`"safeZoneAfterWaves": [3]`); the match pauses into the safe-zone screen and returns to a normal setup phase. The eight-minute Power Yard gets one; the teaching mission gets none. The owner's "or every third round" is honoured *per mission, in data*, without a global rule that fights the build phase.

**Controller UX — the inventory grid.**

- **Layout:** left, eight equipped slots as a doll; right, a backpack grid **8 wide × 5 tall (40)**; bottom, a live compare panel.
- **Navigation:** LS or D-pad steps one cell — first step immediate, repeat 8/s ramping to 14/s after 0.6 s, deadzone 0.2. Deliberately the same curve as the build cursor, so the thumb learns it once. Wrapping left from column 0 moves onto the doll.
- **A** equip (auto-routes to its slot; a displaced item returns to the backpack). **X** sell — **hold 0.4 s** for rarity ≥ Capo or anything equipped. **Y** pin as compare baseline. **RB/LB** filter tabs (All / Weapons / Armour / New). **LT held** enters mark-for-sale: everything below the pinned rarity flashes, release sells the lot under one confirm. **RT** auto-equips best by Weight. **B** exits (blocked while items are marked). **Menu** opens the account shop.
- **The compare panel is always on**, showing Δ Weight, Δ effective DPS and Δ effective HP against whatever is in that slot. Nothing about a controller inventory is hard except comparison — which is why Diablo compares by default.
- **Kill criterion (Open Decision #6 format):** the owner enters the safe zone holding **12 new items**, equips the three best and sells the rest, in **under 60 s with zero mis-sells** (a mis-sell = selling an equipped item, or one with a higher Weight than what stays in that slot), **2 of 3 attempts**, stopwatch in the build. **Fallback 1:** cut the grid for one "SORT BY WEIGHT + SELL ALL BELOW &lt;rarity&gt;" button. **Fallback 2:** cut the backpack entirely — the safe zone offers three items per slot and everything else auto-sells.

---

## 6. Scenario format as data

### Schema (v1)

```
ScenarioDef {
  schema:int, id, displayName, tier:int, brief,
  map:      { width, height, preset, walls:[{rect:[x,y,w,h], kind}] },
  heroSpawn:{x,y}, spawnCells:[{x,y,gate}], vault:{x,y,hp},
  actors:   [{ id, kind:Structure|Process|Crew, x, y, hp?, durationSeconds?, requiresHeroWithin? }],
  economy:  { startCash, cashPerKill, waveClearBonusPerWave },   // optional; default = EconomyConfig
  director: { seed, sapperFirstAt, spitterFirstAt, ... },        // optional; default = DirectorConfig
  waves:    [{ setupSeconds, count, spawnPerSecond, mix:{Runner,Sapper,Spitter} }],
  safeZoneAfterWaves: [int],
  objectives: [ObjectiveDef],      // ALL must complete to win; ANY failure loses
  rewards:  { scrip, guaranteedDrops:[{slot,rarity,ilvl}], unlocks:[id] },
  medals:   { bronze:[Criterion], silver:[...], gold:[...] }
}
```

### `IObjective`

```csharp
public enum ObjectiveState { Pending, Complete, Failed }

public interface IObjective {
    string Id { get; }
    string Hud { get; }          // one live line: "GRID RESTART  4:12"
    float Progress01 { get; }
    ObjectiveState Tick(in ObjectiveContext ctx, float dt);
}
```

`ObjectiveContext` is a readonly struct carrying `MatchSeconds, WaveIndex, WavesCleared, AliveEnemies, VaultHp, HeroDowns, IHeroQuery Heroes, IActorQuery Actors`. The v0 set is six: **`ClearWaves`**, **`SurviveSeconds`**, **`ProtectVault`**, **`ProtectActors`** (N of M alive), **`HoldUntil`** (a `Process` actor accumulating time, optionally only while a hero is within R cells), **`KeepCrewAlive`** (the permadeath hook). Three are non-elimination — the premise's whole point: the Power Yard is won by a clock, not a kill count.

### Mission A — teaching (`content/scenarios/tut-01-the-corner.json`)

```json
{
  "schema": 1, "id": "tut-01-the-corner", "displayName": "The Corner", "tier": 1,
  "brief": "Two gates. One vault. They are not mindless - they will find the cheapest way in.",
  "map": { "width": 64, "height": 48, "preset": "arena", "walls": [] },
  "heroSpawn": { "x": 58, "y": 24 },
  "spawnCells": [ { "x": 1, "y": 14, "gate": "west-a" }, { "x": 1, "y": 34, "gate": "west-b" } ],
  "vault": { "x": 62, "y": 24, "hp": 25 },
  "actors": [],
  "economy": { "startCash": 400, "cashPerKill": 5, "waveClearBonusPerWave": 100 },
  "director": { "seed": 20260910, "sapperFirstAt": 99999, "spitterFirstAt": 99999 },
  "waves": [
    { "setupSeconds": 45, "count": 60,  "spawnPerSecond": 6,  "mix": { "Runner": 1.0 } },
    { "setupSeconds": 30, "count": 120, "spawnPerSecond": 8,  "mix": { "Runner": 1.0 } },
    { "setupSeconds": 25, "count": 200, "spawnPerSecond": 10, "mix": { "Runner": 0.98, "Spitter": 0.02 } }
  ],
  "safeZoneAfterWaves": [],
  "objectives": [
    { "type": "ClearWaves", "count": 3 },
    { "type": "ProtectVault", "minHp": 1 }
  ],
  "rewards": {
    "scrip": 60,
    "guaranteedDrops": [ { "slot": "Vest", "rarity": "Corner", "ilvl": 4 } ],
    "unlocks": [ "contract.trigger.cutwith" ]
  },
  "medals": {
    "bronze": [ { "type": "Complete" } ],
    "silver": [ { "type": "VaultHpAtLeast", "value": 20 } ],
    "gold":   [ { "type": "VaultHpAtLeast", "value": 25 }, { "type": "TimeUnder", "seconds": 300 } ]
  }
}
```

### Mission B — the power yard (`content/scenarios/act1-03-power-yard.json`)

```json
{
  "schema": 1, "id": "act1-03-power-yard", "displayName": "Substation 9", "tier": 2,
  "brief": "The lab needs power for eight minutes. They know that too - and they know which transformer to hit.",
  "map": { "width": 80, "height": 56, "preset": "power-yard",
           "walls": [ { "rect": [24, 0, 2, 18], "kind": "Static" },
                      { "rect": [24, 38, 2, 18], "kind": "Static" } ] },
  "heroSpawn": { "x": 40, "y": 28 },
  "spawnCells": [ { "x": 1, "y": 10, "gate": "west" }, { "x": 1, "y": 46, "gate": "south-west" },
                  { "x": 40, "y": 1, "gate": "north" }, { "x": 78, "y": 46, "gate": "east-service" } ],
  "vault": { "x": 74, "y": 28, "hp": 30 },
  "actors": [
    { "id": "transformer.a", "kind": "Structure", "x": 20, "y": 14, "hp": 600 },
    { "id": "transformer.b", "kind": "Structure", "x": 20, "y": 42, "hp": 600 },
    { "id": "grid.restart",  "kind": "Process",   "x": 40, "y": 28,
      "durationSeconds": 480, "requiresHeroWithin": 0 }
  ],
  "economy": { "startCash": 650, "cashPerKill": 5, "waveClearBonusPerWave": 120 },
  "director": { "seed": 771103, "sapperFirstAt": 40, "sapperSpacing": 45, "maxSappersAlive": 2,
                "spitterFirstAt": 35, "maxSpittersAlive": 4 },
  "waves": [
    { "setupSeconds": 60, "count": 200, "spawnPerSecond": 9,  "mix": { "Runner": 0.97, "Spitter": 0.03 } },
    { "setupSeconds": 25, "count": 320, "spawnPerSecond": 12, "mix": { "Runner": 0.94, "Spitter": 0.04, "Sapper": 0.02 } },
    { "setupSeconds": 25, "count": 450, "spawnPerSecond": 14, "mix": { "Runner": 0.92, "Spitter": 0.05, "Sapper": 0.03 } },
    { "setupSeconds": 25, "count": 600, "spawnPerSecond": 16, "mix": { "Runner": 0.90, "Spitter": 0.06, "Sapper": 0.04 } },
    { "setupSeconds": 25, "count": 850, "spawnPerSecond": 20, "mix": { "Runner": 0.88, "Spitter": 0.07, "Sapper": 0.05 } }
  ],
  "safeZoneAfterWaves": [3],
  "objectives": [
    { "type": "HoldUntil",     "actor": "grid.restart", "seconds": 480 },
    { "type": "ProtectActors", "actors": ["transformer.a", "transformer.b"], "requireAlive": 1 },
    { "type": "ProtectVault",  "minHp": 1 }
  ],
  "rewards": {
    "scrip": 220,
    "guaranteedDrops": [ { "slot": "Weapon", "rarity": "Connected", "ilvl": 9 },
                         { "slot": "CrewToken", "rarity": "Capo", "ilvl": 9 } ],
    "unlocks": [ "scenario.act1-04-the-lab", "shop.armour.tier2" ]
  },
  "medals": {
    "bronze": [ { "type": "Complete" } ],
    "silver": [ { "type": "ActorsAlive", "actors": ["transformer.a","transformer.b"], "value": 2 } ],
    "gold":   [ { "type": "ActorsAlive", "actors": ["transformer.a","transformer.b"], "value": 2 },
                { "type": "HeroDownsAtMost", "value": 0 } ]
  }
}
```

Note the shape: this mission **wins at T+480 s with the street still full of enemies**. That is the premise made mechanical, and it is a `MatchPhase.Won` transition today's code cannot express.

### Smallest change that makes today's arena the first scenario

Three edits, roughly 120 lines, no rewrite, each independently testable:

1. **`WaveTable.Default` becomes a fallback, not the source.** Add `ScenarioDef` plus a loader, write `content/scenarios/tut-01-the-corner.json`, and add one test asserting the loaded waves are element-wise equal to `WaveTable.Default`. Ship *that* first, so the JSON path is proven before anything depends on it.
2. **`MatchState` takes `IReadOnlyList<IObjective>`** and moves its two win/lose transitions out of `Tick`: `Phase = Won` when every objective is `Complete`, `Phase = Lost` when any is `Failed`. Construct it with `[new ClearWaves(waves.Count), new ProtectVault(1)]` and behaviour is byte-identical — the existing `MatchRulesTests` and `MatchIntegrationTests` are the regression guard.
3. **`WaveDef` gains `Mix`** (archetype weights) and `SpawnDirector` reads it alongside `DirectorConfig`, with today's constant ratios as the default mix. `FloodBootstrap`'s `SpawnCells`, `GoalX/GoalY`, `GridW/GridH` and `HeroSpawn` constants become fields read from the loaded `ScenarioDef`.

After step 3, `FloodBootstrap` boots any JSON file and the Power Yard is a content ticket, not an engineering one.

---

## 7. Architecture (SOLID, testable)

**JSON, not ScriptableObject.** ADR-001 chose Unity partly for assets an AI author can diff; a `.asset` file is YAML full of GUID references — unreviewable in a PR, un-authorable by a headless test, merge-hostile. JSON under `content/` is greppable, diffable and, decisively, **loadable by `dotnet test` with zero Unity dependency**, so catalogue and scenario validation runs inside the CI gate that already exists. Parser: `com.unity.nuget.newtonsoft-json` (Unity-blessed; `JsonUtility` has neither dictionaries nor polymorphism, and `mix` and `objectives` need both). ScriptableObjects, if ever wanted, become an *editor-only* view over the JSON — never the source of truth.

**A StatBlock replaces the `GunTiers` mutation.** `GunTiers.Apply(cfg, tier)` writes into a shared `HeroConfig`: no source can be removed, two heroes cannot exist, nothing is comparable. Replacement:

```csharp
public enum Stat { GunDamage, FireInterval, GunRange, MoveSpeed, MaxHealth, Armor,
                   CashPerKill, AirstrikeCooldown, TurretDamage, /* ... */ Count }

public readonly struct StatMod {              // Add applies before Mul, within a stat
    public readonly Stat Stat; public readonly float Add; public readonly float Mul;
    public StatMod(Stat s, float add = 0f, float mul = 1f) { Stat = s; Add = add; Mul = mul; }
}

/// Anything that contributes stats: an equipped item, a contract card, an account
/// perk, a mission-scoped field mod (the crate), the archetype baseline.
public interface IStatSource { string Id { get; } void Collect(ICollection<StatMod> into); }

public sealed class StatResolver {
    private readonly List<IStatSource> _sources = new();
    private readonly List<StatMod> _scratch = new();
    public int Version { get; private set; }                  // bump => consumers re-read

    public void Add(IStatSource s) { _sources.Add(s); Version++; }
    public bool Remove(string id) {
        int i = _sources.FindIndex(s => s.Id == id);
        if (i < 0) return false;
        _sources.RemoveAt(i); Version++; return true;
    }

    /// Deterministic: sources in insertion order, adds summed then muls multiplied.
    public ResolvedStats Resolve(in StatBaseline baseline) {
        Span<float> add = stackalloc float[(int)Stat.Count];
        Span<float> mul = stackalloc float[(int)Stat.Count];
        for (int i = 0; i < (int)Stat.Count; i++) mul[i] = 1f;
        foreach (var src in _sources) {
            _scratch.Clear(); src.Collect(_scratch);
            foreach (var m in _scratch) { add[(int)m.Stat] += m.Add; mul[(int)m.Stat] *= m.Mul; }
        }
        var v = new float[(int)Stat.Count];
        for (int i = 0; i < v.Length; i++) v[i] = (baseline[i] + add[i]) * mul[i];
        return new ResolvedStats(v, Version);
    }
}
```

`HeroConfig` becomes the immutable `StatBaseline`; `HeroModel` holds a `ResolvedStats` and re-reads when `Version` changes. `GunTiers` becomes `GunTierSource : IStatSource` and mutates nothing. Multiply order is fixed by insertion order, so the state hash stays stable.

**`Inventory` — pure, no Unity.** `Inventory(int capacity)` exposing `TryAdd(ItemInstance)`, `TryEquip(int backpackIndex, out ItemInstance? displaced)`, `Unequip(EquipSlot)`, `Sell(int index) → int scrip`, and an `IStatSource` over the equipped set. An `ItemInstance` stores only `{ DefId, Ilvl, Rarity, ulong Seed }` and rolls its affixes deterministically from `IItemCatalog` — saves stay tiny, and a balance patch retro-applies to owned items instead of stranding them.

**`SaveGame` — one file, versioned, atomic.** `save.json` with `"schema": N` at the top. Write `save.json.tmp`, then `File.Replace(tmp, save, save.bak)`. Migration is a forward-only chain of `ISaveMigration { int From { get; } JObject Up(JObject) }` applied in order, each with a checked-in fixture at `tests/fixtures/save-v{n}.json`; a file whose `schema` exceeds the current version is refused rather than guessed at. Per Trap 8, **every economy mutation goes through `IProgressionStore`**, so the co-op/server swap is a constructor argument.

**Seams for fakes:** `IItemCatalog` (defs + affix rolling), `ILootTable` (kill → drop decision, injected RNG), `IScenarioSource` (id → `ScenarioDef`), `IProgressionStore` (load/save/mutate account state).

**Tests** (all EditMode / `dotnet test`, no Unity):

1. `Scenario_TutorialJson_RoundTripsToTodaysWaveTable` — the JSON path is proven before anything depends on it.
2. `Scenario_MissingOptionalFields_FallBackToEconomyAndDirectorDefaults`.
3. `Scenario_UnknownObjectiveType_FailsLoudlyWithScenarioIdInMessage`.
4. `Match_AllObjectivesComplete_TransitionsToWon_WithEnemiesStillAlive` — the Power Yard's central case.
5. `Match_AnyObjectiveFailed_TransitionsToLost_EvenMidWave`.
6. `HoldUntil_AccumulatesOnlyWhileHeroWithinRadius_WhenRadiusNonZero`.
7. `ProtectActors_RequireAliveOne_SurvivesLosingOneTransformer`.
8. `StatResolver_AddsThenMultiplies_InInsertionOrder_Deterministic`.
9. `StatResolver_RemoveSource_RevertsExactly` — the bug `GunTiers` cannot be tested for today.
10. `GunTierSource_ProducesSameNumbersAsLegacyGunTiersApply` — proves the refactor preserves behaviour.
11. `Inventory_EquipDisplacedItem_ReturnsToBackpack_OrFailsWhenFull`.
12. `Inventory_SellEquippedItem_IsRefusedWithoutExplicitUnequip`.
13. `ItemRoller_SameSeedAndDef_ProducesIdenticalAffixes`.
14. `ItemRoller_PowerScoreWithinTwelvePercentOfBudget_AcrossAllSlotsAndRarities` — the balance guard for §3.
15. `LootTable_SapperAlwaysDrops_AtLeastConnected_SpitterAtLeastCorner`.
16. `SaveGame_V1FixtureMigratesToCurrent_AndNewerSchemaIsRefused`.
17. `SaveGame_InterruptedWrite_LeavesPreviousSaveIntact`.

---

## 8. Couch co-op

**Recommendation: split-screen, two players, PC only, post-v1.** Shared-screen with a dynamic zoom-out is a non-starter under ADR-002: the camera is over-the-shoulder and the hero *faces where the camera looks*. Two heroes cannot share that camera without abandoning the fantasy sentence ADR-002 exists to protect. Unity's `PlayerInputManager` does the split natively — enable Split-Screen, set `Camera` on the `PlayerInput` prefab, and it resizes and repositions each player's camera automatically; per-player screen-space UI is scoped with `MultiplayerEventSystem` ([docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/PlayerInputManager.html)). The real cost is not input: **split-screen renders the 500–1,000-agent flood twice.** That, not code, is why local 2P is a PC feature and why the Android perf floor should not carry it.

**What to do now, cheaply:** stop writing singular. Wrap the hero in a `PlayerContext { int Id; HeroModel Hero; StatResolver Stats; Inventory Inventory; BuildCursor Cursor; }` and hold an `IReadOnlyList<PlayerContext>` with `Count == 1`. Make every "is a hero near this" query take an `IHeroQuery` instead of a `Vec2`. A day now, a week later.

**Five assumptions in today's code that have to die:**

1. **`FloodBootstrap._hero` is one field and `HeroSpawn` is a static constant** — hero identity is baked into the bootstrap, not into a player slot.
2. **`BuildModel` fuses one cursor and one selected item with the shared world.** Two players need two cursors over one `GridMap`; split into a per-player `BuildCursor` and a shared `BuildAuthority` that owns validation and placement.
3. **`MatchState.Bank` is a single wallet and `ReportKills(int)` carries no player id.** Even with a shared bank (recommended — it is the syndicate's money), attribution is needed for the payout screen and for `Laundered` affixes that differ per player.
4. **One camera rig, built in `Awake`** (`ChaseDistance` / `ChaseLookHeight` plus a single transform). Rigs must be created per `PlayerInput` prefab instance.
5. **Every proximity query takes a single `Vec2 heroPosition`** — `RepairDrone.Tick`, `PickupSystem.Tick`, `HeroModel.ApplyContact` — and `PickupSystem` also mutates *the* `HeroConfig` in place, which §7's `StatResolver` already fixes.

---

## 9. Risks, cut list, questions

**Risks.** (a) **Content debt, Trap 7 again** — affixes × rarities × slots is a mountain if hand-authored; the §3 budget formula is the mitigation and test 14 the guard rail. (b) **Two currencies confuse people** — never show Scrip in a mission or $ in the safe zone. (c) **Gear surviving a loss makes losing cheap** — medals, not loot, carry the sting; if losing feels free, the lever is "keep drops, lose the reward drops", one line. (d) **Scenario JSON grows into a scripting language** — the objective list is capped at six types until v1 ships; a seventh is a design smell first.

**Cut list, in order.** Cosmetics → account perk tree → Crew Token / permadeath → medals → mid-mission Stash Run → account shop (Scrip just accumulates) → affix jitter. The irreducible v0 is: item drops, an inventory, a between-missions safe zone, one JSON scenario, and the `StatResolver`. That is the two weeks.

**Questions for the owner** (defaults are in force meanwhile).

1. **Does gear survive a mission loss?** Default: yes — drops you already picked up are kept, the mission's reward drops are not. Say the word and losing wipes the mission's loot instead.
2. **Safe zone between missions only, or also mid-mission where a scenario asks for it?** Default: both, with the mid-mission Stash Run declared per scenario (the Power Yard gets one after wave 3).
3. **Pick-1-of-3 cards at wave clear, or a static build tree you commit to at mission start?** Default: cards for v0, tree in v0.2 once the gamepad-tree kill criterion (Open Decision #6) has actually been passed.

---

**Sources:** [Dungeon Defenders — Tavern](https://dungeondefenders.wiki.gg/wiki/Tavern) · [Dungeon Defenders — Tavernkeep](https://dungeondefenders.fandom.com/wiki/Tavernkeep) · [Risk of Rain 2 — Items](https://riskofrain2.fandom.com/wiki/Items) · [Hades — Mirror of Night](https://hades.fandom.com/wiki/Mirror_of_Night) · [Diablo 4 — Equipment](https://diablo4.wiki.fextralife.com/Equipment) · [Deep Rock Galactic — Weapon Overclocks](https://deeprockgalactic.wiki.gg/wiki/Weapon_Overclocks) · [Unity — Player Input Manager](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/PlayerInputManager.html)
