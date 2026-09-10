using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    public class FlowFieldTests
    {
        [Fact]
        public void OpenGrid_AllCellsReachGoal_AndGoalCostIsZero()
        {
            var map = new GridMap(10, 10);
            var field = new FlowField(map);
            field.Compute(5, 5);

            Assert.Equal(0f, field.IntegrationCostAt(5, 5));
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 10; x++)
                    Assert.True(field.HasPath(x, y), $"Cell ({x},{y}) should reach the goal on an open grid.");
        }

        [Fact]
        public void Wall_ForcesDetour_IntegrationCostReflectsIt()
        {
            // 11x11, goal at right edge, full vertical wall at x=5 except a gap at y=10.
            var map = new GridMap(11, 11);
            for (int y = 0; y < 10; y++) map.SetBlocked(5, y, true);

            var field = new FlowField(map);
            field.Compute(10, 0);

            // Direct distance from (0,0) is 10; the detour through (5,10) must cost well over that.
            Assert.True(field.HasPath(0, 0));
            Assert.True(field.IntegrationCostAt(0, 0) > 15f,
                $"Expected a detour cost > 15, got {field.IntegrationCostAt(0, 0)}.");
        }

        [Fact]
        public void SealedRegion_HasNoPath()
        {
            var map = new GridMap(10, 10);
            // Box in the left column fully: wall at x=1.
            for (int y = 0; y < 10; y++) map.SetBlocked(1, y, true);

            var field = new FlowField(map);
            field.Compute(9, 5);

            for (int y = 0; y < 10; y++)
            {
                Assert.False(field.HasPath(0, y), $"Sealed cell (0,{y}) must have no path.");
                Assert.Equal(Vec2.Zero, field.DirectionAt(0, y));
            }
        }

        [Fact]
        public void Directions_NeverPointIntoBlockedCells()
        {
            var map = new GridMap(20, 20);
            // Scatter some walls deterministically.
            for (int i = 0; i < 20; i += 3) map.SetBlocked(i, 10, true);
            for (int i = 1; i < 20; i += 4) map.SetBlocked(7, i, true);

            var field = new FlowField(map);
            field.Compute(18, 18);

            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    if (map.IsBlocked(x, y) || !field.HasPath(x, y)) continue;
                    var dir = field.DirectionAt(x, y);
                    if (dir.Equals(Vec2.Zero)) continue;

                    // One unit step along the direction must not land in a blocked cell.
                    var probe = GridMap.CellCenter(x, y) + dir;
                    var (px, py) = map.WorldToCell(probe);
                    Assert.False(map.IsBlocked(px, py),
                        $"Direction at ({x},{y}) points into blocked cell ({px},{py}).");
                }
            }
        }

        [Fact]
        public void DiagonalMovement_DoesNotCutBlockedCorners()
        {
            // Goal diagonal from start with both orthogonal cells blocked:
            // the field must route around, not squeeze through the corner.
            var map = new GridMap(3, 3);
            map.SetBlocked(1, 0, true);
            map.SetBlocked(0, 1, true);

            var field = new FlowField(map);
            field.Compute(2, 2);

            // (0,0) is sealed off by the two walls (its only exits are the blocked
            // orthogonals and the illegal corner-cut diagonal).
            Assert.False(field.HasPath(0, 0), "Corner-cut diagonal must be illegal, sealing (0,0).");
        }

        [Fact]
        public void HigherCostTerrain_IsAvoided_WhenCheaperRouteExists()
        {
            // Two equal-length corridors; one is mud (cost 5). Direction at the fork must choose the clean one.
            var map = new GridMap(7, 3);
            // Row y=1 is a wall except the two corridor mouths at x=0 and x=6.
            for (int x = 1; x < 6; x++) map.SetBlocked(x, 1, true);
            // Top corridor (y=2) is mud.
            for (int x = 0; x < 7; x++) map.SetCost(x, 2, 5);

            var field = new FlowField(map);
            field.Compute(6, 0); // goal on the bottom row's right end

            // From the top-left, integration through the clean bottom row must beat staying in mud.
            Assert.True(field.IntegrationCostAt(0, 2) < 5f * 6f,
                "Route should drop into the cheap corridor rather than pay mud cost the whole way.");
        }

        [Fact]
        public void Recompute_AfterBarricade_RaisesCostAndTracksMapVersion()
        {
            var map = new GridMap(12, 12);
            var field = new FlowField(map);
            field.Compute(11, 6);
            float before = field.IntegrationCostAt(0, 6);
            Assert.Equal(map.Version, field.ComputedForMapVersion);

            // Player builds a wall with a gap — the mazing move.
            for (int y = 0; y < 11; y++) map.SetBlocked(6, y, true);
            Assert.NotEqual(map.Version, field.ComputedForMapVersion); // staleness is detectable

            field.Compute(11, 6);
            float after = field.IntegrationCostAt(0, 6);

            Assert.Equal(map.Version, field.ComputedForMapVersion);
            Assert.True(after > before, $"Barricade must lengthen the route ({before} -> {after}).");
        }
    }
}
