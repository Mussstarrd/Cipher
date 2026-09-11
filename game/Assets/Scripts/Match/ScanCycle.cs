#nullable enable
using System;

namespace Cipher.Game.Match
{
    /// <summary>
    /// The scan cycle (ADR-005). HALCYON cannot see the unchipped continuously: a wide-area
    /// sweep needs an orbital window and compute it does not have spare, so it runs on a cycle.
    /// A scan is what sends the signed at you; the gap between scans is the whole rest of the game.
    ///
    /// This type is the tuning for one position. The campaign layer shortens <see cref="CycleSeconds"/>
    /// mission by mission, which is the difficulty curve and the fiction in the same number.
    /// </summary>
    public sealed class ScanCycleConfig
    {
        /// <summary>
        /// The budget for the whole cycle: fighting, then packing, then whatever is left is prep
        /// at the next position. Spending it in one place is spending it.
        /// </summary>
        public float CycleSeconds { get; set; } = 600f;

        /// <summary>Waves you must clear before the game will let you call it. Stops wave-1 bailing.</summary>
        public int MinWavesBeforeExtract { get; set; } = 2;

        /// <summary>The pack-up window that opens once your declared last wave is cleared.</summary>
        public float ExtractSeconds { get; set; } = 90f;

        /// <summary>How long one emplacement takes to unbolt and get on the truck.</summary>
        public float UnboltSeconds { get; set; } = 4f;

        /// <summary>
        /// Bonus on the wave-clear payout for declaring the last wave BEFORE it spawns, i.e. for
        /// committing while you still do not know what is in it.
        /// </summary>
        public float CommitBonus { get; set; } = 0.25f;

        /// <summary>
        /// Dollars of materials per second of cycle time left over when you pull out.
        ///
        /// ADR-005 says the remainder of the cycle is prep at the next position -- fortify, trap,
        /// rest -- and this is how that becomes a number the player can feel. It cannot be MORE
        /// setup time, because the opening setup at a new position is already untimed; time you
        /// spent not fighting has to arrive as something you built with it.
        /// </summary>
        public float PrepDollarsPerSecond { get; set; } = 1.2f;

        public ScanCycleConfig Clone() => new ScanCycleConfig
        {
            PrepDollarsPerSecond = PrepDollarsPerSecond,
            CycleSeconds = CycleSeconds,
            MinWavesBeforeExtract = MinWavesBeforeExtract,
            ExtractSeconds = ExtractSeconds,
            UnboltSeconds = UnboltSeconds,
            CommitBonus = CommitBonus,
        };
    }

    /// <summary>Why a call to <see cref="MatchState.DeclareLastWave"/> was refused.</summary>
    public enum DeclareResult
    {
        Ok,
        /// <summary>You have not held long enough yet.</summary>
        TooEarly,
        /// <summary>Already called.</summary>
        AlreadyDeclared,
        /// <summary>Match is over, or you are already packing.</summary>
        NotFighting,
        /// <summary>This position has no line behind it. Mission 12 is this.</summary>
        NowhereToGo,
    }

    /// <summary>Result of trying to recover one emplacement during the pack-up window.</summary>
    public enum SalvageResult
    {
        Ok,
        /// <summary>Not in the pack-up window.</summary>
        NotExtracting,
        /// <summary>The next scan is too close. You are leaving it.</summary>
        OutOfTime,
    }
}
