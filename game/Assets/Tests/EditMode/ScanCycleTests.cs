#nullable enable
using System.Collections.Generic;
using Cipher.Game.Match;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The scan cycle (ADR-005): the player decides when a position is done, and the pack-up
    /// window decides how much of their kit goes with them.
    /// </summary>
    public sealed class ScanCycleTests
    {
        private static IReadOnlyList<WaveDef> Waves(int n)
        {
            var list = new List<WaveDef>();
            for (int i = 0; i < n; i++) list.Add(new WaveDef(2, 100f, 1f));
            return list;
        }

        private static MatchState Make(
            int waves = 6, int minWaves = 2, float extract = 90f, bool fallback = true)
            => new MatchState(
                Waves(waves),
                new EconomyConfig(),
                vaultHp: 25,
                cycle: new ScanCycleConfig { MinWavesBeforeExtract = minWaves, ExtractSeconds = extract },
                hasFallbackPosition: fallback);

        /// <summary>Runs one wave to completion. Returns the state for chaining.</summary>
        private static void ClearOneWave(MatchState m)
        {
            // Burn the setup countdown.
            while (m.Phase == MatchPhase.Setup) m.Tick(0.5f, 0, 0);
            // Spawn the wave out.
            while (m.Phase == MatchPhase.Wave && !m.WaveFullySpawned) m.Tick(0.1f, 1, 0);
            // Nothing alive: the wave clears.
            m.Tick(0.1f, 0, 0);
        }

        [Test]
        public void CannotCallLastWaveBeforeTheMinimum()
        {
            var m = Make(minWaves: 2);
            Assert.AreEqual(DeclareResult.TooEarly, m.DeclareLastWave());
            Assert.IsFalse(m.CanDeclareLastWave);

            ClearOneWave(m);
            Assert.AreEqual(DeclareResult.TooEarly, m.DeclareLastWave(), "one wave is not two");

            ClearOneWave(m);
            Assert.IsTrue(m.CanDeclareLastWave, "two cleared: the call unlocks");
            Assert.AreEqual(DeclareResult.Ok, m.DeclareLastWave());
        }

        [Test]
        public void DeclaringTwiceIsRefused()
        {
            var m = Make(minWaves: 0);
            Assert.AreEqual(DeclareResult.Ok, m.DeclareLastWave());
            Assert.AreEqual(DeclareResult.AlreadyDeclared, m.DeclareLastWave());
        }

        [Test]
        public void APositionWithNothingBehindItCannotBeLeft()
        {
            // Mission 12. The extract call is greyed out and that is the horror.
            var m = Make(minWaves: 0, fallback: false);
            Assert.AreEqual(DeclareResult.NowhereToGo, m.DeclareLastWave());
            Assert.IsFalse(m.CanDeclareLastWave);
        }

        [Test]
        public void ClearingTheDeclaredWaveOpensThePackUpWindow()
        {
            var m = Make(minWaves: 0, extract: 90f);
            m.DeclareLastWave();
            ClearOneWave(m);

            Assert.AreEqual(MatchPhase.Extraction, m.Phase);
            Assert.AreEqual(90f, m.ExtractTimeLeft, 0.001f);
            Assert.IsFalse(m.IsOver, "packing up is not over");
        }

        [Test]
        public void AnUndeclaredWaveClearGoesToSetupAsBefore()
        {
            var m = Make();
            ClearOneWave(m);
            Assert.AreEqual(MatchPhase.Setup, m.Phase);
            Assert.AreEqual(2, m.WaveNumber);
        }

        [Test]
        public void CommittingBeforeTheWaveSpawnsPaysABonus()
        {
            var committed = Make(minWaves: 0);
            committed.DeclareLastWave();                 // during Setup: blind commitment
            ClearOneWave(committed);

            var late = Make(minWaves: 0);
            while (late.Phase == MatchPhase.Setup) late.Tick(0.5f, 0, 0);
            late.DeclareLastWave();                      // mid-wave, having seen it
            while (late.Phase == MatchPhase.Wave && !late.WaveFullySpawned) late.Tick(0.1f, 1, 0);
            late.Tick(0.1f, 0, 0);

            Assert.Greater(committed.Bank.Cash, late.Bank.Cash,
                "declaring blind should pay more than declaring once you can count them");
        }

        [Test]
        public void SalvageBanksTheValueAndEatsTheWindow()
        {
            var m = Make(minWaves: 0, extract: 30f);
            m.DeclareLastWave();
            ClearOneWave(m);
            int before = m.Bank.Cash;

            Assert.AreEqual(SalvageResult.Ok, m.TrySalvage(150));

            Assert.AreEqual(before + 150, m.Bank.Cash);
            Assert.AreEqual(150, m.Salvaged);
            Assert.AreEqual(1, m.SalvagedCount);
            Assert.AreEqual(26f, m.ExtractTimeLeft, 0.001f, "4s unbolt off a 30s window");
        }

        [Test]
        public void WhenTheWindowIsTooShortTheKitStaysBolted()
        {
            var m = Make(minWaves: 0, extract: 3f);   // shorter than one unbolt
            m.DeclareLastWave();
            ClearOneWave(m);

            Assert.AreEqual(SalvageResult.OutOfTime, m.TrySalvage(150));
            Assert.AreEqual(0, m.Salvaged);
            Assert.AreEqual(1, m.AbandonedCount, "the attempt is recorded for the debrief");
        }

        [Test]
        public void SalvageOutsideTheWindowIsRefused()
        {
            var m = Make();
            Assert.AreEqual(SalvageResult.NotExtracting, m.TrySalvage(150));
        }

        [Test]
        public void TheWindowClosingEndsTheMissionWithoutLosing()
        {
            var m = Make(minWaves: 0, extract: 2f);
            m.DeclareLastWave();
            ClearOneWave(m);

            m.Tick(1f, 0, 0);
            Assert.AreEqual(MatchPhase.Extraction, m.Phase);
            m.Tick(1.5f, 0, 0);

            Assert.AreEqual(MatchPhase.Extracted, m.Phase);
            Assert.IsTrue(m.IsOver);
            Assert.AreNotEqual(MatchPhase.Lost, m.Phase, "leaving on schedule is never a loss");
        }

        [Test]
        public void PullingOutEarlyEndsItImmediately()
        {
            var m = Make(minWaves: 0, extract: 90f);
            m.DeclareLastWave();
            ClearOneWave(m);

            Assert.IsTrue(m.PullOutNow());
            Assert.AreEqual(MatchPhase.Extracted, m.Phase);
        }

        [Test]
        public void PullingOutWhenNotPackingDoesNothing()
        {
            var m = Make();
            Assert.IsFalse(m.PullOutNow());
            Assert.AreEqual(MatchPhase.Setup, m.Phase);
        }

        [Test]
        public void EveryExtraWaveComesOutOfPrepTimeAtTheNextPosition()
        {
            var quick = Make(minWaves: 0);
            quick.DeclareLastWave();
            ClearOneWave(quick);
            quick.PullOutNow();

            var slow = Make(minWaves: 0);
            ClearOneWave(slow);           // took an extra wave first
            slow.DeclareLastWave();
            ClearOneWave(slow);
            slow.PullOutNow();

            Assert.Greater(quick.PrepSecondsRemaining, slow.PrepSecondsRemaining,
                "fighting longer must cost fortify time at the next line");
            Assert.Greater(slow.Bank.Cash, 0);
        }

        [Test]
        public void PrepTimeNeverGoesNegative()
        {
            var m = new MatchState(
                Waves(3), new EconomyConfig(), 25,
                new ScanCycleConfig { CycleSeconds = 1f, MinWavesBeforeExtract = 0 });
            for (int i = 0; i < 50; i++) m.Tick(0.5f, 1, 0);
            Assert.AreEqual(0f, m.PrepSecondsRemaining, 0.001f);
        }

        [Test]
        public void LosingTheVaultStillBeatsEverythingElse()
        {
            var m = Make(minWaves: 0, extract: 90f);
            m.DeclareLastWave();
            ClearOneWave(m);
            Assert.AreEqual(MatchPhase.Extraction, m.Phase);

            m.Tick(0.1f, 0, 99);     // they got to the vault while you were packing
            Assert.AreEqual(MatchPhase.Lost, m.Phase);
        }
    }
}
