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
