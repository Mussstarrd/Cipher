#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game.Match
{
    /// <summary>Cash economy numbers (docs/design/economy-towers-and-aiming.md B, ROADMAP #3).</summary>
    public sealed class EconomyConfig
    {
        public int StartCash { get; set; } = 400;
        public int CashPerKill { get; set; } = 5;
        public int WaveClearBonusPerWave { get; set; } = 100;   // x wave number
        public int BarricadeCost { get; set; } = 20;
        // Turret prices live on the turret family in the sim catalogue, not here.
        public int DroneCost { get; set; } = 150;
        public float SetupRefund { get; set; } = 1f;            // between waves
        public float CombatRefund { get; set; } = 0.5f;         // mid-wave, times remaining health
    }

    /// <summary>The player's wallet. Integer cash; never negative.</summary>
    public sealed class Bank
    {
        public int Cash { get; private set; }
        public int TotalEarned { get; private set; }
        public int TotalSpent { get; private set; }

        public Bank(int startCash) { Cash = Math.Max(0, startCash); }

        public void Earn(int amount)
        {
            if (amount <= 0) return;
            Cash += amount;
            TotalEarned += amount;
        }

        public bool CanAfford(int cost) => cost <= Cash;

        public bool TrySpend(int cost)
        {
            if (cost < 0 || cost > Cash) return false;
            Cash -= cost;
            TotalSpent += cost;
            return true;
        }
    }

    public sealed class WaveDef
    {
        public int Count { get; }
        public float SpawnPerSecond { get; }
        public float SetupSeconds { get; }

        public WaveDef(int count, float spawnPerSecond, float setupSeconds)
        {
            Count = count; SpawnPerSecond = spawnPerSecond; SetupSeconds = setupSeconds;
        }
    }

    /// <summary>The five graybox waves (blueprint M2). Data, not code: a data table replaces this later.</summary>
    public static class WaveTable
    {
        public static IReadOnlyList<WaveDef> Default { get; } = new[]
        {
            new WaveDef(150, 8f, 25f),
            new WaveDef(250, 10f, 20f),
            new WaveDef(350, 12f, 20f),
            new WaveDef(500, 15f, 20f),
            new WaveDef(700, 18f, 20f),
        };
    }

    public enum MatchPhase { Setup, Wave, Won, Lost }

    /// <summary>
    /// Wave flow and win/lose, as pure state. The game layer feeds it the tick, how many
    /// runners are alive, and how many breached this tick; it answers how many to spawn.
    /// </summary>
    public sealed class MatchState
    {
        private readonly IReadOnlyList<WaveDef> _waves;
        private readonly EconomyConfig _eco;
        private float _spawnAccumulator;

        public MatchPhase Phase { get; private set; } = MatchPhase.Setup;
        /// <summary>0-based index of the current (or next, during Setup) wave.</summary>
        public int WaveIndex { get; private set; }
        public int WaveNumber => WaveIndex + 1;
        public int WaveCount => _waves.Count;
        public float SetupTimeLeft { get; private set; }
        public int SpawnedThisWave { get; private set; }
        public int VaultHp { get; private set; }
        public int VaultMaxHp { get; }
        public Bank Bank { get; }
        public int WavesCleared { get; private set; }

        public WaveDef CurrentWave => _waves[Math.Min(WaveIndex, _waves.Count - 1)];
        public bool WaveFullySpawned => SpawnedThisWave >= CurrentWave.Count;
        public bool IsOver => Phase == MatchPhase.Won || Phase == MatchPhase.Lost;
        public float RefundMultiplier => Phase == MatchPhase.Setup ? _eco.SetupRefund : _eco.CombatRefund;

        public MatchState(IReadOnlyList<WaveDef> waves, EconomyConfig economy, int vaultHp = 25)
        {
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
            if (waves.Count == 0) throw new ArgumentException("Need at least one wave.", nameof(waves));
            _eco = economy ?? throw new ArgumentNullException(nameof(economy));
            Bank = new Bank(economy.StartCash);
            VaultMaxHp = VaultHp = Math.Max(1, vaultHp);
            SetupTimeLeft = _waves[0].SetupSeconds;
        }

        /// <summary>Player skips the remaining setup countdown.</summary>
        public void StartWaveNow()
        {
            if (Phase == MatchPhase.Setup) SetupTimeLeft = 0f;
        }

        /// <summary>Kills from any source pay out.</summary>
        public void ReportKills(int kills)
        {
            if (kills > 0) Bank.Earn(kills * _eco.CashPerKill);
        }

        /// <summary>
        /// Advances the match. Returns how many runners the caller must spawn this tick.
        /// <paramref name="aliveRunners"/> is the swarm's live count after its step;
        /// <paramref name="breachedThisTick"/> runners reached the exit this tick.
        /// </summary>
        public int Tick(float dt, int aliveRunners, int breachedThisTick)
        {
            if (IsOver || dt <= 0f) return 0;

            if (breachedThisTick > 0)
            {
                VaultHp = Math.Max(0, VaultHp - breachedThisTick);
                if (VaultHp == 0) { Phase = MatchPhase.Lost; return 0; }
            }

            switch (Phase)
            {
                case MatchPhase.Setup:
                    SetupTimeLeft = Math.Max(0f, SetupTimeLeft - dt);
                    if (SetupTimeLeft <= 0f)
                    {
                        Phase = MatchPhase.Wave;
                        SpawnedThisWave = 0;
                        _spawnAccumulator = 0f;
                    }
                    return 0;

                case MatchPhase.Wave:
                    int toSpawn = 0;
                    if (!WaveFullySpawned)
                    {
                        _spawnAccumulator += CurrentWave.SpawnPerSecond * dt;
                        toSpawn = Math.Min((int)_spawnAccumulator, CurrentWave.Count - SpawnedThisWave);
                        _spawnAccumulator -= toSpawn;
                        SpawnedThisWave += toSpawn;
                    }
                    else if (aliveRunners == 0)
                    {
                        WavesCleared++;
                        Bank.Earn(_eco.WaveClearBonusPerWave * WaveNumber);
                        if (WaveIndex + 1 >= _waves.Count)
                        {
                            Phase = MatchPhase.Won;
                        }
                        else
                        {
                            WaveIndex++;
                            Phase = MatchPhase.Setup;
                            SetupTimeLeft = CurrentWave.SetupSeconds;
                        }
                    }
                    return toSpawn;
            }
            return 0;
        }
    }
}
