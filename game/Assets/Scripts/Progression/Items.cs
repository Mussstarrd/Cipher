#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace Cipher.Game.Progression
{
    /// <summary>
    /// Eight slots, per docs/design/progression-and-campaign.md section 3. The Dog Tag is the
    /// premise hook: a named survivor's tag, so losing that person destroys an item.
    /// </summary>
    public enum Slot { Weapon, Vest, Helm, Gloves, Boots, CharmA, CharmB, DogTag }

    /// <summary>
    /// Five rarities. The design memo named these for a kingpin fantasy that ADR-003 retired, so
    /// the names are scavenger-grade now; the multipliers and affix counts are unchanged.
    /// </summary>
    public enum Rarity { Scavenged, Serviceable, Issued, Hardened, Legacy }

    /// <summary>
    /// Affix families. Names come from the veteran's world rather than the old gang vocabulary:
    /// he hand-loads his own ammunition and he plates his own vest.
    /// </summary>
    public enum AffixKind
    {
        /// <summary>+N% gun damage.</summary>
        HandLoaded,
        /// <summary>+N% fire rate.</summary>
        Cyclic,
        /// <summary>+N% max health.</summary>
        Conditioned,
        /// <summary>+N flat armour.</summary>
        Plated,
        /// <summary>+N% cash per kill.</summary>
        Scrounger,
        /// <summary>Reduces airstrike cooldown by N%.</summary>
        ColdBlooded,
        /// <summary>+N% damage for turrets near you.</summary>
        Overwatch,
        /// <summary>+N% move speed.</summary>
        Runner,
        /// <summary>Once per wave, survive a lethal hit at 1 HP. Legacy only, magnitude ignored.</summary>
        SecondWind,
    }

    public readonly struct Affix
    {
        public readonly AffixKind Kind;
        /// <summary>Percent affixes store whole percents (18 means +18%); Plated stores points.</summary>
        public readonly float Magnitude;

        public Affix(AffixKind kind, float magnitude) { Kind = kind; Magnitude = magnitude; }

        public override string ToString() => ItemRules.Describe(this);
    }

    /// <summary>
    /// The numbers behind items, in one place so an item's worth is a single comparable figure.
    /// Budget and weights are taken verbatim from the design memo.
    /// </summary>
    public static class ItemRules
    {
        public static float RarityMultiplier(Rarity r) => r switch
        {
            Rarity.Scavenged => 1.00f,
            Rarity.Serviceable => 1.25f,
            Rarity.Issued => 1.55f,
            Rarity.Hardened => 1.90f,
            Rarity.Legacy => 2.30f,
            _ => 1f,
        };

        /// <summary>Affix count by rarity: 0 / 1 / 2 / 3 / 4.</summary>
        public static int AffixCount(Rarity r) => (int)r;

        public static float SlotWeight(Slot s) => s switch
        {
            Slot.Weapon => 1.00f,
            Slot.Vest => 0.80f,
            Slot.Helm => 0.50f,
            Slot.Gloves => 0.50f,
            Slot.Boots => 0.50f,
            Slot.CharmA => 0.35f,
            Slot.CharmB => 0.35f,
            Slot.DogTag => 0.35f,
            _ => 0.5f,
        };

        /// <summary>How much one point of an affix is worth, so everything trades against one budget.</summary>
        public static float StatWeight(AffixKind a) => a switch
        {
            AffixKind.HandLoaded => 1.0f,
            AffixKind.Cyclic => 1.2f,
            AffixKind.Conditioned => 0.8f,
            AffixKind.Plated => 4.0f,
            AffixKind.Scrounger => 0.5f,
            AffixKind.ColdBlooded => 1.1f,
            AffixKind.Overwatch => 0.7f,
            AffixKind.Runner => 1.0f,
            AffixKind.SecondWind => 12.0f,   // a flat, expensive, non-scaling effect
            _ => 1f,
        };

        /// <summary>Total power an item of this shape is allowed to roll.</summary>
        public static float Budget(Slot slot, Rarity rarity, int ilvl)
            => SlotWeight(slot) * (4f + 1.6f * Math.Max(0, ilvl)) * RarityMultiplier(rarity);

        /// <summary>What an item is actually worth, in the same units as Budget.</summary>
        public static float PowerScore(IReadOnlyList<Affix> affixes)
        {
            float total = 0f;
            for (int i = 0; i < affixes.Count; i++)
                total += affixes[i].Magnitude * StatWeight(affixes[i].Kind);
            return total;
        }

        /// <summary>Sell value, paid in Scrip and never in cash, so gear cannot fund turrets.</summary>
        public static int ScripValue(Rarity rarity, int ilvl)
            => (int)Math.Round(2.5f * Math.Max(1, ilvl) * RarityMultiplier(rarity));

        /// <summary>Item level from where it dropped. Tier 1 wave 3 gives 7.</summary>
        public static int ItemLevel(int missionTier, int waveIndex)
            => 4 * Math.Max(1, missionTier) + Math.Max(0, waveIndex);

        public static bool IsFlat(AffixKind a) => a == AffixKind.Plated || a == AffixKind.SecondWind;

        public static string Describe(Affix affix) => affix.Kind switch
        {
            AffixKind.HandLoaded => $"Hand-Loaded  +{affix.Magnitude:F0}% gun damage",
            AffixKind.Cyclic => $"Cyclic  +{affix.Magnitude:F0}% fire rate",
            AffixKind.Conditioned => $"Conditioned  +{affix.Magnitude:F0}% max health",
            AffixKind.Plated => $"Plated  +{affix.Magnitude:F0} armour",
            AffixKind.Scrounger => $"Scrounger  +{affix.Magnitude:F0}% cash per kill",
            AffixKind.ColdBlooded => $"Cold-Blooded  -{affix.Magnitude:F0}% airstrike cooldown",
            AffixKind.Overwatch => $"Overwatch  +{affix.Magnitude:F0}% nearby turret damage",
            AffixKind.Runner => $"Runner  +{affix.Magnitude:F0}% move speed",
            AffixKind.SecondWind => "Second Wind  survive one lethal hit per wave",
            _ => affix.Kind.ToString(),
        };

        /// <summary>Which affixes may roll on which slot. Keeps a helmet from rolling gun damage.</summary>
        public static bool CanRoll(Slot slot, AffixKind affix) => slot switch
        {
            Slot.Weapon => affix is AffixKind.HandLoaded or AffixKind.Cyclic or AffixKind.ColdBlooded
                                 or AffixKind.Scrounger,
            Slot.Vest => affix is AffixKind.Conditioned or AffixKind.Plated or AffixKind.SecondWind
                               or AffixKind.Scrounger,
            Slot.Helm => affix is AffixKind.Conditioned or AffixKind.Plated or AffixKind.Overwatch,
            Slot.Gloves => affix is AffixKind.Cyclic or AffixKind.HandLoaded or AffixKind.Plated,
            Slot.Boots => affix is AffixKind.Runner or AffixKind.Plated or AffixKind.Conditioned,
            Slot.CharmA or Slot.CharmB => affix is AffixKind.ColdBlooded or AffixKind.Scrounger
                                                or AffixKind.Overwatch or AffixKind.Runner,
            Slot.DogTag => affix is AffixKind.Overwatch or AffixKind.Conditioned or AffixKind.Scrounger,
            _ => true,
        };
    }

    /// <summary>One rolled piece of gear. Immutable once rolled; there is no crafting in v0.</summary>
    public sealed class ItemInstance
    {
        public Slot Slot { get; }
        public Rarity Rarity { get; }
        public int ItemLevel { get; }
        public IReadOnlyList<Affix> Affixes { get; }
        public string Name { get; }

        public ItemInstance(Slot slot, Rarity rarity, int itemLevel, IReadOnlyList<Affix> affixes, string name)
        {
            Slot = slot;
            Rarity = rarity;
            ItemLevel = Math.Max(1, itemLevel);
            Affixes = affixes ?? Array.Empty<Affix>();
            Name = string.IsNullOrWhiteSpace(name) ? slot.ToString() : name;
        }

        public float PowerScore => ItemRules.PowerScore(Affixes);
        public int ScripValue => ItemRules.ScripValue(Rarity, ItemLevel);

        public bool Has(AffixKind kind)
        {
            for (int i = 0; i < Affixes.Count; i++)
                if (Affixes[i].Kind == kind) return true;
            return false;
        }

        /// <summary>Turns the affixes into stat modifiers. This is the only place items become power.</summary>
        public StatBlock ToStats()
        {
            var block = new StatBlock();
            for (int i = 0; i < Affixes.Count; i++)
            {
                var a = Affixes[i];
                float frac = a.Magnitude / 100f;
                switch (a.Kind)
                {
                    case AffixKind.HandLoaded: block.AddPercent(StatKind.GunDamage, frac); break;
                    case AffixKind.Cyclic: block.AddPercent(StatKind.FireRate, frac); break;
                    case AffixKind.Conditioned: block.AddPercent(StatKind.MaxHealth, frac); break;
                    case AffixKind.Plated: block.AddFlat(StatKind.Armour, a.Magnitude); break;
                    case AffixKind.Scrounger: block.AddPercent(StatKind.CashPerKill, frac); break;
                    case AffixKind.ColdBlooded: block.AddPercent(StatKind.AirstrikeCooldown, frac); break;
                    case AffixKind.Overwatch: block.AddPercent(StatKind.TurretDamage, frac); break;
                    case AffixKind.Runner: block.AddPercent(StatKind.MoveSpeed, frac); break;
                    case AffixKind.SecondWind: break;   // handled by the hero, not by a stat
                }
            }
            return block;
        }

        public string Card()
        {
            var sb = new StringBuilder();
            sb.Append(Name).Append("  [").Append(Rarity).Append(" i").Append(ItemLevel).Append(']');
            sb.Append("  weight ").Append(PowerScore.ToString("F1"));
            for (int i = 0; i < Affixes.Count; i++) sb.Append('\n').Append("  ").Append(Affixes[i]);
            return sb.ToString();
        }

        public override string ToString() => $"{Name} ({Rarity} i{ItemLevel}, {PowerScore:F1})";
    }
}
