#nullable enable
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// THE TURRETS MUST FIRE.
    ///
    /// Owner playtest, 2026-09-11: "the turrets arent shooting". In a tower-defense game that is
    /// the whole product, and it shipped, because line-of-sight acquisition marched its ray from
    /// the turret's own cell — which is marked <see cref="WallKind.Structure"/> the moment a turret
    /// is placed on it. Every turret in the game was blocked by itself, saw nothing, and never
    /// pulled the trigger. No test caught it because the sight tests all asked "can it see through
    /// a wall" and never "can it see at all".
    ///
    /// These live in the EditMode suite rather than in sim/tests because the machine building this
    /// has no .NET SDK: a sim test could not have been RUN here, only written. Both suites run in CI.
    /// </summary>
    public sealed class TurretSightTests
    {
        private static (GridMap map, AgentWorld world, TurretSystem turrets) Field()
        {
            var map = new GridMap(32, 32);
            var field = new FlowField(map);
            field.Compute(30, 16);
            var world = new AgentWorld(map, field, new SimConfig(), initialCapacity: 64);
            return (map, world, new TurretSystem());
        }

        [Test]
        public void ATurretFiresAtSomethingStandingInFrontOfIt()
        {
            var (map, world, turrets) = Field();
            turrets.Place(map, 10, 16);
            world.Spawn(new Vec2(14.5f, 16.5f), health: 100f);

            var shots = new System.Collections.Generic.List<TurretShot>();
            turrets.Step(world, 1f, shots);

            Assert.That(turrets.Turrets[0].ShotsFired, Is.GreaterThan(0),
                        "a turret with a clear shot at four cells must fire");
            Assert.That(shots, Is.Not.Empty);
        }

        [Test]
        public void ATurretCellIsSolidAndStillDoesNotBlindItsOwnTurret()
        {
            // The precondition for the bug, asserted so nobody "fixes" it by making turret cells
            // passable — they block pathing on purpose, which is what makes them mazeable.
            var (map, world, turrets) = Field();
            turrets.Place(map, 10, 16);

            Assert.That(map.IsBlocked(10, 16), Is.True, "a turret still blocks the grid");
            Assert.That(world.HasLineOfSight(GridMap.CellCenter(10, 16), new Vec2(14.5f, 16.5f)),
                        Is.True, "and can still see out of its own cell");
        }

        [Test]
        public void AGrinderAlsoFires()
        {
            // Area fire goes through CountWithinVisible, which had exactly the same blind spot.
            var (map, world, turrets) = Field();
            turrets.Place(map, 10, 16, family: 1);
            world.Spawn(new Vec2(11.2f, 16.5f), health: 100f);

            turrets.Step(world, 1f, null);
            Assert.That(turrets.Turrets[0].ShotsFired, Is.GreaterThan(0));
        }

        [Test]
        public void AWallBetweenStillStopsIt()
        {
            // The feature the bug rode in on has to keep working: the owner's rule is that a turret
            // cannot detect through a wall.
            var (map, world, turrets) = Field();
            turrets.Place(map, 10, 16);
            for (int y = 0; y < 32; y++) map.SetWall(12, y, WallKind.Wall, GridMap.DefaultWallHp);
            world.Spawn(new Vec2(14.5f, 16.5f), health: 100f);

            Assert.That(world.HasLineOfSight(GridMap.CellCenter(10, 16), new Vec2(14.5f, 16.5f)),
                        Is.False, "a wall between them is still a wall");
        }

        [Test]
        public void AWallRightInFrontIsShotRatherThanSeenThrough()
        {
            var (map, world, turrets) = Field();
            turrets.Place(map, 10, 16);
            map.SetWall(11, 16, WallKind.Barricade, GridMap.DefaultWallHp);
            world.Spawn(new Vec2(14.5f, 16.5f), health: 100f);

            Assert.That(world.HasLineOfSight(GridMap.CellCenter(10, 16), new Vec2(14.5f, 16.5f)),
                        Is.False, "the cell next door is not the turret's own cell");
        }
    }
}
