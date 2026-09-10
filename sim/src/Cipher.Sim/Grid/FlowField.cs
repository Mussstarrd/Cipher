using System;
using Cipher.Sim.Core;

namespace Cipher.Sim.Grid
{
    /// <summary>
    /// Dijkstra integration field + per-cell direction toward a single goal.
    /// One field serves any number of agents — this is why 1,000 agents path for
    /// the price of one graph search (docs/03, Trap 1). Recompute on map change;
    /// callers compare <see cref="ComputedForMapVersion"/> against GridMap.Version.
    /// </summary>
    public sealed class FlowField : IFlowField
    {
        private const float Sqrt2 = 1.41421356f;

        // 8-way neighborhood. Order is fixed: it is part of the sim's determinism contract.
        private static readonly (int Dx, int Dy)[] Neighbors =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1),
        };

        private readonly GridMap _map;
        private readonly float[] _integration;
        private readonly Vec2[] _direction;

        public int GoalX { get; private set; }
        public int GoalY { get; private set; }
        public int ComputedForMapVersion { get; private set; } = -1;

        public FlowField(GridMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _integration = new float[map.Width * map.Height];
            _direction = new Vec2[map.Width * map.Height];
        }

        public Vec2 DirectionAt(int x, int y) => _direction[_map.CellIndex(x, y)];

        public bool HasPath(int x, int y) => !float.IsPositiveInfinity(_integration[_map.CellIndex(x, y)]);

        public float IntegrationCostAt(int x, int y) => _integration[_map.CellIndex(x, y)];

        /// <summary>Full recompute from a goal cell. (Incremental dirty-region recompute is a planned optimization; the API will not change.)</summary>
        public void Compute(int goalX, int goalY)
        {
            if (!_map.InBounds(goalX, goalY))
                throw new ArgumentOutOfRangeException(nameof(goalX), "Goal must be inside the grid.");
            if (_map.IsBlocked(goalX, goalY))
                throw new ArgumentException("Goal cell is blocked.", nameof(goalX));

            GoalX = goalX;
            GoalY = goalY;

            Array.Fill(_integration, float.PositiveInfinity);
            Array.Fill(_direction, Vec2.Zero);

            RunDijkstra(goalX, goalY);
            BakeDirections();

            ComputedForMapVersion = _map.Version;
        }

        private void RunDijkstra(int goalX, int goalY)
        {
            var heap = new MinHeap(_map.Width * _map.Height / 4);
            int goalIndex = _map.CellIndex(goalX, goalY);
            _integration[goalIndex] = 0f;
            heap.Push(0f, goalIndex);

            while (heap.Count > 0)
            {
                var (cost, index) = heap.Pop();
                if (cost > _integration[index]) continue; // stale heap entry

                int cx = index % _map.Width;
                int cy = index / _map.Width;

                foreach (var (dx, dy) in Neighbors)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (!_map.InBounds(nx, ny) || _map.IsBlocked(nx, ny)) continue;
                    if (IsDiagonal(dx, dy) && CutsBlockedCorner(cx, cy, dx, dy)) continue;

                    float stepCost = _map.CostAt(nx, ny) * (IsDiagonal(dx, dy) ? Sqrt2 : 1f);
                    float candidate = cost + stepCost;
                    int neighborIndex = _map.CellIndex(nx, ny);
                    if (candidate < _integration[neighborIndex])
                    {
                        _integration[neighborIndex] = candidate;
                        heap.Push(candidate, neighborIndex);
                    }
                }
            }
        }

        private void BakeDirections()
        {
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    int index = _map.CellIndex(x, y);
                    if (float.IsPositiveInfinity(_integration[index])) continue;
                    if (x == GoalX && y == GoalY) continue;

                    float best = _integration[index];
                    int bestDx = 0, bestDy = 0;

                    foreach (var (dx, dy) in Neighbors)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (!_map.InBounds(nx, ny) || _map.IsBlocked(nx, ny)) continue;
                        if (IsDiagonal(dx, dy) && CutsBlockedCorner(x, y, dx, dy)) continue;

                        float neighborCost = _integration[_map.CellIndex(nx, ny)];
                        if (neighborCost < best)
                        {
                            best = neighborCost;
                            bestDx = dx;
                            bestDy = dy;
                        }
                    }

                    if (bestDx != 0 || bestDy != 0)
                        _direction[index] = new Vec2(bestDx, bestDy).Normalized();
                }
            }
        }

        private static bool IsDiagonal(int dx, int dy) => dx != 0 && dy != 0;

        /// <summary>A diagonal step is legal only when both adjacent orthogonal cells are open — agents must not squeeze through wall corners.</summary>
        private bool CutsBlockedCorner(int x, int y, int dx, int dy)
            => _map.IsBlocked(x + dx, y) || _map.IsBlocked(x, y + dy);
    }
}
