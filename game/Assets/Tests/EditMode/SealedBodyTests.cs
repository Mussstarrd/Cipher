#nullable enable
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// A body with no route to the objective goes at what is stopping it.
    ///
    /// Found by -exodus-autoplay on Crowbar: a funded defence walled four streets, forty of
    /// forty-two bodies died, and then the kill count moved by ONE over six minutes. Four bodies
    /// stood in a pocket the barricades had closed. The flow field's direction on an unreachable
    /// cell is zero -- it was never written -- so they followed nothing, drifted on separation
    /// alone, and waited to be shot by turrets that could not see them. The wave could not clear
    /// and the mission could not end.
    ///
    /// Player walls were a hard counter with no counterplay, which is the opposite of what
    /// the-contracting-perimeter.md asks of mission 6: "the ground still funnels, but only while
    /// it holds".
    /// </summary>
    public sealed class SealedBodyTests
    {
        /// <summary>A body boxed into a pocket of barricades, with the goal outside it.</summary>
        private static (GridMap map, AgentWorld world, int id) Boxed()
        {
            var map = new GridMap(48, 48);

            // A closed room around (10,10). Barricade, not Rock: this is the player's own wall,
            // which is the whole point -- scenery is not breachable and never was the problem.
            for (int x = 6; x <= 14; x++)
            {
                map.SetWall(x, 6, WallKind.Barricade, GridMap.DefaultWallHp);
                map.SetWall(x, 14, WallKind.Barricade, GridMap.DefaultWallHp);
            }
            for (int y = 6; y <= 14; y++)
            {
                map.SetWall(6, y, WallKind.Barricade, GridMap.DefaultWallHp);
                map.SetWall(14, y, WallKind.Barricade, GridMap.DefaultWallHp);
            }

            var flow = new FlowField(map);
            flow.Compute(46, 24);
            var world = new AgentWorld(map, flow, new SimConfig(), initialCapacity: 16);
            int id = world.Spawn(new Vec2(10.5f, 10.5f), 100f, Intent.Vault);
            return (map, world, id);
        }

        private static int TotalBarricadeHp(GridMap map)
        {
            int total = 0;
            for (int y = 6; y <= 14; y++)
                for (int x = 6; x <= 14; x++)
                    if (map.KindAt(x, y) == WallKind.Barricade) total += map.HpAt(x, y);
            return total;
        }

        private static int IntactBarricades(GridMap map)
        {
            int n = 0;
            for (int y = 6; y <= 14; y++)
                for (int x = 6; x <= 14; x++)
                    if (map.KindAt(x, y) == WallKind.Barricade && map.StageAt(x, y) == BreachStage.Intact) n++;
            return n;
        }

        [Test]
        public void TheBodyIsGenuinelySealedIn()
        {
            var (_, world, _) = Boxed();
            // Guards the fixture itself: if a later change to GridMap or FlowField reopened this
            // room, the test below would pass for entirely the wrong reason.
            Assert.That(world.Map.Width, Is.EqualTo(48));
            var flow = new FlowField(world.Map);
            flow.Compute(46, 24);
            Assert.That(flow.HasPath(10, 10), Is.False, "the fixture must actually seal the body in");
            Assert.That(flow.HasPath(30, 24), Is.True, "and the rest of the map must still be open");
        }

        [Test]
        public void ASealedBodyPullsAtTheWallInsteadOfStandingThere()
        {
            var (map, world, _) = Boxed();

            int intactBefore = IntactBarricades(map);
            int hpBefore = TotalBarricadeHp(map);

            for (int i = 0; i < 600; i++) world.Step(1f / 30f);   // twenty seconds

            // ANY cell of the box, not a named one: which wall it picks is FindNearestWallCell's
            // business and a test that pins the choice would break the first time that scan order
            // is tuned. What must never change is that the box takes damage at all.
            Assert.That(TotalBarricadeHp(map) < hpBefore || IntactBarricades(map) < intactBefore,
                        Is.True,
                        "a body that cannot reach the objective must attack the wall that is " +
                        "stopping it, or a well-walled position never ends");
        }

        [Test]
        public void ASealedBodyDoesNotSimplyStopMoving()
        {
            var (_, world, id) = Boxed();
            var start = world.PositionOf(id);

            for (int i = 0; i < 60; i++) world.Step(1f / 30f);

            // It does not matter WHERE it went -- only that a pathless body is still doing
            // something. Zero displacement over two seconds was the signature of the frozen wave.
            var now = world.PositionOf(id);
            float moved = (now - start).Length;
            Assert.That(moved, Is.GreaterThan(0.05f),
                        "a pathless body followed a zero direction vector and stood still");
        }
    }
}
