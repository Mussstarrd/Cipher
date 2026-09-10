using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    public class AgentWorldTests
    {
        private const float Dt = 1f / 30f; // fixed tick

        private static (GridMap Map, FlowField Field, AgentWorld World) MakeOpenWorld(
            int size, int goalX, int goalY, SimConfig? config = null)
        {
            var map = new GridMap(size, size);
            var field = new FlowField(map);
            field.Compute(goalX, goalY);
            var world = new AgentWorld(map, field, config ?? new SimConfig());
            return (map, field, world);
        }

        [Fact]
        public void Agents_CrossAnOpenGrid_AndArrive()
        {
            var (_, _, world) = MakeOpenWorld(20, 18, 10);
            for (int i = 0; i < 10; i++)
                world.Spawn(new Vec2(1.5f, 8.5f + i * 0.3f), health: 10f);

            // 17 world units at 3 u/s ≈ 5.7s; give it 12s of ticks for separation wiggle.
            for (int step = 0; step < (int)(12f / Dt); step++)
                world.Step(Dt);

            Assert.Equal(10, world.ReachedCount);
            Assert.Equal(0, world.AliveCount);
        }

        [Fact]
        public void Agents_NavigateAMaze_WithoutEnteringWalls()
        {
            // Serpentine: two walls forcing an S-path.
            var map = new GridMap(15, 15);
            for (int y = 0; y < 12; y++) map.SetBlocked(5, y, true);   // wall from bottom, gap at top
            for (int y = 3; y < 15; y++) map.SetBlocked(10, y, true);  // wall from top, gap at bottom

            var field = new FlowField(map);
            field.Compute(14, 7);
            var world = new AgentWorld(map, field, new SimConfig());

            var ids = new int[5];
            for (int i = 0; i < 5; i++)
                ids[i] = world.Spawn(new Vec2(0.5f, 7.5f + i * 0.2f), health: 10f);

            for (int step = 0; step < (int)(40f / Dt); step++)
            {
                world.Step(Dt);
                foreach (int id in ids)
                {
                    if (!world.IsAlive(id)) continue;
                    var pos = world.PositionOf(id);
                    var (cx, cy) = map.WorldToCell(pos);
                    Assert.False(map.IsBlocked(cx, cy),
                        $"Agent {id} is inside wall cell ({cx},{cy}) at {pos}.");
                }
            }

            Assert.Equal(5, world.ReachedCount);
        }

        [Fact]
        public void RadialDamage_KillsOnlyAgentsInRadius_AndCountsKills()
        {
            var (_, _, world) = MakeOpenWorld(20, 19, 19);
            int near1 = world.Spawn(new Vec2(5f, 5f), health: 10f);
            int near2 = world.Spawn(new Vec2(5.5f, 5f), health: 10f);
            int far = world.Spawn(new Vec2(12f, 12f), health: 10f);

            int kills = world.ApplyRadialDamage(new Vec2(5f, 5f), radius: 2f, damage: 25f);

            Assert.Equal(2, kills);
            Assert.Equal(2, world.TotalKills);
            Assert.False(world.IsAlive(near1));
            Assert.False(world.IsAlive(near2));
            Assert.True(world.IsAlive(far));
            Assert.Equal(1, world.AliveCount);
        }

        [Fact]
        public void RadialDamage_WoundsWithoutKilling_WhenDamageIsLow()
        {
            var (_, _, world) = MakeOpenWorld(10, 9, 9);
            int id = world.Spawn(new Vec2(3f, 3f), health: 10f);

            int kills = world.ApplyRadialDamage(new Vec2(3f, 3f), radius: 1f, damage: 4f);

            Assert.Equal(0, kills);
            Assert.True(world.IsAlive(id));
            Assert.Equal(6f, world.HealthOf(id), precision: 3);
        }

        [Fact]
        public void Separation_KeepsStackedAgentsApart()
        {
            var config = new SimConfig { MoveSpeed = 0f, SeparationWeight = 1f }; // isolate separation
            var (_, _, world) = MakeOpenWorld(10, 9, 9, config);
            int a = world.Spawn(new Vec2(5f, 5f), health: 10f);
            int b = world.Spawn(new Vec2(5f, 5f), health: 10f); // perfectly stacked

            // MoveSpeed 0 means flow contributes nothing, but separation is normalized
            // into 'desired' scaled by MoveSpeed — so with speed 0 nothing moves.
            // Use a tiny speed instead to let separation act.
            config.MoveSpeed = 0.5f;
            for (int step = 0; step < 60; step++)
                world.Step(Dt);

            float dist = (world.PositionOf(a) - world.PositionOf(b)).Length;
            Assert.True(dist > 0.05f, $"Stacked agents should separate; distance is {dist}.");
        }

        [Fact]
        public void Determinism_IdenticalRuns_ProduceIdenticalStateHashes()
        {
            static ulong Run()
            {
                var map = new GridMap(20, 20);
                for (int y = 5; y < 15; y++) map.SetBlocked(10, y, true);
                var field = new FlowField(map);
                field.Compute(18, 18);
                var world = new AgentWorld(map, field, new SimConfig());

                for (int i = 0; i < 50; i++)
                    world.Spawn(new Vec2(1f + (i % 5) * 0.4f, 1f + (i / 5) * 0.4f), health: 10f);

                for (int step = 0; step < 300; step++)
                {
                    world.Step(Dt);
                    if (step == 150)
                        world.ApplyRadialDamage(new Vec2(10f, 3f), radius: 3f, damage: 6f);
                }

                return world.StateHash();
            }

            Assert.Equal(Run(), Run());
        }

        [Fact]
        public void StateHash_ChangesWhenOutcomesChange()
        {
            var (_, _, worldA) = MakeOpenWorld(10, 9, 9);
            var (_, _, worldB) = MakeOpenWorld(10, 9, 9);
            worldA.Spawn(new Vec2(2f, 2f), health: 10f);
            worldB.Spawn(new Vec2(2f, 2.5f), health: 10f); // different spawn

            worldA.Step(Dt);
            worldB.Step(Dt);

            Assert.NotEqual(worldA.StateHash(), worldB.StateHash());
        }
    }
}
