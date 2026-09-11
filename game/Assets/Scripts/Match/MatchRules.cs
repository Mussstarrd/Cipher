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

    public enum MatchPhase
    {
        Setup,
        Wave,
        /// <summary>Your declared last wave is down. Pack what you can carry before the next scan.</summary>
        Extraction,
        /// <summary>You left in good order. Not a win, not a loss: the next position is the point.</summary>
        Extracted,
        Won,
        Lost,
    }

    /// <summary>
    /// Wave flow and win/lose, as pure state. The game layer feeds it the tick, how many
    /// runners are alive, and how many breached this tick; it answers how many to spawn.
    /// </summary>
    public sealed class MatchState
    {
        private readonly IReadOnlyList<WaveDef> _waves;
        private readonly EconomyConfig _eco;
        private readonly ScanCycleConfig _cycle;
        private float _spawnAccumulator;
        private bool _declaredBeforeSpawn;

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

        // --- Scan cycle (ADR-005) ---

        /// <summary>Tuning for this position's scan cycle.</summary>
        public ScanCycleConfig Cycle => _cycle;

        /// <summary>
        /// The opening setup has no clock. You dig in for as long as you like and the first wave
        /// comes when you say it does. Owner's call: "you should get as much time as you want to
        /// set up until you press A or something to start". Between-wave setups stay timed, because
        /// that pressure is the game; only the one before the first shot is yours.
        ///
        /// Defaults to FALSE so the constructor stays backward compatible and existing tests keep
        /// their old timing; the game layer opts in. Turning it on by default silently turned three
        /// test loops that wait on Setup into infinite loops, which is exactly the kind of change
        /// that should not ride in on a default.
        /// </summary>
        public bool OpeningIsUntimed { get; }

        /// <summary>True while the match is waiting on the player rather than on a countdown.</summary>
        public bool AwaitingStart =>
            OpeningIsUntimed && Phase == MatchPhase.Setup && WaveIndex == 0 && !_openingReleased;

        private bool _openingReleased;

        /// <summary>
        /// False for a position with no line behind it, which greys out the extract call.
        /// Mission 12 is the only place in Act One where this is false, and that is the point.
        /// </summary>
        public bool HasFallbackPosition { get; }

        /// <summary>The player has called this wave as their last one here.</summary>
        public bool LastWaveDeclared { get; private set; }

        /// <summary>Seconds left in the pack-up window. Only meaningful during Extraction.</summary>
        public float ExtractTimeLeft { get; private set; }

        /// <summary>Real seconds this position has consumed, fighting and packing alike.</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>
        /// What is left of the cycle once you leave: the time you get to fortify, trap and rest at
        /// the next position before the following scan. Every extra wave you take comes out of this.
        /// </summary>
        public float PrepSecondsRemaining => Math.Max(0f, _cycle.CycleSeconds - ElapsedSeconds);

        /// <summary>Cash value of the emplacements you got back on the truck.</summary>
        public int Salvaged { get; private set; }

        /// <summary>How many emplacements you recovered.</summary>
        public int SalvagedCount { get; private set; }

        /// <summary>Emplacements you ran out of time for and left bolted to the ground.</summary>
        public int AbandonedCount { get; private set; }

        public bool CanDeclareLastWave => DeclareLastWaveCheck() == DeclareResult.Ok;

        public WaveDef CurrentWave => _waves[Math.Min(WaveIndex, _waves.Count - 1)];
        public bool WaveFullySpawned => SpawnedThisWave >= CurrentWave.Count;
        public bool IsOver => Phase == MatchPhase.Won || Phase == MatchPhase.Lost
                           || Phase == MatchPhase.Extracted;
        public float RefundMultiplier => Phase == MatchPhase.Setup ? _eco.SetupRefund : _eco.CombatRefund;

        public MatchState(
            IReadOnlyList<WaveDef> waves,
            EconomyConfig economy,
            int vaultHp = 25,
            ScanCycleConfig? cycle = null,
            bool hasFallbackPosition = true,
            bool openingIsUntimed = false)
        {
            _waves = waves ?? throw new ArgumentNullException(nameof(waves));
            if (waves.Count == 0) throw new ArgumentException("Need at least one wave.", nameof(waves));
            _eco = economy ?? throw new ArgumentNullException(nameof(economy));
            _cycle = cycle ?? new ScanCycleConfig();
            HasFallbackPosition = hasFallbackPosition;
            OpeningIsUntimed = openingIsUntimed;
            Bank = new Bank(economy.StartCash);
            VaultMaxHp = VaultHp = Math.Max(1, vaultHp);
            SetupTimeLeft = _waves[0].SetupSeconds;
        }

        private DeclareResult DeclareLastWaveCheck()
        {
            if (!HasFallbackPosition) return DeclareResult.NowhereToGo;
            if (LastWaveDeclared) return DeclareResult.AlreadyDeclared;
            if (Phase != MatchPhase.Setup && Phase != MatchPhase.Wave) return DeclareResult.NotFighting;
            if (WavesCleared < _cycle.MinWavesBeforeExtract) return DeclareResult.TooEarly;
            return DeclareResult.Ok;
        }

        /// <summary>
        /// "This is my last wave here." Clearing it opens the pack-up window instead of another setup.
        /// Calling it during Setup, before you can see what is coming, pays a commitment bonus.
        /// </summary>
        public DeclareResult DeclareLastWave()
        {
            var check = DeclareLastWaveCheck();
            if (check != DeclareResult.Ok) return check;
            LastWaveDeclared = true;
            _declaredBeforeSpawn = Phase == MatchPhase.Setup;
            return DeclareResult.Ok;
        }

        /// <summary>
        /// Unbolt one emplacement during the pack-up window and put it on the truck. Costs
        /// <see cref="ScanCycleConfig.UnboltSeconds"/> off the window; refuses when the window is short.
        /// </summary>
        public SalvageResult TrySalvage(int value)
        {
            if (Phase != MatchPhase.Extraction) return SalvageResult.NotExtracting;
            if (ExtractTimeLeft < _cycle.UnboltSeconds)
            {
                AbandonedCount++;
                return SalvageResult.OutOfTime;
            }
            // The window is the only clock here; Tick already charges real time to ElapsedSeconds.
            ExtractTimeLeft -= _cycle.UnboltSeconds;
            if (value > 0) { Salvaged += value; Bank.Earn(value); }
            SalvagedCount++;
            return SalvageResult.Ok;
        }

        /// <summary>Leave now rather than burn the rest of the window. Banks the unused prep time.</summary>
        public bool PullOutNow()
        {
            if (Phase != MatchPhase.Extraction) return false;
            ExtractTimeLeft = 0f;
            Phase = MatchPhase.Extracted;
            return true;
        }

        /// <summary>Emplacements still standing when the window closed, for the debrief.</summary>
        public void ReportAbandoned(int count)
        {
            if (count > 0) AbandonedCount += count;
        }

        /// <summary>
        /// Player starts the wave: skips a running countdown, or releases the untimed opening.
        /// </summary>
        public void StartWaveNow()
        {
            if (Phase != MatchPhase.Setup) return;
            _openingReleased = true;
            SetupTimeLeft = 0f;
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
        /// <summary>
        /// Ends the match from outside, because the scenario's objectives were met.
        ///
        /// The phase machine here owns spawning, setup and extraction; it does NOT own what winning
        /// this particular mission means, because that is content. Half of Act One is won by a clock
        /// and not by a kill count, and a wave table cannot express that. So the objectives call
        /// this, and clearing the last wave stays as the backstop for a mission that is just waves.
        ///
        /// Ignored once the match is over: a win cannot overwrite a loss that already happened.
        /// </summary>
        public void WinByObjective()
        {
            if (IsOver) return;
            Phase = MatchPhase.Won;
        }

        /// <summary>Ends the match from outside, because an objective failed. See WinByObjective.</summary>
        public void LoseByObjective()
        {
            if (IsOver) return;
            Phase = MatchPhase.Lost;
        }

        public int Tick(float dt, int aliveRunners, int breachedThisTick)
        {
            if (IsOver || dt <= 0f) return 0;

            ElapsedSeconds += dt;

            if (breachedThisTick > 0)
            {
                VaultHp = Math.Max(0, VaultHp - breachedThisTick);
                if (VaultHp == 0) { Phase = MatchPhase.Lost; return 0; }
            }

            switch (Phase)
            {
                case MatchPhase.Setup:
                    // The opening holds until the player says go. Nothing ages while it waits, so
                    // taking your time here does not eat the scan cycle either.
                    if (AwaitingStart) { ElapsedSeconds -= dt; return 0; }
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
                        int bonus = _eco.WaveClearBonusPerWave * WaveNumber;
                        if (LastWaveDeclared && _declaredBeforeSpawn)
                            bonus += (int)Math.Round(bonus * _cycle.CommitBonus);
                        Bank.Earn(bonus);

                        if (LastWaveDeclared)
                        {
                            // You called it and you held it. The scan is still coming.
                            Phase = MatchPhase.Extraction;
                            ExtractTimeLeft = _cycle.ExtractSeconds;
                        }
                        else if (WaveIndex + 1 >= _waves.Count)
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

                case MatchPhase.Extraction:
                    ExtractTimeLeft = Math.Max(0f, ExtractTimeLeft - dt);
                    if (ExtractTimeLeft <= 0f) Phase = MatchPhase.Extracted;
                    return 0;
            }
            return 0;
        }
    }
}
