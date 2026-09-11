using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>
    /// ADR-008 second pass: the implant is decrypted PROGRESSIVELY and nothing is dead before its
    /// bar is empty.
    ///
    /// The first model broke the chip the moment integrity hit zero and then guaranteed death 2.5
    /// seconds later, which the owner beat in one sitting: "just tagging them and being able to run
    /// in circles is an easy way to beat the game." These tests are the shape of that exploit,
    /// written down so it cannot come back.
    /// </summary>
    public sealed class FailingChipTests
    {
        private const float Health = 40f;

        private static AgentWorld World(float soloSeconds = 40f)
        {
            var map = new GridMap(48, 48);
            var field = new FlowField(map);
            field.Compute(46, 24);
            return new AgentWorld(map, field, new SimConfig { SoloDecryptSeconds = soloSeconds });
        }

        /// <summary>
        /// A world whose subject cannot walk out of frame. Anything that takes tens of seconds to
        /// resolve needs this: an unpenned agent crosses a 48-cell map in fourteen seconds, reaches
        /// the goal, leaves the simulation, and the test then measures how fast it WALKS rather
        /// than how fast it dies. Rock, because Rock is solid and ignores damage.
        /// </summary>
        private static (AgentWorld world, int id) Penned(float soloSeconds = 40f, float health = Health)
        {
            var map = new GridMap(48, 48);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0) map.SetWall(6 + dx, 24 + dy, WallKind.Rock, 1);

            var field = new FlowField(map);
            field.Compute(46, 24);
            var world = new AgentWorld(map, field, new SimConfig { SoloDecryptSeconds = soloSeconds });
            return (world, world.Spawn(GridMap.CellCenter(6, 24), health));
        }

        // ---------------------------------------------------------------- the exploit

        [Fact]
        public void OneHitDoesNotKillAnythingInAHurry()
        {
            var (world, id) = Penned();

            Assert.False(world.ApplyDamage(id, 1f), "one pulse is not a kill");

            // Ten seconds of running in circles. Under the old model this one was already gone.
            for (int i = 0; i < 200; i++) world.Step(0.05f);

            Assert.True(world.IsAlive(id), "tagging and kiting must not clear a wave");
            Assert.True(world.Integrity01(id) > 0.6f, "and it is still most of the way up the bar");
        }

        [Fact]
        public void OneHitIsStillAlwaysFatalEventually()
        {
            // The fiction is absolute: the payload lands, the implant is finished. The forty
            // seconds is the punishment for not finishing the job, not a reprieve.
            var (world, id) = Penned(soloSeconds: 40f);
            world.ApplyDamage(id, 1f);

            for (int i = 0; i < 1200 && world.IsAlive(id); i++) world.Step(0.05f);

            Assert.False(world.IsAlive(id));
            Assert.Equal(1, world.TotalKills);
        }

        [Fact]
        public void MorePulsesDecryptFaster()
        {
            static float SecondsToFall(int pulses)
            {
                var (world, id) = Penned();
                for (int p = 0; p < pulses; p++) world.ApplyDamage(id, 0.01f);

                float t = 0f;
                while (world.IsAlive(id) && t < 200f) { world.Step(0.05f); t += 0.05f; }
                return t;
            }

            float one = SecondsToFall(1);
            float six = SecondsToFall(6);

            Assert.True(one > 30f, $"one pulse should take most of a minute, took {one:F1}s");
            Assert.True(six < one / 4f, $"six pulses should be far faster: {six:F1}s against {one:F1}s");
        }

        // ---------------------------------------------------------------- the bar

        [Fact]
        public void TheBarIsTheHealthTheHudDraws()
        {
            var world = World();
            int id = world.Spawn(new Vec2(4.5f, 24.5f), Health);
            Assert.Equal(1f, world.Integrity01(id), 3);

            world.ApplyDamage(id, Health * 0.5f);
            Assert.Equal(0.5f, world.Integrity01(id), 2);
        }

        [Fact]
        public void EmptyingTheBarKillsAndPaysOnce()
        {
            var world = World();
            int id = world.Spawn(new Vec2(4.5f, 24.5f), Health);

            Assert.True(world.ApplyDamage(id, Health), "the pulse that empties the bar is the kill");
            Assert.False(world.IsAlive(id));
            Assert.Equal(1, world.TotalKills);
            Assert.Equal(0, world.AliveCount);

            Assert.False(world.ApplyDamage(id, Health), "no second payment for shooting the ground");
            Assert.Equal(1, world.TotalKills);
        }

        [Fact]
        public void ABodyThatRunsOutOnItsOwnIsStillPaidFor()
        {
            // Withholding the payment would punish spraying and lie to the kill tally. The forty
            // seconds is the punishment; the player did kill that one.
            var (world, id) = Penned(soloSeconds: 2f);
            world.ApplyDamage(id, 1f);

            for (int i = 0; i < 200 && world.IsAlive(id); i++) world.Step(0.05f);

            Assert.False(world.IsAlive(id));
            Assert.Equal(1, world.TotalKills);
            Assert.Equal(0, world.FailingCount);
        }

        // ---------------------------------------------------------------- frailty

        [Fact]
        public void BeingHitDoesNotMakeAnyoneFrailWhileTheBarIsStillHigh()
        {
            // The owner's first sentence: "just because a zombie gets hit doesn't inherently mean
            // they slow down all the way to death."
            var world = World();
            int id = world.Spawn(new Vec2(4.5f, 24.5f), Health);
            world.ApplyDamage(id, Health * 0.2f);

            Assert.Equal(1f, world.SpeedScale(id), 2);
            Assert.Equal(1f, world.ThreatScale(id), 2);
        }

        [Fact]
        public void TheyComeApartOnlyAtTheBottomOfTheBar()
        {
            var world = World();
            int id = world.Spawn(new Vec2(4.5f, 24.5f), Health);
            world.ApplyDamage(id, Health * 0.95f);

            Assert.True(world.SpeedScale(id) < 0.6f, "nearly out: stumbling");
            Assert.True(world.ThreatScale(id) < 0.6f, "and barely swinging");
            Assert.True(world.SpeedScale(id) > 0f, "but never frozen");
        }

        [Fact]
        public void AFailingBodyStillMovesButSlower()
        {
            var world = World();
            int healthy = world.Spawn(new Vec2(4.5f, 20.5f), Health);
            int nearlyOut = world.Spawn(new Vec2(4.5f, 28.5f), Health);
            world.ApplyDamage(nearlyOut, Health * 0.95f);

            float hStart = world.PositionOf(healthy).X;
            float nStart = world.PositionOf(nearlyOut).X;
            for (int i = 0; i < 10; i++) world.Step(0.05f);

            float moved = world.PositionOf(nearlyOut).X - nStart;
            Assert.True(moved > 0.01f, "it keeps coming -- that is the whole point");
            Assert.True(moved < world.PositionOf(healthy).X - hStart, "but it is stumbling");
        }

        // ---------------------------------------------------------------- guns

        [Fact]
        public void AGunIgnoresOnlyWhatIsAboutToFallOver()
        {
            // Under the old model a gun skipped anything infected. With almost everyone infected
            // now, that rule would make the entire crowd invisible to every turret in the game.
            var world = World();
            int tagged = world.Spawn(new Vec2(12.5f, 24.5f), Health);
            int untouched = world.Spawn(new Vec2(11.5f, 24.5f), Health);
            world.ApplyDamage(tagged, 1f);

            Assert.Equal(tagged, world.FindFirstInRangeVisible(new Vec2(12.5f, 24.5f), 6f));

            // Nearly out: now it is somebody else's problem.
            world.ApplyDamage(tagged, Health - 2f);
            Assert.Equal(untouched, world.FindFirstInRangeVisible(new Vec2(12.5f, 24.5f), 6f));
        }

        [Fact]
        public void AreaFirePaysForEachBodyItEmptiesAndNoMore()
        {
            var world = World();
            for (int i = 0; i < 4; i++) world.Spawn(new Vec2(10.5f, 22.5f + i * 0.3f), 5f);

            Assert.Equal(4, world.ApplyRadialDamage(new Vec2(10.5f, 23f), 3f, 5f));
            Assert.Equal(4, world.TotalKills);

            Assert.Equal(0, world.ApplyRadialDamage(new Vec2(10.5f, 23f), 3f, 5f));
            Assert.Equal(4, world.TotalKills);
        }

        [Fact]
        public void BreaksAreCountedPerArchetypeAtTheMomentTheyHappen()
        {
            var world = World();
            int sapper = world.SpawnArchetype(new Vec2(6.5f, 24.5f), Archetype.Sapper);
            world.SpawnArchetype(new Vec2(8.5f, 24.5f), Archetype.Spitter);

            world.ApplyDamage(sapper, 9999f);
            Assert.Equal(1, world.BrokenOf(Archetype.Sapper));
            Assert.Equal(0, world.BrokenOf(Archetype.Spitter));
        }

        // ---------------------------------------------------------------- determinism

        [Fact]
        public void TheStateHashSeesTheDecrypt()
        {
            var a = World();
            var b = World();
            a.Spawn(new Vec2(6.5f, 24.5f), Health);
            b.Spawn(new Vec2(6.5f, 24.5f), Health);
            Assert.Equal(a.StateHash(), b.StateHash());

            a.ApplyDamage(0, 1f);
            Assert.NotEqual(a.StateHash(), b.StateHash());
        }

        [Fact]
        public void TheWholeThingStaysDeterministic()
        {
            static ulong Run()
            {
                var world = World();
                for (int i = 0; i < 30; i++) world.Spawn(new Vec2(4.5f + i * 0.2f, 14.5f + i * 0.3f), Health);
                for (int step = 0; step < 120; step++)
                {
                    if (step % 7 == 0) world.ApplyRadialDamage(new Vec2(8f, 24f), 4f, 3f);
                    world.Step(0.05f);
                }
                return world.StateHash();
            }

            Assert.Equal(Run(), Run());
        }
    }
}
