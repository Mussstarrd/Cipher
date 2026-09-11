#nullable enable
using System.Collections.Generic;
using Cipher.Game.UI.Comic;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The kit page's geometry and cursor. These are the assertions that keep the paper doll a
    /// picture: if a slot box lands on the figure, or a leader line points into space, the screen
    /// stops being readable and goes straight back to being the list the owner could not parse.
    /// </summary>
    public sealed class KitPageLayoutTests
    {
        private static readonly string[] Slots =
        {
            "Helm", "Vest", "Gloves", "CharmA",
            "Weapon", "Boots", "CharmB", "DogTag",
        };

        private static readonly ComicRect Page = new ComicRect(0f, 0f, 1600f, 900f);

        private static KitPageLayout Build(bool withPack = true)
        {
            var equipped = new List<KitItem>
            {
                new KitItem("Watch Cap", "Helm", 8f),
                new KitItem("Plated Vest", "Vest", 20f),
                new KitItem("Service Rifle", "Weapon", 30f),
            };

            var pack = withPack
                ? new List<KitItem>
                {
                    new KitItem("Ranch Coat", "Vest", 27.5f),      // +7.5 over the vest
                    new KitItem("Sidearm", "Weapon", 24f),         // -6.0 against the rifle
                    new KitItem("Better Coat", "Vest", 31f),       // the best vest in the pack
                    new KitItem("Lake Stone", "CharmA", 4f),       // fills an empty slot
                }
                : new List<KitItem>();

            var layout = new KitPageLayout(Slots, equipped, pack);
            layout.Layout(Page);
            return layout;
        }

        [Test]
        public void SlotBoxesNeverTouchTheFigureOrEachOther()
        {
            var page = Build();

            foreach (var slot in page.Slots)
            {
                Assert.IsFalse(slot.Box.Overlaps(page.FigureRect),
                    $"{slot.SlotName} box sits on the body; the figure must stay clear");
                Assert.IsTrue(slot.Box.IsInside(page.DollPanel),
                    $"{slot.SlotName} box escaped its panel");
            }

            for (int i = 0; i < page.Slots.Count; i++)
            {
                for (int j = i + 1; j < page.Slots.Count; j++)
                {
                    Assert.IsFalse(page.Slots[i].Box.Overlaps(page.Slots[j].Box),
                        $"{page.Slots[i].SlotName} overlaps {page.Slots[j].SlotName}");
                }
            }
        }

        [Test]
        public void EveryLeaderLineLandsOnTheBody()
        {
            var page = Build();
            foreach (var slot in page.Slots)
            {
                Assert.IsTrue(page.FigureRect.Contains(slot.LeaderTo),
                    $"{slot.SlotName}'s leader line points off the figure");
                Assert.IsTrue(slot.Box.Contains(slot.LeaderFrom),
                    $"{slot.SlotName}'s leader line does not start at its box");
            }
        }

        [Test]
        public void LeaderLinesDoNotCrossWithinAColumn()
        {
            // Anchors are assigned by row, so y must increase monotonically down each column. That
            // ordering IS the no-crossing guarantee; assert it rather than the lines themselves.
            var page = Build();
            float lastLeft = float.MinValue, lastRight = float.MinValue;

            foreach (var slot in page.Slots)
            {
                if (slot.OnLeft)
                {
                    Assert.Greater(slot.LeaderTo.Y, lastLeft, "left column anchors must descend");
                    lastLeft = slot.LeaderTo.Y;
                }
                else
                {
                    Assert.Greater(slot.LeaderTo.Y, lastRight, "right column anchors must descend");
                    lastRight = slot.LeaderTo.Y;
                }
            }
        }

        [Test]
        public void PanelsDoNotOverlapEachOther()
        {
            var page = Build();
            Assert.IsFalse(page.DollPanel.Overlaps(page.ComparePanel));
            Assert.IsFalse(page.DollPanel.Overlaps(page.PackPanel));
            Assert.IsFalse(page.ComparePanel.Overlaps(page.PackPanel));
            Assert.IsFalse(page.CaptionRect.Overlaps(page.DollPanel));

            Assert.IsFalse(page.EquippedCard.Overlaps(page.CandidateCard));
            Assert.IsFalse(page.EquippedCard.Overlaps(page.DeltaRect));
            Assert.IsFalse(page.CandidateCard.Overlaps(page.DeltaRect));
            Assert.IsTrue(page.DeltaRect.IsInside(page.ComparePanel));
        }

        [Test]
        public void PackGetsOneCardPerItemInsideItsPanel()
        {
            var page = Build();
            Assert.AreEqual(page.Pack.Count, page.PackCards.Count);
            foreach (var card in page.PackCards)
                Assert.IsTrue(card.IsInside(page.PackPanel), "a pack card escaped the pack panel");
        }

        [Test]
        public void UpAndDownWrapWithinTheCurrentColumn()
        {
            var page = Build();
            Assert.AreEqual(KitZone.SlotsLeft, page.Zone);
            Assert.AreEqual(0, page.SelectedSlotIndex);

            page.MoveUp();
            Assert.AreEqual(3, page.SelectedSlotIndex, "up from the top wraps to the bottom of the column");

            page.MoveDown();
            Assert.AreEqual(0, page.SelectedSlotIndex);
        }

        [Test]
        public void LeftAndRightCycleTheThreeZones()
        {
            var page = Build();
            page.MoveRight();
            Assert.AreEqual(KitZone.SlotsRight, page.Zone);
            page.MoveRight();
            Assert.AreEqual(KitZone.Pack, page.Zone);
            page.MoveRight();
            Assert.AreEqual(KitZone.SlotsLeft, page.Zone, "the zones wrap");
            page.MoveLeft();
            Assert.AreEqual(KitZone.Pack, page.Zone, "and wrap the other way");
        }

        [Test]
        public void AnEmptyPackNeverSwallowsTheCursor()
        {
            var page = Build(withPack: false);
            page.MoveRight();
            Assert.AreEqual(KitZone.SlotsRight, page.Zone);
            page.MoveRight();
            Assert.AreEqual(KitZone.SlotsLeft, page.Zone, "an empty pack must be skipped, not entered");
            Assert.AreEqual(-1, page.SelectedPackIndex);
        }

        [Test]
        public void DeltaIsPositiveForAnUpgradeAndNegativeForADowngrade()
        {
            var page = Build();
            page.MoveRight();
            page.MoveRight(); // into the pack, on Ranch Coat (27.5) against the worn vest (20)

            Assert.AreEqual(KitZone.Pack, page.Zone);
            Assert.AreEqual(1, page.DeltaSign);
            Assert.AreEqual(7.5f, page.Delta, 0.001f);
            StringAssert.StartsWith("+", page.DeltaText, "an upgrade must carry its plus sign");

            page.MoveDown(); // Sidearm (24) against the worn rifle (30)
            Assert.AreEqual(-1, page.DeltaSign);
            Assert.AreEqual(-6f, page.Delta, 0.001f);
            StringAssert.StartsWith("-", page.DeltaText);
        }

        [Test]
        public void AnItemForAnEmptySlotIsAllUpside()
        {
            var page = Build();
            page.MoveRight();
            page.MoveRight();
            page.MoveDown();
            page.MoveDown();
            page.MoveDown(); // Lake Stone, CharmA, which is worn by nothing

            Assert.AreEqual("CharmA", page.ComparisonSlot);
            Assert.IsNull(page.ComparisonEquipped);
            Assert.AreEqual(4f, page.Delta, 0.001f);
        }

        [Test]
        public void StandingOnASlotOffersTheBestPackItemForIt()
        {
            var page = Build();
            page.MoveDown(); // Vest, worn at 20

            Assert.AreEqual("Vest", page.ComparisonSlot);
            Assert.IsNotNull(page.ComparisonCandidate);
            Assert.AreEqual("Better Coat", page.ComparisonCandidate!.Value.Name,
                "the slot must offer the strongest candidate, not the first one found");
            Assert.AreEqual(11f, page.Delta, 0.001f);
        }

        [Test]
        public void AnEmptySlotWithNothingToPutInItHasNoComparison()
        {
            var page = Build();
            page.MoveRight();
            page.MoveDown();
            page.MoveDown();
            page.MoveDown(); // DogTag: nothing worn, nothing in the pack

            Assert.AreEqual("DogTag", page.ComparisonSlot);
            Assert.IsFalse(page.HasComparison);
            Assert.AreEqual(0, page.DeltaSign);
            Assert.AreEqual("0.0", page.DeltaText);
        }

        [Test]
        public void ConfirmEquipsFromThePackAndStripsAWornSlot()
        {
            var page = Build();
            page.MoveRight();
            page.MoveRight();
            Assert.AreEqual(KitAction.Equip, page.Confirm());
            Assert.AreEqual("Ranch Coat", page.ActionTarget!.Value.Name);

            page.MoveLeft();
            page.MoveLeft(); // back to the left slot column, on Helm
            Assert.AreEqual(KitAction.Unequip, page.Confirm());
            Assert.AreEqual("Watch Cap", page.ActionTarget!.Value.Name);
        }

        [Test]
        public void ConfirmOnAnEmptySlotDoesNothing()
        {
            var page = Build();
            page.MoveDown();
            page.MoveDown(); // Gloves: empty
            Assert.AreEqual(KitAction.None, page.Confirm());
        }

        [Test]
        public void BackAlwaysCloses()
        {
            Assert.AreEqual(KitAction.Close, Build().Back());
        }
    }
}
