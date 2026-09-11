#nullable enable
using System.Collections.Generic;
using Cipher.Game.Hero;
using Cipher.Game.Match;
using Cipher.Game.Progression;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The owner's 2026-09-11 checkpoint list: untimed opening, truck extraction, adrenaline focus,
    /// levels and the skill tree. See docs/design/owner-requests-2026-09-11.md.
    /// </summary>
    public sealed class CheckpointSystemsTests
    {
        // ---------------------------------------------------------------- untimed opening

        private static MatchState Match(bool untimed = true)
            => new MatchState(
                new[] { new WaveDef(2, 100f, 20f), new WaveDef(2, 100f, 20f) },
                new EconomyConfig(),
                cycle: new ScanCycleConfig { MinWavesBeforeExtract = 0 },
                openingIsUntimed: untimed);

        [Test]
        public void TheOpeningWaitsForThePlayerForever()
        {
            var m = Match();
            Assert.IsTrue(m.AwaitingStart);
            for (int i = 0; i < 500; i++) m.Tick(1f, 0, 0);

            Assert.IsTrue(m.AwaitingStart, "500 seconds and it is still waiting");
            Assert.AreEqual(MatchPhase.Setup, m.Phase);
        }

        [Test]
        public void TakingYourTimeSettingUpDoesNotEatTheScanCycle()
        {
            var m = Match();
            float prepBefore = m.PrepSecondsRemaining;
            for (int i = 0; i < 120; i++) m.Tick(1f, 0, 0);
            Assert.AreEqual(prepBefore, m.PrepSecondsRemaining, 0.001f);
        }

        [Test]
        public void PressingStartReleasesTheOpening()
        {
            var m = Match();
            m.StartWaveNow();
            Assert.IsFalse(m.AwaitingStart);
            m.Tick(0.1f, 0, 0);
            Assert.AreEqual(MatchPhase.Wave, m.Phase);
        }

        [Test]
        public void LaterSetupsAreStillOnAClock()
        {
            var m = Match();
            m.StartWaveNow();
            for (int i = 0; i < 200 && m.Phase == MatchPhase.Setup; i++) m.Tick(0.5f, 0, 0);
            while (m.Phase == MatchPhase.Wave && !m.WaveFullySpawned) m.Tick(0.1f, 1, 0);
            m.Tick(0.1f, 0, 0);                       // wave clears, back to setup

            Assert.AreEqual(MatchPhase.Setup, m.Phase);
            Assert.IsFalse(m.AwaitingStart, "only the opening is untimed");
            float before = m.SetupTimeLeft;
            m.Tick(1f, 0, 0);
            Assert.Less(m.SetupTimeLeft, before, "the between-wave clock runs");
        }

        [Test]
        public void OptingOutOfTheUntimedOpeningRestoresTheOldBehaviour()
        {
            var m = Match(untimed: false);
            Assert.IsFalse(m.AwaitingStart);
            float before = m.SetupTimeLeft;
            m.Tick(1f, 0, 0);
            Assert.Less(m.SetupTimeLeft, before);
        }

        // ---------------------------------------------------------------- truck

        private static SalvagedEmplacement Sentry()
            => new SalvagedEmplacement("Sentry .50", new Haulage(190f, 1.1f), 150);

        private static SalvagedEmplacement Rotor()
            => new SalvagedEmplacement("Chop-Shop Rotor", new Haulage(70f, 0.9f), 120);

        private static SalvagedEmplacement Panel()
            => new SalvagedEmplacement("Barricade Panel", new Haulage(15f, 1.6f), 20);

        [Test]
        public void ThingsLoadUntilAThresholdIsHit()
        {
            var truck = new TruckLoad(maxWeight: 400f, maxVolume: 10f);
            Assert.AreEqual(LoadOutcome.Loaded, truck.TryLoad(Sentry()));
            Assert.AreEqual(LoadOutcome.Loaded, truck.TryLoad(Sentry()));
            Assert.AreEqual(LoadOutcome.TooHeavy, truck.TryLoad(Sentry()), "570kg into a 400kg bed");
        }

        [Test]
        public void VolumeIsASeparateLimitFromWeight()
        {
            // Panels are light and enormous: a bed full of them weighs almost nothing.
            var truck = new TruckLoad(maxWeight: 2000f, maxVolume: 3.5f);
            Assert.AreEqual(LoadOutcome.Loaded, truck.TryLoad(Panel()));
            Assert.AreEqual(LoadOutcome.Loaded, truck.TryLoad(Panel()));
            Assert.AreEqual(LoadOutcome.TooBulky, truck.TryLoad(Panel()));
            Assert.Less(truck.Weight, 50f, "nowhere near the weight limit, and still full");
        }

        [Test]
        public void TheGaugeShowsWhicheverLimitIsClosestToFull()
        {
            var truck = new TruckLoad(maxWeight: 1000f, maxVolume: 2f);
            truck.TryLoad(Panel());                    // 15kg, 1.6 m3
            Assert.AreEqual(0.8f, truck.FullnessFraction, 0.001f, "volume dominates");
        }

        [Test]
        public void UnloadingFreesBothLimits()
        {
            var truck = new TruckLoad(400f, 10f);
            var sentry = Sentry();
            truck.TryLoad(sentry);
            Assert.IsTrue(truck.Unload(sentry));
            Assert.AreEqual(0f, truck.Weight, 0.001f);
            Assert.AreEqual(0f, truck.Volume, 0.001f);
        }

        [Test]
        public void TheSameThingCannotBeLoadedTwice()
        {
            var truck = new TruckLoad();
            var sentry = Sentry();
            truck.TryLoad(sentry);
            Assert.AreEqual(LoadOutcome.AlreadyLoaded, truck.TryLoad(sentry));
        }

        [Test]
        public void AutoLoadPrefersValueForSpace()
        {
            var truck = new TruckLoad(maxWeight: 300f, maxVolume: 2.2f);
            int loaded = truck.AutoLoad(new List<IHaulable> { Panel(), Rotor(), Rotor(), Sentry() });

            Assert.Greater(loaded, 0);
            Assert.LessOrEqual(truck.Weight, truck.MaxWeight);
            Assert.LessOrEqual(truck.Volume, truck.MaxVolume);
            Assert.Greater(truck.TotalValue, 100, "it should not fill the bed with panels");
        }

        [Test]
        public void GearIsNeverTheThingYouHaveToLeaveBehind()
        {
            var truck = new TruckLoad(maxWeight: 1200f, maxVolume: 9f);
            var roller = new ItemRoller(3);
            for (int i = 0; i < 20; i++)
            {
                var item = roller.Roll(Slot.Vest, Rarity.Issued, 10);
                Assert.AreEqual(LoadOutcome.Loaded, truck.TryLoad(new HauledItem(item)));
            }
            Assert.Less(truck.FullnessFraction, 0.3f, "20 pieces of gear barely dents the bed");
        }

        // ---------------------------------------------------------------- adrenaline focus

        [Test]
        public void FocusStartsFullAndSlowsTimeWhenEngaged()
        {
            var f = new AdrenalineFocus();
            Assert.AreEqual(2, f.Charges);
            Assert.AreEqual(1f, f.TimeScale, 0.001f);

            Assert.IsTrue(f.TryEngage());
            Assert.AreEqual(f.SlowFactor, f.TimeScale, 0.001f);
            Assert.AreEqual(1, f.Charges);
        }

        [Test]
        public void TwoChargesThenItIsDepleted()
        {
            var f = new AdrenalineFocus();
            Assert.IsTrue(f.TryEngage()); f.Release();
            Assert.IsTrue(f.TryEngage()); f.Release();
            Assert.IsFalse(f.TryEngage(), "third open gets no slow");
            Assert.IsTrue(f.IsDepleted);
            Assert.AreEqual(1f, f.TimeScale, 0.001f, "build mode still opens, just at full speed");
        }

        [Test]
        public void AChargeRunsOutOnItsOwn()
        {
            var f = new AdrenalineFocus { ChargeSeconds = 4f };
            f.TryEngage();
            f.Tick(3.9f);
            Assert.IsTrue(f.IsActive);
            f.Tick(0.2f);
            Assert.IsFalse(f.IsActive);
            Assert.AreEqual(1f, f.TimeScale, 0.001f);
        }

        [Test]
        public void FocusRechargesOnlyWhileNotBurning()
        {
            var f = new AdrenalineFocus { RechargeSeconds = 10f };
            f.TryEngage();
            f.Tick(30f);                       // burns out, then recharges
            Assert.AreEqual(2, f.Charges);
        }

        [Test]
        public void AGlanceCostsTheSameAsUsingItProperly()
        {
            var f = new AdrenalineFocus();
            f.TryEngage();
            f.Release();
            Assert.AreEqual(1, f.Charges, "releasing early banks nothing");
        }

        [Test]
        public void RechargeNeverOverfills()
        {
            var f = new AdrenalineFocus { RechargeSeconds = 1f };
            f.Tick(500f);
            Assert.AreEqual(f.MaxCharges, f.Charges);
            Assert.AreEqual(1f, f.RechargeFraction, 0.001f);
        }

        [Test]
        public void ResetRefillsAtANewPosition()
        {
            var f = new AdrenalineFocus();
            f.TryEngage(); f.Release();
            f.TryEngage(); f.Release();
            f.Reset();
            Assert.AreEqual(f.MaxCharges, f.Charges);
            Assert.IsFalse(f.IsActive);
        }

        // ---------------------------------------------------------------- levels and tree

        [Test]
        public void ExperienceRaisesLevelAndGrantsPoints()
        {
            var s = new SkillState();
            Assert.AreEqual(1, s.Level);
            Assert.AreEqual(0, s.UnspentPoints);

            int gained = s.AddXp(LevelCurve.TotalXpFor(3));
            Assert.AreEqual(3, s.Level);
            Assert.AreEqual(2, gained);
            Assert.AreEqual(2, s.UnspentPoints);
        }

        [Test]
        public void EveryFifthLevelPaysDouble()
        {
            Assert.AreEqual(1, LevelCurve.PointsForLevel(4));
            Assert.AreEqual(2, LevelCurve.PointsForLevel(5));
            Assert.AreEqual(2, LevelCurve.PointsForLevel(10));
        }

        [Test]
        public void TheCurveIsMonotonicAndCapped()
        {
            for (int l = 1; l < LevelCurve.MaxLevel; l++)
                Assert.Less(LevelCurve.TotalXpFor(l), LevelCurve.TotalXpFor(l + 1));

            var s = new SkillState();
            s.AddXp(100_000_000);
            Assert.AreEqual(LevelCurve.MaxLevel, s.Level);
        }

        [Test]
        public void TheThinkingArchetypesAreWorthFarMoreExperience()
        {
            Assert.Greater(LevelCurve.XpForKill(true, false), LevelCurve.XpForKill(false, false) * 10);
            Assert.Greater(LevelCurve.XpForKill(false, true), LevelCurve.XpForKill(false, false) * 5);
        }

        [Test]
        public void ExtractionPaysMoreTheLongerYouHeld()
        {
            Assert.Greater(LevelCurve.XpForExtraction(5), LevelCurve.XpForExtraction(1),
                "otherwise calling the last wave immediately is always optimal");
        }

        [Test]
        public void ANodeNeedsPointsAndItsPrerequisite()
        {
            var s = new SkillState();
            Assert.AreEqual(SpendResult.NotEnoughPoints, s.Spend("t-marks"));

            s.AddXp(LevelCurve.TotalXpFor(6));
            Assert.AreEqual(SpendResult.PrerequisiteMissing, s.Spend("t-cadence"));
            Assert.AreEqual(SpendResult.Ok, s.Spend("t-marks"));
            Assert.AreEqual(SpendResult.Ok, s.Spend("t-cadence"));
        }

        [Test]
        public void UnknownNodesAreRefused()
        {
            var s = new SkillState();
            s.AddXp(100000);
            Assert.AreEqual(SpendResult.UnknownNode, s.Spend("not-a-node"));
        }

        [Test]
        public void RanksStackUpToTheirMaximum()
        {
            var s = new SkillState();
            s.AddXp(LevelCurve.TotalXpFor(20));
            for (int i = 0; i < 5; i++) Assert.AreEqual(SpendResult.Ok, s.Spend("t-marks"));
            Assert.AreEqual(SpendResult.AtMaxRank, s.Spend("t-marks"));
            Assert.AreEqual(5, s.RankOf("t-marks"));
        }

        [Test]
        public void RankedNodesApplyOncePerRank()
        {
            var s = new SkillState();
            s.AddXp(LevelCurve.TotalXpFor(20));
            for (int i = 0; i < 3; i++) s.Spend("t-marks");
            Assert.AreEqual(0.12f, s.TotalStats().Percent(StatKind.GunDamage), 0.0001f);
        }

        [Test]
        public void RespecReturnsEveryPointEverEarned()
        {
            var s = new SkillState();
            s.AddXp(LevelCurve.TotalXpFor(10));
            int lifetime = s.UnspentPoints;
            s.Spend("t-marks");
            s.Spend("t-marks");
            Assert.Less(s.UnspentPoints, lifetime);

            s.Respec();
            Assert.AreEqual(lifetime, s.UnspentPoints);
            Assert.AreEqual(0, s.RankOf("t-marks"));
        }

        [Test]
        public void EveryCatalogueNodeDoesSomethingAndItsPrerequisiteExists()
        {
            foreach (var node in SkillCatalogue.All)
            {
                var b = new StatBlock();
                node.ApplyTo(b, 1);
                Assert.IsFalse(b.IsEmpty, $"{node.Id} does nothing");
                if (node.Requires != null)
                    Assert.IsNotNull(SkillCatalogue.Find(node.Requires), $"{node.Id} needs a missing node");
            }
        }

        [Test]
        public void TheTreeReachesTheHeroAlongsideGearAndCards()
        {
            var skills = new SkillState();
            skills.AddXp(LevelCurve.TotalXpFor(20));
            var loadout = new Loadout(new HeroConfig(), null, null, skills);
            float baseDamage = loadout.Effective.GunDamage;

            Assert.AreEqual(SpendResult.Ok, loadout.SpendSkillPoint("t-marks"));
            Assert.AreEqual(baseDamage * 1.04f, loadout.Effective.GunDamage, 0.001f);

            loadout.Pickup(new ItemInstance(Slot.Weapon, Rarity.Issued, 10,
                new[] { new Affix(AffixKind.HandLoaded, 20f) }, "test"));

            // 4% from the tree plus 20% from gear, added not compounded.
            Assert.AreEqual(baseDamage * 1.24f, loadout.Effective.GunDamage, 0.001f);
        }

        [Test]
        public void KeystonesSitAtTheEndOfTheirChain()
        {
            foreach (var node in SkillCatalogue.All)
                if (node.IsKeystone)
                {
                    Assert.IsNotNull(node.Requires, $"{node.Id} is a keystone with no chain");
                    Assert.AreEqual(1, node.MaxRank, $"{node.Id} keystone should not stack");
                }
        }

        // ---------------------------------------------------------------- build wheel

        [Test]
        public void TwelveOClockIsTheFirstOption()
        {
            var w = new Cipher.Game.Build.BuildWheel();
            w.Open(4);
            w.Aim(0f, 1f);
            Assert.AreEqual(0, w.Selected);
        }

        [Test]
        public void TheWheelGoesClockwise()
        {
            var w = new Cipher.Game.Build.BuildWheel();
            w.Open(4);
            w.Aim(1f, 0f);   // three o'clock
            Assert.AreEqual(1, w.Selected);
            w.Aim(0f, -1f);  // six o'clock
            Assert.AreEqual(2, w.Selected);
            w.Aim(-1f, 0f);  // nine o'clock
            Assert.AreEqual(3, w.Selected);
        }

        [Test]
        public void WedgesAreCentredOnTheirOption()
        {
            // With four options each wedge is 90 degrees, so 44 degrees off twelve is still option 0.
            var w = new Cipher.Game.Build.BuildWheel();
            w.Open(4);
            w.Aim(0.7f, 0.72f);
            Assert.AreEqual(0, w.Selected);
        }

        [Test]
        public void TheDeadZoneKeepsTheLastChoiceRatherThanClearingIt()
        {
            var w = new Cipher.Game.Build.BuildWheel();
            w.Open(4);
            w.Aim(1f, 0f);
            Assert.AreEqual(1, w.Selected);

            w.Aim(0.05f, 0.02f);       // thumb drifting back to centre
            Assert.AreEqual(1, w.Selected, "a returning thumb must not cancel the pick");
            Assert.IsFalse(w.HasAim);
        }

        [Test]
        public void ClosingReportsTheHighlightedOption()
        {
            var w = new Cipher.Game.Build.BuildWheel();
            w.Open(3, startSelection: 2);
            Assert.AreEqual(2, w.Close());
            Assert.IsFalse(w.IsOpen);
        }

        [Test]
        public void AWheelWithNothingInItNeverOpens()
        {
            var w = new Cipher.Game.Build.BuildWheel();
            w.Open(0);
            Assert.IsFalse(w.IsOpen);
            w.Aim(0f, 1f);
            Assert.AreEqual(-1, w.Selected);
        }

        [Test]
        public void SteppingWrapsBothWays()
        {
            var w = new Cipher.Game.Build.BuildWheel();
            w.Open(3, startSelection: 0);
            w.Step(-1);
            Assert.AreEqual(2, w.Selected);
            w.Step(1);
            Assert.AreEqual(0, w.Selected);
        }

        [Test]
        public void EveryAngleLandsOnSomeOption()
        {
            for (int count = 2; count <= 8; count++)
                for (int deg = 0; deg < 360; deg++)
                {
                    float rad = deg * Mathf.Deg2Rad;
                    int idx = Cipher.Game.Build.BuildWheel.IndexForAngle(rad, count);
                    Assert.That(idx, Is.InRange(0, count - 1), $"{deg} deg, {count} options");
                }
        }

        // ---------------------------------------------------------------- intent split

        private static DirectorView View(int turrets)
            => new DirectorView(60f, 0, 0, 0, false, turrets);

        [Test]
        public void MostBodiesStillComeForTheObjective()
        {
            var d = new SpawnDirector(new DirectorConfig(), 1234);
            int vault = 0, hunters = 0, wreckers = 0;
            for (int i = 0; i < 2000; i++)
                switch (d.DecideIntent(View(3)))
                {
                    case Cipher.Sim.Agents.Intent.HuntStructure: hunters++; break;
                    case Cipher.Sim.Agents.Intent.WreckWall: wreckers++; break;
                    default: vault++; break;
                }

            Assert.Greater(vault, hunters + wreckers,
                "the split should make the crowd unpredictable, not besiege the turret line");
            Assert.Greater(hunters, 0);
            Assert.Greater(wreckers, 0);
        }

        [Test]
        public void NobodyHuntsAGunThatIsNotThere()
        {
            var d = new SpawnDirector(new DirectorConfig(), 99);
            for (int i = 0; i < 500; i++)
                Assert.AreNotEqual(Cipher.Sim.Agents.Intent.HuntStructure, d.DecideIntent(View(0)));
        }

        [Test]
        public void TheSplitIsReproducibleForASeed()
        {
            var a = new SpawnDirector(new DirectorConfig(), 7);
            var b = new SpawnDirector(new DirectorConfig(), 7);
            for (int i = 0; i < 200; i++)
                Assert.AreEqual(a.DecideIntent(View(2)), b.DecideIntent(View(2)));
        }
    }
}
