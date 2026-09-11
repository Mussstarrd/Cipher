#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// All three comic pages side by side with sample data, so the look can be photographed and
    /// judged before any of it is wired to the match.
    ///
    /// CLAUDE.md: "Verify rendering, never infer it." Compiling is not rendering and tests passing is
    /// not rendering, so this exists to put a real frame in front of the owner through the screenshot
    /// harness. It holds no game state and touches nothing outside this folder — call it from an
    /// OnGUI that has already done BeginScaledUi and it draws.
    ///
    /// The sample data is deliberately awkward: a slot with nothing in it, a downgrade in the pack, a
    /// skills column that overflows its window, and a truck that cannot take everything. A demo where
    /// everything fits proves nothing.
    /// </summary>
    public static class ComicPageDemo
    {
        public enum Page { Kit, Skills, Truck }

        /// <summary>
        /// THE entry point. Draws the three pages as three columns of one screen. Each page lays out
        /// from a rect, so a narrow column is a real layout and not a scaled-down picture of one.
        /// </summary>
        public static void DrawAll(float uiW, float uiH)
        {
            const float pad = 10f;
            float w = (uiW - pad * 4f) / 3f;
            float h = uiH - pad * 2f;

            DrawPage(Page.Kit, new ComicRect(pad, pad, w, h));
            DrawPage(Page.Skills, new ComicRect(pad * 2f + w, pad, w, h));
            DrawPage(Page.Truck, new ComicRect(pad * 3f + w * 2f, pad, w, h));
        }

        /// <summary>One page, full bleed into the given rect. Use it to screenshot a single screen.</summary>
        public static void DrawPage(Page page, ComicRect rect)
        {
            switch (page)
            {
                case Page.Kit:
                {
                    var layout = SampleKit();
                    layout.Layout(rect);
                    ComicPages.DrawKit(layout, "KIT  —  what leaves with you");
                    break;
                }

                case Page.Skills:
                {
                    var layout = SampleSkills();
                    layout.Layout(rect);
                    ComicPages.DrawSkills(layout, "SERVICE RECORD", 3);
                    break;
                }

                default:
                {
                    var layout = SampleTruck();
                    layout.Layout(rect);
                    ComicPages.DrawTruck(layout, "PACK UP  —  the bed is the budget");
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ sample data

        /// <summary>
        /// Slot names in the same order as Progression.Slot, but as plain strings — the layout is
        /// generic and the integrator passes whatever the real enum yields.
        /// </summary>
        public static readonly string[] SampleSlots =
        {
            "Helm", "Vest", "Gloves", "Charm A",
            "Weapon", "Boots", "Charm B", "Dog Tag",
        };

        public static KitPageLayout SampleKit()
        {
            var equipped = new List<KitItem>
            {
                new KitItem("Watch Cap", "Helm", 8.5f),
                new KitItem("Plated Vest", "Vest", 21.0f),
                new KitItem("Cut Gloves", "Gloves", 6.0f),
                new KitItem("Service Rifle", "Weapon", 34.5f),
                new KitItem("Worn Boots", "Boots", 5.5f),
                new KitItem("VA Letter", "Dog Tag", 12.0f),
                // Charm A and Charm B deliberately empty: an unfilled slot has to read as a hole.
            };

            var pack = new List<KitItem>
            {
                new KitItem("Ranch Hand's Coat", "Vest", 27.5f),   // an upgrade
                new KitItem("Sheriff's Sidearm", "Weapon", 29.0f), // a downgrade, on purpose
                new KitItem("Lake Stone", "Charm A", 4.0f),
                new KitItem("Boat Key", "Charm B", 3.5f),
                new KitItem("Steel Toes", "Boots", 9.0f),
            };

            var layout = new KitPageLayout(SampleSlots, equipped, pack);
            layout.MoveRight();
            layout.MoveRight(); // land on the pack, on the coat, so the big + is showing
            return layout;
        }

        public static SkillsPageLayout SampleSkills()
        {
            var nodes = new List<SkillNodeView>
            {
                new SkillNodeView("s1", "Steady Hands", "Less sway when you hold your breath.", SkillState.Taken, 1, 0),
                new SkillNodeView("s2", "Hand Loads", "Your rounds hit harder.", SkillState.Taken, 1, 0),
                new SkillNodeView("s3", "Called Shots", "Headshots pay double scrip.", SkillState.Available, 2, 0),
                new SkillNodeView("s4", "Cold Barrel", "First shot after a reload never misses.", SkillState.Locked, 3, 0),
                new SkillNodeView("s5", "Long Guns", "Rifles reach further.", SkillState.Locked, 3, 0),

                new SkillNodeView("e1", "Bolt Cutters", "Unbolt an emplacement in half the time.", SkillState.Taken, 1, 1),
                new SkillNodeView("e2", "Field Repair", "Walls you touch mend themselves.", SkillState.Available, 2, 1),
                new SkillNodeView("e3", "Overwatch", "Turrets see one cell further.", SkillState.Available, 2, 1),
                new SkillNodeView("e4", "Deep Magazines", "Turrets fire longer before they cool.", SkillState.Locked, 3, 1),

                new SkillNodeView("q1", "Scrounger", "Every kill pays a little more.", SkillState.Taken, 1, 2),
                new SkillNodeView("q2", "Light Pack", "One more thing fits in the bed.", SkillState.Available, 2, 2),
                new SkillNodeView("q3", "Aid Kits", "Kits you pick up heal for longer.", SkillState.Locked, 2, 2),
                new SkillNodeView("q4", "Second Wind", "You get up once per position.", SkillState.Locked, 4, 2),
            };

            var layout = new SkillsPageLayout(nodes)
            {
                ColumnTitles = new[] { "THE RIFLE", "THE POSITION", "THE RETREAT" },
            };
            layout.MoveDown();
            layout.MoveDown(); // sit on Called Shots, an available node, so the caption has something to say
            return layout;
        }

        public static TruckPageLayout SampleTruck()
        {
            var items = new List<TruckItemView>
            {
                new TruckItemView("Sentry Mk II", 420f, 2.4f),
                new TruckItemView("Grinder", 510f, 3.1f),
                new TruckItemView("Repair Drone", 60f, 0.5f),
                new TruckItemView("Repair Drone", 60f, 0.5f),
                new TruckItemView("Gun Crate", 180f, 0.9f),
                new TruckItemView("Fence Panels", 240f, 2.2f),
                new TruckItemView("Generator", 700f, 1.6f),
            };

            // Two loaded, five on the gravel: the point of the screen is what is NOT in the bed.
            var loaded = new[] { true, false, true, true, false, false, false };
            return new TruckPageLayout(items, loaded, maxWeight: 1200f, maxVolume: 9f);
        }

        /// <summary>
        /// Convenience for a MonoBehaviour that wants the demo on a key. Kept here rather than in the
        /// bootstrap so this whole folder stays self-contained and deletable.
        /// </summary>
        public static void DrawAllScaled()
        {
            float scale = Mathf.Clamp(Screen.height / 800f, 1f, 3f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            DrawAll(Screen.width / scale, Screen.height / scale);
        }
    }
}
