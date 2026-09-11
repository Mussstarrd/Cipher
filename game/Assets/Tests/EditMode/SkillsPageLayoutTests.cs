#nullable enable
using System.Collections.Generic;
using Cipher.Game.UI.Comic;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The dossier's geometry, scrolling and cursor.
    ///
    /// Two of these tests exist because of specific owner words on 2026-09-11: "once I'm in my skills
    /// if I press up again it exits out of my skills so that's counterproductive", and "there's no
    /// way for me to scroll the skills list". Both are now properties of the model, not habits of the
    /// caller, so neither can regress by someone rebinding a button.
    /// </summary>
    public sealed class SkillsPageLayoutTests
    {
        /// <summary>Short on purpose: this height yields three visible rows, so scrolling is testable.</summary>
        private static readonly ComicRect ShortPage = new ComicRect(0f, 0f, 1200f, 440f);

        private static readonly ComicRect TallPage = new ComicRect(0f, 0f, 1200f, 900f);

        private static List<SkillNodeView> Nodes()
        {
            return new List<SkillNodeView>
            {
                new SkillNodeView("a1", "Steady Hands", "Less sway.", SkillState.Taken, 1, 0),
                new SkillNodeView("a2", "Hand Loads", "Rounds hit harder.", SkillState.Taken, 1, 0),
                new SkillNodeView("a3", "Called Shots", "Headshots pay double.", SkillState.Available, 2, 0),
                new SkillNodeView("a4", "Cold Barrel", "First shot never misses.", SkillState.Locked, 3, 0),
                new SkillNodeView("a5", "Long Guns", "Rifles reach further.", SkillState.Locked, 3, 0),

                new SkillNodeView("b1", "Bolt Cutters", "Unbolt faster.", SkillState.Available, 1, 1),
                new SkillNodeView("b2", "Field Repair", "Walls mend.", SkillState.Locked, 2, 1),

                // Column 2 is deliberately empty; the cursor must skip it rather than land in it.
                new SkillNodeView("d1", "Scrounger", "Kills pay more.", SkillState.Taken, 1, 3),
            };
        }

        private static SkillsPageLayout Build(ComicRect page)
        {
            var layout = new SkillsPageLayout(Nodes())
            {
                ColumnTitles = new[] { "THE RIFLE", "THE POSITION", "UNUSED", "THE RETREAT" },
            };
            layout.Layout(page);
            return layout;
        }

        [Test]
        public void NodesGroupIntoTheirColumnsInOrder()
        {
            var page = Build(TallPage);
            Assert.AreEqual(4, page.ColumnCount);
            Assert.AreEqual(5, page.ColumnNodes(0).Count);
            Assert.AreEqual(2, page.ColumnNodes(1).Count);
            Assert.AreEqual(0, page.ColumnNodes(2).Count);
            Assert.AreEqual(1, page.ColumnNodes(3).Count);
            Assert.AreEqual("a1", page.ColumnNodes(0)[0].Node.Id);
            Assert.AreEqual("a5", page.ColumnNodes(0)[4].Node.Id);
        }

        [Test]
        public void ColumnPanelsTileTheStripWithoutOverlapping()
        {
            var page = Build(TallPage);
            for (int i = 0; i < page.ColumnPanels.Count; i++)
            {
                for (int j = i + 1; j < page.ColumnPanels.Count; j++)
                    Assert.IsFalse(page.ColumnPanels[i].Overlaps(page.ColumnPanels[j]),
                        $"column {i} overlaps column {j}");

                Assert.IsFalse(page.ColumnPanels[i].Overlaps(page.DetailRect),
                    "a column must never run into the caption at the foot");
                Assert.IsFalse(page.ColumnPanels[i].Overlaps(page.CaptionRect));
            }

            Assert.IsFalse(page.CaptionRect.Overlaps(page.PointsStampRect),
                "the points stamp must not land on the page title");
        }

        [Test]
        public void VisibleBoxesStayInsideTheirColumnAndDoNotOverlap()
        {
            var page = Build(TallPage);
            for (int c = 0; c < page.ColumnCount; c++)
            {
                var nodes = page.ColumnNodes(c);
                var panel = page.ColumnPanels[c];
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (!nodes[i].Visible) continue;
                    Assert.IsTrue(nodes[i].Box.IsInside(panel), $"{nodes[i].Node.Id} escaped its column");
                    for (int j = i + 1; j < nodes.Count; j++)
                    {
                        if (!nodes[j].Visible) continue;
                        Assert.IsFalse(nodes[i].Box.Overlaps(nodes[j].Box),
                            $"{nodes[i].Node.Id} overlaps {nodes[j].Node.Id}");
                    }
                }
            }
        }

        [Test]
        public void EverythingFitsWhenThePageIsTallEnough()
        {
            var page = Build(TallPage);
            Assert.GreaterOrEqual(page.VisibleRows, 5);
            foreach (var box in page.ColumnNodes(0)) Assert.IsTrue(box.Visible);
            Assert.AreEqual(0, page.ScrollOf(0), "nothing should scroll when everything fits");
        }

        [Test]
        public void CursorWrapsUpwardBecauseUpIsNeverAnExit()
        {
            var page = Build(TallPage);
            Assert.AreEqual(0, page.Row);

            page.MoveUp();
            Assert.AreEqual(4, page.Row, "up from the top wraps to the bottom; it must never close the page");
            Assert.AreEqual("a5", page.Selected!.Node.Id);

            page.MoveDown();
            Assert.AreEqual(0, page.Row);
        }

        [Test]
        public void ScrollFollowsTheCursorAndKeepsItVisible()
        {
            var page = Build(ShortPage);
            Assert.AreEqual(3, page.VisibleRows, "this page height is chosen to show exactly three rows");
            Assert.AreEqual(0, page.ScrollOf(0));

            page.MoveDown();
            page.MoveDown();
            Assert.AreEqual(0, page.ScrollOf(0), "the third row is still on screen");

            page.MoveDown();
            Assert.AreEqual(1, page.ScrollOf(0), "the fourth row pushes the window down by one");

            page.Layout(ShortPage);
            Assert.IsTrue(page.ColumnNodes(0)[3].Visible, "the cursor's box must always be drawn");
            Assert.IsFalse(page.ColumnNodes(0)[0].Visible, "and the row it pushed off must not be");
        }

        [Test]
        public void WrappingUpwardScrollsToTheBottomOfTheColumn()
        {
            var page = Build(ShortPage);
            page.MoveUp();
            Assert.AreEqual(4, page.Row);
            Assert.AreEqual(2, page.ScrollOf(0), "wrapping to the last row must bring the window with it");
        }

        [Test]
        public void LeftAndRightWrapAndSkipEmptyColumns()
        {
            var page = Build(TallPage);
            Assert.AreEqual(0, page.Column);

            page.MoveRight();
            Assert.AreEqual(1, page.Column);

            page.MoveRight();
            Assert.AreEqual(3, page.Column, "the empty column 2 must be stepped over");

            page.MoveRight();
            Assert.AreEqual(0, page.Column, "columns wrap");

            page.MoveLeft();
            Assert.AreEqual(3, page.Column);
        }

        [Test]
        public void MovingSidewaysClampsTheRowIntoTheNewColumn()
        {
            var page = Build(TallPage);
            page.MoveDown();
            page.MoveDown();
            page.MoveDown();
            page.MoveDown(); // row 4 of column 0

            page.MoveRight(); // column 1 has only two nodes
            Assert.AreEqual(1, page.Row);
            Assert.AreEqual("b2", page.Selected!.Node.Id);
        }

        [Test]
        public void ConfirmTakesAnAvailableNodeAndBlocksTheRest()
        {
            var page = Build(TallPage);

            Assert.AreEqual(SkillsAction.Blocked, page.Confirm(), "a taken node cannot be bought twice");
            Assert.AreEqual("a1", page.ActionNodeId);

            page.MoveDown();
            page.MoveDown(); // Called Shots, available
            Assert.AreEqual(SkillsAction.Take, page.Confirm());
            Assert.AreEqual("a3", page.ActionNodeId);

            page.MoveDown(); // Cold Barrel, locked
            Assert.AreEqual(SkillsAction.Blocked, page.Confirm());
        }

        [Test]
        public void BackIsTheOnlyWayOut()
        {
            var page = Build(TallPage);
            Assert.AreEqual(SkillsAction.Close, page.Back());
        }

        [Test]
        public void AnEmptyDossierStillLaysOutAndSelectsNothing()
        {
            var page = new SkillsPageLayout(new List<SkillNodeView>());
            page.Layout(TallPage);
            Assert.IsNull(page.Selected);
            Assert.AreEqual(SkillsAction.None, page.Confirm());
            page.MoveDown();
            page.MoveRight();
            Assert.AreEqual(0, page.Row);
        }
    }
}
