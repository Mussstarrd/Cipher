#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game.UI.Comic
{
    /// <summary>One item, as far as the page is concerned. Strings and a number, nothing else.</summary>
    /// <remarks>
    /// Deliberately NOT the Progression assembly's Item. The page must not know what an affix is, and
    /// Progression must not grow a dependency on the UI. The integrator maps Item -> KitItem at the
    /// call site, which is also where the "one number" of §5.1 gets chosen.
    /// </remarks>
    public readonly struct KitItem
    {
        public readonly string Name;
        public readonly string Slot;
        public readonly float Power;

        public KitItem(string name, string slot, float power)
        {
            Name = name ?? string.Empty;
            Slot = slot ?? string.Empty;
            Power = power;
        }

        public override string ToString() => $"{Name} ({Slot}) {Power:F1}";
    }

    /// <summary>
    /// Where the cursor lives. Three zones laid left to right exactly as they are drawn, so a push
    /// right on the stick moves right on the page. Left/right cycles zones, up/down scrolls inside
    /// one — which is the owner's own prescription: "up on the d-pad opens this skills list and then
    /// my left analog control scrolling through it" (2026-09-11).
    /// </summary>
    public enum KitZone { SlotsLeft, SlotsRight, Pack }

    public enum KitAction { None, Equip, Unequip, Close }

    /// <summary>One of the eight boxes hanging off the paper doll on a leader line.</summary>
    public sealed class KitSlotBox
    {
        public int Index;
        public string SlotName = string.Empty;
        public ComicRect Box;

        /// <summary>Where the line leaves the box: the edge facing the figure.</summary>
        public ComicPoint LeaderFrom;

        /// <summary>Where the line lands on the body.</summary>
        public ComicPoint LeaderTo;

        public bool OnLeft;
        public bool HasItem;
        public KitItem Item;
    }

    /// <summary>
    /// THE KIT PAGE: an ink paper doll, not an inventory list.
    ///
    /// The rule from ROADMAP-2026-09-12 §5.3 is absolute — "every screen shows a picture of the
    /// thing, never a list of it" — and this screen is the one the owner was looking at when he said
    /// "it just looks like computer gibberish coming at me and don't really understand what's going
    /// on". A list of eight `Slot  Item` rows is a spreadsheet. A body with eight things pinned to it
    /// is a picture you can read before you can read.
    ///
    /// Pure layout and cursor maths, no UnityEngine — ComicPageDemo and the integrator do the
    /// drawing with ComicInk primitives.
    /// </summary>
    public sealed class KitPageLayout
    {
        private readonly string[] _slotNames;
        private readonly KitItem?[] _equippedBySlot;
        private readonly List<KitItem> _pack;
        private readonly KitSlotBox[] _slots;
        private readonly List<ComicRect> _packCards = new List<ComicRect>();

        private readonly int _leftCount;

        public KitPageLayout(IReadOnlyList<string> slotNames,
                             IReadOnlyList<KitItem> equipped,
                             IReadOnlyList<KitItem> pack)
        {
            if (slotNames == null || slotNames.Count == 0)
                throw new ArgumentException("a paper doll needs at least one slot", nameof(slotNames));

            _slotNames = new string[slotNames.Count];
            for (int i = 0; i < slotNames.Count; i++) _slotNames[i] = slotNames[i] ?? string.Empty;

            // The left column takes the extra box on an odd count, so the page stays left-heavy the
            // way a comic page is.
            _leftCount = (_slotNames.Length + 1) / 2;

            _equippedBySlot = new KitItem?[_slotNames.Length];
            if (equipped != null)
            {
                foreach (var item in equipped)
                {
                    int idx = IndexOfSlot(item.Slot);
                    if (idx >= 0) _equippedBySlot[idx] = item;
                }
            }

            _pack = pack == null ? new List<KitItem>() : new List<KitItem>(pack);

            _slots = new KitSlotBox[_slotNames.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new KitSlotBox
                {
                    Index = i,
                    SlotName = _slotNames[i],
                    OnLeft = i < _leftCount,
                    HasItem = _equippedBySlot[i].HasValue,
                    Item = _equippedBySlot[i] ?? default,
                };
            }

            Zone = KitZone.SlotsLeft;
            Recompare();
        }

        // ------------------------------------------------------------------ contents

        public IReadOnlyList<KitSlotBox> Slots => _slots;
        public IReadOnlyList<KitItem> Pack => _pack;
        public int SlotCount => _slots.Length;

        private int IndexOfSlot(string slot)
        {
            for (int i = 0; i < _slotNames.Length; i++)
                if (string.Equals(_slotNames[i], slot, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        // ------------------------------------------------------------------ rects

        public ComicRect PageRect { get; private set; }
        public ComicRect CaptionRect { get; private set; }

        /// <summary>The big left panel. A comic page's establishing shot; it gets the most area.</summary>
        public ComicRect DollPanel { get; private set; }

        /// <summary>Where the ink figure itself is drawn — strictly between the two slot columns.</summary>
        public ComicRect FigureRect { get; private set; }

        public ComicRect ComparePanel { get; private set; }
        public ComicRect EquippedCard { get; private set; }
        public ComicRect CandidateCard { get; private set; }

        /// <summary>The large +/- number. This is the whole point of the comparison panel.</summary>
        public ComicRect DeltaRect { get; private set; }

        public ComicRect PackPanel { get; private set; }
        public IReadOnlyList<ComicRect> PackCards => _packCards;

        /// <summary>How many pack cards sit across the strip. Two reads as cards, more reads as a list.</summary>
        public const int PackColumns = 2;

        public void Layout(ComicRect page)
        {
            PageRect = page;
            var inner = page.Inset(18f);

            // Unequal panels, like a real page: the doll is the splash, the right column is two
            // stacked beats. Nothing here is a golden ratio; it is just never 50/50.
            CaptionRect = new ComicRect(inner.X, inner.Y, inner.W, 34f);
            var body = inner.Inset(0f, 44f, 0f, 0f);

            const float gutter = 14f;
            float dollW = body.W * 0.56f;
            DollPanel = new ComicRect(body.X, body.Y, dollW, body.H);

            float rightX = body.X + dollW + gutter;
            float rightW = body.Right - rightX;
            float compareH = body.H * 0.54f;
            ComparePanel = new ComicRect(rightX, body.Y, rightW, compareH);
            PackPanel = new ComicRect(rightX, body.Y + compareH + gutter, rightW, body.H - compareH - gutter);

            LayoutDoll();
            LayoutCompare();
            LayoutPack();
        }

        private void LayoutDoll()
        {
            var inner = DollPanel.Inset(14f, 34f, 14f, 14f); // 34 at the top leaves room for the panel title

            // Fixed fractions rather than clamped pixel widths: this is what guarantees the figure
            // always has room between the columns, so a box can never land on top of the body no
            // matter how narrow the page gets. 0.26 + 0.26 + 0.04 + 0.04 leaves the figure 0.40.
            float boxW = inner.W * 0.26f;
            float gap = inner.W * 0.04f;

            int rows = (_slotNames.Length + 1) / 2;
            float rowGap = 10f;
            float boxH = Math.Min(74f, (inner.H - (rows + 1) * rowGap) / Math.Max(1, rows));
            float colH = rows * boxH + (rows - 1) * rowGap;
            float colY = inner.Y + Math.Max(0f, (inner.H - colH) * 0.5f);

            float leftX = inner.X;
            float rightX = inner.Right - boxW;
            FigureRect = new ComicRect(leftX + boxW + gap, inner.Y,
                                       inner.W - 2f * boxW - 2f * gap, inner.H);

            for (int i = 0; i < _slots.Length; i++)
            {
                bool onLeft = i < _leftCount;
                int row = onLeft ? i : i - _leftCount;
                int rowsInColumn = onLeft ? _leftCount : _slots.Length - _leftCount;

                float y = colY + row * (boxH + rowGap);
                var box = new ComicRect(onLeft ? leftX : rightX, y, boxW, boxH);

                var anchor = Anchor(onLeft, row, rowsInColumn);
                _slots[i].OnLeft = onLeft;
                _slots[i].Box = box;
                _slots[i].LeaderFrom = new ComicPoint(onLeft ? box.Right : box.X, box.CenterY);
                _slots[i].LeaderTo = new ComicPoint(FigureRect.X + FigureRect.W * anchor.X,
                                                   FigureRect.Y + FigureRect.H * anchor.Y);
            }
        }

        /// <summary>
        /// Anchors are assigned by POSITION IN THE COLUMN, top to bottom — never by slot name. Two
        /// consequences that are both wanted: leader lines can never cross (row 0 gets the highest
        /// point on the body, the last row the lowest), and the layout stays ignorant of the
        /// Progression Slot enum, so reordering the slots is a call-site decision.
        /// </summary>
        private static ComicPoint Anchor(bool onLeft, int row, int rowsInColumn)
        {
            // Hand-placed for the canonical four-a-side doll: head, chest, hand, foot down the left;
            // neck, hand, waist, foot down the right.
            if (rowsInColumn == 4)
            {
                if (onLeft)
                {
                    switch (row)
                    {
                        case 0: return new ComicPoint(0.42f, 0.075f); // head
                        case 1: return new ComicPoint(0.36f, 0.28f);  // chest
                        case 2: return new ComicPoint(0.14f, 0.52f);  // left hand
                        default: return new ComicPoint(0.40f, 0.95f); // left foot
                    }
                }
                switch (row)
                {
                    case 0: return new ComicPoint(0.52f, 0.15f);  // collar
                    case 1: return new ComicPoint(0.86f, 0.52f);  // right hand
                    case 2: return new ComicPoint(0.58f, 0.56f);  // waist
                    default: return new ComicPoint(0.60f, 0.95f); // right foot
                }
            }

            // Any other count: walk evenly down the body's near side. Still monotonic in y, so the
            // no-crossing property survives.
            float t = rowsInColumn <= 1 ? 0.5f : (row + 0.5f) / rowsInColumn;
            return new ComicPoint(onLeft ? 0.34f : 0.66f, 0.07f + t * 0.84f);
        }

        private void LayoutCompare()
        {
            var inner = ComparePanel.Inset(14f, 34f, 14f, 14f);

            // Cards on top, the number underneath and huge. The number is the message; the two cards
            // are only there so the player can see what the number is comparing.
            float cardsH = inner.H * 0.58f;
            float gutter = 12f;
            float cardW = (inner.W - gutter) * 0.5f;
            EquippedCard = new ComicRect(inner.X, inner.Y, cardW, cardsH);
            CandidateCard = new ComicRect(inner.X + cardW + gutter, inner.Y, cardW, cardsH);
            DeltaRect = new ComicRect(inner.X, inner.Y + cardsH + gutter, inner.W, inner.H - cardsH - gutter);
        }

        private void LayoutPack()
        {
            _packCards.Clear();
            if (_pack.Count == 0) return;

            var inner = PackPanel.Inset(14f, 34f, 14f, 14f);
            const float gutter = 10f;
            float cardW = (inner.W - gutter * (PackColumns - 1)) / PackColumns;

            int rows = (_pack.Count + PackColumns - 1) / PackColumns;
            float cardH = Math.Min(62f, (inner.H - gutter * Math.Max(0, rows - 1)) / Math.Max(1, rows));

            for (int i = 0; i < _pack.Count; i++)
            {
                int col = i % PackColumns;
                int row = i / PackColumns;
                _packCards.Add(new ComicRect(inner.X + col * (cardW + gutter),
                                             inner.Y + row * (cardH + gutter),
                                             cardW, cardH));
            }
        }

        // ------------------------------------------------------------------ cursor

        public KitZone Zone { get; private set; }

        /// <summary>Index within the current zone, not a global index.</summary>
        public int Row { get; private set; }

        public int SelectedSlotIndex =>
            Zone == KitZone.SlotsLeft ? Row
            : Zone == KitZone.SlotsRight ? _leftCount + Row
            : -1;

        public int SelectedPackIndex => Zone == KitZone.Pack ? Row : -1;

        private int CountIn(KitZone zone) => zone switch
        {
            KitZone.SlotsLeft => _leftCount,
            KitZone.SlotsRight => _slots.Length - _leftCount,
            _ => _pack.Count,
        };

        public void MoveUp() => Step(-1);
        public void MoveDown() => Step(+1);

        private void Step(int delta)
        {
            int n = CountIn(Zone);
            if (n <= 0) return;
            Row = (Row + delta + n) % n; // wraps: a stick held down must never dead-end
            Recompare();
        }

        public void MoveLeft() => StepZone(-1);
        public void MoveRight() => StepZone(+1);

        /// <summary>
        /// Cycles the three zones and skips any that are empty (an empty pack must not swallow the
        /// cursor). The row is carried across and clamped, so moving sideways lands beside where you
        /// were rather than jumping to the top.
        /// </summary>
        private void StepZone(int delta)
        {
            int carried = Row;
            for (int hop = 1; hop <= 3; hop++)
            {
                var next = (KitZone)(((int)Zone + delta * hop + 3 * 3) % 3);
                if (next == Zone) continue;
                if (CountIn(next) <= 0) continue;
                Zone = next;
                Row = Math.Min(carried, CountIn(next) - 1);
                Recompare();
                return;
            }
        }

        // ------------------------------------------------------------------ comparison

        /// <summary>What the left-hand card shows: the item currently worn in the slot under scrutiny.</summary>
        public KitItem? ComparisonEquipped { get; private set; }

        /// <summary>What the right-hand card shows: the pack item being weighed against it.</summary>
        public KitItem? ComparisonCandidate { get; private set; }

        public bool HasComparison => ComparisonEquipped.HasValue || ComparisonCandidate.HasValue;

        public float Delta => (ComparisonCandidate?.Power ?? 0f) - (ComparisonEquipped?.Power ?? 0f);

        /// <summary>-1, 0 or +1. Drives the accent colour as well as the glyph.</summary>
        public int DeltaSign => Delta > 0.0001f ? 1 : Delta < -0.0001f ? -1 : 0;

        /// <summary>The big number, already signed. "+12.5", "-3.0", "0.0".</summary>
        public string DeltaText =>
            DeltaSign > 0 ? $"+{Delta:F1}"
            : DeltaSign < 0 ? $"{Delta:F1}"
            : "0.0";

        /// <summary>Which slot the comparison is about, for the panel's title. Empty when there is none.</summary>
        public string ComparisonSlot { get; private set; } = string.Empty;

        /// <summary>
        /// Recompute the two cards from the cursor. Every Move calls it, so the comparison panel can
        /// never be stale — the integrator has nothing to remember.
        /// </summary>
        private void Recompare()
        {
            ComparisonEquipped = null;
            ComparisonCandidate = null;
            ComparisonSlot = string.Empty;

            if (Zone == KitZone.Pack)
            {
                if (Row < 0 || Row >= _pack.Count) return;
                var cand = _pack[Row];
                ComparisonCandidate = cand;
                ComparisonSlot = cand.Slot;
                int idx = IndexOfSlot(cand.Slot);
                if (idx >= 0) ComparisonEquipped = _equippedBySlot[idx];
                return;
            }

            int slot = SelectedSlotIndex;
            if (slot < 0 || slot >= _slots.Length) return;
            ComparisonSlot = _slotNames[slot];
            ComparisonEquipped = _equippedBySlot[slot];
            ComparisonCandidate = BestCandidateFor(_slotNames[slot]);
        }

        /// <summary>
        /// Standing on a slot shows the best thing in the pack that could go there. Otherwise the
        /// player has to hunt the pack to discover that an upgrade exists, which is the "I don't
        /// understand how my inventory works" failure in a different costume.
        /// </summary>
        private KitItem? BestCandidateFor(string slot)
        {
            KitItem? best = null;
            foreach (var item in _pack)
            {
                if (!string.Equals(item.Slot, slot, StringComparison.OrdinalIgnoreCase)) continue;
                if (best == null || item.Power > best.Value.Power) best = item;
            }
            return best;
        }

        // ------------------------------------------------------------------ actions

        /// <summary>The item the next Confirm would act on. Null when Confirm would do nothing.</summary>
        public KitItem? ActionTarget { get; private set; }

        /// <summary>
        /// A: take the highlighted pack item, or strip the highlighted slot. The layout reports the
        /// intent; the integrator calls Loadout.EquipFromPack / SellFromPack, which as of
        /// ROADMAP §1 are "tested and called from nowhere".
        /// </summary>
        public KitAction Confirm()
        {
            if (Zone == KitZone.Pack)
            {
                if (Row < 0 || Row >= _pack.Count) return KitAction.None;
                ActionTarget = _pack[Row];
                return KitAction.Equip;
            }

            int slot = SelectedSlotIndex;
            if (slot < 0 || slot >= _slots.Length) return KitAction.None;
            var worn = _equippedBySlot[slot];
            if (!worn.HasValue) return KitAction.None;
            ActionTarget = worn;
            return KitAction.Unequip;
        }

        public KitAction Back() => KitAction.Close;

        /// <summary>Manual hook for a caller that changed the contents underneath the cursor.</summary>
        public void Refresh() => Recompare();

        /// <summary>
        /// Put the cursor back where it was. Equipping something rebuilds this object from the model
        /// — the pack is a different length afterwards — and dumping the player back at the top of
        /// the left column every time they took an item would make the screen feel like it reset.
        /// An empty target zone falls through to the first zone that has anything in it.
        /// </summary>
        public void SetCursor(KitZone zone, int row)
        {
            if (CountIn(zone) <= 0)
            {
                if (CountIn(KitZone.SlotsLeft) > 0) zone = KitZone.SlotsLeft;
                else if (CountIn(KitZone.SlotsRight) > 0) zone = KitZone.SlotsRight;
                else zone = KitZone.Pack;
            }

            Zone = zone;
            int n = CountIn(Zone);
            Row = n <= 0 ? 0 : Math.Min(Math.Max(0, row), n - 1);
            Recompare();
        }
    }
}
