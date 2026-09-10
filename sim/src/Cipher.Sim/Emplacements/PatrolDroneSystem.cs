#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;

namespace Cipher.Sim.Emplacements
{
    /// <summary>Cartel Gunship numbers (docs/design/arsenal-and-terrain.md section 1).</summary>
    public sealed class PatrolDroneConfig
    {
        public int Cost { get; set; } = 260;
        /// <summary>Cells per second along the route.</summary>
        public float Speed { get; set; } = 6f;
        public float Range { get; set; } = 6f;
        public float DamagePerShot { get; set; } = 5f;
        public float ShotsPerSecond { get; set; } = 7f;   // 35 dps
        public ushort MaxHp { get; set; } = 120;
        public int MinWaypoints { get; set; } = 2;
        public int MaxWaypoints { get; set; } = 4;
    }

    /// <summary>
    /// A gunship walking a closed loop of waypoints. It flies, so walls do not route it: that is the
    /// point, it is the only defence that can be moved onto a surprise. Position is a pure function
    /// of route length and distance travelled, so there is no pathfinding and the state stays hash-stable.
    /// </summary>
    public sealed class PatrolDrone
    {
        internal readonly Vec2[] Route;
        internal readonly float[] SegmentEnds; // cumulative length at the end of each segment

        /// <summary>Total distance once round the closed loop.</summary>
        public float LoopLength { get; }

        /// <summary>Distance travelled along the loop, wrapped to [0, LoopLength).</summary>
        public float Progress { get; internal set; }
        public Vec2 Position { get; internal set; }
        public Vec2 Heading { get; internal set; } = new Vec2(1f, 0f);
        public ushort Hp { get; internal set; }
        public ushort MaxHp { get; }
        public float FireCooldown { get; internal set; }
        public int Kills { get; internal set; }
        public int ShotsFired { get; internal set; }
        public bool Alive => Hp > 0;
        public IReadOnlyList<Vec2> Waypoints => Route;

        internal PatrolDrone(Vec2[] route, ushort hp)
        {
            Route = route;
            MaxHp = Hp = hp;
            SegmentEnds = new float[route.Length];
            float total = 0f;
            for (int i = 0; i < route.Length; i++)
            {
                total += (route[(i + 1) % route.Length] - route[i]).Length;
                SegmentEnds[i] = total;
            }
            LoopLength = Math.Max(0.001f, total);
            Position = route[0];
        }

        /// <summary>World position at a distance along the loop. Pure; used by the sim and by the preview.</summary>
        public Vec2 PositionAt(float progress)
        {
            float p = progress % LoopLength;
            if (p < 0f) p += LoopLength;
            for (int i = 0; i < Route.Length; i++)
            {
                if (p > SegmentEnds[i]) continue;
                float segStart = i == 0 ? 0f : SegmentEnds[i - 1];
                float segLength = SegmentEnds[i] - segStart;
                Vec2 a = Route[i], b = Route[(i + 1) % Route.Length];
                if (segLength <= 1e-5f) return a;
                return a + (b - a) * ((p - segStart) / segLength);
            }
            return Route[0];
        }
    }

    /// <summary>
    /// Player-placed gunships. Deterministic: drones step in placement order, target the nearest
    /// living agent (lowest id on ties), and all damage goes through AgentWorld.
    /// </summary>
    public sealed class PatrolDroneSystem
    {
        private readonly PatrolDroneConfig _cfg;
        private readonly List<PatrolDrone> _drones = new List<PatrolDrone>(8);

        public PatrolDroneSystem(PatrolDroneConfig? config = null)
        {
            _cfg = config ?? new PatrolDroneConfig();
        }

        public IReadOnlyList<PatrolDrone> Drones => _drones;
        public PatrolDroneConfig Config => _cfg;

        /// <summary>
        /// Adds a gunship on a closed loop through the given cells. Returns -1 when the route is
        /// unusable (too few or too many waypoints, or every point identical).
        /// </summary>
        public int Add(IReadOnlyList<(int X, int Y)> waypoints)
        {
            if (waypoints == null || waypoints.Count < _cfg.MinWaypoints || waypoints.Count > _cfg.MaxWaypoints) return -1;
            var route = new Vec2[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++) route[i] = Grid.GridMap.CellCenter(waypoints[i].X, waypoints[i].Y);

            float span = 0f;
            for (int i = 1; i < route.Length; i++) span += (route[i] - route[0]).Length;
            if (span <= 1e-4f) return -1; // all waypoints on one cell: it would hover, not patrol

            _drones.Add(new PatrolDrone(route, _cfg.MaxHp));
            return _drones.Count - 1;
        }

        public void RemoveAt(int index) => _drones.RemoveAt(index);

        /// <summary>Structure damage (Spitters). Returns true when the gunship is destroyed and removed.</summary>
        public bool Damage(int index, int amount)
        {
            var d = _drones[index];
            if (amount <= 0 || d.Hp == 0) return d.Hp == 0;
            d.Hp = (ushort)Math.Max(0, d.Hp - amount);
            if (d.Hp > 0) return false;
            _drones.RemoveAt(index);
            return true;
        }

        /// <summary>Flies every drone along its loop and fires. Shots are appended to <paramref name="shots"/> (may be null).</summary>
        public int Step(AgentWorld world, float dt, List<TurretShot>? shots)
        {
            int kills = 0;
            float interval = 1f / Math.Max(0.01f, _cfg.ShotsPerSecond);

            for (int i = 0; i < _drones.Count; i++)
            {
                var d = _drones[i];
                Vec2 before = d.Position;
                d.Progress = (d.Progress + _cfg.Speed * dt) % d.LoopLength;
                d.Position = d.PositionAt(d.Progress);
                Vec2 delta = d.Position - before;
                if (delta.LengthSquared > 1e-8f) d.Heading = delta.Normalized();

                d.FireCooldown -= dt;
                int burst = 0;
                while (d.FireCooldown <= 0f && burst++ < 4)
                {
                    int target = world.FindNearestInRange(d.Position, _cfg.Range);
                    if (target < 0) { d.FireCooldown = 0f; break; }
                    d.FireCooldown += interval;
                    d.ShotsFired++;
                    Vec2 to = world.PositionOf(target);
                    bool killed = world.ApplyDamage(target, _cfg.DamagePerShot);
                    if (killed) { d.Kills++; kills++; }
                    shots?.Add(new TurretShot(i, d.Position, to, killed));
                }
                if (d.FireCooldown < 0f) d.FireCooldown = 0f;
            }
            return kills;
        }

        /// <summary>FNV-1a over drone progress, hp and cooldowns; mix into the match hash.</summary>
        public ulong StateHash()
        {
            const ulong prime = 1099511628211UL;
            ulong h = 14695981039346656037UL;
            unchecked
            {
                foreach (var d in _drones)
                {
                    h = (h ^ (uint)BitConverter.SingleToInt32Bits(d.Progress)) * prime;
                    h = (h ^ d.Hp) * prime;
                    h = (h ^ (uint)BitConverter.SingleToInt32Bits(d.FireCooldown)) * prime;
                }
            }
            return h;
        }
    }
}
