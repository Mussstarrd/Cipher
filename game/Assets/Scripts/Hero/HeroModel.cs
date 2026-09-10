#nullable enable
using System;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

namespace Cipher.Game.Hero
{
    /// <summary>Tunables for the graybox Enforcer. Data, not code: balance lives here.</summary>
    public sealed class HeroConfig
    {
        public float MoveSpeed { get; set; } = 7f;            // cells / s (runners do 3)
        public float MaxHealth { get; set; } = 100f;
        public float FireInterval { get; set; } = 1f / 12f;   // LMG: 12 rounds / s
        public float GunDamage { get; set; } = 6f;            // runners have 10 hp: two taps
        public float GunRange { get; set; } = 25f;
        public float GunHitRadius { get; set; } = 0.45f;
        public float ContactRadius { get; set; } = 0.9f;      // runners inside this chew on you
        public float ContactDamagePerAgentPerSecond { get; set; } = 6f;
        public int ContactAgentCap { get; set; } = 8;         // being buried is lethal, not instant
        public float AirstrikeCooldown { get; set; } = 6f;
        public float AirstrikeRadius { get; set; } = 3.5f;
        public float AirstrikeDamage { get; set; } = 50f;
        public float AirstrikeLead { get; set; } = 8f;        // marker lands this far along facing
    }

    public readonly struct ShotResult
    {
        public readonly Vec2 Origin;
        public readonly Vec2 End;
        public readonly int HitId;
        public readonly bool Killed;
        public bool Hit => HitId >= 0;

        public ShotResult(Vec2 origin, Vec2 end, int hitId, bool killed)
        {
            Origin = origin; End = end; HitId = hitId; Killed = killed;
        }
    }

    /// <summary>
    /// The player's Enforcer, as pure state. No UnityEngine: every rule here is unit-tested,
    /// and every effect on the swarm goes through AgentWorld's public API so the sim stays
    /// the single source of truth. Rendering, input and camera live in the game layer.
    /// </summary>
    public sealed class HeroModel
    {
        private readonly HeroConfig _cfg;

        public Vec2 Position { get; private set; }
        public Vec2 Facing { get; private set; } = new Vec2(1f, 0f);
        public float Health { get; private set; }
        public bool IsDown => Health <= 0f;
        public float FireCooldown { get; private set; }
        public float AirstrikeCooldown { get; private set; }
        public int Kills { get; private set; }
        public int ShotsFired { get; private set; }

        public float HealthFraction => Math.Max(0f, Health / _cfg.MaxHealth);
        public float AirstrikeReadyFraction => 1f - Math.Clamp(AirstrikeCooldown / _cfg.AirstrikeCooldown, 0f, 1f);
        public bool AirstrikeReady => AirstrikeCooldown <= 0f && !IsDown;
        public Vec2 AirstrikeMarker => Position + Facing * _cfg.AirstrikeLead;
        public float AirstrikeRadius => _cfg.AirstrikeRadius;

        public HeroModel(HeroConfig config, Vec2 spawn)
        {
            _cfg = config ?? throw new ArgumentNullException(nameof(config));
            Position = spawn;
            Health = config.MaxHealth;
        }

        /// <summary>Cooldowns tick down here; call once per frame or sim tick with that dt.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            FireCooldown = Math.Max(0f, FireCooldown - dt);
            AirstrikeCooldown = Math.Max(0f, AirstrikeCooldown - dt);
        }

        /// <summary>
        /// Moves along a world-space input vector (magnitude at most 1, clamped) using the
        /// same wall rule as the swarm. Does not change facing: aiming owns that.
        /// </summary>
        public void Move(GridMap map, Vec2 input, float dt)
        {
            if (IsDown || dt <= 0f) return;
            float mag = input.Length;
            if (mag < 1e-4f) return;
            if (mag > 1f) input = input * (1f / mag);
            // Never more than 0.9 cell per call, same tunnel guard as agents.
            float step = Math.Min(_cfg.MoveSpeed * dt, 0.9f);
            Position = Movement.ResolveWalls(map, Position, Position + input * step);
        }

        /// <summary>Sets facing from any non-zero direction; zero input keeps the last facing.</summary>
        public void Aim(Vec2 direction)
        {
            if (direction.LengthSquared < 1e-6f) return;
            Facing = direction.Normalized();
        }

        /// <summary>
        /// Fires one hitscan round along Facing rotated by <paramref name="spreadDegrees"/>
        /// (the caller supplies the random spread so this stays deterministic under test).
        /// Returns false if on cooldown or down.
        /// </summary>
        public bool TryFire(AgentWorld world, float spreadDegrees, out ShotResult shot)
        {
            shot = default;
            if (IsDown || FireCooldown > 0f) return false;

            FireCooldown = _cfg.FireInterval;
            ShotsFired++;

            Vec2 dir = Rotate(Facing, spreadDegrees);
            Vec2 origin = Position;
            bool hit = world.Raycast(origin, dir, _cfg.GunRange, _cfg.GunHitRadius, out int hitId, out float dist);
            bool killed = false;
            Vec2 end;
            if (hit)
            {
                killed = world.ApplyDamage(hitId, _cfg.GunDamage);
                if (killed) Kills++;
                end = origin + dir * dist;
            }
            else
            {
                end = origin + dir * _cfg.GunRange;
            }
            shot = new ShotResult(origin, end, hit ? hitId : -1, killed);
            return true;
        }

        /// <summary>Airstrike ultimate stub: radial damage at the marker ahead of the hero.</summary>
        public bool TryAirstrike(AgentWorld world, out Vec2 center, out int kills)
        {
            center = AirstrikeMarker;
            kills = 0;
            if (!AirstrikeReady) return false;
            AirstrikeCooldown = _cfg.AirstrikeCooldown;
            kills = world.ApplyRadialDamage(center, _cfg.AirstrikeRadius, _cfg.AirstrikeDamage);
            Kills += kills;
            return true;
        }

        /// <summary>Runners in contact chew on the hero. Returns damage taken this call.</summary>
        public float ApplyContact(AgentWorld world, float dt)
        {
            if (IsDown || dt <= 0f) return 0f;
            int n = Math.Min(world.CountWithin(Position, _cfg.ContactRadius), _cfg.ContactAgentCap);
            if (n == 0) return 0f;
            float damage = n * _cfg.ContactDamagePerAgentPerSecond * dt;
            Health = Math.Max(0f, Health - damage);
            return damage;
        }

        public void Respawn(Vec2 at)
        {
            Position = at;
            Health = _cfg.MaxHealth;
            FireCooldown = 0f;
            AirstrikeCooldown = 0f;
        }

        private static Vec2 Rotate(Vec2 v, float degrees)
        {
            if (degrees == 0f) return v;
            float r = degrees * (MathF.PI / 180f);
            float c = MathF.Cos(r), s = MathF.Sin(r);
            return new Vec2(v.X * c - v.Y * s, v.X * s + v.Y * c);
        }
    }
}
