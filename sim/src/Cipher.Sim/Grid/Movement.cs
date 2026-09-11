#nullable enable
using Cipher.Sim.Core;

namespace Cipher.Sim.Grid
{
    /// <summary>
    /// The one wall-collision rule for anything that walks the grid — swarm agents and
    /// the hero alike. Shares the flow field's corner-cut rule so nothing can squeeze
    /// through a sealed wall corner the field treats as impassable ("preview never lies").
    /// </summary>
    public static class Movement
    {
        public static bool IsOpen(GridMap map, Vec2 pos)
        {
            if (pos.X < 0f || pos.Y < 0f || pos.X >= map.Width || pos.Y >= map.Height)
                return false;
            var (x, y) = map.WorldToCell(pos);
            return !map.IsBlocked(x, y);
        }

        /// <summary>
        /// The destination must be open AND, for a diagonal cell transition, both shared
        /// orthogonal cells must be open.
        /// </summary>
        public static bool CanTravel(GridMap map, Vec2 from, Vec2 to)
        {
            if (!IsOpen(map, to)) return false;

            var (fx, fy) = map.WorldToCell(from);
            var (tx, ty) = map.WorldToCell(to);
            if (tx != fx && ty != fy && (map.IsBlocked(tx, fy) || map.IsBlocked(fx, ty)))
                return false;

            return true;
        }

        /// <summary>
        /// Marches a ray from <paramref name="origin"/> and reports the first blocked cell it enters.
        /// Walls stop bullets: without this the hero shoots straight through the maze. Step is a
        /// quarter cell, so a ray can never skip over a cell on any axis. Deterministic, no allocation.
        /// </summary>
        public static bool FirstBlockedCell(GridMap map, Vec2 origin, Vec2 direction, float maxDistance,
                                            out int cellX, out int cellY, out float distance)
        {
            cellX = -1; cellY = -1; distance = float.PositiveInfinity;
            Vec2 dir = direction.Normalized();
            if (dir.LengthSquared < 0.5f || maxDistance <= 0f) return false;

            const float step = 0.25f;

            // START PAST THE SHOOTER'S OWN CELL. A turret stands on a cell marked Structure, which
            // IS blocked, so a march that samples the origin finds a wall at distance zero and the
            // turret decides it cannot see anything at all. That is exactly what shipped: line of
            // sight went in, and every turret in the game silently stopped firing, because each one
            // was blocked by itself. The same applies to a shot fired by anything standing on a
            // solid cell; nothing can be hit by the cell it is already inside.
            var (originX, originY) = map.WorldToCell(origin);
            int lastX = originX, lastY = originY;

            for (float travelled = 0f; travelled <= maxDistance; travelled += step)
            {
                Vec2 p = origin + dir * travelled;
                if (p.X < 0f || p.Y < 0f || p.X >= map.Width || p.Y >= map.Height) return false;
                var (x, y) = map.WorldToCell(p);
                if (x == lastX && y == lastY) continue;
                lastX = x; lastY = y;
                if (!map.IsBlocked(x, y)) continue;
                cellX = x; cellY = y; distance = travelled;
                return true;
            }
            return false;
        }

        /// <summary>Blocked-cell collision with axis sliding: full move, then x-only, then y-only, else stay.</summary>
        public static Vec2 ResolveWalls(GridMap map, Vec2 from, Vec2 to)
        {
            if (CanTravel(map, from, to)) return to;

            var slideX = new Vec2(to.X, from.Y);
            if (CanTravel(map, from, slideX)) return slideX;

            var slideY = new Vec2(from.X, to.Y);
            if (CanTravel(map, from, slideY)) return slideY;

            return from;
        }
    }
}
