#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

namespace Cipher.Game.Match
{
    /// <summary>
    /// Something to walk to. Owner: "maybe there's some kind of healing that I can pick up like potion".
    ///
    /// Aid kits appear near the abandoned vehicles and the guardhouse -- places a first-aid box would
    /// actually be in a lake community -- a few at the start of each position and one more with each
    /// wave cleared. They do not expire, so a player can leave one for later, which is a decision.
    ///
    /// This is the roadmap's one permitted exception to the systems freeze, and it reuses
    /// <see cref="PickupSystem"/>'s pattern rather than inventing one: seeded placement on buildable
    /// cells, a pickup radius, and a heal that is a fraction of max health so it scales with gear.
    /// Pure C# so it is testable without a scene.
    /// </summary>
    public sealed class AidKits
    {
        public const float PickupRadius = 1.15f;

        /// <summary>Fraction of max health a kit restores.</summary>
        public float HealFraction { get; set; } = 0.45f;

        /// <summary>How many kits are laid down when a position starts.</summary>
        public int OpeningKits { get; set; } = 3;

        /// <summary>
        /// Slow regeneration when nothing has hurt the player for a while, in health per second.
        /// Small on purpose: it lets a player who backs off recover between waves without making
        /// the kits pointless, and it never outpaces being bitten.
        /// </summary>
        public float RegenPerSecond { get; set; } = 1.6f;

        /// <summary>Seconds without damage before regeneration starts.</summary>
        public float RegenDelay { get; set; } = 6f;

        private readonly GridMap _map;
        private readonly List<Vec2> _spots;
        private readonly List<Vec2> _kits = new List<Vec2>();
        private XorShift64 _rng;
        private float _sinceHurt;
        private float _lastHealth = -1f;

        public IReadOnlyList<Vec2> Kits => _kits;
        public int Collected { get; private set; }

        /// <summary>
        /// <paramref name="spots"/> are candidate cells (vehicles, the guardhouse door); kits are drawn
        /// from those first and fall back to any buildable cell so a mission without vehicles still
        /// has healing in it.
        /// </summary>
        public AidKits(GridMap map, IEnumerable<Vec2> spots, ulong seed)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _spots = new List<Vec2>(spots ?? Array.Empty<Vec2>());
            _rng = new XorShift64(seed);
        }

        public void PlaceOpening() { for (int i = 0; i < OpeningKits; i++) PlaceOne(); }

        /// <summary>One more kit per wave cleared. Drop it where it will be needed: near the player.</summary>
        public void OnWaveCleared() => PlaceOne();

        public void PlaceOne()
        {
            if (_spots.Count > 0)
            {
                // Prefer the authored spots, shuffled, skipping any that already hold a kit.
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    var spot = _spots[_rng.NextInt(_spots.Count)];
                    if (!Occupied(spot)) { _kits.Add(spot); return; }
                }
            }

            for (int attempt = 0; attempt < 48; attempt++)
            {
                int x = 2 + _rng.NextInt(Math.Max(1, _map.Width - 4));
                int y = 2 + _rng.NextInt(Math.Max(1, _map.Height - 4));
                if (!_map.IsBuildable(x, y)) continue;
                var p = GridMap.CellCenter(x, y);
                if (Occupied(p)) continue;
                _kits.Add(p);
                return;
            }
        }

        private bool Occupied(Vec2 at)
        {
            for (int i = 0; i < _kits.Count; i++)
                if (Vec2.DistanceSquared(_kits[i], at) < 4f) return true;
            return false;
        }

        /// <summary>
        /// Picks up any kit the hero is standing on, and applies regeneration. Returns the health
        /// restored this call, so the game can say so.
        /// </summary>
        public float Tick(float dt, Vec2 heroPosition, float health, float maxHealth, bool heroDown,
                          out bool tookKit)
        {
            tookKit = false;
            if (dt <= 0f || heroDown) { _lastHealth = health; return 0f; }

            // Being hurt resets the regen clock. Detected from the number, like everything else
            // that reads the hero: the model does not announce damage.
            if (_lastHealth >= 0f && health < _lastHealth - 0.01f) _sinceHurt = 0f;
            else _sinceHurt += dt;
            _lastHealth = health;

            float healed = 0f;
            for (int i = _kits.Count - 1; i >= 0; i--)
            {
                if (Vec2.DistanceSquared(heroPosition, _kits[i]) > PickupRadius * PickupRadius) continue;
                if (health >= maxHealth - 0.01f) continue;   // do not waste a kit at full health
                _kits.RemoveAt(i);
                Collected++;
                tookKit = true;
                healed += maxHealth * HealFraction;
                break;
            }

            if (_sinceHurt >= RegenDelay && health < maxHealth)
                healed += RegenPerSecond * dt;

            _lastHealth = Math.Min(maxHealth, health + healed);
            return healed;
        }
    }
}
