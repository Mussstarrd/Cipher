#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Hero;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

namespace Cipher.Game.Match
{
    /// <summary>Seeded xorshift so every "random" decision in the game layer is replayable.</summary>
    public struct XorShift64
    {
        private ulong _s;
        public XorShift64(ulong seed) { _s = seed == 0 ? 0x9E3779B97F4A7C15UL : seed; }
        public ulong Next() { _s ^= _s << 13; _s ^= _s >> 7; _s ^= _s << 17; return _s; }
        public int NextInt(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)(Next() % (ulong)maxExclusive);
        /// <summary>Uniform in [0, 1).</summary>
        public float NextFloat() => (Next() >> 40) * (1f / 16777216f);
    }

    /// <summary>What the director needs to know about the live match each time it decides.</summary>
    public readonly struct DirectorView
    {
        public readonly float MatchSeconds;
        public readonly int SappersAlive;
        public readonly int ActiveBreaches;
        public readonly int SpittersAlive;
        public readonly bool AnySpawnSealed;
        public readonly int TurretCount;

        public DirectorView(float matchSeconds, int sappersAlive, int activeBreaches, int spittersAlive, bool anySpawnSealed, int turretCount)
        {
            MatchSeconds = matchSeconds; SappersAlive = sappersAlive; ActiveBreaches = activeBreaches;
            SpittersAlive = spittersAlive; AnySpawnSealed = anySpawnSealed; TurretCount = turretCount;
        }
    }

    /// <summary>Spawn-rate numbers (ROADMAP-2026-09 #6). The owner's 0.05% is the late-game floor, not v0.</summary>
    public sealed class DirectorConfig
    {
        public float SapperFirstAt { get; set; } = 45f;
        public float SapperChance { get; set; } = 0.005f;     // 1 per 200 spawns
        public float SapperSpacing { get; set; } = 60f;
        public int MaxSappersAlive { get; set; } = 1;
        public int MaxActiveBreaches { get; set; } = 1;
        /// <summary>When a spawn has no path (full seal), a Sapper is forced this often regardless of chance.</summary>
        public float SealedSapperSpacing { get; set; } = 20f;

        public float SpitterFirstAt { get; set; } = 40f;
        public float SpitterChance { get; set; } = 0.005f;
        public float SpitterPity { get; set; } = 45f;         // at least one per this many seconds
        public int MaxSpittersAlive { get; set; } = 3;
    }

    /// <summary>
    /// Decides which archetype each spawn is. Pure and seeded: the same match plays out the
    /// same way. Spitters only appear once there is a turret to hunt.
    /// </summary>
    public sealed class SpawnDirector
    {
        private readonly DirectorConfig _cfg;
        private XorShift64 _rng;
        private float _lastSapperAt = float.NegativeInfinity;
        private float _lastSpitterAt = float.NegativeInfinity;
        private bool _firstSapperDone, _firstSpitterDone;

        public int SappersSpawned { get; private set; }
        public int SpittersSpawned { get; private set; }

        public SpawnDirector(DirectorConfig config, ulong seed)
        {
            _cfg = config;
            _rng = new XorShift64(seed);
        }

        public Archetype Decide(in DirectorView v)
        {
            // Sapper: forced on a seal, scripted first appearance, then rare with spacing and caps.
            bool sapperRoom = v.SappersAlive < _cfg.MaxSappersAlive && v.ActiveBreaches < _cfg.MaxActiveBreaches;
            if (sapperRoom)
            {
                if (v.AnySpawnSealed && v.MatchSeconds - _lastSapperAt >= _cfg.SealedSapperSpacing)
                    return Take(Archetype.Sapper, v.MatchSeconds);

                if (!_firstSapperDone && v.MatchSeconds >= _cfg.SapperFirstAt)
                    return Take(Archetype.Sapper, v.MatchSeconds);

                if (_firstSapperDone && v.MatchSeconds - _lastSapperAt >= _cfg.SapperSpacing && _rng.NextFloat() < _cfg.SapperChance)
                    return Take(Archetype.Sapper, v.MatchSeconds);
            }

            // Spitter: needs something to hunt.
            if (v.TurretCount > 0 && v.SpittersAlive < _cfg.MaxSpittersAlive && v.MatchSeconds >= _cfg.SpitterFirstAt)
            {
                bool pity = v.MatchSeconds - _lastSpitterAt >= _cfg.SpitterPity;
                if (!_firstSpitterDone || pity || _rng.NextFloat() < _cfg.SpitterChance)
                    return Take(Archetype.Spitter, v.MatchSeconds);
            }

            return Archetype.Runner;
        }

        private Archetype Take(Archetype a, float now)
        {
            if (a == Archetype.Sapper) { _lastSapperAt = now; _firstSapperDone = true; SappersSpawned++; }
            else { _lastSpitterAt = now; _firstSpitterDone = true; SpittersSpawned++; }
            return a;
        }
    }

    /// <summary>
    /// Consumable repair drone: bought in build mode and dropped on a breached cell. It repairs
    /// one stage every RepairSeconds, but only while the hero stands within EscortRange
    /// (ROADMAP question 1 default: leaving the front line is the cost).
    /// </summary>
    public sealed class RepairDrone
    {
        public const float RepairSeconds = 4f;
        public const float EscortRange = 8f;

        public int X { get; }
        public int Y { get; }
        public float Progress { get; private set; }
        public bool Done { get; private set; }
        public bool HeroInRange { get; private set; }

        public RepairDrone(int x, int y) { X = x; Y = y; }

        /// <summary>Advances the repair. Returns the number of stages repaired this call (0 or 1).</summary>
        public int Tick(AgentWorld world, Vec2 heroPosition, float dt)
        {
            if (Done) return 0;
            GridMap map = world.Map;
            if (map.KindAt(X, Y) == WallKind.None || map.StageAt(X, Y) == BreachStage.Intact)
            {
                Done = true; // nothing left to fix (repaired, sold, or never breached)
                return 0;
            }

            HeroInRange = Vec2.DistanceSquared(heroPosition, GridMap.CellCenter(X, Y)) <= EscortRange * EscortRange;
            if (!HeroInRange) return 0;

            Progress += dt;
            if (Progress < RepairSeconds) return 0;
            Progress -= RepairSeconds;
            BreachStage stage = world.RepairStage(X, Y);
            if (stage == BreachStage.Intact) Done = true;
            return 1;
        }
    }

    /// <summary>The hero gun ladder: data, applied to a HeroConfig in place.</summary>
    public static class GunTiers
    {
        public static readonly string[] Names = { "LMG", "LMG Mk2", "LMG Mk3", "Gold-plated LMG" };
        public const int MaxTier = 3;

        public static void Apply(HeroConfig cfg, int tier)
        {
            tier = Math.Clamp(tier, 0, MaxTier);
            cfg.GunDamage = tier switch { 0 => 6f, 1 => 9f, 2 => 9f, _ => 12f };
            cfg.FireInterval = tier >= 2 ? 1f / 16f : 1f / 12f;
            cfg.GunHitRadius = tier >= 3 ? 0.6f : 0.45f;
        }
    }

    /// <summary>
    /// Gun crates: every CrateInterval a crate appears at a seeded open cell on the hero's
    /// side of the arena and lives for CrateLifetime. Walking over it raises the gun tier.
    /// </summary>
    public sealed class PickupSystem
    {
        public const float CrateInterval = 45f;
        public const float CrateLifetime = 30f;
        public const float PickupRadius = 1.1f;

        private readonly GridMap _map;
        private readonly int _minX, _maxX;
        private XorShift64 _rng;
        private float _nextCrateAt = CrateInterval;

        public Vec2 CratePosition { get; private set; }
        public float CrateTimeLeft { get; private set; }
        public bool CrateActive => CrateTimeLeft > 0f;
        public int GunTier { get; private set; }
        public int Collected { get; private set; }
        public string GunName => GunTiers.Names[GunTier];

        public PickupSystem(GridMap map, ulong seed, int minX, int maxX)
        {
            _map = map;
            _rng = new XorShift64(seed);
            _minX = minX;
            _maxX = maxX;
        }

        /// <summary>Returns true when the hero collected a crate this call (config already upgraded).</summary>
        public bool Tick(float matchSeconds, float dt, Vec2 heroPosition, HeroConfig heroCfg)
        {
            if (CrateActive)
            {
                CrateTimeLeft -= dt;
                if (Vec2.DistanceSquared(heroPosition, CratePosition) <= PickupRadius * PickupRadius)
                {
                    CrateTimeLeft = 0f;
                    Collected++;
                    if (GunTier < GunTiers.MaxTier) { GunTier++; GunTiers.Apply(heroCfg, GunTier); }
                    return true;
                }
                return false;
            }

            if (matchSeconds >= _nextCrateAt && GunTier < GunTiers.MaxTier)
            {
                _nextCrateAt += CrateInterval;
                for (int attempt = 0; attempt < 32; attempt++)
                {
                    int x = _minX + _rng.NextInt(Math.Max(1, _maxX - _minX));
                    int y = 2 + _rng.NextInt(Math.Max(1, _map.Height - 4));
                    if (!_map.IsBuildable(x, y)) continue;
                    CratePosition = GridMap.CellCenter(x, y);
                    CrateTimeLeft = CrateLifetime;
                    break;
                }
            }
            return false;
        }
    }
}
