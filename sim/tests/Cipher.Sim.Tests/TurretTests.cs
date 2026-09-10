using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    public class TurretTests
    {
        private const float Dt = 1f / 30f;

        private static (GridMap map, FlowField field, AgentWorld world) OpenField(int size = 32)
        {
            var map = new GridMap(size, size);
            var field = new FlowField(map);
            field.Compute(size - 2, size / 2);
            return (map, field, new AgentWorld(map, field, new SimConfig()));
        }

        [Fact]
        public void Place_OccupiesCellAsStructure_Remove_FreesIt()
        {
            var (map, _, _) = OpenField();
            var turrets = new TurretSystem(new TurretConfig());
            int i = turrets.Place(map, 10, 10);
            Assert.True(map.IsBlocked(10, 10));
            Assert.Equal(WallKind.Structure, map.KindAt(10, 10));
            Assert.Equal(0, i);
            Assert.Equal(0, turrets.IndexAt(10, 10));

            float refund = turrets.Remove(map, i);
            Assert.Equal(1f, refund, 3);
            Assert.True(map.IsBuildable(10, 10));
            Assert.Empty(turrets.Turrets);
        }

        [Fact]
        public void Targets_TheRunnerClosestToTheExit_NotTheNearestToTheTurret()
        {
            var (map, _, world) = OpenField();
            var turrets = new TurretSystem(new TurretConfig { Range = 10f });
            turrets.Place(map, 10, 16);
            int nearTurret = world.Spawn(new Vec2(11.5f, 16.5f), 100f);   // 1 cell away, far from exit
            int nearExit = world.Spawn(new Vec2(18.5f, 16.5f), 100f);     // 8 cells away, closer to the exit at x=30
            world.Spawn(new Vec2(28.5f, 16.5f), 100f);                    // out of range

            var shots = new List<TurretShot>();
            turrets.Step(world, Dt, shots);
            Assert.Single(shots);
            Assert.Equal(nearExit, world.FindFirstInRange(GridMap.CellCenter(10, 16), 10f));
            Assert.Equal(100f - turrets.Config.DamagePerShot, world.HealthOf(nearExit), 3);
            Assert.Equal(100f, world.HealthOf(nearTurret), 3);
        }

        [Fact]
        public void FireRate_DeliversConfiguredDps_AndCountsKills()
        {
            var (map, _, world) = OpenField();
            var cfg = new TurretConfig { Range = 10f, DamagePerShot = 5f, ShotsPerSecond = 12f };
            var turrets = new TurretSystem(cfg);
            turrets.Place(map, 10, 16);
            int target = world.Spawn(new Vec2(13.5f, 16.5f), 55f); // 11 shots

            int kills = 0;
            for (int t = 0; t < 30; t++) kills += turrets.Step(world, Dt, null); // 1 s: 12 shots available
            Assert.Equal(1, kills);
            Assert.False(world.IsAlive(target));
            Assert.Equal(1, turrets.Turrets[0].Kills);
            Assert.Equal(1, world.TotalKills);
            Assert.InRange(turrets.Turrets[0].ShotsFired, 11, 12);
        }

        [Fact]
        public void IdleTurret_DoesNotBankShots_ForABurstLater()
        {
            var (map, _, world) = OpenField();
            var turrets = new TurretSystem(new TurretConfig { ShotsPerSecond = 12f });
            turrets.Place(map, 10, 16);
            for (int t = 0; t < 90; t++) turrets.Step(world, Dt, null); // 3 s with nothing in range

            world.Spawn(new Vec2(12.5f, 16.5f), 1000f);
            var shots = new List<TurretShot>();
            turrets.Step(world, Dt, shots);
            Assert.InRange(shots.Count, 1, 2);
        }

        [Fact]
        public void Damage_DestroysTurret_AndFreesCell()
        {
            var (map, _, _) = OpenField();
            var turrets = new TurretSystem(new TurretConfig { MaxHp = 100 });
            turrets.Place(map, 5, 5);
            Assert.False(turrets.Damage(map, 0, 60));
            Assert.Equal(40, turrets.Turrets[0].Hp);
            Assert.True(turrets.Damage(map, 0, 60));
            Assert.Empty(turrets.Turrets);
            Assert.True(map.IsBuildable(5, 5));
        }

        [Fact]
        public void Upgrade_AppliesTierStats_HealsAndTracksInvestment_ThenCapsAtMax()
        {
            var (map, _, world) = OpenField();
            var cfg = new TurretConfig();
            var turrets = new TurretSystem(cfg);
            turrets.Place(map, 10, 16);
            turrets.Damage(map, 0, 150);
            Assert.Equal(150, turrets.Turrets[0].Hp);

            Assert.Equal("Twin .50", turrets.NextTier(0)!.Name);
            Assert.True(turrets.Upgrade(map, 0));
            var t = turrets.Turrets[0];
            Assert.Equal(1, t.Tier);
            Assert.Equal(cfg.DamagePerShot * 1.6f, t.DamagePerShot, 3);
            Assert.Equal(cfg.Range, t.Range, 3);
            Assert.Equal(250, t.Hp);
            Assert.Equal(400, t.MaxHp);
            Assert.Equal(120, t.Invested);

            Assert.True(turrets.Upgrade(map, 0));
            Assert.Equal(cfg.Range + 4f, turrets.Turrets[0].Range, 3);
            Assert.Null(turrets.NextTier(0));
            Assert.False(turrets.Upgrade(map, 0));

            // Upgraded stats are what fire.
            int far = world.Spawn(new Vec2(23.5f, 16.5f), 100f); // 13 cells: only in range after Overwatch
            var shots = new List<TurretShot>();
            turrets.Step(world, Dt, shots);
            Assert.Single(shots);
            Assert.Equal(100f - cfg.DamagePerShot * 1.6f, world.HealthOf(far), 2);
        }

        [Fact]
        public void StructureQuery_ExposesTurretsToSpitters()
        {
            var (map, _, _) = OpenField();
            var turrets = new TurretSystem(new TurretConfig());
            turrets.Place(map, 4, 4);
            turrets.Place(map, 8, 8);
            var q = turrets.AsStructureQuery();
            Assert.Equal(2, q.Count);
            Assert.Equal(8.5f, q.PositionAt(1).X, 3);
            turrets.Remove(map, 0);
            Assert.Equal(1, q.Count);
            Assert.Equal(8.5f, q.PositionAt(0).X, 3);
        }

        [Fact]
        public void Turrets_AreDeterministic_AcrossRuns()
        {
            ulong Run()
            {
                var (map, _, world) = OpenField();
                var turrets = new TurretSystem(new TurretConfig());
                turrets.Place(map, 12, 14);
                turrets.Place(map, 12, 18);
                for (int i = 0; i < 40; i++) world.Spawn(new Vec2(2f + (i % 5) * 0.5f, 10f + (i / 5) * 0.8f), 10f);
                for (int t = 0; t < 300; t++) { world.Step(Dt); turrets.Step(world, Dt, null); }
                return world.StateHash() ^ turrets.StateHash();
            }
            Assert.Equal(Run(), Run());
        }
    }
}
