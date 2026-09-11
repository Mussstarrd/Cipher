#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// One thing that could go in the bed. Generic (name + two numbers) so this page never has to
    /// know what a turret is; the integrator maps TruckLoad's Haulage onto it.
    /// </summary>
    public readonly struct TruckItemView
    {
        public readonly string Name;

        /// <summary>Kilograms. Drives how dark the crate prints, not how big it is.</summary>
        public readonly float Weight;

        /// <summary>Cubic metres. Drives how much of the bed the crate eats, which is the picture.</summary>
        public readonly float Volume;

        public TruckItemView(string name, float weight, float volume)
        {
            Name = name ?? string.Empty;
            Weight = Math.Max(0f, weight);
            Volume = Math.Max(0f, volume);
        }

        public override string ToString() => $"{Name} {Weight:F0}kg {Volume:F1}m3";
    }

    public enum TruckZone { Bed, LeftBehind }

    public enum TruckAction { None, Load, Unload, Blocked, Close }

    /// <summary>A crate as drawn: where it sits and how heavily it prints.</summary>
    public sealed class TruckCrate
    {
        public int Index;
        public TruckItemView Item;
        public ComicRect Box;
        public bool InBed;

        /// <summary>
        /// 0..1 halftone density, from weight per cubic metre. Same footprint, darker print = the
        /// thing that will cost you axle rather than floor. One visual variable per quantity: width
        /// is volume, darkness is weight, and nothing else moves.
        /// </summary>
        public float ShadeDensity;
    }

    /// <summary>
    /// THE TRUCK PAGE: a side elevation of the bed you are filling, and — at the same size, on the
    /// same page — everything you are about to abandon.
    ///
    /// ROADMAP §5.3: "a side elevation of the bed you slot boxes into, two ink gauges for weight and
    /// volume, and what gets left behind drawn with equal weight — the decision is about loss, not
    /// capacity." The owner played the old version and said "I don't really understand the pack up
    /// mechanism either because my two towers can come and the truck is only like 4% full"
    /// (2026-09-11). A percentage cannot be argued with; a picture of a half-empty bed next to three
    /// crates on the gravel can.
    ///
    /// The layout OWNS the loaded flags. It is a selection screen, so it holds the choice and the
    /// integrator mirrors the result into TruckLoad on close — rather than round-tripping every
    /// cursor move through the match rules.
    /// </summary>
    public sealed class TruckPageLayout
    {
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

        private void Repartition()
        {
            _bed.Clear();
            _left.Clear();
            Weight = 0f;
            Volume = 0f;
            foreach (var c in _all)
            {
                if (c.InBed)
                {
                    _bed.Add(c);
                    Weight += c.Item.Weight;
                    Volume += c.Item.Volume;
                }
                else
                {
                    _left.Add(c);
                }
            }
        }

        public bool Fits(TruckCrate crate) =>
            Weight + crate.Item.Weight <= MaxWeight + 0.0001f &&
            Volume + crate.Item.Volume <= MaxVolume + 0.0001f;

        // ------------------------------------------------------------------ rects

        public ComicRect PageRect { get; private set; }
        public ComicRect CaptionRect { get; private set; }

        public ComicRect TruckPanel { get; private set; }

        /// <summary>
        /// The bonnet, ahead of the cab. A cab alone reads as a box; a cab with a lower nose in
        /// front of it reads as a vehicle, and it is the cheapest mark on this page.
        /// </summary>
        public ComicRect HoodRect { get; private set; }

        /// <summary>The cab, drawn as a blunt ink shape ahead of the bed.</summary>
        public ComicRect CabRect { get; private set; }

        /// <summary>The bed walls. Crates go inside BedInner.</summary>
        public ComicRect BedRect { get; private set; }

        public ComicRect BedInner { get; private set; }

        public ComicRect WheelFront { get; private set; }
        public ComicRect WheelRear { get; private set; }

        /// <summary>Y of the ground the truck stands on. The left-behind crates sit on the same line.</summary>
        public float GroundY { get; private set; }

        public ComicRect LeftBehindPanel { get; private set; }

        public ComicRect WeightGauge { get; private set; }
        public ComicRect VolumeGauge { get; private set; }

        /// <summary>
        /// A one-line band under the two panels for whatever the cursor is on.
        ///
        /// Crate width is volume, which is the honest picture and also means a half-cubic-metre
        /// rotor is twenty pixels wide and its name is a single letter. The page needs somewhere to
        /// say what you are actually standing on, and one caption is cheaper than shrinking every
        /// crate to fit its label.
        /// </summary>
        public ComicRect DetailRect { get; private set; }

        public void Layout(ComicRect page)
        {
            PageRect = page;
            var inner = page.Inset(18f);

            CaptionRect = new ComicRect(inner.X, inner.Y, inner.W, 34f);

            // The gauges get a full-width strip along the foot. That is what lets the two panels
            // above be the same size: "equal visual weight" is the brief, so neither the truck nor
            // the loss gets to be the big one.
            const float gaugeH = 56f;
            const float detailH = 32f;
            var gaugeStrip = new ComicRect(inner.X + 10f, inner.Bottom - gaugeH, inner.W - 20f, gaugeH);

            DetailRect = new ComicRect(inner.X, gaugeStrip.Y - 14f - detailH, inner.W, detailH);

            var body = new ComicRect(inner.X, inner.Y + 44f, inner.W, DetailRect.Y - 12f - (inner.Y + 44f));

            const float gutter = 14f;
            float halfW = (body.W - gutter) * 0.5f;
            TruckPanel = new ComicRect(body.X, body.Y, halfW, body.H);
            LeftBehindPanel = new ComicRect(body.X + halfW + gutter, body.Y, halfW, body.H);

            float gg = 26f;
            float gw = (gaugeStrip.W - gg) * 0.5f;
            WeightGauge = new ComicRect(gaugeStrip.X, gaugeStrip.Y, gw, gaugeStrip.H);
            VolumeGauge = new ComicRect(gaugeStrip.X + gw + gg, gaugeStrip.Y, gw, gaugeStrip.H);

            LayoutTruck();
            LayoutLeftBehind();
        }

        /// <summary>Pixels per cubic metre. Shared by both panels so crates are directly comparable.</summary>
        private float _pxPerVolume;

        private void LayoutTruck()
        {
            var inner = TruckPanel.Inset(14f, 34f, 14f, 14f);

            // THE HEIGHT COMES FROM THE WIDTH, not from the panel.
            //
            // A four-door pickup in side elevation is about three times as long as it is tall. The
            // panel here is nearly square, so a vehicle stretched to fill it stops being a vehicle:
            // the first version had a cab three hundred pixels tall with its window at the roofline
            // and photographed as two boxes and two rings rather than a truck.
            //
            // Three volumes, not two: nose, cab, bed. The nose is what tells you which way it is
            // pointing, and which way it is pointing is what makes it a truck about to leave.
            float vehicleH = Math.Min(inner.H * 0.62f, inner.W * 0.36f);
            float wheelD = vehicleH * 0.34f;

            // The ground sits at four fifths down the panel rather than on its floor, so the vehicle
            // has air over it. A drawing pinned to the bottom edge of its frame reads as a diagram.
            GroundY = inner.Y + inner.H * 0.78f;
            float bodyBottom = GroundY - wheelD * 0.45f;
            float bodyTop = bodyBottom - vehicleH;

            float hoodW = inner.W * 0.17f;
            float cabW = inner.W * 0.31f;
            // The bonnet line sits nearly two thirds up the body. Lower than that and the cab towers
            // over the nose, which is the silhouette of a box van; this is a pickup.
            float hoodH = vehicleH * 0.63f;

            HoodRect = new ComicRect(inner.X, bodyBottom - hoodH, hoodW, hoodH);
            CabRect = new ComicRect(inner.X + hoodW, bodyTop, cabW, vehicleH);

            // Bed walls under half the cab's height: that silhouette is what says pickup and not van.
            float bedH = vehicleH * 0.56f;
            BedRect = new ComicRect(CabRect.Right, bodyBottom - bedH, inner.Right - CabRect.Right, bedH);
            BedInner = BedRect.Inset(6f, 6f, 6f, 4f);

            // Both wheels overlap the body by half their diameter, so the rings read as arches cut
            // into it rather than as two circles parked underneath.
            WheelFront = new ComicRect(inner.X + hoodW * 0.62f, GroundY - wheelD, wheelD, wheelD);
            WheelRear = new ComicRect(inner.Right - wheelD * 1.9f, GroundY - wheelD, wheelD, wheelD);

            _pxPerVolume = MaxVolume > 0f ? BedInner.W / MaxVolume : 0f;

            // Crates fill the bed's depth. Their WIDTH is the variable, because width is volume and
            // one visual variable per quantity is the rule this page is built on.
            float x = BedInner.X;
            foreach (var c in _bed)
            {
                float w = c.Item.Volume * _pxPerVolume;
                c.Box = new ComicRect(x, BedInner.Y, w, BedInner.H);
                x += w;
            }
        }

        private void LayoutLeftBehind()
        {
            var inner = LeftBehindPanel.Inset(14f, 34f, 14f, 14f);

            // EXACTLY the same scale and the same crate height as the bed, wrapped into rows on the
            // gravel. Standing a crate here beside the bed it did not get into is the whole argument
            // of this screen, and it only works if the two are directly comparable.
            float crateH = Math.Max(24f, BedInner.H);
            float rowH = crateH + 26f;

            // Count the rows first so the block can be centred. A single row of crates pinned to the
            // top of a tall panel looks like a mistake; the same row in the middle of it looks like a
            // photograph of a driveway with three things standing on it.
            int rows = 1;
            float probe = inner.X;
            foreach (var c in _left)
            {
                float w = Math.Max(16f, Math.Min(inner.W, c.Item.Volume * _pxPerVolume));
                if (probe + w > inner.Right + 0.001f && probe > inner.X)
                {
                    rows++;
                    probe = inner.X;
                }
                probe += w + 8f;
            }

            float block = rows * rowH - 26f;
            float x = inner.X;
            float y = inner.Y + Math.Max(0f, (inner.H - block) * 0.42f);

            foreach (var c in _left)
            {
                float w = Math.Max(16f, Math.Min(inner.W, c.Item.Volume * _pxPerVolume));
                if (x + w > inner.Right + 0.001f && x > inner.X)
                {
                    x = inner.X;
                    y += rowH;
                }
                if (y + crateH > inner.Bottom + 0.001f)
                {
                    // Out of gravel. Park it off-page rather than drawing crates over the caption.
                    c.Box = ComicRect.Zero;
                    continue;
                }
                c.Box = new ComicRect(x, y, w, crateH);
                x += w + 8f;
            }
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

        /// <summary>Left/right walk along the row of crates, wrapping.</summary>
        public void MoveLeft() => StepRow(-1);

        public void MoveRight() => StepRow(+1);

        private void StepRow(int delta)
        {
            var list = Zone == TruckZone.Bed ? _bed : _left;
            if (list.Count == 0) return;
            Row = (Row + delta + list.Count) % list.Count;
        }

        /// <summary>Up/down cross between the bed and the gravel — the two zones, stacked as drawn.</summary>
        public void MoveUp() => SwapZone();

        public void MoveDown() => SwapZone();

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
        /// gravel in value-density order, which is not cursor order, and driving it by moving the
        /// cursor would leave the player standing somewhere they never navigated to.
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
