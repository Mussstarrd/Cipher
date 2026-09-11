#nullable enable
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>
    /// Owner's 2026-09-11 rules: emplacements cannot see or shoot through walls, fire that hits a
    /// wall degrades it, and bodies do not all run the same errand.
    /// </summary>
    public sealed class VisionAndIntentTests
    {
        private static (AgentWorld world, GridMap map) MakeWorld(int w = 32, int h = 24)
        {
            var map = new GridMap(w, h);
            // Goal on the right edge so the flow field has somewhere to point.
            var field = new FlowField(map);
            field.Compute(w - 2, h / 2);
            var world = new AgentWorld(map, field, new SimConfig());
            return (world, map);
        }

        private static void WallColumn(GridMap map, int x, int fromY, int toY)
        {
            for (int y = fromY; y <= toY; y++) map.SetWall(x, y, WallKind.Wall, 100);
        }

        // ---------------------------------------------------------------- line of sight

        [Fact]
        public void ClearGroundIsVisible()
        {
            var (world, _) = MakeWorld();
            Assert.True(world.HasLineOfSight(new Vec2(4.5f, 12.5f), new Vec2(10.5f, 12.5f)));
        }

        [Fact]
        public void AWallBreaksLineOfSight()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 8, 10, 15);
            Assert.False(world.HasLineOfSight(new Vec2(4.5f, 12.5f), new Vec2(12.5f, 12.5f)));
        }

        [Fact]
        public void SightIsSymmetric()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 8, 10, 15);
            var a = new Vec2(4.5f, 12.5f);
            var b = new Vec2(12.5f, 12.5f);
            Assert.Equal(world.HasLineOfSight(a, b), world.HasLineOfSight(b, a));
        }

        [Fact]
        public void AWallBeyondTheTargetDoesNotBlockIt()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 20, 10, 15);
            // Target sits well short of the wall, so the wall is irrelevant.
            Assert.True(world.HasLineOfSight(new Vec2(4.5f, 12.5f), new Vec2(10.5f, 12.5f)));
        }

        [Fact]
        public void FirstWallBetweenReportsTheBlockingCell()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 8, 10, 15);
            Assert.True(world.FirstWallBetween(new Vec2(4.5f, 12.5f), new Vec2(12.5f, 12.5f),
                                               out int wx, out int wy));
            Assert.Equal(8, wx);
            Assert.InRange(wy, 10, 15);
        }

        [Fact]
        public void FirstWallBetweenIsFalseOnClearGround()
        {
            var (world, _) = MakeWorld();
            Assert.False(world.FirstWallBetween(new Vec2(4.5f, 12.5f), new Vec2(10.5f, 12.5f),
                                                out _, out _));
        }

        // ---------------------------------------------------------------- turret acquisition

        [Fact]
        public void ATurretWillNotAcquireThroughItsOwnBarricade()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 8, 8, 18);
            world.Spawn(new Vec2(12.5f, 12.5f), 10f);

            var gunPos = new Vec2(4.5f, 12.5f);
            Assert.True(world.FindFirstInRange(gunPos, 20f) >= 0, "it is in plain range");
            Assert.True(world.FindFirstInRangeVisible(gunPos, 20f) < 0, "but not in sight");
        }

        [Fact]
        public void ATurretStillAcquiresWhatItCanSee()
        {
            var (world, _) = MakeWorld();
            world.Spawn(new Vec2(12.5f, 12.5f), 10f);
            Assert.True(world.FindFirstInRangeVisible(new Vec2(4.5f, 12.5f), 20f) >= 0);
        }

        [Fact]
        public void ItSeesThroughACollapsedBreach()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 8, 8, 18);
            // Intact -> Cracked -> Broken -> Collapsed.
            for (int y = 11; y <= 14; y++)
                while (map.StageAt(8, y) != BreachStage.Collapsed) map.Breach(8, y);

            world.Spawn(new Vec2(12.5f, 12.5f), 10f);

            Assert.True(world.FindFirstInRangeVisible(new Vec2(4.5f, 12.5f), 20f) >= 0,
                        "a hole in the wall is a firing lane, in both directions");
        }

        [Fact]
        public void AreaDamageDoesNotReachThroughCover()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 8, 8, 18);
            world.Spawn(new Vec2(12.5f, 12.5f), 10f);

            int kills = world.ApplyRadialDamageVisible(new Vec2(4.5f, 12.5f), 20f, 999f);
            Assert.Equal(0, kills);
            Assert.Equal(1, world.AliveCount);
        }

        [Fact]
        public void CountWithinVisibleIgnoresWhatIsBehindAWall()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 8, 8, 18);
            world.Spawn(new Vec2(12.5f, 12.5f), 10f);
            world.Spawn(new Vec2(5.5f, 12.5f), 10f);

            Assert.Equal(1, world.CountWithinVisible(new Vec2(4.5f, 12.5f), 20f));
        }

        // ---------------------------------------------------------------- intents

        [Fact]
        public void PlainSpawnsStillHeadForTheObjective()
        {
            var (world, _) = MakeWorld();
            int id = world.Spawn(new Vec2(2.5f, 12.5f), 10f);
            Assert.Equal(Intent.Vault, world.IntentOf(id));
        }

        [Fact]
        public void IntentIsRecordedAndCounted()
        {
            var (world, _) = MakeWorld();
            world.Spawn(new Vec2(2.5f, 10.5f), 10f, Intent.Vault);
            world.Spawn(new Vec2(2.5f, 11.5f), 10f, Intent.HuntStructure);
            world.Spawn(new Vec2(2.5f, 12.5f), 10f, Intent.WreckWall);
            world.Spawn(new Vec2(2.5f, 13.5f), 10f, Intent.WreckWall);

            var (vault, hunters, wreckers) = world.IntentCensus();
            Assert.Equal(1, vault);
            Assert.Equal(1, hunters);
            Assert.Equal(2, wreckers);
        }

        [Fact]
        public void AWreckerAttacksAWallInsteadOfWalkingAroundIt()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 6, 10, 15);
            ushort before = map.HpAt(6, 12);

            world.Spawn(new Vec2(5.2f, 12.5f), 10f, Intent.WreckWall);
            for (int i = 0; i < 60; i++) world.Step(1f / 30f);

            Assert.True(map.HpAt(6, 12) < before, "the wall should be coming down");
        }

        [Fact]
        public void AVaultRunnerLeavesTheWallAlone()
        {
            var (world, map) = MakeWorld();
            WallColumn(map, 6, 10, 15);
            ushort before = map.HpAt(6, 12);

            world.Spawn(new Vec2(5.2f, 12.5f), 10f, Intent.Vault);
            for (int i = 0; i < 60; i++) world.Step(1f / 30f);

            Assert.Equal(before, map.HpAt(6, 12));
        }

        [Fact]
        public void AHunterWithNothingToHuntJustKeepsWalking()
        {
            var (world, _) = MakeWorld();
            int id = world.Spawn(new Vec2(2.5f, 12.5f), 10f, Intent.HuntStructure);
            Vec2 start = world.PositionOf(id);

            for (int i = 0; i < 30; i++) world.Step(1f / 30f);

            Assert.True(world.PositionOf(id).X > start.X,
                        "no structures, so it rejoins the crowd rather than standing still");
        }

        [Fact]
        public void AHunterWalksAtTheNearestEmplacement()
        {
            var (world, _) = MakeWorld();
            world.Structures = new FakeStructures(new Vec2(6.5f, 4.5f));

            int id = world.Spawn(new Vec2(4.5f, 12.5f), 10f, Intent.HuntStructure);
            float startDist = Vec2.DistanceSquared(world.PositionOf(id), new Vec2(6.5f, 4.5f));

            for (int i = 0; i < 40; i++) world.Step(1f / 30f);

            float endDist = Vec2.DistanceSquared(world.PositionOf(id), new Vec2(6.5f, 4.5f));
            Assert.True(endDist < startDist, "it should be closing on the gun, not the objective");
        }

        [Fact]
        public void AHunterInContactReportsItSoTheGameLayerCanApplyDamage()
        {
            var (world, _) = MakeWorld();
            world.Structures = new FakeStructures(new Vec2(5.0f, 12.5f));
            world.Spawn(new Vec2(4.6f, 12.5f), 10f, Intent.HuntStructure);

            bool mauled = false;
            var drained = new System.Collections.Generic.List<SimEvent>();
            for (int i = 0; i < 90 && !mauled; i++)
            {
                world.Step(1f / 30f);
                drained.Clear();
                world.DrainEvents(drained);
                foreach (var e in drained)
                    if (e.Kind == SimEventKind.StructureMauled) mauled = true;
            }
            Assert.True(mauled);
        }

        [Fact]
        public void TheIntentSplitStaysDeterministic()
        {
            ulong HashRun()
            {
                var (world, map) = MakeWorld();
                WallColumn(map, 10, 8, 16);
                for (int i = 0; i < 12; i++)
                {
                    var intent = (Intent)(i % 3);
                    world.Spawn(new Vec2(2.5f, 6.5f + i * 0.7f), 10f, intent);
                }
                for (int t = 0; t < 120; t++) world.Step(1f / 30f);
                return world.StateHash();
            }

            Assert.Equal(HashRun(), HashRun());
        }

        private sealed class FakeStructures : IStructureQuery
        {
            private readonly Vec2[] _positions;
            public FakeStructures(params Vec2[] positions) { _positions = positions; }
            public int Count => _positions.Length;
            public Vec2 PositionAt(int index) => _positions[index];
        }

        // ---------------------------------------------------------------- turret scaling

        [Fact]
        public void TurretMultipliersScaleGunsAlreadyOnTheBoard()
        {
            // Scaling at fire time rather than at placement is the point: a Doctrine card has to
            // improve the turrets the player already owns, or it reads as a bug.
            var turrets = new TurretSystem();
            Assert.Equal(1f, turrets.DamageMultiplier);
            Assert.Equal(1f, turrets.RangeMultiplier);

            turrets.DamageMultiplier = 1.3f;
            turrets.RangeMultiplier = 1.2f;

            Assert.Equal(1.3f, turrets.DamageMultiplier, 3);
            Assert.Equal(1.2f, turrets.RangeMultiplier, 3);
        }
    }
}
