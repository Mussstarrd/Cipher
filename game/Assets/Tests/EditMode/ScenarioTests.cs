#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Cipher.Game.Scenarios;
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
        public void RejectsAnObjectiveThatCannotRunYetRatherThanLoadingIt()
        {
            // Loading a mission whose objective can never complete produces an unwinnable level that
            // looks like a balance problem. Refusing at load is far cheaper to diagnose.
            var ex = Assert.Throws<ScenarioException>(
                () => ScenarioReader.Read(Minimal.Replace(
                    "{ \"type\": \"ClearWaves\", \"count\": 1 }",
                    "{ \"type\": \"HoldUntil\", \"actorId\": \"gen-1\" }")));
            Assert.That(ex!.Message, Does.Contain("HoldUntil"));
            Assert.That(ex.Message, Does.Contain("actor system"));
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
        private static Cipher.Game.Match.MatchState NewMatch() =>
            new Cipher.Game.Match.MatchState(
                new[] { new Cipher.Game.Match.WaveDef(10, 5f, 1f) },
                new Cipher.Game.Match.EconomyConfig(),
                vaultHp: 5);

        [Test]
        public void AnObjectiveCanWinTheMatchBeforeTheWaveTableRunsOut()
        {
            var match = NewMatch();
            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Setup));
            match.WinByObjective();
            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Won));
        }

        [Test]
        public void AWinCannotOverwriteALossThatAlreadyHappened()
        {
            var match = NewMatch();
            match.LoseByObjective();
            match.WinByObjective();
            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Lost),
                        "an outcome is final; a later objective completing must not rewrite it");
        }

        [Test]
        public void TheVerdictIsIgnoredOnceTheMatchIsOver()
        {
            var match = NewMatch();
            match.WinByObjective();
            match.LoseByObjective();
            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Won));
        }

        [Test]
        public void TheShippedGateMissionIsWonBeforeItsLastWave()
        {
            // The gate asks for two waves out of five. If the objective did not shorten the match,
            // the mission would be five waves long and the objective would be decoration.
            var def = ScenarioReader.Read(
                File.ReadAllText(Path.Combine("Assets", "Resources", "Scenarios", "act1-01-the-gate.json")));
            var set = ObjectiveFactory.CreateSet(def.Objectives);

            set.Tick(new ObjectiveContext(0f, 1, 2, 0, def.VaultHp, def.VaultHp), 1f);
            Assert.That(set.IsComplete, Is.True);
            Assert.That(def.Waves.Count, Is.GreaterThan(2));
        }
    }
}
