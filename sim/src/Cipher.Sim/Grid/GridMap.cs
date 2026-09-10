#nullable enable
using System;
using Cipher.Sim.Core;

namespace Cipher.Sim.Grid
{
    /// <summary>
    /// The tactical grid: traversal costs and blocked cells (walls, barricades).
    /// World units: one cell = 1.0 x 1.0, cell (x, y) has its center at (x + 0.5, y + 0.5).
    /// </summary>
    public sealed class GridMap
    {
        public const byte MinCost = 1;

        public int Width { get; }
        public int Height { get; }

        private readonly byte[] _cost;   // >= MinCost for passable cells
        private readonly bool[] _blocked;

        /// <summary>Bumped on every mutation so dependents (flow fields) can detect staleness.</summary>
        public int Version { get; private set; }

        public GridMap(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");

            Width = width;
            Height = height;
            _cost = new byte[width * height];
            _blocked = new bool[width * height];
            for (int i = 0; i < _cost.Length; i++) _cost[i] = MinCost;
        }

        /// <summary>Linear index for a cell. Out-of-range coordinates throw — silent linearization would alias to a different valid cell and corrupt it.</summary>
        public int CellIndex(int x, int y)
        {
            if (!InBounds(x, y))
                throw new ArgumentOutOfRangeException(nameof(x), $"Cell ({x},{y}) is outside the {Width}x{Height} grid.");
            return y * Width + x;
        }

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsBlocked(int x, int y) => _blocked[CellIndex(x, y)];

        public byte CostAt(int x, int y) => _cost[CellIndex(x, y)];

        public void SetBlocked(int x, int y, bool blocked)
        {
            int i = CellIndex(x, y);
            if (_blocked[i] == blocked) return;
            _blocked[i] = blocked;
            Version++;
        }

        public void SetCost(int x, int y, byte cost)
        {
            if (cost < MinCost)
                throw new ArgumentOutOfRangeException(nameof(cost), $"Cost must be >= {MinCost}; use SetBlocked for impassable cells.");
            int i = CellIndex(x, y);
            if (_cost[i] == cost) return;
            _cost[i] = cost;
            Version++;
        }

        public static Vec2 CellCenter(int x, int y) => new Vec2(x + 0.5f, y + 0.5f);

        public (int X, int Y) WorldToCell(Vec2 position)
        {
            int x = Math.Clamp((int)MathF.Floor(position.X), 0, Width - 1);
            int y = Math.Clamp((int)MathF.Floor(position.Y), 0, Height - 1);
            return (x, y);
        }
    }
}
