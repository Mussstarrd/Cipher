#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

namespace Cipher.Sim.Emplacements
{
    /// <summary>Graybox Sentry .50 numbers (docs/design/economy-towers-and-aiming.md B).</summary>
    public sealed class TurretConfig
    {
        public float Range { get; set; } = 10f;
        public float DamagePerShot { get; set; } = 5f;
        public float ShotsPerSecond { get; set; } = 12f;   // 60 dps
        public ushort MaxHp { get; set; } = 300;
    }

    public sealed class Turret
    {
        public int X { get; }
        public int Y { get; }
        public Vec2 Center => GridMap.CellCenter(X, Y);
        public ushort Hp { get; internal set; }
        public float FireCooldown { get; internal set; }
        public int Kills { get; internal set; }
        public int ShotsFired { get; internal set; }
        public bool Alive => Hp > 0;

        internal Turret(int x, int y, ushort hp) { X = x; Y = y; Hp = hp; }
    }

    /// <summary>A turret shot that happened this tick; the game layer draws it.</summary>
    public readonly struct TurretShot
    {
        public readonly int TurretIndex;
        public readonly Vec2 From;
        public readonly Vec2 To;
        public readonly bool Killed;
        public TurretShot(int turretIndex, Vec2 from, Vec2 to, bool killed) { TurretIndex = turretIndex; From = from; To = to; Killed = killed; }
    }

    /// <summary>
    /// Player emplacements. Deterministic: turrets step in placement order, target "first"
    /// (the living runner closest to the exit by flow-field cost, lowest id on ties), and all
    /// damage goes through AgentWorld. Turrets occupy a Structure cell so pathing routes around them.
    /// </summary>
    public sealed class TurretSystem
    {
        private readonly TurretConfig _cfg;
        private readonly List<Turret> _turrets = new List<Turret>(32);

        public TurretSystem(TurretConfig config)
        {
            _cfg = config ?? throw new ArgumentNullException(nameof(config));
        }

        public IReadOnlyList<Turret> Turrets => _turrets;
        public TurretConfig Config => _cfg;

        /// <summary>Occupies the cell as a Structure. Caller validates buildability first (BuildValidator).</summary>
        public int Place(GridMap map, int x, int y)
        {
            map.SetWall(x, y, WallKind.Structure, _cfg.MaxHp);
            _turrets.Add(new Turret(x, y, _cfg.MaxHp));
            return _turrets.Count - 1;
        }

        public int IndexAt(int x, int y)
        {
            for (int i = 0; i < _turrets.Count; i++)
                if (_turrets[i].X == x && _turrets[i].Y == y) return i;
            return -1;
        }

        /// <summary>Removes the turret and frees its cell. Returns the remaining hit-point fraction (sell refunds scale on it).</summary>
        public float Remove(GridMap map, int index)
        {
            var t = _turrets[index];
            float fraction = _cfg.MaxHp == 0 ? 0f : (float)t.Hp / _cfg.MaxHp;
            map.Clear(t.X, t.Y);
            _turrets.RemoveAt(index);
            return fraction;
        }

        /// <summary>Structure damage (Spitters). Returns true when the turret is destroyed; its cell is freed.</summary>
        public bool Damage(GridMap map, int index, int amount)
        {
            var t = _turrets[index];
            if (amount <= 0 || t.Hp == 0) return t.Hp == 0;
            t.Hp = (ushort)Math.Max(0, t.Hp - amount);
            map.Damage(t.X, t.Y, amount);
            if (t.Hp > 0) return false;
            map.Clear(t.X, t.Y);
            _turrets.RemoveAt(index);
            return true;
        }

        /// <summary>Fires every ready turret at its "first" target. Shots are appended to <paramref name="shots"/> (may be null).</summary>
        public int Step(AgentWorld world, float dt, List<TurretShot>? shots)
        {
            int kills = 0;
            float interval = 1f / Math.Max(0.01f, _cfg.ShotsPerSecond);
            for (int i = 0; i < _turrets.Count; i++)
            {
                var t = _turrets[i];
                t.FireCooldown -= dt;
                // Catch up at most a few shots after a hitch; never an unbounded burst.
                int burst = 0;
                while (t.FireCooldown <= 0f && burst++ < 4)
                {
                    int target = world.FindFirstInRange(t.Center, _cfg.Range);
                    if (target < 0) { t.FireCooldown = 0f; break; }
                    t.FireCooldown += interval;
                    t.ShotsFired++;
                    Vec2 to = world.PositionOf(target);
                    bool killed = world.ApplyDamage(target, _cfg.DamagePerShot);
                    if (killed) { t.Kills++; kills++; }
                    shots?.Add(new TurretShot(i, t.Center, to, killed));
                }
                if (t.FireCooldown < 0f) t.FireCooldown = 0f;
            }
            return kills;
        }

        /// <summary>FNV-1a over turret cells, hp and cooldowns; mix into the match hash.</summary>
        public ulong StateHash()
        {
            const ulong prime = 1099511628211UL;
            ulong h = 14695981039346656037UL;
            unchecked
            {
                foreach (var t in _turrets)
                {
                    h = (h ^ (uint)t.X) * prime;
                    h = (h ^ (uint)t.Y) * prime;
                    h = (h ^ t.Hp) * prime;
                    h = (h ^ (uint)BitConverter.SingleToInt32Bits(t.FireCooldown)) * prime;
                }
            }
            return h;
        }
    }
}
