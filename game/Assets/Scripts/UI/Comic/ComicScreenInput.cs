#nullable enable
using System;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// One frame of navigation, already reduced to intent. Edge-triggered: every field means "this
    /// happened on this frame", never "this is held".
    ///
    /// This struct is the whole reason the three screen controllers are unit-testable. The device
    /// reading lives in <see cref="ComicInput"/>, which touches UnityEngine and is not tested; every
    /// decision the screens make is a function of a ComicNav and the model, which is exactly the
    /// PauseMenuModel seam.
    /// </summary>
    public struct ComicNav
    {
        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;

        /// <summary>A / Enter / Space.</summary>
        public bool Confirm;

        /// <summary>
        /// B / Back / Escape. THE ONLY WAY OUT of any of these screens.
        ///
        /// The button that opened a page is deliberately not wired to close it: the owner lost his
        /// skills page to exactly that (2026-09-11, "once I'm in my skills if I press up again it
        /// exits out of my skills so that's counterproductive"). While a screen is open it consumes
        /// every input, so the opening binding in the bootstrap is never even reached.
        /// </summary>
        public bool Cancel;

        /// <summary>X / F. The one extra verb a page is allowed: scrap, auto-load.</summary>
        public bool Secondary;

        /// <summary>Y / L. Only the truck uses it, and only to leave the position.</summary>
        public bool Leave;

        public bool Moved => Up || Down || Left || Right;

        public bool Any => Moved || Confirm || Cancel || Secondary || Leave;
    }

    /// <summary>
    /// An analogue axis turned into discrete menu steps, with a hold-to-repeat.
    ///
    /// A stick is not a button: without a deadzone plus a latch, one nudge scrolls a list from top
    /// to bottom in three frames, and without a repeat the player has to flick once per row, which
    /// is what makes a long skill list feel broken on a pad. First step is immediate, then a pause,
    /// then a steady repeat — the same shape as a keyboard's key repeat, because that is the
    /// cadence everyone already has in their hands.
    /// </summary>
    public sealed class AxisRepeat
    {
        /// <summary>Below this the axis is considered centred. Generous: sticks rest off-zero.</summary>
        public float Deadzone { get; set; } = 0.5f;

        /// <summary>How long a direction must be held before it starts repeating.</summary>
        public float FirstDelay { get; set; } = 0.38f;

        /// <summary>Time between repeats once it has started.</summary>
        public float RepeatDelay { get; set; } = 0.11f;

        private int _held;
        private float _timer;

        /// <summary>Returns -1, 0 or +1: how many rows to move this frame.</summary>
        public int Step(float axis, float dt)
        {
            int dir = axis > Deadzone ? 1 : axis < -Deadzone ? -1 : 0;

            if (dir == 0)
            {
                _held = 0;
                _timer = 0f;
                return 0;
            }

            if (dir != _held)
            {
                // A new direction always fires at once. Anything else feels like input lag.
                _held = dir;
                _timer = FirstDelay;
                return dir;
            }

            _timer -= dt;
            if (_timer > 0f) return 0;
            _timer = RepeatDelay;
            return dir;
        }

        /// <summary>Forget the hold. Call it when a screen opens so a stick already off-centre does not fire.</summary>
        public void Reset()
        {
            _held = 0;
            _timer = 0f;
        }
    }

    /// <summary>
    /// Every prompt these screens print, named for the device actually in the player's hands.
    ///
    /// ADR-002 makes the controller the design centre on BOTH platforms and it is easy to forget
    /// while testing on a keyboard — the skill tree shipped with no pad binding at all and prompts
    /// that named keyboard keys, which is the complaint that started this work: "there are only
    /// instructions on screen for keyboard keys and the point of this is to use controller
    /// primarily". Routing every prompt through here means a keyboard-only string cannot be written
    /// by accident, and a test can assert it.
    /// </summary>
    public static class ComicPrompts
    {
        public static string Move(bool pad) => pad ? "Left stick" : "Arrow keys";
        public static string Sideways(bool pad) => pad ? "Left stick left/right" : "Left/Right";
        public static string Confirm(bool pad) => pad ? "A" : "Enter";
        public static string Cancel(bool pad) => pad ? "B" : "Esc";
        public static string Secondary(bool pad) => pad ? "X" : "F";
        public static string Leave(bool pad) => pad ? "Y" : "L";

        public static string OpenKit(bool pad) => pad ? "D-pad right" : "I";
        public static string OpenSkills(bool pad) => pad ? "D-pad up" : "K";

        /// <summary>Joins prompt clauses with the separator the pages use. Empty clauses drop out.</summary>
        public static string Join(params string[] clauses)
        {
            if (clauses == null) return string.Empty;
            var sb = new System.Text.StringBuilder();
            foreach (var c in clauses)
            {
                if (string.IsNullOrEmpty(c)) continue;
                if (sb.Length > 0) sb.Append("     ");
                sb.Append(c);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// What a screen needs from the game around it: a line of feedback and a noise. Deliberately
    /// tiny, so the bootstrap satisfies it with four one-line lambdas and a test satisfies it with a
    /// recorder.
    /// </summary>
    public interface IComicScreenHost
    {
        /// <summary>A transient line under the HUD. The bootstrap's _declineNotice.</summary>
        void Notice(string text);

        /// <summary>Cursor moved.</summary>
        void Tick();

        /// <summary>Something was accepted.</summary>
        void Accept();

        /// <summary>Something was refused. Silence on a button press reads as a broken game.</summary>
        void Refuse();
    }

    /// <summary>
    /// The truck screen additionally has to spend the pack-up window, which belongs to MatchState.
    /// Unbolting is charged BEFORE the crate goes in the bed, and the screen never touches the clock
    /// itself.
    /// </summary>
    public interface ITruckHost : IComicScreenHost
    {
        /// <summary>Seconds until the next hyper-scan (ADR-009). Printed, never decided here.</summary>
        float SecondsLeft { get; }

        /// <summary>
        /// Spend window time unbolting one emplacement. False means there is no time left, which is
        /// the answer to "why can my two towers come and the truck is only 4% full".
        /// </summary>
        bool TryUnbolt(int recoveredValue);

        /// <summary>Leave the position now, with whatever is aboard.</summary>
        bool PullOut();
    }

    /// <summary>A host that does nothing. Lets a caller construct a screen without wiring feedback yet.</summary>
    public sealed class NullComicHost : IComicScreenHost
    {
        public static readonly NullComicHost Instance = new NullComicHost();
        public void Notice(string text) { }
        public void Tick() { }
        public void Accept() { }
        public void Refuse() { }
    }

    /// <summary>
    /// A host built from delegates, so the bootstrap can satisfy the interface at the call site
    /// without declaring a nested class.
    /// </summary>
    public sealed class DelegateComicHost : IComicScreenHost
    {
        private readonly Action<string>? _notice;
        private readonly Action? _tick;
        private readonly Action? _accept;
        private readonly Action? _refuse;

        public DelegateComicHost(Action<string>? notice = null, Action? tick = null,
                                 Action? accept = null, Action? refuse = null)
        {
            _notice = notice;
            _tick = tick;
            _accept = accept;
            _refuse = refuse;
        }

        public void Notice(string text) => _notice?.Invoke(text);
        public void Tick() => _tick?.Invoke();
        public void Accept() => _accept?.Invoke();
        public void Refuse() => _refuse?.Invoke();
    }

    /// <summary>Same, for the truck's three extra members.</summary>
    public sealed class DelegateTruckHost : ITruckHost
    {
        private readonly IComicScreenHost _inner;
        private readonly Func<float> _secondsLeft;
        private readonly Func<int, bool> _unbolt;
        private readonly Func<bool> _pullOut;

        public DelegateTruckHost(IComicScreenHost inner, Func<float> secondsLeft,
                                 Func<int, bool> unbolt, Func<bool> pullOut)
        {
            _inner = inner ?? NullComicHost.Instance;
            _secondsLeft = secondsLeft ?? throw new ArgumentNullException(nameof(secondsLeft));
            _unbolt = unbolt ?? throw new ArgumentNullException(nameof(unbolt));
            _pullOut = pullOut ?? throw new ArgumentNullException(nameof(pullOut));
        }

        public float SecondsLeft => _secondsLeft();
        public bool TryUnbolt(int recoveredValue) => _unbolt(recoveredValue);
        public bool PullOut() => _pullOut();

        public void Notice(string text) => _inner.Notice(text);
        public void Tick() => _inner.Tick();
        public void Accept() => _inner.Accept();
        public void Refuse() => _inner.Refuse();
    }
}
