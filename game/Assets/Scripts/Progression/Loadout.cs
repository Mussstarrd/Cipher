#nullable enable
using System;
using Cipher.Game.Hero;

namespace Cipher.Game.Progression
{
    /// <summary>
    /// The one place that answers "what are my numbers right now". Gear plus upgrades in, an
    /// effective <see cref="HeroConfig"/> out.
    ///
    /// It rebuilds a whole config rather than patching the hero, which keeps HeroModel ignorant of
    /// progression entirely: the hero reads a config, and something else decides what that config
    /// says. That is the seam that lets gear, upgrades, difficulty modifiers and co-op handicaps
    /// all arrive later without touching combat code.
    /// </summary>
    public sealed class Loadout
    {
        private readonly HeroConfig _base;

        public Inventory Inventory { get; }
        public ImprovisationDeck Deck { get; }

        /// <summary>Permanent progression. Survives the mission; the deck does not.</summary>
        public SkillState Skills { get; }

        /// <summary>The stats in force, recomputed whenever gear or upgrades change.</summary>
        public StatBlock Stats { get; private set; } = new StatBlock();

        /// <summary>The config the hero should be running. Replaced on every recompute.</summary>
        public HeroConfig Effective { get; private set; }

        /// <summary>Cash multiplier from Scrounger affixes and supply cards.</summary>
        public float CashMultiplier => 1f + Stats.Percent(StatKind.CashPerKill);

        /// <summary>Turret damage and range multipliers, applied by the game layer to emplacements.</summary>
        public float TurretDamageMultiplier => 1f + Stats.Percent(StatKind.TurretDamage);
        public float TurretRangeMultiplier => 1f + Stats.Percent(StatKind.TurretRange);
        public float RepairSpeedMultiplier => 1f + Stats.Percent(StatKind.RepairSpeed);

        /// <summary>Flat damage removed from each contact tick, from Plated and Scrap Plate.</summary>
        public float Armour => Stats.Flat(StatKind.Armour);

        /// <summary>True while a Legacy vest can still save the player this wave.</summary>
        public bool HasSecondWind => Inventory.HasEquippedAffix(AffixKind.SecondWind);

        public Loadout(HeroConfig baseConfig, Inventory? inventory = null,
                       ImprovisationDeck? deck = null, SkillState? skills = null)
        {
            _base = baseConfig ?? throw new ArgumentNullException(nameof(baseConfig));
            Inventory = inventory ?? new Inventory();
            Deck = deck ?? new ImprovisationDeck(1);
            Skills = skills ?? new SkillState();
            Effective = _base;
            Recompute();
        }

        /// <summary>Call after any equip, pickup or card take.</summary>
        public HeroConfig Recompute()
        {
            // Three sources, one total: permanent tree, run-scoped gear, mission-scoped cards.
            var stats = new StatBlock();
            stats.Add(Skills.TotalStats());
            stats.Add(Inventory.EquippedStats());
            stats.Add(Deck.TotalStats());
            Stats = stats;

            var c = new HeroConfig
            {
                // Untouched by progression: geometry and timing the player has learned.
                GunRange = _base.GunRange,
                GunHitRadius = _base.GunHitRadius,
                ContactRadius = _base.ContactRadius,
                ContactAgentCap = _base.ContactAgentCap,
                AirstrikeMinRange = _base.AirstrikeMinRange,
                AirstrikeMaxRange = _base.AirstrikeMaxRange,
                AirstrikeLineLength = _base.AirstrikeLineLength,
                AirstrikeBombCount = _base.AirstrikeBombCount,
                AirstrikeInboundDelay = _base.AirstrikeInboundDelay,
                AirstrikeBombInterval = _base.AirstrikeBombInterval,
                GunWallDamage = _base.GunWallDamage,
                AirstrikeWallDamage = _base.AirstrikeWallDamage,
                FriendlyWallDamageScale = _base.FriendlyWallDamageScale,
                ContactDamagePerAgentPerSecond = _base.ContactDamagePerAgentPerSecond,

                // Scaled by gear and upgrades.
                MoveSpeed = stats.Apply(StatKind.MoveSpeed, _base.MoveSpeed),
                MaxHealth = stats.Apply(StatKind.MaxHealth, _base.MaxHealth),
                GunDamage = stats.Apply(StatKind.GunDamage, _base.GunDamage),
                AirstrikeBombDamage = stats.Apply(StatKind.AirstrikeDamage, _base.AirstrikeBombDamage),
                AirstrikeBombRadius = stats.Apply(StatKind.AirstrikeRadius, _base.AirstrikeBombRadius),

                // Lower is better for these two, and both are floored so stacking cannot break them.
                FireInterval = stats.ApplyReduction(StatKind.FireRate, _base.FireInterval),
                AirstrikeCooldown = stats.ApplyReduction(StatKind.AirstrikeCooldown, _base.AirstrikeCooldown),

                // Self-damage may legitimately reach zero: that is what the Ordnance capstone buys.
                AirstrikeSelfDamage = Math.Max(
                    0f, _base.AirstrikeSelfDamage * (1f - stats.Percent(StatKind.AirstrikeSelfDamage))),
            };

            Effective = c;
            return c;
        }

        /// <summary>Picks an item up and recomputes if it changed anything worn.</summary>
        public PickupResult Pickup(ItemInstance item)
        {
            var result = Inventory.Pickup(item);
            if (result.Outcome is PickupOutcome.EquippedEmptySlot or PickupOutcome.Upgraded) Recompute();
            return result;
        }

        public bool EquipFromPack(int index)
        {
            if (!Inventory.EquipFromPack(index)) return false;
            Recompute();
            return true;
        }

        public bool Unequip(Slot slot)
        {
            if (!Inventory.Unequip(slot)) return false;
            Recompute();
            return true;
        }

        public Improvisation? TakeImprovisation(int offerIndex)
        {
            var card = Deck.Take(offerIndex);
            if (card != null) Recompute();
            return card;
        }

        /// <summary>Buys a skill node and recomputes if it took.</summary>
        public SpendResult SpendSkillPoint(string nodeId)
        {
            var result = Skills.Spend(nodeId);
            if (result == SpendResult.Ok) Recompute();
            return result;
        }

        /// <summary>Cash for a batch of kills, after Scrounger and supply cards.</summary>
        public int CashForKills(int kills, int cashPerKill)
            => kills <= 0 ? 0 : (int)Math.Round(kills * cashPerKill * CashMultiplier);

        /// <summary>Contact damage after armour. Armour never reduces a tick below a tenth.</summary>
        public float MitigateContact(float rawDamage)
            => Math.Max(rawDamage * 0.1f, rawDamage - Armour);
    }
}
