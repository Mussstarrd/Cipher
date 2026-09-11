using System.Collections.Generic;
using System.Diagnostics;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Cipher.Sim.Spatial;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>
    /// Regression guards for the 2026-09-10 live freeze: a Sapper opened the only route, the whole
    /// wave piled into the throttled cell, and uncapped neighbour queries made the per-tick cost
    /// quadratic in the pile size until the frame loop death-spiralled.
    /// </summary>
    public class DensePileTests
    {
        private const float Dt = 1f / 30f;

        [Fact]
        public void QueryCircle_IsUnbounded_ByDefault_ButCapsPerCell_WhenAsked()
        {
            var hash = new SpatialHash(0.6f);
            for (int i = 0; i < 500; i++) hash.Insert(i, new Vec2(5f, 5f)); // all in one cell

            var all = new List<int>();
            hash.QueryCircle(new Vec2(5f, 5f), 0.6f, all);
            Assert.Equal(500, all.Count);

            var capped = new List<int>();
            hash.QueryCircle(new Vec2(5f, 5f), 0.6f, capped, maxPerCell: 4);
            Assert.InRange(capped.Count, 1, 4 * 9); // at most the cap times the cells a 0.6 radius can span
            Assert.All(capped, id => Assert.Contains(id, all));
        }

        [Fact]
        public void Separation_CostStaysBounded_WhenTheWholeWavePilesIntoOneGate()
        {
            var (map, world) = GateCorridor();
            for (int i = 0; i < 600; i++)
                world.Spawn(new Vec2(8f + (i % 20) * 0.09f, 1.2f + (i / 20) * 0.09f), 10f);
            Assert.Equal(600, world.AliveCount);

            // Warm up the JIT, then time a fixed slice of the worst case.
            for (int t = 0; t < 30; t++) world.Step(Dt);
            var sw = Stopwatch.StartNew();
            for (int t = 0; t < 300; t++) world.Step(Dt); // 10 s of sim
            sw.Stop();

            // Generous: the capped version runs this in tens of milliseconds; the uncapped one took
            // tens of seconds. Anything near the old behaviour blows this budget by orders of magnitude.
            Assert.True(sw.ElapsedMilliseconds < 4000,
                $"600 agents piled on one gate took {sw.ElapsedMilliseconds} ms for 10 s of sim");
            Assert.True(map.IsGate(10, 2) || map.StageAt(10, 2) == BreachStage.Collapsed);
        }

        [Fact]
        public void PiledWave_DrainsThroughTheHole_AsItWidens()
        {
            var (map, world) = GateCorridor();
            for (int i = 0; i < 300; i++)
                world.Spawn(new Vec2(8f + (i % 15) * 0.12f, 1.2f + (i / 15) * 0.12f), 10f);

            for (int t = 0; t < (int)(90f / Dt); t++) world.Step(Dt);

            Assert.Equal(BreachStage.Collapsed, map.StageAt(10, 2));
            Assert.True(world.ReachedCount > 100, $"only {world.ReachedCount} of 300 got through in 90 s");
        }

        [Fact]
        public void DensePile_IsStillBitExact_AcrossRuns()
        {
            ulong Run()
            {
                var (_, world) = GateCorridor();
                for (int i = 0; i < 200; i++)
                    world.Spawn(new Vec2(8f + (i % 14) * 0.1f, 1.2f + (i / 14) * 0.1f), 10f);
                for (int t = 0; t < 600; t++) world.Step(Dt);
                return world.StateHash();
            }
            Assert.Equal(Run(), Run());
        }

        [Fact]
        public void DeadSapperPlans_DoNotAccumulate()
        {
            var map = new GridMap(30, 21);
            for (int y = 1; y < 21; y++) map.SetWall(15, y, WallKind.Barricade, GridMap.DefaultWallHp);
            var field = new FlowField(map);
            field.Compute(28, 19);
            var world = new AgentWorld(map, field, new SimConfig());

            for (int n = 0; n < 40; n++)
            {
                int id = world.SpawnArchetype(new Vec2(2.5f, 19.5f), Archetype.Sapper);
                world.Step(Dt);
                world.ApplyDamage(id, 1000f); // chip broken on the walk, every time
                // ADR-008: the body does not drop until its failure window closes, so the plan is
                // not pruned until then either. Step past it.
                for (int t = 0; t < 40; t++) world.Step(0.1f);
            }

            Assert.Equal(0, world.ActiveSapperCount);
            Assert.Equal(BreachStage.Intact, map.StageAt(15, 19));
            // 40 dead planners must not leave 40 live plans behind; one more spawn still works.
            int last = world.SpawnArchetype(new Vec2(2.5f, 19.5f), Archetype.Sapper);
            Assert.NotEqual(-1, world.SapperTargetCell(last));
        }

        /// <summary>20x5 corridor sealed at x=10 except for a single cracked (throttled) hole at y=2.</summary>
        private static (GridMap map, AgentWorld world) GateCorridor()
        {
            var map = new GridMap(20, 5);
            for (int y = 0; y < 5; y++) map.SetWall(10, y, WallKind.Barricade, GridMap.DefaultWallHp);
            var field = new FlowField(map);
            field.Compute(18, 2);
            var world = new AgentWorld(map, field, new SimConfig(), initialCapacity: 1024);
            map.Breach(10, 2); // Cracked: admits ~1 agent/s
            return (map, world);
        }
    }
}
