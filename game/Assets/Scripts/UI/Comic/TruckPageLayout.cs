#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// One thing that could go in the bed. Generic (a name and three numbers) so this page never has
    /// to know what a turret is; the integrator maps TruckLoad's Haulage onto it.
    /// </summary>
    public readonly struct TruckItemView
    {
        public readonly string Name;

        /// <summary>Kilograms. Printed on the card and drives how dark the bed slab prints.</summary>
        public readonly float Weight;

        /// <summary>Cubic metres. Printed on the card and drives how much of the bed the slab eats.</summary>
        public readonly float Volume;

        /// <summary>Dollars back if it gets home. Printed on the card — see <see cref="TruckCrate"/>.</summary>
        public readonly int Value;

        public TruckItemView(string name, float weight, float volume, int value = 0)
        {
            Name = name ?? string.Empty;
            Weight = Math.Max(0f, weight);
            Volume = Math.Max(0f, volume);
            Value = Math.Max(0, value);
        }

        public override string ToString() => $"{Name} {Weight:F0}kg {Volume:F1}m3";
    }

    public enum TruckZone { Bed, LeftBehind }

    public enum TruckAction { None, Load, Unload, Blocked, Close }

    /// <summary>
    /// A crate as drawn. It has TWO rectangles and the split is the whole fix to the owner's
    /// "nothing was labeled" (2026-09-11):
    ///
    ///   - <see cref="Slab"/> is the proportional block inside the truck bed. Its width IS its share
    ///     of the hold, which is the honest picture of how full the truck is, and which is also why
    ///     it can be twelve pixels wide and can never carry a name.
    ///   - <see cref="Box"/> is the CARD: a fixed, legible rectangle in a grid, big enough to print
    ///     what the thing is called, what it weighs, what it takes up and what it is worth.
    ///
    /// Every item gets a card. The cursor lives on cards. The bed slab is a drawing, not a list.
    /// Trying to make one rectangle do both jobs is what produced twenty-six blank boxes.
    /// </summary>
    public sealed class TruckCrate
    {
        public int Index;
        public TruckItemView Item;

        /// <summary>The labelled card. What the cursor highlights and what the player reads.</summary>
        public ComicRect Box;

        /// <summary>The to-scale block in the bed. Zero when this crate is on the gravel.</summary>
        public ComicRect Slab;

        public bool InBed;

        /// <summary>
        /// 1-based manifest number, printed on the card and stencilled on the bed slab so a slab too
        /// narrow to name can still be traced to the card that names it.
        /// </summary>
        public int Tag;

        /// <summary>
        /// 0..1 halftone density, from weight per cubic metre. Same footprint, darker print = the
        /// thing that will cost you axle rather than floor.
        /// </summary>
        public float ShadeDensity;
    }

    /// <summary>
    /// THE TRUCK PAGE: a side elevation of the bed you are filling, a labelled manifest of what is
    /// in it, and — with equal weight on the same page — a labelled manifest of what you are about
    /// to abandon.
    ///
    /// ## What the owner said, and what changed
    ///
    /// 2026-09-11, having played it:
    ///
    /// > "I also have no idea what I packed the truck with there was nothing was labeled or gave me
    /// > any values or anything"
    ///
    /// The previous version drew every item as a rectangle whose WIDTH was its volume, and named it
    /// only if that came out wider than 56 pixels. Fed the real extraction list — six emplacements
    /// and twenty pack items — that is twenty-six identical blank boxes, because a dog tag is
    /// 0.0005 cubic metres. The page was internally consistent and completely unreadable.
    ///
    /// Two changes fix it, and both are about honesty rather than decoration:
    ///
    /// 1. **Cards, not slabs, carry the labels.** Every item has a card sized for its name and its
    ///    three numbers, in a grid that fits the panel. Volume still draws the bed, to scale, as a
    ///    picture — it just stopped being the thing that decides whether you get to know what you
    ///    are looking at.
    /// 2. **Gear is not on this page at all.** The integrator (<see cref="TruckScreen"/>) keeps only
    ///    bed-scale things here and says, once, that the kit rides in the cab. A volume gauge whose
    ///    readings are 2.4 and 0.0005 is a gauge that means nothing, and ADR-005 is explicit that
    ///    gear is never the thing you cut. Making the page refuse to pretend otherwise is what makes
    ///    both gauges worth reading.
    ///
    /// The layout OWNS the loaded flags. It is a selection screen, so it holds the choice and the
    /// integrator mirrors the result into TruckLoad on the press.
    /// </summary>
    public sealed class TruckPageLayout
    {
        /// <summary>Gap between cards in a grid, and between a grid and its panel title.</summary>
        private const float CardGap = 8f;

        /// <summary>A card below this is not worth printing three lines on. Columns grow instead.</summary>
        private const float MinCardHeight = 44f;

        /// <summary>And above this a grid of four items looks like four doors.</summary>
        private const float MaxCardHeight = 86f;

        private readonly List<TruckCrate> _all = new List<TruckCrate>();
        private readonly List<TruckCrate> _bed = new List<TruckCrate>();
        private readonly List<TruckCrate> _left = new List<TruckCrate>();

        public float MaxWeight { get; }
        public float MaxVolume { get; }

        public TruckPageLayout(IReadOnlyList<TruckItemView> items,
                               IReadOnlyList<bool>? loaded,
                               float maxWeight,
                               float maxVolume)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            MaxWeight = Math.Max(1f, maxWeight);
            MaxVolume = Math.Max(0.01f, maxVolume);

            // Darkness is relative to the densest thing on the page, so the contrast is always used.
            float densest = 0f;
            foreach (var it in items)
            {
                if (it.Volume <= 0f) continue;
                densest = Math.Max(densest, it.Weight / it.Volume);
            }

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                float density = it.Volume > 0f && densest > 0f ? it.Weight / it.Volume / densest : 0.5f;
                _all.Add(new TruckCrate
                {
                    Index = i,
                    Tag = i + 1,
                    Item = it,
                    InBed = loaded != null && i < loaded.Count && loaded[i],
                    ShadeDensity = 0.15f + 0.85f * Clamp01(density),
                });
            }

            Repartition();
            Zone = _bed.Count > 0 ? TruckZone.Bed : TruckZone.LeftBehind;
        }

        // ------------------------------------------------------------------ contents

        public IReadOnlyList<TruckCrate> All => _all;
        public IReadOnlyList<TruckCrate> Bed => _bed;

        /// <summary>Exactly the crates that are not in the bed. The panel that matters.</summary>
        public IReadOnlyList<TruckCrate> LeftBehind => _left;

        public float Weight { get; private set; }
        public float Volume { get; private set; }
        public float WeightFraction => Weight / MaxWeight;
        public float VolumeFraction => Volume / MaxVolume;

        /// <summary>Dollars riding in the bed. The page prints it; leaving is a transaction.</summary>
        public int LoadedValue { get; private set; }

        /// <summary>Dollars standing on the gravel. The number the whole screen is an argument about.</summary>
        public int AbandonedValue { get; private set; }

        private void Repartition()
        {
            _bed.Clear();
            _left.Clear();
            Weight = 0f;
            Volume = 0f;
            LoadedValue = 0;
            AbandonedValue = 0;
            foreach (var c in _all)
            {
                if (c.InBed)
                {
                    _bed.Add(c);
                    Weight += c.Item.Weight;
                    Volume += c.Item.Volume;
                    LoadedValue += c.Item.Value;
                }
                else
                {
                    _left.Add(c);
                    AbandonedValue += c.Item.Value;
                    c.Slab = ComicRect.Zero;
                }
            }
        }

        public bool Fits(TruckCrate crate) =>
            Weight + crate.Item.Weight <= MaxWeight + 0.0001f &&
            Volume + crate.Item.Volume <= MaxVolume + 0.0001f;

        /// <summary>Which limit refuses this crate. Named, because "no room" was never an answer.</summary>
        public bool TooHeavy(TruckCrate crate) => Weight + crate.Item.Weight > MaxWeight + 0.0001f;

        // ------------------------------------------------------------------ rects

        public ComicRect PageRect { get; private set; }
        public ComicRect CaptionRect { get; private set; }

        /// <summary>
        /// The clock, as a stamp big enough to see from the other side of the room.
        ///
        /// The owner: "after a certain amount of time it just kicked me out". A countdown living in
        /// a header line among four other clauses is not a countdown, it is a footnote. This is the
        /// largest single element on the page after the truck itself.
        /// </summary>
        public ComicRect ClockRect { get; private set; }

        /// <summary>
        /// The band the page shouts in once the window is nearly over. Sits directly under the
        /// caption and is left blank while there is still time, so its arrival is itself the signal.
        /// </summary>
        public ComicRect WarnRect { get; private set; }

        public ComicRect TruckPanel { get; private set; }

        /// <summary>The bonnet, ahead of the cab. What tells you which way it is pointing.</summary>
        public ComicRect HoodRect { get; private set; }

        /// <summary>The cab. His wife in the front, two children behind her (ADR-009).</summary>
        public ComicRect CabRect { get; private set; }

        /// <summary>The bed walls. Slabs go inside BedInner.</summary>
        public ComicRect BedRect { get; private set; }

        public ComicRect BedInner { get; private set; }

        public ComicRect WheelFront { get; private set; }
        public ComicRect WheelRear { get; private set; }

        /// <summary>Y of the ground the truck stands on.</summary>
        public float GroundY { get; private set; }

        /// <summary>The quiet line at the truck's wheels that says the kit rides in the cab.</summary>
        public ComicRect CabNoteRect { get; private set; }

        /// <summary>The grid of labelled cards under the elevation: what is aboard.</summary>
        public ComicRect BedCardArea { get; private set; }

        public ComicRect LeftBehindPanel { get; private set; }

        /// <summary>The grid of labelled cards filling the right-hand panel.</summary>
        public ComicRect GravelCardArea { get; private set; }

        public ComicRect WeightGauge { get; private set; }
        public ComicRect VolumeGauge { get; private set; }

        /// <summary>
        /// A band under the two panels. It no longer has to name the thing under the cursor — every
        /// card does that itself — so it carries the totals, which is the sentence the player is
        /// actually composing: this much is coming, this much is staying.
        /// </summary>
        public ComicRect DetailRect { get; private set; }

        /// <summary>Columns in each grid. Chosen to fit, so nothing is ever hidden off-panel.</summary>
        public int BedColumns { get; private set; }

        public int GravelColumns { get; private set; }

        public void Layout(ComicRect page)
        {
            PageRect = page;
            var inner = page.Inset(18f);

            // The clock takes the top-right corner outright and the caption gives way to it. The
            // clock is the only thing on this page the player cannot afford to miss.
            const float clockW = 210f;
            const float clockH = 62f;
            ClockRect = new ComicRect(inner.Right - clockW, inner.Y - 2f, clockW, clockH);
            CaptionRect = new ComicRect(inner.X, inner.Y, inner.W - clockW - 16f, 34f);
            // Tall enough for eighteen-point type to sit in rather than be squeezed through. At 26
            // the sentence photographed as a cramped strip, which is not what a page shouts with.
            WarnRect = new ComicRect(inner.X, CaptionRect.Bottom + 6f, inner.W - clockW - 16f, 32f);

            const float gaugeH = 56f;
            const float detailH = 30f;
            var gaugeStrip = new ComicRect(inner.X + 10f, inner.Bottom - gaugeH, inner.W - 20f, gaugeH);

            DetailRect = new ComicRect(inner.X, gaugeStrip.Y - 14f - detailH, inner.W, detailH);

            float bodyTop = inner.Y + clockH + 6f;
            var body = new ComicRect(inner.X, bodyTop, inner.W, Math.Max(80f, DetailRect.Y - 12f - bodyTop));

            // EQUAL HALVES, and asserted in the tests. "What gets left behind drawn with equal
            // weight" is the brief; neither the truck nor the loss gets to be the big one.
            const float gutter = 16f;
            float halfW = (body.W - gutter) * 0.5f;
            TruckPanel = new ComicRect(body.X, body.Y, halfW, body.H);
            LeftBehindPanel = new ComicRect(body.X + halfW + gutter, body.Y, halfW, body.H);

            float gg = 26f;
            float gw = (gaugeStrip.W - gg) * 0.5f;
            WeightGauge = new ComicRect(gaugeStrip.X, gaugeStrip.Y, gw, gaugeStrip.H);
            VolumeGauge = new ComicRect(gaugeStrip.X + gw + gg, gaugeStrip.Y, gw, gaugeStrip.H);

            LayoutTruck();
            BedColumns = LayoutCards(BedCardArea, _bed);
            GravelColumns = LayoutCards(GravelCardArea, _left);
        }

        private void LayoutTruck()
        {
            var inner = TruckPanel.Inset(14f, 36f, 14f, 12f);

            // The elevation gets the top of the panel and the manifest gets the rest. Roughly half
            // and half: a picture that eats the whole panel leaves nowhere to say what is in it,
            // which is the fault being fixed.
            float elevH = Math.Min(inner.H * 0.54f, inner.W * 0.42f);

            // A FOUR-DOOR PICKUP IS ABOUT THREE AND A QUARTER TIMES AS LONG AS IT IS TALL.
            //
            // Deriving the length from the height and then CENTRING it is what keeps it a vehicle.
            // Stretching it to the panel width gave a 4.6:1 shape that reads as a bus, and squeezing
            // it into a square panel gave two boxes and two rings.
            // THE ROOF HAS TO CLEAR THE PANEL TITLE, and the arithmetic that decides whether it
            // does runs through the wheels, which hang BELOW the sill. Everything here is derived
            // from one number so that stays true at any page shape:
            //
            //     wheelD     = 0.40 * vehicleH      (a truck tyre is a big part of its side view)
            //     bodyBottom = GroundY - wheelD/2   (the wheels sit half in, half out — arches)
            //     bodyTop    = GroundY - 1.2 * vehicleH
            //
            // so the roof clears the panel while vehicleH <= elevH * 0.667. A first pass used 0.70
            // of the elevation with the wheels ignored and put the cab's window band ELEVEN PIXELS
            // into the title bar. `RoofStaysInsideThePanel` is the guard.
            float vehicleH = Math.Min(elevH * 0.60f, inner.W * 0.30f);
            float vehicleL = Math.Min(inner.W, vehicleH * 3.25f);
            float leftPad = (inner.W - vehicleL) * 0.5f;
            float x0 = inner.X + leftPad;

            float wheelD = vehicleH * 0.40f;

            // The ground sits at four fifths of the elevation, not at its floor. The band below it
            // is not slack: it carries the cast shadow AND the cab note, and the first version put
            // the ground at 0.88 so the two were printed on top of each other.
            GroundY = inner.Y + elevH * 0.80f;
            float bodyBottom = GroundY - wheelD * 0.5f;
            float bodyTop = bodyBottom - vehicleH;

            float hoodW = vehicleL * 0.20f;
            float cabW = vehicleL * 0.34f;
            float hoodH = vehicleH * 0.58f;

            HoodRect = new ComicRect(x0, bodyBottom - hoodH, hoodW, hoodH);
            CabRect = new ComicRect(x0 + hoodW, bodyTop, cabW, vehicleH);

            // A LOW BED IS WHAT MAKES IT A PICKUP. At 0.56 of the cab the bed wall came within a
            // few pixels of the cab's waistline, the two paper rectangles abutted, and the whole
            // vehicle read as one long box with a stripe down it. Two fifths leaves a visible step
            // down from cab to bed, and that step is the silhouette.
            float bedH = vehicleH * 0.40f;
            BedRect = new ComicRect(CabRect.Right, bodyBottom - bedH, x0 + vehicleL - CabRect.Right, bedH);
            BedInner = BedRect.Inset(6f, 7f, 6f, 5f);

            WheelFront = new ComicRect(x0 + hoodW * 0.55f, GroundY - wheelD, wheelD, wheelD);
            WheelRear = new ComicRect(x0 + vehicleL - wheelD * 1.75f, GroundY - wheelD, wheelD, wheelD);

            _pxPerVolume = MaxVolume > 0f ? BedInner.W / MaxVolume : 0f;

            float sx = BedInner.X;
            foreach (var c in _bed)
            {
                float w = c.Item.Volume * _pxPerVolume;
                c.Slab = new ComicRect(sx, BedInner.Y, w, BedInner.H);
                sx += w;
            }

            // Below the ground line AND below the shadow the drawing casts onto it.
            CabNoteRect = new ComicRect(inner.X + 4f, GroundY + 18f,
                                        inner.W - 8f, Math.Max(0f, inner.Y + elevH - GroundY - 20f));

            float cardsTop = inner.Y + elevH + 10f;
            BedCardArea = new ComicRect(inner.X, cardsTop, inner.W, Math.Max(0f, inner.Bottom - cardsTop));
            GravelCardArea = LeftBehindPanel.Inset(14f, 36f, 14f, 12f);
        }

        /// <summary>Pixels per cubic metre inside the bed. Only the bed drawing uses it now.</summary>
        private float _pxPerVolume;

        /// <summary>
        /// A grid of equal, legible cards. Columns GROW until the rows fit rather than the cards
        /// shrinking below the point where three lines of type fit, and nothing is ever pushed off
        /// the panel — a card the player cannot see is a crate they cannot choose, and this screen is
        /// nothing but choosing. Returns the column count used.
        /// </summary>
        private static int LayoutCards(ComicRect area, List<TruckCrate> list)
        {
            foreach (var c in list) c.Box = ComicRect.Zero;
            if (list.Count == 0 || area.IsEmpty) return 1;

            int columns = 1;
            int rows = list.Count;
            while (columns < 6)
            {
                rows = (list.Count + columns - 1) / columns;
                float need = rows * MinCardHeight + (rows - 1) * CardGap;
                // Two columns is the floor for a readable page; one tall column of four reads as a
                // menu. Past that, only overflow makes it add another.
                if (columns >= 2 && need <= area.H) break;
                columns++;
            }
            rows = Math.Max(1, (list.Count + columns - 1) / columns);

            float cardW = (area.W - (columns - 1) * CardGap) / columns;
            float cardH = Math.Min(MaxCardHeight, (area.H - (rows - 1) * CardGap) / rows);
            if (cardH < 18f) cardH = 18f;

            // Two cards pinned to the top of a tall panel look like a mistake; the same two in the
            // middle of it look like a photograph of a driveway with two things standing on it.
            // Slightly above centre, because the eye reads a page from the top.
            float block = rows * cardH + (rows - 1) * CardGap;
            float top = area.Y + Math.Max(0f, (area.H - block) * 0.38f);

            for (int i = 0; i < list.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                list[i].Box = new ComicRect(area.X + col * (cardW + CardGap),
                                            top + row * (cardH + CardGap),
                                            cardW, cardH);
            }
            return columns;
        }

        // ------------------------------------------------------------------ cursor

        public TruckZone Zone { get; private set; }

        /// <summary>Index within the current zone's list.</summary>
        public int Row { get; private set; }

        public TruckCrate? Selected
        {
            get
            {
                var list = Zone == TruckZone.Bed ? _bed : _left;
                return Row >= 0 && Row < list.Count ? list[Row] : null;
            }
        }

        /// <summary>Left/right walks the grid in reading order, wrapping.</summary>
        public void MoveLeft() => StepRow(-1);

        public void MoveRight() => StepRow(+1);

        private void StepRow(int delta)
        {
            var list = Zone == TruckZone.Bed ? _bed : _left;
            if (list.Count == 0) return;
            Row = (Row + delta + list.Count) % list.Count;
        }

        /// <summary>
        /// Up/down moves a whole row inside the grid, and crosses to the other panel when there is
        /// no row that way. One control does both because with two panels and two grids there is
        /// nowhere else for the stick to go, and a cursor that stops dead at the top of a column is
        /// how the last panel lost its way out.
        /// </summary>
        public void MoveUp() => StepColumn(-1);

        public void MoveDown() => StepColumn(+1);

        private void StepColumn(int delta)
        {
            var list = Zone == TruckZone.Bed ? _bed : _left;
            int columns = Math.Max(1, Zone == TruckZone.Bed ? BedColumns : GravelColumns);
            if (list.Count > 0)
            {
                int target = Row + delta * columns;
                if (target >= 0 && target < list.Count)
                {
                    Row = target;
                    return;
                }
            }
            SwapZone();
        }

        private void SwapZone()
        {
            var other = Zone == TruckZone.Bed ? TruckZone.LeftBehind : TruckZone.Bed;
            var list = other == TruckZone.Bed ? _bed : _left;
            if (list.Count == 0) return; // an empty bed must not swallow the cursor
            Zone = other;
            Row = Math.Min(Row, list.Count - 1);
        }

        // ------------------------------------------------------------------ actions

        /// <summary>
        /// A: put the highlighted crate in the bed, or take it out again. Refusing to fit reports
        /// Blocked rather than None so the refusal can be heard as well as seen.
        /// </summary>
        public TruckAction Confirm()
        {
            var sel = Selected;
            if (sel == null) return TruckAction.None;

            if (sel.InBed)
            {
                sel.InBed = false;
                Repartition();
                ClampCursor();
                return TruckAction.Unload;
            }

            if (!Fits(sel)) return TruckAction.Blocked;

            sel.InBed = true;
            Repartition();
            ClampCursor();
            return TruckAction.Load;
        }

        public TruckAction Back() => TruckAction.Close;

        /// <summary>
        /// Put a named crate in the bed, cursor or no cursor. The integrator's auto-load walks the
        /// gravel in value-density order, which is not cursor order.
        /// Returns false when it does not fit or is already aboard.
        /// </summary>
        public bool Load(TruckCrate crate)
        {
            if (crate == null || crate.InBed || !Fits(crate)) return false;
            crate.InBed = true;
            Repartition();
            ClampCursor();
            return true;
        }

        /// <summary>Take a named crate back out. Returns false when it was not aboard.</summary>
        public bool Unload(TruckCrate crate)
        {
            if (crate == null || !crate.InBed) return false;
            crate.InBed = false;
            Repartition();
            ClampCursor();
            return true;
        }

        /// <summary>A crate that moved zones leaves a hole; land on its neighbour, never out of range.</summary>
        private void ClampCursor()
        {
            var list = Zone == TruckZone.Bed ? _bed : _left;
            if (list.Count == 0)
            {
                var other = Zone == TruckZone.Bed ? TruckZone.LeftBehind : TruckZone.Bed;
                var otherList = other == TruckZone.Bed ? _bed : _left;
                Zone = other;
                Row = otherList.Count == 0 ? 0 : Math.Min(Row, otherList.Count - 1);
                return;
            }
            Row = Math.Min(Row, list.Count - 1);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
