#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

namespace Cipher.Sim.Emplacements
{
    /// <summary>How a family delivers damage.</summary>
    public enum FireMode
    {
        /// <summary>One target per shot: the living agent closest to the vault by flow-field cost.</summary>
        Single = 0,
        /// <summary>Everything inside the radius, every shot, no target cap.</summary>
        Area = 1,
    }

    /// <summary>
    /// A tower family as data (docs/design/arsenal-and-terrain.md section 1). Adding a tower is a
    /// catalogue entry, not code — Trap 7 in the blueprint is content-scaling debt, and the way out
    /// is to keep every tower's numbers and upgrade ladder in a table.
    /// </summary>
    public sealed class TurretFamily
    {
        public string Name { get; }
        public int Cost { get; }
        public float Range { get; }
        public float DamagePerShot { get; }
        public float ShotsPerSecond { get; }
        public ushort MaxHp { get; }
        public FireMode Mode { get; }
        public TurretTier[] Tiers { get; }

        public float DamagePerSecond => DamagePerShot * ShotsPerSecond;

        public TurretFamily(string name, int cost, float range, float damagePerShot, float shotsPerSecond,
                            ushort maxHp, FireMode mode, TurretTier[] tiers)
        {
            Name = name; Cost = cost; Range = range; DamagePerShot = damagePerShot;
            ShotsPerSecond = shotsPerSecond; MaxHp = maxHp; Mode = mode; Tiers = tiers;
        }
    }

    public sealed class TurretTier
    {
        public string Name { get; }
        public int Cost { get; }
        public float DamageMultiplier { get; }
        public float RangeBonus { get; }
        public ushort HpBonus { get; }

        public TurretTier(string name, int cost, float damageMultiplier, float rangeBonus, ushort hpBonus)
        {
            Name = name; Cost = cost; DamageMultiplier = damageMultiplier; RangeBonus = rangeBonus; HpBonus = hpBonus;
        }
    }

    /// <summary>The graybox tower catalogue. Replace with a loaded data file once scenarios land.</summary>
    public static class TurretCatalog
    {
        // BALANCE, owner 2026-09-11: "The turrets are killing the mobs to easily a single turret
        // should be able to be overrun if I don't intervene come on."
        //
        // The Sentry was 60 dps against 34-hp bodies -- one kill every half second, forever, with no
        // input from the player. It is 22 now. Paired with opportunists hitting guns harder, a lone
        // turret holds a lane against a trickle and loses it to a wave, which is the decision the
        // whole build layer exists to give him.

        /// <summary>Long reach, single target, picks the runner nearest the vault. Answers leakers down a lane.</summary>
        public static TurretFamily Sentry { get; } = new TurretFamily(
            "Sentry .50", cost: 150, range: 10f, damagePerShot: 4f, shotsPerSecond: 5.5f, maxHp: 300, FireMode.Single,
            new[]
            {
                new TurretTier("Twin .50", cost: 120, damageMultiplier: 1.6f, rangeBonus: 0f, hpBonus: 100),
                new TurretTier("Overwatch", cost: 200, damageMultiplier: 1f, rangeBonus: 4f, hpBonus: 100),
            });

        /// <summary>
        /// Short, all-round, hits everything at once. Wants to sit at a corner where the crowd bunches.
        ///
        /// A rotary mower deck off a tractor, spun up off a generator: the most plausible area weapon
        /// a rural property actually owns. It was called the "Chop-Shop Rotor", which is vocabulary
        /// from the kingpin fiction ADR-003 retired -- there is no chop shop in a lake community.
        /// </summary>
        public static TurretFamily Grinder { get; } = new TurretFamily(
            "Brush Hog", cost: 120, range: 2.4f, damagePerShot: 2.2f, shotsPerSecond: 5f, maxHp: 260, FireMode.Area,
            new[]
            {
                new TurretTier("Flail Drum", cost: 110, damageMultiplier: 1.7f, rangeBonus: 0f, hpBonus: 90),
                new TurretTier("Wide Deck", cost: 180, damageMultiplier: 1f, rangeBonus: 1f, hpBonus: 90),
            });

        public static TurretFamily[] All { get; } = { Sentry, Grinder };
    }

    public sealed class Turret
    {
        public int X { get; }
        public int Y { get; }
        public int Family { get; }
        public Vec2 Center => GridMap.CellCenter(X, Y);
        public ushort Hp { get; internal set; }
        public ushort MaxHp { get; internal set; }
        public float FireCooldown { get; internal set; }
        public int Kills { get; internal set; }
        public int ShotsFired { get; internal set; }
        public int Tier { get; internal set; }
        public float Range { get; internal set; }
        public float DamagePerShot { get; internal set; }
        public int Invested { get; internal set; }
        public bool Alive => Hp > 0;

        internal Turret(int x, int y, int family, ushort hp, float range, float damage)
        {
            X = x; Y = y; Family = family; Hp = hp; MaxHp = hp; Range = range; DamagePerShot = damage;
        }
    }

    /// <summary>A turret shot that happened this tick; the game layer draws it.</summary>
    public readonly struct TurretShot
    {
        public readonly int TurretIndex;
        public readonly Vec2 From;
        public readonly Vec2 To;
        public readonly bool Killed;
        /// <summary>Area families sweep rather than fire a tracer; the game draws these differently.</summary>
        public readonly bool Area;

        public TurretShot(int turretIndex, Vec2 from, Vec2 to, bool killed, bool area = false)
        {
            TurretIndex = turretIndex; From = from; To = to; Killed = killed; Area = area;
        }
    }

    /// <summary>
    /// Player emplacements. Deterministic: turrets step in placement order, single-fire families
    /// target "first" (the living runner closest to the vault, lowest id on ties), and all damage
    /// goes through AgentWorld. Turrets occupy a Structure cell so pathing routes around them.
    /// </summary>
    public sealed class TurretSystem
    {
        private readonly TurretFamily[] _families;
        private readonly List<Turret> _turrets = new List<Turret>(32);

        public TurretSystem(TurretFamily[]? families = null)
        {
            _families = families ?? TurretCatalog.All;
            if (_families.Length == 0) throw new ArgumentException("Need at least one turret family.", nameof(families));
        }

        public IReadOnlyList<Turret> Turrets => _turrets;
        public IReadOnlyList<TurretFamily> Families => _families;
        public TurretFamily FamilyOf(int index) => _families[Math.Clamp(index, 0, _families.Length - 1)];
        public TurretFamily FamilyOfTurret(int turretIndex) => FamilyOf(_turrets[turretIndex].Family);

        /// <summary>Occupies the cell as a Structure. Caller validates buildability first (BuildValidator).</summary>
        public int Place(GridMap map, int x, int y, int family = 0)
        {
            TurretFamily f = FamilyOf(family);
            map.SetWall(x, y, WallKind.Structure, f.MaxHp);
            _turrets.Add(new Turret(x, y, Math.Clamp(family, 0, _families.Length - 1), f.MaxHp, f.Range, f.DamagePerShot));
            return _turrets.Count - 1;
        }

        /// <summary>The next tier for a turret, or null at max.</summary>
        public TurretTier? NextTier(int index)
        {
            var f = FamilyOfTurret(index);
            int tier = _turrets[index].Tier;
            return tier < f.Tiers.Length ? f.Tiers[tier] : null;
        }

        /// <summary>Applies the next tier (caller pays). Returns false at max tier. Heals by the tier's hp bonus.</summary>
        public bool Upgrade(GridMap map, int index)
        {
            var next = NextTier(index);
            if (next == null) return false;
            var t = _turrets[index];
            t.Tier++;
            t.DamagePerShot *= next.DamageMultiplier;
            t.Range += next.RangeBonus;
            t.MaxHp = (ushort)(t.MaxHp + next.HpBonus);
            t.Hp = (ushort)Math.Min(t.MaxHp, t.Hp + next.HpBonus);
            t.Invested += next.Cost;
            map.SetWall(t.X, t.Y, WallKind.Structure, t.Hp);
            return true;
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
            float fraction = t.MaxHp == 0 ? 0f : (float)t.Hp / t.MaxHp;
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

        /// <summary>
        /// Global scaling applied at fire time rather than at placement, so a Doctrine card or a
        /// skill point improves guns the player ALREADY owns. Baking it in at placement would mean
        /// an upgrade only helped the next turret, which reads as a bug to anyone playing.
        /// </summary>
        public float DamageMultiplier { get; set; } = 1f;
        public float RangeMultiplier { get; set; } = 1f;

        /// <summary>
        /// How much of a turret's shot a wall absorbs when the round is stopped by it. Below one
        /// because a rifle round chews masonry slowly; the point is that walling in your own guns
        /// costs you the wall, not that it costs you instantly.
        /// </summary>
        public const float TurretWallDamageScale = 0.5f;

        /// <summary>Fires every ready turret. Shots are appended to <paramref name="shots"/> (may be null).</summary>
        public int Step(AgentWorld world, float dt, List<TurretShot>? shots)
        {
            int kills = 0;
            for (int i = 0; i < _turrets.Count; i++)
            {
                var t = _turrets[i];
                var family = FamilyOf(t.Family);
                float interval = 1f / Math.Max(0.01f, family.ShotsPerSecond);
                float range = t.Range * Math.Max(0.1f, RangeMultiplier);
                float damage = t.DamagePerShot * Math.Max(0f, DamageMultiplier);

                t.FireCooldown -= dt;
                // Catch up at most a few shots after a hitch; never an unbounded burst.
                int burst = 0;
                while (t.FireCooldown <= 0f && burst++ < 4)
                {
                    if (family.Mode == FireMode.Area)
                    {
                        // Cover blocks a blast the same way it blocks a bullet: a rotor behind
                        // your own barricade grinds the barricade, not the street beyond it.
                        if (world.CountWithinVisible(t.Center, range) == 0) { t.FireCooldown = 0f; break; }
                        t.FireCooldown += interval;
                        t.ShotsFired++;
                        int k = world.ApplyRadialDamageVisible(t.Center, range, damage);
                        t.Kills += k;
                        kills += k;
                        shots?.Add(new TurretShot(i, t.Center, t.Center, k > 0, area: true));
                        continue;
                    }

                    // Owner's rule: a turret cannot detect through a wall. Acquisition is now
                    // sight-limited, which is what makes the maze a decision instead of scenery.
                    int target = world.FindFirstInRangeVisible(t.Center, range);
                    if (target < 0) { t.FireCooldown = 0f; break; }
                    t.FireCooldown += interval;
                    t.ShotsFired++;
                    Vec2 to = world.PositionOf(target);

                    // Belt and braces: a wall raised between acquisition and the shot eats the
                    // round and takes the damage, so fire never passes through intact cover.
                    if (world.FirstWallBetween(t.Center, to, out int wx, out int wy))
                    {
                        world.DamageWall(wx, wy, damage * TurretWallDamageScale, 1f);
                        shots?.Add(new TurretShot(i, t.Center, GridMap.CellCenter(wx, wy), false));
                        continue;
                    }

                    bool killed = world.ApplyDamage(target, damage);
                    if (killed) { t.Kills++; kills++; }
                    shots?.Add(new TurretShot(i, t.Center, to, killed));
                }
                if (t.FireCooldown < 0f) t.FireCooldown = 0f;
            }
            return kills;
        }

        /// <summary>Spitters see turrets through this. Order is placement order, stable within a tick.</summary>
        public IStructureQuery AsStructureQuery() => new StructureView(this);

        private sealed class StructureView : IStructureQuery
        {
            private readonly TurretSystem _owner;
            public StructureView(TurretSystem owner) { _owner = owner; }
            public int Count => _owner._turrets.Count;
            public Vec2 PositionAt(int index) => _owner._turrets[index].Center;
        }

        /// <summary>FNV-1a over turret cells, family, hp, tier and cooldowns; mix into the match hash.</summary>
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
                    h = (h ^ (uint)t.Family) * prime;
                    h = (h ^ t.Hp) * prime;
                    h = (h ^ (uint)t.Tier) * prime;
                    h = (h ^ (uint)BitConverter.SingleToInt32Bits(t.FireCooldown)) * prime;
                }
            }
            return h;
        }
    }
}
