#nullable enable
using System.Collections.Generic;
using Cipher.Game.Build;
using Cipher.Game.Match;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    public sealed class BuildModelTests
    {
        private sealed class Rig
        {
            public GridMap Map = null!;
            public FlowField Field = null!;
            public AgentWorld World = null!;
            public TurretSystem Turrets = null!;
            public MatchState Match = null!;
            public EconomyConfig Eco = null!;
            public BuildModel Build = null!;
            public List<(int X, int Y)> Spawns = null!;
        }

        private static Rig Make(int cash = 400)
        {
            var r = new Rig
            {
                Map = new GridMap(24, 12),
                Eco = new EconomyConfig { StartCash = cash, BarricadeCost = 20, TurretCost = 150 },
                Spawns = new List<(int X, int Y)> { (1, 6) },
            };
            r.Field = new FlowField(r.Map);
            r.Field.Compute(22, 6);
            r.World = new AgentWorld(r.Map, r.Field, new SimConfig());
            r.Turrets = new TurretSystem(new TurretConfig());
            r.Match = new MatchState(WaveTable.Default, r.Eco);
            r.Build = new BuildModel(r.Map, r.World, r.Turrets, r.Match, r.Eco, r.Spawns, 22, 6, 12, 6);
            return r;
        }

        [Test]
        public void Cursor_ClampsToGrid_AndItemsCycleBothWays()
        {
            var r = Make();
            r.Build.SetCursor(-5, 99);
            Assert.AreEqual(0, r.Build.CursorX);
            Assert.AreEqual(11, r.Build.CursorY);
            r.Build.MoveCursor(3, -20);
            Assert.AreEqual(3, r.Build.CursorX);
            Assert.AreEqual(0, r.Build.CursorY);

            Assert.AreEqual(BuildItem.Barricade, r.Build.Item);
            r.Build.CycleItem(1);
            Assert.AreEqual(BuildItem.Turret, r.Build.Item);
            r.Build.CycleItem(1);
            Assert.AreEqual(BuildItem.RepairDrone, r.Build.Item);
            r.Build.CycleItem(1);
            Assert.AreEqual(BuildItem.Barricade, r.Build.Item);
            r.Build.CycleItem(-1);
            Assert.AreEqual(BuildItem.RepairDrone, r.Build.Item);
        }

        [Test]
        public void Place_SpendsCash_BlocksCell_AndRefusesWhenBroke()
        {
            var r = Make(cash: 30);
            Assert.IsTrue(r.Build.TryPlace());
            Assert.AreEqual(10, r.Match.Bank.Cash);
            Assert.IsTrue(r.Map.IsBlocked(12, 6));
            Assert.AreEqual(WallKind.Barricade, r.Map.KindAt(12, 6));

            r.Build.MoveCursor(0, 1);
            Assert.IsFalse(r.Build.TryPlace(), "cannot afford a second barricade");
            Assert.AreEqual("need $20", r.Build.Message);
            Assert.IsFalse(r.Map.IsBlocked(12, 7));

            r.Build.MoveCursor(0, -1);
            Assert.IsFalse(r.Build.TryPlace(), "cannot stack on an existing wall");
            Assert.AreEqual(PlacementResult.NotBuildable, r.Build.LastResult);
        }

        [Test]
        public void Turret_Placement_UsesTurretSystem_AndCost()
        {
            var r = Make();
            r.Build.CycleItem(1);
            Assert.AreEqual(150, r.Build.ItemCost);
            Assert.IsTrue(r.Build.TryPlace());
            Assert.AreEqual(250, r.Match.Bank.Cash);
            Assert.AreEqual(1, r.Turrets.Turrets.Count);
            Assert.AreEqual(WallKind.Structure, r.Map.KindAt(12, 6));
        }

        [Test]
        public void Sell_RefundsFullInSetup_HalfMidWave_AndFreesCell()
        {
            var r = Make();
            r.Build.CycleItem(1);
            r.Build.TryPlace();                       // -150 → 250
            Assert.IsTrue(r.Build.TrySell());          // setup: +150
            Assert.AreEqual(400, r.Match.Bank.Cash);
            Assert.IsTrue(r.Map.IsBuildable(12, 6));
            Assert.IsEmpty(r.Turrets.Turrets);

            r.Build.CycleItem(-1);
            r.Build.TryPlace();                       // barricade -20 → 380
            r.Match.StartWaveNow();
            r.Match.Tick(0.01f, 0, 0);
            Assert.AreEqual(MatchPhase.Wave, r.Match.Phase);
            Assert.IsTrue(r.Build.TrySell());          // combat: +10
            Assert.AreEqual(390, r.Match.Bank.Cash);
            Assert.IsFalse(r.Build.TrySell(), "nothing left to sell");
        }

        [Test]
        public void Sealing_IsAllowedButLabelled_AndRouteTraceReportsIt()
        {
            var r = Make();
            for (int y = 0; y < 12; y++) { r.Build.SetCursor(12, y); if (y < 11) Assert.IsTrue(r.Build.TryPlace()); }
            // Last cell (12,11): placing it seals the spawn.
            r.Build.SetCursor(12, 11);
            Assert.AreEqual(PlacementResult.SealsSpawn, r.Build.Refresh());
            StringAssert.Contains("SEALED", r.Build.Message);

            var route = new List<Vec2>();
            Assert.IsFalse(r.Build.TraceRoute(r.Spawns[0], route), "preview shows the spawn has no path if we place here");

            Assert.IsTrue(r.Build.TryPlace(), "full seals are allowed, never refused");
            r.World.Step(1f / 30f);
            Assert.IsFalse(r.Field.HasPath(1, 6));
        }

        [Test]
        public void Preview_MatchesLive_WhenNothingCanBePlaced_AndRouteReachesGoal()
        {
            var r = Make();
            r.Build.SetCursor(12, 6);
            r.Build.TryPlace();
            r.World.Step(1f / 30f);

            r.Build.SetCursor(12, 6); // on the wall: NotBuildable
            Assert.AreEqual(PlacementResult.NotBuildable, r.Build.Refresh());
            Assert.IsTrue(r.Field.Equals(r.Build.Validator.PreviewField), "preview must show live routing when the cursor cannot place");

            var route = new List<Vec2>();
            Assert.IsTrue(r.Build.TraceRoute(r.Spawns[0], route));
            Assert.Greater(route.Count, 20);
            var (gx, gy) = r.Map.WorldToCell(route[route.Count - 1]);
            Assert.AreEqual((22, 6), (gx, gy));
        }

        [Test]
        public void Upgrade_OnHoveredTurret_SpendsCash_AndSellRefundsInvestment()
        {
            var r = Make(cash: 600);
            r.Build.CycleItem(1);
            Assert.IsTrue(r.Build.TryPlace());                 // 450
            Assert.AreEqual(0, r.Build.HoveredTurret);
            StringAssert.Contains("Twin .50 $120", r.Build.UpgradeOffer);
            Assert.AreEqual(PlacementResult.NotBuildable, r.Build.Refresh());
            StringAssert.Contains("Y upgrade", r.Build.Message);

            Assert.IsTrue(r.Build.TryUpgrade());               // 330
            Assert.AreEqual(330, r.Match.Bank.Cash);
            Assert.AreEqual(1, r.Turrets.Turrets[0].Tier);
            Assert.IsTrue(r.Build.TryUpgrade());               // 130
            Assert.IsFalse(r.Build.TryUpgrade(), "max tier");
            Assert.AreEqual("MAX tier", r.Build.Message);

            Assert.IsTrue(r.Build.TrySell());                  // setup refund: (150 + 320) x 1.0 x full hp
            Assert.AreEqual(600, r.Match.Bank.Cash);
        }

        [Test]
        public void Drone_OnlyOnBreachedCell_RepairsWithHeroNearby_ThenIsPruned()
        {
            var r = Make(cash: 400);
            r.Build.SetCursor(12, 6);
            r.Build.TryPlace();                                // barricade, 380
            r.Map.Breach(12, 6);                               // someone cracked it
            r.Build.CycleItem(1); r.Build.CycleItem(1);        // drone
            Assert.AreEqual(BuildItem.RepairDrone, r.Build.Item);
            r.Build.SetCursor(12, 7);
            Assert.AreEqual(PlacementResult.NotBuildable, r.Build.Refresh());
            Assert.IsFalse(r.Build.TryPlace(), "open floor is not a breach");
            r.Build.SetCursor(12, 6);
            Assert.AreEqual(PlacementResult.Ok, r.Build.Refresh());
            Assert.IsTrue(r.Build.TryPlace());
            Assert.AreEqual(230, r.Match.Bank.Cash);
            Assert.AreEqual(1, r.Build.Drones.Count);

            Assert.AreEqual(0, r.Build.TickDrones(new Vec2(40f, 6f), 10f), "hero too far");
            Assert.AreEqual(1, r.Build.TickDrones(new Vec2(11f, 6f), 4.1f));
            Assert.AreEqual(BreachStage.Intact, r.Map.StageAt(12, 6));
            Assert.IsEmpty(r.Build.Drones);
        }

        [Test]
        public void Occupied_IsRefused_AndPreviewStillLive()
        {
            var r = Make();
            r.World.Spawn(new Vec2(12.5f, 6.5f), 10f);
            Assert.AreEqual(PlacementResult.Occupied, r.Build.Refresh());
            Assert.IsFalse(r.Build.TryPlace());
            Assert.AreEqual(400, r.Match.Bank.Cash);
        }
    }
}
