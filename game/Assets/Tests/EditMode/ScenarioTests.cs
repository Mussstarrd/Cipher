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
        public void CompletingTheObjectivesOpensThePackUpWindow()
        {
            // ADR-005: finishing the job is not the end of the mission where there is a line behind
            // you. It is the cue to unbolt what you can carry and go. Setting Won here instead
            // skipped extraction and the truck entirely.
            var match = NewMatch();
            match.CompleteByObjective();

            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Extraction));
            Assert.That(match.ExtractTimeLeft, Is.GreaterThan(0f));
            Assert.That(match.LastWaveDeclared, Is.True);
        }

        [Test]
        public void WithNowhereToFallBackCompletingSimplyWins()
        {
            var match = NewMatch(hasFallback: false);
            match.CompleteByObjective();
            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Won));
        }

        [Test]
        public void CompletingAgainDuringExtractionDoesNotRestartTheWindow()
        {
            var match = NewMatch();
            match.CompleteByObjective();
            float first = match.ExtractTimeLeft;

            match.Tick(3f, aliveRunners: 0, breachedThisTick: 0);
            match.CompleteByObjective();

            Assert.That(match.ExtractTimeLeft, Is.LessThan(first),
                        "the pack-up clock must keep running; objectives stay complete every tick");
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
        public void TheShippedGateMissionReachesItsPackUpWindow()
        {
            // The Gate asks for two of its five waves. If completion ended the match outright, and
            // MinWavesBeforeExtract is also two, the extraction phase would be unreachable in
            // mission one -- the exact bug this pair of changes exists to close.
            var def = ScenarioReader.Read(
                File.ReadAllText(Path.Combine("Assets", "Resources", "Scenarios", "act1-01-the-gate.json")));
            var set = ObjectiveFactory.CreateSet(def.Objectives);
            var match = new Cipher.Game.Match.MatchState(
                def.ToWaveTable(), def.Economy, def.VaultHp);

            set.Tick(new ObjectiveContext(0f, 1, 2, 0, def.VaultHp, def.VaultHp), 1f);
            Assert.That(set.IsComplete, Is.True);

            match.CompleteByObjective();
            Assert.That(match.Phase, Is.EqualTo(Cipher.Game.Match.MatchPhase.Extraction));
            Assert.That(def.Waves.Count, Is.GreaterThan(2), "the objective must shorten the match");
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
