#nullable enable
using UnityEngine;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// The three screens, drawn. Each takes a laid-out page object and a rect and paints it with
    /// ComicInk primitives; none of them owns state, reads input or touches the match.
    ///
    /// This is the half the integrator calls every frame. The layout classes decide WHERE things go
    /// and what the cursor is on (and are unit-tested for it); these decide what each thing looks
    /// like. Splitting them is what lets the geometry be tested at all, and it is the same seam as
    /// BuildWheel (maths) versus the bootstrap (drawing).
    /// </summary>
    public static class ComicPages
    {
        /// <summary>
        /// The black between the panels. A comic page's gutter, screened rather than flat, and opaque
        /// enough to hide the firefight behind it — these screens own input while they are up, so the
        /// player should not be reading the battle through them.
        /// </summary>
        public static void Gutter(ComicRect page)
        {
            var wash = ComicInk.Ink;
            wash.a = 0.94f;
            ComicInk.Fill(page, wash);
            ComicInk.Halftone(page, 0.25f, new Color(ComicInk.Paper.r, ComicInk.Paper.g, ComicInk.Paper.b, 0.16f));
        }

        // ================================================================== kit

        /// <summary>
        /// The paper doll. Leader lines first, then the boxes on top of them, so a line never crosses
        /// the label it points at.
        /// </summary>
        public static void DrawKit(KitPageLayout page, string subtitle)
        {
            Gutter(page.PageRect);
            ComicInk.Caption(page.CaptionRect, subtitle);

            ComicInk.Panel(page.DollPanel, "What you are carrying");
            foreach (var slot in page.Slots) ComicInk.LeaderLine(slot.LeaderFrom, slot.LeaderTo);
            ComicInk.Figure(page.FigureRect);

            foreach (var slot in page.Slots)
            {
                var box = slot.Box;
                if (slot.HasItem)
                {
                    ComicInk.PaperFill(box);
                    ComicInk.Border(box, 2f);
                    ComicInk.Small(new ComicRect(box.X + 6f, box.Y + 3f, box.W - 12f, 16f), slot.SlotName.ToUpperInvariant());
                    ComicInk.Body(new ComicRect(box.X + 6f, box.Y + 19f, box.W - 12f, 20f), slot.Item.Name);
                    ComicInk.Small(new ComicRect(box.X + 6f, box.Bottom - 19f, box.W - 12f, 16f), $"{slot.Item.Power:F1}");
                }
                else
                {
                    // An empty slot is a hole in the kit, and it should look like one you could fill.
                    ComicInk.Pencil(box, $"{slot.SlotName.ToUpperInvariant()}  —");
                }

                if (slot.Index == page.SelectedSlotIndex) ComicInk.Cursor(box);
            }

            DrawCompare(page);

            ComicInk.Panel(page.PackPanel, $"Pack ({page.Pack.Count})");
            for (int i = 0; i < page.PackCards.Count; i++)
            {
                var card = page.PackCards[i];
                var item = page.Pack[i];
                ComicInk.PaperFill(card);
                ComicInk.Border(card, 2f);
                ComicInk.Body(new ComicRect(card.X + 6f, card.Y + 4f, card.W - 12f, 20f), item.Name);
                ComicInk.Small(new ComicRect(card.X + 6f, card.Bottom - 20f, card.W - 12f, 16f),
                               $"{item.Slot.ToUpperInvariant()}  {item.Power:F1}");
                if (i == page.SelectedPackIndex) ComicInk.Cursor(card);
            }
        }

        private static void DrawCompare(KitPageLayout page)
        {
            string title = string.IsNullOrEmpty(page.ComparisonSlot) ? "Swap" : $"Swap — {page.ComparisonSlot}";
            ComicInk.Panel(page.ComparePanel, title);

            DrawCompareCard(page.EquippedCard, "WORN", page.ComparisonEquipped);
            DrawCompareCard(page.CandidateCard, "IN PACK", page.ComparisonCandidate);

            var delta = page.DeltaRect;
            ComicInk.PaperFill(delta);
            // Only an improvement gets the accent. Amber means "the implant lit up"; spending it on a
            // downgrade would make the one colour in this UI mean nothing.
            if (page.DeltaSign > 0) ComicInk.Halftone(delta.Inset(4f), 0.5f, ComicInk.Amber);
            else if (page.DeltaSign < 0) ComicInk.Halftone(delta.Inset(4f), 0.35f);
            ComicInk.Border(delta, ComicInk.BorderWidth);

            string text = page.HasComparison ? page.DeltaText : "—";
            ComicInk.BigNumber(delta, text, ComicInk.Ink);
        }

        private static void DrawCompareCard(ComicRect card, string heading, KitItem? item)
        {
            if (item == null)
            {
                ComicInk.Pencil(card, $"{heading}  —  nothing");
                return;
            }

            ComicInk.PaperFill(card);
            ComicInk.Border(card, 2f);
            ComicInk.Small(new ComicRect(card.X + 8f, card.Y + 4f, card.W - 16f, 16f), heading);
            ComicInk.Body(new ComicRect(card.X + 8f, card.Y + 22f, card.W - 16f, 22f), item.Value.Name);
            ComicInk.BigNumber(new ComicRect(card.X, card.Bottom - 44f, card.W, 40f),
                               $"{item.Value.Power:F1}", ComicInk.Ink);
        }

        // ================================================================== skills

        /// <summary>
        /// The dossier. One column per discipline, a scroll rule on any column that overflows, and
        /// the selected node's single line of prose in the caption at the foot — which is the only
        /// place this page explains anything in words.
        /// </summary>
        public static void DrawSkills(SkillsPageLayout page, string subtitle, int unspentPoints)
        {
            Gutter(page.PageRect);
            ComicInk.Caption(page.CaptionRect, subtitle);
            ComicInk.Stamp(page.PointsStampRect, $"{unspentPoints} PTS");

            for (int c = 0; c < page.ColumnCount; c++)
            {
                var panel = page.ColumnPanels[c];
                ComicInk.Panel(panel, page.TitleOf(c));

                var nodes = page.ColumnNodes(c);
                foreach (var box in nodes)
                {
                    if (!box.Visible) continue;
                    DrawSkillBox(box);
                    if (c == page.Column && box.RowInColumn == page.Row) ComicInk.Cursor(box.Box);
                }

                DrawScrollRule(panel, page.ScrollOf(c), page.VisibleRows, nodes.Count);
            }

            var sel = page.Selected;
            ComicInk.Caption(page.DetailRect, sel == null ? "—" : $"{sel.Node.Name.ToUpperInvariant()}   {sel.Node.Text}");
        }

        private static void DrawSkillBox(SkillBox box)
        {
            var r = box.Box;
            switch (box.Node.State)
            {
                case SkillState.Taken:
                    // Taken is stamped, not ticked. A stamp on a form is unarguable and needs no key.
                    ComicInk.PaperFill(r);
                    ComicInk.Border(r, 2f);
                    ComicInk.Stamp(r.Inset(6f), box.Node.Name);
                    break;

                case SkillState.Available:
                    ComicInk.PaperFill(r);
                    ComicInk.Border(r, 2f);
                    ComicInk.Body(new ComicRect(r.X + 9f, r.Y + 6f, r.W - 60f, 22f), box.Node.Name);
                    ComicInk.Small(new ComicRect(r.X + 9f, r.Bottom - 22f, r.W - 60f, 18f), box.Node.Text);
                    ComicInk.BigNumber(new ComicRect(r.Right - 52f, r.Y, 44f, r.H), box.Node.Cost.ToString(), ComicInk.Ink);
                    break;

                default:
                    ComicInk.Pencil(r, $"{box.Node.Name}   {box.Node.Cost}");
                    break;
            }
        }

        /// <summary>
        /// A scroll rule down the column's right edge. The owner's third complaint about this screen
        /// was "there's no way for me to scroll the skills list" — so when there is more below, the
        /// page has to say so without being asked.
        /// </summary>
        private static void DrawScrollRule(ComicRect panel, int scroll, int visibleRows, int total)
        {
            if (total <= visibleRows) return;

            var track = new ComicRect(panel.Right - 9f, panel.Y + 38f, 4f, panel.H - 48f);
            ComicInk.Halftone(track, 0.5f);

            float span = Mathf.Clamp01(visibleRows / (float)total);
            float at = scroll / (float)Mathf.Max(1, total - visibleRows);
            float thumbH = Mathf.Max(14f, track.H * span);
            ComicInk.Fill(new ComicRect(track.X, track.Y + (track.H - thumbH) * at, track.W, thumbH), ComicInk.Amber);
        }

        // ================================================================== truck

        /// <summary>
        /// The bed and the gravel. Crates in the bed are inked; crates left on the gravel are drawn in
        /// pencil, which is this toolkit's word for "not yours" — here meaning "not coming with you".
        /// </summary>
        public static void DrawTruck(TruckPageLayout page, string subtitle)
        {
            Gutter(page.PageRect);
            ComicInk.Caption(page.CaptionRect, subtitle);

            ComicInk.Panel(page.TruckPanel, "On the truck");
            DrawTruckBody(page);

            foreach (var crate in page.Bed)
            {
                DrawCrate(crate, inked: true);
                if (page.Zone == TruckZone.Bed && ReferenceEquals(crate, page.Selected)) ComicInk.Cursor(crate.Box);
            }

            ComicInk.Panel(page.LeftBehindPanel, $"Left behind ({page.LeftBehind.Count})");
            foreach (var crate in page.LeftBehind)
            {
                DrawCrate(crate, inked: false);
                if (page.Zone == TruckZone.LeftBehind && ReferenceEquals(crate, page.Selected)) ComicInk.Cursor(crate.Box);
            }

            ComicInk.Gauge(page.WeightGauge, page.WeightFraction,
                           $"Weight  {page.Weight:F0} / {page.MaxWeight:F0} kg");
            ComicInk.Gauge(page.VolumeGauge, page.VolumeFraction,
                           $"Volume  {page.Volume:F1} / {page.MaxVolume:F1} m3");
        }

        private static void DrawTruckBody(TruckPageLayout page)
        {
            var truck = page.TruckPanel;
            ComicInk.InkBlock(new ComicRect(truck.X + 14f, page.GroundY, truck.W - 28f, 3f));

            var cab = page.CabRect;
            ComicInk.PaperFill(cab);
            ComicInk.Border(cab, ComicInk.BorderWidth);
            // A window band and a wheel arch are the two marks that turn a box into a truck.
            var window = new ComicRect(cab.X + 8f, cab.Y + 8f, cab.W - 16f, cab.H * 0.34f);
            ComicInk.InkBlock(window);
            ComicInk.Halftone(window.Inset(3f), 0.68f, ComicInk.Paper);

            var bed = page.BedRect;
            ComicInk.PaperFill(bed);
            ComicInk.Border(bed, ComicInk.BorderWidth);

            ComicInk.Ring(page.WheelFront, 8f, ComicInk.Ink);
            ComicInk.Ring(page.WheelRear, 8f, ComicInk.Ink);
        }

        private static void DrawCrate(TruckCrate crate, bool inked)
        {
            var r = crate.Box;
            if (r.IsEmpty) return;

            if (!inked)
            {
                ComicInk.Pencil(r, crate.Item.Name);
                return;
            }

            ComicInk.PaperFill(r);
            // Darkness is weight. Two crates that take the same floor can print very differently, and
            // that is the whole "a heavy sentry or four light rotors" trade ADR-005 asks the player to make.
            ComicInk.Halftone(r.Inset(3f), crate.ShadeDensity * 0.7f);
            ComicInk.Border(r, 2f);
            if (r.W > 54f) ComicInk.Small(new ComicRect(r.X + 5f, r.CenterY - 9f, r.W - 10f, 18f), crate.Item.Name);
        }
    }
}
