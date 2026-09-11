#nullable enable
using System;
using System.Collections.Generic;
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
        public float FireInterval { get; set; } = 1f / 12f;   // twelve packets a second
        public float GunDamage { get; set; } = 6f;            // runners have 10 hp: two taps
        public float GunRange { get; set; } = 25f;
        public float GunHitRadius { get; set; } = 0.45f;
        // Wider than SimConfig.HeroContactRange, deliberately and with margin: the swarm closes to
        // that distance and stops, so if this is the smaller of the two they stand just outside it
        // and are completely harmless. That shipped.
        public float ContactRadius { get; set; } = 1.35f;     // runners inside this chew on you
        public float ContactDamagePerAgentPerSecond { get; set; } = 6f;
        public int ContactAgentCap { get; set; } = 10;        // being buried is lethal, not instant

        /// <summary>
        /// Flat damage removed from each body's contact tick, from Plated affixes and Scrap
        /// Plate. Applied per agent rather than to the total, so armour is worth most when a
        /// few are on you and least when you are buried, which is the right shape: it makes
        /// armour a reason to hold a line, not a reason to stand in the middle of a wave.
        /// </summary>
        public float ContactArmour { get; set; }

        /// <summary>Once per wave, a lethal hit leaves you on 1 HP instead. Legacy vests only.</summary>
        public bool HasSecondWind { get; set; }
        // Airstrike ultimate: a line of bombs along the look axis, aimed by looking (ROADMAP-2026-09 #1).
        /// <summary>
        /// Owner, 2026-09-11: "Airstrikes need to have a longer cool down they are overpowered."
        /// Eight seconds made it a primary weapon. At forty-five it is the thing you save for the
        /// moment the line actually breaks, which is what ADR-003's hijacked cargo drone should be:
        /// leverage you get occasionally, not a button you hold.
        /// </summary>
        public float AirstrikeCooldown { get; set; } = 45f;
        public float AirstrikeMinRange { get; set; } = 6f;    // marker clamps to this band from the hero
        public float AirstrikeMaxRange { get; set; } = 24f;
        public float AirstrikeLineLength { get; set; } = 14f; // bombs walk the line far to near
        public int AirstrikeBombCount { get; set; } = 6;
        public float AirstrikeBombRadius { get; set; } = 2f;  // 4-cell-wide line
        public float AirstrikeBombDamage { get; set; } = 50f;
        public float AirstrikeInboundDelay { get; set; } = 1.2f;
        public float AirstrikeBombInterval { get; set; } = 0.12f;
        public float AirstrikeSelfDamage { get; set; } = 25f; // standing in your own strike hurts

        // Terrain damage (docs/design/arsenal-and-terrain.md section 2). Your own walls take a
        // quarter, so holding a line at your barricade erodes it without a stray round griefing you.
        public float GunWallDamage { get; set; } = 3f;
        public float AirstrikeWallDamage { get; set; } = 120f;
        public float FriendlyWallDamageScale { get; set; } = 0.25f;
    }

    public readonly struct ShotResult
    {
        public readonly Vec2 Origin;
        public readonly Vec2 End;
        public readonly int HitId;
        public readonly bool Killed;
        /// <summary>The wall cell the round stopped in, or (-1,-1). Walls block line of fire.</summary>
        public readonly int WallX;
        public readonly int WallY;
        public bool Hit => HitId >= 0;
        public bool HitWall => WallX >= 0;

        public ShotResult(Vec2 origin, Vec2 end, int hitId, bool killed, int wallX = -1, int wallY = -1)
        {
            Origin = origin; End = end; HitId = hitId; Killed = killed; WallX = wallX; WallY = wallY;
        }
    }

    /// <summary>One bomb of a strike that has landed this tick; the game layer draws it.</summary>
    public readonly struct StrikeImpact
    {
        public readonly Vec2 Center;
        public readonly float Radius;
        public readonly int Kills;
        public StrikeImpact(Vec2 center, float radius, int kills) { Center = center; Radius = radius; Kills = kills; }
    }

    /// <summary>
    /// The player's Enforcer, as pure state. No UnityEngine: every rule here is unit-tested,
    /// and every effect on the swarm goes through AgentWorld's public API so the sim stays
    /// the single source of truth. Rendering, input and camera live in the game layer.
    /// </summary>
    public sealed class HeroModel
    {
        private HeroConfig _cfg;

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

        /// <summary>Where the strike line is centred: the last aim point, clamped to the range band.</summary>
        public Vec2 StrikeTarget { get; private set; }
        /// <summary>Unit axis of the strike line (hero toward target).</summary>
        public Vec2 StrikeAxis { get; private set; } = new Vec2(1f, 0f);
        public float StrikeLineLength => _cfg.AirstrikeLineLength;
        public float StrikeLineWidth => _cfg.AirstrikeBombRadius * 2f;
        /// <summary>True from the call until the last bomb lands.</summary>
        public bool StrikeInbound => _strikeBombsLeft > 0;

        /// <summary>
        /// How far through the run the delivery is, 0 to 1: the approach, then the bombs walking the
        /// line. The game flies the drone on this, so what is overhead and what is landing agree.
        /// </summary>
        public float StrikeProgress01
        {
            get
            {
                int total = Math.Max(1, _cfg.AirstrikeBombCount);
                if (_strikeBombsLeft <= 0) return 1f;
                // The approach is the first third of the pass; the drop is the rest.
                float approach = _cfg.AirstrikeInboundDelay <= 0f ? 1f
                               : 1f - Math.Max(0f, _strikeTimer) / _cfg.AirstrikeInboundDelay;
                if (total - _strikeBombsLeft == 0 && _strikeTimer > 0f)
                    return Math.Clamp(approach, 0f, 1f) * 0.34f;
                float dropped = (total - _strikeBombsLeft) / (float)total;
                return 0.34f + Math.Clamp(dropped, 0f, 1f) * 0.66f;
            }
        }
        public float StrikeTimeToImpact => Math.Max(0f, _strikeTimer);

        private int _strikeBombsLeft;
        private float _strikeTimer;   // counts down to the next bomb
        private Vec2 _strikeFar;      // first bomb lands here
        private Vec2 _strikeStep;     // per-bomb displacement toward the hero

        public HeroModel(HeroConfig config, Vec2 spawn)
        {
            _cfg = config ?? throw new ArgumentNullException(nameof(config));
            Position = spawn;
            Health = config.MaxHealth;
            StrikeTarget = spawn + Facing * config.AirstrikeMinRange;
        }

        /// <summary>
        /// Swaps in a new config after gear or an upgrade changed the numbers. Health is carried
        /// across as a FRACTION, so a card that raises max health heals proportionally rather than
        /// either wasting the bonus or topping the player up for free mid-wave.
        /// </summary>
        public void Retune(HeroConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            float fraction = _cfg.MaxHealth > 0f ? Health / _cfg.MaxHealth : 1f;
            _cfg = config;
            Health = Math.Clamp(fraction, 0f, 1f) * _cfg.MaxHealth;
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

            // Whichever comes first along the ray wins: a body, or the wall behind it.
            bool hitAgent = world.Raycast(origin, dir, _cfg.GunRange, _cfg.GunHitRadius, out int hitId, out float agentDist);
            bool hitWall = Movement.FirstBlockedCell(world.Map, origin, dir, _cfg.GunRange, out int wx, out int wy, out float wallDist);

            if (hitAgent && (!hitWall || agentDist <= wallDist))
            {
                bool killed = world.ApplyDamage(hitId, _cfg.GunDamage);
                if (killed) Kills++;
                shot = new ShotResult(origin, origin + dir * agentDist, hitId, killed);
                return true;
            }

            if (hitWall)
            {
                world.DamageWall(wx, wy, _cfg.GunWallDamage, _cfg.FriendlyWallDamageScale);
                shot = new ShotResult(origin, origin + dir * wallDist, -1, false, wx, wy);
                return true;
            }

            shot = new ShotResult(origin, origin + dir * _cfg.GunRange, -1, false);
            return true;
        }

        /// <summary>
        /// Aim the strike at a world point (where the camera looks at the ground). The target is
        /// clamped to the [min, max] range band from the hero; the line axis follows hero toward target.
        /// </summary>
        public void AimStrike(Vec2 aimPoint)
        {
            Vec2 rel = aimPoint - Position;
            float d = rel.Length;
            Vec2 axis = d > 1e-4f ? rel * (1f / d) : Facing;
            float clamped = Math.Clamp(d, _cfg.AirstrikeMinRange, _cfg.AirstrikeMaxRange);
            StrikeAxis = axis;
            StrikeTarget = Position + axis * clamped;
        }

        /// <summary>
        /// Calls the strike on the current StrikeTarget: after the inbound delay, bombs walk the
        /// line from the far end toward the hero. Returns false if not ready. Damage is applied
        /// by <see cref="TickStrike"/> as each bomb lands.
        /// </summary>
        public bool TryAirstrike()
        {
            if (!AirstrikeReady || StrikeInbound) return false;
            AirstrikeCooldown = _cfg.AirstrikeCooldown;

            int n = Math.Max(1, _cfg.AirstrikeBombCount);
            float half = _cfg.AirstrikeLineLength * 0.5f;
            _strikeFar = StrikeTarget + StrikeAxis * half;
            _strikeStep = n > 1 ? StrikeAxis * (-_cfg.AirstrikeLineLength / (n - 1)) : Vec2.Zero;
            _strikeBombsLeft = n;
            _strikeTimer = _cfg.AirstrikeInboundDelay;
            return true;
        }

        /// <summary>
        /// Advances an inbound strike. Bombs that land this call are appended to
        /// <paramref name="impacts"/> (may be null). Returns kills this call.
        /// </summary>
        public int TickStrike(AgentWorld world, float dt, List<StrikeImpact>? impacts)
        {
            if (_strikeBombsLeft <= 0 || dt <= 0f) return 0;
            _strikeTimer -= dt;
            int kills = 0;
            int total = Math.Max(1, _cfg.AirstrikeBombCount);
            while (_strikeBombsLeft > 0 && _strikeTimer <= 0f)
            {
                int index = total - _strikeBombsLeft;
                Vec2 center = _strikeFar + _strikeStep * index;
                int k = world.ApplyRadialDamage(center, _cfg.AirstrikeBombRadius, _cfg.AirstrikeBombDamage);
                kills += k;
                // The street erupts: bombs open walls too, including one you were relying on.
                world.DamageWallsInRadius(center, _cfg.AirstrikeBombRadius, _cfg.AirstrikeWallDamage, _cfg.FriendlyWallDamageScale);
                if (!IsDown && Vec2.DistanceSquared(center, Position) <= _cfg.AirstrikeBombRadius * _cfg.AirstrikeBombRadius)
                    Health = Math.Max(0f, Health - _cfg.AirstrikeSelfDamage);
                impacts?.Add(new StrikeImpact(center, _cfg.AirstrikeBombRadius, k));
                _strikeBombsLeft--;
                _strikeTimer += _cfg.AirstrikeBombInterval;
            }
            Kills += kills;
            return kills;
        }

        /// <summary>Runners in contact chew on the hero. Returns damage taken this call.</summary>
        /// <summary>True while a Legacy vest can still save the player this wave.</summary>
        public bool SecondWindReady => _cfg.HasSecondWind && !_secondWindUsed;

        /// <summary>Fires when Second Wind catches a lethal hit, so the game layer can react.</summary>
        public bool ConsumedSecondWindThisTick { get; private set; }

        private bool _secondWindUsed;

        /// <summary>Rearms Second Wind. Called at the start of each wave, not each mission.</summary>
        public void RearmSecondWind() => _secondWindUsed = false;

        public float ApplyContact(AgentWorld world, float dt)
        {
            ConsumedSecondWindThisTick = false;
            if (IsDown || dt <= 0f) return 0f;
            // Threat, not headcount: a body whose chip is failing is still leaning on you, just
            // with less and less in it. ADR-008.
            float n = Math.Min(world.ThreatWithin(Position, _cfg.ContactRadius), _cfg.ContactAgentCap);
            if (n <= 0f) return 0f;

            // Armour bites per body, and never reduces a tick to nothing: being swarmed still kills.
            float perAgent = Math.Max(_cfg.ContactDamagePerAgentPerSecond * 0.1f,
                                      _cfg.ContactDamagePerAgentPerSecond - _cfg.ContactArmour);
            float damage = n * perAgent * dt;
            return ApplyDamageToSelf(damage);
        }

        /// <summary>
        /// The single place the hero loses health, so Second Wind cannot be bypassed by a new
        /// damage source someone adds later.
        /// </summary>
        /// <summary>
        /// Damage from something other than contact: a sidearm, a Spitter, the player's own strike.
        /// Goes through the same path as everything else, so Second Wind covers it too.
        /// </summary>
        public float TakeDamage(float damage) => ApplyDamageToSelf(damage);

        /// <summary>Restores health, capped at max. Returns what was actually restored.</summary>
        public float Heal(float amount)
        {
            if (amount <= 0f || IsDown) return 0f;
            float before = Health;
            Health = Math.Min(_cfg.MaxHealth, Health + amount);
            return Health - before;
        }

        private float ApplyDamageToSelf(float damage)
        {
            if (damage <= 0f || IsDown) return 0f;

            if (damage >= Health && SecondWindReady)
            {
                float dealt = Health - 1f;
                Health = 1f;
                _secondWindUsed = true;
                ConsumedSecondWindThisTick = true;
                return Math.Max(0f, dealt);
            }

            Health = Math.Max(0f, Health - damage);
            return damage;
        }

        public void Respawn(Vec2 at)
        {
            Position = at;
            Health = _cfg.MaxHealth;
            _secondWindUsed = false;
            FireCooldown = 0f;
            AirstrikeCooldown = 0f;
            _strikeBombsLeft = 0;
            StrikeTarget = at + Facing * _cfg.AirstrikeMinRange;
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
