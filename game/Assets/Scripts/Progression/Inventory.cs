#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game.Progression
{
    /// <summary>What happened when an item was picked up.</summary>
    public enum PickupOutcome
    {
        /// <summary>Nothing was in the slot, so it went straight on.</summary>
        EquippedEmptySlot,
        /// <summary>It beat what was equipped and was swapped in; the old piece went to the pack.</summary>
        Upgraded,
        /// <summary>Worse than equipped but worth carrying out.</summary>
        Stowed,
        /// <summary>Beneath the junk threshold; auto-scrapped for Scrip on the spot.</summary>
        AutoScrapped,
        /// <summary>Pack was full and it was not an upgrade.</summary>
        PackFull,
    }

    public readonly struct PickupResult
    {
        public readonly PickupOutcome Outcome;
        public readonly ItemInstance Item;
        public readonly ItemInstance? Replaced;
        public readonly int ScripGained;

        public PickupResult(PickupOutcome outcome, ItemInstance item, ItemInstance? replaced, int scrip)
        {
            Outcome = outcome; Item = item; Replaced = replaced; ScripGained = scrip;
        }

        public bool WentToInventory => Outcome is PickupOutcome.EquippedEmptySlot
                                               or PickupOutcome.Upgraded or PickupOutcome.Stowed;
    }

    /// <summary>
    /// Eight equipped slots plus a pack. Gear survives a lost mission on purpose
    /// (docs/design/progression-and-campaign.md section 2): missions here are ten to fifteen
    /// minutes of hand-built defence, and losing wave five with nothing to show would read as theft.
    ///
    /// The auto-scrap filter exists so that picking up loot never becomes admin. Anything clearly
    /// worse than what you are wearing turns into Scrip without asking.
    /// </summary>
    public sealed class Inventory
    {
        /// <summary>Items below this fraction of the equipped piece are scrapped on pickup.</summary>
        public float JunkThreshold { get; set; } = 0.6f;

        public int PackCapacity { get; }
        public int Scrip { get; private set; }

        private readonly ItemInstance?[] _equipped =
            new ItemInstance?[Enum.GetValues(typeof(Slot)).Length];
        private readonly List<ItemInstance> _pack;

        public Inventory(int packCapacity = 24, int startingScrip = 0)
        {
            PackCapacity = Math.Max(1, packCapacity);
            Scrip = Math.Max(0, startingScrip);
            _pack = new List<ItemInstance>(PackCapacity);
        }

        public IReadOnlyList<ItemInstance> Pack => _pack;
        public ItemInstance? Equipped(Slot slot) => _equipped[(int)slot];
        public bool PackIsFull => _pack.Count >= PackCapacity;

        public int EquippedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _equipped.Length; i++) if (_equipped[i] != null) n++;
                return n;
            }
        }

        /// <summary>Everything worn, totalled. This is what the hero actually feels.</summary>
        public StatBlock EquippedStats()
        {
            var block = new StatBlock();
            for (int i = 0; i < _equipped.Length; i++)
            {
                var item = _equipped[i];
                if (item != null) block.Add(item.ToStats());
            }
            return block;
        }

        public bool HasEquippedAffix(AffixKind kind)
        {
            for (int i = 0; i < _equipped.Length; i++)
                if (_equipped[i]?.Has(kind) == true) return true;
            return false;
        }

        /// <summary>
        /// Picks an item up mid-fight and decides what happens to it without stopping play.
        /// Strictly better goes straight on; clearly worse becomes Scrip; the ambiguous middle
        /// goes in the pack for the player to judge later.
        /// </summary>
        public PickupResult Pickup(ItemInstance item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var current = _equipped[(int)item.Slot];
            if (current == null)
            {
                _equipped[(int)item.Slot] = item;
                return new PickupResult(PickupOutcome.EquippedEmptySlot, item, null, 0);
            }

            if (item.PowerScore > current.PowerScore)
            {
                _equipped[(int)item.Slot] = item;
                if (_pack.Count < PackCapacity)
                {
                    _pack.Add(current);
                    return new PickupResult(PickupOutcome.Upgraded, item, current, 0);
                }
                // Pack is full, so the piece coming off is the one that gets scrapped.
                Scrip += current.ScripValue;
                return new PickupResult(PickupOutcome.Upgraded, item, current, current.ScripValue);
            }

            if (item.PowerScore < current.PowerScore * JunkThreshold)
            {
                Scrip += item.ScripValue;
                return new PickupResult(PickupOutcome.AutoScrapped, item, null, item.ScripValue);
            }

            if (PackIsFull) return new PickupResult(PickupOutcome.PackFull, item, null, 0);

            _pack.Add(item);
            return new PickupResult(PickupOutcome.Stowed, item, null, 0);
        }

        /// <summary>Equips a pack item, sending whatever it replaces back to the pack.</summary>
        public bool EquipFromPack(int packIndex)
        {
            if (packIndex < 0 || packIndex >= _pack.Count) return false;
            var item = _pack[packIndex];
            _pack.RemoveAt(packIndex);

            var current = _equipped[(int)item.Slot];
            _equipped[(int)item.Slot] = item;
            if (current != null) _pack.Add(current);
            return true;
        }

        public bool Unequip(Slot slot)
        {
            var current = _equipped[(int)slot];
            if (current == null || PackIsFull) return false;
            _equipped[(int)slot] = null;
            _pack.Add(current);
            return true;
        }

        public int SellFromPack(int packIndex)
        {
            if (packIndex < 0 || packIndex >= _pack.Count) return 0;
            int value = _pack[packIndex].ScripValue;
            _pack.RemoveAt(packIndex);
            Scrip += value;
            return value;
        }

        /// <summary>Sells everything in the pack. The safe-zone convenience button.</summary>
        public int SellPack()
        {
            int total = 0;
            for (int i = 0; i < _pack.Count; i++) total += _pack[i].ScripValue;
            _pack.Clear();
            Scrip += total;
            return total;
        }

        public bool SpendScrip(int amount)
        {
            if (amount < 0 || amount > Scrip) return false;
            Scrip -= amount;
            return true;
        }

        /// <summary>
        /// How much better the pack item would be than what is worn, as a weight delta. The
        /// single number the comparison UI shows.
        /// </summary>
        public float UpgradeDelta(ItemInstance item)
        {
            if (item == null) return 0f;
            var current = _equipped[(int)item.Slot];
            return item.PowerScore - (current?.PowerScore ?? 0f);
        }
    }
}
