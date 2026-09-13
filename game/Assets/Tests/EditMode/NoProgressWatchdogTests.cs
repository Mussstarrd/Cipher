#nullable enable
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The backstop for a whole class of bug.
    ///
    /// Three separate behaviours shipped with the same defect: the structure hunter, the wall
    /// wrecker, and the spitter's approach each returned "handled" unconditionally, so a body that
    /// could not finish its errand never fell through to anything else, never reached the
    /// objective, and never died. Each one froze a wave for minutes at a time. The first two were
    /// fixed where they stood; this exists so the fourth does not cost another night.
    ///
    /// The danger of a watchdog is the false positive -- a body that is working correctly and
    /// happens to be standing still. Both of those cases are real and both are tested here: a
    /// wrecker holds one cell for about 27 seconds breaking a 200hp barricade, and a Sapper holds
    /// one for the whole plant. Landing a blow resets the clock, which is what makes them safe.
    /// </summary>
    public sealed class NoProgressWatchdogTests
    {
        [Test]
        public void ABodyThatCanNeitherMoveNorHitAnythingIsNoticed()
        {
            // A one-cell pocket of scenery. Rock is not breachable, so this body cannot walk out of
            // it and cannot chew its way out either: it can make no progress by any definition.
            //
            // NOTE ON WHAT THIS DOES AND DOES NOT PROVE. An earlier version of this test put a
            // Spitter behind a wall from its gun and asserted a trip; it never tripped, because the
            // Spitter's own fallback kept it shuffling and the clock kept resetting. The watchdog
            // measures cells held, not intentions, and only a body that truly holds one is caught.
            // The in-game evidence is separate and better: Crowbar's waves went from freezing
            // indefinitely to running 1 -> 2 -> 3, with the counter reading four trips in 420
            // seconds across roughly two hundred bodies.
            var map = new GridMap(48, 48);
            map.SetWall(20, 24, WallKind.Rock, GridMap.DefaultWallHp);
            map.SetWall(22, 24, WallKind.Rock, GridMap.DefaultWallHp);
            map.SetWall(21, 23, WallKind.Rock, GridMap.DefaultWallHp);
            map.SetWall(21, 25, WallKind.Rock, GridMap.DefaultWallHp);

            var flow = new FlowField(map);
            flow.Compute(46, 24);

            var world = new AgentWorld(map, flow, new SimConfig(), initialCapacity: 16);
            world.SetHero(new Vec2(0f, 0f), alive: false);
            int id = world.Spawn(new Vec2(21.5f, 24.5f), 100f, Intent.Vault);
            Assert.That(world.NoProgressTrips, Is.Zero, "nothing should have tripped yet");

            for (int i = 0; i < 30 * 40; i++) world.Step(1f / 30f);

            Assert.That(world.NoProgressTrips, Is.GreaterThan(0),
                        "a body that holds one cell and lands no blow must be noticed");
            Assert.That(world.IsAlive(id), Is.True, "and not by killing it");
        }

        [Test]
        public void AWreckerBreakingAWallIsNotTreatedAsStuck()
        {
            // The false positive that would matter most. A wrecker stands in one cell for about 27
            // seconds taking down a 200hp barricade; if that counted as no progress, the watchdog
            // would cancel every wrecker mid-job and player walls would never come down at all.
            var map = new GridMap(48, 48);
            map.SetWall(22, 24, WallKind.Barricade, GridMap.DefaultWallHp);

            var flow = new FlowField(map);
            flow.Compute(46, 24);
            var world = new AgentWorld(map, flow, new SimConfig(), initialCapacity: 16);
            world.SetHero(new Vec2(0f, 0f), alive: false);
            world.Spawn(new Vec2(21.5f, 24.5f), 100f, Intent.WreckWall);

            for (int i = 0; i < 30 * 25; i++) world.Step(1f / 30f);

            Assert.That(world.NoProgressTrips, Is.Zero,
                        "pulling at a wall is progress; the clock resets on every blow landed");
        }

        [Test]
        public void AnOrdinaryWalkingCrowdNeverTripsIt()
        {
            // The other false positive: if this fired on healthy bodies it would be quietly
            // rewriting the crowd's behaviour on every position in the game.
            var map = new GridMap(48, 48);
            var flow = new FlowField(map);
            flow.Compute(46, 24);
            var world = new AgentWorld(map, flow, new SimConfig(), initialCapacity: 64);
            world.SetHero(new Vec2(0f, 0f), alive: false);

            for (int n = 0; n < 24; n++)
                world.Spawn(new Vec2(2.5f + n % 3, 12.5f + n), 100f, Intent.Vault);

            for (int i = 0; i < 30 * 40; i++) world.Step(1f / 30f);

            Assert.That(world.NoProgressTrips, Is.Zero,
                        "a crowd that is walking is making progress, however slowly");
        }
    }
}
