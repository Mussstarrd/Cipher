#nullable enable
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// Wrecking is an errand, not a career.
    ///
    /// StepWallWrecker used to return true for as long as ANY wall sat within WreckerSearchCells,
    /// with no way to finish. On a position where the player has built fifty barricades that means
    /// a wrecker demolishes the neighbourhood for the rest of the match and never once goes at the
    /// truck. Found by -exodus-autoplay on Crowbar, where two of them held wave one open
    /// indefinitely with the truck untouched: the field was not stuck, it was BUSY.
    ///
    /// SimConfig has always described these as the share "that go at a barricade rather than
    /// walking round it" -- a body dealing with the wall in its way, not a demolition contract.
    /// </summary>
    public sealed class WreckerErrandTests
    {
        /// <summary>One barricade beside the wrecker, objective away to the east.</summary>
        private static (GridMap map, AgentWorld world, int id) AtAWall(float wallHp = 40f)
        {
            var map = new GridMap(48, 48);
            map.SetWall(22, 24, WallKind.Barricade, (ushort)wallHp);

            var flow = new FlowField(map);
            flow.Compute(46, 24);

            var world = new AgentWorld(map, flow, new SimConfig(), initialCapacity: 16);
            world.SetHero(new Vec2(0f, 0f), alive: false);
            int id = world.Spawn(new Vec2(21.5f, 24.5f), 100f, Intent.WreckWall);
            return (map, world, id);
        }

        [Test]
        public void AWreckerGoesBackToTheObjectiveOnceItsHoleIsOpen()
        {
            var (map, world, id) = AtAWall(wallHp: 40f);
            Assert.That(world.IntentOf(id), Is.EqualTo(Intent.WreckWall), "fixture should start wrecking");

            for (int i = 0; i < 30 * 30; i++) world.Step(1f / 30f);

            Assert.That(map.StageAt(22, 24), Is.EqualTo(BreachStage.Collapsed), "the wall should have gone down");
            Assert.That(world.IntentOf(id), Is.EqualTo(Intent.Vault),
                        "the hole is open, so the body goes through it -- that was the entire point");
        }

        [Test]
        public void AWreckerDoesNotAbandonAWallItIsStillBreaking()
        {
            // The safety direction. A patience that expired mid-job would delete the intent: every
            // wrecker would wander off before anything came down, and the wall would be free.
            var (map, world, id) = AtAWall(wallHp: GridMap.DefaultWallHp);

            for (int i = 0; i < 30 * 20; i++) world.Step(1f / 30f);   // 20s, short of a 200hp wall

            Assert.That(map.StageAt(22, 24), Is.Not.EqualTo(BreachStage.Collapsed),
                        "fixture assumes this wall has NOT come down yet");
            Assert.That(world.IntentOf(id), Is.EqualTo(Intent.WreckWall),
                        "a wrecker making visible progress must keep at it");
        }

        [Test]
        public void AWreckerWithNothingToBreakGivesUpAndGoesForTheObjective()
        {
            // Nothing breakable anywhere: scenery is Rock and does not count.
            var map = new GridMap(48, 48);
            for (int y = 20; y < 28; y++) map.SetWall(22, y, WallKind.Rock, GridMap.DefaultWallHp);

            var flow = new FlowField(map);
            flow.Compute(46, 24);
            var world = new AgentWorld(map, flow, new SimConfig(), initialCapacity: 16);
            world.SetHero(new Vec2(0f, 0f), alive: false);
            int id = world.Spawn(new Vec2(18.5f, 24.5f), 100f, Intent.WreckWall);
            float startX = world.PositionOf(id).X;

            for (int i = 0; i < 30 * 60; i++) world.Step(1f / 30f);

            // Asserts BEHAVIOUR, not the label. With nothing breakable in range StepWallWrecker
            // returns false and the body drops through to StepRunner on the same tick, so it walks
            // at the objective while still nominally a wrecker. Keeping the label is deliberate:
            // clearing it would permanently demote any wrecker that happens to spawn further than
            // WreckerSearchCells from a wall, and the whole "go at a barricade" share would quietly
            // collapse on maps whose walls are not next to the gates.
            Assert.That(world.PositionOf(id).X, Is.GreaterThan(startX + 5f),
                        "a wrecker with nothing to break must still be making for the objective");
        }
    }
}
