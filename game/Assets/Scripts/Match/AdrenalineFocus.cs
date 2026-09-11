#nullable enable
using System;

namespace Cipher.Game.Match
{
    /// <summary>
    /// Build mode slows time for a moment so the player can actually read the board. Owner's call,
    /// 2026-09-11:
    ///
    ///   "you should be able to ooen build mode and time slows a good percent for a few seconds to
    ///    give you a chance o actually absorb and adjust plan. Maybe it's like adrenaline focus or
    ///    something you can open build mode for 2 cycles of this before your slowdown is depleted"
    ///
    /// The charge limit is what makes it a decision rather than a pause button. Two charges means
    /// the player can re-plan twice under pressure and then has to live with the maze they built,
    /// which is exactly the tension a tower defence wants at the moment a wall goes down.
    ///
    /// Pure state, no engine types: the game layer reads <see cref="TimeScale"/> and sets it.
    /// </summary>
    public sealed class AdrenalineFocus
    {
        /// <summary>How slow the world gets while focus is burning. 0.35 is a hard, readable drop.</summary>
        public float SlowFactor { get; set; } = 0.35f;

        /// <summary>Seconds of slowed time one charge buys.</summary>
        public float ChargeSeconds { get; set; } = 4f;

        /// <summary>Charges available when full.</summary>
        public int MaxCharges { get; set; } = 2;

        /// <summary>Seconds to regain one charge once the player has stopped using it.</summary>
        public float RechargeSeconds { get; set; } = 25f;

        public int Charges { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>Seconds left in the burning charge.</summary>
        public float ActiveSecondsLeft { get; private set; }

        /// <summary>Progress toward the next charge, 0 to 1.</summary>
        public float RechargeFraction => MaxCharges <= 0 || Charges >= MaxCharges
            ? 1f
            : Math.Clamp(_rechargeElapsed / Math.Max(0.01f, RechargeSeconds), 0f, 1f);

        public bool IsDepleted => Charges <= 0 && !IsActive;

        /// <summary>What the game should set its time scale to right now.</summary>
        public float TimeScale => IsActive ? SlowFactor : 1f;

        private float _rechargeElapsed;

        public AdrenalineFocus()
        {
            Charges = MaxCharges;
        }

        /// <summary>Refills on entering a new position. Focus is per-mission, not per-run.</summary>
        public void Reset()
        {
            Charges = MaxCharges;
            IsActive = false;
            ActiveSecondsLeft = 0f;
            _rechargeElapsed = 0f;
        }

        /// <summary>
        /// Player opened build mode. Spends a charge if one is available; returns false when the
        /// player is out, in which case build mode still opens, just at full speed.
        /// </summary>
        public bool TryEngage()
        {
            if (IsActive) return true;
            if (Charges <= 0) return false;

            Charges--;
            IsActive = true;
            ActiveSecondsLeft = ChargeSeconds;
            _rechargeElapsed = 0f;
            return true;
        }

        /// <summary>
        /// Player closed build mode. Ends the slow immediately and banks nothing: a charge spent is
        /// spent, so opening build mode to glance at the map costs the same as using it properly.
        /// </summary>
        public void Release()
        {
            IsActive = false;
            ActiveSecondsLeft = 0f;
        }

        /// <summary>
        /// Advance. Pass UNSCALED delta time: the focus clock must not be slowed by its own effect,
        /// or four seconds of focus would last eleven.
        /// </summary>
        public void Tick(float unscaledDt)
        {
            if (unscaledDt <= 0f) return;

            if (IsActive)
            {
                ActiveSecondsLeft -= unscaledDt;
                if (ActiveSecondsLeft > 0f) return;

                // The charge ran out part-way through this tick. Spill what is left over into
                // recharging rather than discarding it, or a long frame quietly eats real time.
                unscaledDt = -ActiveSecondsLeft;
                IsActive = false;
                ActiveSecondsLeft = 0f;
                if (unscaledDt <= 0f) return;
            }

            if (Charges >= MaxCharges) { _rechargeElapsed = 0f; return; }

            _rechargeElapsed += unscaledDt;
            while (_rechargeElapsed >= RechargeSeconds && Charges < MaxCharges)
            {
                _rechargeElapsed -= RechargeSeconds;
                Charges++;
            }
            if (Charges >= MaxCharges) _rechargeElapsed = 0f;
        }

        public string StatusLine()
        {
            if (IsActive) return $"FOCUS {ActiveSecondsLeft:F1}s";
            if (Charges > 0) return $"focus {Charges}/{MaxCharges}";
            return $"focus recharging {RechargeFraction * 100f:F0}%";
        }
    }
}
