#nullable enable
using System.Collections.Generic;
using Cipher.Game.Hero;
using Cipher.Game.Progression;
using Cipher.Game.UI.Comic;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The three comic screen controllers, tested without a scene — the PauseMenuModel seam.
    ///
    /// Everything these classes decide is a function of a <see cref="ComicNav"/> and the model, so
    /// everything they decide can be asserted here. What cannot be asserted here is what they LOOK
    /// like, which is what the screenshot harness is for: `-exodus-comic kit|skills|truck`.
    ///
    /// Three of these tests exist because of a specific thing the owner said on 2026-09-11, and they
    /// are named for it. If one of them starts failing, the screen has regressed to the version he
    /// could not use.
    /// </summary>
    public sealed class ComicScreenTests
    {
        // ================================================================== helpers

        /// <summary>Records what a screen asked the game around it to do.</summary>
        private class Recorder : IComicScreenHost
        {
            public readonly List<string> Notices = new List<string>();
            public int Ticks;
            public int Accepts;
            public int Refusals;

            public void Notice(string text) => Notices.Add(text);
            public void Tick() => Ticks++;
            public void Accept() => Accepts++;
            public void Refuse() => Refusals++;

            public string Last => Notices.Count == 0 ? string.Empty : Notices[Notices.Count - 1];
        }

        private sealed class TruckRecorder : Recorder, ITruckHost
        {
            /// <summary>How many more emplacements the window has time for. -1 means unlimited.</summary>
            public int UnboltsAllowed = -1;
            public int Unbolts;
            public bool PullOutSucceeds = true;
            public int PullOuts;

            public float SecondsLeft { get; set; } = 60f;

            public bool TryUnbolt(int recoveredValue)
            {
                if (UnboltsAllowed >= 0 && Unbolts >= UnboltsAllowed) return false;
                Unbolts++;
                return true;
            }

            public bool PullOut()
            {
                PullOuts++;
                return PullOutSucceeds;
            }
        }

        private static ComicNav Nav(bool up = false, bool down = false, bool left = false, bool right = false,
                                    bool confirm = false, bool cancel = false, bool secondary = false,
                                    bool leave = false)
            => new ComicNav
            {
                Up = up, Down = down, Left = left, Right = right,
                Confirm = confirm, Cancel = cancel, Secondary = secondary, Leave = leave,
            };

        private static ItemInstance Item(Slot slot, AffixKind kind, float magnitude, string name,
                                         Rarity rarity = Rarity.Serviceable, int ilvl = 5)
            => new ItemInstance(slot, rarity, ilvl, new[] { new Affix(kind, magnitude) }, name);

        /// <summary>
        /// A loadout with a vest and an emitter worn and one slightly worse vest in the pack.
        /// "Slightly" matters: the auto-scrap filter turns anything under 60% of what is worn into
        /// scrip on the spot, so a junk item would never reach the pack to be tested against.
        /// </summary>
        private static Loadout BuildLoadout()
        {
            var loadout = new Loadout(new HeroConfig());
            loadout.Pickup(Item(Slot.Vest, AffixKind.Plated, 5f, "Plated Vest"));       // power 20
            loadout.Pickup(Item(Slot.Weapon, AffixKind.HandLoaded, 12f, "Field Jammer")); // power 12
            loadout.Pickup(Item(Slot.Vest, AffixKind.Plated, 4f, "Scrap Vest"));        // power 16 -> pack
            return loadout;
        }

        // ================================================================== AxisRepeat

        [Test]
        public void AxisRepeat_FiresImmediatelyThenWaitsBeforeRepeating()
        {
            var axis = new AxisRepeat { FirstDelay = 0.4f, RepeatDelay = 0.1f };

            Assert.AreEqual(1, axis.Step(1f, 0.016f), "a fresh push must move on the frame it happens");
            Assert.AreEqual(0, axis.Step(1f, 0.2f), "still inside the first delay");
            Assert.AreEqual(0, axis.Step(1f, 0.15f), "still inside the first delay");
            Assert.AreEqual(1, axis.Step(1f, 0.1f), "the hold has earned a repeat");
        }

        [Test]
        public void AxisRepeat_IgnoresARestingStick()
        {
            var axis = new AxisRepeat();
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(0, axis.Step(0.3f, 0.016f), "inside the deadzone nothing may move");
        }

        [Test]
        public void AxisRepeat_ReversingDirectionFiresAtOnce()
        {
            var axis = new AxisRepeat();
            Assert.AreEqual(1, axis.Step(1f, 0.016f));
            Assert.AreEqual(-1, axis.Step(-1f, 0.016f), "a flick the other way must not wait out the repeat");
        }

        [Test]
        public void AxisRepeat_ResetForgetsTheHold()
        {
            var axis = new AxisRepeat();
            axis.Step(1f, 0.016f);
            axis.Reset();
            Assert.AreEqual(1, axis.Step(1f, 0.016f), "after a reset a held stick counts as a fresh push");
        }

        // ================================================================== prompts

        /// <summary>
        /// OWNER, 2026-09-11: "there are only instructions on screen for keyboard keys and the point
        /// of this is to use controller primarily". Every prompt line on every page, with a pad in
        /// hand, must name pad controls and no keyboard ones.
        /// </summary>
        [Test]
        public void EveryPromptLine_NamesTheDeviceInThePlayersHands()
        {
            var padLines = new[]
            {
                KitScreen.PromptLine(true, onPack: true),
                KitScreen.PromptLine(true, onPack: false),
                SkillsScreen.PromptLine(true),
                TruckScreen.PromptLine(true),
            };

            var keyboardOnly = new[] { "Enter", "Esc", "Arrow keys" };
            foreach (var line in padLines)
            {
                Assert.IsNotEmpty(line);
                foreach (var token in keyboardOnly)
                    Assert.IsFalse(line.Contains(token), $"pad prompt names a keyboard key: '{line}'");
            }
        }

        [Test]
        public void EveryPromptLine_ChangesWithTheDevice()
        {
            Assert.AreNotEqual(KitScreen.PromptLine(true, true), KitScreen.PromptLine(false, true));
            Assert.AreNotEqual(SkillsScreen.PromptLine(true), SkillsScreen.PromptLine(false));
            Assert.AreNotEqual(TruckScreen.PromptLine(true), TruckScreen.PromptLine(false));
        }

        [Test]
        public void EveryPromptLine_TellsThePlayerHowToGetOut()
        {
            foreach (bool pad in new[] { true, false })
            {
                StringAssert.Contains(ComicPrompts.Cancel(pad), KitScreen.PromptLine(pad, true));
                StringAssert.Contains(ComicPrompts.Cancel(pad), SkillsScreen.PromptLine(pad));
                StringAssert.Contains(ComicPrompts.Cancel(pad), TruckScreen.PromptLine(pad));
            }
        }

        // ================================================================== kit

        [Test]
        public void Kit_MapsTheInventoryOntoTheDoll()
        {
            var loadout = BuildLoadout();

            var worn = KitScreen.Equipped(loadout.Inventory);
            Assert.AreEqual(2, worn.Count);

            var pack = KitScreen.Pack(loadout.Inventory);
            Assert.AreEqual(1, pack.Count);
            Assert.AreEqual("Scrap Vest", pack[0].Name);
            Assert.AreEqual(KitScreen.NameOf(Slot.Vest), pack[0].Slot,
                            "a pack item's slot must match the doll's slot name or it can never be compared");
        }

        [Test]
        public void Kit_SlotNamesAndOrderAgree()
        {
            Assert.AreEqual(KitScreen.DollOrder.Length, KitScreen.SlotNames.Length);
            CollectionAssert.AllItemsAreUnique(KitScreen.SlotNames,
                "the page finds a slot by name, so two slots sharing one would alias");
        }

        [Test]
        public void Kit_OnlyCancelCloses()
        {
            var screen = new KitScreen(BuildLoadout());
            screen.Show();

            // Every direction, twice, plus confirm and the secondary verb. None of them is an exit.
            foreach (var nav in new[]
                     {
                         Nav(up: true), Nav(down: true), Nav(left: true), Nav(right: true),
                         Nav(up: true), Nav(right: true), Nav(confirm: true), Nav(secondary: true),
                     })
            {
                screen.Apply(nav);
                Assert.IsTrue(screen.IsOpen, "nothing but B closes the kit");
            }

            Assert.AreEqual(KitScreenAction.Closed, screen.Apply(Nav(cancel: true)));
            Assert.IsFalse(screen.IsOpen);
        }

        [Test]
        public void Kit_EquipsFromThePackAndKeepsTheCursorNearby()
        {
            var loadout = BuildLoadout();
            var host = new Recorder();
            int changes = 0;
            var screen = new KitScreen(loadout, host, () => changes++);
            screen.Show();

            screen.Apply(Nav(right: true));
            screen.Apply(Nav(right: true));
            Assert.AreEqual(KitZone.Pack, screen.Page.Zone, "two steps right from the left column is the pack");

            Assert.AreEqual(KitScreenAction.Equipped, screen.Apply(Nav(confirm: true)));
            Assert.AreEqual("Scrap Vest", loadout.Inventory.Equipped(Slot.Vest)?.Name);
            Assert.AreEqual(1, changes, "the bootstrap has to re-apply the loadout to the world");
            Assert.AreEqual(1, host.Accepts);
            StringAssert.Contains("Scrap Vest", host.Last);
        }

        [Test]
        public void Kit_UnequipsIntoThePack()
        {
            var loadout = BuildLoadout();
            var screen = new KitScreen(loadout);
            screen.Show();

            // Vest is the second box down the left column.
            screen.Apply(Nav(down: true));
            Assert.AreEqual(KitScreenAction.Unequipped, screen.Apply(Nav(confirm: true)));
            Assert.IsNull(loadout.Inventory.Equipped(Slot.Vest));
            Assert.AreEqual(2, loadout.Inventory.Pack.Count);
        }

        [Test]
        public void Kit_AnEmptySlotSaysWhyNothingHappened()
        {
            var host = new Recorder();
            var screen = new KitScreen(BuildLoadout(), host);
            screen.Show();

            // Helm is the top box and nothing is in it.
            Assert.AreEqual(KitScreenAction.Refused, screen.Apply(Nav(confirm: true)));
            Assert.AreEqual(1, host.Refusals, "a press that does nothing must still make a sound");
            Assert.IsNotEmpty(host.Last);
        }

        [Test]
        public void Kit_ScrapsAPackItemForScrip()
        {
            var loadout = BuildLoadout();
            var host = new Recorder();
            var screen = new KitScreen(loadout, host);
            screen.Show();

            screen.Apply(Nav(right: true));
            screen.Apply(Nav(right: true));
            int before = loadout.Inventory.Scrip;

            Assert.AreEqual(KitScreenAction.Scrapped, screen.Apply(Nav(secondary: true)));
            Assert.Greater(loadout.Inventory.Scrip, before);
            Assert.AreEqual(0, loadout.Inventory.Pack.Count);
            StringAssert.Contains("scrip", host.Last);
        }

        [Test]
        public void Kit_ExplainsWhereGearComesFrom()
        {
            // The owner's question, verbatim: "I don't understand where I'm picking up gear from how
            // my inventory works". The answer has to be ON the page, not in a design doc.
            StringAssert.Contains("drop", KitScreen.Explain);
            StringAssert.Contains("pack", KitScreen.Explain);
        }

        // ================================================================== skills

        [Test]
        public void Skills_StatesReadTakenAvailableLocked()
        {
            var skills = new Progression.SkillState();
            skills.AddXp(LevelCurve.TotalXpFor(3));
            skills.Spend("t-marks");

            var views = SkillsScreen.Nodes(skills);
            var byId = new Dictionary<string, SkillNodeView>();
            foreach (var v in views) byId[v.Id] = v;

            Assert.AreEqual(UI.Comic.SkillState.Available, byId["t-marks"].State,
                            "a stacking node with a rank and points spare is still buyable");
            Assert.AreEqual(UI.Comic.SkillState.Available, byId["t-cadence"].State,
                            "its prerequisite is bought and there are points");
            Assert.AreEqual(UI.Comic.SkillState.Locked, byId["t-reach"].State);
            StringAssert.Contains("needs", byId["t-reach"].Text,
                                  "a locked box with no reason on it is a dead end");
        }

        [Test]
        public void Skills_ColumnsFollowThePaths()
        {
            var views = SkillsScreen.Nodes(new Progression.SkillState());
            foreach (var v in views)
                Assert.Less(v.Column, SkillsScreen.ColumnTitles.Length, "every node needs a titled column");

            Assert.AreEqual(SkillsScreen.ColumnOf(Path.Trigger), 0);
            Assert.AreEqual(3, SkillsScreen.ColumnTitles.Length);
        }

        /// <summary>
        /// OWNER, 2026-09-11: "once I'm in my skills if I press up again it exits out of my skills so
        /// that's counterproductive". Up scrolls. Up has never closed anything since.
        /// </summary>
        [Test]
        public void Skills_UpNeverCloses()
        {
            var screen = new SkillsScreen(BuildLoadout());
            screen.Show();

            for (int i = 0; i < 12; i++)
            {
                screen.Apply(Nav(up: true));
                Assert.IsTrue(screen.IsOpen);
            }

            Assert.AreEqual(SkillsScreenAction.Closed, screen.Apply(Nav(cancel: true)));
        }

        [Test]
        public void Skills_ScrollingWrapsAndKeepsTheCursorOnScreen()
        {
            var screen = new SkillsScreen(BuildLoadout());
            screen.Show();
            var rect = new ComicRect(0f, 0f, 1400f, 700f);
            screen.Page.Layout(rect);

            for (int i = 0; i < 30; i++)
            {
                screen.Apply(Nav(down: true));
                // Visible is stamped by Layout, which the draw call does every frame.
                screen.Page.Layout(rect);
                var sel = screen.Page.Selected;
                Assert.IsNotNull(sel);
                Assert.IsTrue(sel!.Visible || screen.Page.ColumnNodes(screen.Page.Column).Count <= 1,
                              "the cursor must never be on a box that has scrolled out of view");
            }
        }

        [Test]
        public void Skills_BuysANodeAndTellsTheWorld()
        {
            var loadout = BuildLoadout();
            loadout.Skills.AddXp(LevelCurve.TotalXpFor(4));
            var host = new Recorder();
            int changes = 0;
            var screen = new SkillsScreen(loadout, host, () => changes++);
            screen.Show();

            int before = loadout.Skills.UnspentPoints;
            Assert.AreEqual(SkillsScreenAction.Bought, screen.Apply(Nav(confirm: true)));
            Assert.AreEqual(1, loadout.Skills.RankOf("t-marks"));
            Assert.Less(loadout.Skills.UnspentPoints, before);
            Assert.AreEqual(1, changes);
            Assert.AreEqual(1, host.Accepts);
        }

        [Test]
        public void Skills_RefusesWithAReason()
        {
            // No experience, so no points at all.
            var loadout = BuildLoadout();
            var host = new Recorder();
            var screen = new SkillsScreen(loadout, host);
            screen.Show();

            Assert.AreEqual(SkillsScreenAction.Refused, screen.Apply(Nav(confirm: true)));
            Assert.AreEqual(1, host.Refusals);
            StringAssert.Contains("points", host.Last);
        }

        // ================================================================== truck

        private static List<IHaulable> Recoverable() => new List<IHaulable>
        {
            new SalvagedEmplacement("Sentry T2", new Haulage(420f, 2.4f), 260),
            new SalvagedEmplacement("Grinder T1", new Haulage(510f, 3.1f), 300),
            new SalvagedEmplacement("Repair Drone", new Haulage(60f, 0.5f), 80),
        };

        [Test]
        public void Truck_MapsHaulablesOntoCrates()
        {
            var things = Recoverable();
            var truck = new TruckLoad();
            truck.TryLoad(things[1]);

            var views = TruckScreen.Items(things);
            Assert.AreEqual(3, views.Count);
            Assert.AreEqual("Sentry T2", views[0].Name);
            Assert.AreEqual(2.4f, views[0].Volume, 0.001f);

            var flags = TruckScreen.LoadedFlags(truck, things);
            CollectionAssert.AreEqual(new[] { false, true, false }, flags);
        }

        [Test]
        public void Truck_ChargesTheClockBeforeTheCrateGoesAboard()
        {
            var host = new TruckRecorder();
            var screen = new TruckScreen(host);
            var things = Recoverable();
            var truck = new TruckLoad();
            screen.Open(truck, things);

            Assert.AreEqual(TruckScreenAction.Loaded, screen.Apply(Nav(confirm: true)));
            Assert.AreEqual(1, host.Unbolts);
            Assert.AreEqual(1, truck.Loaded.Count, "the real manifest has to follow the picture");
            Assert.IsTrue(truck.IsLoaded(things[0]));
        }

        /// <summary>
        /// The bug this shape of code had once: charging the window BEFORE checking the bed meant a
        /// crate that did not fit still cost four seconds and still paid out, and the same crate
        /// could be sold again every four seconds until the window closed.
        /// </summary>
        [Test]
        public void Truck_ACrateThatDoesNotFitCostsNoTime()
        {
            var host = new TruckRecorder();
            var screen = new TruckScreen(host);
            var things = Recoverable();
            screen.Open(new TruckLoad(maxWeight: 10f, maxVolume: 0.1f), things);

            Assert.AreEqual(TruckScreenAction.Refused, screen.Apply(Nav(confirm: true)));
            Assert.AreEqual(0, host.Unbolts, "a refusal must not spend the pack-up window");
            Assert.AreEqual(1, host.Refusals);
        }

        [Test]
        public void Truck_SaysWhichLimitStoppedIt()
        {
            var host = new TruckRecorder();
            var screen = new TruckScreen(host);
            // Room by volume, nowhere near it by weight: "no room" would be a lie.
            screen.Open(new TruckLoad(maxWeight: 10f, maxVolume: 99f), Recoverable());

            screen.Apply(Nav(confirm: true));
            StringAssert.Contains("heavy", host.Last);
        }

        [Test]
        public void Truck_NoTimeLeftSaysSo()
        {
            var host = new TruckRecorder { UnboltsAllowed = 0 };
            var screen = new TruckScreen(host);
            screen.Open(new TruckLoad(), Recoverable());

            Assert.AreEqual(TruckScreenAction.Refused, screen.Apply(Nav(confirm: true)));
            StringAssert.Contains("time", host.Last,
                "the answer to 'why can only two towers come' is the clock, and the page has to say it");
        }

        [Test]
        public void Truck_UnloadsBackOntoTheGravel()
        {
            var host = new TruckRecorder();
            var screen = new TruckScreen(host);
            var things = Recoverable();
            var truck = new TruckLoad();
            screen.Open(truck, things);

            screen.Apply(Nav(confirm: true));
            Assert.AreEqual(1, truck.Loaded.Count);

            // The crate that just loaded is now in the bed, and the cursor followed it there.
            screen.Apply(Nav(up: true));
            Assert.AreEqual(TruckZone.Bed, screen.Page.Zone);
            Assert.AreEqual(TruckScreenAction.Unloaded, screen.Apply(Nav(confirm: true)));
            Assert.AreEqual(0, truck.Loaded.Count);
        }

        [Test]
        public void Truck_AutoLoadStopsWhenTheClockDoes()
        {
            var host = new TruckRecorder { UnboltsAllowed = 2 };
            var screen = new TruckScreen(host);
            var truck = new TruckLoad();
            screen.Open(truck, Recoverable());

            Assert.AreEqual(TruckScreenAction.Loaded, screen.Apply(Nav(secondary: true)));
            Assert.AreEqual(2, truck.Loaded.Count);
            Assert.AreEqual(2, host.Unbolts);
            StringAssert.Contains("clock", host.Last);
        }

        [Test]
        public void Truck_LeaveGoesThroughTheHost()
        {
            var host = new TruckRecorder();
            var screen = new TruckScreen(host);
            screen.Open(new TruckLoad(), Recoverable());

            Assert.AreEqual(TruckScreenAction.PulledOut, screen.Apply(Nav(leave: true)));
            Assert.AreEqual(1, host.PullOuts);
            Assert.IsFalse(screen.IsOpen);
        }

        [Test]
        public void Truck_OnlyCancelAndLeaveClose()
        {
            var host = new TruckRecorder();
            var screen = new TruckScreen(host);
            screen.Open(new TruckLoad(), Recoverable());

            foreach (var nav in new[] { Nav(up: true), Nav(down: true), Nav(left: true), Nav(right: true), Nav(confirm: true) })
            {
                screen.Apply(nav);
                Assert.IsTrue(screen.IsOpen);
            }

            Assert.AreEqual(TruckScreenAction.Closed, screen.Apply(Nav(cancel: true)));
        }


        // ---------------------------------------------------------------- the gear decision

        /// <summary>Six emplacements and a pack, which is what a real extraction hands the screen.</summary>
        private static List<IHaulable> RecoverableWithGear()
        {
            var list = new List<IHaulable>(Recoverable());
            var loadout = new Loadout(new HeroConfig());
            var roller = new ItemRoller(7);
            for (int i = 0; i < 12; i++)
            {
                var item = roller.TryDrop(DropSource.SapperKill, 2, 3);
                if (item != null) loadout.Pickup(item);
            }
            Assert.Greater(loadout.Inventory.Pack.Count, 0, "the fixture needs a pack to test with");
            for (int i = 0; i < loadout.Inventory.Pack.Count; i++)
                list.Add(new HauledItem(loadout.Inventory.Pack[i]));
            return list;
        }

        [Test]
        public void Truck_GearRidesInTheCabAndIsNotOnThePage()
        {
            // THE DECISION, pinned. A dog tag is 0.0005 m3 next to a 2.4 m3 sentry, so with gear on
            // the page the volume gauge reads nothing and the gravel panel is a wall of identical
            // blank boxes -- which is exactly what the owner played. ADR-005 says gear is never
            // what you cut, so it does not compete for the bed and the page says so once.
            var host = new TruckRecorder();
            var screen = new TruckScreen(host);
            var things = RecoverableWithGear();

            screen.Open(new TruckLoad(), things);

            Assert.AreEqual(3, screen.Page.All.Count, "only emplacements are choices on this page");
            Assert.Greater(screen.CabCount, 0, "the pack still exists, it just rides in the cab");
            foreach (var crate in screen.Page.All)
                Assert.GreaterOrEqual(crate.Item.Volume, 0.1f, "nothing bed-scale was dropped");
        }

        [Test]
        public void Truck_TheCabLineAccountsForEveryPieceOfKit()
        {
            var screen = new TruckScreen(new TruckRecorder());
            var things = RecoverableWithGear();
            int gear = 0;
            foreach (var t in things) if (TruckScreen.RidesInTheCab(t)) gear++;

            screen.Open(new TruckLoad(), things);

            Assert.AreEqual(gear, screen.CabCount);
            StringAssert.Contains(gear.ToString(), screen.CabLine);
            StringAssert.Contains("cab", screen.CabLine);
        }

        [Test]
        public void Truck_GearNeverEatsBedSpace()
        {
            var screen = new TruckScreen(new TruckRecorder());
            screen.Open(new TruckLoad(), RecoverableWithGear());
            Assert.AreEqual(0f, screen.Page.Volume, 0.0001f, "an empty bed is empty, gear or no gear");
            Assert.AreEqual(0f, screen.Page.Weight, 0.0001f);
        }

        // ---------------------------------------------------------------- the clock

        [Test]
        public void Truck_TheClockIsItsOwnElementNotAClauseInTheHeading()
        {
            // The owner: "after a certain amount of time it just kicked me out". It HAD told him --
            // in a header line, after two other clauses. The number now has its own stamp, and the
            // heading is about the cargo.
            var host = new TruckRecorder { SecondsLeft = 95f };
            var screen = new TruckScreen(host);
            screen.Open(new TruckLoad(), Recoverable());

            Assert.AreEqual("1:35", screen.ClockText);
            StringAssert.Contains("SCAN", screen.ClockLabel.ToUpperInvariant());
            StringAssert.DoesNotContain("1:35", screen.Subtitle, "the clock does not live in the heading any more");
            StringAssert.Contains("gravel", screen.Subtitle);
        }

        [Test]
        public void Truck_UrgencyRisesTwiceAndTheLastTenSecondsCountOutLoud()
        {
            var host = new TruckRecorder { SecondsLeft = 60f };
            var screen = new TruckScreen(host);
            screen.Open(new TruckLoad(), Recoverable());
            Assert.AreEqual(TruckUrgency.Calm, screen.Urgency);
            Assert.AreEqual(string.Empty, screen.Warning, "a calm page does not shout");

            host.SecondsLeft = 22f;
            Assert.AreEqual(TruckUrgency.Hurry, screen.Urgency);
            StringAssert.Contains("SCAN", screen.Warning.ToUpperInvariant());

            host.SecondsLeft = 7f;
            Assert.AreEqual(TruckUrgency.Final, screen.Urgency);
            Assert.AreEqual("7", screen.ClockText, "the last ten seconds are a countdown, not a clock");
            StringAssert.Contains("PULLING OUT", screen.Warning.ToUpperInvariant());
        }

        [Test]
        public void Truck_TheWarningNamesWhatLeavingCosts()
        {
            // "Time low" is a fact about the clock. "3 stay here" is a fact about his sentries, and
            // it is the one that makes him press a button.
            var host = new TruckRecorder { SecondsLeft = 12f };
            var screen = new TruckScreen(host);
            screen.Open(new TruckLoad(), Recoverable());

            Assert.AreEqual(3, screen.Page.LeftBehind.Count);
            StringAssert.Contains("3", screen.Warning);
        }

        [Test]
        public void Truck_TheExplainLineSaysWhatRunningOutDoes()
        {
            StringAssert.Contains("clock", TruckScreen.Explain);
            StringAssert.Contains("bed", TruckScreen.Explain);
            StringAssert.Contains("gravel", TruckScreen.Explain);
            StringAssert.Contains("drive", TruckScreen.Explain);
        }

        [Test]
        public void Truck_TheLedgerPricesBothSidesOfTheChoice()
        {
            var screen = new TruckScreen(new TruckRecorder());
            var things = Recoverable();
            var truck = new TruckLoad();
            truck.TryLoad(things[0]); // Sentry T2, $260
            screen.Open(truck, things);

            StringAssert.Contains("260", screen.Ledger);
            StringAssert.Contains("LEAVING", screen.Ledger.ToUpperInvariant());
        }

        [Test]
        public void Truck_TheCopyStructCarriesEveryWordThePagePrints()
        {
            var host = new TruckRecorder { SecondsLeft = 8f };
            var screen = new TruckScreen(host);
            screen.Open(new TruckLoad(), Recoverable());

            var copy = screen.Copy;
            Assert.AreEqual(screen.Subtitle, copy.Subtitle);
            Assert.AreEqual(screen.ClockText, copy.ClockText);
            Assert.AreEqual(screen.ClockLabel, copy.ClockLabel);
            Assert.AreEqual(screen.Warning, copy.Warning);
            Assert.AreEqual(screen.Ledger, copy.Ledger);
            Assert.AreEqual(screen.CabLine, copy.CabLine);
            Assert.AreEqual(screen.Urgency, copy.Urgency);
        }
    }
}
