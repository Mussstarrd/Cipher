using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>Milestone 2: mutable walls, breach gates, honest preview (docs/design/sim-dynamic-walls-plan.md section 5).</summary>
    public class DynamicWallTests
    {
        private const float Dt = 1f / 30f;

        /// <summary>20x5 corridor, full wall column at x=10, goal at the east end.</summary>
        private static (GridMap map, FlowField field, AgentWorld world) Corridor()
        {
            var map = new GridMap(20, 5);
            for (int y = 0; y < 5; y++) map.SetWall(10, y, WallKind.Barricade, GridMap.DefaultWallHp);
            var field = new FlowField(map);
            field.Compute(18, 2);
            var world = new AgentWorld(map, field, new SimConfig(), initialCapacity: 256);
            return (map, field, world);
        }

        [Fact]
        public void Preview_ScratchField_EqualsLiveField_AfterCommit()
        {
            var map = new GridMap(24, 16);
            var field = new FlowField(map);
            field.Compute(22, 8);
            var world = new AgentWorld(map, field, new SimConfig());
            var validator = new BuildValidator(map);
            var cells = new List<(int, int)> { (10, 4), (10, 5), (10, 6), (10, 7), (10, 8), (10, 9) };
            var spawns = new List<(int, int)> { (1, 8) };

            Assert.Equal(PlacementResult.Ok, validator.Validate(world, cells, WallKind.Barricade, 200, 22, 8, spawns));

            BuildValidator.Commit(map, cells, WallKind.Barricade, 200);
            world.Step(Dt); // EnsureFresh recomputes the live field

            Assert.True(field.Equals(validator.PreviewField), "the what-if field must be cell-for-cell identical to the live field after commit");
            Assert.Equal(map.Version, field.ComputedForMapVersion);
        }

        [Fact]
        public void Placement_RefusedWhenCellOccupied_OrNotBuildable_OrOutOfBounds()
        {
            var (map, _, world) = Corridor();
            var validator = new BuildValidator(map);
            var spawns = new List<(int, int)> { (1, 2) };
            world.Spawn(new Vec2(5.5f, 2.5f), 10f);

            Assert.Equal(PlacementResult.Occupied, validator.Validate(world, new[] { (5, 2) }, WallKind.Barricade, 200, 18, 2, spawns));
            Assert.Equal(PlacementResult.NotBuildable, validator.Validate(world, new[] { (10, 2) }, WallKind.Barricade, 200, 18, 2, spawns));
            Assert.Equal(PlacementResult.OutOfBounds, validator.Validate(world, new[] { (25, 2) }, WallKind.Barricade, 200, 18, 2, spawns));
            // The fixture's wall column already seals the spawn, so a legal cell reports SealsSpawn (allowed, labelled), never a hard failure.
            Assert.Equal(PlacementResult.SealsSpawn, validator.Validate(world, new[] { (5, 3) }, WallKind.Barricade, 200, 18, 2, spawns));
            map.Clear(10, 0);
            Assert.Equal(PlacementResult.Ok, validator.Validate(world, new[] { (5, 3) }, WallKind.Barricade, 200, 18, 2, spawns));
            Assert.False(map.IsBlocked(5, 3), "validate never mutates the live map");
        }

        [Fact]
        public void Placement_ThatSealsSpawn_ReportsSealsSpawn_AndLiveMapUntouched()
        {
            var map = new GridMap(12, 3);
            var field = new FlowField(map);
            field.Compute(10, 1);
            var world = new AgentWorld(map, field, new SimConfig());
            var validator = new BuildValidator(map);

            var seal = new List<(int, int)> { (6, 0), (6, 1), (6, 2) };
            Assert.Equal(PlacementResult.SealsSpawn, validator.Validate(world, seal, WallKind.Barricade, 200, 10, 1, new[] { (1, 1) }));
            Assert.True(field.HasPath(1, 1));
            Assert.Equal(0, map.Version);
        }

        [Theory]
        [InlineData(BreachStage.Cracked, 1f)]
        [InlineData(BreachStage.Broken, 3f)]
        public void Gate_AdmitsAtMostRatePerSecond(BreachStage stage, float ratePerSecond)
        {
            // Isolate admission rate: freeze the stage so widening cannot muddy the count.
            var map = new GridMap(20, 5);
            for (int y = 0; y < 5; y++) map.SetWall(10, y, WallKind.Barricade, GridMap.DefaultWallHp);
            var field = new FlowField(map);
            field.Compute(18, 2);
            var world = new AgentWorld(map, field,
                new SimConfig { BreachStageSeconds = 10_000f, TrafficShaveSeconds = 0f }, initialCapacity: 256);

            map.Breach(10, 2);
            if (stage == BreachStage.Broken) map.Breach(10, 2);
            Assert.Equal(stage, map.StageAt(10, 2));
            Assert.Equal(1, map.GateCount);

            // A crowd of 60 agents already pressed against the hole.
            for (int i = 0; i < 60; i++) world.Spawn(new Vec2(7f + (i % 6) * 0.4f, 0.4f + (i / 6) * 0.42f), 10f);

            const float seconds = 10f;
            for (int t = 0; t < (int)(seconds / Dt); t++) world.Step(Dt);

            int crossed = 0;
            for (int id = 0; id < world.Count; id++)
                if (world.IsAlive(id) && world.PositionOf(id).X > 10.5f) crossed++;
            crossed += world.ReachedCount; // fast ones may already have left through the goal

            int expected = (int)(ratePerSecond * seconds);
            Assert.InRange(crossed, expected - 2, expected + 2);
        }

        [Fact]
        public void Gate_Throttle_IsBitExact_AcrossRuns()
        {
            ulong Run()
            {
                var (map, _, world) = Corridor();
                map.Breach(10, 2);
                for (int i = 0; i < 80; i++) world.Spawn(new Vec2(6f + (i % 8) * 0.45f, 0.4f + (i / 8) * 0.42f), 10f);
                for (int t = 0; t < 240; t++) world.Step(Dt);
                return world.StateHash();
            }
            Assert.Equal(Run(), Run());
        }

        [Fact]
        public void Breach_OpensHole_AgentsReroute_Repair_AgentsRerouteBack()
        {
            // Serpentine: wall column at x=8 open only at the top; spawn at bottom-left.
            var map = new GridMap(16, 8);
            for (int y = 1; y < 8; y++) map.SetWall(8, y, WallKind.Wall, GridMap.DefaultWallHp);
            var field = new FlowField(map);
            field.Compute(14, 6);
            var world = new AgentWorld(map, field, new SimConfig());

            float detour = field.IntegrationCostAt(1, 6);

            map.Breach(8, 6); map.Breach(8, 6); map.Breach(8, 6); // to Collapsed
            Assert.Equal(BreachStage.Collapsed, map.StageAt(8, 6));
            world.Step(Dt);
            float shortcut = field.IntegrationCostAt(1, 6);
            Assert.True(shortcut < detour - 5f, $"collapse must shorten the route ({detour} to {shortcut})");
            Assert.Equal(GridMap.MinCost, map.CostAt(8, 6));
            Assert.False(map.IsGate(8, 6));

            map.Repair(8, 6);
            world.Step(Dt);
            Assert.Equal(detour, field.IntegrationCostAt(1, 6));
            Assert.True(map.IsBlocked(8, 6));
            Assert.Equal(GridMap.DefaultWallHp, map.HpAt(8, 6));
        }

        [Fact]
        public void Breach_Stages_SetCostAndGates_RockIsImmune()
        {
            var map = new GridMap(4, 4);
            map.SetWall(1, 1, WallKind.Barricade, 50);
            map.SetWall(2, 2, WallKind.Rock, 1);

            Assert.Equal(BreachStage.Cracked, map.Breach(1, 1));
            Assert.Equal(GridMap.CrackedCost, map.CostAt(1, 1));
            Assert.False(map.IsBlocked(1, 1));
            Assert.True(map.IsGate(1, 1));
            Assert.Equal(1, map.GateCount);

            Assert.Equal(BreachStage.Broken, map.Breach(1, 1));
            Assert.Equal(GridMap.BrokenCost, map.CostAt(1, 1));
            Assert.Equal(1, map.GateCount);

            Assert.Equal(BreachStage.Collapsed, map.Breach(1, 1));
            Assert.Equal(0, map.GateCount);
            Assert.Equal(BreachStage.Collapsed, map.Breach(1, 1));

            Assert.Equal(BreachStage.Intact, map.Breach(2, 2));
            Assert.True(map.IsBlocked(2, 2));
            Assert.False(map.Damage(2, 2, 999));
        }

        [Fact]
        public void Damage_ReturnsTrueAtZeroHp_AndClearRemovesEverything()
        {
            var map = new GridMap(4, 4);
            map.SetWall(1, 1, WallKind.Barricade, 100);
            Assert.False(map.Damage(1, 1, 60));
            Assert.Equal(40, map.HpAt(1, 1));
            Assert.True(map.Damage(1, 1, 60));
            Assert.Equal(0, map.HpAt(1, 1));

            int v = map.Version;
            map.Clear(1, 1);
            Assert.True(map.IsBuildable(1, 1));
            Assert.Equal(WallKind.None, map.KindAt(1, 1));
            Assert.Equal(v + 1, map.Version);
        }

        [Fact]
        public void StateHash_CoversWallState()
        {
            var (map, _, world) = Corridor();
            ulong before = world.StateHash();
            map.Breach(10, 1);
            Assert.NotEqual(before, world.StateHash());
            map.Repair(10, 1);
            Assert.Equal(before, world.StateHash());
        }

        [Fact]
        public void FieldRecompute_CoalescesToOnePerTick()
        {
            var (map, field, world) = Corridor();
            int before = field.ComputeCount;
            for (int i = 0; i < 50; i++) map.SetWall(2 + (i % 7), 4, WallKind.Barricade, (ushort)(100 + i));
            world.Step(Dt);
            Assert.Equal(before + 1, field.ComputeCount);
            world.Step(Dt);
            Assert.Equal(before + 1, field.ComputeCount);
        }

        [Fact]
        public void Movement_And_Field_AgreeOnBreachedCells()
        {
            var (map, field, _) = Corridor();
            map.Breach(10, 2);
            field.EnsureFresh();
            Assert.True(Movement.IsOpen(map, GridMap.CellCenter(10, 2)));
            Assert.True(field.HasPath(1, 2));
            Assert.False(Movement.IsOpen(map, GridMap.CellCenter(10, 1)));
            for (int y = 0; y < 5; y++)
            {
                Vec2 d = field.DirectionAt(9, y);
                var (nx, ny) = map.WorldToCell(GridMap.CellCenter(9, y) + d);
                Assert.False(map.IsBlocked(nx, ny), $"direction at (9,{y}) points into a wall");
            }
        }

        [Fact]
        public void CopyTo_IsExact_AndIndependent()
        {
            var (map, _, _) = Corridor();
            map.Breach(10, 3);
            var copy = new GridMap(20, 5);
            map.CopyTo(copy);
            Assert.Equal(map.StateHash(), copy.StateHash());
            Assert.Equal(map.GateCount, copy.GateCount);
            copy.Breach(10, 3);
            Assert.NotEqual(map.StateHash(), copy.StateHash());
            Assert.Equal(BreachStage.Cracked, map.StageAt(10, 3));
        }
    }
}
