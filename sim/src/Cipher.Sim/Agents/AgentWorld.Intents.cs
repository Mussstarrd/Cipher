#nullable enable
using System;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

namespace Cipher.Sim.Agents
{
    /// <summary>
    /// What an ordinary body is actually trying to do. Owner's call, 2026-09-11:
    ///
    ///   "Mobs shouldn't project their path and they shouldn't all follow the same logic. Some
    ///    should branch off and try to kill tower or blockade infra."
    ///
    /// He is right, and the reason is that a flow field is *too* readable. A thousand agents all
    /// solving the same shortest path is one lane, and one lane is a puzzle the player solves once
    /// and then holds forever. Splitting intent at spawn means the wall you did not garrison is the
    /// one that gets hit, and it turns the Sapper and Spitter from two special cases into the sharp
    /// end of a spectrum every body is somewhere on.
    /// </summary>
    public enum Intent : byte
    {
        /// <summary>Follow the field to the objective. The default, and still most of them.</summary>
        Vault = 0,
        /// <summary>Peel off for the nearest emplacement and pull it down.</summary>
        HuntStructure = 1,
        /// <summary>Go at the nearest barricade rather than walking round it.</summary>
        WreckWall = 2,
    }

    public sealed partial class AgentWorld
    {
        /// <summary>
        /// Spawns with an explicit errand. The intent is chosen OUTSIDE the sim, by the spawn
        /// director's seeded generator, because the core carries no randomness of its own.
        /// </summary>
        public int Spawn(Vec2 position, float health, Intent intent)
        {
            int id = SpawnInternal(position, health, Archetype.Runner);
            _intent[id] = (byte)intent;
            return id;
        }

        public Intent IntentOf(int id) => (Intent)_intent[id];

        /// <summary>How many living agents are on each errand. Used by the HUD and by tests.</summary>
        public (int vault, int hunters, int wreckers) IntentCensus()
        {
            int v = 0, h = 0, w = 0;
            for (int i = 0; i < Count; i++)
            {
                if (!_alive[i]) continue;
                switch ((Intent)_intent[i])
                {
                    case Intent.HuntStructure: h++; break;
                    case Intent.WreckWall: w++; break;
                    default: v++; break;
                }
            }
            return (v, h, w);
        }

        /// <summary>
        /// Walks at the nearest emplacement and tears at it on arrival. Returns true when it has
        /// taken over movement for this tick.
        /// </summary>
        private bool StepStructureHunter(int i, Vec2 pos, float dt, bool gates)
        {
            var structures = Structures;
            if (structures == null || structures.Count == 0) return false;

            int best = -1;
            float bestDistSq = _config.HunterAcquireRange * _config.HunterAcquireRange;
            for (int s = 0; s < structures.Count; s++)
            {
                float dsq = Vec2.DistanceSquared(pos, structures.PositionAt(s));
                // Strict less-than plus ascending scan makes ties resolve to the lowest index.
                if (dsq < bestDistSq) { bestDistSq = dsq; best = s; }
            }
            if (best < 0) return false;          // nothing worth peeling off for: keep walking

            Vec2 target = structures.PositionAt(best);
            float dist = MathF.Sqrt(bestDistSq);

            if (dist > _config.HunterContactRange)
            {
                SteerToward(i, pos, target, dt, _config.HunterSpeed, gates);
                _target[i] = best;
                return true;
            }

            // In contact. Tearing at a turret is the game layer's business to resolve, because the
            // sim does not own emplacement health; it just reports that it is happening.
            _target[i] = best;
            _timer[i] -= dt;
            if (_timer[i] <= 0f)
            {
                _timer[i] = _config.HunterAttackInterval;
                _events.Add(new SimEvent(SimEventKind.StructureMauled, i, best,
                                         _config.HunterStructureDamage * ThreatScale(i)));
            }
            return true;
        }

        /// <summary>
        /// Goes at a barricade instead of round it. Unlike the Sapper this is brute force: no plan,
        /// no staged breach, just a body pulling at a wall until it gives.
        /// </summary>
        private bool StepWallWrecker(int i, Vec2 pos, float dt, bool gates)
        {
            var (cx, cy) = _map.WorldToCell(pos);

            // Already touching something breakable? Pull at it.
            if (TryFindAdjacentWall(cx, cy, out int wx, out int wy))
            {
                _timer[i] -= dt;
                if (_timer[i] <= 0f)
                {
                    _timer[i] = _config.WreckerAttackInterval;
                    DamageWall(wx, wy, _config.WreckerWallDamage * ThreatScale(i), 1f);
                }
                return true;
            }

            // Otherwise head for the nearest wall within reach, scanning in a fixed order so the
            // choice is identical on every machine.
            if (!FindNearestWallCell(cx, cy, _config.WreckerSearchCells, out int tx, out int ty))
                return false;

            SteerToward(i, pos, GridMap.CellCenter(tx, ty), dt, _config.MoveSpeed, gates);
            return true;
        }

        /// <summary>Straight-line steering with separation, sharing the runner's movement rules.</summary>
        /// <summary>
        /// Steers one agent at a speed, through the wall and gate rules.
        ///
        /// **The failing-chip slowdown is applied HERE, not by the caller.** It was a caller's
        /// responsibility for exactly as long as it took to review: one of the six movement paths
        /// multiplied by it and five did not, so a broken chip was a full-speed attacker the moment
        /// anything interesting was happening -- chasing the player, closing on a gun, going at a
        /// wall -- and only decayed while walking unopposed at the truck. The test that was meant to
        /// guard it exercised the one path that worked. A scale that every mover must remember to
        /// apply is a scale that will be forgotten again.
        /// </summary>
        private void SteerToward(int i, Vec2 pos, Vec2 target, float dt, float speed, bool gates)
        {
            Vec2 toTarget = (target - pos).Normalized();
            Vec2 separation = ComputeSeparation(i, pos);
            Vec2 desired = (toTarget + separation * _config.SeparationWeight).Normalized();
            float stepLength = MathF.Min(speed * SpeedScale(i) * dt, 0.9f);
            MoveResolved(i, pos, pos + desired * stepLength, gates);
        }

        private bool TryFindAdjacentWall(int cx, int cy, out int wx, out int wy)
        {
            // Fixed order: west, east, south, north. Determinism beats realism here.
            if (IsBreakable(cx - 1, cy)) { wx = cx - 1; wy = cy; return true; }
            if (IsBreakable(cx + 1, cy)) { wx = cx + 1; wy = cy; return true; }
            if (IsBreakable(cx, cy - 1)) { wx = cx; wy = cy - 1; return true; }
            if (IsBreakable(cx, cy + 1)) { wx = cx; wy = cy + 1; return true; }
            wx = -1; wy = -1;
            return false;
        }

        private bool IsBreakable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height) return false;
            var kind = _map.KindAt(x, y);
            if (kind == WallKind.None || kind == WallKind.Rock) return false;
            return _map.StageAt(x, y) != BreachStage.Collapsed;
        }

        /// <summary>
        /// Nearest breakable cell by expanding square rings. Bounded by
        /// <paramref name="maxRadius"/> so a wrecker with nothing nearby rejoins the crowd instead
        /// of scanning the whole map every tick.
        /// </summary>
        private bool FindNearestWallCell(int cx, int cy, int maxRadius, out int outX, out int outY)
        {
            outX = -1; outY = -1;
            for (int r = 1; r <= maxRadius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (IsBreakable(cx + dx, cy - r)) { outX = cx + dx; outY = cy - r; return true; }
                    if (IsBreakable(cx + dx, cy + r)) { outX = cx + dx; outY = cy + r; return true; }
                }
                for (int dy = -r + 1; dy <= r - 1; dy++)
                {
                    if (IsBreakable(cx - r, cy + dy)) { outX = cx - r; outY = cy + dy; return true; }
                    if (IsBreakable(cx + r, cy + dy)) { outX = cx + r; outY = cy + dy; return true; }
                }
            }
            return false;
        }
    }
}
