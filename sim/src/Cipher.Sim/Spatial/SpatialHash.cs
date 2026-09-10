#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Core;

namespace Cipher.Sim.Spatial
{
    /// <summary>
    /// Uniform-grid spatial hash for neighbor queries (separation steering, AoE damage).
    /// Rebuilt each tick in agent-index order; queries scan covered buckets in a fixed
    /// order, so results are deterministic. Buckets are pooled — steady-state allocation is zero.
    /// </summary>
    public sealed class SpatialHash
    {
        private readonly float _cellSize;
        private readonly Dictionary<long, List<int>> _buckets = new Dictionary<long, List<int>>();
        private readonly Stack<List<int>> _pool = new Stack<List<int>>();
        private readonly List<Vec2> _positions = new List<Vec2>();

        public SpatialHash(float cellSize)
        {
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
            _cellSize = cellSize;
        }

        public void Clear()
        {
            foreach (var bucket in _buckets.Values)
            {
                bucket.Clear();
                _pool.Push(bucket);
            }
            _buckets.Clear();
            _positions.Clear();
        }

        /// <summary>Inserts an item; id must equal the number of prior inserts since Clear (i.e. insert in index order).</summary>
        public void Insert(int id, Vec2 position)
        {
            if (id != _positions.Count)
                throw new ArgumentException($"Items must be inserted in index order; expected {_positions.Count}, got {id}.", nameof(id));

            _positions.Add(position);
            long key = KeyFor(position);
            if (!_buckets.TryGetValue(key, out var bucket))
            {
                bucket = _pool.Count > 0 ? _pool.Pop() : new List<int>();
                _buckets[key] = bucket;
            }
            bucket.Add(id);
        }

        /// <summary>
        /// Appends ids of all items within <paramref name="radius"/> of <paramref name="center"/> to
        /// <paramref name="results"/> (not cleared), in ascending id order per bucket scan.
        ///
        /// <paramref name="maxPerCell"/> caps how many hits each grid cell may contribute. Queries that
        /// need completeness (damage, ray casts, occupancy) leave it unbounded; the swarm's neighbour
        /// avoidance caps it, because a throttled breach can pile hundreds of agents into one cell and
        /// an uncapped scan there is quadratic in the pile size (it froze a live build on 2026-09-10).
        /// Capping per cell rather than per query keeps neighbours sampled from every direction.
        /// </summary>
        public void QueryCircle(Vec2 center, float radius, List<int> results, int maxPerCell = int.MaxValue)
        {
            float radiusSq = radius * radius;
            int minX = CellCoord(center.X - radius);
            int maxX = CellCoord(center.X + radius);
            int minY = CellCoord(center.Y - radius);
            int maxY = CellCoord(center.Y + radius);

            for (int cy = minY; cy <= maxY; cy++)
            {
                for (int cx = minX; cx <= maxX; cx++)
                {
                    if (!_buckets.TryGetValue(Combine(cx, cy), out var bucket)) continue;
                    int taken = 0;
                    foreach (int id in bucket)
                    {
                        if (taken >= maxPerCell) break;
                        if (Vec2.DistanceSquared(_positions[id], center) <= radiusSq)
                        {
                            results.Add(id);
                            taken++;
                        }
                    }
                }
            }
        }

        private int CellCoord(float v) => (int)MathF.Floor(v / _cellSize);

        private long KeyFor(Vec2 position) => Combine(CellCoord(position.X), CellCoord(position.Y));

        private static long Combine(int x, int y) => ((long)x << 32) ^ (uint)y;
    }
}
