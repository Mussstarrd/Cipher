using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>
    /// ADR-008: weapons break the implant, and a broken implant takes its host with it slowly.
    /// The window is the whole point — an agent that vanishes on its last point of integrity can
    /// never finish the swing it started, which is why turrets felt unbeatable and a crowd that
    /// reached the player did not frighten anyone.
    /// </summary>
    public sealed class FailingChipTests
    {
        private static AgentWorld World(float failSeconds = 2.5f)
        {
            var map = new GridMap(32, 32);
            var field = new FlowField(map);
            field.Compute(30, 16);
            return new AgentWorld(map, field, new SimConfig { FailSeconds = failSeconds });
        }

        [Fact]
        public void BreakingAChipDoesNotRemoveTheBody()
        {
            var world = World();
            int id = world.Spawn(new Vec2(4.5f, 16.5f), health: 10f);

            Assert.True(world.ApplyDamage(id, 10f), "the call that broke it reports so");
            Assert.True(world.IsAlive(id), "still on its feet");
            Assert.True(world.IsFailing(id));
            Assert.Equal(1, world.AliveCount);      // a wave is not clear while it is still coming
            Assert.Equal(1, world.TotalKills);      // but the player is paid now, not in 2.5s
        }

        [Fact]
        public void TheBodyDropsWhenTheWindowRunsOut()
        {
            var world = World(failSeconds: 1f);
            int id = world.Spawn(new Vec2(4.5f, 16.5f), health: 10f);
            world.ApplyDamage(id, 10f);

            for (int i = 0; i < 9; i++) world.Step(0.1f);
            Assert.True(world.IsAlive(id), "still up just before the window closes");

            world.Step(0.2f);
            Assert.False(world.IsAlive(id));
            Assert.Equal(0, world.AliveCount);
            Assert.Equal(0, world.FailingCount);
        }

        [Fact]
        public void ThreatDecaysAcrossTheWindow()
        {
            var world = World(failSeconds: 1f);
            int id = world.Spawn(new Vec2(4.5f, 16.5f), health: 10f);
            Assert.Equal(1f, world.ThreatScale(id), 3);

            world.ApplyDamage(id, 10f);
            world.Step(0.5f);
            Assert.InRange(world.ThreatScale(id), 0.44f, 0.56f);   // half the window, half the punch

            world.Step(0.4f);
            Assert.True(world.ThreatScale(id) < 0.2f);
        }

        [Fact]
        public void AFailingBodyStillMovesButSlower()
        {
            var world = World(failSeconds: 4f);
            int healthy = world.Spawn(new Vec2(4.5f, 12.5f), health: 10f);
            int failing = world.Spawn(new Vec2(4.5f, 20.5f), health: 10f);
            world.ApplyDamage(failing, 10f);

            float healthyStart = world.PositionOf(healthy).X;
            float failingStart = world.PositionOf(failing).X;
            for (int i = 0; i < 10; i++) world.Step(0.05f);

            float healthyMoved = world.PositionOf(healthy).X - healthyStart;
            float failingMoved = world.PositionOf(failing).X - failingStart;

            Assert.True(failingMoved > 0.01f, "it keeps coming — that is the whole point");
            Assert.True(failingMoved < healthyMoved, "but it is stumbling");
        }

        [Fact]
        public void AFailingBodyNeverStopsDead()
        {
            var world = World(failSeconds: 2f);
            int id = world.Spawn(new Vec2(4.5f, 16.5f), health: 10f);
            world.ApplyDamage(id, 10f);
            world.Step(1.9f);   // as close to gone as it gets

            float before = world.PositionOf(id).X;
            world.Step(0.05f);
            Assert.True(world.PositionOf(id).X > before,
                        "a body that freezes the instant its chip breaks reads as a bug");
        }

        [Fact]
        public void AChipCannotBeBrokenTwice()
        {
            var world = World();
            int id = world.Spawn(new Vec2(4.5f, 16.5f), health: 10f);

            Assert.True(world.ApplyDamage(id, 10f));
            Assert.False(world.ApplyDamage(id, 10f));   // no double pay for shooting a dying body
            Assert.Equal(1, world.TotalKills);
            Assert.Equal(1, world.TotalBroken);
        }

        [Fact]
        public void AreaFireBreaksEveryIntactChipAndPaysForEachOnce()
        {
            var world = World();
            for (int i = 0; i < 4; i++) world.Spawn(new Vec2(10.5f, 14.5f + i * 0.3f), health: 5f);

            Assert.Equal(4, world.ApplyRadialDamage(new Vec2(10.5f, 15f), 3f, 5f));
            Assert.Equal(4, world.TotalKills);

            // A second pulse over the same failing bodies pays nothing.
            Assert.Equal(0, world.ApplyRadialDamage(new Vec2(10.5f, 15f), 3f, 5f));
            Assert.Equal(4, world.TotalKills);
        }

        [Fact]
        public void TurretsDoNotWasteFireOnADyingBody()
        {
            var world = World();
            int near = world.Spawn(new Vec2(12.5f, 16.5f), health: 5f);
            int far = world.Spawn(new Vec2(11.5f, 16.5f), health: 5f);

            Assert.Equal(near, world.FindFirstInRangeVisible(new Vec2(12.5f, 16.5f), 6f));

            world.ApplyDamage(near, 5f);
            Assert.Equal(far, world.FindFirstInRangeVisible(new Vec2(12.5f, 16.5f), 6f));
        }

        [Fact]
        public void BreaksAreCountedPerArchetypeAtTheMomentTheyHappen()
        {
            var world = World();
            int sapper = world.SpawnArchetype(new Vec2(6.5f, 16.5f), Archetype.Sapper);
            world.SpawnArchetype(new Vec2(8.5f, 16.5f), Archetype.Spitter);

            world.ApplyDamage(sapper, 9999f);
            Assert.Equal(1, world.BrokenOf(Archetype.Sapper));   // now, not a window later
            Assert.Equal(0, world.BrokenOf(Archetype.Spitter));
        }

        [Fact]
        public void TheStateHashSeesTheFailureWindow()
        {
            var a = World();
            var b = World();
            a.Spawn(new Vec2(6.5f, 16.5f), 5f);
            b.Spawn(new Vec2(6.5f, 16.5f), 5f);
            Assert.Equal(a.StateHash(), b.StateHash());

            a.ApplyDamage(0, 5f);
            Assert.NotEqual(a.StateHash(), b.StateHash());
        }

        [Fact]
        public void TheWholeThingStaysDeterministic()
        {
            static ulong Run()
            {
                var world = World(failSeconds: 1.5f);
                for (int i = 0; i < 30; i++) world.Spawn(new Vec2(4.5f + i * 0.2f, 10.5f + i * 0.3f), 6f);
                for (int step = 0; step < 60; step++)
                {
                    if (step % 7 == 0) world.ApplyRadialDamage(new Vec2(8f, 16f), 4f, 3f);
                    world.Step(0.05f);
                }
                return world.StateHash();
            }

            Assert.Equal(Run(), Run());
        }
    }
}
