using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>
    /// ADR-011. A Collector is a boss because of three things, and "more health" is not one of
    /// them: it comes for the PLAYER rather than the objective, its hardened implant makes the
    /// passive decrypt useless, and it does not come apart until the very end.
    ///
    /// Each of those is a test here, because each of them is the kind of property a later tuning
    /// pass erodes without noticing it has changed the design.
    /// </summary>
    public sealed class CollectorTests
    {
        private sealed class Structures : IStructureQuery
        {
            private readonly List<Vec2> _at = new List<Vec2>();
            public void Add(Vec2 p) => _at.Add(p);
            public int Count => _at.Count;
            public Vec2 PositionAt(int index) => _at[index];
        }

        private static AgentWorld World(out GridMap map, SimConfig? config = null)
        {
            map = new GridMap(64, 64);
            var field = new FlowField(map);
            field.Compute(62, 32);
            return new AgentWorld(map, field, config ?? new SimConfig());
        }

        // ---------------------------------------------------------------- it comes for you

        [Fact]
        public void ItWalksAtTheHeroRatherThanTheObjective()
        {
            // The goal is east at x=62. The hero is WEST of the spawn, so a Collector that is
            // following the flow field like everything else will move the other way.
            var world = World(out _);
            world.SetHero(new Vec2(6f, 32f), alive: true);
            int id = world.SpawnArchetype(new Vec2(30f, 32f), Archetype.Collector);

            float startX = world.PositionOf(id).X;
            for (int i = 0; i < 40; i++)
            {
                world.SetHero(new Vec2(6f, 32f), alive: true);
                world.Step(0.05f);
            }

            Assert.True(world.PositionOf(id).X < startX - 0.5f,
                        "it was sent for him; the vault is not its problem");
        }

        [Fact]
        public void AnOrdinaryBodyStillWalksToTheObjective()
        {
            // The control. If this ever fails, the Collector branch has leaked onto the crowd.
            var world = World(out _);
            world.SetHero(new Vec2(6f, 32f), alive: true);
            int id = world.Spawn(new Vec2(30f, 32f), 10f);

            float startX = world.PositionOf(id).X;
            for (int i = 0; i < 40; i++)
            {
                world.SetHero(new Vec2(6f, 32f), alive: true);
                world.Step(0.05f);
            }

            Assert.True(world.PositionOf(id).X > startX, "the crowd is still going for the truck");
        }

        [Fact]
        public void WithNoHeroToHuntItFallsBackToTheFieldRatherThanFreezing()
        {
            var world = World(out _);
            world.SetHero(new Vec2(6f, 32f), alive: false);
            int id = world.SpawnArchetype(new Vec2(30f, 32f), Archetype.Collector);

            float startX = world.PositionOf(id).X;
            for (int i = 0; i < 40; i++) world.Step(0.05f);

            Assert.True(world.PositionOf(id).X > startX,
                        "a downed player must not leave it standing in the road");
        }

        [Fact]
        public void ItGoesThroughAnEmplacementInItsWayRatherThanAroundIt()
        {
            var world = World(out _);
            var structures = new Structures();
            structures.Add(new Vec2(29f, 32f));      // directly between it and the hero
            world.Structures = structures;
            world.SetHero(new Vec2(6f, 32f), alive: true);
            world.SpawnArchetype(new Vec2(30f, 32f), Archetype.Collector);

            var events = new List<SimEvent>();
            bool mauled = false;
            for (int i = 0; i < 80 && !mauled; i++)
            {
                world.SetHero(new Vec2(6f, 32f), alive: true);
                world.Step(0.05f);
                events.Clear();
                world.DrainEvents(events);
                foreach (var e in events)
                    if (e.Kind == SimEventKind.StructureMauled) mauled = true;
            }

            Assert.True(mauled, "a gun in the way is something to go through");
        }

        // ---------------------------------------------------------------- hardened

        [Fact]
        public void TheDecryptBarelyTakesHold()
        {
            // The whole point: a citizen tagged once dies in forty seconds. A Collector tagged once
            // is a Collector with a scratch on it, so every point has to come off by direct fire.
            var cfg = new SimConfig { SoloDecryptSeconds = 40f };
            var world = World(out _, cfg);
            int collector = world.SpawnArchetype(new Vec2(30f, 32f), Archetype.Collector);
            int citizen = world.Spawn(new Vec2(31f, 32f), cfg.CollectorHealth);

            world.ApplyDamage(collector, 1f);
            world.ApplyDamage(citizen, 1f);

            float hardened = world.SecondsToDie(collector);
            float soft = world.SecondsToDie(citizen);

            Assert.True(hardened > soft * 4f,
                        $"lab hardware should resist: {hardened:F0}s against {soft:F0}s");
        }

        [Fact]
        public void TheArsenalStillWorksOnIt
            ()
        {
            // Resistance, not immunity. A Collector the player never stops shooting still dies, or
            // the weapon the entire game is built around has an exception the player cannot beat.
            var world = World(out _);
            int id = world.SpawnArchetype(new Vec2(30f, 32f), Archetype.Collector);

            Assert.True(world.ApplyDamage(id, 1e6f), "enough direct fire kills it");
            Assert.False(world.IsAlive(id));
            Assert.Equal(1, world.TotalKills);
        }

        [Fact]
        public void ItDoesNotComeApartUntilTheVeryEnd()
        {
            var cfg = new SimConfig();
            var world = World(out _, cfg);
            int id = world.SpawnArchetype(new Vec2(30f, 32f), Archetype.Collector);

            // A fifth of the bar left. An ordinary body is well into its frailty band here.
            world.ApplyDamage(id, cfg.CollectorHealth * 0.8f);
            Assert.Equal(1f, world.SpeedScale(id), 2);
            Assert.Equal(1f, world.ThreatScale(id), 2);

            world.ApplyDamage(id, cfg.CollectorHealth * 0.17f);
            Assert.True(world.SpeedScale(id) < 1f, "it does give out eventually");
        }

        // ---------------------------------------------------------------- accounting

        [Fact]
        public void ItIsCountedAsItsOwnArchetypeWhenItFalls()
        {
            var world = World(out _);
            int id = world.SpawnArchetype(new Vec2(30f, 32f), Archetype.Collector);
            world.ApplyDamage(id, 1e6f);

            Assert.Equal(1, world.BrokenOf(Archetype.Collector));
            Assert.Equal(0, world.BrokenOf(Archetype.Sapper));
            Assert.Equal(0, world.BrokenOf(Archetype.Spitter));
        }

        [Fact]
        public void TheWorldStaysDeterministicWithOneOnTheField()
        {
            static ulong Run()
            {
                var map = new GridMap(64, 64);
                var field = new FlowField(map);
                field.Compute(62, 32);
                var world = new AgentWorld(map, field, new SimConfig());
                world.SpawnArchetype(new Vec2(30f, 32f), Archetype.Collector);
                for (int i = 0; i < 20; i++) world.Spawn(new Vec2(28f + i * 0.3f, 30f), 20f);

                for (int step = 0; step < 120; step++)
                {
                    world.SetHero(new Vec2(6f, 32f), alive: true);
                    if (step % 11 == 0) world.ApplyRadialDamage(new Vec2(29f, 32f), 3f, 12f);
                    world.Step(0.05f);
                }
                return world.StateHash();
            }

            Assert.Equal(Run(), Run());
        }
    }
}
