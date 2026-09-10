#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

namespace Cipher.Sim.Agents
{
    public enum Archetype : byte
    {
        Runner = 0,
        /// <summary>Walks to the wall that shortcuts most, plants a kit, opens a staged breach.</summary>
        Sapper = 1,
        /// <summary>Hunts player structures (turrets) and lobs acid from range.</summary>
        Spitter = 2,
    }

    public enum SimEventKind : byte
    {
        /// <summary>A = agent id, B = wall cell index. The Sapper chose its wall.</summary>
        SapperTargeted,
        /// <summary>A = agent id, B = wall cell index, F = seconds until the hole opens.</summary>
        BreachPlanting,
        /// <summary>B = cell index, F = stage as float.</summary>
        BreachStage,
        /// <summary>B = cell index.</summary>
        BreachCollapsed,
        /// <summary>B = cell index.</summary>
        BreachRepaired,
        /// <summary>A = agent id, B = structure index, F = damage.</summary>
        StructureHit,
        /// <summary>A = agent id, B = structure index. A Spitter locked on.</summary>
        SpitterEngaged,
    }

    public readonly struct SimEvent
    {
        public readonly SimEventKind Kind;
        public readonly int A;
        public readonly int B;
        public readonly float F;
        public SimEvent(SimEventKind kind, int a, int b, float f) { Kind = kind; A = a; B = b; F = f; }
    }

    /// <summary>What Spitters can see: positions of player structures, in a stable order per tick.</summary>
    public interface IStructureQuery
    {
        int Count { get; }
        Vec2 PositionAt(int index);
    }

    /// <summary>
    /// Rare archetypes and breach progression. Kept in a partial so the hot runner loop in
    /// AgentWorld.cs stays branch-light. All randomness stays outside the sim: the game layer
    /// decides WHEN to spawn a Sapper or Spitter; everything they do afterwards is deterministic.
    /// </summary>
    public sealed partial class AgentWorld
    {
        private enum SapperState : byte { Seek = 0, Plant = 1 }
        private enum SpitterState : byte { Follow = 0, Approach = 1, Attack = 2 }

        private sealed class SapperPlan
        {
            public readonly List<int> Path = new List<int>(128); // cell indices, first = next waypoint
            public int Next;
            public int WallCell;
            public int StandCell;
        }

        private struct Breach
        {
            public int Cell;
            public float Timer;
        }

        private readonly Dictionary<int, SapperPlan> _plans = new Dictionary<int, SapperPlan>();
        private readonly List<Breach> _breaches = new List<Breach>(8);
        private readonly List<SimEvent> _events = new List<SimEvent>(32);
        private readonly List<int> _planScratch = new List<int>(8);
        private int[]? _bfsDist;
        private int[]? _bfsParent;
        private int[]? _bfsQueue;

        /// <summary>Set by the game layer so Spitters can see turrets. May be null.</summary>
        public IStructureQuery? Structures { get; set; }

        public int ActiveBreachCount => _breaches.Count;

        public Archetype ArchetypeOf(int id) => (Archetype)_archetype[id];

        public int CountAlive(Archetype archetype)
        {
            int n = 0;
            for (int i = 0; i < Count; i++)
                if (_alive[i] && _archetype[i] == (byte)archetype) n++;
            return n;
        }

        /// <summary>Number of living Sappers still walking to or planting at a wall.</summary>
        public int ActiveSapperCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Count; i++)
                    if (_alive[i] && _archetype[i] == (byte)Archetype.Sapper) n++;
                return n;
            }
        }

        /// <summary>The wall cell a Sapper is heading for, or -1.</summary>
        public int SapperTargetCell(int id) => _archetype[id] == (byte)Archetype.Sapper ? _target[id] : -1;

        /// <summary>Spawns a rare archetype with its configured health. Sappers pick a wall immediately.</summary>
        public int SpawnArchetype(Vec2 position, Archetype archetype)
        {
            float health = archetype switch
            {
                Archetype.Sapper => _config.SapperHealth,
                Archetype.Spitter => _config.SpitterHealth,
                _ => _config.RunnerHealth,
            };
            int id = SpawnInternal(position, health, archetype);
            if (archetype == Archetype.Sapper) PlanSapper(id);
            return id;
        }

        /// <summary>Moves accumulated events to <paramref name="into"/> and clears them.</summary>
        public void DrainEvents(List<SimEvent> into)
        {
            into.AddRange(_events);
            _events.Clear();
        }

        /// <summary>Repairs one breach stage toward Intact. Returns the new stage.</summary>
        public BreachStage RepairStage(int x, int y)
        {
            BreachStage stage = _map.RepairStage(x, y);
            int cell = _map.CellIndex(x, y);
            for (int b = _breaches.Count - 1; b >= 0; b--)
            {
                if (_breaches[b].Cell != cell) continue;
                if (stage == BreachStage.Intact) _breaches.RemoveAt(b);
                else _breaches[b] = new Breach { Cell = cell, Timer = _config.BreachStageSeconds };
            }
            if (stage == BreachStage.Intact) _events.Add(new SimEvent(SimEventKind.BreachRepaired, -1, cell, 0f));
            else _events.Add(new SimEvent(SimEventKind.BreachStage, -1, cell, (float)stage));
            return stage;
        }

        /// <summary>Seconds until the breach at the cell advances, or -1 if none is active there.</summary>
        public float BreachTimerAt(int x, int y)
        {
            int cell = _map.CellIndex(x, y);
            foreach (var b in _breaches) if (b.Cell == cell) return b.Timer;
            return -1f;
        }

        // ---------------------------------------------------------------- sapper

        /// <summary>Returns true if the Sapper handled its own movement this tick.</summary>
        private bool StepSapper(int i, Vec2 pos, float dt, bool gates)
        {
            if (!_plans.TryGetValue(i, out var plan)) return false;

            int wall = plan.WallCell;
            int wx = wall % _map.Width, wy = wall / _map.Width;
            if (!IsBreachableIntact(wx, wy))
            {
                // Someone sold, breached or collapsed it first: pick again.
                _plans.Remove(i);
                PlanSapper(i);
                return _plans.ContainsKey(i) && StepSapper(i, pos, dt, gates);
            }

            if ((SapperState)_state[i] == SapperState.Plant)
            {
                _timer[i] -= dt;
                if (_timer[i] <= 0f)
                {
                    BreachStage stage = _map.Breach(wx, wy);
                    _breaches.Add(new Breach { Cell = wall, Timer = _config.BreachStageSeconds });
                    _events.Add(new SimEvent(SimEventKind.BreachStage, i, wall, (float)stage));
                    _plans.Remove(i);
                    _archetype[i] = (byte)Archetype.Runner; // job done: joins the flood through its own hole
                    _state[i] = 0;
                    _target[i] = -1;
                }
                return true; // planting: stands still
            }

            // Seek: walk the 4-connected BFS path to the stand cell.
            if (plan.Next >= plan.Path.Count)
            {
                _state[i] = (byte)SapperState.Plant;
                _timer[i] = _config.SapperPlantSeconds;
                _events.Add(new SimEvent(SimEventKind.BreachPlanting, i, wall, _config.SapperPlantSeconds));
                return true;
            }

            int wp = plan.Path[plan.Next];
            Vec2 target = GridMap.CellCenter(wp % _map.Width, wp / _map.Width);
            Vec2 delta = target - pos;
            float dist = delta.Length;
            if (dist < 0.35f)
            {
                plan.Next++;
                return true;
            }
            float step = MathF.Min(_config.SapperSpeed * dt, 0.9f);
            Vec2 next = pos + delta * (MathF.Min(step, dist) / dist);
            MoveResolved(i, pos, next, gates);
            return true;
        }

        private bool IsBreachableIntact(int x, int y)
        {
            WallKind kind = _map.KindAt(x, y);
            return (kind == WallKind.Wall || kind == WallKind.Barricade) && _map.StageAt(x, y) == BreachStage.Intact;
        }

        /// <summary>
        /// Chooses the wall that saves the most walking: BFS over open cells from the Sapper, then
        /// for every intact breachable wall adjacent to the reachable region whose far side has a
        /// finite path to the goal, score = walk-to-wall + 1 + far-side integration. The lowest
        /// score wins if it beats the current route by SapperMinGain, or unconditionally when the
        /// Sapper is sealed in (no route at all). Deterministic: ties go to the lowest cell index.
        /// </summary>
        private void PlanSapper(int id)
        {
            _target[id] = -1;
            _state[id] = (byte)SapperState.Seek;
            int w = _map.Width, h = _map.Height, n = w * h;
            _bfsDist ??= new int[n];
            _bfsParent ??= new int[n];
            _bfsQueue ??= new int[n];
            Array.Fill(_bfsDist, -1);

            var (sx, sy) = _map.WorldToCell(new Vec2(_posX[id], _posY[id]));
            int start = sy * w + sx;
            int head = 0, tail = 0;
            _bfsDist[start] = 0;
            _bfsParent[start] = -1;
            _bfsQueue[tail++] = start;

            float current = _flowField.IntegrationCostAt(sx, sy);
            bool sealedIn = float.IsPositiveInfinity(current);
            float bestScore = float.PositiveInfinity;
            int bestWall = -1, bestStand = -1;

            while (head < tail)
            {
                int c = _bfsQueue[head++];
                int cx = c % w, cy = c / w;
                int d = _bfsDist[c];

                for (int k = 0; k < 4; k++)
                {
                    int dx = k == 0 ? 1 : k == 1 ? -1 : 0;
                    int dy = k == 2 ? 1 : k == 3 ? -1 : 0;
                    int nx = cx + dx, ny = cy + dy;
                    if (!_map.InBounds(nx, ny)) continue;
                    int ni = ny * w + nx;

                    if (!_map.IsBlocked(nx, ny))
                    {
                        if (_bfsDist[ni] < 0)
                        {
                            _bfsDist[ni] = d + 1;
                            _bfsParent[ni] = c;
                            _bfsQueue[tail++] = ni;
                        }
                        continue;
                    }

                    // A wall: is it breachable, and does the far side lead to the goal?
                    if (!IsBreachableIntact(nx, ny)) continue;
                    int fx = nx + dx, fy = ny + dy;
                    if (!_map.InBounds(fx, fy) || _map.IsBlocked(fx, fy)) continue;
                    float far = _flowField.IntegrationCostAt(fx, fy);
                    if (float.IsPositiveInfinity(far)) continue;

                    float score = d + 2f + far;
                    if (score < bestScore || (score == bestScore && ni < bestWall))
                    {
                        bestScore = score;
                        bestWall = ni;
                        bestStand = c;
                    }
                }
            }

            if (bestWall < 0) return;
            if (!sealedIn && current - bestScore < _config.SapperMinGain) return;

            var plan = new SapperPlan { WallCell = bestWall, StandCell = bestStand };
            for (int c = bestStand; c != -1; c = _bfsParent[c]) plan.Path.Add(c);
            plan.Path.Reverse(); // start → stand
            plan.Next = plan.Path.Count > 0 && plan.Path[0] == start ? 1 : 0;
            _plans[id] = plan;
            _state[id] = (byte)SapperState.Seek;
            _target[id] = bestWall;
            _events.Add(new SimEvent(SimEventKind.SapperTargeted, id, bestWall, bestScore));
        }

        // ---------------------------------------------------------------- spitter

        /// <summary>Returns true if the Spitter handled its own movement this tick.</summary>
        private bool StepSpitter(int i, Vec2 pos, float dt, bool gates)
        {
            var structures = Structures;
            if (structures == null || structures.Count == 0)
            {
                _state[i] = (byte)SpitterState.Follow;
                _target[i] = -1;
                return false;
            }

            // Re-acquire every tick: nearest structure inside the acquire range (lowest index on ties).
            int best = -1;
            float bestDistSq = _config.SpitterAcquireRange * _config.SpitterAcquireRange;
            for (int s = 0; s < structures.Count; s++)
            {
                float dsq = Vec2.DistanceSquared(pos, structures.PositionAt(s));
                if (dsq < bestDistSq) { bestDistSq = dsq; best = s; }
            }

            if (best < 0)
            {
                _state[i] = (byte)SpitterState.Follow;
                _target[i] = -1;
                return false;
            }

            if (_target[i] != best)
            {
                _target[i] = best;
                _timer[i] = _config.SpitterAttackInterval * 0.5f; // first glob comes quickly once in range
                _events.Add(new SimEvent(SimEventKind.SpitterEngaged, i, best, 0f));
            }

            Vec2 targetPos = structures.PositionAt(best);
            float dist = MathF.Sqrt(bestDistSq);
            if (dist > _config.SpitterAttackRange)
            {
                _state[i] = (byte)SpitterState.Approach;
                Vec2 dir = (targetPos - pos) * (1f / dist);
                float step = MathF.Min(_config.SpitterSpeed * dt, 0.9f);
                Vec2 before = pos;
                MoveResolved(i, pos, pos + dir * step, gates);
                // Wall in the way: fall back to the field this tick so it never freezes against a barricade.
                if (Vec2.DistanceSquared(before, new Vec2(_posX[i], _posY[i])) < 1e-6f)
                    StepRunner(i, pos, dt, gates, _config.SpitterSpeed);
                return true;
            }

            _state[i] = (byte)SpitterState.Attack;
            _timer[i] -= dt;
            if (_timer[i] <= 0f)
            {
                _timer[i] += _config.SpitterAttackInterval;
                _events.Add(new SimEvent(SimEventKind.StructureHit, i, best, _config.SpitterDamage));
            }
            return true; // holds position while spitting
        }

        // ---------------------------------------------------------------- breaches

        /// <summary>Drops plans belonging to Sappers that died (or finished and became runners).</summary>
        private void PruneDeadPlans()
        {
            if (_plans.Count == 0) return;
            _planScratch.Clear();
            foreach (var kv in _plans)
                if (!_alive[kv.Key] || _archetype[kv.Key] != (byte)Archetype.Sapper) _planScratch.Add(kv.Key);
            foreach (int id in _planScratch) _plans.Remove(id);
        }

        /// <summary>
        /// Every gated cell on the map must be widening, whoever opened it. Tracking only what the
        /// Sapper registered left holes that never progressed (and so could never be understood or
        /// repaired) whenever a breach arrived by another route. The invariant is cheap to check:
        /// one tracked breach per gate cell.
        /// </summary>
        private void ReconcileBreaches()
        {
            if (_breaches.Count == _map.GateCount) return;

            for (int b = _breaches.Count - 1; b >= 0; b--)
            {
                int c = _breaches[b].Cell;
                if (!_map.IsGate(c % _map.Width, c / _map.Width)) _breaches.RemoveAt(b);
            }
            if (_breaches.Count == _map.GateCount) return;

            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    if (!_map.IsGate(x, y)) continue;
                    int cell = y * _map.Width + x;
                    bool tracked = false;
                    foreach (var b in _breaches) if (b.Cell == cell) { tracked = true; break; }
                    if (!tracked) _breaches.Add(new Breach { Cell = cell, Timer = _config.BreachStageSeconds });
                }
            }
        }

        private void AdvanceBreaches(float dt)
        {
            ReconcileBreaches();

            for (int b = _breaches.Count - 1; b >= 0; b--)
            {
                var br = _breaches[b];
                br.Timer -= dt;
                if (br.Timer > 0f) { _breaches[b] = br; continue; }

                int x = br.Cell % _map.Width, y = br.Cell / _map.Width;
                BreachStage stage = _map.Breach(x, y);
                if (stage == BreachStage.Collapsed)
                {
                    _breaches.RemoveAt(b);
                    _events.Add(new SimEvent(SimEventKind.BreachCollapsed, -1, br.Cell, 0f));
                }
                else
                {
                    br.Timer = _config.BreachStageSeconds;
                    _breaches[b] = br;
                    _events.Add(new SimEvent(SimEventKind.BreachStage, -1, br.Cell, (float)stage));
                }
            }
        }

        /// <summary>Traffic through a hole chews it wider: every admitted agent shaves time off the stage.</summary>
        private void OnGatePassed(int cell)
        {
            for (int b = 0; b < _breaches.Count; b++)
            {
                if (_breaches[b].Cell != cell) continue;
                var br = _breaches[b];
                br.Timer -= _config.TrafficShaveSeconds;
                _breaches[b] = br;
                return;
            }
        }

        private ulong BreachHash()
        {
            const ulong prime = 1099511628211UL;
            ulong h = 14695981039346656037UL;
            unchecked
            {
                foreach (var b in _breaches)
                {
                    h = (h ^ (uint)b.Cell) * prime;
                    h = (h ^ (uint)BitConverter.SingleToInt32Bits(b.Timer)) * prime;
                }
            }
            return h;
        }
    }
}
