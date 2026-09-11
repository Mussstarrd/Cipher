#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Progression;

namespace Cipher.Game.UI.Comic
{
    public enum SkillsScreenAction { None, Closed, Bought, Refused }

    /// <summary>
    /// THE SKILLS SCREEN: the service-record dossier wired to the real <see cref="SkillState"/>.
    ///
    /// The old version was a flat list of fifteen rows of `[Path] Name rank cost text` and the owner
    /// called it what it was: "computer gibberish coming at me". It also could not be scrolled with
    /// a stick and pressing up — the button that opened it — closed it again.
    ///
    /// This fixes all three at once. Three columns of stamped forms, one per discipline; the left
    /// stick scrolls a column and wraps; and only B closes.
    /// </summary>
    public sealed class SkillsScreen
    {
        /// <summary>
        /// Column titles, in <see cref="Path"/> order. Named for what the points buy after ADR-008:
        /// the hero's weapon is a signal emitter, the airstrike is an EMP drone, and the third
        /// column is the position he is holding.
        /// </summary>
        public static readonly string[] ColumnTitles = { "THE EMITTER", "THE DRONE", "THE LINE" };

        public const string Title = "SERVICE RECORD";

        /// <summary>
        /// Where points come from and what they are for, in one line. The tree is permanent and the
        /// pick-one-of-three at a wave clear is not, and nothing on the old screen said so.
        /// </summary>
        public const string Explain =
            "Levels come from kills and from holding a position to the end. These stay bought for the whole "
            + "retreat — the cards you pick between waves do not.";

        private readonly Loadout _loadout;
        private readonly IComicScreenHost _host;
        private readonly Action? _changed;
        private readonly ComicInput _input = new ComicInput();

        private SkillsPageLayout _page;

        public SkillsScreen(Loadout loadout, IComicScreenHost? host = null, Action? onChanged = null)
        {
            _loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
            _host = host ?? NullComicHost.Instance;
            _changed = onChanged;
            _page = BuildPage();
        }

        // ------------------------------------------------------------------ the tiny surface

        public bool IsOpen { get; private set; }
        public SkillsPageLayout Page => _page;

        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            Rebuild();
            _input.Reset();
        }

        public void Hide() => IsOpen = false;

        // ------------------------------------------------------------------ translation

        /// <summary>Which column a path prints in. Order is the enum's, so the titles line up.</summary>
        public static int ColumnOf(Path path) => (int)path;

        /// <summary>
        /// The catalogue, as dossier entries.
        ///
        /// The three states carry the whole legend, so the mapping matters more than it looks:
        /// Available is anything the player could buy RIGHT NOW (which includes a partly-ranked node
        /// with points spare), Taken is anything already bought that cannot be bought again today,
        /// and Locked is everything else. That ordering means the amber stamps always sit above the
        /// pencil in a column, and the first pencil box under the last stamp is the next thing to buy.
        /// </summary>
        public static List<SkillNodeView> Nodes(Progression.SkillState skills)
        {
            if (skills == null) throw new ArgumentNullException(nameof(skills));

            var all = SkillCatalogue.All;
            var views = new List<SkillNodeView>(all.Count);
            for (int i = 0; i < all.Count; i++)
            {
                var node = all[i];
                int rank = skills.RankOf(node.Id);
                var can = skills.CanSpend(node.Id);

                var state = can == SpendResult.Ok ? SkillState.Available
                          : rank > 0 ? SkillState.Taken
                          : SkillState.Locked;

                string name = node.MaxRank > 1 ? $"{node.Name}  {rank}/{node.MaxRank}" : node.Name;

                // One line, and when it is locked the line has to say WHY. "Needs Cadence" is a
                // route; a greyed box with no reason is a dead end.
                string text = node.Text;
                if (state == SkillState.Locked)
                {
                    text = can switch
                    {
                        SpendResult.PrerequisiteMissing =>
                            $"{node.Text}  —  needs {SkillCatalogue.Find(node.Requires!)?.Name ?? node.Requires}",
                        SpendResult.NotEnoughPoints => $"{node.Text}  —  {node.Cost} points short of it",
                        _ => node.Text,
                    };
                }
                else if (state == SkillState.Taken && rank >= node.MaxRank)
                {
                    text = $"{node.Text}  —  maxed";
                }

                views.Add(new SkillNodeView(node.Id, name, text, state, node.Cost, ColumnOf(node.Path),
                                           invested: rank > 0));
            }
            return views;
        }

        private SkillsPageLayout BuildPage()
            => new SkillsPageLayout(Nodes(_loadout.Skills)) { ColumnTitles = ColumnTitles };

        public void Rebuild()
        {
            int column = _page.Column;
            int row = _page.Row;
            _page = BuildPage();
            _page.SetCursor(column, row);
        }

        // ------------------------------------------------------------------ input

        public bool HandleInput()
        {
            if (!IsOpen) return false;
            Apply(_input.Read());
            return true;
        }

        public SkillsScreenAction Apply(ComicNav nav)
        {
            if (!IsOpen) return SkillsScreenAction.None;

            if (nav.Cancel)
            {
                Hide();
                _host.Tick();
                return SkillsScreenAction.Closed;
            }

            if (nav.Moved)
            {
                // Up NEVER closes this page, at any cursor position. That binding is the exact bug
                // the owner hit, and it is worth the comment every time someone reads this method.
                if (nav.Up) _page.MoveUp();
                if (nav.Down) _page.MoveDown();
                if (nav.Left) _page.MoveLeft();
                if (nav.Right) _page.MoveRight();
                _host.Tick();
            }

            if (nav.Confirm) return Buy();
            return SkillsScreenAction.None;
        }

        private SkillsScreenAction Buy()
        {
            var action = _page.Confirm();
            if (action == SkillsAction.None) return Refuse("nothing under the cursor");

            string id = _page.ActionNodeId;
            var result = _loadout.SpendSkillPoint(id);
            if (result == SpendResult.Ok)
            {
                var node = SkillCatalogue.Find(id);
                Rebuild();
                _changed?.Invoke();
                _host.Accept();
                _host.Notice(node == null ? "bought" : $"{node.Name} — {node.Text}");
                return SkillsScreenAction.Bought;
            }

            return Refuse(result switch
            {
                SpendResult.NotEnoughPoints => "not enough points — hold a position to the end for more",
                SpendResult.PrerequisiteMissing => $"needs {SkillCatalogue.Find(SkillCatalogue.Find(id)?.Requires ?? "")?.Name ?? "the one above it"} first",
                SpendResult.AtMaxRank => "that one is already maxed",
                _ => "cannot buy that",
            });
        }

        private SkillsScreenAction Refuse(string why)
        {
            _host.Refuse();
            _host.Notice(why);
            return SkillsScreenAction.Refused;
        }

        // ------------------------------------------------------------------ copy

        public static string PromptLine(bool pad) => ComicPrompts.Join(
            $"{ComicPrompts.Move(pad)} — move and scroll",
            $"{ComicPrompts.Sideways(pad)} — change file",
            $"{ComicPrompts.Confirm(pad)} — spend a point",
            $"{ComicPrompts.Cancel(pad)} — back to the fight");

        public string Subtitle
        {
            get
            {
                var s = _loadout.Skills;
                string xp = s.XpNeededForNext <= 0
                    ? "top of the ladder"
                    : $"{s.XpIntoLevel}/{s.XpNeededForNext} to the next";
                return $"{Title}     ·     level {s.Level}     ·     {xp}";
            }
        }

        // ------------------------------------------------------------------ drawing

        public void Draw(float uiW, float uiH) => Draw(uiW, uiH, ComicInput.PadPresent);

        public void Draw(float uiW, float uiH, bool pad)
        {
            if (!IsOpen) return;

            ComicScreenChrome.Backdrop(uiW, uiH);
            _page.Layout(ComicScreenChrome.PageRect(uiW, uiH));
            ComicPages.DrawSkills(_page, Subtitle, _loadout.Skills.UnspentPoints);
            ComicScreenChrome.Footer(ComicScreenChrome.FooterRect(uiW, uiH), Explain, PromptLine(pad));
        }
    }
}
