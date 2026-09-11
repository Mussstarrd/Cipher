#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Progression;

namespace Cipher.Game.UI.Comic
{
    public enum TruckScreenAction { None, Closed, Loaded, Unloaded, Refused, PulledOut }

    /// <summary>
    /// THE TRUCK SCREEN: the pack-up window, drawn as a side elevation of the bed and the gravel
    /// beside it.
    ///
    /// The owner played the old one and said: "I don't really understand the pack up mechanism
    /// either because my two towers can come and the truck is only like 4% full". That is two
    /// separate confusions wearing one sentence, and the page has to answer both:
    ///
    ///   - **Why can I only bring two?** Because TIME decides how many emplacements you can reach
    ///     and unbolt, not space. The clock is in the heading for that reason.
    ///   - **Then why is there a gauge at all?** Because when you CAN reach them, the bed decides
    ///     which ones fit. A heavy sentry or four light rotors is a real choice (ADR-005).
    ///
    /// And one thing it must not shout (ADR-009): the truck is a four-door with his wife in the
    /// front and two children in the back. The bed is what is left over around two car seats. That
    /// is stated once, quietly, and never turned into a mechanic.
    ///
    /// This screen keeps two models in step on purpose. <see cref="TruckPageLayout"/> owns the
    /// picture and the cursor; <see cref="TruckLoad"/> owns the real manifest that the match reads
    /// when the window closes. Mirroring one into the other at the moment of the press is cheaper
    /// than rebuilding the page every frame, and it keeps the abandoned-count report honest.
    /// </summary>
    public sealed class TruckScreen
    {
        public const string Title = "PACK UP";

        /// <summary>
        /// The sentence that answers the 4% question. Deliberately about loss, not capacity.
        /// </summary>
        public const string Explain =
            "The clock says how many you reach. The bed says which of those fit around two car seats. "
            + "What is on the gravel when you leave is gone.";

        private readonly ITruckHost _host;
        private readonly ComicInput _input = new ComicInput();

        private readonly List<IHaulable> _source = new List<IHaulable>();
        private TruckLoad _truck;
        private TruckPageLayout _page;

        public TruckScreen(ITruckHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _truck = new TruckLoad();
            _page = BuildPage();
        }

        // ------------------------------------------------------------------ the tiny surface

        public bool IsOpen { get; private set; }
        public TruckPageLayout Page => _page;

        /// <summary>The real manifest. The match reads this to count what was abandoned.</summary>
        public TruckLoad Truck => _truck;

        /// <summary>
        /// Open the window on a fresh set of candidates. Called when the pack-up phase begins; the
        /// truck is new each position because the bed was emptied at the last one.
        /// </summary>
        public void Open(TruckLoad truck, IReadOnlyList<IHaulable> recoverable)
        {
            _truck = truck ?? throw new ArgumentNullException(nameof(truck));
            _source.Clear();
            if (recoverable != null) _source.AddRange(recoverable);
            _page = BuildPage();
            IsOpen = true;
            _input.Reset();
        }

        /// <summary>Re-open the page on the candidates it already has.</summary>
        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            _input.Reset();
        }

        public void Hide() => IsOpen = false;

        // ------------------------------------------------------------------ translation

        /// <summary>Everything that could ride, as the page's generic vocabulary.</summary>
        public static List<TruckItemView> Items(IReadOnlyList<IHaulable> recoverable)
        {
            var list = new List<TruckItemView>(recoverable?.Count ?? 0);
            if (recoverable == null) return list;
            for (int i = 0; i < recoverable.Count; i++)
            {
                var thing = recoverable[i];
                list.Add(new TruckItemView(thing.HaulName, thing.Haulage.Weight, thing.Haulage.Volume));
            }
            return list;
        }

        /// <summary>Which of them are already aboard, index for index.</summary>
        public static List<bool> LoadedFlags(TruckLoad truck, IReadOnlyList<IHaulable> recoverable)
        {
            var flags = new List<bool>(recoverable?.Count ?? 0);
            if (recoverable == null) return flags;
            for (int i = 0; i < recoverable.Count; i++) flags.Add(truck != null && truck.IsLoaded(recoverable[i]));
            return flags;
        }

        private TruckPageLayout BuildPage()
            => new TruckPageLayout(Items(_source), LoadedFlags(_truck, _source), _truck.MaxWeight, _truck.MaxVolume);

        /// <summary>The haulable a drawn crate stands for. Crate.Index is its index in the source list.</summary>
        private IHaulable? SourceOf(TruckCrate crate)
            => crate != null && crate.Index >= 0 && crate.Index < _source.Count ? _source[crate.Index] : null;

        // ------------------------------------------------------------------ input

        public bool HandleInput()
        {
            if (!IsOpen) return false;
            Apply(_input.Read());
            return true;
        }

        public TruckScreenAction Apply(ComicNav nav)
        {
            if (!IsOpen) return TruckScreenAction.None;

            // Leaving is checked before anything else. The old panel had no exit at all on a pad and
            // swallowed the whole pack-up window when it was open.
            if (nav.Leave)
            {
                if (_host.PullOut())
                {
                    Hide();
                    _host.Accept();
                    return TruckScreenAction.PulledOut;
                }
                return Refuse("cannot leave yet");
            }

            if (nav.Cancel)
            {
                Hide();
                _host.Tick();
                return TruckScreenAction.Closed;
            }

            if (nav.Moved)
            {
                if (nav.Left) _page.MoveLeft();
                if (nav.Right) _page.MoveRight();
                if (nav.Up) _page.MoveUp();
                if (nav.Down) _page.MoveDown();
                _host.Tick();
            }

            if (nav.Secondary) return AutoLoad();
            if (nav.Confirm) return Toggle();
            return TruckScreenAction.None;
        }

        /// <summary>
        /// A on the cursor. The order here is load-bearing and was a bug once: check the bed BEFORE
        /// spending window time, or a crate that does not fit still costs four seconds and still pays
        /// out, and the same crate can be sold again every four seconds until the window closes.
        /// </summary>
        private TruckScreenAction Toggle()
        {
            var crate = _page.Selected;
            if (crate == null) return Refuse("nothing under the cursor");

            var thing = SourceOf(crate);
            if (thing == null) return Refuse("nothing under the cursor");

            if (crate.InBed)
            {
                _page.Unload(crate);
                _truck.Unload(thing);
                _host.Tick();
                _host.Notice($"{thing.HaulName} back on the gravel");
                return TruckScreenAction.Unloaded;
            }

            if (!_page.Fits(crate))
            {
                // Name WHICH limit stopped it. "No room" when the bed is 40% full by volume and full
                // by weight is exactly the kind of answer that produced the 4% complaint.
                bool heavy = _page.Weight + crate.Item.Weight > _page.MaxWeight;
                return Refuse(heavy
                    ? $"too heavy — {_page.Weight:F0} of {_page.MaxWeight:F0} kg already aboard"
                    : $"no space — {_page.Volume:F1} of {_page.MaxVolume:F1} m3 already aboard");
            }

            if (!_host.TryUnbolt(thing.RecoveredValue))
                return Refuse("no time left to unbolt it — the scan is coming");

            _page.Load(crate);
            _truck.TryLoad(thing);
            _host.Accept();
            _host.Notice($"loaded {thing.HaulName}");
            return TruckScreenAction.Loaded;
        }

        /// <summary>
        /// X: fill the bed by value density, best first. A convenience, not a strategy — it optimises
        /// value per unit carried, which is not always what the player wants at the next position.
        /// It stops the moment the clock refuses, which is the honest way to show that time is the
        /// binding constraint.
        /// </summary>
        private TruckScreenAction AutoLoad()
        {
            var order = new List<TruckCrate>(_page.LeftBehind);
            order.Sort((a, b) => Density(b).CompareTo(Density(a)));

            int loaded = 0;
            bool ranOutOfTime = false;
            foreach (var crate in order)
            {
                if (!_page.Fits(crate)) continue;
                var thing = SourceOf(crate);
                if (thing == null) continue;
                if (!_host.TryUnbolt(thing.RecoveredValue)) { ranOutOfTime = true; break; }

                if (!_page.Load(crate)) continue;
                _truck.TryLoad(thing);
                loaded++;
            }

            if (loaded == 0)
            {
                return Refuse(ranOutOfTime ? "no time left to unbolt anything" : "nothing else fits in the bed");
            }

            _host.Accept();
            _host.Notice(ranOutOfTime
                ? $"loaded {loaded} — then the clock ran out"
                : $"loaded {loaded}");
            return TruckScreenAction.Loaded;
        }

        private float Density(TruckCrate crate)
        {
            var thing = SourceOf(crate);
            if (thing == null) return 0f;
            float cost = Math.Max(crate.Item.Weight / 100f, crate.Item.Volume);
            return cost <= 0.0001f ? float.MaxValue : thing.RecoveredValue / cost;
        }

        private TruckScreenAction Refuse(string why)
        {
            _host.Refuse();
            _host.Notice(why);
            return TruckScreenAction.Refused;
        }

        // ------------------------------------------------------------------ copy

        public static string PromptLine(bool pad) => ComicPrompts.Join(
            $"{ComicPrompts.Move(pad)} — pick a crate",
            $"{ComicPrompts.Confirm(pad)} — load or unload it",
            $"{ComicPrompts.Secondary(pad)} — load what fits",
            $"{ComicPrompts.Leave(pad)} — leave now",
            $"{ComicPrompts.Cancel(pad)} — back to the fight");

        /// <summary>
        /// The heading carries the clock, because the clock is the answer to "why only two". ADR-009
        /// names what the clock is counting down to.
        /// </summary>
        public string Subtitle
        {
            get
            {
                int left = _page.LeftBehind.Count;
                string loss = left == 0 ? "nothing left on the gravel" : $"{left} still on the gravel";
                return $"{Title}     ·     {Math.Max(0f, _host.SecondsLeft):F0}s before the next scan     ·     {loss}";
            }
        }

        // ------------------------------------------------------------------ drawing

        public void Draw(float uiW, float uiH) => Draw(uiW, uiH, ComicInput.PadPresent);

        public void Draw(float uiW, float uiH, bool pad)
        {
            if (!IsOpen) return;

            ComicScreenChrome.Backdrop(uiW, uiH);
            _page.Layout(ComicScreenChrome.PageRect(uiW, uiH));
            ComicPages.DrawTruck(_page, Subtitle);
            ComicScreenChrome.Footer(ComicScreenChrome.FooterRect(uiW, uiH), Explain, PromptLine(pad));
        }
    }
}
