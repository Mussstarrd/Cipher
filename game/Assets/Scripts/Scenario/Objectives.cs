#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cipher.Game.Scenarios
{
    public enum ObjectiveState { Pending, Complete, Failed }

    /// <summary>
    /// Everything an objective is allowed to look at, as a snapshot.
    ///
    /// A readonly struct rather than a reference to the match, deliberately: an objective that can
    /// only read cannot quietly become a second place where win/lose is decided. The match owns the
    /// state; <see cref="ObjectiveSet"/> owns the verdict.
    /// </summary>
    public readonly struct ObjectiveContext
    {
        public readonly float MatchSeconds;
        public readonly int WaveIndex;
        public readonly int WavesCleared;
        public readonly int AliveEnemies;
        public readonly int VaultHp;
        public readonly int VaultMaxHp;

        public ObjectiveContext(float matchSeconds, int waveIndex, int wavesCleared,
                                int aliveEnemies, int vaultHp, int vaultMaxHp)
        {
            MatchSeconds = matchSeconds;
            WaveIndex = waveIndex;
            WavesCleared = wavesCleared;
            AliveEnemies = aliveEnemies;
            VaultHp = vaultHp;
            VaultMaxHp = vaultMaxHp;
        }
    }

    public interface IObjective
    {
        string Id { get; }
        /// <summary>One live line for the HUD, e.g. "HOLD  4:12" or "WAVES  2/3".</summary>
        string Hud { get; }
        float Progress01 { get; }
        ObjectiveState Tick(in ObjectiveContext ctx, float dt);
    }

    /// <summary>Clear N waves. The ordinary win condition.</summary>
    public sealed class ClearWaves : IObjective
    {
        private readonly int _target;
        public ClearWaves(int count)
        {
            if (count <= 0) throw new ScenarioException("ClearWaves.count must be at least 1");
            _target = count;
        }

        public string Id => "clear-waves";
        public int Cleared { get; private set; }
        public string Hud => $"WAVES  {Cleared}/{_target}";
        public float Progress01 => Math.Min(1f, Cleared / (float)_target);

        public ObjectiveState Tick(in ObjectiveContext ctx, float dt)
        {
            Cleared = ctx.WavesCleared;
            return Cleared >= _target ? ObjectiveState.Complete : ObjectiveState.Pending;
        }
    }

    /// <summary>
    /// Stay alive for N seconds. The premise's whole point: half of Act One is won by a clock and
    /// not by a kill count, because the enemy are ordinary people and there is no bottom to them.
    /// </summary>
    public sealed class SurviveSeconds : IObjective
    {
        private readonly float _target;
        public SurviveSeconds(float seconds)
        {
            if (seconds <= 0f) throw new ScenarioException("SurviveSeconds.seconds must be positive");
            _target = seconds;
        }

        public string Id => "survive";
        public float Elapsed { get; private set; }
        public string Hud
        {
            get
            {
                float left = Math.Max(0f, _target - Elapsed);
                return $"HOLD  {(int)(left / 60f)}:{((int)left % 60).ToString("00", CultureInfo.InvariantCulture)}";
            }
        }
        public float Progress01 => Math.Min(1f, Elapsed / _target);

        public ObjectiveState Tick(in ObjectiveContext ctx, float dt)
        {
            // Reads the match clock rather than accumulating dt, so a paused or slowed frame cannot
            // drift the objective away from the time the HUD and the scan cycle agree on.
            Elapsed = ctx.MatchSeconds;
            return Elapsed >= _target ? ObjectiveState.Complete : ObjectiveState.Pending;
        }
    }

    /// <summary>
    /// The vault must stay above a floor. This one can only ever FAIL or stay pending: it is a
    /// losing condition wearing an objective's clothes, and it completes when the match ends for
    /// some other reason. <see cref="ObjectiveSet"/> knows not to wait on it.
    /// </summary>
    public sealed class ProtectVault : IObjective
    {
        private readonly int _minHp;
        public ProtectVault(int minHp) { _minHp = Math.Max(0, minHp); }

        public string Id => "protect-vault";
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsFailCondition => true;
        public string Hud => $"VAULT  {Hp}/{MaxHp}";
        public float Progress01 => MaxHp <= 0 ? 1f : Math.Max(0f, Hp / (float)MaxHp);

        public ObjectiveState Tick(in ObjectiveContext ctx, float dt)
        {
            Hp = ctx.VaultHp;
            MaxHp = ctx.VaultMaxHp;
            return Hp < _minHp ? ObjectiveState.Failed : ObjectiveState.Pending;
        }
    }

    /// <summary>
    /// The scenario's objectives together. ALL must complete to win; ANY failure loses.
    ///
    /// Fail-conditions (<see cref="ProtectVault"/>) are excluded from the "all complete" test,
    /// because they never complete on their own. Without that exclusion a mission with a vault
    /// objective is unwinnable, which is exactly the kind of bug that looks like a balance problem
    /// for a week.
    /// </summary>
    public sealed class ObjectiveSet
    {
        private readonly List<IObjective> _objectives;
        private readonly ObjectiveState[] _states;

        public ObjectiveSet(IEnumerable<IObjective> objectives)
        {
            _objectives = new List<IObjective>(objectives ?? throw new ArgumentNullException(nameof(objectives)));
            if (_objectives.Count == 0)
                throw new ScenarioException("a scenario needs at least one objective");
            _states = new ObjectiveState[_objectives.Count];
        }

        public IReadOnlyList<IObjective> All => _objectives;
        public ObjectiveState StateOf(int index) => _states[index];

        /// <summary>Complete when every non-fail objective has completed.</summary>
        public bool IsComplete { get; private set; }
        /// <summary>Failed as soon as any objective fails. Sticky: a failure does not un-fail.</summary>
        public bool IsFailed { get; private set; }
        /// <summary>The objective that lost the mission, for the after-action line.</summary>
        public IObjective? FailedBy { get; private set; }

        public void Tick(in ObjectiveContext ctx, float dt)
        {
            if (IsFailed) return;

            bool allComplete = true;
            bool anyGoal = false;

            for (int i = 0; i < _objectives.Count; i++)
            {
                var o = _objectives[i];
                var state = o.Tick(in ctx, dt);
                _states[i] = state;

                if (state == ObjectiveState.Failed)
                {
                    IsFailed = true;
                    FailedBy = o;
                    return;
                }

                if (o is ProtectVault) continue;   // a fail-condition, never a goal
                anyGoal = true;
                if (state != ObjectiveState.Complete) allComplete = false;
            }

            IsComplete = anyGoal && allComplete;
        }
    }

    /// <summary>Turns parsed <see cref="ObjectiveDef"/>s into live objectives.</summary>
    public static class ObjectiveFactory
    {
        /// <summary>
        /// Objectives the schema defines but the game cannot yet run, because they need the
        /// Structure/Process/Crew actor system that does not exist. Named explicitly so a mission
        /// using one fails LOUDLY at load rather than loading as an objective that can never
        /// complete — an unwinnable mission is far more expensive to diagnose than a parse error.
        /// </summary>
        private static readonly string[] NotYetImplemented =
            { "ProtectActors", "HoldUntil", "KeepCrewAlive" };

        public static IObjective Create(ObjectiveDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            switch (def.Type)
            {
                case "ClearWaves": return new ClearWaves(def.Count);
                case "SurviveSeconds": return new SurviveSeconds(def.Seconds);
                case "ProtectVault": return new ProtectVault(def.MinHp);
            }

            if (Array.IndexOf(NotYetImplemented, def.Type) >= 0)
                throw new ScenarioException(
                    $"objective type '{def.Type}' is in the schema but needs the actor system, " +
                    "which is not built yet. Use ClearWaves, SurviveSeconds or ProtectVault.");

            throw new ScenarioException(
                $"unknown objective type '{def.Type}'. Known: ClearWaves, SurviveSeconds, ProtectVault");
        }

        public static ObjectiveSet CreateSet(IEnumerable<ObjectiveDef> defs)
        {
            var built = new List<IObjective>();
            foreach (var d in defs) built.Add(Create(d));
            return new ObjectiveSet(built);
        }
    }
}
