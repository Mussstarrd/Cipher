#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game.Progression
{
    /// <summary>
    /// What a thing costs to carry. Owner's call, 2026-09-11:
    ///
    ///   "At end of level it's weight/size based retrieval. The exit is a truck and I can bring
    ///    whatll fit in it which is a threshold you can figure out with size and weight parameters"
    ///
    /// Two numbers, not one, because they constrain differently and that is the whole point: a
    /// sentry gun is heavy and compact, a barricade panel is light and enormous. A bed full of
    /// panels weighs nothing and fits nothing else in.
    /// </summary>
    public readonly struct Haulage
    {
        /// <summary>Kilograms.</summary>
        public readonly float Weight;
        /// <summary>Cubic metres of bed space.</summary>
        public readonly float Volume;

        public Haulage(float weight, float volume)
        {
            Weight = Math.Max(0f, weight);
            Volume = Math.Max(0f, volume);
        }

        public static Haulage operator +(Haulage a, Haulage b)
            => new Haulage(a.Weight + b.Weight, a.Volume + b.Volume);

        public override string ToString() => $"{Weight:F0} kg / {Volume:F1} m3";
    }

    /// <summary>Anything that can go on the truck.</summary>
    public interface IHaulable
    {
        string HaulName { get; }
        Haulage Haulage { get; }
        /// <summary>Cash value if it gets home. Gear pays Scrip; emplacements pay cash.</summary>
        int RecoveredValue { get; }
    }

    /// <summary>An emplacement being recovered at extraction.</summary>
    public sealed class SalvagedEmplacement : IHaulable
    {
        public string HaulName { get; }
        public Haulage Haulage { get; }
        public int RecoveredValue { get; }

        public SalvagedEmplacement(string name, Haulage haulage, int value)
        {
            HaulName = name; Haulage = haulage; RecoveredValue = Math.Max(0, value);
        }
    }

    /// <summary>Gear being carried out. Light and small, so gear is almost never the thing you cut.</summary>
    public sealed class HauledItem : IHaulable
    {
        public ItemInstance Item { get; }
        public string HaulName => Item.Name;
        public Haulage Haulage { get; }
        public int RecoveredValue => Item.ScripValue;

        public HauledItem(ItemInstance item)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Haulage = TruckLoad.HaulageFor(item.Slot);
        }
    }

    public enum LoadOutcome { Loaded, TooHeavy, TooBulky, AlreadyLoaded }

    /// <summary>
    /// The truck bed. A hard threshold on both weight and volume, so extraction becomes a packing
    /// problem rather than a timer.
    ///
    /// This does not replace the pack-up window from ADR-005, it sits inside it. Time decides how
    /// many emplacements you can physically get to and unbolt. The truck decides which of them are
    /// worth the bed space. Four light rotors or one heavy sentry is a genuine decision; a clock
    /// alone never asked that question.
    /// </summary>
    public sealed class TruckLoad
    {
        public float MaxWeight { get; }
        public float MaxVolume { get; }

        private readonly List<IHaulable> _loaded = new List<IHaulable>();

        public TruckLoad(float maxWeight = 1200f, float maxVolume = 9f)
        {
            MaxWeight = Math.Max(1f, maxWeight);
            MaxVolume = Math.Max(0.1f, maxVolume);
        }

        public IReadOnlyList<IHaulable> Loaded => _loaded;
        public float Weight { get; private set; }
        public float Volume { get; private set; }

        public float WeightFraction => Weight / MaxWeight;
        public float VolumeFraction => Volume / MaxVolume;

        /// <summary>Whichever limit is closest to full. What the gauge should show.</summary>
        public float FullnessFraction => Math.Max(WeightFraction, VolumeFraction);

        public int TotalValue
        {
            get
            {
                int v = 0;
                for (int i = 0; i < _loaded.Count; i++) v += _loaded[i].RecoveredValue;
                return v;
            }
        }

        /// <summary>Already on the truck? Cheaper and clearer than pulling LINQ into the HUD.</summary>
        public bool IsLoaded(IHaulable thing)
        {
            if (thing == null) return false;
            for (int i = 0; i < _loaded.Count; i++) if (ReferenceEquals(_loaded[i], thing)) return true;
            return false;
        }

        public bool Fits(IHaulable thing)
            => thing != null
               && Weight + thing.Haulage.Weight <= MaxWeight
               && Volume + thing.Haulage.Volume <= MaxVolume;

        public LoadOutcome TryLoad(IHaulable thing)
        {
            if (thing == null) throw new ArgumentNullException(nameof(thing));
            if (IsLoaded(thing)) return LoadOutcome.AlreadyLoaded;

            if (Weight + thing.Haulage.Weight > MaxWeight) return LoadOutcome.TooHeavy;
            if (Volume + thing.Haulage.Volume > MaxVolume) return LoadOutcome.TooBulky;

            _loaded.Add(thing);
            Weight += thing.Haulage.Weight;
            Volume += thing.Haulage.Volume;
            return LoadOutcome.Loaded;
        }

        public bool Unload(IHaulable thing)
        {
            if (thing == null || !_loaded.Remove(thing)) return false;
            Weight = Math.Max(0f, Weight - thing.Haulage.Weight);
            Volume = Math.Max(0f, Volume - thing.Haulage.Volume);
            return true;
        }

        /// <summary>
        /// Fills the bed greedily by value density, best first. This is the "load it for me" button,
        /// and it is deliberately only a suggestion: it optimises value per unit carried, which is
        /// not always what the player wants next mission.
        /// </summary>
        public int AutoLoad(IEnumerable<IHaulable> candidates)
        {
            if (candidates == null) return 0;

            var sorted = new List<IHaulable>(candidates);
            sorted.Sort((a, b) => Density(b).CompareTo(Density(a)));

            int loaded = 0;
            foreach (var thing in sorted)
                if (TryLoad(thing) == LoadOutcome.Loaded) loaded++;
            return loaded;
        }

        private static float Density(IHaulable thing)
        {
            // Cost is whichever limit the thing eats more of, normalised so neither dominates.
            float cost = Math.Max(thing.Haulage.Weight / 100f, thing.Haulage.Volume);
            return cost <= 0.0001f ? float.MaxValue : thing.RecoveredValue / cost;
        }

        /// <summary>
        /// Gear is small and light on purpose: the truck decision should be about emplacements.
        /// Nobody should ever have to leave a helmet behind.
        /// </summary>
        public static Haulage HaulageFor(Slot slot) => slot switch
        {
            Slot.Weapon => new Haulage(8f, 0.06f),
            Slot.Vest => new Haulage(9f, 0.05f),
            Slot.Helm => new Haulage(1.5f, 0.02f),
            Slot.Gloves => new Haulage(0.4f, 0.005f),
            Slot.Boots => new Haulage(1.8f, 0.02f),
            Slot.CharmA or Slot.CharmB => new Haulage(0.1f, 0.001f),
            Slot.DogTag => new Haulage(0.02f, 0.0005f),
            _ => new Haulage(1f, 0.01f),
        };
    }
}
