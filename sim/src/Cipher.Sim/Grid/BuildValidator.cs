#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;

namespace Cipher.Sim.Grid
{
    public enum PlacementResult
    {
        Ok = 0,
        /// <summary>Cell is not open floor (wall, rock, existing structure).</summary>
        NotBuildable,
        /// <summary>A living agent stands in the cell; placing would entomb it.</summary>
        Occupied,
        /// <summary>Legal, but at least one spawn would have no path to the goal. Allowed, loudly labelled.</summary>
        SealsSpawn,
        OutOfBounds,
    }

    /// <summary>
    /// The one placement check used by BOTH the build-mode preview and the commit path
    /// ("preview never lies", docs/03 Trap 5 / Open Decision #5). It runs the live
    /// FlowField class on a scratch copy of the live map, so the what-if is the real algorithm.
    /// Allocated once; no per-call garbage.
    /// </summary>
    public sealed class BuildValidator
    {
        private readonly GridMap _scratchMap;
        private readonly FlowField _scratchField;

        public BuildValidator(GridMap live)
        {
            _scratchMap = new GridMap(live.Width, live.Height);
            _scratchField = new FlowField(_scratchMap);
        }

        /// <summary>The what-if field after the last <see cref="Validate"/>; the preview overlay reads directions from it.</summary>
        public FlowField PreviewField => _scratchField;
        public GridMap PreviewMap => _scratchMap;

        /// <summary>
        /// Checks placing <paramref name="kind"/> on every cell, against the live world. Never mutates the live map.
        /// Returns the first hard failure, else SealsSpawn if any spawn loses its path, else Ok.
        /// </summary>
        public PlacementResult Validate(AgentWorld world, IReadOnlyList<(int X, int Y)> cells, WallKind kind, ushort hp,
                                        int goalX, int goalY, IReadOnlyList<(int X, int Y)> spawns)
        {
            GridMap live = world.Map;
            foreach (var (x, y) in cells)
            {
                if (!live.InBounds(x, y)) return PlacementResult.OutOfBounds;
                if (!live.IsBuildable(x, y)) return PlacementResult.NotBuildable;
                if (world.IsCellOccupied(x, y)) return PlacementResult.Occupied;
            }

            live.CopyTo(_scratchMap);
            foreach (var (x, y) in cells) _scratchMap.SetWall(x, y, kind, hp);
            _scratchField.Compute(goalX, goalY);

            foreach (var (sx, sy) in spawns)
                if (!_scratchField.HasPath(sx, sy)) return PlacementResult.SealsSpawn;

            return PlacementResult.Ok;
        }

        /// <summary>Applies the same cells to the live map. Callers validate first; this trusts them.</summary>
        public static void Commit(GridMap live, IReadOnlyList<(int X, int Y)> cells, WallKind kind, ushort hp)
        {
            foreach (var (x, y) in cells) live.SetWall(x, y, kind, hp);
        }
    }
}
