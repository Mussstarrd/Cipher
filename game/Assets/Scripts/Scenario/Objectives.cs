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

        /// <summary>The mission's actors, or null for a mission that has none.</summary>
        public readonly IActorQuery? Actors;

        public ObjectiveContext(float matchSeconds, int waveIndex, int wavesCleared,
                                int aliveEnemies, int vaultHp, int vaultMaxHp,
                                IActorQuery? actors = null)
        {
            MatchSeconds = matchSeconds;
            WaveIndex = waveIndex;
            WavesCleared = wavesCleared;
            AliveEnemies = aliveEnemies;
            VaultHp = vaultHp;
            VaultMaxHp = vaultMaxHp;
            Actors = actors;
        }
    }

    public interface IObjective
    {
        string Id { get; }
        /// <summary>One live line for the HUD, e.g. "HOLD  4:12" or "WAVES  2/3".</summary>
        string Hud { get; }
        float Progress01 { get; }

        /// <summary>
        /// True for an objective that can only FAIL or stay pending, never complete: protecting a
        /// thing is not a task you finish, it is a condition you hold. <see cref="ObjectiveSet"/>
        /// must not wait on these, because a mission whose only "goal" is a fail-condition can
        /// never be won -- a bug that reads as a balance problem for a week.
        /// </summary>
        bool IsFailCondition { get; }

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
        public bool IsFailCondition => false;
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
        public bool IsFailCondition => false;
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
        public bool IsFailCondition => true;
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
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
    /// Objectives that declare <see cref="IObjective.IsFailCondition"/> are excluded from the "all
    /// complete" test, because they never complete on their own. Without that exclusion a mission
    /// with a vault objective is unwinnable, which is exactly the kind of bug that looks like a
    /// balance problem for a week.
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

                if (o.IsFailCondition) continue;   // a condition you hold, never a task you finish
                anyGoal = true;
                if (state != ObjectiveState.Complete) allComplete = false;
            }

            IsComplete = anyGoal && allComplete;
        }
    }

    /// <summary>
    /// A Process actor has to finish its work. This is the objective the premise is built on: the
    /// pump house transfer, the lab assay, the boats loading. Seven of Act One's twelve missions
    /// are won by a clock rather than a kill count, and a wave table cannot express that.
    ///
    /// It FAILS if the thing is destroyed, which is the whole tension: the horde does not have to
    /// reach you, it only has to reach the generator.
    /// </summary>
    public sealed class HoldUntil : IObjective
    {
        private readonly string _actorId;
        public HoldUntil(string actorId)
        {
            if (string.IsNullOrEmpty(actorId))
                throw new ScenarioException("HoldUntil needs an actorId");
            _actorId = actorId;
        }

        public string Id => "hold-until:" + _actorId;
        public bool IsFailCondition => false;
        public float Progress01 { get; private set; }
        private bool _running;
        private bool _dead;

        public string Hud
        {
            get
            {
                if (_dead) return "DESTROYED";
                int pct = (int)(Progress01 * 100f);
                return _running ? $"HOLD  {pct}%" : $"HOLD  {pct}%  (STALLED)";
            }
        }

        public ObjectiveState Tick(in ObjectiveContext ctx, float dt)
        {
            var actors = ctx.Actors;
            if (actors == null || !actors.Exists(_actorId)) return ObjectiveState.Pending;

            Progress01 = actors.Progress01(_actorId);
            _running = actors.IsRunning(_actorId);

            if (actors.IsComplete(_actorId)) return ObjectiveState.Complete;
            if (!actors.IsAlive(_actorId)) { _dead = true; return ObjectiveState.Failed; }
            return ObjectiveState.Pending;
        }
    }

    /// <summary>
    /// Keep at least N of the mission's actors standing. A fail-condition: protecting something is
    /// not a task you finish.
    /// </summary>
    public sealed class ProtectActors : IObjective
    {
        private readonly int _minAlive;
        public ProtectActors(int minAlive)
        {
            if (minAlive < 1) throw new ScenarioException("ProtectActors.minAlive must be at least 1");
            _minAlive = minAlive;
        }

        public string Id => "protect-actors";
        public bool IsFailCondition => true;
        public float Progress01 => _total <= 0 ? 1f : Math.Min(1f, _alive / (float)_total);

        private int _alive, _total;
        public string Hud => $"INTACT  {_alive}/{_total}";

        public ObjectiveState Tick(in ObjectiveContext ctx, float dt)
        {
            var actors = ctx.Actors;
            if (actors == null) return ObjectiveState.Pending;

            _alive = actors.AliveCount(ActorKind.Structure) + actors.AliveCount(ActorKind.Process);
            _total = actors.TotalCount(ActorKind.Structure) + actors.TotalCount(ActorKind.Process);
            return _alive < _minAlive ? ObjectiveState.Failed : ObjectiveState.Pending;
        }
    }

    /// <summary>
    /// The permadeath hook. Crew are people, they are named, and they do not come back -- ADR-003's
    /// attrition, and the reason a player cares about a mission they have already technically won.
    /// </summary>
    public sealed class KeepCrewAlive : IObjective
    {
        private readonly int _minAlive;
        public KeepCrewAlive(int minAlive)
        {
            if (minAlive < 1) throw new ScenarioException("KeepCrewAlive.minAlive must be at least 1");
            _minAlive = minAlive;
        }

        public string Id => "keep-crew-alive";
        public bool IsFailCondition => true;
        private int _alive, _total;
        public string Hud => $"CREW  {_alive}/{_total}";
        public float Progress01 => _total <= 0 ? 1f : Math.Min(1f, _alive / (float)_total);

        public ObjectiveState Tick(in ObjectiveContext ctx, float dt)
        {
            var actors = ctx.Actors;
            if (actors == null) return ObjectiveState.Pending;

            _alive = actors.AliveCount(ActorKind.Crew);
            _total = actors.TotalCount(ActorKind.Crew);
            return _alive < _minAlive ? ObjectiveState.Failed : ObjectiveState.Pending;
        }
    }

    /// <summary>Turns parsed <see cref="ObjectiveDef"/>s into live objectives.</summary>
    public static class ObjectiveFactory
    {
        private static readonly string[] Known =
            { "ClearWaves", "SurviveSeconds", "ProtectVault", "HoldUntil", "ProtectActors", "KeepCrewAlive" };

        public static IObjective Create(ObjectiveDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));

            switch (def.Type)
            {
                case "ClearWaves": return new ClearWaves(def.Count);
                case "SurviveSeconds": return new SurviveSeconds(def.Seconds);
                case "ProtectVault": return new ProtectVault(def.MinHp);
                case "HoldUntil": return new HoldUntil(def.ActorId);
                case "ProtectActors": return new ProtectActors(def.MinAlive);
                case "KeepCrewAlive": return new KeepCrewAlive(def.MinAlive);
            }

            throw new ScenarioException(
                $"unknown objective type '{def.Type}'. Known: {string.Join(", ", Known)}");
        }
        public static ObjectiveSet CreateSet(IEnumerable<ObjectiveDef> defs)
        {
            var built = new List<IObjective>();
            foreach (var d in defs) built.Add(Create(d));
            return new ObjectiveSet(built);
        }
    }
}
