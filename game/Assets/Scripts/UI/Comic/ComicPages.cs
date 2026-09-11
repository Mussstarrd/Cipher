using System;
using System.Collections.Generic;
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
                    DrawEffects(new ComicRect(box.X + 6f, box.Y + 40f, box.W - 12f, box.H - 44f),
                                slot.Item.Effects);
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
                ComicInk.Small(new ComicRect(card.X + 6f, card.Y + 3f, card.W - 12f, 14f),
                               item.Slot.ToUpperInvariant());
                ComicInk.Body(new ComicRect(card.X + 6f, card.Y + 17f, card.W - 12f, 20f), item.Name);
                DrawEffects(new ComicRect(card.X + 6f, card.Y + 38f, card.W - 12f, card.H - 42f),
                            item.Effects);
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
            // A LOSS GETS NO SCREEN AT ALL. A halftone behind a minus sign eats the minus sign,
            // and "is that a 0.8 or a -0.8" is the one question this number exists to answer.
            ComicInk.Border(delta, ComicInk.BorderWidth);

            string text = page.HasComparison ? page.DeltaText : "—";
            ComicInk.BigNumber(new ComicRect(delta.X, delta.Y, delta.W, delta.H - 16f),
                               text, ComicInk.Ink);
            if (page.HasComparison)
                ComicInk.Small(new ComicRect(delta.X, delta.Bottom - 18f, delta.W, 15f),
                               page.DeltaSign > 0 ? "better overall" : "worse overall", centred: true);
        }

        /// <summary>
        /// The plain lines that say what a piece of gear does. Clipped to the space available and
        /// the remainder counted, because a helmet with five affixes must not spill onto the one
        /// below it -- and "+2 more" is honest where silently dropping them is not.
        /// </summary>
        private static void DrawEffects(ComicRect area, IReadOnlyList<string> effects)
        {
            if (effects == null || effects.Count == 0 || area.H < 14f) return;

            const float Line = 14f;
            // The margin is why: without it the last line sits ON the card's border and is sheared
            // in half, which reads as a rendering fault rather than as a full box.
            int room = Math.Max(0, (int)((area.H - 3f) / Line));
            if (room == 0) return;

            int shown = Math.Min(room, effects.Count);
            bool truncated = effects.Count > room;

            // With room for exactly one line, "+2 more" would be the only thing on the card: a fact
            // about the card instead of a fact about the gear. Show the real line and drop the
            // counter -- the swap panel is where the full list belongs anyway.
            if (truncated && room == 1) { truncated = false; shown = 1; }
            else if (truncated && shown > 0) shown--;

            for (int i = 0; i < shown; i++)
                ComicInk.Small(new ComicRect(area.X, area.Y + i * Line, area.W, Line), effects[i]);

            if (truncated)
                ComicInk.Small(new ComicRect(area.X, area.Y + shown * Line, area.W, Line),
                               $"+{effects.Count - shown} more");
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
            DrawEffects(new ComicRect(card.X + 8f, card.Y + 46f, card.W - 16f, card.H - 78f),
                        item.Value.Effects);
            // The score stays, small, at the foot: it is the single number that says which is
            // better overall once the effects disagree, and that is ALL it is. It was the only
            // thing on the card, which made it a riddle rather than a summary.
            ComicInk.Small(new ComicRect(card.X + 8f, card.Bottom - 22f, card.W - 16f, 16f),
                           $"overall  {item.Value.Power:F1}");
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
                {
                    ComicInk.PaperFill(r);
                    // An AMBER SPINE means "you have already put points here".
                    //
                    // Without it a node at rank 2 of 5 and a node at rank 0 print identically,
                    // because both are buyable — which is exactly what the first capture showed, and
                    // it makes a whole column of investment invisible. The stamp is reserved for a
                    // node that is finished; this is the mark for one that is under way.
                    if (box.Node.Invested) ComicInk.Fill(new ComicRect(r.X + 3f, r.Y + 3f, 6f, r.H - 6f), ComicInk.Amber);
                    ComicInk.Border(r, 2f);
                    float textX = r.X + (box.Node.Invested ? 17f : 9f);
                    ComicInk.Body(new ComicRect(textX, r.Y + 6f, r.Right - 54f - textX, 22f), box.Node.Name);
                    ComicInk.Small(new ComicRect(textX, r.Bottom - 22f, r.Right - 54f - textX, 18f), box.Node.Text);
                    ComicInk.MidNumber(new ComicRect(r.Right - 48f, r.Y, 40f, r.H), box.Node.Cost.ToString(), ComicInk.Ink);
                    break;
                }

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

            var picked = page.Selected;
            ComicInk.Caption(page.DetailRect, picked == null
                ? "nothing selected"
                : $"{picked.Item.Name.ToUpperInvariant()}     {picked.Item.Weight:F0} kg     "
                  + $"{picked.Item.Volume:F1} m3     {(picked.InBed ? "— coming with you" : "— staying here")}");

            ComicInk.Gauge(page.WeightGauge, page.WeightFraction,
                           $"Weight  {page.Weight:F0} / {page.MaxWeight:F0} kg");
            ComicInk.Gauge(page.VolumeGauge, page.VolumeFraction,
                           $"Volume  {page.Volume:F1} / {page.MaxVolume:F1} m3");
        }

        private static void DrawTruckBody(TruckPageLayout page)
        {
            var truck = page.TruckPanel;
            ComicInk.InkBlock(new ComicRect(truck.X + 14f, page.GroundY, truck.W - 28f, 3f));

            var hood = page.HoodRect;
            ComicInk.PaperFill(hood);
            ComicInk.Border(hood, ComicInk.BorderWidth);
            // Grille and lamp. Two marks, and the truck is suddenly facing somewhere.
            ComicInk.InkBlock(new ComicRect(hood.X + 3f, hood.Y + hood.H * 0.34f, 7f, hood.H * 0.34f));
            ComicInk.Halftone(new ComicRect(hood.X + 12f, hood.Y + hood.H * 0.30f, hood.W * 0.34f, hood.H * 0.42f), 0.5f);

            var cab = page.CabRect;
            ComicInk.PaperFill(cab);
            ComicInk.Border(cab, ComicInk.BorderWidth);

            // A window band high in the cab, a pillar splitting windscreen from door glass, and a
            // dark sill under it. The glass is nearly paper so the people inside it read as ink.
            var window = new ComicRect(cab.X + 10f, cab.Y + cab.H * 0.13f, cab.W - 20f, cab.H * 0.34f);
            ComicInk.InkBlock(window);
            ComicInk.Halftone(window.Inset(3f), 0.85f, ComicInk.Paper);
            DrawCabOccupants(window.Inset(3f));
            ComicInk.InkBlock(new ComicRect(window.X + window.W * 0.30f, window.Y, 4f, window.H));
            ComicInk.InkBlock(new ComicRect(window.X, window.Bottom + 5f, window.W, 4f));

            // Two door shuts and a handle. Three marks, and the cab stops being a paper rectangle.
            float shut = cab.X + cab.W * 0.42f;
            float doorTop = window.Bottom + 9f;
            float doorH = Mathf.Max(0f, cab.Bottom - doorTop - 4f);
            ComicInk.InkBlock(new ComicRect(shut, doorTop, 2f, doorH));
            ComicInk.InkBlock(new ComicRect(cab.Right - 5f, doorTop, 2f, doorH));
            ComicInk.InkBlock(new ComicRect(shut + 9f, doorTop + 14f, 17f, 4f));

            var bed = page.BedRect;
            ComicInk.PaperFill(bed);
            ComicInk.Border(bed, ComicInk.BorderWidth);

            // Wheels last, over the body, so each ring reads as an arch cut into the panel.
            ComicInk.Ring(page.WheelFront, 9f, ComicInk.Ink);
            ComicInk.Ring(page.WheelRear, 9f, ComicInk.Ink);
            ComicInk.Disc(page.WheelFront.Inset(page.WheelFront.W * 0.34f), ComicInk.Ink);
            ComicInk.Disc(page.WheelRear.Inset(page.WheelRear.W * 0.34f), ComicInk.Ink);
        }

        /// <summary>
        /// Three heads in the cab window: one adult and, behind her, two small ones.
        ///
        /// ADR-009 made the truck the family, and this page is the one place in the game where the
        /// player is asked to choose what comes with them. Drawing them is what turns a cargo
        /// manifest into a decision about the bed space left over around two car seats. It gets three
        /// outlined circles and nothing else — no faces, no names, no mechanic. The ADR is explicit
        /// that they are roles rather than identities, and a page that laboured the point would be
        /// worse than one that never made it.
        /// </summary>
        private static void DrawCabOccupants(ComicRect window)
        {
            if (window.W < 34f || window.H < 14f) return;

            float adult = Mathf.Min(window.H * 0.52f, window.W * 0.20f);
            float child = adult * 0.70f;
            float baseY = window.Bottom;

            Occupant(window.X + window.W * 0.14f, baseY, adult);
            Occupant(window.X + window.W * 0.50f, baseY, child);
            Occupant(window.X + window.W * 0.74f, baseY, child);
        }

        /// <summary>A head and a pair of shoulders in solid ink, cut off by the sill.</summary>
        private static void Occupant(float cx, float baseY, float headD)
        {
            if (headD < 4f) return;
            float shoulderW = headD * 1.7f;
            float headY = baseY - headD * 2.1f;
            ComicInk.Disc(new ComicRect(cx - headD * 0.5f, headY, headD, headD), ComicInk.Ink);
            ComicInk.InkBlock(new ComicRect(cx - shoulderW * 0.5f, headY + headD * 0.9f,
                                            shoulderW, baseY - (headY + headD * 0.9f)));
        }

        private static void DrawCrate(TruckCrate crate, bool inked)
        {
            var r = crate.Box;
            if (r.IsEmpty) return;

            if (!inked)
            {
                // A ground rule under every crate, so the gravel is a place rather than a list.
                ComicInk.InkBlock(new ComicRect(r.X - 3f, r.Bottom + 2f, r.W + 6f, 2f));
                ComicInk.Pencil(r, string.Empty);
                // A lid line: three pixels that stop a hatched rectangle looking like a swatch.
                ComicInk.Fill(new ComicRect(r.X + 2f, r.Y + r.H * 0.22f, r.W - 4f, 1f), ComicInk.PencilGrey);

                // Width is volume and volume is the argument, so a narrow crate does NOT grow to fit
                // its label — it goes unnamed and the caption band names whatever the cursor is on.
                if (r.W > 56f)
                {
                    var tag = new ComicRect(r.X + 5f, r.CenterY - 11f, r.W - 10f, 22f);
                    ComicInk.PaperFill(tag);
                    ComicInk.Fill(new ComicRect(tag.X, tag.Y, tag.W, 1f), ComicInk.PencilGrey);
                    ComicInk.Fill(new ComicRect(tag.X, tag.Bottom - 1f, tag.W, 1f), ComicInk.PencilGrey);
                    ComicInk.Small(tag.Inset(4f, 0f, 3f, 0f), crate.Item.Name);
                }
                return;
            }

            ComicInk.PaperFill(r);
            // Darkness is weight. Two crates that take the same floor can print very differently, and
            // that is the whole "a heavy sentry or four light rotors" trade ADR-005 asks the player to make.
            ComicInk.Halftone(r.Inset(3f), crate.ShadeDensity * 0.55f);
            ComicInk.Border(r, 2f);

            // The name goes on a stencilled paper band, not straight onto the screen. Small ink text
            // over a halftone is unreadable at any density worth printing.
            if (r.W > 56f)
            {
                var band = new ComicRect(r.X + 5f, r.CenterY - 11f, r.W - 10f, 22f);
                ComicInk.PaperFill(band);
                ComicInk.Border(band, 1f);
                ComicInk.Small(band.Inset(4f, 0f, 3f, 0f), crate.Item.Name);
            }
        }
    }
}
