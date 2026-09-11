#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// Three states, three drawing treatments, no legend required. Taken gets the amber rubber stamp
    /// (the implant colour, ADR-003 — the one accent this UI owns); Available is clean paper, which
    /// is the page inviting you to write on it; Locked is faint pencil, present but not yet inked.
    /// </summary>
    public enum SkillState { Taken, Available, Locked }

    public enum SkillsAction { None, Take, Blocked, Close }

    /// <summary>One dossier entry. Generic on purpose — no dependency on Progression.SkillNode.</summary>
    public readonly struct SkillNodeView
    {
        public readonly string Id;
        public readonly string Name;

        /// <summary>One line. If it needs two, the skill is too complicated for this page.</summary>
        public readonly string Text;

        public readonly SkillState State;
        public readonly int Cost;

        /// <summary>Which dossier column this belongs in — a discipline, not a tree depth.</summary>
        public readonly int Column;

        public SkillNodeView(string id, string name, string text, SkillState state, int cost, int column)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Text = text ?? string.Empty;
            State = state;
            Cost = cost;
            Column = Math.Max(0, column);
        }

        public override string ToString() => $"{Name} [{State}] {Cost}pt";
    }

    /// <summary>A laid-out node: the view plus where it goes and whether it is currently on screen.</summary>
    public sealed class SkillBox
    {
        public SkillNodeView Node;
        public ComicRect Box;
        public int Column;
        public int RowInColumn;

        /// <summary>False when it has scrolled out of its column; the drawing code skips it.</summary>
        public bool Visible;
    }

    /// <summary>
    /// THE SKILLS PAGE: a dossier, not a node graph.
    ///
    /// ROADMAP §5.3 calls for "columns of stamped boxes, taken ones stamped amber, locked ones in
    /// faint pencil" — deliberately not a web of lines, because the owner's complaint was that he
    /// could not read the screen, and a graph asks you to trace edges before you can read anything.
    /// Prerequisites are expressed by ORDER DOWN THE COLUMN plus the Locked treatment: the next
    /// pencil box under the last stamped one is the next thing you can buy.
    ///
    /// Two owner requirements are encoded here as hard rules (2026-09-11):
    ///   - "once I'm in my skills if I press up again it exits out of my skills so that's
    ///     counterproductive" — MoveUp NEVER closes this page. Only Back() does.
    ///   - "there's no way for me to scroll the skills list" — every column scrolls independently
    ///     and always keeps the cursor visible.
    /// </summary>
    public sealed class SkillsPageLayout
    {
        private readonly List<List<SkillBox>> _columns = new List<List<SkillBox>>();
        private readonly List<ComicRect> _columnPanels = new List<ComicRect>();
        private readonly int[] _scroll;

        private int _visibleRows = 1;

        public SkillsPageLayout(IReadOnlyList<SkillNodeView> nodes)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            int columnCount = 1;
            foreach (var n in nodes) columnCount = Math.Max(columnCount, n.Column + 1);
            for (int c = 0; c < columnCount; c++) _columns.Add(new List<SkillBox>());

            foreach (var n in nodes)
            {
                var col = _columns[n.Column];
                col.Add(new SkillBox { Node = n, Column = n.Column, RowInColumn = col.Count });
            }

            _scroll = new int[columnCount];

            // Land on the first column that has anything in it, so an empty discipline never
            // presents an empty page on open.
            Column = 0;
            for (int c = 0; c < columnCount; c++)
            {
                if (_columns[c].Count > 0) { Column = c; break; }
            }
        }

        // ------------------------------------------------------------------ contents

        public int ColumnCount => _columns.Count;
        public IReadOnlyList<SkillBox> ColumnNodes(int column) => _columns[Clamp(column, 0, _columns.Count - 1)];
        public IReadOnlyList<ComicRect> ColumnPanels => _columnPanels;

        /// <summary>Column headers are captions, so the drawing code needs them separately.</summary>
        public string[] ColumnTitles { get; set; } = Array.Empty<string>();

        public string TitleOf(int column) =>
            column >= 0 && column < ColumnTitles.Length ? ColumnTitles[column] : $"FILE {column + 1}";

        // ------------------------------------------------------------------ rects

        public ComicRect PageRect { get; private set; }
        public ComicRect CaptionRect { get; private set; }

        /// <summary>The bold ink caption across the foot: the selected node's one line, big.</summary>
        public ComicRect DetailRect { get; private set; }

        /// <summary>Where the points-remaining stamp goes. Top right, like a date stamp on a file.</summary>
        public ComicRect PointsStampRect { get; private set; }

        /// <summary>How many boxes fit in a column at the last Layout. Drives the scroll window.</summary>
        public int VisibleRows => _visibleRows;

        public void Layout(ComicRect page)
        {
            PageRect = page;
            var inner = page.Inset(18f);

            CaptionRect = new ComicRect(inner.X, inner.Y, inner.W * 0.62f, 34f);
            PointsStampRect = new ComicRect(inner.Right - inner.W * 0.22f, inner.Y - 4f, inner.W * 0.22f, 44f);

            // The detail caption is a fixed band at the foot. Fixed, not proportional: it holds one
            // sentence and one sentence is one height.
            const float detailH = 68f;
            DetailRect = new ComicRect(inner.X, inner.Bottom - detailH, inner.W, detailH);

            var strip = new ComicRect(inner.X, inner.Y + 44f, inner.W, inner.Bottom - detailH - 14f - (inner.Y + 44f));

            _columnPanels.Clear();
            const float gutter = 12f;
            int n = Math.Max(1, _columns.Count);
            float colW = (strip.W - gutter * (n - 1)) / n;
            for (int c = 0; c < n; c++)
                _columnPanels.Add(new ComicRect(strip.X + c * (colW + gutter), strip.Y, colW, strip.H));

            // Box height is fixed so the boxes are the same size in every column — a dossier is a
            // stack of identical forms. Whatever does not fit scrolls.
            const float boxH = 62f;
            const float boxGap = 8f;
            float usable = Math.Max(boxH, colW > 0f ? strip.H - 34f - 10f : boxH); // 34 = column header
            _visibleRows = Math.Max(1, (int)((usable + boxGap) / (boxH + boxGap)));

            for (int c = 0; c < _columns.Count; c++)
            {
                var panel = _columnPanels[c];
                var body = panel.Inset(10f, 34f, 10f, 10f);
                ClampScroll(c);

                var col = _columns[c];
                for (int r = 0; r < col.Count; r++)
                {
                    int visRow = r - _scroll[c];
                    bool visible = visRow >= 0 && visRow < _visibleRows;
                    col[r].Visible = visible;
                    col[r].Box = visible
                        ? new ComicRect(body.X, body.Y + visRow * (boxH + boxGap), body.W, boxH)
                        : ComicRect.Zero;
                }
            }
        }

        // ------------------------------------------------------------------ cursor

        public int Column { get; private set; }
        public int Row { get; private set; }

        public int ScrollOf(int column) => _scroll[Clamp(column, 0, _scroll.Length - 1)];

        public SkillBox? Selected
        {
            get
            {
                var col = _columns[Column];
                return Row >= 0 && Row < col.Count ? col[Row] : null;
            }
        }

        /// <summary>
        /// Up and down scroll the current column and WRAP. Up is never an exit — see the class
        /// comment; the owner lost his skills page to exactly that binding.
        /// </summary>
        public void MoveUp() => StepRow(-1);

        public void MoveDown() => StepRow(+1);

        private void StepRow(int delta)
        {
            int n = _columns[Column].Count;
            if (n <= 0) return;
            Row = (Row + delta + n) % n;
            ScrollToCursor();
        }

        /// <summary>Left/right change discipline, wrapping, skipping empty columns.</summary>
        public void MoveLeft() => StepColumn(-1);

        public void MoveRight() => StepColumn(+1);

        private void StepColumn(int delta)
        {
            int n = _columns.Count;
            if (n <= 1) return;
            for (int hop = 1; hop <= n; hop++)
            {
                int next = ((Column + delta * hop) % n + n) % n;
                if (next == Column) continue;
                if (_columns[next].Count == 0) continue;
                Column = next;
                Row = Math.Min(Row, _columns[next].Count - 1); // land beside where you were, not at the top
                ScrollToCursor();
                return;
            }
        }

        /// <summary>
        /// Keep the cursor inside the visible window with no smooth scrolling and no lag: the box the
        /// stick is on is always drawn, or the player is navigating something they cannot see.
        /// </summary>
        private void ScrollToCursor()
        {
            int c = Column;
            if (Row < _scroll[c]) _scroll[c] = Row;
            else if (Row > _scroll[c] + _visibleRows - 1) _scroll[c] = Row - _visibleRows + 1;
            ClampScroll(c);
        }

        private void ClampScroll(int c)
        {
            int maxScroll = Math.Max(0, _columns[c].Count - _visibleRows);
            _scroll[c] = Clamp(_scroll[c], 0, maxScroll);
        }

        // ------------------------------------------------------------------ actions

        /// <summary>The node id the last Confirm referred to. Empty when there was nothing under the cursor.</summary>
        public string ActionNodeId { get; private set; } = string.Empty;

        /// <summary>
        /// A: buy the highlighted node. Locked and Taken both report Blocked rather than None so the
        /// integrator can play the refusal sound — silence on a button press reads as a broken game.
        /// </summary>
        public SkillsAction Confirm()
        {
            var sel = Selected;
            if (sel == null) { ActionNodeId = string.Empty; return SkillsAction.None; }
            ActionNodeId = sel.Node.Id;
            return sel.Node.State == SkillState.Available ? SkillsAction.Take : SkillsAction.Blocked;
        }

        public SkillsAction Back() => SkillsAction.Close;

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
