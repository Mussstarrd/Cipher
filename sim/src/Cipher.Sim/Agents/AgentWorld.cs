#nullable enable
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
    public sealed partial class AgentWorld
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
        private byte[] _archetype;
        private byte[] _state;
        private float[] _timer;
        private int[] _target;

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
            int capacity = Math.Max(1, initialCapacity);
            _posX = new float[capacity];
            _posY = new float[capacity];
            _health = new float[capacity];
            _alive = new bool[capacity];
            _archetype = new byte[capacity];
            _state = new byte[capacity];
            _timer = new float[capacity];
            _target = new int[capacity];
        }

        public Vec2 PositionOf(int id) => new Vec2(_posX[id], _posY[id]);
        public bool IsAlive(int id) => _alive[id];
        public float HealthOf(int id) => _health[id];

        /// <summary>Spawns a plain runner.</summary>
        public int Spawn(Vec2 position, float health) => SpawnInternal(position, health, Archetype.Runner);

        private int SpawnInternal(Vec2 position, float health, Archetype archetype)
        {
            if (health <= 0f)
                throw new ArgumentOutOfRangeException(nameof(health), "Spawn health must be positive.");

            if (Count == _posX.Length)
            {
                int newSize = Math.Max(4, _posX.Length * 2);
                Array.Resize(ref _posX, newSize);
                Array.Resize(ref _posY, newSize);
                Array.Resize(ref _health, newSize);
                Array.Resize(ref _alive, newSize);
                Array.Resize(ref _archetype, newSize);
                Array.Resize(ref _state, newSize);
                Array.Resize(ref _timer, newSize);
                Array.Resize(ref _target, newSize);
            }

            int id = Count++;
            _posX[id] = position.X;
            _posY[id] = position.Y;
            _health[id] = health;
            _alive[id] = true;
            _archetype[id] = (byte)archetype;
            _state[id] = 0;
            _timer[id] = 0f;
            _target[id] = -1;
            AliveCount++;
            _hashDirty = true;
            return id;
        }

        /// <summary>Advances the sim one fixed tick.</summary>
        public void Step(float dt)
        {
            _flowField.EnsureFresh();
            RebuildHashIfDirty();
            _map.RefillGates(dt);
            bool gates = _map.GateCount > 0;

            Vec2 goalCenter = GridMap.CellCenter(_flowField.GoalX, _flowField.GoalY);
            float goalRadiusSq = _config.GoalRadius * _config.GoalRadius;

            for (int i = 0; i < Count; i++)
            {
                if (!_alive[i]) continue;

                var pos = new Vec2(_posX[i], _posY[i]);

                if (Vec2.DistanceSquared(pos, goalCenter) <= goalRadiusSq)
                {
                    // Arrived: leaves the sim. The match layer turns ReachedCount deltas into vault damage.
                    _alive[i] = false;
                    AliveCount--;
                    ReachedCount++;
                    continue;
                }

                switch ((Archetype)_archetype[i])
                {
                    case Archetype.Sapper:
                        if (StepSapper(i, pos, dt, gates)) continue;
                        break; // no plan: walks like a runner
                    case Archetype.Spitter:
                        if (StepSpitter(i, pos, dt, gates)) continue;
                        break;
                }

                StepRunner(i, pos, dt, gates, _config.MoveSpeed);
            }

            AdvanceBreaches(dt);

            // Positions changed; neighbor queries next tick need a fresh hash.
            _hashDirty = true;
        }

        /// <summary>Field-following movement with separation, wall sliding and gate throttling.</summary>
        private void StepRunner(int i, Vec2 pos, float dt, bool gates, float speed)
        {
            var (cx, cy) = _map.WorldToCell(pos);
            Vec2 flowDir = _flowField.DirectionAt(cx, cy);
            Vec2 separation = ComputeSeparation(i, pos);
            Vec2 desired = (flowDir + separation * _config.SeparationWeight).Normalized();

            // Displacement is capped below one cell per tick so agents can never
            // tunnel a wall between two cells CanTravel never gets to inspect.
            float stepLength = MathF.Min(speed * dt, 0.9f);
            MoveResolved(i, pos, pos + desired * stepLength, gates);
        }

        /// <summary>Applies a move through the wall rule and, when holes exist, the gate rule.</summary>
        private void MoveResolved(int i, Vec2 pos, Vec2 next, bool gates)
        {
            Vec2 resolved = Movement.ResolveWalls(_map, pos, next);
            if (gates)
            {
                // Entering a breach hole from outside it costs a gate token; refused agents
                // hold position and bunch at the mouth (the "1 per second" throttle).
                var (cx, cy) = _map.WorldToCell(pos);
                var (rx, ry) = _map.WorldToCell(resolved);
                if ((rx != cx || ry != cy) && _map.IsGate(rx, ry) && !_map.IsGate(cx, cy))
                {
                    if (_map.TryEnterGate(rx, ry)) OnGatePassed(_map.CellIndex(rx, ry));
                    else resolved = pos;
                }
            }
            _posX[i] = resolved.X;
            _posY[i] = resolved.Y;
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

        /// <summary>Damages one agent. Returns true if this call killed it. Dead agents are ignored.</summary>
        public bool ApplyDamage(int id, float damage)
        {
            if (id < 0 || id >= Count || !_alive[id]) return false;
            _health[id] -= damage;
            if (_health[id] > 0f) return false;
            _alive[id] = false;
            AliveCount--;
            TotalKills++;
            _hashDirty = true;
            return true;
        }

        /// <summary>Number of living agents within the circle (hero contact damage, trap triggers).</summary>
        public int CountWithin(Vec2 center, float radius)
        {
            RebuildHashIfDirty();
            _queryScratch.Clear();
            _hash.QueryCircle(center, radius, _queryScratch);
            float rSq = radius * radius;
            int n = 0;
            foreach (int hashId in _queryScratch)
            {
                int id = _hashToAgent[hashId];
                if (!_alive[id]) continue;
                if (Vec2.DistanceSquared(center, new Vec2(_posX[id], _posY[id])) <= rSq) n++;
            }
            return n;
        }

        /// <summary>
        /// Hitscan: the first living agent whose center lies within <paramref name="hitRadius"/>
        /// of the segment origin → origin + dir * maxDistance. Ties resolve to the lower id
        /// (deterministic). Returns false on a miss. Does not mutate state.
        /// </summary>
        public bool Raycast(Vec2 origin, Vec2 direction, float maxDistance, float hitRadius, out int hitId, out float hitDistance)
        {
            hitId = -1;
            hitDistance = float.PositiveInfinity;
            if (maxDistance <= 0f) return false;
            Vec2 dir = direction.Normalized();
            if (dir.LengthSquared < 0.5f) return false; // zero direction

            RebuildHashIfDirty();
            _queryScratch.Clear();
            // One broad-phase circle covering the whole segment plus the hit radius.
            Vec2 mid = origin + dir * (maxDistance * 0.5f);
            _hash.QueryCircle(mid, maxDistance * 0.5f + hitRadius, _queryScratch);

            float rSq = hitRadius * hitRadius;
            foreach (int hashId in _queryScratch)
            {
                int id = _hashToAgent[hashId];
                if (!_alive[id]) continue;
                Vec2 rel = new Vec2(_posX[id], _posY[id]) - origin;
                float t = Vec2.Dot(rel, dir);
                if (t < 0f || t > maxDistance) continue;
                Vec2 closest = dir * t;
                if (Vec2.DistanceSquared(rel, closest) > rSq) continue;
                if (t < hitDistance || (t == hitDistance && id < hitId))
                {
                    hitDistance = t;
                    hitId = id;
                }
            }
            return hitId >= 0;
        }

        /// <summary>
        /// "First" targeting: the living agent inside the circle that is closest to the goal by
        /// flow-field cost (ties: lower id). Returns -1 when none. Deterministic; does not mutate.
        /// </summary>
        public int FindFirstInRange(Vec2 center, float radius)
        {
            RebuildHashIfDirty();
            _queryScratch.Clear();
            _hash.QueryCircle(center, radius, _queryScratch);
            int best = -1;
            float bestCost = float.PositiveInfinity;
            foreach (int hashId in _queryScratch)
            {
                int id = _hashToAgent[hashId];
                if (!_alive[id]) continue;
                var (cx, cy) = _map.WorldToCell(new Vec2(_posX[id], _posY[id]));
                float cost = _flowField.IntegrationCostAt(cx, cy);
                if (cost < bestCost || (cost == bestCost && id < best))
                {
                    bestCost = cost;
                    best = id;
                }
            }
            return best;
        }

        /// <summary>The grid this world walks on (build validation, preview).</summary>
        public GridMap Map => _map;

        /// <summary>True when any living agent stands in the cell (placement must never entomb an agent).</summary>
        public bool IsCellOccupied(int x, int y)
        {
            if (AliveCount == 0) return false;
            RebuildHashIfDirty();
            _queryScratch.Clear();
            _hash.QueryCircle(GridMap.CellCenter(x, y), 0.71f, _queryScratch);
            foreach (int hashId in _queryScratch)
            {
                int id = _hashToAgent[hashId];
                if (!_alive[id]) continue;
                var (ax, ay) = _map.WorldToCell(new Vec2(_posX[id], _posY[id]));
                if (ax == x && ay == y) return true;
            }
            return false;
        }

        /// <summary>FNV-1a over live agent state and wall state. Two worlds fed identical inputs must match — the determinism regression guard.</summary>
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
                hash = Mix(hash, _archetype[i] | (_state[i] << 8));
                hash = Mix(hash, BitConverter.SingleToInt32Bits(_timer[i]));
                hash = Mix(hash, _target[i]);
            }

            hash = Mix(hash, ReachedCount);
            hash = Mix(hash, (int)(BreachHash() & 0xFFFFFFFF));
            return hash ^ _map.StateHash();

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
    }
}
