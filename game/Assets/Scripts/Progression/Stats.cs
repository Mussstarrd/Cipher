#nullable enable
using System;

namespace Cipher.Game.Progression
{
    /// <summary>
    /// Everything gear and upgrades are allowed to change. Keeping this list short and closed is
    /// deliberate: every new stat multiplies the balance surface and every affix and card has to
    /// mean something against it.
    /// </summary>
    public enum StatKind
    {
        GunDamage,
        FireRate,
        MaxHealth,
        /// <summary>Flat damage subtracted per contact tick. The only flat-only stat.</summary>
        Armour,
        MoveSpeed,
        CashPerKill,
        /// <summary>Percent here REDUCES the cooldown. Positive is good, as everywhere else.</summary>
        AirstrikeCooldown,
        /// <summary>Blast radius per bomb, which is how the strike gets wider rather than stronger.</summary>
        AirstrikeRadius,
        AirstrikeDamage,
        /// <summary>Percent here REDUCES what your own strike does to you.</summary>
        AirstrikeSelfDamage,
        TurretDamage,
        TurretRange,
        /// <summary>How fast repair drones close a breach stage.</summary>
        RepairSpeed,
    }

    /// <summary>
    /// A bag of stat modifiers. Two channels per stat, because they compose differently:
    /// flat bonuses add, percent bonuses add together and then apply once. Percent is stored as a
    /// fraction, so 0.18 is +18%.
    ///
    /// Additive percent (rather than multiplicative) is a deliberate balance choice: it keeps eight
    /// pieces of gear from compounding into an unreadable number, and it makes an item's card
    /// honest, because +18% means the same thing on the first item and the eighth.
    /// </summary>
    public sealed class StatBlock
    {
        private static readonly int Count = Enum.GetValues(typeof(StatKind)).Length;

        private readonly float[] _flat = new float[Count];
        private readonly float[] _percent = new float[Count];

        public float Flat(StatKind kind) => _flat[(int)kind];
        public float Percent(StatKind kind) => _percent[(int)kind];

        public StatBlock AddFlat(StatKind kind, float amount)
        {
            _flat[(int)kind] += amount;
            return this;
        }

        public StatBlock AddPercent(StatKind kind, float fraction)
        {
            _percent[(int)kind] += fraction;
            return this;
        }

        /// <summary>Folds another block into this one. Used to total gear plus upgrades.</summary>
        public StatBlock Add(StatBlock other)
        {
            if (other == null) return this;
            for (int i = 0; i < Count; i++)
            {
                _flat[i] += other._flat[i];
                _percent[i] += other._percent[i];
            }
            return this;
        }

        public StatBlock Clone()
        {
            var copy = new StatBlock();
            Array.Copy(_flat, copy._flat, Count);
            Array.Copy(_percent, copy._percent, Count);
            return copy;
        }

        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < Count; i++)
                    if (_flat[i] != 0f || _percent[i] != 0f) return false;
                return true;
            }
        }

        /// <summary>Applies this block to a base value. Flat first, then the summed percent.</summary>
        public float Apply(StatKind kind, float baseValue)
            => (baseValue + _flat[(int)kind]) * (1f + _percent[(int)kind]);

        /// <summary>
        /// For stats where lower is better. Clamped so stacked cooldown reduction can never reach
        /// zero and turn the ultimate into a primary fire.
        /// </summary>
        public float ApplyReduction(StatKind kind, float baseValue, float floorFraction = 0.35f)
        {
            float reduced = (baseValue - _flat[(int)kind]) * (1f - _percent[(int)kind]);
            return Math.Max(baseValue * floorFraction, reduced);
        }
    }
}
