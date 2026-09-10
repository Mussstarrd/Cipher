using System;
using System.Collections.Generic;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Cipher.Sim.Spatial;

namespace Cipher.Sim.Agents
{
    /// <summary>
    /// The swarm. Structure-of-arrays on purpose: the hot loop touches contiguous
    /// float arrays, not scattered objects (docs/05 §1 — SOLID at the seams, DOD inside).
    /// Fixed-tick, fixed iteration order, no RNG: Step is deterministic, guarded by StateHash.
    /// </summary>
    public sealed class AgentWorld
    {
        private readonly GridMap _map;
        private readonly IFlowField _flowField;
        private readonly SimConfig _config;
        private readonly SpatialHash _hash;
        private readonly List<int> _queryScratch = new List<int>(64);
        private readonly List<int> _hashToAgent = new List<int>();
        private bool _hashDirty = true;

        private float[] _posX;
        private float[] _posY;
        private float[] _health;
        private bool[] _alive;

        public int Count { get; private set; }
        public int AliveCount { get; private set; }
        public int ReachedCount { get; private set; }
        public long TotalKills { get; private set; }

        public AgentWorld(GridMap map, IFlowField flowField, SimConfig config, int initialCapacity = 1024)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _flowField = flowField ?? throw new ArgumentNullException(nameof(flowField));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _hash = new SpatialHash(Math.Max(0.25f, config.SeparationRadius));
            _posX = new float[initialCapacity];
            _posY = new float[initialCapacity];
            _health = new float[initialCapacity];
            _alive = new bool[initialCapacity];
        }

        public Vec2 PositionOf(int id) => new Vec2(_posX[id], _posY[id]);
        public bool IsAlive(int id) => _alive[id];
        public float HealthOf(int id) => _health[id];

        public int Spawn(Vec2 position, float health)
        {
            if (health <= 0f)
                throw new ArgumentOutOfRangeException(nameof(health), "Spawn health must be positive.");

            if (Count == _posX.Length)
            {
                int newSize = _posX.Length * 2;
                Array.Resize(ref _posX, newSize);
                Array.Resize(ref _posY, newSize);
                Array.Resize(ref _health, newSize);
                Array.Resize(ref _alive, newSize);
            }

            int id = Count++;
            _posX[id] = position.X;
            _posY[id] = position.Y;
            _health[id] = health;
            _alive[id] = true;
            AliveCount++;
            _hashDirty = true;
            return id;
        }

        /// <summary>Advances the sim one fixed tick.</summary>
        public void Step(float dt)
        {
            RebuildHashIfDirty();

            Vec2 goalCenter = GridMap.CellCenter(_flowField.GoalX, _flowField.GoalY);
            float goalRadiusSq = _config.GoalRadius * _config.GoalRadius;

            for (int i = 0; i < Count; i++)
            {
                if (!_alive[i]) continue;

                var pos = new Vec2(_posX[i], _posY[i]);

                if (Vec2.DistanceSquared(pos, goalCenter) <= goalRadiusSq)
                {
                    // Arrived: leaves the sim. (Vault damage hookup comes with the wave system.)
                    _alive[i] = false;
                    AliveCount--;
                    ReachedCount++;
                    continue;
                }

                var (cx, cy) = _map.WorldToCell(pos);
                Vec2 flowDir = _flowField.DirectionAt(cx, cy);
                Vec2 separation = ComputeSeparation(i, pos);
                Vec2 desired = (flowDir + separation * _config.SeparationWeight).Normalized();

                Vec2 next = pos + desired * (_config.MoveSpeed * dt);
                (_posX[i], _posY[i]) = ResolveWalls(pos, next);
            }

            // Positions changed; neighbor queries next tick need a fresh hash.
            _hashDirty = true;
        }

        /// <summary>Damages every living agent within the circle. Returns kills. O(neighbors), not O(agents).</summary>
        public int ApplyRadialDamage(Vec2 center, float radius, float damage)
        {
            RebuildHashIfDirty();
            _queryScratch.Clear();
            _hash.QueryCircle(center, radius, _queryScratch);

            int kills = 0;
            foreach (int hashId in _queryScratch)
            {
                int id = _hashToAgent[hashId];
                if (!_alive[id]) continue;
                _health[id] -= damage;
                if (_health[id] <= 0f)
                {
                    _alive[id] = false;
                    AliveCount--;
                    kills++;
                    _hashDirty = true;
                }
            }

            TotalKills += kills;
            return kills;
        }

        /// <summary>FNV-1a over live agent state. Two worlds fed identical inputs must match — the determinism regression guard.</summary>
        public ulong StateHash()
        {
            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offsetBasis;

            for (int i = 0; i < Count; i++)
            {
                hash = Mix(hash, _alive[i] ? 1 : 0);
                if (!_alive[i]) continue;
                hash = Mix(hash, BitConverter.SingleToInt32Bits(_posX[i]));
                hash = Mix(hash, BitConverter.SingleToInt32Bits(_posY[i]));
                hash = Mix(hash, BitConverter.SingleToInt32Bits(_health[i]));
            }

            return Mix(hash, ReachedCount);

            static ulong Mix(ulong h, int value)
            {
                unchecked
                {
                    return (h ^ (uint)value) * prime;
                }
            }
        }

        /// <summary>The spatial hash indexes only living agents (compacted ids); _hashToAgent maps hash id → agent id.</summary>
        private void RebuildHashIfDirty()
        {
            if (!_hashDirty) return;

            _hash.Clear();
            _hashToAgent.Clear();
            int nextHashId = 0;
            for (int i = 0; i < Count; i++)
            {
                if (!_alive[i]) continue;
                _hash.Insert(nextHashId++, new Vec2(_posX[i], _posY[i]));
                _hashToAgent.Add(i);
            }
            _hashDirty = false;
        }

        private Vec2 ComputeSeparation(int selfIndex, Vec2 pos)
        {
            _queryScratch.Clear();
            _hash.QueryCircle(pos, _config.SeparationRadius, _queryScratch);

            Vec2 push = Vec2.Zero;
            foreach (int hashId in _queryScratch)
            {
                int otherIndex = _hashToAgent[hashId];
                if (otherIndex == selfIndex) continue;

                Vec2 away = pos - new Vec2(_posX[otherIndex], _posY[otherIndex]);
                float distSq = away.LengthSquared;
                if (distSq < 1e-8f)
                {
                    // Perfectly stacked agents: deterministic nudge by index parity.
                    away = otherIndex > selfIndex ? new Vec2(0.01f, 0f) : new Vec2(-0.01f, 0f);
                    distSq = away.LengthSquared;
                }
                // Closer neighbors push harder (inverse-square falloff, bounded by the query radius).
                push += away * (1f / distSq);
            }

            return push.Normalized();
        }

        /// <summary>Blocked-cell collision with axis sliding: try full move, then x-only, then y-only, else stay.</summary>
        private (float X, float Y) ResolveWalls(Vec2 from, Vec2 to)
        {
            if (IsOpen(to)) return (to.X, to.Y);

            var slideX = new Vec2(to.X, from.Y);
            if (IsOpen(slideX)) return (slideX.X, slideX.Y);

            var slideY = new Vec2(from.X, to.Y);
            if (IsOpen(slideY)) return (slideY.X, slideY.Y);

            return (from.X, from.Y);
        }

        private bool IsOpen(Vec2 pos)
        {
            if (pos.X < 0f || pos.Y < 0f || pos.X >= _map.Width || pos.Y >= _map.Height)
                return false;
            var (x, y) = _map.WorldToCell(pos);
            return !_map.IsBlocked(x, y);
        }
    }
}
