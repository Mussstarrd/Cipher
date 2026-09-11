#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Match;

namespace Cipher.Game.Progression
{
    /// <summary>Where a drop came from. Rates and quality floors differ per source.</summary>
    public enum DropSource { RunnerKill, SapperKill, SpitterKill, WaveClear, MissionReward }

    /// <summary>
    /// Rolls gear. Seeded, so a mission replays identically and a bug is reproducible.
    ///
    /// The whole point of the budget formula is that a roller cannot accidentally create a
    /// best-in-slot: it spends one budget across the affix count with a little jitter, so two
    /// items of the same shape are worth about the same and the choice is about which stats,
    /// not which item is secretly better.
    /// </summary>
    public sealed class ItemRoller
    {
        private const float Jitter = 0.12f;
        private XorShift64 _rng;

        public ItemRoller(ulong seed) { _rng = new XorShift64(seed); }

        /// <summary>Un-floored rarity roll: 62 / 24 / 10 / 3.5 / 0.5 percent.</summary>
        public Rarity RollRarity(Rarity floor = Rarity.Scavenged)
        {
            float r = _rng.NextFloat() * 100f;
            Rarity rolled =
                r < 62f ? Rarity.Scavenged :
                r < 86f ? Rarity.Serviceable :
                r < 96f ? Rarity.Issued :
                r < 99.5f ? Rarity.Hardened :
                Rarity.Legacy;
            return rolled < floor ? floor : rolled;
        }

        public Slot RollSlot() => (Slot)_rng.NextInt(Enum.GetValues(typeof(Slot)).Length);

        /// <summary>Rolls one item of the given shape, spending exactly its budget.</summary>
        public ItemInstance Roll(Slot slot, Rarity rarity, int ilvl)
        {
            int count = ItemRules.AffixCount(rarity);
            var affixes = new List<Affix>(count);
            if (count == 0)
                return new ItemInstance(slot, rarity, ilvl, affixes, NameFor(slot, rarity));

            var pool = AllowedAffixes(slot, rarity);
            float budget = ItemRules.Budget(slot, rarity, ilvl);
            float perAffix = budget / count;

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int pick = _rng.NextInt(pool.Count);
                var kind = pool[pick];
                pool.RemoveAt(pick);                       // no duplicate affixes on one item

                float share = perAffix * (1f + (_rng.NextFloat() * 2f - 1f) * Jitter);
                float magnitude = share / ItemRules.StatWeight(kind);

                // Percent affixes read as whole numbers; flat armour is points. Never roll a zero,
                // because an affix line that does nothing is worse than no line at all.
                magnitude = Math.Max(1f, (float)Math.Round(magnitude));
                if (kind == AffixKind.SecondWind) magnitude = 1f;

                affixes.Add(new Affix(kind, magnitude));
            }

            return new ItemInstance(slot, rarity, ilvl, affixes, NameFor(slot, rarity));
        }

        /// <summary>
        /// Decides whether a source drops at all, and rolls it if so. Rates from the design memo:
        /// runners almost never, and the two thinking archetypes always, because those are the
        /// units the playtest showed the owner actually hunting.
        /// </summary>
        public ItemInstance? TryDrop(DropSource source, int missionTier, int waveIndex)
        {
            int ilvl = ItemRules.ItemLevel(missionTier, waveIndex);
            switch (source)
            {
                case DropSource.RunnerKill:
                    if (_rng.NextFloat() >= 0.0025f) return null;
                    return Roll(RollSlot(), RollRarity(), ilvl);

                case DropSource.SapperKill:
                    return Roll(RollSlot(), RollRarity(Rarity.Issued), ilvl);

                case DropSource.SpitterKill:
                    return Roll(RollSlot(), RollRarity(Rarity.Serviceable), ilvl);

                case DropSource.WaveClear:
                    var floor = (Rarity)Math.Min(waveIndex, (int)Rarity.Hardened);
                    return Roll(RollSlot(), RollRarity(floor), ilvl);

                case DropSource.MissionReward:
                    return Roll(RollSlot(), RollRarity(Rarity.Issued), ilvl + 2);

                default:
                    return null;
            }
        }

        private static List<AffixKind> AllowedAffixes(Slot slot, Rarity rarity)
        {
            var list = new List<AffixKind>();
            foreach (AffixKind kind in Enum.GetValues(typeof(AffixKind)))
            {
                if (!ItemRules.CanRoll(slot, kind)) continue;
                // Second Wind is the one effect that does not scale, so it is the one effect
                // reserved for the top rarity. Everything else is available everywhere.
                if (kind == AffixKind.SecondWind && rarity != Rarity.Legacy) continue;
                list.Add(kind);
            }
            return list;
        }

        private static string NameFor(Slot slot, Rarity rarity)
        {
            string noun = slot switch
            {
                Slot.Weapon => "Light Machine Gun",
                Slot.Vest => "Plate Carrier",
                Slot.Helm => "Bump Helmet",
                Slot.Gloves => "Work Gloves",
                Slot.Boots => "Field Boots",
                Slot.CharmA or Slot.CharmB => "Pocket Charm",
                Slot.DogTag => "Dog Tag",
                _ => slot.ToString(),
            };
            string adjective = rarity switch
            {
                Rarity.Scavenged => "Scavenged",
                Rarity.Serviceable => "Serviceable",
                Rarity.Issued => "Issued",
                Rarity.Hardened => "Hardened",
                Rarity.Legacy => "Legacy",
                _ => string.Empty,
            };
            return $"{adjective} {noun}";
        }
    }
}
