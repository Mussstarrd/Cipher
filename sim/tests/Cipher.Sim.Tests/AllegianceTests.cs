using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>
    /// ADR-010, the turncoat drone: the first mechanic in this simulation where an agent fights
    /// another agent.
    ///
    /// Everything before it was crowd-versus-structure or crowd-versus-hero, so every targeting
    /// query in the codebase quietly assumed one side. These tests exist because that assumption
    /// was load-bearing in about ten separate places, and a filter added to nine of them is worse
    /// than a filter added to none: the one that was missed becomes a gun that shoots the player's
    /// own machine, and it looks like a rendering bug.
    /// </summary>
    public sealed class AllegianceTests
    {
        private const float Health = 40f;

        /// <summary>
        /// A world whose bodies cannot walk out of frame. An unpenned agent crosses a 48-cell map
        /// in fourteen seconds and leaves the simulation, so anything measured over tens of seconds
        /// has to be boxed in or the test measures walking instead of the thing it names.
        /// </summary>
        private static AgentWorld Penned(SimConfig? config = null)
        {
            var map = new GridMap(48, 48);
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    if (dx * dx + dy * dy >= 4) map.SetWall(8 + dx, 24 + dy, WallKind.Rock, 1);

            var field = new FlowField(map);
            field.Compute(46, 24);
            return new AgentWorld(map, field, config ?? new SimConfig());
        }

        /// <summary>Spawns until it finds a body the hash calls a machine. Returns -1 if none.</summary>
        private static int SpawnMachine(AgentWorld world, Vec2 at, float health = Health)
        {
            for (int attempt = 0; attempt < 200; attempt++)
            {
                int id = world.Spawn(at, health);
                if (world.IsMachine(id)) return id;
            }
            return -1;
        }

        // ---------------------------------------------------------------- who is what

        [Fact]
        public void RoughlyTheOwnersShareOfTheCrowdIsMachines()
        {
            var world = Penned();
            world.MachineSeed = 20260910;

            int machines = 0;
            const int sample = 2000;
            for (int i = 0; i < sample; i++)
                if (world.IsMachine(world.Spawn(GridMap.CellCenter(8, 24), Health))) machines++;

            // The owner asked for "like 30%". A hash is not a quota, so this is a band, not a value.
            double share = machines / (double)sample;
            Assert.InRange(share, 0.26, 0.34);
        }

        [Fact]
        public void MachinesAreNotHandedOutInRuns()
        {
            // The whole reason for a proper finaliser rather than a cheap mix. If consecutive ids
            // correlate, a wave arrives as a block of robots followed by a block of people, which
            // reads as a bug even though the total share is right.
            var world = Penned();
            world.MachineSeed = 7;

            int longestRun = 0, run = 0;
            bool previous = false;
            for (int i = 0; i < 600; i++)
            {
                bool machine = world.IsMachine(world.Spawn(GridMap.CellCenter(8, 24), Health));
                run = (i > 0 && machine == previous) ? run + 1 : 1;
                if (machine && run > longestRun) longestRun = run;
                previous = machine;
            }

            Assert.True(longestRun < 12, $"machines arrived in a run of {longestRun}");
        }

        [Fact]
        public void TheSameSeedGivesTheSameCrowdEveryTime()
        {
            var a = Penned();
            var b = Penned();
            a.MachineSeed = b.MachineSeed = 424242;

            for (int i = 0; i < 300; i++)
            {
                int ia = a.Spawn(GridMap.CellCenter(8, 24), Health);
                int ib = b.Spawn(GridMap.CellCenter(8, 24), Health);
                Assert.Equal(a.IsMachine(ia), b.IsMachine(ib));
            }
        }

        [Fact]
        public void ADifferentSeedGivesADifferentCrowd()
        {
            var a = Penned();
            var b = Penned();
            a.MachineSeed = 1;
            b.MachineSeed = 2;

            int differences = 0;
            for (int i = 0; i < 300; i++)
            {
                int ia = a.Spawn(GridMap.CellCenter(8, 24), Health);
                int ib = b.Spawn(GridMap.CellCenter(8, 24), Health);
                if (a.IsMachine(ia) != b.IsMachine(ib)) differences++;
            }

            Assert.True(differences > 30, "two positions should not send the same crowd");
        }

        // ---------------------------------------------------------------- what may be turned

        [Fact]
        public void APersonCanNeverBeTurned()
        {
            // ADR-010's central limit. Hacking the implant in a neighbour's head is a different and
            // much darker game; the drone only takes back machines.
            var world = Penned();
            world.MachineSeed = 99;

            int people = 0;
            for (int i = 0; i < 400; i++)
            {
                int id = world.Spawn(GridMap.CellCenter(8, 24), Health);
                if (world.IsMachine(id)) continue;
                people++;
                Assert.False(world.Turn(id), "a chipped person was converted");
                Assert.False(world.IsTurned(id));
            }

            Assert.True(people > 100, "sample never produced enough people to be worth asserting on");
        }

        [Fact]
        public void SappersAndCollectorsAreNeverMachines()
        {
            var world = Penned();
            for (int i = 0; i < 50; i++)
            {
                int sapper = world.SpawnArchetype(GridMap.CellCenter(8, 24), Archetype.Sapper);
                int collector = world.SpawnArchetype(GridMap.CellCenter(8, 24), Archetype.Collector);
                Assert.False(world.IsMachine(sapper));
                Assert.False(world.IsMachine(collector));
                Assert.False(world.Turn(sapper));
                Assert.False(world.Turn(collector));
            }
        }

        [Fact]
        public void TurningTwiceIsRefused()
        {
            var world = Penned();
            int id = SpawnMachine(world, GridMap.CellCenter(8, 24));
            Assert.True(id >= 0);

            Assert.True(world.Turn(id));
            Assert.False(world.Turn(id));
            Assert.Equal(1, world.TurnedCount);
        }

        // ---------------------------------------------------------------- the player's guns

        [Fact]
        public void NoPlayerSideQueryEverReturnsAConvert()
        {
            // THE POINT OF THE WHOLE FILE. A filter added to some queries and not others is how a
            // turret ends up shooting the player's own machine, so every query is asked here, in
            // one place, against a field containing exactly one body -- a convert.
            var world = Penned();
            int id = SpawnMachine(world, GridMap.CellCenter(8, 24));
            Assert.True(id >= 0);
            Assert.True(world.Turn(id));

            Vec2 at = world.PositionOf(id);
            Vec2 from = at - new Vec2(4f, 0f);

            Assert.Equal(-1, world.FindFirstInRange(at, 12f));
            Assert.Equal(-1, world.FindNearestInRange(at, 12f));
            Assert.Equal(-1, world.FindFirstInRangeVisible(from, 12f));
            Assert.False(world.Raycast(from, new Vec2(1f, 0f), 12f, 1f, out _, out _));
            Assert.Equal(0, world.CountWithin(at, 12f));
            Assert.Equal(0, world.CountWithinVisible(from, 12f));
            Assert.Equal(0f, world.ThreatWithin(at, 12f));
        }

        [Fact]
        public void AreaWeaponsDoNotHarmConverts()
        {
            var world = Penned();
            int id = SpawnMachine(world, GridMap.CellCenter(8, 24));
            Assert.True(id >= 0);
            Assert.True(world.Turn(id));

            float before = world.HealthOf(id);
            Assert.Equal(0, world.ApplyRadialDamage(world.PositionOf(id), 10f, 500f));
            Assert.Equal(0, world.ApplyRadialDamageVisible(world.PositionOf(id) - new Vec2(3f, 0f), 10f, 500f));

            Assert.True(world.IsAlive(id), "the EMP killed the player's own machine");
            Assert.Equal(before, world.HealthOf(id));
        }

        [Fact]
        public void AConvertStillBlocksNothingAndOccupiesItsCell()
        {
            // Placement safety is NOT a targeting question. A convert standing on a cell would be
            // entombed by a wall dropped on it exactly like anyone else, so this query is the one
            // that must NOT learn about allegiance.
            var world = Penned();
            int id = SpawnMachine(world, GridMap.CellCenter(8, 24));
            Assert.True(id >= 0);
            Assert.True(world.Turn(id));

            Assert.True(world.IsCellOccupied(8, 24));
        }

        // ---------------------------------------------------------------- what a convert does

        [Fact]
        public void AConvertAttacksTheNearestBodyStillWorkingForHalcyon()
        {
            var world = Penned();
            int convert = SpawnMachine(world, GridMap.CellCenter(8, 24));
            Assert.True(convert >= 0);
            Assert.True(world.Turn(convert));

            int prey = world.Spawn(GridMap.CellCenter(8, 25), Health);
            float before = world.HealthOf(prey);

            for (int i = 0; i < 120; i++) world.Step(0.05f);

            Assert.True(world.HealthOf(prey) < before || !world.IsAlive(prey),
                "the convert never touched the body standing next to it");
        }

        [Fact]
        public void AConvertWithNothingToFightHoldsWhereItStands()
        {
            // Falling through to the crowd's movement would walk the player's own machine into the
            // vault, which is the one place it must never go.
            var world = Penned();
            int convert = SpawnMachine(world, GridMap.CellCenter(8, 24));
            Assert.True(convert >= 0);
            Vec2 start = world.PositionOf(convert);
            Assert.True(world.Turn(convert));

            for (int i = 0; i < 200; i++) world.Step(0.05f);

            if (world.IsAlive(convert))
                Assert.True(Vec2.DistanceSquared(start, world.PositionOf(convert)) < 1f,
                    "an idle convert wandered off");
            Assert.Equal(0, world.ReachedCount);
        }

        [Fact]
        public void AConvertBurnsOutOnItsOwn()
        {
            // It is not a permanent unit. The network takes it back, and that is what stops the
            // drone snowballing into an army.
            var config = new SimConfig { TurnedSeconds = 5f };
            var world = Penned(config);
            int convert = SpawnMachine(world, GridMap.CellCenter(8, 24));
            Assert.True(convert >= 0);
            Assert.True(world.Turn(convert));

            for (int i = 0; i < 40; i++) world.Step(0.05f);
            Assert.True(world.IsAlive(convert), "it burned out almost immediately");

            for (int i = 0; i < 160; i++) world.Step(0.05f);
            Assert.False(world.IsAlive(convert), "it never burned out");
            Assert.Equal(0, world.TurnedCount);
        }

        [Fact]
        public void ABurnedOutConvertIsNotAKillAndIsNotPaidFor()
        {
            // Without this the drone prints money: convert, wait, collect a bounty on your own
            // machine expiring, repeat.
            var config = new SimConfig { TurnedSeconds = 4f };
            var world = Penned(config);
            int convert = SpawnMachine(world, GridMap.CellCenter(8, 24));
            Assert.True(convert >= 0);

            long killsBefore = world.TotalKills;
            long brokenBefore = world.TotalBroken;
            Assert.True(world.Turn(convert));

            for (int i = 0; i < 200; i++) world.Step(0.05f);

            Assert.False(world.IsAlive(convert));
            Assert.Equal(killsBefore, world.TotalKills);
            Assert.Equal(brokenBefore, world.TotalBroken);
        }

        [Fact]
        public void AMachineAlreadyBeingShotIsWorthLessAsAConvert()
        {
            // The drain stacks, so converting early beats converting a body you have been working
            // on. That is a real decision at the moment of use, and it is the right way round.
            var fresh = Penned(new SimConfig { TurnedSeconds = 20f });
            int a = SpawnMachine(fresh, GridMap.CellCenter(8, 24));
            Assert.True(a >= 0);
            Assert.True(fresh.Turn(a));

            var shot = Penned(new SimConfig { TurnedSeconds = 20f });
            int b = SpawnMachine(shot, GridMap.CellCenter(8, 24));
            Assert.True(b >= 0);
            for (int i = 0; i < 6; i++) shot.ApplyDamage(b, 1f);
            Assert.True(shot.Turn(b));

            Assert.True(shot.SecondsToDie(b) < fresh.SecondsToDie(a),
                "a machine the player had been shooting lasted as long as a fresh one");
        }

        // ---------------------------------------------------------------- the match layer

        [Fact]
        public void HostileCountExcludesConvertsSoAWaveCanClear()
        {
            // MatchRules clears a wave on "nothing alive". If that reads the raw living count, the
            // player's own convert holds the wave open, the clear bonus is late, the pack-up window
            // is late, and nothing on screen explains why.
            var world = Penned();
            int convert = SpawnMachine(world, GridMap.CellCenter(8, 24));
            Assert.True(convert >= 0);

            Assert.Equal(world.AliveCount, world.HostileCount);
            Assert.True(world.Turn(convert));

            Assert.Equal(1, world.AliveCount);
            Assert.Equal(0, world.HostileCount);
        }

        // ---------------------------------------------------------------- determinism

        [Fact]
        public void ConversionKeepsTheWorldDeterministic()
        {
            // Hard rule 3. Allegiance is state, so it has to be in the hash, and two worlds fed the
            // same conversions have to stay identical tick for tick.
            static (AgentWorld world, int convert) Build()
            {
                var world = Penned(new SimConfig { TurnedSeconds = 12f });
                world.MachineSeed = 555;
                int convert = -1;
                for (int i = 0; i < 24; i++)
                {
                    int id = world.Spawn(GridMap.CellCenter(8 + (i % 3), 23 + (i % 3)), Health);
                    if (convert < 0 && world.IsMachine(id)) convert = id;
                }
                return (world, convert);
            }

            var (a, ca) = Build();
            var (b, cb) = Build();
            Assert.True(ca >= 0);
            Assert.Equal(ca, cb);

            Assert.Equal(a.Turn(ca), b.Turn(cb));
            for (int i = 0; i < 400; i++)
            {
                a.Step(0.05f);
                b.Step(0.05f);
            }

            Assert.Equal(a.StateHash(), b.StateHash());
        }

        [Fact]
        public void TurningChangesTheStateHash()
        {
            // A guard that does not see the field it guards is not a guard. This asserts allegiance
            // actually reaches StateHash, so a desync involving converts cannot pass unnoticed.
            var a = Penned();
            var b = Penned();
            a.MachineSeed = b.MachineSeed = 31337;

            int ia = SpawnMachine(a, GridMap.CellCenter(8, 24));
            int ib = SpawnMachine(b, GridMap.CellCenter(8, 24));
            Assert.True(ia >= 0 && ia == ib);
            Assert.Equal(a.StateHash(), b.StateHash());

            Assert.True(a.Turn(ia));
            Assert.NotEqual(a.StateHash(), b.StateHash());
        }
    }
}
