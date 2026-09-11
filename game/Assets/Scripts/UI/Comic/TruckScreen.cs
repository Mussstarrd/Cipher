#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Progression;

namespace Cipher.Game.UI.Comic
{
    public enum TruckScreenAction { None, Closed, Loaded, Unloaded, Refused, PulledOut }

    /// <summary>
    /// How close the scan is, as one value the whole page reads. Three states rather than a
    /// continuous number because a page cannot shout by degrees.
    /// </summary>
    public enum TruckUrgency { Calm, Hurry, Final }

    /// <summary>
    /// Everything the truck page prints in words, gathered once per frame.
    ///
    /// <see cref="ComicPages"/> stays a pure drawing layer — it takes geometry and copy and owns no
    /// state — and the copy is a single struct rather than six string parameters so that adding a
    /// line to the page is not a signature change in three files. Every field is asserted in
    /// ComicScreenTests, which is only possible because none of it is composed inside OnGUI.
    /// </summary>
    public readonly struct TruckPageCopy
    {
        public readonly string Subtitle;
        public readonly string ClockText;
        public readonly string ClockLabel;
        public readonly string Warning;
        public readonly string Ledger;
        public readonly string CabLine;
        public readonly TruckUrgency Urgency;

        public TruckPageCopy(string subtitle, string clockText, string clockLabel,
                             string warning, string ledger, string cabLine, TruckUrgency urgency)
        {
            Subtitle = subtitle ?? string.Empty;
            ClockText = clockText ?? string.Empty;
            ClockLabel = clockLabel ?? string.Empty;
            Warning = warning ?? string.Empty;
            Ledger = ledger ?? string.Empty;
            CabLine = cabLine ?? string.Empty;
            Urgency = urgency;
        }
    }

    /// <summary>
    /// THE TRUCK SCREEN: the pack-up window, drawn as a side elevation of the bed, a labelled
    /// manifest of what is in it, and a labelled manifest of what you are leaving.
    ///
    /// ## The owner played it and gave us three faults (2026-09-11)
    ///
    /// > "I also have no idea what I packed the truck with there was nothing was labeled or gave me
    /// > any values or anything but after a certain amount of time it just kicked me out to that
    /// > fell back screen so I'm not sure what that really meant either"
    ///
    /// **1. Nothing was labelled.** See <see cref="TruckPageLayout"/>: labels moved onto cards.
    ///
    /// **2. GEAR IS OFF THIS PAGE.** The extraction list handed to this screen was every emplacement
    /// PLUS every item in the pack, and a pack in the middle of a mission is twenty-odd items. A dog
    /// tag is 0.0005 cubic metres against a sentry's 2.4, so gear drew as identical hairline boxes
    /// and dragged the volume gauge's scale into meaninglessness. ADR-005 is explicit that gear is
    /// never the thing you cut, and the previous specialist flagged this and did not act on it.
    ///
    /// The call, made here: **the bed page shows emplacements; the kit rides in the cab and the page
    /// says so, once.** That is what makes both gauges honest — every number on them is now about
    /// something that could genuinely be left behind. It costs the player nothing, because nothing
    /// downstream ever charged gear for the ride: the abandoned count already filtered to
    /// emplacements, and the pack is never emptied by extraction.
    ///
    /// **3. The window ended and he did not know why.** The clock was one clause in a header line
    /// among three. It is now the largest element on the page after the truck, it changes at thirty
    /// seconds and again at ten, and it says in words what happens when it reaches zero. See
    /// <see cref="ClockText"/> and <see cref="Warning"/>.
    ///
    /// And one thing it must not shout (ADR-009): the truck is a four-door with his wife in the
    /// front and two children in the back. The bed is what is left over around two car seats. That
    /// is stated once, quietly, and never turned into a mechanic.
    ///
    /// This screen keeps two models in step on purpose. <see cref="TruckPageLayout"/> owns the
    /// picture and the cursor; <see cref="TruckLoad"/> owns the real manifest that the match reads
    /// when the window closes.
    /// </summary>
    public sealed class TruckScreen
    {
        public const string Title = "PACK UP";

        /// <summary>Below this many seconds the page stops being calm about it.</summary>
        public const float HurrySeconds = 30f;

        /// <summary>And below this it counts out loud.</summary>
        public const float FinalSeconds = 10f;

        /// <summary>
        /// The sentence at the foot. Deliberately about loss, and it now also says what the clock
        /// reaching zero does, because that is the thing he did not know.
        /// </summary>
        public const string Explain =
            "The clock says how many you reach. The bed says which of those fit around two car seats. "
            + "When it runs out you drive; what is still on the gravel stays here for good.";

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

        /// <summary>How many pieces of kit are riding in the cab rather than competing for the bed.</summary>
        public int CabCount { get; private set; }

        /// <summary>
        /// Open the window on a fresh set of candidates. Called when the pack-up phase begins; the
        /// truck is new each position because the bed was emptied at the last one.
        /// </summary>
        public void Open(TruckLoad truck, IReadOnlyList<IHaulable> recoverable)
        {
            _truck = truck ?? throw new ArgumentNullException(nameof(truck));
            _source.Clear();
            CabCount = 0;
            if (recoverable != null)
            {
                for (int i = 0; i < recoverable.Count; i++)
                {
                    var thing = recoverable[i];
                    if (thing == null) continue;
                    if (RidesInTheCab(thing)) CabCount++;
                    else _source.Add(thing);
                }
            }
            _page = BuildPage();
            IsOpen = true;
            _input.Reset();
        }

        /// <summary>
        /// Gear goes in the cab, not the bed. The test is the type rather than a size threshold: a
        /// threshold would silently reclassify any emplacement we later make small, and this page's
        /// whole argument is that everything on it is something you could lose.
        /// </summary>
        public static bool RidesInTheCab(IHaulable thing) => thing is HauledItem;

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
                list.Add(new TruckItemView(thing.HaulName, thing.Haulage.Weight, thing.Haulage.Volume,
                                           thing.RecoveredValue));
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
                return Refuse(_page.TooHeavy(crate)
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

        // ------------------------------------------------------------------ the clock

        public float SecondsLeft => Math.Max(0f, _host.SecondsLeft);

        public TruckUrgency Urgency
        {
            get
            {
                float s = SecondsLeft;
                if (s <= FinalSeconds) return TruckUrgency.Final;
                if (s <= HurrySeconds) return TruckUrgency.Hurry;
                return TruckUrgency.Calm;
            }
        }

        /// <summary>
        /// The stamp's number. Minutes and seconds while there is time to read them, and a bare
        /// count in the last ten — a clock that reads 0:07 and one that reads 7 are the same fact
        /// and the second one is a countdown.
        /// </summary>
        public string ClockText
        {
            get
            {
                float s = SecondsLeft;
                if (s <= FinalSeconds) return ((int)Math.Ceiling(s)).ToString();
                int whole = (int)Math.Ceiling(s);
                return $"{whole / 60}:{whole % 60:00}";
            }
        }

        /// <summary>What the number is counting, printed under it so it is never just a number.</summary>
        public string ClockLabel => Urgency == TruckUrgency.Final ? "SECONDS" : "UNTIL THE SCAN";

        /// <summary>
        /// The line the page shouts as the window closes. Empty while there is time.
        ///
        /// The owner: "after a certain amount of time it just kicked me out". An ending that is
        /// announced is not a kicking out, and the announcement has to name the consequence — not
        /// "time low" but what will happen to the four crates he is looking at.
        /// </summary>
        public string Warning
        {
            get
            {
                if (Urgency == TruckUrgency.Calm) return string.Empty;
                int left = _page.LeftBehind.Count;
                if (Urgency == TruckUrgency.Final)
                    return left == 0
                        ? "PULLING OUT — THE BED IS CLEAR"
                        : $"PULLING OUT — {left} STAY HERE";
                return left == 0
                    ? "THE SCAN IS CLOSE. NOTHING LEFT TO LOSE HERE."
                    : $"THE SCAN IS CLOSE. {left} STILL ON THE GRAVEL.";
            }
        }

        /// <summary>
        /// Said once, quietly, so the page never has to explain where the helmets went. ADR-005:
        /// gear is never the thing you cut.
        /// </summary>
        public string CabLine => CabCount == 1
            ? "Your kit rides in the cab — 1 piece, no bed space."
            : $"Your kit rides in the cab — {CabCount} pieces, no bed space.";

        // ------------------------------------------------------------------ copy

        public static string PromptLine(bool pad) => ComicPrompts.Join(
            $"{ComicPrompts.Move(pad)} — pick a crate",
            $"{ComicPrompts.Confirm(pad)} — load or unload it",
            $"{ComicPrompts.Secondary(pad)} — load what fits",
            $"{ComicPrompts.Leave(pad)} — leave now",
            $"{ComicPrompts.Cancel(pad)} — back to the fight");

        /// <summary>
        /// The heading. The clock came OUT of it and onto its own stamp — four clauses separated by
        /// dots is a place a countdown goes to be missed, which is exactly what happened.
        /// </summary>
        public string Subtitle
        {
            get
            {
                int left = _page.LeftBehind.Count;
                string loss = left == 0 ? "nothing left on the gravel" : $"{left} still on the gravel";
                return $"{Title}  —  {_page.Bed.Count} on the truck, {loss}";
            }
        }

        /// <summary>The totals band under the panels: the sentence the player is composing.</summary>
        public string Ledger
        {
            get
            {
                int left = _page.LeftBehind.Count;
                string taking = $"TAKING {_page.Bed.Count}  ·  ${_page.LoadedValue}";
                string losing = left == 0
                    ? "LEAVING NOTHING"
                    : $"LEAVING {left}  ·  ${_page.AbandonedValue} bolted down for good";
                return $"{taking}          {losing}";
            }
        }

        /// <summary>Every word on the page, in one value. See <see cref="TruckPageCopy"/>.</summary>
        public TruckPageCopy Copy => new TruckPageCopy(Subtitle, ClockText, ClockLabel,
                                                       Warning, Ledger, CabLine, Urgency);

        // ------------------------------------------------------------------ drawing

        public void Draw(float uiW, float uiH) => Draw(uiW, uiH, ComicInput.PadPresent);

        public void Draw(float uiW, float uiH, bool pad)
        {
            if (!IsOpen) return;

            ComicScreenChrome.Backdrop(uiW, uiH);
            _page.Layout(ComicScreenChrome.PageRect(uiW, uiH));
            ComicPages.DrawTruck(_page, Copy);
            ComicScreenChrome.Footer(ComicScreenChrome.FooterRect(uiW, uiH), Explain, PromptLine(pad));
        }
    }
}
