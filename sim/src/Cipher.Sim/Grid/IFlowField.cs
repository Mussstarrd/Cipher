#nullable enable
using Cipher.Sim.Core;

namespace Cipher.Sim.Grid
{
    /// <summary>
    /// Pathing guidance sampled by agents AND by the build-mode preview.
    /// Covenant (docs/03, Trap 5): the preview must consume the same implementation
    /// the live sim uses — never a parallel reimplementation.
    /// </summary>
    public interface IFlowField
    {
        /// <summary>Goal cell coordinates this field currently points toward.</summary>
        int GoalX { get; }
        int GoalY { get; }

        /// <summary>Unit direction toward the goal from this cell; Vec2.Zero at the goal or where no path exists.</summary>
        Vec2 DirectionAt(int x, int y);

        /// <summary>True when the goal is reachable from this cell.</summary>
        bool HasPath(int x, int y);

        /// <summary>Accumulated travel cost from this cell to the goal (float.PositiveInfinity when unreachable).</summary>
        float IntegrationCostAt(int x, int y);

        /// <summary>Recompute if the map changed since the last compute. The sim calls this once per tick.</summary>
        void EnsureFresh();
    }
}
