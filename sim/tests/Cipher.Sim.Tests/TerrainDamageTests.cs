using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>Player weapons erode the map (docs/design/arsenal-and-terrain.md section 2).</summary>
    public class TerrainDamageTests
    {
        private static (GridMap map, AgentWorld world) World()
        {
            var map = new GridMap(24, 9);
            for (int y = 0; y < 9; y++) map.SetWall(12, y, WallKind.Wall, GridMap.DefaultWallHp);
            var field = new FlowField(map);
            field.Compute(22, 4);
            return (map, new AgentWorld(map, field, new SimConfig()));
        }

        [Fact]
        public void Ray_StopsAtTheFirstWall_AndReportsTheCell()
        {
            var (map, _) = World();
            Assert.True(Movement.FirstBlockedCell(map, new Vec2(2.5f, 4.5f), new Vec2(1f, 0f), 25f,
                out int x, out int y, out float dist));
            Assert.Equal((12, 4), (x, y));
            Assert.InRange(dist, 9.4f, 10.1f);

            // Aimed down an open lane: nothing blocks.
            map.Clear(12, 4);
            Assert.False(Movement.FirstBlockedCell(map, new Vec2(2.5f, 4.5f), new Vec2(1f, 0f), 25f, out _, out _, out _));

            // Zero direction and zero range never report a hit.
            Assert.False(Movement.FirstBlockedCell(map, new Vec2(2.5f, 4.5f), Vec2.Zero, 25f, out _, out _, out _));
            Assert.False(Movement.FirstBlockedCell(map, new Vec2(2.5f, 3.5f), new Vec2(1f, 0f), 0f, out _, out _, out _));
        }

        [Fact]
        public void DamagingAWall_ToZero_OpensABreachStage()
        {
            var (map, world) = World();
            Assert.False(world.DamageWall(12, 4, 150f, 0.25f));
            Assert.Equal(BreachStage.Intact, map.StageAt(12, 4));
            Assert.Equal(50, map.HpAt(12, 4));

            Assert.True(world.DamageWall(12, 4, 60f, 0.25f));
            Assert.Equal(BreachStage.Cracked, map.StageAt(12, 4));
            Assert.False(map.IsBlocked(12, 4));
            Assert.Equal(1, map.GateCount);
        }

        [Fact]
        public void PlayerWalls_TakeOnlyTheFriendlyShare_AndRockIsImmune()
        {
            var map = new GridMap(8, 8);
            map.SetWall(1, 1, WallKind.Barricade, 100);
            map.SetWall(3, 3, WallKind.Rock, 100);
            var field = new FlowField(map);
            field.Compute(6, 6);
            var world = new AgentWorld(map, field, new SimConfig());

            world.DamageWall(1, 1, 100f, 0.25f);
            Assert.Equal(75, map.HpAt(1, 1)); // a quarter of 100

            world.DamageWall(3, 3, 1000f, 0.25f);
            Assert.Equal(100, map.HpAt(3, 3));
            Assert.True(map.IsBlocked(3, 3));
        }

        [Fact]
        public void RadiusDamage_HitsEveryWallCellInside_AndEmitsEvents()
        {
            var (map, world) = World();
            var events = new System.Collections.Generic.List<SimEvent>();
            world.DrainEvents(events);
            events.Clear();

            int advanced = world.DamageWallsInRadius(GridMap.CellCenter(12, 4), 2f, 250f, 0.25f);
            Assert.InRange(advanced, 3, 5); // a 2-cell radius covers ~5 cells of a vertical wall
            Assert.Equal(BreachStage.Cracked, map.StageAt(12, 4));
            Assert.Equal(BreachStage.Intact, map.StageAt(12, 8)); // outside the radius stays whole

            world.DrainEvents(events);
            Assert.Contains(events, e => e.Kind == SimEventKind.BreachStage);
        }

        [Fact]
        public void BreachesOpenedByThePlayer_WidenLikeAnyOther()
        {
            var (map, world) = World();
            world.DamageWall(12, 4, 1000f, 0.25f);
            Assert.Equal(BreachStage.Cracked, map.StageAt(12, 4));

            var cfg = new SimConfig();
            for (int t = 0; t < (int)(cfg.BreachStageSeconds * 2.2f * 30f); t++) world.Step(1f / 30f);
            Assert.Equal(BreachStage.Collapsed, map.StageAt(12, 4));
        }

        [Fact]
        public void TerrainDamage_IsDeterministic()
        {
            ulong Run()
            {
                var (map, world) = World();
                for (int i = 0; i < 20; i++)
                {
                    world.DamageWallsInRadius(GridMap.CellCenter(12, 1 + i % 7), 1.5f, 90f, 0.25f);
                    world.Step(1f / 30f);
                }
                return world.StateHash();
            }
            Assert.Equal(Run(), Run());
        }
    }
}
