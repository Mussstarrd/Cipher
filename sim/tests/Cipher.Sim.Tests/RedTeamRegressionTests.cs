using System;
using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>
    /// Findings from the outside review of 2026-09-11. Every one of these was green code with a
    /// green test beside it; what they have in common is that the test exercised the path that
    /// worked and not the path that mattered.
    ///
    /// docs/05 §4: a bug found twice becomes a test. These were found once and get tests anyway.
    /// </summary>
    public sealed class RedTeamRegressionTests
    {
        private static (GridMap map, FlowField field, AgentWorld world) Open(float soloSeconds = 40f)
        {
            var map = new GridMap(40, 40);
            var field = new FlowField(map);
            field.Compute(38, 20);
            var world = new AgentWorld(map, field, new SimConfig { SoloDecryptSeconds = soloSeconds });
            return (map, field, world);
        }

        // ---------------------------------------------------------------- ADR-008 reached one of six

        [Fact]
        public void AFailingBodyChasingTheHeroIsAlsoSlowedDown()
        {
            // The original slowdown was applied by ONE caller out of six, and the test that was
            // meant to guard it exercised that one. Chasing the player is the path that matters.
            var (_, _, world) = Open(soloSeconds: 40f);
            world.SetHero(new Vec2(20f, 20f), alive: true);

            int healthy = world.Spawn(new Vec2(16f, 18f), 10f);
            int failing = world.Spawn(new Vec2(16f, 22f), 10f);
            // Nearly out, not out: emptying the bar now kills outright, and a corpse demonstrates
            // nothing about how a stumbling body moves.
            world.ApplyDamage(failing, 9.7f);

            var hStart = world.PositionOf(healthy);
            var fStart = world.PositionOf(failing);
            for (int i = 0; i < 10; i++) { world.SetHero(new Vec2(20f, 20f), alive: true); world.Step(0.05f); }

            float healthyMoved = MathF.Sqrt(Vec2.DistanceSquared(hStart, world.PositionOf(healthy)));
            float failingMoved = MathF.Sqrt(Vec2.DistanceSquared(fStart, world.PositionOf(failing)));

            Assert.True(failingMoved > 0.01f, "it still comes at you");
            Assert.True(failingMoved < healthyMoved, "but it is stumbling, even in a chase");
        }

        [Fact]
        public void AFailingWreckerHitsTheWallSofterThanAHealthyOne()
        {
            // The window has to be comparable to the failure window or the chip has barely decayed
            // and the per-swing rounding hides the difference -- which is how the first draft of
            // this test passed against the broken build.
            const float Window = 8f;

            static int DamageDealtOver(float seconds, bool broken)
            {
                var map = new GridMap(40, 40);
                for (int y = 0; y < 40; y++) map.SetWall(20, y, WallKind.Barricade, 2000);
                var field = new FlowField(map);
                field.Compute(38, 20);
                var world = new AgentWorld(map, field, new SimConfig { SoloDecryptSeconds = Window });

                int id = world.Spawn(new Vec2(19f, 20.5f), 10f, Intent.WreckWall);
                if (broken) world.ApplyDamage(id, 10f);

                ushort before = map.HpAt(20, 20);
                for (int i = 0; i < (int)(seconds / 0.05f); i++) world.Step(0.05f);
                return before - map.HpAt(20, 20);
            }

            int healthy = DamageDealtOver(Window, broken: false);
            int failing = DamageDealtOver(Window, broken: true);

            Assert.True(healthy > 0, "a healthy wrecker does damage at all");
            Assert.True(failing < healthy,
                        "a broken chip swings weaker -- ADR-008's whole promise is decaying strength");
        }

        [Fact]
        public void ASapperWhoseChipBreaksCannotFinishThePlant()
        {
            // The plant was a four-second timer nothing checked, so killing a Sapper inside its
            // last 2.5 seconds still opened the hole: a grace period in the enemy's favour on the
            // counter-play the entire wall system exists to serve.
            var map = new GridMap(30, 21);
            for (int y = 1; y < 21; y++) map.SetWall(15, y, WallKind.Barricade, GridMap.DefaultWallHp);
            var field = new FlowField(map);
            field.Compute(28, 19);
            var world = new AgentWorld(map, field, new SimConfig { SoloDecryptSeconds = 6f });

            int sapper = world.SpawnArchetype(new Vec2(14.2f, 19.5f), Archetype.Sapper);
            for (int i = 0; i < 200 && map.StageAt(15, 19) == BreachStage.Intact; i++)
            {
                world.Step(0.05f);
                // Break it the moment it settles in to plant.
                if (world.IsAlive(sapper) && !world.IsFailing(sapper)) world.ApplyDamage(sapper, 9999f);
            }

            for (int i = 0; i < 200; i++) world.Step(0.05f);
            Assert.Equal(BreachStage.Intact, map.StageAt(15, 19));
        }

        // ---------------------------------------------------------------- accounting

        [Fact]
        public void ABodyThatReachesTheGoalWhileFailingGivesItsSlotBack()
        {
            // There are exactly two places an agent stops being alive and only one of them used to
            // hand the counter back, so FailingCount leaked forever. Latent today; a soft lock for
            // whoever next trusts "a wave is not clear while any of these remain".
            var (_, _, world) = Open(soloSeconds: 6f);
            int id = world.Spawn(new Vec2(36.5f, 20.5f), 10f);
            world.ApplyDamage(id, 1f);
            Assert.Equal(1, world.FailingCount);

            for (int i = 0; i < 200 && world.IsAlive(id); i++) world.Step(0.05f);

            Assert.False(world.IsAlive(id));
            Assert.Equal(1, world.ReachedCount);
            Assert.Equal(0, world.FailingCount);
            Assert.Equal(0, world.AliveCount);
        }

        [Fact]
        public void APatrolDroneDoesNotEmptyItselfIntoABodyThatIsAlreadyGoingDown()
        {
            // Fixed for turrets when ADR-008 landed, missed for drones, which use a different
            // query. Seven rounds a second into a corpse while a healthy body walks past.
            var (_, _, world) = Open();
            int dying = world.Spawn(new Vec2(10.6f, 20f), 5f);
            int healthy = world.Spawn(new Vec2(12.5f, 20f), 100f);
            world.ApplyDamage(dying, 4.9f);   // about to fall, not fallen

            Assert.Equal(healthy, world.FindNearestInRange(new Vec2(10.5f, 20f), 6f));
        }

        // ---------------------------------------------------------------- the wall pool

        [Fact]
        public void EachBreachStageCostsAFreshBeatingRatherThanOnePoint()
        {
            // A 20 hp wall went Intact -> Cracked for 20 damage, then Cracked -> Broken for ONE,
            // then Broken -> Collapsed for ONE, because the wall was left sitting at zero and
            // Damage returned true for every point that landed afterwards. The twenty-seconds-per-
            // stage widening pipeline was bypassed for any wall taken down by gunfire.
            var map = new GridMap(8, 8);
            map.SetWall(4, 4, WallKind.Barricade, 20);

            Assert.True(map.Damage(4, 4, 20));
            Assert.Equal(BreachStage.Cracked, map.Breach(4, 4));

            Assert.False(map.Damage(4, 4, 1));
            Assert.Equal(BreachStage.Cracked, map.StageAt(4, 4));
            Assert.True(map.HpAt(4, 4) > 0, "a cracked wall still has something to take");

            // It does take less each time -- it is damaged -- but it is not free.
            int hits = 0;
            while (!map.Damage(4, 4, 1) && hits < 200) hits++;
            Assert.True(hits > 1, $"the second stage cost {hits + 1} points, not a fresh beating");
        }

        [Fact]
        public void AWallMendedOneStageIsNotOneStrayRoundFromOpeningAgain()
        {
            var map = new GridMap(8, 8);
            map.SetWall(4, 4, WallKind.Barricade, 20);
            map.Damage(4, 4, 20);
            map.Breach(4, 4);
            map.Damage(4, 4, 999);
            map.Breach(4, 4);
            Assert.Equal(BreachStage.Broken, map.StageAt(4, 4));

            map.RepairStage(4, 4);
            Assert.Equal(BreachStage.Cracked, map.StageAt(4, 4));
            Assert.False(map.Damage(4, 4, 1), "repairing it bought more than one round of grace");
        }

        [Fact]
        public void AWallKeepsItsOwnMaximumRatherThanTheDefault()
        {
            var weak = new GridMap(8, 8);
            weak.SetWall(4, 4, WallKind.Barricade, 10);
            weak.Damage(4, 4, 10);
            weak.Breach(4, 4);

            var strong = new GridMap(8, 8);
            strong.SetWall(4, 4, WallKind.Barricade, 400);
            strong.Damage(4, 4, 400);
            strong.Breach(4, 4);

            Assert.True(strong.HpAt(4, 4) > weak.HpAt(4, 4),
                        "a heavy barricade is still heavy after it cracks");
        }

        // ---------------------------------------------------------------- the hero layer, untested until now

        [Fact]
        public void TheHeroAggressionLayerRunsAtAll()
        {
            // SetHero had zero references in the suite, so the chase, sticky aggro, the contact
            // hold, ChasingCount, the pistol standoff and the PistolShot event were never executed
            // by a single test -- which is exactly the layer the decay bug was hiding in.
            var (_, _, world) = Open();
            world.SetHero(new Vec2(20f, 20f), alive: true);
            int id = world.Spawn(new Vec2(16f, 20f), 10f);

            for (int i = 0; i < 20; i++) { world.SetHero(new Vec2(20f, 20f), alive: true); world.Step(0.05f); }

            Assert.True(world.ChasingCount > 0, "someone within aggro range should be coming for him");
            Assert.True(world.PositionOf(id).X > 16f, "and closing");
        }

        [Fact]
        public void AnArmedBodyShootsTheHeroAndTheEventCarriesDecayedDamage()
        {
            var map = new GridMap(40, 40);
            var field = new FlowField(map);
            field.Compute(38, 20);
            // A slow decrypt on purpose: this test is about how HARD a frail body shoots, and with
            // a fast drain the subject dies between the two measurements.
            var world = new AgentWorld(map, field, new SimConfig { SoloDecryptSeconds = 200f });
            world.SetHero(new Vec2(20f, 20f), alive: true);

            world.Spawn(new Vec2(16f, 20f), 10f, Intent.Vault, pace: 1f, armed: true);

            float healthyShot = FirstPistolShot(world);
            Assert.True(healthyShot > 0f, "an armed body takes pot shots at him");

            // Down to a quarter of the bar, which is inside the frailty band but a long way from
            // empty -- emptying it would kill outright and there would be nothing left to shoot.
            world.ApplyDamage(0, 7.5f);
            float failingShot = FirstPistolShot(world);
            Assert.True(failingShot > 0f && failingShot < healthyShot,
                        "a broken chip still shoots, and it shoots weaker");
        }

        private static readonly List<SimEvent> _drained = new List<SimEvent>();

        private static float FirstPistolShot(AgentWorld world)
        {
            for (int i = 0; i < 400; i++)
            {
                world.SetHero(new Vec2(20f, 20f), alive: true);
                world.Step(0.05f);
                _drained.Clear();
                world.DrainEvents(_drained);
                foreach (var e in _drained)
                    if (e.Kind == SimEventKind.PistolShot) return e.F;
            }
            return 0f;
        }
    }
}
