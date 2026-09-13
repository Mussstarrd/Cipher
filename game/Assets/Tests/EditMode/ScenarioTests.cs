#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Cipher.Game.Scenarios;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The JSON reader. Small, hand-rolled, and the thing every mission file goes through, so it is
    /// tested harder than its size suggests.
    /// </summary>
    public sealed class JsonTests
    {
        [Test]
        public void ParsesTheScalarTypes()
        {
            var v = JsonValue.Parse("{\"a\": 1, \"b\": \"two\", \"c\": true, \"d\": null, \"e\": -2.5e2}");
            Assert.That(v.Get("a").AsInt(), Is.EqualTo(1));
            Assert.That(v.Get("b").AsString(), Is.EqualTo("two"));
            Assert.That(v.Get("c").AsBool(), Is.True);
            Assert.That(v.Get("d").Kind, Is.EqualTo(JsonKind.Null));
            Assert.That(v.Get("e").AsDouble(), Is.EqualTo(-250.0).Within(1e-9));
        }

        [Test]
        public void OptTreatsAnExplicitNullAsAbsent()
        {
            var v = JsonValue.Parse("{\"a\": null}");
            Assert.That(v.Has("a"), Is.True);
            Assert.That(v.Opt("a"), Is.Null, "an explicit null should fall back to the default");
        }

        [Test]
        public void ReadsEscapesIncludingUnicode()
        {
            var v = JsonValue.Parse("{\"s\": \"a\\\"b\\\\c\\nd\\u0041\"}");
            Assert.That(v.Get("s").AsString(), Is.EqualTo("a\"b\\c\ndA"));
        }

        [Test]
        public void ErrorsCarryTheDottedPath()
        {
            var v = JsonValue.Parse("{\"waves\": [ {\"count\": \"lots\"} ]}");
            var ex = Assert.Throws<ScenarioException>(() => v.Get("waves").Items[0].Get("count").AsInt());
            Assert.That(ex!.Message, Does.Contain("$.waves[0].count"));
            Assert.That(ex.Message, Does.Contain("expected a number"));
        }

        [Test]
        public void MissingFieldNamesItself()
        {
            var v = JsonValue.Parse("{\"map\": {}}");
            var ex = Assert.Throws<ScenarioException>(() => v.Get("map").Get("width"));
            Assert.That(ex!.Message, Does.Contain("$.map"));
            Assert.That(ex.Message, Does.Contain("width"));
        }

        [Test]
        public void RejectsTrailingCommasDuplicateKeysAndTrailingContent()
        {
            Assert.Throws<ScenarioException>(() => JsonValue.Parse("{\"a\": 1,}"));
            Assert.Throws<ScenarioException>(() => JsonValue.Parse("[1, 2,]"));
            Assert.Throws<ScenarioException>(() => JsonValue.Parse("{\"a\": 1, \"a\": 2}"));
            Assert.Throws<ScenarioException>(() => JsonValue.Parse("{} junk"));
            Assert.Throws<ScenarioException>(() => JsonValue.Parse("{\"a\": 1"));
        }

        [Test]
        public void ParseErrorsReportLineAndColumn()
        {
            var ex = Assert.Throws<ScenarioException>(() => JsonValue.Parse("{\n  \"a\": ,\n}"));
            Assert.That(ex!.Message, Does.Contain("line 2"));
        }

        [Test]
        public void AWholeNumberIsRequiredWhereAnIntIsExpected()
        {
            var v = JsonValue.Parse("{\"n\": 3.5}");
            var ex = Assert.Throws<ScenarioException>(() => v.Get("n").AsInt());
            Assert.That(ex!.Message, Does.Contain("whole number"));
        }

        [Test]
        public void NumbersDoNotDependOnTheMachineLocale()
        {
            // A comma-decimal locale must not turn 8.5 into 85 or a parse failure. Mission files are
            // machine data and a player in Germany has to read the same numbers as everyone else.
            var previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                var v = JsonValue.Parse("{\"n\": 8.5}");
                Assert.That(v.Get("n").AsDouble(), Is.EqualTo(8.5).Within(1e-9));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void UnknownFieldsAreRejectedByName()
        {
            var v = JsonValue.Parse("{\"count\": 1, \"spawnPerSeconds\": 8}");
            var ex = Assert.Throws<ScenarioException>(() => v.RejectUnknownKeys("count", "spawnPerSecond"));
            Assert.That(ex!.Message, Does.Contain("spawnPerSeconds"));
            Assert.That(ex.Message, Does.Contain("spawnPerSecond"), "the message should show what was meant");
        }
    }

    public sealed class ScenarioReaderTests
    {
        /// <summary>A minimal valid scenario, which each test then breaks in exactly one way.</summary>
        private const string Minimal = @"{
            ""schema"": 1, ""id"": ""t"", ""displayName"": ""T"",
            ""map"": { ""width"": 32, ""height"": 32 },
            ""heroSpawn"": { ""x"": 30, ""y"": 16 },
            ""spawnCells"": [ { ""x"": 1, ""y"": 16, ""gate"": ""west"" } ],
            ""vault"": { ""x"": 31, ""y"": 16, ""hp"": 10 },
            ""waves"": [ { ""setupSeconds"": 10, ""count"": 20, ""spawnPerSecond"": 4 } ],
            ""objectives"": [ { ""type"": ""ClearWaves"", ""count"": 1 } ]
        }";

        [Test]
        public void ReadsTheMinimalScenario()
        {
            var def = ScenarioReader.Read(Minimal);
            Assert.That(def.Id, Is.EqualTo("t"));
            Assert.That(def.Map.Width, Is.EqualTo(32));
            Assert.That(def.HeroSpawn.X, Is.EqualTo(30));
            Assert.That(def.SpawnCells[0].Gate, Is.EqualTo("west"));
            Assert.That(def.VaultHp, Is.EqualTo(10));
            Assert.That(def.Waves, Has.Count.EqualTo(1));
            Assert.That(def.Objectives[0].Type, Is.EqualTo("ClearWaves"));
        }

        [Test]
        public void OmittedSectionsFallBackToTheDefaults()
        {
            var def = ScenarioReader.Read(Minimal);
            Assert.That(def.Economy.StartCash, Is.EqualTo(400));
            Assert.That(def.Director.SapperFirstAt, Is.EqualTo(45f));
            Assert.That(def.Map.Preset, Is.EqualTo("arena"));
            Assert.That(def.Tier, Is.EqualTo(1));
        }

        [Test]
        public void ProducesAWaveTableMatchStateCanUse()
        {
            var table = ScenarioReader.Read(Minimal).ToWaveTable();
            Assert.That(table, Has.Count.EqualTo(1));
            Assert.That(table[0].Count, Is.EqualTo(20));
            Assert.That(table[0].SpawnPerSecond, Is.EqualTo(4f));
            Assert.That(table[0].SetupSeconds, Is.EqualTo(10f));
        }

        [Test]
        public void RejectsAFutureSchema()
        {
            var ex = Assert.Throws<ScenarioException>(
                () => ScenarioReader.Read(Minimal.Replace("\"schema\": 1", "\"schema\": 2")));
            Assert.That(ex!.Message, Does.Contain("schema"));
        }

        [Test]
        public void RejectsCellsOutsideTheMap()
        {
            var ex = Assert.Throws<ScenarioException>(
                () => ScenarioReader.Read(Minimal.Replace("\"x\": 30, \"y\": 16", "\"x\": 99, \"y\": 16")));
            Assert.That(ex!.Message, Does.Contain("heroSpawn"));
            Assert.That(ex.Message, Does.Contain("32x32"));
        }

        [Test]
        public void RejectsAWallRectThatLeavesTheMap()
        {
            string json = Minimal.Replace(
                "\"map\": { \"width\": 32, \"height\": 32 }",
                "\"map\": { \"width\": 32, \"height\": 32, \"walls\": [ { \"rect\": [30, 0, 4, 4] } ] }");
            var ex = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(json));
            Assert.That(ex!.Message, Does.Contain("outside"));
        }

        [Test]
        public void StaticWallsBecomeRockAndTheDefaultIsBreachable()
        {
            string json = Minimal.Replace(
                "\"map\": { \"width\": 32, \"height\": 32 }",
                "\"map\": { \"width\": 32, \"height\": 32, \"walls\": [ " +
                "{ \"rect\": [4, 4, 2, 2], \"kind\": \"Static\" }, { \"rect\": [8, 8, 2, 2] } ] }");
            var def = ScenarioReader.Read(json);
            Assert.That(def.Map.Walls[0].Kind, Is.EqualTo(WallKind.Rock));
            Assert.That(def.Map.Walls[1].Kind, Is.EqualTo(WallKind.Wall),
                        "an unqualified map wall should be breachable, or sappers have nothing to do");
        }

        [Test]
        public void RejectsAWaveThatNeverArrives()
        {
            var ex = Assert.Throws<ScenarioException>(
                () => ScenarioReader.Read(Minimal.Replace("\"spawnPerSecond\": 4", "\"spawnPerSecond\": 0")));
            Assert.That(ex!.Message, Does.Contain("spawnPerSecond"));
        }

        [Test]
        public void RejectsAMixThatDoesNotSumToOne()
        {
            string json = Minimal.Replace(
                "\"spawnPerSecond\": 4",
                "\"spawnPerSecond\": 4, \"mix\": { \"Runner\": 0.5, \"Spitter\": 0.2 }");
            var ex = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(json));
            Assert.That(ex!.Message, Does.Contain("sum to 1"));
        }

        [Test]
        public void RejectsSharesThatOverlapTheWholePopulation()
        {
            string json = Minimal.Replace(
                "\"waves\"",
                "\"director\": { \"hunterShare\": 0.7, \"wreckerShare\": 0.6 }, \"waves\"");
            var ex = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(json));
            Assert.That(ex!.Message, Does.Contain("cannot exceed 1"));
        }

        [Test]
        public void RejectsAScenarioWithNoWayToWin()
        {
            var ex = Assert.Throws<ScenarioException>(
                () => ScenarioReader.Read(Minimal.Replace(
                    "[ { \"type\": \"ClearWaves\", \"count\": 1 } ]", "[]")));
            Assert.That(ex!.Message, Does.Contain("objectives"));
        }

        [Test]
        public void AnObjectiveNamingAnActorTheMissionLacksIsRejected()
        {
            // Loading a mission whose objective can never complete produces an unwinnable level that
            // looks like a balance problem. Refusing at load is far cheaper to diagnose.
            var ex = Assert.Throws<ScenarioException>(
                () => ScenarioReader.Read(Minimal.Replace(
                    "{ \"type\": \"ClearWaves\", \"count\": 1 }",
                    "{ \"type\": \"HoldUntil\", \"actorId\": \"gen-1\" }")));
            Assert.That(ex!.Message, Does.Contain("HoldUntil"));
            Assert.That(ex.Message, Does.Contain("gen-1"));
        }

        [Test]
        public void RejectsAnUnknownObjectiveType()
        {
            var ex = Assert.Throws<ScenarioException>(
                () => ScenarioReader.Read(Minimal.Replace("\"ClearWaves\"", "\"ClearWave\"")));
            Assert.That(ex!.Message, Does.Contain("ClearWave"));
        }

        [Test]
        public void RejectsATypoedFieldInsteadOfIgnoringIt()
        {
            var ex = Assert.Throws<ScenarioException>(
                () => ScenarioReader.Read(Minimal.Replace("\"tier\"", "\"teir\"")
                                                 .Replace("\"displayName\": \"T\"",
                                                          "\"displayName\": \"T\", \"teir\": 2")));
            Assert.That(ex!.Message, Does.Contain("teir"));
        }

        [Test]
        public void ParsesActorsEvenThoughNothingConsumesThemYet()
        {
            string json = Minimal.Replace(
                "\"waves\"",
                "\"actors\": [ { \"id\": \"gen-1\", \"kind\": \"Process\", \"x\": 10, \"y\": 10, " +
                "\"durationSeconds\": 480, \"requiresHeroWithin\": 6 } ], \"waves\"");
            var def = ScenarioReader.Read(json);
            Assert.That(def.Actors, Has.Count.EqualTo(1));
            Assert.That(def.Actors[0].Kind, Is.EqualTo(ActorKind.Process));
            Assert.That(def.Actors[0].DurationSeconds, Is.EqualTo(480f));
        }

        [Test]
        public void ParsesRewardsAndMedals()
        {
            string json = Minimal.Replace(
                "\"waves\"",
                "\"rewards\": { \"scrip\": 60, \"guaranteedDrops\": [ { \"slot\": \"Vest\", " +
                "\"rarity\": \"Scavenged\", \"ilvl\": 3 } ], \"unlocks\": [ \"x\" ] }, " +
                "\"medals\": { \"gold\": [ { \"type\": \"TimeUnder\", \"seconds\": 300 } ] }, \"waves\"");
            var def = ScenarioReader.Read(json);
            Assert.That(def.Rewards.Scrip, Is.EqualTo(60));
            Assert.That(def.Rewards.GuaranteedDrops[0].Slot, Is.EqualTo("Vest"));
            Assert.That(def.Rewards.Unlocks, Does.Contain("x"));
            Assert.That(def.Medals.Gold[0].Seconds, Is.EqualTo(300f));
        }
    }

    /// <summary>
    /// The guard that matters most: every mission that ships must load. A broken content file is
    /// otherwise found by a player, not by CI.
    /// </summary>
    public sealed class ShippedScenarioTests
    {
        private static IEnumerable<string> ScenarioFiles()
        {
            string dir = Path.Combine(Path.Combine(Path.Combine("Assets", "Resources"), "Scenarios"));
            return Directory.Exists(dir) ? Directory.GetFiles(dir, "*.json") : new string[0];
        }

        [Test]
        public void AtLeastOneScenarioShips()
        {
            Assert.That(ScenarioFiles(), Is.Not.Empty,
                        "Assets/Resources/Scenarios should hold the campaign's mission files");
        }

        [Test, TestCaseSource(nameof(ScenarioFiles))]
        public void EveryShippedScenarioLoads(string path)
        {
            var def = ScenarioReader.Read(File.ReadAllText(path));

            Assert.That(def.Id, Is.EqualTo(Path.GetFileNameWithoutExtension(path)),
                        "a mission's id should match its filename, so a brief can name the file");
            Assert.That(def.Waves, Is.Not.Empty);

            // The objectives must be buildable, and the objective set must be winnable: a set of
            // nothing but fail-conditions is a mission you can only lose.
            var set = ObjectiveFactory.CreateSet(def.Objectives);
            bool hasGoal = false;
            foreach (var o in set.All) if (o is not ProtectVault) hasGoal = true;
            Assert.That(hasGoal, Is.True, $"{def.Id} has no objective that can ever complete");
        }

        [Test, TestCaseSource(nameof(ScenarioFiles))]
        public void EveryFallbackPositionExists(string path)
        {
            // "next" is followed at runtime the moment a player leaves a position, so a typo here
            // is a crash in the middle of a session rather than at load.
            var def = ScenarioReader.Read(File.ReadAllText(path));
            if (def.Next.Length == 0) return;

            string next = Path.Combine(Path.GetDirectoryName(path)!, def.Next + ".json");
            Assert.That(File.Exists(next), Is.True,
                        $"{def.Id} falls back to '{def.Next}', which is not a mission file");
        }

        /// <summary>
        /// EVERY GATE MUST STILL HAVE A ROUTE ONCE THE BUILDINGS ARE ON THE MAP.
        ///
        /// `ScenarioReader.Validate` runs a flow field from the vault and refuses a file whose
        /// WALLS seal a gate — but props became solid on 2026-09-12 and the reader's probe never
        /// learned about them. So a clubhouse dropped across the only corridor loads perfectly
        /// clean, and the fault appears as a wave that spawns and then mills about, which is about
        /// the worst symptom a content bug can have: no error, no crash, just a level that is
        /// "wrong somehow" in the middle of a playtest.
        ///
        /// This is the reader's probe again with <see cref="PropCatalog"/> applied on top, in the
        /// same order and with the same rules the bootstrap uses (already-solid cells are skipped,
        /// and the KeepClear lane is never built on). It is the guard that lets a mission be
        /// authored with real architecture without the author having to hold the whole flow field
        /// in their head.
        /// </summary>
        [Test, TestCaseSource(nameof(ScenarioFiles))]
        public void PropsNeverSealAGate(string path)
        {
            var def = ScenarioReader.Read(File.ReadAllText(path));
            var map = BuildSolidMap(def);

            var field = new FlowField(map);
            field.Compute(def.Vault.X, def.Vault.Y);

            foreach (var sc in def.SpawnCells)
                Assert.That(field.HasPath(sc.X, sc.Y), Is.True,
                    $"{def.Id}: the buildings seal the gate '{sc.Gate}' at ({sc.X},{sc.Y}). " +
                    "The file loads because the reader only probes walls; this is the check that "
                    + "would have caught it.");

            Assert.That(map.KindAt(def.Vault.X, def.Vault.Y), Is.EqualTo(WallKind.None),
                $"{def.Id}: a prop is standing on the truck");
            Assert.That(field.HasPath(def.HeroSpawn.X, def.HeroSpawn.Y), Is.True,
                $"{def.Id}: the hero cannot walk to the truck");
        }

        /// <summary>
        /// The walls and the prop footprints, in the order and with the rules the bootstrap uses.
        ///
        /// Shared by the seal test and the P3 merge test so the two can never disagree about what
        /// is solid — which would be the worst possible way for either of them to be wrong.
        /// </summary>
        private static GridMap BuildSolidMap(ScenarioDef def)
        {
            var map = new GridMap(def.Map.Width, def.Map.Height);

            foreach (var w in def.Map.Walls)
                for (int x = w.X; x < w.X + w.Width; x++)
                    for (int y = w.Y; y < w.Y + w.Height; y++)
                        map.SetWall(x, y, w.Kind, GridMap.DefaultWallHp);

            foreach (var prop in def.Props)
                foreach (var (x, y) in PropCatalog.Footprint(prop.Kind, prop.X, prop.Y, prop.Yaw))
                {
                    if (x < 0 || y < 0 || x >= def.Map.Width || y >= def.Map.Height) continue;
                    if (map.KindAt(x, y) != WallKind.None) continue;
                    if (KeepClear(def, x, y)) continue;
                    map.SetWall(x, y, WallKind.Rock, GridMap.DefaultWallHp);
                }

            return map;
        }

        /// <summary>
        /// Design pillar P3: every position owes the area tower a place where lanes actually merge.
        ///
        /// The Brush Hog is `FireMode.Area` with a **2.4 metre** reach. On a long straight lane it is
        /// strictly worse than a Sentry, everywhere, all game — and a tower family that is never the
        /// right answer is a documented sign of a badly balanced tower defence. So a position that
        /// offers it nowhere to be is not a neutral position; it is one where a third of the build
        /// bar is decoration.
        ///
        /// WHY "AWAY FROM THE GOAL" IS THE WHOLE TEST. Every path ends at the truck, so the truck is
        /// trivially a merge point and asserting on it would pass forever while proving nothing. What
        /// makes the area family interesting is a merge OUT IN THE FIELD — somewhere the player can
        /// choose to hold, rather than the last two cells before they lose. Six cells is the line:
        /// far enough that holding it is a decision, close enough to be reachable on a first build.
        ///
        /// This measures the real thing rather than a proxy: it walks the actual flow field the
        /// actual sim will use, from every authored gate, and asks which cells more than one gate's
        /// traffic crosses.
        /// </summary>
        [Test, TestCaseSource(nameof(ScenarioFiles))]
        public void EveryPositionOwesTheAreaTowerAMergePoint(string path)
        {
            var def = ScenarioReader.Read(File.ReadAllText(path));
            var map = BuildSolidMap(def);

            var field = new FlowField(map);
            field.Compute(def.Vault.X, def.Vault.Y);

            // Which gates' traffic crosses each cell.
            var crossedBy = new Dictionary<(int X, int Y), HashSet<int>>();
            for (int i = 0; i < def.SpawnCells.Count; i++)
            {
                var sc = def.SpawnCells[i];
                foreach (var cell in WalkToGoal(map, field, sc.X, sc.Y, def.Vault.X, def.Vault.Y))
                {
                    if (!crossedBy.TryGetValue(cell, out var set))
                        crossedBy[cell] = set = new HashSet<int>();
                    set.Add(i);
                }
            }

            const int MinCellsFromGoal = 6;
            (int X, int Y) best = (-1, -1);
            int bestGates = 0, bestDistance = 0;

            foreach (var (cell, gates) in crossedBy)
            {
                if (gates.Count < 2) continue;
                int dx = cell.X - def.Vault.X, dy = cell.Y - def.Vault.Y;
                int distance = (int)System.Math.Sqrt(dx * dx + dy * dy);
                if (distance < MinCellsFromGoal) continue;

                // Prefer more lanes; break ties by the merge that happens furthest out, because a
                // player who can hold it early keeps more of the position.
                if (gates.Count > bestGates || (gates.Count == bestGates && distance > bestDistance))
                {
                    best = cell; bestGates = gates.Count; bestDistance = distance;
                }
            }

            Assert.That(bestGates, Is.GreaterThanOrEqualTo(2),
                $"{def.Id}: no two gates' routes meet more than {MinCellsFromGoal} cells from the truck, " +
                "so the Brush Hog has nowhere on this position where an area weapon beats a Sentry. " +
                "See docs/design/level-design-principles.md, pillar P3.");

            UnityEngine.Debug.Log($"[P3] {def.Id}: best merge at ({best.X},{best.Y}) — " +
                                  $"{bestGates} of {def.SpawnCells.Count} gates, {bestDistance} cells from the truck");
        }

        /// <summary>
        /// Design pillar P4: every approach can be covered from somewhere you are allowed to build.
        ///
        /// The pillar is informational, not compositional — "leading lines" are folklore, and any
        /// corridor produces perspective convergence whether a designer meant it or not. The real
        /// question is whether a player standing on buildable ground can SEE what is coming down the
        /// lane that ground covers. A slot with no sightline is a guess, and guessing is not strategy.
        ///
        /// So this asks the question the way the game will: it places a notional Sentry on every
        /// buildable cell near the truck and counts how much of each gate's approach that cell can
        /// actually see, using <see cref="Movement.FirstBlockedCell"/> — the same march the turrets
        /// use. A lane nothing can see is a lane emplacements cannot defend, which quietly turns a
        /// tower defence into a game about standing in the right place personally.
        ///
        /// It reports the number as well as asserting on it, because the interesting use is
        /// comparative: the doc's escalation lever #3 is "later positions get SHORTER engagement
        /// lanes", and that is this number going down on purpose.
        /// </summary>
        [Test, TestCaseSource(nameof(ScenarioFiles))]
        public void EveryApproachCanBeSeenFromBuildableGround(string path)
        {
            var def = ScenarioReader.Read(File.ReadAllText(path));
            var map = BuildSolidMap(def);

            var field = new FlowField(map);
            field.Compute(def.Vault.X, def.Vault.Y);

            // Sentry reach, from TurretCatalog. Hard-coded rather than referenced because the test
            // is about the SHAPE of the ground, and it should not start passing because someone
            // buffed a tower's range.
            const float SentryRange = 10f;
            const int DefensibleRadius = 26;   // how far out a first build realistically reaches
            const int MinCovered = 5;

            foreach (var sc in def.SpawnCells)
            {
                // The part of this gate's route that is close enough to the truck to be worth
                // defending. Covering the far end of a forty-cell walk is not a real option.
                var approach = new List<(int X, int Y)>();
                foreach (var cell in WalkToGoal(map, field, sc.X, sc.Y, def.Vault.X, def.Vault.Y))
                {
                    int dx = cell.X - def.Vault.X, dy = cell.Y - def.Vault.Y;
                    if (dx * dx + dy * dy <= DefensibleRadius * DefensibleRadius) approach.Add(cell);
                }

                int best = 0;
                (int X, int Y) bestSlot = (-1, -1);

                for (int y = def.Vault.Y - DefensibleRadius; y <= def.Vault.Y + DefensibleRadius; y++)
                for (int x = def.Vault.X - DefensibleRadius; x <= def.Vault.X + DefensibleRadius; x++)
                {
                    if (x < 0 || y < 0 || x >= def.Map.Width || y >= def.Map.Height) continue;
                    if (map.KindAt(x, y) != WallKind.None) continue;   // cannot build in a building

                    int seen = 0;
                    var from = new Vec2(x + 0.5f, y + 0.5f);

                    foreach (var cell in approach)
                    {
                        var to = new Vec2(cell.X + 0.5f, cell.Y + 0.5f);
                        var delta = new Vec2(to.X - from.X, to.Y - from.Y);
                        float distance = delta.Length;
                        if (distance > SentryRange) continue;

                        if (!Movement.FirstBlockedCell(map, from, delta, distance, out _, out _, out _))
                            seen++;
                    }

                    if (seen > best) { best = seen; bestSlot = (x, y); }
                }

                Assert.That(best, Is.GreaterThanOrEqualTo(MinCovered),
                    $"{def.Id}: the approach from '{sc.Gate}' cannot be covered — the best buildable " +
                    $"cell near the truck sees only {best} cells of it. A lane no emplacement can see " +
                    "is a lane only the player's own body can hold. See level-design-principles.md, P4.");

                UnityEngine.Debug.Log($"[P4] {def.Id} / {sc.Gate}: best slot ({bestSlot.X},{bestSlot.Y}) " +
                                      $"covers {best} of {approach.Count} approach cells");
            }
        }

        /// <summary>
        /// Walks the flow field from a cell to the goal by DESCENDING INTEGRATION COST.
        ///
        /// Cost descent rather than following <c>DirectionAt</c>: the direction is a normalised float
        /// vector meant for steering an agent, and rounding it to a neighbour every step accumulates
        /// error into a path that drifts off the one the crowd would really take. The integration
        /// cost is the field's own ground truth and stepping down it cannot drift.
        /// </summary>
        private static IEnumerable<(int X, int Y)> WalkToGoal(GridMap map, FlowField field,
                                                              int startX, int startY, int goalX, int goalY)
        {
            int x = startX, y = startY;
            int guard = map.Width * map.Height;

            while (guard-- > 0)
            {
                yield return (x, y);
                if (x == goalX && y == goalY) yield break;

                float best = field.IntegrationCostAt(x, y);
                int bx = -1, by = -1;

                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= map.Width || ny >= map.Height) continue;
                    if (map.KindAt(nx, ny) != WallKind.None) continue;

                    float c = field.IntegrationCostAt(nx, ny);
                    if (c < best) { best = c; bx = nx; by = ny; }
                }

                if (bx < 0) yield break;   // a local minimum; the seal test owns that case
                x = bx; y = by;
            }
        }

        /// <summary>
        /// FloodBootstrap.KeepClear, restated. Duplicated rather than shared because the bootstrap's
        /// version reads live hero state and a MonoBehaviour field; what matters is that the two
        /// agree about the two rules that are about the MAP, which are the only two that can decide
        /// whether a gate is sealed.
        /// </summary>
        private static bool KeepClear(ScenarioDef def, int x, int y)
        {
            if (System.Math.Abs(y - def.Map.Height / 2) <= 3) return true;
            if (x <= 4 || x >= def.Map.Width - 5) return true;
            return System.Math.Abs(x - def.HeroSpawn.X) <= 3 && System.Math.Abs(y - def.HeroSpawn.Y) <= 3;
        }
    }

    public sealed class ObjectiveTests
    {
        private static ObjectiveContext Ctx(float seconds = 0f, int wavesCleared = 0,
                                            int vaultHp = 25, int vaultMax = 25) =>
            new ObjectiveContext(seconds, 0, wavesCleared, 0, vaultHp, vaultMax);

        [Test]
        public void ClearWavesCompletesOnTheTargetCount()
        {
            var o = new ClearWaves(3);
            Assert.That(o.Tick(Ctx(wavesCleared: 2), 1f), Is.EqualTo(ObjectiveState.Pending));
            Assert.That(o.Progress01, Is.EqualTo(2f / 3f).Within(1e-5));
            Assert.That(o.Tick(Ctx(wavesCleared: 3), 1f), Is.EqualTo(ObjectiveState.Complete));
            Assert.That(o.Hud, Is.EqualTo("WAVES  3/3"));
        }

        [Test]
        public void SurviveSecondsReadsTheMatchClockRatherThanAccumulating()
        {
            var o = new SurviveSeconds(60f);
            // A single tick carrying a big match time must complete it: the objective must agree
            // with the clock the HUD shows, not with the sum of the frames it happened to see.
            Assert.That(o.Tick(Ctx(seconds: 59f), 0.016f), Is.EqualTo(ObjectiveState.Pending));
            Assert.That(o.Hud, Is.EqualTo("HOLD  0:01"));
            Assert.That(o.Tick(Ctx(seconds: 60f), 0.016f), Is.EqualTo(ObjectiveState.Complete));
        }

        [Test]
        public void ProtectVaultFailsOnlyBelowTheFloor()
        {
            var o = new ProtectVault(1);
            Assert.That(o.Tick(Ctx(vaultHp: 1), 1f), Is.EqualTo(ObjectiveState.Pending));
            Assert.That(o.Tick(Ctx(vaultHp: 0), 1f), Is.EqualTo(ObjectiveState.Failed));
        }

        [Test]
        public void AVaultObjectiveDoesNotMakeTheMissionUnwinnable()
        {
            // ProtectVault never completes, so counting it as a goal would make every mission that
            // has one impossible to finish. That bug reads as a balance problem for a week.
            var set = ObjectiveFactory.CreateSet(new[]
            {
                new ObjectiveDef { Type = "ClearWaves", Count = 1 },
                new ObjectiveDef { Type = "ProtectVault", MinHp = 1 },
            });

            set.Tick(Ctx(wavesCleared: 1), 1f);
            Assert.That(set.IsComplete, Is.True);
            Assert.That(set.IsFailed, Is.False);
        }

        [Test]
        public void EveryGoalMustCompleteBeforeTheSetDoes()
        {
            var set = ObjectiveFactory.CreateSet(new[]
            {
                new ObjectiveDef { Type = "ClearWaves", Count = 2 },
                new ObjectiveDef { Type = "SurviveSeconds", Seconds = 100f },
            });

            set.Tick(Ctx(seconds: 100f, wavesCleared: 1), 1f);
            Assert.That(set.IsComplete, Is.False, "one goal left");

            set.Tick(Ctx(seconds: 100f, wavesCleared: 2), 1f);
            Assert.That(set.IsComplete, Is.True);
        }

        [Test]
        public void AFailureIsStickyAndNamesTheObjective()
        {
            var set = ObjectiveFactory.CreateSet(new[]
            {
                new ObjectiveDef { Type = "ClearWaves", Count = 1 },
                new ObjectiveDef { Type = "ProtectVault", MinHp = 1 },
            });

            set.Tick(Ctx(vaultHp: 0), 1f);
            Assert.That(set.IsFailed, Is.True);
            Assert.That(set.FailedBy, Is.TypeOf<ProtectVault>());

            // The vault coming back up, or the waves being cleared afterwards, does not undo it.
            set.Tick(Ctx(wavesCleared: 5, vaultHp: 25), 1f);
            Assert.That(set.IsFailed, Is.True);
            Assert.That(set.IsComplete, Is.False);
        }

        [Test]
        public void RejectsNonsenseParameters()
        {
            Assert.Throws<ScenarioException>(() => new ClearWaves(0));
            Assert.Throws<ScenarioException>(() => new SurviveSeconds(0f));
            Assert.Throws<ScenarioException>(() => new ObjectiveSet(new IObjective[0]));
        }
    }

    /// <summary>The seam where a scenario's objectives take over the match's verdict.</summary>
    public sealed class ObjectiveVerdictTests
    {
        private static Cipher.Game.Match.MatchState NewMatch(bool hasFallback = true) =>
            new Cipher.Game.Match.MatchState(
                new[] { new Cipher.Game.Match.WaveDef(10, 5f, 1f) },
                new Cipher.Game.Match.EconomyConfig(),
                vaultHp: 5,
                hasFallbackPosition: hasFallback);

        [Test]
        public void CompletingTheObjectivesUnlocksTheExitWithoutForcingIt()
        {
            // ADR-005 is explicit that leaving is the PLAYER's call, and that decision is the whole
            // risk curve of a mission. The first version dropped him into the pack-up window the
            // instant the objective ticked over, which he ran into and did not understand.
            var match = NewMatch();
            match.CompleteByObjective();

            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Setup),
                        "finishing the job must not shove him out of the door");
            Assert.That(match.ObjectivesMet, Is.True);
            Assert.That(match.CanDeclareLastWave, Is.True,
                        "but he may now leave whenever he likes");
        }

        [Test]
        public void FinishingTheJobUnlocksTheExitEvenBelowTheWaveMinimum()
        {
            // The minimum exists to stop wave-one bailing, not to trap someone already finished.
            var match = NewMatch();
            Assert.That(match.CanDeclareLastWave, Is.False, "nothing cleared yet");

            match.CompleteByObjective();
            Assert.That(match.CanDeclareLastWave, Is.True);
        }

        [Test]
        public void CompletingAgainEveryTickChangesNothing()
        {
            var match = NewMatch();
            match.CompleteByObjective();
            match.DeclareLastWave();

            // Clear the declared wave to open the window, then keep completing.
            match.StartWaveNow();
            for (int i = 0; i < 200 && match.Phase != Cipher.Game.Match.MatchPhase.Extraction; i++)
                match.Tick(0.1f, aliveRunners: 0, breachedThisTick: 0);

            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Extraction));
            float first = match.ExtractTimeLeft;

            match.Tick(3f, aliveRunners: 0, breachedThisTick: 0);
            match.CompleteByObjective();

            Assert.That(match.ExtractTimeLeft, Is.LessThan(first),
                        "the pack-up clock keeps running; objectives stay complete every tick");
        }

        [Test]
        public void AnObjectiveFailureEndsTheMatchEvenMidExtraction()
        {
            // The vault falling while you are loading the truck is still the vault falling.
            var match = NewMatch();
            match.CompleteByObjective();
            match.LoseByObjective();
            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Lost));
        }

        [Test]
        public void AnOutcomeIsFinal()
        {
            var match = NewMatch();
            match.LoseByObjective();
            match.CompleteByObjective();
            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Lost),
                        "a later completion must not rewrite a loss");
        }

        [Test]
        public void TheShippedGateMissionCanBeFinishedAndLeft()
        {
            // The Gate asks for two of its five waves. Finishing them unlocks the exit; taking more
            // is the player buying cash and experience out of the same scan cycle.
            var def = ScenarioReader.Read(
                File.ReadAllText(Path.Combine("Assets", "Resources", "Scenarios", "act1-01-the-gate.json")));
            var set = ObjectiveFactory.CreateSet(def.Objectives);
            var match = new Cipher.Game.Match.MatchState(def.ToWaveTable(), def.Economy, def.VaultHp);

            set.Tick(new ObjectiveContext(0f, 1, 2, 0, def.VaultHp, def.VaultHp), 1f);
            Assert.That(set.IsComplete, Is.True);

            match.CompleteByObjective();
            Assert.That(match.ObjectivesMet, Is.True);
            Assert.That(match.CanDeclareLastWave, Is.True);
            Assert.That(def.Waves.Count, Is.GreaterThan(2), "and he may keep going for more");
        }
    }

    /// <summary>
    /// Inputs that must fail as a ScenarioException rather than as a crash, a hang, or silence.
    /// </summary>
    public sealed class ScenarioHardeningTests
    {
        [Test]
        public void DeeplyNestedInputIsAParseErrorRatherThanAStackOverflow()
        {
            // A StackOverflowException cannot be caught in .NET; it takes the process with it. This
            // is the one input class that could defeat "a bad file is always a parse error".
            string bomb = new string('[', 50000);
            Assert.Throws<ScenarioException>(() => JsonValue.Parse(bomb));
        }

        [TestCase("+5")]
        [TestCase(".5")]
        [TestCase("5.")]
        [TestCase("01")]
        [TestCase("-")]
        [TestCase("1e")]
        [TestCase("1e+")]
        public void MalformedNumbersAreRejected(string literal)
        {
            Assert.Throws<ScenarioException>(() => JsonValue.Parse("{\"n\": " + literal + "}"),
                                             $"'{literal}' is not JSON and must not be accepted");
        }

        [Test]
        public void AnOverflowingNumberIsRejectedRatherThanBecomingInfinity()
        {
            // double.TryParse returns true on overflow, so 1e999 used to load as +Infinity and flow
            // into a spawn rate.
            Assert.Throws<ScenarioException>(() => JsonValue.Parse("{\"n\": 1e999}"));
        }

        [Test]
        public void UnicodeEscapesTakeExactlyFourHexDigits()
        {
            // NumberStyles.HexNumber allows surrounding whitespace, so "\u 41 " read as "A".
            Assert.Throws<ScenarioException>(() => JsonValue.Parse("{\"s\": \"\\u 41 \"}"));
            Assert.That(JsonValue.Parse("{\"s\": \"\\u0041\"}").Get("s").AsString(), Is.EqualTo("A"));
        }

        [Test]
        public void ARawNewlineInAStringIsRejected()
        {
            Assert.Throws<ScenarioException>(() => JsonValue.Parse("{\"s\": \"a\nb\"}"));
        }
    }

    /// <summary>
    /// Cross-field validation: the scenarios that LOAD and then play wrong, which is the expensive
    /// kind of broken.
    /// </summary>
    public sealed class ScenarioValidationTests
    {
        private const string Base = @"{
            ""schema"": 1, ""id"": ""t"", ""displayName"": ""T"",
            ""map"": { ""width"": 32, ""height"": 32, ""walls"": [] },
            ""heroSpawn"": { ""x"": 30, ""y"": 16 },
            ""spawnCells"": [ { ""x"": 1, ""y"": 16 } ],
            ""vault"": { ""x"": 31, ""y"": 16, ""hp"": 10 },
            ""waves"": [ { ""setupSeconds"": 10, ""count"": 20, ""spawnPerSecond"": 4 } ],
            ""objectives"": [ { ""type"": ""ClearWaves"", ""count"": 1 } ]
        }";

        [Test]
        public void AnObjectiveAskingForMoreWavesThanExistIsRejected()
        {
            // Left unchecked, MatchState wins when the waves run out while the HUD still shows the
            // objective unfinished: the verdict and the display disagree and neither looks wrong.
            var ex = Assert.Throws<ScenarioException>(
                () => ScenarioReader.Read(Base.Replace("\"count\": 1", "\"count\": 7")));
            Assert.That(ex!.Message, Does.Contain("never complete"));
        }

        [Test]
        public void AGateWalledOffFromTheVaultIsRejected()
        {
            string json = Base.Replace("\"walls\": []",
                "\"walls\": [ { \"rect\": [10, 0, 1, 32] } ]");
            var ex = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(json));
            Assert.That(ex!.Message, Does.Contain("no route"));
        }

        [Test]
        public void AWallAcrossTheMapWithAGapIsFine()
        {
            string json = Base.Replace("\"walls\": []",
                "\"walls\": [ { \"rect\": [10, 0, 1, 14] }, { \"rect\": [10, 18, 1, 14] } ]");
            Assert.DoesNotThrow(() => ScenarioReader.Read(json));
        }

        [Test]
        public void SpawningOrDefendingInsideAWallIsRejected()
        {
            var ex = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(
                Base.Replace("\"walls\": []", "\"walls\": [ { \"rect\": [1, 16, 1, 1] } ]")));
            Assert.That(ex!.Message, Does.Contain("inside a wall"));

            var ex2 = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(
                Base.Replace("\"walls\": []", "\"walls\": [ { \"rect\": [30, 16, 1, 1] } ]")));
            Assert.That(ex2!.Message, Does.Contain("hero"));
        }

        [Test]
        public void ADuplicatedGateIsRejected()
        {
            var ex = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(
                Base.Replace("[ { \"x\": 1, \"y\": 16 } ]",
                             "[ { \"x\": 1, \"y\": 16 }, { \"x\": 1, \"y\": 16 } ]")));
            Assert.That(ex!.Message, Does.Contain("twice"));
        }

        [Test]
        public void AbsurdMapSizesAreRejectedAtBothEnds()
        {
            Assert.Throws<ScenarioException>(() => ScenarioReader.Read(
                Base.Replace("\"width\": 32, \"height\": 32", "\"width\": 10, \"height\": 10")));
            Assert.Throws<ScenarioException>(() => ScenarioReader.Read(
                Base.Replace("\"width\": 32, \"height\": 32", "\"width\": 100000, \"height\": 100000")));
        }

        [Test]
        public void APositionWithNowhereBehindItCannotAlsoNameOne()
        {
            // The two facts are separate on purpose: "nothing behind this line" is design, and
            // "the next mission is not written yet" is content. A file claiming both is confused.
            var ex = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(
                Base.Replace("\"waves\"", "\"lastStand\": true, \"next\": \"other\", \"waves\"")));
            Assert.That(ex!.Message, Does.Contain("lastStand"));
        }

        [Test]
        public void AnUnauthoredNextDoesNotTurnAPositionIntoALastStand()
        {
            // The regression this guards: HasFallbackPosition used to be derived from `next` being
            // empty, so the last authored mission silently lost its extract call and became a
            // mission you could only hold or lose.
            var def = ScenarioReader.Read(Base);
            Assert.That(def.Next, Is.Empty);
            Assert.That(def.HasFallbackPosition, Is.True);

            var last = ScenarioReader.Read(Base.Replace("\"waves\"", "\"lastStand\": true, \"waves\""));
            Assert.That(last.HasFallbackPosition, Is.False);
        }

        [Test]
        public void ASeedTooLargeToSurviveTheFileIsRejected()
        {
            var ex = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(
                Base.Replace("\"waves\"", "\"director\": { \"seed\": 1e300 }, \"waves\"")));
            Assert.That(ex!.Message, Does.Contain("seed"));
        }
    }

    /// <summary>
    /// The actor system: the objects a mission is fought over. Pure rules, no scene.
    /// </summary>
    public sealed class ActorTests
    {
        /// <summary>A threat that reports a fixed crowd on every actor.</summary>
        private sealed class Crowd : IActorThreat
        {
            private readonly int _n;
            public Crowd(int n) { _n = n; }
            public int EnemiesWithin(int x, int y, float radius) => _n;
        }

        private static ActorSystem OneProcess(float duration, float requiresHeroWithin = 0f, int hp = 100) =>
            new ActorSystem(new[]
            {
                new ActorDef
                {
                    Id = "gen", Kind = ActorKind.Process, X = 10, Y = 10,
                    Hp = hp, DurationSeconds = duration, RequiresHeroWithin = requiresHeroWithin,
                },
            });

        [Test]
        public void AnUnattendedProcessRunsOnItsOwn()
        {
            var sys = OneProcess(10f);
            for (int i = 0; i < 100; i++) sys.Tick(0.1f, 0f, 0f, null!);
            Assert.That(sys.IsComplete("gen"), Is.True);
            Assert.That(sys.Progress01("gen"), Is.EqualTo(1f).Within(1e-4));
        }

        [Test]
        public void AnAttendedProcessOnlyRunsWithTheHeroThere()
        {
            var sys = OneProcess(10f, requiresHeroWithin: 6f);

            // Hero across the map: nothing happens, and the HUD should say so rather than tick.
            for (int i = 0; i < 50; i++) sys.Tick(0.1f, 60f, 60f, null!);
            Assert.That(sys.Progress01("gen"), Is.EqualTo(0f));
            Assert.That(sys.IsRunning("gen"), Is.False);

            for (int i = 0; i < 50; i++) sys.Tick(0.1f, 10.5f, 10.5f, null!);
            Assert.That(sys.Progress01("gen"), Is.EqualTo(0.5f).Within(1e-3));
            Assert.That(sys.IsRunning("gen"), Is.True);
        }

        [Test]
        public void LeavingPausesTheWorkRatherThanLosingIt()
        {
            // Eight minutes thrown away by one bad thirty seconds is the kind of punishment that
            // stops people taking the risk the mission is about.
            var sys = OneProcess(10f, requiresHeroWithin: 6f);
            for (int i = 0; i < 40; i++) sys.Tick(0.1f, 10.5f, 10.5f, null!);
            float banked = sys.Progress01("gen");

            for (int i = 0; i < 40; i++) sys.Tick(0.1f, 60f, 60f, null!);
            Assert.That(sys.Progress01("gen"), Is.EqualTo(banked).Within(1e-4));
        }

        [Test]
        public void ACrowdWrecksAnActorAndStopsItsWork()
        {
            var sys = OneProcess(100f, hp: 10);
            sys.DamagePerEnemyPerSecond = 2f;

            sys.Tick(1f, 0f, 0f, new Crowd(3));       // 6 damage
            Assert.That(sys.IsAlive("gen"), Is.True);

            sys.Tick(1f, 0f, 0f, new Crowd(3));       // 12 total
            Assert.That(sys.IsAlive("gen"), Is.False);

            float stopped = sys.Progress01("gen");
            sys.Tick(5f, 0f, 0f, null!);
            Assert.That(sys.Progress01("gen"), Is.EqualTo(stopped),
                        "a wrecked machine does not keep working");
        }

        [Test]
        public void CrewDieFasterThanMachines()
        {
            var sys = new ActorSystem(new[]
            {
                new ActorDef { Id = "box", Kind = ActorKind.Structure, X = 1, Y = 1, Hp = 30 },
                new ActorDef { Id = "dave", Kind = ActorKind.Crew, X = 2, Y = 2, Hp = 30 },
            });
            sys.DamagePerEnemyPerSecond = 2f;
            sys.CrewDamageMultiplier = 3f;

            sys.Tick(5f, 0f, 0f, new Crowd(1));       // machine 10, person 30
            Assert.That(sys.AliveCount(ActorKind.Structure), Is.EqualTo(1));
            Assert.That(sys.AliveCount(ActorKind.Crew), Is.EqualTo(0));
        }

        [Test]
        public void DuplicateActorIdsAreRejected()
        {
            Assert.Throws<ScenarioException>(() => new ActorSystem(new[]
            {
                new ActorDef { Id = "gen", Kind = ActorKind.Process, DurationSeconds = 1f },
                new ActorDef { Id = "gen", Kind = ActorKind.Process, DurationSeconds = 1f },
            }));
        }
    }

    public sealed class ActorObjectiveTests
    {
        private static ObjectiveContext Ctx(IActorQuery actors, int vaultHp = 25) =>
            new ObjectiveContext(0f, 0, 0, 0, vaultHp, 25, actors);

        private sealed class Crowd : IActorThreat
        {
            private readonly int _n;
            public Crowd(int n) { _n = n; }
            public int EnemiesWithin(int x, int y, float radius) => _n;
        }

        [Test]
        public void HoldUntilCompletesWhenTheWorkDoes()
        {
            var sys = new ActorSystem(new[]
            {
                new ActorDef { Id = "gen", Kind = ActorKind.Process, X = 5, Y = 5, Hp = 100, DurationSeconds = 4f },
            });
            var o = new HoldUntil("gen");

            sys.Tick(2f, 0f, 0f, null!);
            Assert.That(o.Tick(Ctx(sys), 2f), Is.EqualTo(ObjectiveState.Pending));
            Assert.That(o.Hud, Does.Contain("50%"));

            sys.Tick(2f, 0f, 0f, null!);
            Assert.That(o.Tick(Ctx(sys), 2f), Is.EqualTo(ObjectiveState.Complete));
        }

        [Test]
        public void HoldUntilFailsIfTheThingIsDestroyed()
        {
            // The horde does not have to reach you. It only has to reach the generator.
            var sys = new ActorSystem(new[]
            {
                new ActorDef { Id = "gen", Kind = ActorKind.Process, X = 5, Y = 5, Hp = 4, DurationSeconds = 600f },
            });
            sys.DamagePerEnemyPerSecond = 2f;
            var o = new HoldUntil("gen");

            sys.Tick(1f, 0f, 0f, new Crowd(3));
            Assert.That(o.Tick(Ctx(sys), 1f), Is.EqualTo(ObjectiveState.Failed));
            Assert.That(o.Hud, Is.EqualTo("DESTROYED"));
        }

        [Test]
        public void HoldUntilIsAGoalAndProtectActorsIsNot()
        {
            // If ProtectActors counted as a goal, every mission carrying one would be unwinnable.
            Assert.That(new HoldUntil("gen").IsFailCondition, Is.False);
            Assert.That(new ProtectActors(1).IsFailCondition, Is.True);
            Assert.That(new KeepCrewAlive(1).IsFailCondition, Is.True);
            Assert.That(new ProtectVault(1).IsFailCondition, Is.True);
            Assert.That(new ClearWaves(1).IsFailCondition, Is.False);
        }

        [Test]
        public void AMissionOfHoldUntilPlusProtectActorsIsWinnable()
        {
            var sys = new ActorSystem(new[]
            {
                new ActorDef { Id = "gen", Kind = ActorKind.Process, X = 5, Y = 5, Hp = 100, DurationSeconds = 2f },
                new ActorDef { Id = "xfmr", Kind = ActorKind.Structure, X = 8, Y = 8, Hp = 100 },
            });
            var set = ObjectiveFactory.CreateSet(new[]
            {
                new ObjectiveDef { Type = "HoldUntil", ActorId = "gen" },
                new ObjectiveDef { Type = "ProtectActors", MinAlive = 2 },
            });

            sys.Tick(2f, 0f, 0f, null!);
            set.Tick(Ctx(sys), 2f);

            Assert.That(set.IsComplete, Is.True);
            Assert.That(set.IsFailed, Is.False);
        }

        [Test]
        public void LosingOneOfTwoProtectedActorsFailsTheMission()
        {
            var sys = new ActorSystem(new[]
            {
                new ActorDef { Id = "a", Kind = ActorKind.Structure, X = 5, Y = 5, Hp = 4 },
                new ActorDef { Id = "b", Kind = ActorKind.Structure, X = 8, Y = 8, Hp = 400 },
            });
            sys.DamagePerEnemyPerSecond = 2f;
            var set = ObjectiveFactory.CreateSet(new[]
            {
                new ObjectiveDef { Type = "ClearWaves", Count = 1 },
                new ObjectiveDef { Type = "ProtectActors", MinAlive = 2 },
            });

            sys.Tick(1f, 0f, 0f, new Crowd(3));
            set.Tick(Ctx(sys), 1f);
            Assert.That(set.IsFailed, Is.True);
            Assert.That(set.FailedBy, Is.TypeOf<ProtectActors>());
        }

        [Test]
        public void ObjectivesNamingMissingOrWrongActorsAreRejectedAtLoad()
        {
            const string with = @"{
                ""schema"": 1, ""id"": ""t"", ""displayName"": ""T"",
                ""map"": { ""width"": 32, ""height"": 32 },
                ""heroSpawn"": { ""x"": 30, ""y"": 16 },
                ""spawnCells"": [ { ""x"": 1, ""y"": 16 } ],
                ""vault"": { ""x"": 31, ""y"": 16, ""hp"": 10 },
                ""actors"": [ { ""id"": ""box"", ""kind"": ""Structure"", ""x"": 10, ""y"": 10 } ],
                ""waves"": [ { ""setupSeconds"": 10, ""count"": 20, ""spawnPerSecond"": 4 } ],
                ""objectives"": [ { ""type"": ""HoldUntil"", ""actorId"": ""box"" } ]
            }";

            // A Structure cannot run a process, so HoldUntil on one could never finish.
            var ex = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(with));
            Assert.That(ex!.Message, Does.Contain("Process"));

            // Wanting more alive than exist fails on the first tick.
            var ex2 = Assert.Throws<ScenarioException>(() => ScenarioReader.Read(
                with.Replace(@"{ ""type"": ""HoldUntil"", ""actorId"": ""box"" }",
                             @"{ ""type"": ""ProtectActors"", ""minAlive"": 3 }")));
            Assert.That(ex2!.Message, Does.Contain("ProtectActors"));
        }
    }
}
