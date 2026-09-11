#nullable enable
using System;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

namespace Cipher.Sim.Agents
{
    /// <summary>
    /// Line of sight for emplacements. Owner's call, 2026-09-11:
    ///
    ///   "Towers should not be able to detect through walls [...] Towers shouldn't be able to shoot
    ///    through walls without the walls deteriorating."
    ///
    /// Before this, a turret behind a sealed wall was a free kill box: it saw and shot through its
    /// own barricade, which made the maze a decoration rather than a decision. Now a turret only
    /// engages what it can actually see, and anything that does get fired into a wall degrades it,
    /// so a player who walls in their own guns pays for it.
    /// </summary>
    public sealed partial class AgentWorld
    {
        /// <summary>
        /// True when nothing solid stands between the two points. Uses the same ray march as
        /// bullets, so what a turret can see and what a bullet can reach are the same rule.
        /// </summary>
        public bool HasLineOfSight(Vec2 from, Vec2 to)
        {
            Vec2 delta = to - from;
            float distance = delta.Length;
            if (distance <= 0.0001f) return true;

            return !Movement.FirstBlockedCell(_map, from, delta, distance, out _, out _, out float hit)
                   || hit >= distance - 0.0001f;
        }

        /// <summary>
        /// The wall a shot from <paramref name="from"/> toward <paramref name="to"/> would strike
        /// first, if any. Returns false when the path is clear.
        /// </summary>
        public bool FirstWallBetween(Vec2 from, Vec2 to, out int cellX, out int cellY)
        {
            cellX = -1; cellY = -1;
            Vec2 delta = to - from;
            float distance = delta.Length;
            if (distance <= 0.0001f) return false;

            if (!Movement.FirstBlockedCell(_map, from, delta, distance, out int wx, out int wy, out float hit))
                return false;
            if (hit >= distance - 0.0001f) return false;

            cellX = wx; cellY = wy;
            return true;
        }

        /// <summary>
        /// Closest-to-the-goal agent in range that the emplacement can actually see. Same ordering
        /// rule as <see cref="FindFirstInRange"/> so targeting stays deterministic; only the
        /// visibility filter is new.
        /// </summary>
        public int FindFirstInRangeVisible(Vec2 center, float radius)
        {
            RebuildHashIfDirty();
            _queryScratch.Clear();
            _hash.QueryCircle(center, radius, _queryScratch);

            int best = -1;
            float bestCost = float.PositiveInfinity;
            foreach (int hashId in _queryScratch)
            {
                int id = _hashToAgent[hashId];
                // A body whose chip is already failing is not worth a round. Without this a turret
                // empties itself into someone who is going down anyway while the person behind them
                // walks past, which is the opposite of what an auto-targeting gun should do.
                if (!_alive[id] || _failing[id] > 0f) continue;

                var target = new Vec2(_posX[id], _posY[id]);
                if (!HasLineOfSight(center, target)) continue;

                var (cx, cy) = _map.WorldToCell(target);
                float cost = _flowField.IntegrationCostAt(cx, cy);
                if (cost < bestCost || (cost == bestCost && id < best))
                {
                    bestCost = cost;
                    best = id;
                }
            }
            return best;
        }

        /// <summary>Area damage that respects cover. A blast does not reach through a barricade.</summary>
        public int ApplyRadialDamageVisible(Vec2 center, float radius, float damage)
        {
            RebuildHashIfDirty();
            _queryScratch.Clear();
            _hash.QueryCircle(center, radius, _queryScratch);

            int kills = 0;
            float r2 = radius * radius;
            // Ascending id keeps the kill order fixed regardless of hash bucket order.
            _queryScratch.Sort();
            foreach (int hashId in _queryScratch)
            {
                int id = _hashToAgent[hashId];
                if (!_alive[id]) continue;

                var target = new Vec2(_posX[id], _posY[id]);
                float dx = target.X - center.X, dy = target.Y - center.Y;
                if (dx * dx + dy * dy > r2) continue;
                if (!HasLineOfSight(center, target)) continue;

                if (ApplyDamage(id, damage)) kills++;
            }
            return kills;
        }

        /// <summary>Number of visible agents in range. Used to decide whether an area gun bothers firing.</summary>
        public int CountWithinVisible(Vec2 center, float radius)
        {
            RebuildHashIfDirty();
            _queryScratch.Clear();
            _hash.QueryCircle(center, radius, _queryScratch);

            int n = 0;
            float r2 = radius * radius;
            foreach (int hashId in _queryScratch)
            {
                int id = _hashToAgent[hashId];
                if (!_alive[id]) continue;
                var target = new Vec2(_posX[id], _posY[id]);
                float dx = target.X - center.X, dy = target.Y - center.Y;
                if (dx * dx + dy * dy > r2) continue;
                if (!HasLineOfSight(center, target)) continue;
                n++;
            }
            return n;
        }
    }
}
