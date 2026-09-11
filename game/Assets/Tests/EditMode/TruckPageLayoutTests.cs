#nullable enable
using System.Collections.Generic;
using Cipher.Game.UI.Comic;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The truck page's geometry and load maths.
    ///
    /// The owner played the numeric version and said "the truck is only like 4% full I think I don't
    /// understand the pack up mechanism" (2026-09-11). The fix is that the bed is drawn to scale, so
    /// the tests that matter are the ones that keep it honest: a crate's width IS its share of the
    /// hold, and the left-behind panel IS everything not in the bed.
    /// </summary>
    public sealed class TruckPageLayoutTests
    {
        private static readonly ComicRect Page = new ComicRect(0f, 0f, 1600f, 900f);

        private static List<TruckItemView> Items() => new List<TruckItemView>
        {
            new TruckItemView("Sentry", 420f, 2.0f),
            new TruckItemView("Grinder", 510f, 3.0f),
            new TruckItemView("Rotor", 60f, 1.0f),
            new TruckItemView("Gun Crate", 180f, 0.5f),
        };

        private static TruckPageLayout Build(params bool[] loaded)
        {
            var page = new TruckPageLayout(Items(), loaded, maxWeight: 1200f, maxVolume: 9f);
            page.Layout(Page);
            return page;
        }

        [Test]
        public void LeftBehindIsExactlyTheUnloadedSet()
        {
            var page = Build(true, false, true, false);

            CollectionAssert.AreEqual(new[] { "Sentry", "Rotor" }, Names(page.Bed));
            CollectionAssert.AreEqual(new[] { "Grinder", "Gun Crate" }, Names(page.LeftBehind));
            Assert.AreEqual(page.All.Count, page.Bed.Count + page.LeftBehind.Count,
                "every crate is in exactly one of the two panels");
        }

        [Test]
        public void LeftBehindTracksEveryLoadAndUnload()
        {
            var page = Build(false, false, false, false);
            Assert.AreEqual(4, page.LeftBehind.Count);
            Assert.AreEqual(0, page.Bed.Count);

            Assert.AreEqual(TruckAction.Load, page.Confirm());
            Assert.AreEqual(3, page.LeftBehind.Count);
            Assert.AreEqual(1, page.Bed.Count);
            CollectionAssert.DoesNotContain(Names(page.LeftBehind), "Sentry");

            page.MoveUp(); // into the bed, onto the crate just loaded
            Assert.AreEqual(TruckZone.Bed, page.Zone);
            Assert.AreEqual(TruckAction.Unload, page.Confirm());
            Assert.AreEqual(4, page.LeftBehind.Count);
            Assert.AreEqual(0, page.Bed.Count);
        }

        [Test]
        public void CrateWidthIsItsShareOfTheHold()
        {
            var page = Build(true, false, true, false); // Sentry 2.0 m3, Rotor 1.0 m3

            var sentry = page.Bed[0];
            var rotor = page.Bed[1];
            Assert.AreEqual(2f * rotor.Box.W, sentry.Box.W, 0.01f,
                "twice the volume must be twice the width, or the bed lies about what fits");

            // And the scale is absolute, not relative to what happens to be loaded: a crate filling
            // the whole hold would be exactly as wide as the bed.
            float perCubicMetre = sentry.Box.W / 2f;
            Assert.AreEqual(page.BedInner.W, perCubicMetre * page.MaxVolume, 0.01f);
        }

        [Test]
        public void CratesSitInsideTheBedAndDoNotOverlap()
        {
            var page = Build(true, true, true, true); // 6.5 of 9 m3

            foreach (var crate in page.Bed)
                Assert.IsTrue(crate.Box.IsInside(page.BedInner), $"{crate.Item.Name} is hanging out of the bed");

            for (int i = 0; i < page.Bed.Count; i++)
                for (int j = i + 1; j < page.Bed.Count; j++)
                    Assert.IsFalse(page.Bed[i].Box.Overlaps(page.Bed[j].Box),
                        "crates may abut, never overlap");
        }

        [Test]
        public void TheTwoPanelsCarryEqualVisualWeight()
        {
            // ROADMAP §5.3: "what gets left behind drawn with equal weight — the decision is about
            // loss, not capacity." Equal here is literal, and asserted so nobody quietly shrinks it.
            var page = Build(true, false, false, false);
            Assert.AreEqual(page.TruckPanel.W, page.LeftBehindPanel.W, 0.01f);
            Assert.AreEqual(page.TruckPanel.H, page.LeftBehindPanel.H, 0.01f);
            Assert.IsFalse(page.TruckPanel.Overlaps(page.LeftBehindPanel));
        }

        [Test]
        public void GaugesReadTheLoadNotTheCount()
        {
            var page = Build(true, false, false, true); // 600 kg, 2.5 m3

            Assert.AreEqual(600f, page.Weight, 0.01f);
            Assert.AreEqual(2.5f, page.Volume, 0.01f);
            Assert.AreEqual(0.5f, page.WeightFraction, 0.001f);
            Assert.AreEqual(2.5f / 9f, page.VolumeFraction, 0.001f);
        }

        [Test]
        public void GaugesAndPanelsDoNotCollide()
        {
            var page = Build(true, false, false, false);
            Assert.IsFalse(page.WeightGauge.Overlaps(page.VolumeGauge));
            Assert.IsFalse(page.WeightGauge.Overlaps(page.TruckPanel));
            Assert.IsFalse(page.VolumeGauge.Overlaps(page.LeftBehindPanel));
            Assert.IsFalse(page.CaptionRect.Overlaps(page.TruckPanel));
        }

        [Test]
        public void ATruckThatCannotTakeItSaysSoRatherThanDoingNothing()
        {
            // A bed with room for the rotor and nothing bigger.
            var page = new TruckPageLayout(Items(), new[] { false, false, false, false },
                                           maxWeight: 1200f, maxVolume: 1.2f);
            page.Layout(Page);

            Assert.AreEqual(TruckZone.LeftBehind, page.Zone);
            Assert.AreEqual(TruckAction.Blocked, page.Confirm(), "the Sentry is 2.0 m3 and the hold is 1.2");
            Assert.AreEqual(0, page.Bed.Count, "a blocked load must not half-happen");

            page.MoveRight();
            page.MoveRight(); // the Rotor, 1.0 m3
            Assert.AreEqual(TruckAction.Load, page.Confirm());
            Assert.AreEqual(1, page.Bed.Count);
        }

        [Test]
        public void WeightBlocksJustAsVolumeDoes()
        {
            var page = new TruckPageLayout(Items(), new[] { false, false, false, false },
                                           maxWeight: 100f, maxVolume: 9f);
            page.Layout(Page);
            Assert.AreEqual(TruckAction.Blocked, page.Confirm(), "the Sentry is 420 kg and the axle takes 100");
        }

        [Test]
        public void LeftRightWrapsWithinAZone()
        {
            var page = Build(false, false, false, false);
            Assert.AreEqual(TruckZone.LeftBehind, page.Zone);
            Assert.AreEqual(0, page.Row);

            page.MoveLeft();
            Assert.AreEqual(3, page.Row, "left from the first crate wraps to the last");

            page.MoveRight();
            Assert.AreEqual(0, page.Row);
        }

        [Test]
        public void AnEmptyBedNeverSwallowsTheCursor()
        {
            var page = Build(false, false, false, false);
            page.MoveUp(); // there is nothing in the bed to cross to
            Assert.AreEqual(TruckZone.LeftBehind, page.Zone);
            Assert.IsNotNull(page.Selected);
        }

        [Test]
        public void EmptyingTheZoneMovesTheCursorToTheOtherPanel()
        {
            var page = Build(true, false, false, false);
            Assert.AreEqual(TruckZone.Bed, page.Zone);

            Assert.AreEqual(TruckAction.Unload, page.Confirm());
            Assert.AreEqual(TruckZone.LeftBehind, page.Zone,
                "unloading the last crate must not leave the cursor on an empty bed");
            Assert.IsNotNull(page.Selected);
        }

        [Test]
        public void HeavierPerCubicMetreMeansADarkerCrate()
        {
            // One visual variable per quantity: width is volume, print density is weight.
            var page = Build(true, false, true, false);
            var sentry = page.Bed[0];  // 420 kg / 2.0 m3 = 210
            var rotor = page.Bed[1];   //  60 kg / 1.0 m3 =  60
            Assert.Greater(sentry.ShadeDensity, rotor.ShadeDensity);
            Assert.LessOrEqual(sentry.ShadeDensity, 1f);
            Assert.GreaterOrEqual(rotor.ShadeDensity, 0f);
        }

        [Test]
        public void BackAlwaysCloses()
        {
            Assert.AreEqual(TruckAction.Close, Build(true, false, false, false).Back());
        }

        private static string[] Names(IReadOnlyList<TruckCrate> crates)
        {
            var names = new string[crates.Count];
            for (int i = 0; i < crates.Count; i++) names[i] = crates[i].Item.Name;
            return names;
        }
    }
}
