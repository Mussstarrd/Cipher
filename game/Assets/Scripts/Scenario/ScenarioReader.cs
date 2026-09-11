#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Match;
using Cipher.Sim.Grid;

namespace Cipher.Game.Scenarios
{
    /// <summary>
    /// Turns a scenario JSON document into a <see cref="ScenarioDef"/>.
    ///
    /// Strict on purpose. Unknown fields are an error, out-of-range cells are an error, an empty
    /// wave table is an error. Mission files are hand-authored content and the failure mode that
    /// matters is not a malformed file — it is a file that loads and plays subtly wrong, which
    /// costs a playtest to notice and an afternoon to trace. Every message names the JSON path.
    /// </summary>
    public static class ScenarioReader
    {
        /// <summary>Below this the build cursor, the pickup band and the camera have nowhere to go.</summary>
        public const int MinMapSize = 24;
        /// <summary>Above this the grid, the flow field and the minimap texture stop being sane.</summary>
        public const int MaxMapSize = 512;


        public static ScenarioDef Read(string json)
        {
            var root = JsonValue.Parse(json);
            root.RejectUnknownKeys(
                "schema", "id", "displayName", "tier", "brief", "next", "lastStand", "map", "heroSpawn", "spawnCells",
                "vault", "actors", "economy", "director", "waves", "safeZoneAfterWaves",
                "objectives", "rewards", "medals");

            var def = new ScenarioDef();

            def.Schema = root.Get("schema").AsInt();
            if (def.Schema != ScenarioDef.SupportedSchema)
                throw new ScenarioException(
                    $"$.schema: this build reads schema {ScenarioDef.SupportedSchema}, the file says {def.Schema}");

            def.Id = NonEmpty(root.Get("id"), "id");
            def.DisplayName = NonEmpty(root.Get("displayName"), "displayName");
            def.Tier = root.Opt("tier")?.AsInt() ?? 1;
            def.Brief = root.Opt("brief")?.AsString() ?? "";
            def.Next = root.Opt("next")?.AsString() ?? "";
            def.LastStand = root.Opt("lastStand")?.AsBool() ?? false;
            if (def.Next == def.Id)
                throw new ScenarioException("$.next: a position cannot fall back to itself");
            if (def.LastStand && def.Next.Length > 0)
                throw new ScenarioException(
                    "$.lastStand: this position has nowhere behind it, so it cannot also name a 'next'");

            ReadMap(root.Get("map"), def.Map);
            def.HeroSpawn = ReadCell(root.Get("heroSpawn"), def.Map, "heroSpawn");

            foreach (var s in root.Get("spawnCells").Items)
            {
                s.RejectUnknownKeys("x", "y", "gate");
                var cell = ReadCell(s, def.Map, "spawnCells");
                def.SpawnCells.Add(new SpawnPoint(cell.X, cell.Y, s.Opt("gate")?.AsString() ?? ""));
            }
            if (def.SpawnCells.Count == 0)
                throw new ScenarioException("$.spawnCells: a scenario needs at least one spawn point");

            var vault = root.Get("vault");
            vault.RejectUnknownKeys("x", "y", "hp");
            var vaultCell = ReadCell(vault, def.Map, "vault");
            def.Vault = vaultCell;
            def.VaultHp = vault.Opt("hp")?.AsInt() ?? 25;
            if (def.VaultHp < 1) throw new ScenarioException("$.vault.hp: must be at least 1");

            if (root.Opt("actors") is { } actors) ReadActors(actors, def);
            if (root.Opt("economy") is { } economy) ReadEconomy(economy, def);
            if (root.Opt("director") is { } director) ReadDirector(director, def);

            ReadWaves(root.Get("waves"), def);

            if (root.Opt("safeZoneAfterWaves") is { } safe)
                foreach (var v in safe.Items) def.SafeZoneAfterWaves.Add(v.AsInt());

            ReadObjectives(root.Get("objectives"), def);
            if (root.Opt("rewards") is { } rewards) ReadRewards(rewards, def);
            if (root.Opt("medals") is { } medals) ReadMedals(medals, def);

            Validate(def);
            return def;
        }

        /// <summary>
        /// Cross-field checks, once everything is read.
        ///
        /// These are the ones a per-field check cannot make, and they are the ones that produce a
        /// mission that LOADS and then plays wrong, which is the expensive failure. A ClearWaves
        /// count larger than the wave table is the worst of them: the match ends in a win when the
        /// waves run out while the HUD still shows the objective unfinished, so the game's verdict
        /// and the game's own display disagree and neither is obviously the bug.
        /// </summary>
        private static void Validate(ScenarioDef def)
        {
            foreach (var o in def.Objectives)
            {
                if (o.Type != "ClearWaves") continue;
                if (o.Count > def.Waves.Count)
                    throw new ScenarioException(
                        $"$.objectives: ClearWaves asks for {o.Count} waves but only {def.Waves.Count} " +
                        "are defined, so the objective can never complete");
            }

            var occupied = new HashSet<(int, int)>();
            foreach (var w in def.Map.Walls)
                for (int x = w.X; x < w.X + w.Width; x++)
                    for (int y = w.Y; y < w.Y + w.Height; y++)
                        occupied.Add((x, y));

            if (occupied.Contains((def.HeroSpawn.X, def.HeroSpawn.Y)))
                throw new ScenarioException("$.heroSpawn: the hero would start inside a wall");
            if (occupied.Contains((def.Vault.X, def.Vault.Y)))
                throw new ScenarioException("$.vault: the vault is inside a wall, so nothing can reach it");

            var seen = new HashSet<(int, int)>();
            foreach (var sc in def.SpawnCells)
            {
                if (occupied.Contains((sc.X, sc.Y)))
                    throw new ScenarioException(
                        $"$.spawnCells: the gate at ({sc.X},{sc.Y}) is inside a wall");
                if (!seen.Add((sc.X, sc.Y)))
                    throw new ScenarioException(
                        $"$.spawnCells: ({sc.X},{sc.Y}) is listed twice; each gate spawns on its own");
            }

            // The map the sim will build, and the same flow field the game uses. A gate with no
            // route to the vault is a mission where a whole wave stands still, and the cheapest
            // possible place to find that out is here.
            var probe = new GridMap(def.Map.Width, def.Map.Height);
            foreach (var w in def.Map.Walls)
                for (int x = w.X; x < w.X + w.Width; x++)
                    for (int y = w.Y; y < w.Y + w.Height; y++)
                        probe.SetWall(x, y, w.Kind, GridMap.DefaultWallHp);

            var field = new FlowField(probe);
            field.Compute(def.Vault.X, def.Vault.Y);
            foreach (var sc in def.SpawnCells)
                if (!field.HasPath(sc.X, sc.Y))
                    throw new ScenarioException(
                        $"$.spawnCells: no route from the gate at ({sc.X},{sc.Y}) to the vault at " +
                        $"({def.Vault.X},{def.Vault.Y}); the walls seal it off");
        }

        private static void ReadMap(JsonValue map, MapDef into)
        {
            map.RejectUnknownKeys("width", "height", "preset", "walls");
            into.Width = map.Get("width").AsInt();
            into.Height = map.Get("height").AsInt();
            // The lower bound is what the match needs to function; the upper bound is what the
            // machine can allocate. 100000x100000 passes every per-field check and then dies
            // allocating the grid.
            if (into.Width < MinMapSize || into.Height < MinMapSize)
                throw new ScenarioException(
                    $"{map.Path}: a map smaller than {MinMapSize}x{MinMapSize} cannot hold a match");
            if (into.Width > MaxMapSize || into.Height > MaxMapSize)
                throw new ScenarioException(
                    $"{map.Path}: {into.Width}x{into.Height} is past the {MaxMapSize}x{MaxMapSize} ceiling");
            into.Preset = map.Opt("preset")?.AsString() ?? "arena";

            if (map.Opt("walls") is not { } walls) return;
            foreach (var w in walls.Items)
            {
                w.RejectUnknownKeys("rect", "kind");
                var rect = w.Get("rect");
                if (rect.Count != 4)
                    throw new ScenarioException($"{rect.Path}: a rect is [x, y, width, height]");

                int x = rect.Items[0].AsInt(), y = rect.Items[1].AsInt();
                int rw = rect.Items[2].AsInt(), rh = rect.Items[3].AsInt();
                if (rw <= 0 || rh <= 0)
                    throw new ScenarioException($"{rect.Path}: width and height must be positive");
                if (x < 0 || y < 0 || x + rw > into.Width || y + rh > into.Height)
                    throw new ScenarioException(
                        $"{rect.Path}: [{x},{y},{rw},{rh}] falls outside the {into.Width}x{into.Height} map");

                into.Walls.Add(new WallRect(x, y, rw, rh, ReadWallKind(w.Opt("kind"))));
            }
        }

        /// <summary>
        /// Map walls default to <see cref="WallKind.Wall"/>, which is breachable. "Static" in the
        /// design docs means the indestructible kind, which the sim calls Rock.
        /// </summary>
        private static WallKind ReadWallKind(JsonValue? kind)
        {
            if (kind == null) return WallKind.Wall;
            string name = kind.AsString();
            switch (name)
            {
                case "Wall": return WallKind.Wall;
                case "Static":
                case "Rock": return WallKind.Rock;
                case "Barricade": return WallKind.Barricade;
                case "Structure": return WallKind.Structure;
                default:
                    throw new ScenarioException(
                        $"{kind.Path}: unknown wall kind '{name}'. Known: Wall, Static (= Rock), Barricade, Structure");
            }
        }

        private static void ReadActors(JsonValue actors, ScenarioDef def)
        {
            foreach (var a in actors.Items)
            {
                a.RejectUnknownKeys("id", "kind", "x", "y", "hp", "durationSeconds", "requiresHeroWithin");
                var cell = ReadCell(a, def.Map, "actors");
                string kindName = a.Get("kind").AsString();
                if (!Enum.TryParse(kindName, out ActorKind kind))
                    throw new ScenarioException(
                        $"{a.Get("kind").Path}: unknown actor kind '{kindName}'. Known: Structure, Process, Crew");

                def.Actors.Add(new ActorDef
                {
                    Id = NonEmpty(a.Get("id"), "id"),
                    Kind = kind,
                    X = cell.X,
                    Y = cell.Y,
                    Hp = a.Opt("hp")?.AsInt() ?? 100,
                    DurationSeconds = a.Opt("durationSeconds")?.AsFloat() ?? 0f,
                    RequiresHeroWithin = a.Opt("requiresHeroWithin")?.AsFloat() ?? 0f,
                });
            }
        }

        private static void ReadEconomy(JsonValue e, ScenarioDef def)
        {
            e.RejectUnknownKeys("startCash", "cashPerKill", "waveClearBonusPerWave",
                                "barricadeCost", "droneCost");
            var eco = new EconomyConfig();
            eco.StartCash = e.Opt("startCash")?.AsInt() ?? eco.StartCash;
            eco.CashPerKill = e.Opt("cashPerKill")?.AsInt() ?? eco.CashPerKill;
            eco.WaveClearBonusPerWave = e.Opt("waveClearBonusPerWave")?.AsInt() ?? eco.WaveClearBonusPerWave;
            eco.BarricadeCost = e.Opt("barricadeCost")?.AsInt() ?? eco.BarricadeCost;
            eco.DroneCost = e.Opt("droneCost")?.AsInt() ?? eco.DroneCost;
            def.Economy = eco;
        }

        private static void ReadDirector(JsonValue d, ScenarioDef def)
        {
            d.RejectUnknownKeys("seed", "sapperFirstAt", "sapperChance", "spitterFirstAt",
                                "spitterChance", "spitterPity", "hunterShare", "wreckerShare",
                                "maxSappersAlive", "maxSpittersAlive", "maxActiveBreaches");

            var cfg = new DirectorConfig();
            cfg.SapperFirstAt = d.Opt("sapperFirstAt")?.AsFloat() ?? cfg.SapperFirstAt;
            cfg.SapperChance = d.Opt("sapperChance")?.AsFloat() ?? cfg.SapperChance;
            cfg.SpitterFirstAt = d.Opt("spitterFirstAt")?.AsFloat() ?? cfg.SpitterFirstAt;
            cfg.SpitterChance = d.Opt("spitterChance")?.AsFloat() ?? cfg.SpitterChance;
            cfg.SpitterPity = d.Opt("spitterPity")?.AsFloat() ?? cfg.SpitterPity;
            cfg.HunterShare = d.Opt("hunterShare")?.AsFloat() ?? cfg.HunterShare;
            cfg.WreckerShare = d.Opt("wreckerShare")?.AsFloat() ?? cfg.WreckerShare;
            cfg.MaxSappersAlive = d.Opt("maxSappersAlive")?.AsInt() ?? cfg.MaxSappersAlive;
            cfg.MaxSpittersAlive = d.Opt("maxSpittersAlive")?.AsInt() ?? cfg.MaxSpittersAlive;
            cfg.MaxActiveBreaches = d.Opt("maxActiveBreaches")?.AsInt() ?? cfg.MaxActiveBreaches;

            if (cfg.HunterShare + cfg.WreckerShare > 1f)
                throw new ScenarioException(
                    $"{d.Path}: hunterShare + wreckerShare is {cfg.HunterShare + cfg.WreckerShare:0.##}; " +
                    "they are shares of the same population and cannot exceed 1");

            def.Director = cfg;
            if (d.Opt("seed") is { } seed)
            {
                double raw = seed.AsDouble();
                if (raw < 0 || raw > 9007199254740992d)   // 2^53, past which a double is not exact
                    throw new ScenarioException(
                        $"{seed.Path}: a seed must be between 0 and 2^53, so it survives the file exactly");
                def.DirectorSeed = (ulong)raw;
            }
        }

        private static void ReadWaves(JsonValue waves, ScenarioDef def)
        {
            foreach (var w in waves.Items)
            {
                w.RejectUnknownKeys("setupSeconds", "count", "spawnPerSecond", "mix");
                var spec = new WaveSpec
                {
                    SetupSeconds = w.Get("setupSeconds").AsFloat(),
                    Count = w.Get("count").AsInt(),
                    SpawnPerSecond = w.Get("spawnPerSecond").AsFloat(),
                };
                if (spec.Count <= 0) throw new ScenarioException($"{w.Path}.count: must be at least 1");
                if (spec.SpawnPerSecond <= 0f)
                    throw new ScenarioException($"{w.Path}.spawnPerSecond: must be positive, or the wave never arrives");
                if (spec.SetupSeconds < 0f) throw new ScenarioException($"{w.Path}.setupSeconds: cannot be negative");

                if (w.Opt("mix") is { } mix)
                {
                    float total = 0f;
                    foreach (var key in mix.Keys)
                    {
                        float share = mix.Get(key).AsFloat();
                        if (share < 0f) throw new ScenarioException($"{mix.Path}.{key}: a share cannot be negative");
                        spec.Mix[key] = share;
                        total += share;
                    }
                    if (spec.Mix.Count > 0 && Math.Abs(total - 1f) > 0.001f)
                        throw new ScenarioException($"{mix.Path}: shares sum to {total:0.###}, they must sum to 1");
                }

                def.Waves.Add(spec);
            }

            if (def.Waves.Count == 0)
                throw new ScenarioException("$.waves: a scenario needs at least one wave");
        }

        private static void ReadObjectives(JsonValue objectives, ScenarioDef def)
        {
            foreach (var o in objectives.Items)
            {
                o.RejectUnknownKeys("type", "count", "seconds", "minHp", "minAlive", "actorId");
                def.Objectives.Add(new ObjectiveDef
                {
                    Type = NonEmpty(o.Get("type"), "type"),
                    Count = o.Opt("count")?.AsInt() ?? 0,
                    Seconds = o.Opt("seconds")?.AsFloat() ?? 0f,
                    MinHp = o.Opt("minHp")?.AsInt() ?? 0,
                    MinAlive = o.Opt("minAlive")?.AsInt() ?? 0,
                    ActorId = o.Opt("actorId")?.AsString() ?? "",
                });
            }

            if (def.Objectives.Count == 0)
                throw new ScenarioException("$.objectives: a scenario with no objectives cannot be won");

            // Build them now, at load, so an unusable objective is a load failure rather than a
            // mission that starts and turns out to be unwinnable once someone plays it.
            ObjectiveFactory.CreateSet(def.Objectives);
        }

        private static void ReadRewards(JsonValue r, ScenarioDef def)
        {
            r.RejectUnknownKeys("scrip", "guaranteedDrops", "unlocks");
            def.Rewards.Scrip = r.Opt("scrip")?.AsInt() ?? 0;

            if (r.Opt("guaranteedDrops") is { } drops)
                foreach (var d in drops.Items)
                {
                    d.RejectUnknownKeys("slot", "rarity", "ilvl");
                    def.Rewards.GuaranteedDrops.Add(new DropDef
                    {
                        Slot = NonEmpty(d.Get("slot"), "slot"),
                        Rarity = NonEmpty(d.Get("rarity"), "rarity"),
                        ItemLevel = d.Opt("ilvl")?.AsInt() ?? 1,
                    });
                }

            if (r.Opt("unlocks") is { } unlocks)
                foreach (var u in unlocks.Items) def.Rewards.Unlocks.Add(u.AsString());
        }

        private static void ReadMedals(JsonValue m, ScenarioDef def)
        {
            m.RejectUnknownKeys("bronze", "silver", "gold");
            ReadCriteria(m.Opt("bronze"), def.Medals.Bronze);
            ReadCriteria(m.Opt("silver"), def.Medals.Silver);
            ReadCriteria(m.Opt("gold"), def.Medals.Gold);
        }

        private static void ReadCriteria(JsonValue? list, List<Criterion> into)
        {
            if (list == null) return;
            foreach (var c in list.Items)
            {
                c.RejectUnknownKeys("type", "value", "seconds");
                into.Add(new Criterion
                {
                    Type = NonEmpty(c.Get("type"), "type"),
                    Value = c.Opt("value")?.AsInt() ?? 0,
                    Seconds = c.Opt("seconds")?.AsFloat() ?? 0f,
                });
            }
        }

        /// <summary>Reads an {x, y} pair and bounds-checks it against the map.</summary>
        private static Cell ReadCell(JsonValue v, MapDef map, string what)
        {
            int x = v.Get("x").AsInt();
            int y = v.Get("y").AsInt();
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                throw new ScenarioException(
                    $"{v.Path}: {what} at ({x},{y}) is outside the {map.Width}x{map.Height} map");
            return new Cell(x, y);
        }

        private static string NonEmpty(JsonValue v, string what)
        {
            string s = v.AsString();
            if (string.IsNullOrWhiteSpace(s))
                throw new ScenarioException($"{v.Path}: {what} cannot be empty");
            return s;
        }
    }
}
