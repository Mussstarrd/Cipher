using System;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>
    /// Regression guards from the first adversarial sim review (docs/05 §4: a bug
    /// found twice becomes a test — these were found once and get tests anyway).
    /// </summary>
    public class ReviewRegressionTests
    {
        [Fact]
        public void GridMap_OutOfRangeCoordinates_ThrowInsteadOfAliasing()
        {
            // Pre-fix: SetBlocked(-1, 5) linearized to index 49 and silently blocked (9, 4).
            var map = new GridMap(10, 10);

            Assert.Throws<ArgumentOutOfRangeException>(() => map.SetBlocked(-1, 5, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.IsBlocked(10, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.CostAt(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.SetCost(0, 10, 2));

            // And the aliasing victim cell is untouched.
            Assert.False(map.IsBlocked(9, 4));
        }

        [Fact]
        public void Spawn_WithMinimalInitialCapacity_GrowsInsteadOfCrashing()
        {
            // Pre-fix: capacity 0 doubled to 0 and Spawn threw IndexOutOfRangeException.
            var map = new GridMap(10, 10);
            var field = new FlowField(map);
            field.Compute(9, 9);
            var world = new AgentWorld(map, field, new SimConfig(), initialCapacity: 0);

            for (int i = 0; i < 10; i++)
                world.Spawn(new Vec2(1f + i * 0.1f, 1f), health: 5f);

            Assert.Equal(10, world.AliveCount);
        }

        private sealed class ConstantDiagonalField : IFlowField
        {
            public int GoalX => 9;
            public int GoalY => 9;
            public Vec2 DirectionAt(int x, int y) => new Vec2(1f, 1f).Normalized();
            public bool HasPath(int x, int y) => true;
            public float IntegrationCostAt(int x, int y) => 0f;
            public void EnsureFresh() { }
        }

        [Fact]
        public void Agents_CannotSqueezeThroughSealedDiagonalCorners()
        {
            // Cells (5,4) and (4,5) blocked; (4,4) and (5,5) open. The flow field's
            // corner-cut rule treats the (4,4)→(5,5) junction as sealed, so movement
            // must too. Pre-fix: an agent at (4.95, 4.95) pushed diagonally crossed it.
            var map = new GridMap(10, 10);
            map.SetBlocked(5, 4, true);
            map.SetBlocked(4, 5, true);

            var world = new AgentWorld(map, new ConstantDiagonalField(), new SimConfig());
            int id = world.Spawn(new Vec2(4.95f, 4.95f), health: 10f);

            for (int step = 0; step < 60; step++)
            {
                world.Step(1f / 30f);
                var (cx, cy) = map.WorldToCell(world.PositionOf(id));
                Assert.True(cx == 4 && cy == 4,
                    $"Agent crossed a sealed corner into ({cx},{cy}) at step {step}.");
            }
        }

        [Fact]
        public void ExtremeMoveSpeed_CannotTunnelThroughAWall()
        {
            // Displacement is capped below one cell per tick, so even absurd configured
            // speeds cannot step across a wall column in a single tick.
            var map = new GridMap(20, 5);
            for (int y = 0; y < 5; y++) map.SetBlocked(10, y, true);

            var config = new SimConfig { MoveSpeed = 500f, SeparationWeight = 0f };
            var world = new AgentWorld(map, new ConstantDiagonalField(), config);
            int id = world.Spawn(new Vec2(9.5f, 2.5f), health: 10f);

            for (int step = 0; step < 120; step++)
            {
                world.Step(1f / 30f);
                var (cx, _) = map.WorldToCell(world.PositionOf(id));
                Assert.True(cx <= 9, $"Agent tunneled the x=10 wall to column {cx}.");
            }
        }
    }
}
