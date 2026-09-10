#nullable enable
using Cipher.Game.Match;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    public sealed class MatchRulesTests
    {
        private static readonly WaveDef[] TwoWaves = { new WaveDef(10, 10f, 5f), new WaveDef(20, 20f, 3f) };

        [Test]
        public void Bank_NeverGoesNegative_AndTracksTotals()
        {
            var bank = new Bank(100);
            Assert.IsFalse(bank.TrySpend(150));
            Assert.AreEqual(100, bank.Cash);
            Assert.IsTrue(bank.TrySpend(60));
            Assert.AreEqual(40, bank.Cash);
            bank.Earn(15);
            bank.Earn(-5);
            Assert.AreEqual(55, bank.Cash);
            Assert.AreEqual(15, bank.TotalEarned);
            Assert.AreEqual(60, bank.TotalSpent);
            Assert.IsTrue(bank.CanAfford(55));
            Assert.IsFalse(bank.CanAfford(56));
        }

        [Test]
        public void Setup_CountsDown_ThenWaveSpawnsAtRate()
        {
            var m = new MatchState(TwoWaves, new EconomyConfig());
            Assert.AreEqual(MatchPhase.Setup, m.Phase);
            Assert.AreEqual(1, m.WaveNumber);

            Assert.AreEqual(0, m.Tick(4.9f, 0, 0));
            Assert.AreEqual(MatchPhase.Setup, m.Phase);
            Assert.AreEqual(0, m.Tick(0.2f, 0, 0));
            Assert.AreEqual(MatchPhase.Wave, m.Phase);

            int spawned = 0;
            for (int i = 0; i < 15; i++) spawned += m.Tick(0.1f, spawned, 0); // 1.5 s at 10/s
            Assert.AreEqual(10, spawned, "wave 1 caps at its count");
            Assert.IsTrue(m.WaveFullySpawned);
        }

        [Test]
        public void StartWaveNow_SkipsSetup()
        {
            var m = new MatchState(TwoWaves, new EconomyConfig());
            m.StartWaveNow();
            m.Tick(0.01f, 0, 0);
            Assert.AreEqual(MatchPhase.Wave, m.Phase);
        }

        [Test]
        public void WaveClear_PaysBonus_AdvancesWave_ThenWins()
        {
            var eco = new EconomyConfig { StartCash = 0, WaveClearBonusPerWave = 100 };
            var m = new MatchState(TwoWaves, eco);
            m.StartWaveNow();
            m.Tick(0.01f, 0, 0);
            int spawned = 0;
            while (!m.WaveFullySpawned) spawned += m.Tick(0.1f, spawned, 0);

            m.Tick(0.1f, 5, 0); // still runners alive: no clear
            Assert.AreEqual(MatchPhase.Wave, m.Phase);

            m.Tick(0.1f, 0, 0);
            Assert.AreEqual(MatchPhase.Setup, m.Phase);
            Assert.AreEqual(2, m.WaveNumber);
            Assert.AreEqual(100, m.Bank.Cash, "wave 1 bonus = 100 x 1");
            Assert.AreEqual(3f, m.SetupTimeLeft, 1e-4f);

            m.StartWaveNow();
            m.Tick(0.01f, 0, 0);
            spawned = 0;
            while (!m.WaveFullySpawned) spawned += m.Tick(0.1f, spawned, 0);
            Assert.AreEqual(20, spawned);
            m.Tick(0.1f, 0, 0);
            Assert.AreEqual(MatchPhase.Won, m.Phase);
            Assert.AreEqual(300, m.Bank.Cash, "plus 100 x 2 for wave 2");
            Assert.AreEqual(2, m.WavesCleared);
            Assert.AreEqual(0, m.Tick(1f, 0, 0), "a finished match spawns nothing");
        }

        [Test]
        public void Breaches_DrainVault_ThenLose()
        {
            var m = new MatchState(TwoWaves, new EconomyConfig(), vaultHp: 3);
            m.StartWaveNow();
            m.Tick(0.01f, 0, 0);
            m.Tick(0.1f, 5, 2);
            Assert.AreEqual(1, m.VaultHp);
            Assert.AreEqual(MatchPhase.Wave, m.Phase);
            m.Tick(0.1f, 5, 1);
            Assert.AreEqual(MatchPhase.Lost, m.Phase);
            Assert.IsTrue(m.IsOver);
        }

        [Test]
        public void Kills_PayCash_AndRefundMultiplierFollowsPhase()
        {
            var eco = new EconomyConfig { StartCash = 10, CashPerKill = 5, SetupRefund = 1f, CombatRefund = 0.5f };
            var m = new MatchState(TwoWaves, eco);
            m.ReportKills(3);
            Assert.AreEqual(25, m.Bank.Cash);
            Assert.AreEqual(1f, m.RefundMultiplier);
            m.StartWaveNow();
            m.Tick(0.01f, 0, 0);
            Assert.AreEqual(0.5f, m.RefundMultiplier);
        }
    }
}
