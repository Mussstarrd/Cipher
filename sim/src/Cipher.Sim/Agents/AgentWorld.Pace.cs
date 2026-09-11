#nullable enable
using System;
using Cipher.Sim.Core;

namespace Cipher.Sim.Agents
{
    /// <summary>
    /// How fast an individual moves, and whether they are armed.
    ///
    /// Owner, 2026-09-11: "Some of the mobs need to Sprint some need to run some need to walk a few
    /// of them should have pistols that they take pop shots at me."
    ///
    /// Every body moved at exactly one speed, which is the single loudest tell that a crowd is a
    /// particle system. Real people arriving at the same place do not arrive together: the fast ones
    /// are on you while the slow ones are still crossing the field, and that spread is what turns a
    /// wave into something with a shape.
    ///
    /// Pace is a MULTIPLIER set once at spawn and never changed, so it costs one float per agent and
    /// nothing per tick. It is chosen OUTSIDE the sim by the seeded director, exactly like intent,
    /// because the core still carries no RNG.
    ///
    /// Pistols are a per-agent flag rather than an archetype. An archetype implies its own model,
    /// its own spawn budget and its own rules; a pistol is just a thing an ordinary person might be
    /// carrying, which is also the more unsettling reading: not a special enemy, a neighbour who
    /// owned a handgun.
    /// </summary>
    public sealed partial class AgentWorld
    {
        /// <summary>Speed multiplier for one body. 1 is the configured walk.</summary>
        public float PaceOf(int id) => _pace[id];

        /// <summary>True for a body carrying a sidearm.</summary>
        public bool IsArmed(int id) => _armed[id];

        /// <summary>
        /// Spawns an ordinary body with a pace and, optionally, a sidearm. The director picks both.
        /// </summary>
        public int Spawn(Vec2 position, float health, Intent intent, float pace, bool armed)
        {
            int id = Spawn(position, health, intent);
            _pace[id] = pace <= 0f ? 1f : pace;
            _armed[id] = armed;
            return id;
        }

        /// <summary>
        /// A body with a sidearm stops and takes a shot when it can see the player.
        ///
        /// Deliberately weak and slow: the danger is not the damage, it is that stopping to shoot
        /// makes a pocket of the crowd behave differently from the rest of it, so a player learns to
        /// read the wave rather than treat it as one object. Returns true when it has taken over
        /// movement for this tick.
        /// </summary>
        private bool StepPistol(int i, Vec2 pos, float dt, bool gates)
        {
            if (!_armed[i] || !HeroIsPrey) return false;

            float distSq = Vec2.DistanceSquared(pos, HeroPosition);
            float range = _config.PistolRange;
            if (distSq > range * range) return false;

            // Same rule as the turrets: no shooting through a wall.
            if (!HasLineOfSight(pos, HeroPosition)) return false;

            // Close the gap to a comfortable firing distance, then stand and shoot.
            float dist = MathF.Sqrt(distSq);
            if (dist > _config.PistolStandoff)
            {
                SteerToward(i, pos, HeroPosition, dt, _config.MoveSpeed * _pace[i], gates);
                return true;
            }

            _timer[i] -= dt;
            if (_timer[i] <= 0f)
            {
                _timer[i] = _config.PistolInterval;
                // The sim does not own hero health, so it reports the shot and the game resolves it.
                _events.Add(new SimEvent(SimEventKind.PistolShot, i, 0, _config.PistolDamage));
            }
            return true;
        }
    }
}
