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
        public void SlabWidthIsItsShareOfTheHold()
        {
            // The SLAB is the to-scale picture of the bed; the Box is the labelled card. Splitting
            // them is what let every item get a name (the owner: "nothing was labeled"), and this
            // test is the half that keeps the picture honest.
            var page = Build(true, false, true, false); // Sentry 2.0 m3, Rotor 1.0 m3

            var sentry = page.Bed[0];
            var rotor = page.Bed[1];
            Assert.AreEqual(2f * rotor.Slab.W, sentry.Slab.W, 0.01f,
                "twice the volume must be twice the width, or the bed lies about what fits");

            // And the scale is absolute, not relative to what happens to be loaded: a crate filling
            // the whole hold would be exactly as wide as the bed.
            float perCubicMetre = sentry.Slab.W / 2f;
            Assert.AreEqual(page.BedInner.W, perCubicMetre * page.MaxVolume, 0.01f);
        }

        [Test]
        public void SlabsSitInsideTheBedAndDoNotOverlap()
        {
            var page = Build(true, true, true, true); // 6.5 of 9 m3

            foreach (var crate in page.Bed)
                Assert.IsTrue(crate.Slab.IsInside(page.BedInner), $"{crate.Item.Name} is hanging out of the bed");

            for (int i = 0; i < page.Bed.Count; i++)
                for (int j = i + 1; j < page.Bed.Count; j++)
                    Assert.IsFalse(page.Bed[i].Slab.Overlaps(page.Bed[j].Slab),
                        "slabs may abut, never overlap");
        }

        [Test]
        public void ACrateOnTheGravelHasNoSlab()
        {
            var page = Build(true, false, false, false);
            Assert.IsFalse(page.Bed[0].Slab.IsEmpty, "the loaded one is drawn in the bed");
            foreach (var crate in page.LeftBehind)
                Assert.IsTrue(crate.Slab.IsEmpty, $"{crate.Item.Name} is on the gravel and must not draw in the bed");
        }

        [Test]
        public void EveryCrateGetsACardBigEnoughToLabel()
        {
            // THE OWNER'S SECOND COMPLAINT, pinned. "I also have no idea what I packed the truck
            // with there was nothing was labeled." Under the old layout a card's width WAS its
            // volume, so a 0.5 m3 rotor got fourteen pixels and went unnamed. Every card now has to
            // carry a name, two numbers and a value.
            var page = Build(true, false, true, false);

            foreach (var crate in page.All)
            {
                Assert.IsFalse(crate.Box.IsEmpty, $"{crate.Item.Name} has no card at all");
                Assert.GreaterOrEqual(crate.Box.W, 120f, $"{crate.Item.Name} has a card too narrow to name it");
                Assert.GreaterOrEqual(crate.Box.H, 40f, $"{crate.Item.Name} has a card that cannot hold three lines");
            }
        }

        [Test]
        public void CardsStayInsideTheirPanelAndDoNotOverlap()
        {
            var page = Build(true, true, false, false);

            foreach (var c in page.Bed)
                Assert.IsTrue(c.Box.IsInside(page.TruckPanel), $"{c.Item.Name} has a card outside the truck panel");
            foreach (var c in page.LeftBehind)
                Assert.IsTrue(c.Box.IsInside(page.LeftBehindPanel), $"{c.Item.Name} has a card outside the gravel panel");

            var all = new List<TruckCrate>(page.All);
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                    Assert.IsFalse(all[i].Box.Overlaps(all[j].Box), "two cards are printed on top of each other");
        }

        [Test]
        public void ACardIsNeverPushedOffThePanel()
        {
            // Columns grow rather than cards vanishing. A crate the player cannot see is a crate
            // they cannot choose, and choosing is the whole screen.
            var many = new List<TruckItemView>();
            for (int i = 0; i < 24; i++) many.Add(new TruckItemView($"Emplacement {i}", 40f, 0.3f, 50));

            var page = new TruckPageLayout(many, null, maxWeight: 1200f, maxVolume: 9f);
            page.Layout(Page);

            foreach (var c in page.All)
                Assert.IsFalse(c.Box.IsEmpty, $"{c.Item.Name} was pushed off the page");
        }

        [Test]
        public void EveryCrateCarriesItsManifestNumber()
        {
            var page = Build(false, false, false, false);
            for (int i = 0; i < page.All.Count; i++)
                Assert.AreEqual(i + 1, page.All[i].Tag, "tags are 1-based and tie a bed slab to its card");
        }

        [Test]
        public void TheLedgerCountsMoneyOnBothSidesOfTheChoice()
        {
            var items = new List<TruckItemView>
            {
                new TruckItemView("Sentry", 420f, 2.0f, 260),
                new TruckItemView("Grinder", 510f, 3.0f, 300),
            };
            var page = new TruckPageLayout(items, new[] { true, false }, 1200f, 9f);
            page.Layout(Page);

            Assert.AreEqual(260, page.LoadedValue);
            Assert.AreEqual(300, page.AbandonedValue,
                "what is staying is the number this screen is an argument about");
        }

        [Test]
        public void UpAndDownWalkTheGridBeforeLeavingIt()
        {
            var page = Build(false, false, false, false); // four on the gravel
            Assert.AreEqual(TruckZone.LeftBehind, page.Zone);
            Assert.AreEqual(0, page.Row);

            page.MoveDown();
            Assert.AreEqual(page.GravelColumns, page.Row, "down moves a whole row, not one card");

            page.MoveUp();
            Assert.AreEqual(0, page.Row);

            // And off the top of the grid it crosses to the other panel -- or stays put when the
            // other panel is empty, which is the rule that stops the cursor being swallowed.
            page.MoveUp();
            Assert.AreEqual(TruckZone.LeftBehind, page.Zone);
            Assert.IsNotNull(page.Selected);
        }

        [Test]
        public void TooHeavyNamesTheLimitThatRefused()
        {
            var items = new List<TruckItemView> { new TruckItemView("Anvil", 900f, 0.2f, 10) };
            var page = new TruckPageLayout(items, null, maxWeight: 100f, maxVolume: 9f);
            page.Layout(Page);
            Assert.IsTrue(page.TooHeavy(page.LeftBehind[0]), "weight refused it, and the page must say so");

            var bulky = new TruckPageLayout(
                new List<TruckItemView> { new TruckItemView("Panels", 10f, 8f, 10) },
                null, maxWeight: 1200f, maxVolume: 1f);
            bulky.Layout(Page);
            Assert.IsFalse(bulky.TooHeavy(bulky.LeftBehind[0]), "this one is bulk, not weight");
        }

        [Test]
        public void RoofStaysInsideThePanelAtEveryShape()
        {
            // THE CAB ESCAPED THE PANEL and printed its window band over the panel title, because
            // the body height was derived from the elevation without allowing for the wheels
            // hanging below the body. It only showed up in a screenshot. The arithmetic is worth
            // one test at several page shapes, because the failure is invisible to every other one.
            float[] widths = { 900f, 1280f, 1600f, 2200f };
            float[] heights = { 620f, 720f, 900f, 1400f };

            foreach (float w in widths)
            foreach (float h in heights)
            {
                var page = new TruckPageLayout(Items(), new[] { true, true, false, false }, 1200f, 9f);
                page.Layout(new ComicRect(0f, 0f, w, h));

                Assert.IsTrue(page.CabRect.IsInside(page.TruckPanel),
                    $"cab escapes the panel at {w}x{h}");
                Assert.IsTrue(page.HoodRect.IsInside(page.TruckPanel),
                    $"bonnet escapes the panel at {w}x{h}");
                Assert.IsTrue(page.BedRect.IsInside(page.TruckPanel),
                    $"bed escapes the panel at {w}x{h}");
                Assert.Less(page.GroundY, page.TruckPanel.Bottom, $"the ground is off-panel at {w}x{h}");

                // And the drawing must not collide with the cards it sits above, nor with the line
                // that says the kit rides in the cab.
                Assert.Greater(page.CabNoteRect.Y, page.GroundY, $"the cab note is on the ground line at {w}x{h}");
                foreach (var c in page.Bed)
                    Assert.Greater(c.Box.Y, page.GroundY, $"a card is drawn over the truck at {w}x{h}");
            }
        }

        [Test]
        public void TheCabIsAPickupNotABoxVan()
        {
            // Three volumes with the bonnet lower than the cab and a bed lower still. If any of
            // those inverts, the silhouette stops being the vehicle ADR-009 is about.
            var page = Build(true, false, false, false);

            Assert.Less(page.HoodRect.Y, page.BedRect.Y, "the bonnet sits above the bed wall");
            Assert.Greater(page.HoodRect.Y, page.CabRect.Y, "the cab is the tallest volume");
            Assert.AreEqual(page.CabRect.Right, page.BedRect.X, 0.01f, "the bed starts where the cab ends");
            Assert.AreEqual(page.HoodRect.Right, page.CabRect.X, 0.01f, "the cab starts where the bonnet ends");

            float length = page.BedRect.Right - page.HoodRect.X;
            float height = page.HoodRect.Bottom - page.CabRect.Y;
            Assert.That(length / height, Is.InRange(2.6f, 4.0f), "a four-door pickup, not a bus and not a box");
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
