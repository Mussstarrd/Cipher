#nullable enable
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// A chase has to be able to fail.
    ///
    /// StepStructureHunter used to steer and return true on EVERY tick it was out of contact, with
    /// no check that it was getting anywhere. A hunter with a wall between it and the gun slid
    /// along that wall forever: never closing, never falling through to the objective, never dying.
    /// Found by -exodus-autoplay on Crowbar, where one of them held wave one open for SIX MINUTES
    /// with the truck untouched at 30/30 and the rest of the field clear.
    /// </summary>
    public sealed class HunterGiveUpTests
    {
        /// <summary>A gun walled off from the hunter, with the objective away to the east.</summary>
        private static (GridMap map, AgentWorld world, int id) WalledOffGun()
        {
            var map = new GridMap(48, 48);

            // A solid column between the hunter and the gun, long enough that sliding along it
            // never finds a way round inside the pursuit budget.
            for (int y = 4; y < 44; y++) map.SetWall(24, y, WallKind.Rock, GridMap.DefaultWallHp);

            var flow = new FlowField(map);
            flow.Compute(46, 24);

            var world = new AgentWorld(map, flow, new SimConfig(), initialCapacity: 16);
            var turrets = new TurretSystem();
            turrets.Place(map, 26, 24);                      // east of the wall
            world.Structures = turrets.AsStructureQuery();
            world.SetHero(new Vec2(0f, 0f), alive: false);   // nobody to be distracted by

            // West of the wall, inside HunterAcquireRange (14) of the gun, so it acquires at once.
            int id = world.Spawn(new Vec2(21.5f, 24.5f), 100f, Intent.HuntStructure);
            return (map, world, id);
        }

        [Test]
        public void AHunterThatCannotReachItsGunGivesUp()
        {
            var (_, world, id) = WalledOffGun();
            Assert.That(world.IntentOf(id), Is.EqualTo(Intent.HuntStructure), "fixture should start as a hunter");

            // Comfortably past HunterPursuitSeconds (14).
            for (int i = 0; i < 30 * 20; i++) world.Step(1f / 30f);

            Assert.That(world.IntentOf(id), Is.EqualTo(Intent.Vault),
                        "a hunter that never closes must go back to the objective, or it is a " +
                        "body that can never be resolved and the wave can never end");
        }

        [Test]
        public void AHunterThatCanReachItsGunDoesNotGiveUp()
        {
            // The other direction, and the one that makes the fix safe: an ordinary chase across
            // open ground must still work. A give-up that fires on a legitimate pursuit would
            // quietly delete the whole Hunter intent.
            var map = new GridMap(48, 48);
            var flow = new FlowField(map);
            flow.Compute(46, 24);

            var world = new AgentWorld(map, flow, new SimConfig(), initialCapacity: 16);
            var turrets = new TurretSystem();
            turrets.Place(map, 30, 24);
            world.Structures = turrets.AsStructureQuery();
            world.SetHero(new Vec2(0f, 0f), alive: false);

            int id = world.Spawn(new Vec2(21.5f, 24.5f), 100f, Intent.HuntStructure);
            for (int i = 0; i < 30 * 20; i++) world.Step(1f / 30f);

            Assert.That(world.IntentOf(id), Is.EqualTo(Intent.HuntStructure),
                        "a hunter with a clear run at the gun must still be hunting it");
        }

        [Test]
        public void TheFixtureReallyDoesWallOffTheGun()
        {
            // Guards the first test against passing for the wrong reason: if the column ever stops
            // being solid, "gave up" would just mean "walked round and got there".
            var (map, _, _) = WalledOffGun();
            for (int y = 4; y < 44; y++)
                Assert.That(map.KindAt(24, y), Is.EqualTo(WallKind.Rock), $"column broken at y={y}");
        }
    }
}
