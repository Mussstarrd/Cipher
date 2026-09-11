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

        public void Layout(ComicRect page)
        {
            PageRect = page;
            var inner = page.Inset(18f);

            CaptionRect = new ComicRect(inner.X, inner.Y, inner.W, 34f);

            // The gauges get a full-width strip along the foot. That is what lets the two panels
            // above be the same size: "equal visual weight" is the brief, so neither the truck nor
            // the loss gets to be the big one.
            const float gaugeH = 64f;
            var gaugeStrip = new ComicRect(inner.X, inner.Bottom - gaugeH, inner.W, gaugeH);

            var body = new ComicRect(inner.X, inner.Y + 44f, inner.W, gaugeStrip.Y - 12f - (inner.Y + 44f));

            const float gutter = 14f;
            float halfW = (body.W - gutter) * 0.5f;
            TruckPanel = new ComicRect(body.X, body.Y, halfW, body.H);
            LeftBehindPanel = new ComicRect(body.X + halfW + gutter, body.Y, halfW, body.H);

            float gg = 14f;
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

            // Elevation proportions: cab a third, bed two thirds, wheels below the chassis line.
            float wheelD = Math.Min(46f, inner.H * 0.20f);
            GroundY = inner.Bottom - 2f;
            float chassisY = GroundY - wheelD * 0.55f;

            float bodyH = Math.Max(40f, chassisY - inner.Y);
            float cabW = inner.W * 0.26f;
            float cabH = bodyH * 0.72f;

            CabRect = new ComicRect(inner.X, chassisY - cabH, cabW, cabH);
            BedRect = new ComicRect(inner.X + cabW, chassisY - bodyH * 0.58f, inner.W - cabW, bodyH * 0.58f);
            BedInner = BedRect.Inset(6f, 6f, 6f, 4f);

            WheelFront = new ComicRect(inner.X + cabW * 0.35f, GroundY - wheelD, wheelD, wheelD);
            WheelRear = new ComicRect(inner.Right - wheelD * 1.6f, GroundY - wheelD, wheelD, wheelD);

            // A crate that fills the whole hold is exactly as wide as the bed. Everything else is a
            // true fraction of it, which is what makes "the truck is only 4% full" visible instead of
            // stated.
            _pxPerVolume = MaxVolume > 0f ? BedInner.W / MaxVolume : 0f;

            float x = BedInner.X;
            foreach (var c in _bed)
            {
                float w = c.Item.Volume * _pxPerVolume;
                float h = BedInner.H * 0.9f;
                c.Box = new ComicRect(x, BedInner.Bottom - h, w, h);
                x += w;
            }
        }

        private void LayoutLeftBehind()
        {
            var inner = LeftBehindPanel.Inset(14f, 34f, 14f, 14f);

            // Same scale as the bed, wrapped into rows on the gravel. Standing a crate here next to
            // the bed it did not get into is the entire argument of this screen.
            float rowH = Math.Min(58f, inner.H * 0.28f);
            float x = inner.X;
            float y = inner.Y;

            foreach (var c in _left)
            {
                float w = Math.Max(18f, Math.Min(inner.W, c.Item.Volume * _pxPerVolume));
                if (x + w > inner.Right + 0.001f && x > inner.X)
                {
                    x = inner.X;
                    y += rowH + 8f;
                }
                if (y + rowH > inner.Bottom + 0.001f)
                {
                    // Out of gravel. Park it off-page rather than drawing crates over the caption.
                    c.Box = ComicRect.Zero;
                    continue;
                }
                c.Box = new ComicRect(x, y, w, rowH);
                x += w + 6f;
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
