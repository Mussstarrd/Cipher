#nullable enable
using System;
using System.Text;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// What the end-of-position screen says. Facts in, two strings out; no engine, no state.
    ///
    /// ## Why this exists as its own class
    ///
    /// The owner, 2026-09-11: *"after a certain amount of time it just kicked me out to that fell
    /// back screen so I'm not sure what that really meant either."*
    ///
    /// The screen he saw led with five numbers and never said what had happened to him. Two ADRs
    /// spend paragraphs on the fact that this moment is **not a defeat** — ADR-005: *"Extracted is
    /// neither a win nor a loss, which is the whole point: leaving on schedule is the correct play,
    /// not a failure"* — and the screen that is the entire player-facing expression of that idea
    /// opened with a wave count.
    ///
    /// So the first line is now a sentence, not a statistic, and the numbers are a labelled ledger
    /// underneath it rather than three run-on lines. Composing it here rather than inline in the
    /// bootstrap's OnGUI is what makes every one of those sentences assertable in EditMode; the
    /// previous copy was a format string inside a draw call and nothing could reach it.
    /// </summary>
    public static class FellBackReport
    {
        /// <summary>The heading. Deliberately the same two words the player already knows.</summary>
        public const string Title = "FELL BACK";

        /// <summary>
        /// The line that answers "what did that mean". It has to do two jobs in one sentence: say
        /// this is not a failure, and say why leaving was the point.
        /// </summary>
        public const string Lead =
            "Not a defeat. The position is spent and you were gone before the sweep — which is what a position is for.";

        /// <summary>The same line for the one position with nothing behind it (ADR-005 §5).</summary>
        public const string LeadNowhereToGo =
            "Not a defeat. The position is spent — but there is no line behind this one.";

        /// <summary>
        /// Everything the screen needs. A struct so the bootstrap hands over facts rather than a
        /// pre-formatted string, and so a test can build a position that never happened.
        /// </summary>
        public readonly struct Facts
        {
            public readonly int WavesHeld;
            public readonly int ChipsBroken;
            public readonly int OnTheTruck;
            public readonly int LeftBehind;

            /// <summary>Seconds of the scan cycle still unspent when the truck pulled out.</summary>
            public readonly float PrepSeconds;

            /// <summary>Dollars those seconds plus the salvage are worth at the next position.</summary>
            public readonly int Materials;

            /// <summary>Display name of the next position, or empty when there is not one.</summary>
            public readonly string NextName;

            /// <summary>Print controller prompts rather than keyboard ones.</summary>
            public readonly bool Pad;

            public Facts(int wavesHeld, int chipsBroken, int onTheTruck, int leftBehind,
                         float prepSeconds, int materials, string? nextName, bool pad)
            {
                WavesHeld = Math.Max(0, wavesHeld);
                ChipsBroken = Math.Max(0, chipsBroken);
                OnTheTruck = Math.Max(0, onTheTruck);
                LeftBehind = Math.Max(0, leftBehind);
                PrepSeconds = Math.Max(0f, prepSeconds);
                Materials = Math.Max(0, materials);
                NextName = nextName ?? string.Empty;
                Pad = pad;
            }
        }

        /// <summary>True when there is a position behind this one to fall back to.</summary>
        public static bool HasNext(in Facts f) => !string.IsNullOrWhiteSpace(f.NextName);

        /// <summary>
        /// The body of the screen: the lead sentence, a blank line, the ledger, then where you are
        /// going and the button that takes you there.
        ///
        /// The ledger's left column is a LABEL rather than a number. "3 emplacements on the truck,
        /// 4 left bolted down" is a sentence the eye has to parse; "ON THE TRUCK   3 emplacements"
        /// is a row on a form, and a form is what a man packing a truck against a clock would keep.
        /// </summary>
        public static string Compose(in Facts f)
        {
            var sb = new StringBuilder();
            sb.Append(HasNext(f) ? Lead : LeadNowhereToGo).Append('\n').Append('\n');

            Row(sb, "HELD", f.WavesHeld == 1 ? "1 wave" : $"{f.WavesHeld} waves");
            Row(sb, "PUT DOWN", $"{f.ChipsBroken} of them");
            Row(sb, "ON THE TRUCK", Emplacements(f.OnTheTruck));

            // Naming the loss in the same breath as the word "gone" is the point of the whole
            // pack-up window. A count with no verdict on it reads as inventory.
            Row(sb, "LEFT BEHIND", f.LeftBehind == 0
                ? "nothing — the gravel is clear"
                : $"{Emplacements(f.LeftBehind)}, bolted down for good");

            // THE THIRD PLACE THE CLOCK IS SPENT (ADR-005 §4). This row is the only time the player
            // is ever shown that pulling out early bought something, and it was previously a
            // fragment reading "2:14 of the cycle left -> $161 of materials at the next line".
            Row(sb, "TIME UNSPENT", $"{Clock(f.PrepSeconds)}  —  ${f.Materials} of materials waiting for you");

            sb.Append('\n');
            sb.Append(HasNext(f)
                ? $"NEXT POSITION: {f.NextName}"
                : "There is nowhere else to fall back to.");
            sb.Append('\n');
            sb.Append(f.Pad ? "A: move out" : "Enter: move out");
            return sb.ToString();
        }

        private static void Row(StringBuilder sb, string label, string value)
            => sb.Append(label.PadRight(16)).Append(value).Append('\n');

        private static string Emplacements(int n) => n == 1 ? "1 emplacement" : $"{n} emplacements";

        /// <summary>m:ss. A bare second count is a number; a clock is a duration.</summary>
        public static string Clock(float seconds)
        {
            int whole = (int)Math.Round(Math.Max(0f, seconds));
            return $"{whole / 60}:{whole % 60:00}";
        }
    }
}
