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
        /// THE PACK-UP PAGE. Top left, the truck in side elevation with the bed filled to scale;
        /// under it, a card for every emplacement aboard. Right, the same cards for everything
        /// standing on the gravel. Top right, the clock, as big as it deserves to be.
        ///
        /// The previous version photographed as a technical drawing: outlines, rulers and a row of
        /// blank hatched rectangles. Three things fix that, and none of them is a new primitive —
        /// the toolkit already had all of it and the page was not using it.
        ///
        ///   - **Mass.** The vehicle is halftoned and shadowed rather than outlined, the tyres are
        ///     solid, and it stands on a cast shadow. An unfilled shape reads as a plan drawing no
        ///     matter how good its proportions are.
        ///   - **Two states, drawn differently.** Aboard is inked — paper plate, hard border, amber
        ///     spine. On the gravel is pencilled — hatch, hairline, grey type. That is this
        ///     toolkit's existing word for "not yours" and it needs no key.
        ///   - **Type on plates.** Every label sits on its own paper band. Small ink text straight
        ///     over a halftone is unreadable at any density worth printing, which is why the old
        ///     page simply gave up on naming anything narrow.
        /// </summary>
        public static void DrawTruck(TruckPageLayout page, in TruckPageCopy copy)
        {
            Gutter(page.PageRect);
            ComicInk.Caption(page.CaptionRect, copy.Subtitle);
            DrawWarning(page.WarnRect, copy.Warning, copy.Urgency);
            DrawClock(page.ClockRect, copy.ClockText, copy.ClockLabel, copy.Urgency);

            ComicInk.Panel(page.TruckPanel, $"On the truck ({page.Bed.Count})");
            DrawTruckBody(page);
            ComicInk.Small(page.CabNoteRect, copy.CabLine);

            foreach (var crate in page.Bed)
            {
                DrawSlab(crate);
                DrawCard(crate, inked: true);
                if (page.Zone == TruckZone.Bed && ReferenceEquals(crate, page.Selected)) ComicInk.Cursor(crate.Box);
            }

            ComicInk.Panel(page.LeftBehindPanel, $"Left on the gravel ({page.LeftBehind.Count})");
            foreach (var crate in page.LeftBehind)
            {
                DrawCard(crate, inked: false);
                if (page.Zone == TruckZone.LeftBehind && ReferenceEquals(crate, page.Selected)) ComicInk.Cursor(crate.Box);
            }

            ComicInk.Caption(page.DetailRect, copy.Ledger);

            ComicInk.Gauge(page.WeightGauge, page.WeightFraction,
                           $"Weight  {page.Weight:F0} / {page.MaxWeight:F0} kg");
            ComicInk.Gauge(page.VolumeGauge, page.VolumeFraction,
                           $"Bed space  {page.Volume:F1} / {page.MaxVolume:F1} m3");
        }

        /// <summary>
        /// The countdown, as a stamped plate rather than a clause in a sentence.
        ///
        /// The owner: "after a certain amount of time it just kicked me out to that fell back
        /// screen". It did tell him — in a header line, after the words PACK UP and a dot. This is
        /// the second largest thing on the page, it gains an amber wash at thirty seconds and a
        /// heavier plate at ten, and the word under the number says what it is counting so the
        /// number is never on its own.
        /// </summary>
        private static void DrawClock(ComicRect r, string text, string label, TruckUrgency urgency)
        {
            if (r.IsEmpty) return;

            ComicInk.InkBlock(r.Offset(ComicInk.ShadowOffset, ComicInk.ShadowOffset));
            ComicInk.PaperFill(r);

            // The wash is a HALFTONE, not a flat tint: a solid amber plate would swallow the ink
            // numerals, and a screen behind them still reads as alarm at a glance.
            if (urgency == TruckUrgency.Hurry) ComicInk.Halftone(r.Inset(5f), 0.45f, ComicInk.Amber);
            else if (urgency == TruckUrgency.Final) ComicInk.Halftone(r.Inset(5f), 0.85f, ComicInk.Amber);

            ComicInk.Border(r, urgency == TruckUrgency.Final ? 5f : ComicInk.BorderWidth);

            ComicInk.BigNumber(new ComicRect(r.X, r.Y + 2f, r.W, r.H - 20f), text, ComicInk.Ink);
            ComicInk.Small(new ComicRect(r.X, r.Bottom - 19f, r.W, 16f), label, centred: true);
        }

        /// <summary>
        /// The band that says the window is closing, and what closing costs. Blank while there is
        /// time, so the fact that there is anything here at all is the first thing the player reads.
        /// </summary>
        private static void DrawWarning(ComicRect r, string text, TruckUrgency urgency)
        {
            if (r.IsEmpty || string.IsNullOrEmpty(text) || urgency == TruckUrgency.Calm) return;

            ComicInk.InkBlock(r.Offset(4f, 4f));
            ComicInk.PaperFill(r);
            // Light enough that ink type still reads over it. An amber screen dense enough to be
            // felt across the room is also dense enough to eat the sentence printed on it, and the
            // sentence is the whole point of the band.
            ComicInk.Halftone(r.Inset(3f), urgency == TruckUrgency.Final ? 0.55f : 0.3f, ComicInk.Amber);
            ComicInk.Border(r, ComicInk.BorderWidth);
            ComicInk.Body(r.Inset(12f, 0f, 8f, 0f), text);
        }

        private static void DrawTruckBody(TruckPageLayout page)
        {
            var truck = page.TruckPanel;
            var hood = page.HoodRect;
            var cab = page.CabRect;
            var bed = page.BedRect;
            float sill = cab.Bottom;

            // The ground, and the shadow the truck casts on it. A vehicle drawn without a shadow
            // floats, and floating is the single clearest tell of a diagram. The shadow stops at
            // the rear wheel rather than running the width of the panel, or it stops being a
            // shadow and becomes a rule.
            ComicInk.InkBlock(new ComicRect(truck.X + 14f, page.GroundY, truck.W - 28f, 3f));
            ComicInk.Halftone(new ComicRect(hood.X - 4f, page.GroundY + 3f,
                                            page.WheelRear.Right - hood.X + 10f, 6f), 0.5f);

            // --- the bonnet. Lower than the cab, which is what says pickup before anything else.
            ComicInk.PaperFill(hood);
            ComicInk.Border(hood, ComicInk.BorderWidth);
            // Grille and lamp. Two marks, and the truck is facing somewhere.
            ComicInk.InkBlock(new ComicRect(hood.X + 3f, hood.Y + hood.H * 0.30f, 7f, hood.H * 0.36f));
            ComicInk.Halftone(new ComicRect(hood.X + 12f, hood.Y + hood.H * 0.26f,
                                            hood.W * 0.34f, hood.H * 0.44f), 0.5f);

            // --- the cab. Left as paper on purpose: it is the lightest volume on the vehicle and
            // the bed is the darkest, and that contrast is what stops the two merging into a van.
            ComicInk.PaperFill(cab);
            ComicInk.Border(cab, ComicInk.BorderWidth);

            var window = new ComicRect(cab.X + 9f, cab.Y + cab.H * 0.11f, cab.W - 18f, cab.H * 0.31f);
            ComicInk.InkBlock(window);
            ComicInk.Halftone(window.Inset(3f), 0.85f, ComicInk.Paper);
            DrawCabOccupants(window.Inset(3f));
            ComicInk.InkBlock(new ComicRect(window.X + window.W * 0.30f, window.Y, 4f, window.H));
            ComicInk.InkBlock(new ComicRect(window.X, window.Bottom + 5f, window.W, 4f));

            // Two door shuts and a handle — a four-door, which is the entire premise of ADR-009.
            float shut = cab.X + cab.W * 0.46f;
            float doorTop = window.Bottom + 9f;
            float doorH = Mathf.Max(0f, cab.Bottom - doorTop - 5f);
            ComicInk.InkBlock(new ComicRect(shut, doorTop, 2f, doorH));
            ComicInk.InkBlock(new ComicRect(cab.Right - 5f, doorTop, 2f, doorH));
            ComicInk.InkBlock(new ComicRect(shut + 9f, doorTop + Mathf.Min(14f, doorH * 0.3f), 16f, 4f));

            // --- the bed. Screened all over rather than striped along the bottom: a container is
            // darker inside than the cab is outside, and one flat screen says that in one mark.
            // The earlier version put a band along the lower flank of all three volumes at once,
            // which welded them together into a single grey smear.
            ComicInk.PaperFill(bed);
            ComicInk.Halftone(bed.Inset(4f), 0.3f);
            ComicInk.Border(bed, ComicInk.BorderWidth);
            // The rub rail along the top of the bed wall, so the empty part of the bed is a space
            // rather than a hole in the drawing.
            ComicInk.InkBlock(new ComicRect(bed.X, bed.Y, bed.W, 4f));

            // --- the rocker. One solid bar along the whole sill. It ties the three volumes into
            // one vehicle and reads as the shadow under the body, which is the job the lower-flank
            // screens were failing at.
            ComicInk.InkBlock(new ComicRect(hood.X, sill - 4f, bed.Right - hood.X, 5f));

            // --- wheels last, hanging half below the sill so each ring reads as an arch cut into
            // the body rather than as a circle parked under it.
            DrawWheel(page.WheelFront);
            DrawWheel(page.WheelRear);
        }

        /// <summary>A tyre: a paper arch, a heavy ring and a solid hub.</summary>
        private static void DrawWheel(ComicRect r)
        {
            if (r.IsEmpty) return;
            // The paper disc first, so the arch is cut OUT of the body rather than drawn over it.
            ComicInk.Disc(r.Inset(-3f), ComicInk.Paper);
            ComicInk.Ring(r, Mathf.Max(6f, r.W * 0.22f), ComicInk.Ink);
            ComicInk.Disc(r.Inset(r.W * 0.34f), ComicInk.Ink);
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

        /// <summary>
        /// The to-scale block inside the bed. This is the only thing on the page whose size is a
        /// quantity, and it is a picture rather than a list item: it is allowed to be twelve pixels
        /// wide, because its card says what it is.
        ///
        /// Darkness is weight per cubic metre — same footprint, darker print = the thing that costs
        /// you axle rather than floor, which is the trade ADR-005 asks the player to make.
        /// </summary>
        private static void DrawSlab(TruckCrate crate)
        {
            var r = crate.Slab;
            if (r.IsEmpty || r.W < 1.5f) return;

            ComicInk.PaperFill(r);
            ComicInk.Halftone(r.Inset(Mathf.Min(3f, r.W * 0.2f)), crate.ShadeDensity * 0.6f);
            ComicInk.Border(r, 2f);

            // The manifest number, if it will fit. It is the thread back to the card that names it.
            if (r.W > 22f && r.H > 20f)
            {
                var tag = new ComicRect(r.CenterX - 10f, r.CenterY - 10f, 20f, 20f);
                ComicInk.PaperFill(tag);
                ComicInk.Border(tag, 1f);
                ComicInk.Small(tag, crate.Tag.ToString(), centred: true);
            }
        }

        /// <summary>
        /// A labelled card. THE fix to "nothing was labeled": every item gets one, both states get
        /// the same three facts, and neither state has to earn its name by being wide enough.
        ///
        /// Aboard is INKED — a paper plate with a drop shadow, a hard border and an amber spine.
        /// On the gravel is PENCILLED — hatched ground, hairline, grey type, no shadow, and a ground
        /// rule under it so it is standing somewhere rather than listed somewhere. The two are meant
        /// to be told apart across a room without reading either.
        /// </summary>
        private static void DrawCard(TruckCrate crate, bool inked)
        {
            var r = crate.Box;
            if (r.IsEmpty) return;

            string stats = $"{crate.Item.Weight:F0} kg    {crate.Item.Volume:F1} m3";
            string worth = crate.Item.Value > 0 ? $"${crate.Item.Value}" : string.Empty;

            if (inked)
            {
                ComicInk.InkBlock(r.Offset(5f, 5f));
                ComicInk.PaperFill(r);
                // The amber spine is this toolkit's existing mark for "you have committed to this",
                // borrowed from the skills page rather than invented. It is also the only colour on
                // the card, which is what makes a full bed read at a glance.
                ComicInk.Fill(new ComicRect(r.X + 3f, r.Y + 3f, 6f, r.H - 6f), ComicInk.Amber);
                ComicInk.Border(r, ComicInk.BorderWidth);

                float x = r.X + 16f;
                float w = r.Right - 10f - x;
                ComicInk.Small(new ComicRect(x, r.Y + 4f, w, 14f), $"{crate.Tag}.");
                ComicInk.Body(new ComicRect(x, r.Y + 17f, w, 20f), crate.Item.Name);
                ComicInk.Small(new ComicRect(x, r.Bottom - 21f, w, 16f), stats);
                if (worth.Length > 0)
                    ComicInk.Small(new ComicRect(r.Right - 66f, r.Bottom - 21f, 56f, 16f), worth, centred: true);
                return;
            }

            // A ground rule under every card, so the gravel is a place rather than a list.
            ComicInk.InkBlock(new ComicRect(r.X + 2f, r.Bottom + 2f, r.W - 4f, 2f));
            ComicInk.Pencil(r, string.Empty);

            // The NAME goes on a paper strip. Grey type straight over a hatch is what made the old
            // crates unreadable even on the three that were wide enough to carry a label at all.
            var band = new ComicRect(r.X + 5f, r.Y + 5f, r.W - 10f, 22f);
            ComicInk.PaperFill(band);
            ComicInk.Border(band, 1f);
            ComicInk.Body(band.Inset(5f, 0f, 4f, 0f), crate.Item.Name);

            var line = new ComicRect(r.X + 5f, r.Bottom - 21f, r.W - 10f, 16f);
            ComicInk.PaperFill(line);
            ComicInk.Small(line.Inset(5f, 0f, 4f, 0f),
                           worth.Length > 0 ? $"{stats}    {worth}" : stats);
        }
    }
}
