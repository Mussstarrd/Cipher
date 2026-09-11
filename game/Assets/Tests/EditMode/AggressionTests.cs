#nullable enable
using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using NUnit.Framework;
using UnityEngine;

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

    /// <summary>
    /// The texture of the crowd: speed, sidearms, and guns that can actually be lost.
    /// </summary>
    public sealed class CrowdTextureTests
    {
        private static (GridMap map, AgentWorld world) Field(SimConfig? cfg = null)
        {
            var map = new GridMap(48, 48);
            var flow = new FlowField(map);
            flow.Compute(46, 24);
            return (map, new AgentWorld(map, flow, cfg ?? new SimConfig(), initialCapacity: 64));
        }

        [Test]
        public void ASprinterOutrunsAWalker()
        {
            // "Some of the mobs need to Sprint some need to run some need to walk." One speed for
            // everybody is the loudest tell that a crowd is a particle system.
            var (_, world) = Field();
            int fast = world.Spawn(new Vec2(10.5f, 20.5f), 100f, Intent.Vault, pace: 1.75f, armed: false);
            int slow = world.Spawn(new Vec2(10.5f, 28.5f), 100f, Intent.Vault, pace: 0.55f, armed: false);
            world.SetHero(new Vec2(0f, 0f), alive: false);

            for (int i = 0; i < 60; i++) world.Step(1f / 30f);

            float fastGain = world.PositionOf(fast).X - 10.5f;
            float slowGain = world.PositionOf(slow).X - 10.5f;
            Assert.That(fastGain, Is.GreaterThan(slowGain * 2f),
                        "a sprinter should be well clear of a walker after two seconds");
        }

        [Test]
        public void PaceSurvivesTheChase()
        {
            var (_, world) = Field();
            int fast = world.Spawn(new Vec2(20.5f, 20.5f), 100f, Intent.Vault, pace: 1.75f, armed: false);
            int slow = world.Spawn(new Vec2(20.5f, 28.5f), 100f, Intent.Vault, pace: 0.55f, armed: false);
            world.SetHero(new Vec2(20.5f, 24.5f), alive: true);

            for (int i = 0; i < 20; i++) world.Step(1f / 30f);

            float fastLeft = Vec2.DistanceSquared(world.PositionOf(fast), new Vec2(20.5f, 24.5f));
            float slowLeft = Vec2.DistanceSquared(world.PositionOf(slow), new Vec2(20.5f, 24.5f));
            Assert.That(fastLeft, Is.LessThan(slowLeft), "the fast one reaches him first");
        }

        [Test]
        public void AnArmedBodyShootsRatherThanClosingAllTheWay()
        {
            var (_, world) = Field();
            int id = world.Spawn(new Vec2(24.5f, 24.5f), 100f, Intent.Vault, pace: 1f, armed: true);
            world.SetHero(new Vec2(15.5f, 24.5f), alive: true);   // 9 cells: inside pistol range

            var events = new List<SimEvent>();
            bool shot = false;
            for (int i = 0; i < 120 && !shot; i++)
            {
                world.Step(1f / 30f);
                world.DrainEvents(events);
                foreach (var e in events) if (e.Kind == SimEventKind.PistolShot) shot = true;
                events.Clear();
            }

            Assert.That(shot, Is.True, "a sidearm inside its range should be used");
            float dist = Mathf.Sqrt(Vec2.DistanceSquared(world.PositionOf(id), new Vec2(15.5f, 24.5f)));
            Assert.That(dist, Is.GreaterThan(3f), "it stands off rather than closing to contact");
        }

        [Test]
        public void NobodyShootsThroughAWall()
        {
            var (map, world) = Field();
            for (int y = 0; y < 48; y++) map.SetWall(20, y, WallKind.Wall, GridMap.DefaultWallHp);

            world.Spawn(new Vec2(24.5f, 24.5f), 100f, Intent.Vault, pace: 1f, armed: true);
            world.SetHero(new Vec2(15.5f, 24.5f), alive: true);

            var events = new List<SimEvent>();
            for (int i = 0; i < 60; i++)
            {
                world.Step(1f / 30f);
                world.DrainEvents(events);
                foreach (var e in events)
                    Assert.That(e.Kind, Is.Not.EqualTo(SimEventKind.PistolShot),
                                "the same rule as the turrets: no shooting through cover");
                events.Clear();
            }
        }

        [Test]
        public void ACrowdCanPullDownALoneTurret()
        {
            // "a single turret should be able to be overrun if I don't intervene come on."
            var (map, world) = Field();
            var turrets = new TurretSystem();
            turrets.Place(map, 24, 24);
            world.Structures = turrets.AsStructureQuery();
            world.SetHero(new Vec2(0f, 0f), alive: false);

            for (int i = 0; i < 24; i++)
                world.Spawn(new Vec2(22.0f + (i % 4) * 0.4f, 23.0f + (i / 4) * 0.5f), 400f,
                            Intent.Vault, pace: 1f, armed: false);

            var events = new List<SimEvent>();
            float hp = turrets.Turrets[0].Hp;
            bool down = false;

            for (int i = 0; i < 30 * 25 && !down; i++)
            {
                world.Step(1f / 30f);
                turrets.Step(world, 1f / 30f, null);
                world.DrainEvents(events);
                foreach (var e in events)
                {
                    // Bounds-checked, like the game does: a destroyed turret leaves the list, and
                    // events queued in the same batch still carry its old index.
                    if (e.Kind != SimEventKind.StructureMauled) continue;
                    if (e.B < 0 || e.B >= turrets.Turrets.Count) continue;
                    turrets.Damage(map, e.B, Mathf.CeilToInt(e.F));
                }
                events.Clear();

                // A destroyed turret leaves the list entirely, so "gone" and "not alive" are both
                // ways of losing it.
                down = turrets.Turrets.Count == 0 || !turrets.Turrets[0].Alive;
            }

            Assert.That(down, Is.True,
                        $"a crowd standing on a gun should pull it down; it started at {hp} hp");
        }
    }
}
