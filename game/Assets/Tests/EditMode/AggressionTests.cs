#nullable enable
using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The signed go for whatever is in front of them.
    ///
    /// Owner, after playing: "if I'm close to him then they chase and try to kill me and if they run
    /// by a turret they try to kill the turret". Before this, an errand was picked once at spawn and
    /// never revisited, so a body walked past the player's boots and past a gun shooting it.
    /// </summary>
    public sealed class AggressionTests
    {
        private static (GridMap map, AgentWorld world) Field(SimConfig? cfg = null)
        {
            var map = new GridMap(48, 48);
            var flow = new FlowField(map);
            flow.Compute(46, 24);
            return (map, new AgentWorld(map, flow, cfg ?? new SimConfig(), initialCapacity: 64));
        }

        [Test]
        public void ABodyNearThePlayerComesForThePlayer()
        {
            var (_, world) = Field();
            // Spawned heading for the vault at (46,24), i.e. east. The player is WEST of it, so a
            // body that turns around is unambiguously chasing rather than drifting.
            int id = world.Spawn(new Vec2(20.5f, 24.5f), 100f, Intent.Vault);
            world.SetHero(new Vec2(15.5f, 24.5f), alive: true);

            float before = world.PositionOf(id).X;
            for (int i = 0; i < 10; i++) world.Step(1f / 30f);

            Assert.That(world.PositionOf(id).X, Is.LessThan(before),
                        "a body within aggro range should turn around and come at the player");
            Assert.That(world.ChasingCount, Is.GreaterThan(0));
        }

        [Test]
        public void ABodyFarFromThePlayerKeepsWalkingToTheVault()
        {
            var (_, world) = Field();
            int id = world.Spawn(new Vec2(10.5f, 24.5f), 100f, Intent.Vault);
            world.SetHero(new Vec2(40.5f, 24.5f), alive: true);   // way outside the range

            float before = world.PositionOf(id).X;
            for (int i = 0; i < 10; i++) world.Step(1f / 30f);

            Assert.That(world.PositionOf(id).X, Is.GreaterThan(before), "still heading for the vault");
        }

        [Test]
        public void NothingChasesADownedPlayer()
        {
            var (_, world) = Field();
            int id = world.Spawn(new Vec2(20.5f, 24.5f), 100f, Intent.Vault);
            world.SetHero(new Vec2(15.5f, 24.5f), alive: false);

            float before = world.PositionOf(id).X;
            for (int i = 0; i < 10; i++) world.Step(1f / 30f);

            Assert.That(world.PositionOf(id).X, Is.GreaterThan(before));
            Assert.That(world.ChasingCount, Is.Zero);
        }

        [Test]
        public void InterestOutlastsSteppingOutOfRange()
        {
            // A crowd that drops you the instant you cross a radius reads as a light switch.
            var cfg = new SimConfig { AggroMemory = 2.5f };
            var (_, world) = Field(cfg);
            int id = world.Spawn(new Vec2(20.5f, 24.5f), 100f, Intent.Vault);

            world.SetHero(new Vec2(15.5f, 24.5f), alive: true);
            for (int i = 0; i < 5; i++) world.Step(1f / 30f);

            // Now just outside the base range, but inside the sticky one.
            world.SetHero(new Vec2(20.5f - cfg.HeroAggroRange - 1.5f, 24.5f), alive: true);
            float before = world.PositionOf(id).X;
            for (int i = 0; i < 5; i++) world.Step(1f / 30f);

            Assert.That(world.PositionOf(id).X, Is.LessThan(before), "should still be following");
        }

        [Test]
        public void ABodyThatWalksPastAGunClawsAtIt()
        {
            var (map, world) = Field();
            var turrets = new TurretSystem();
            turrets.Place(map, 22, 24);
            world.Structures = turrets.AsStructureQuery();

            // Spawned for the vault, passing right by the gun. No hero anywhere.
            world.Spawn(new Vec2(20.5f, 24.5f), 100f, Intent.Vault);
            world.SetHero(new Vec2(0f, 0f), alive: false);

            var events = new List<SimEvent>();
            bool mauled = false;
            for (int i = 0; i < 90 && !mauled; i++)
            {
                world.Step(1f / 30f);
                world.DrainEvents(events);
                foreach (var e in events) if (e.Kind == SimEventKind.StructureMauled) mauled = true;
                events.Clear();
            }

            Assert.That(mauled, Is.True, "a gun it nearly walks into should get clawed at");
        }

        [Test]
        public void AGunAcrossTheFieldIsIgnoredByAnOrdinaryBody()
        {
            // Only the dedicated hunters cross a field for a gun. Everyone else engages what is in
            // arm's reach, or the distinction between the two intents means nothing.
            var (map, world) = Field();
            var turrets = new TurretSystem();
            turrets.Place(map, 40, 8);
            world.Structures = turrets.AsStructureQuery();

            int id = world.Spawn(new Vec2(10.5f, 24.5f), 100f, Intent.Vault);
            world.SetHero(new Vec2(0f, 0f), alive: false);

            for (int i = 0; i < 30; i++) world.Step(1f / 30f);

            // Heading east along its lane, not north-east at the gun.
            Assert.That(world.PositionOf(id).Y, Is.EqualTo(24.5f).Within(1.5f));
        }

        [Test]
        public void ThePlayerOutranksTheGun()
        {
            var (map, world) = Field();
            var turrets = new TurretSystem();
            turrets.Place(map, 21, 24);
            world.Structures = turrets.AsStructureQuery();

            int id = world.Spawn(new Vec2(20.5f, 24.5f), 100f, Intent.Vault);
            world.SetHero(new Vec2(20.5f, 30.5f), alive: true);   // north, gun is east

            for (int i = 0; i < 12; i++) world.Step(1f / 30f);

            Assert.That(world.PositionOf(id).Y, Is.GreaterThan(24.5f),
                        "they are here for people; a gun in the way is second");
        }

        [Test]
        public void TheSwarmStaysDeterministicWithAHeroInIt()
        {
            // The whole sim rests on this, and aggression adds a new input to every tick.
            ulong Run()
            {
                var (map, world) = Field();
                var turrets = new TurretSystem();
                turrets.Place(map, 24, 24);
                world.Structures = turrets.AsStructureQuery();

                for (int i = 0; i < 20; i++)
                    world.Spawn(new Vec2(6.5f + i * 0.3f, 20.5f + (i % 5)), 100f,
                                i % 7 == 0 ? Intent.HuntStructure : Intent.Vault);

                for (int t = 0; t < 120; t++)
                {
                    world.SetHero(new Vec2(18f + t * 0.02f, 24.5f), alive: true);
                    world.Step(1f / 30f);
                }
                return world.StateHash();
            }

            Assert.That(Run(), Is.EqualTo(Run()));
        }
    }
}
