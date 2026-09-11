#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game.Scenarios
{
    /// <summary>
    /// How many enemies are pressing on a point. The one thing the actor system needs from the
    /// simulation, expressed as an interface so the rules are testable without a world.
    /// </summary>
    public interface IActorThreat
    {
        /// <summary>Living enemies within <paramref name="radius"/> cells of a cell.</summary>
        int EnemiesWithin(int x, int y, float radius);
    }

    /// <summary>What an objective is allowed to ask about the mission's actors.</summary>
    public interface IActorQuery
    {
        bool Exists(string id);
        bool IsAlive(string id);
        /// <summary>0..1 for a Process actor; 0 for anything else.</summary>
        float Progress01(string id);
        bool IsComplete(string id);
        /// <summary>Is a Process actually progressing right now, or stalled waiting for the hero?</summary>
        bool IsRunning(string id);
        int AliveCount(ActorKind kind);
        int TotalCount(ActorKind kind);
    }

    /// <summary>
    /// One thing on the map that is not a wall, a turret or an enemy: a transformer, a pump, a
    /// generator running an eight-minute transfer, or a person.
    ///
    /// ADR-003's premise needs these. Half of Act One is won by a clock rather than a kill count,
    /// and "hold until the transfer finishes" is not a wave table -- it is an object on the map with
    /// a timer and a health bar, which the horde can take away from you.
    /// </summary>
    public sealed class ActorState
    {
        public string Id { get; }
        public ActorKind Kind { get; }
        public int X { get; }
        public int Y { get; }

        public float Hp { get; private set; }
        public float MaxHp { get; }
        public bool IsAlive => Hp > 0f;

        /// <summary>Seconds of work a Process needs. Zero for everything else.</summary>
        public float Duration { get; }
        /// <summary>Seconds of work a Process has banked.</summary>
        public float Elapsed { get; private set; }
        /// <summary>Cells the hero must be within for a Process to run. Zero means unattended.</summary>
        public float RequiresHeroWithin { get; }

        /// <summary>True on the last tick's evaluation: is the work actually progressing?</summary>
        public bool Running { get; private set; }

        public bool IsComplete => Kind == ActorKind.Process && Elapsed >= Duration && Duration > 0f;
        public float Progress01 => Duration <= 0f ? 0f : Math.Min(1f, Elapsed / Duration);
        public float HealthFraction => MaxHp <= 0f ? 0f : Math.Max(0f, Hp / MaxHp);

        public ActorState(ActorDef def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            Id = def.Id;
            Kind = def.Kind;
            X = def.X;
            Y = def.Y;
            MaxHp = Hp = Math.Max(1f, def.Hp);
            Duration = Math.Max(0f, def.DurationSeconds);
            RequiresHeroWithin = Math.Max(0f, def.RequiresHeroWithin);
        }

        internal void Damage(float amount)
        {
            if (amount <= 0f || !IsAlive) return;
            Hp = Math.Max(0f, Hp - amount);
        }

        internal void Advance(float dt, bool running)
        {
            Running = running;
            // Work does not un-happen. A process the horde pushes you off of is paused, not reset:
            // losing eight minutes to one bad thirty seconds is the kind of punishment that makes
            // people stop taking the risk the mission is about.
            if (running && IsAlive && !IsComplete) Elapsed += dt;
        }
    }

    /// <summary>
    /// The mission's actors, and the rules that act on them. Pure C#: no scene, no simulation types.
    ///
    /// Damage is modelled as pressure rather than as attacks. An enemy standing on a transformer is
    /// wrecking it, and how fast depends on how many of them there are. Going through the sim's
    /// per-agent attack code would mean giving every actor a grid cell, which would make it solid
    /// and change pathing -- and the build preview must never lie about where things walk.
    /// </summary>
    public sealed class ActorSystem : IActorQuery
    {
        /// <summary>How close an enemy has to be to be wrecking something.</summary>
        public float ContactRadius { get; set; } = 1.6f;

        /// <summary>Damage per enemy in contact, per second.</summary>
        public float DamagePerEnemyPerSecond { get; set; } = 2.2f;

        /// <summary>Crew are people, and people do not soak a crowd. They take it far faster.</summary>
        public float CrewDamageMultiplier { get; set; } = 3f;

        private readonly List<ActorState> _actors = new List<ActorState>();
        private readonly Dictionary<string, ActorState> _byId =
            new Dictionary<string, ActorState>(StringComparer.Ordinal);

        public ActorSystem(IEnumerable<ActorDef> defs)
        {
            if (defs == null) return;
            foreach (var def in defs)
            {
                var actor = new ActorState(def);
                if (_byId.ContainsKey(actor.Id))
                    throw new ScenarioException($"two actors share the id '{actor.Id}'");
                _actors.Add(actor);
                _byId[actor.Id] = actor;
            }
        }

        public IReadOnlyList<ActorState> All => _actors;
        public int Count => _actors.Count;

        public void Tick(float dt, float heroX, float heroY, IActorThreat threat)
        {
            if (dt <= 0f) return;

            for (int i = 0; i < _actors.Count; i++)
            {
                var a = _actors[i];
                if (!a.IsAlive)
                {
                    a.Advance(dt, running: false);
                    continue;
                }

                if (threat != null)
                {
                    int pressing = threat.EnemiesWithin(a.X, a.Y, ContactRadius);
                    if (pressing > 0)
                    {
                        float scale = a.Kind == ActorKind.Crew ? CrewDamageMultiplier : 1f;
                        a.Damage(pressing * DamagePerEnemyPerSecond * scale * dt);
                    }
                }

                if (a.Kind != ActorKind.Process) { a.Advance(dt, running: false); continue; }

                bool attended = a.RequiresHeroWithin <= 0f || WithinCells(a, heroX, heroY);
                a.Advance(dt, running: attended && a.IsAlive);
            }
        }

        private static bool WithinCells(ActorState a, float heroX, float heroY)
        {
            float dx = heroX - (a.X + 0.5f);
            float dy = heroY - (a.Y + 0.5f);
            return dx * dx + dy * dy <= a.RequiresHeroWithin * a.RequiresHeroWithin;
        }

        // ---- IActorQuery ----

        public bool Exists(string id) => _byId.ContainsKey(id);
        public bool IsAlive(string id) => _byId.TryGetValue(id, out var a) && a.IsAlive;
        public float Progress01(string id) => _byId.TryGetValue(id, out var a) ? a.Progress01 : 0f;
        public bool IsComplete(string id) => _byId.TryGetValue(id, out var a) && a.IsComplete;
        public bool IsRunning(string id) => _byId.TryGetValue(id, out var a) && a.Running;

        public int AliveCount(ActorKind kind)
        {
            int n = 0;
            for (int i = 0; i < _actors.Count; i++)
                if (_actors[i].Kind == kind && _actors[i].IsAlive) n++;
            return n;
        }

        public int TotalCount(ActorKind kind)
        {
            int n = 0;
            for (int i = 0; i < _actors.Count; i++) if (_actors[i].Kind == kind) n++;
            return n;
        }
    }
}
