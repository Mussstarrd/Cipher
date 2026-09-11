#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Match;

namespace Cipher.Game.Progression
{
    /// <summary>
    /// The three build paths (docs/design/progression-and-campaign.md section 4). One hero, three
    /// paths, because three heroes means three weapon feels, three animation sets and three balance
    /// passes, while three paths on one body is three data tables.
    /// </summary>
    public enum Path
    {
        /// <summary>You are the damage. Gun output and uptime.</summary>
        Trigger,
        /// <summary>You delete lanes. The airstrike gets wider, harder and safer to stand near.</summary>
        Ordnance,
        /// <summary>The kit is the weapon. Emplacements, reach and repair.</summary>
        Doctrine,
    }

    /// <summary>
    /// One upgrade offered at a wave clear. Deliberately NOT a Bloons-style tree for v0: a tree
    /// needs roughly forty-five nodes, full gamepad tree navigation (an unproven UX here) and
    /// respec code, while pick-one-of-three needs a row of cards and the A button. It also lands
    /// the reward exactly when the player is deciding whether to take another wave, which is the
    /// decision the whole scan cycle is built around.
    /// </summary>
    public sealed class Improvisation
    {
        public string Id { get; }
        public string Name { get; }
        public Path Path { get; }
        public string Text { get; }
        public bool IsCapstone { get; }
        private readonly Action<StatBlock> _apply;

        public Improvisation(string id, string name, Path path, string text,
                             Action<StatBlock> apply, bool isCapstone = false)
        {
            Id = id; Name = name; Path = path; Text = text;
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            IsCapstone = isCapstone;
        }

        public void ApplyTo(StatBlock block) => _apply(block);

        public override string ToString() => $"{Name} ({Path}){(IsCapstone ? " *" : "")}";
    }

    /// <summary>The card pool. Data, not code: balance lives in this one list.</summary>
    public static class ImprovisationCatalogue
    {
        // BALANCE, owner 2026-09-11: "The in-between waves boosts are too powerful because I'm
        // still winning the level just with a single turret."
        //
        // A pick-one-of-three at every wave clear COMPOUNDS. Five picks of +20% turret damage is
        // not +100%, it is a different game, and the wave table never sees it coming. Every
        // percentage in this deck was halved. The cards should tilt a run, not decide it.

        public static IReadOnlyList<Improvisation> All { get; } = Build();

        private static List<Improvisation> Build()
        {
            var list = new List<Improvisation>();

            void Add(string id, string name, Path path, string text, Action<StatBlock> apply,
                     bool capstone = false)
                => list.Add(new Improvisation(id, name, path, text, apply, capstone));

            // ---- Trigger: the gun ----
            Add("trg-zeroed", "Zeroed In", Path.Trigger, "+8% gun damage",
                b => b.AddPercent(StatKind.GunDamage, 0.075f));
            Add("trg-discipline", "Trigger Discipline", Path.Trigger, "+6% fire rate",
                b => b.AddPercent(StatKind.FireRate, 0.060f));
            Add("trg-handload", "Hand-Loads", Path.Trigger, "+11% gun damage",
                b => b.AddPercent(StatKind.GunDamage, 0.110f));
            Add("trg-followthrough", "Follow Through", Path.Trigger, "+5% gun damage, +4% fire rate",
                b => b.AddPercent(StatKind.GunDamage, 0.050f).AddPercent(StatKind.FireRate, 0.040f));
            Add("trg-lightkit", "Light Kit", Path.Trigger, "+5% move speed, +3% fire rate",
                b => b.AddPercent(StatKind.MoveSpeed, 0.050f).AddPercent(StatKind.FireRate, 0.030f));
            Add("trg-scrounge", "Brass Discipline", Path.Trigger, "+12% cash per kill",
                b => b.AddPercent(StatKind.CashPerKill, 0.125f));
            Add("trg-barrel", "Spare Barrel", Path.Trigger, "+9% fire rate",
                b => b.AddPercent(StatKind.FireRate, 0.090f));
            Add("trg-capstone", "Cyclic Rate", Path.Trigger,
                "CAPSTONE  +15% fire rate and +8% gun damage",
                b => b.AddPercent(StatKind.FireRate, 0.150f).AddPercent(StatKind.GunDamage, 0.075f),
                capstone: true);

            // ---- Ordnance: the strike ----
            Add("ord-shortfuse", "Short Fuse", Path.Ordnance, "-9% airstrike cooldown",
                b => b.AddPercent(StatKind.AirstrikeCooldown, 0.090f));
            Add("ord-wide", "Wide Pattern", Path.Ordnance, "+12% blast radius",
                b => b.AddPercent(StatKind.AirstrikeRadius, 0.125f));
            Add("ord-thermite", "Thermite", Path.Ordnance, "+15% airstrike damage",
                b => b.AddPercent(StatKind.AirstrikeDamage, 0.150f));
            Add("ord-sandbagged", "Sandbagged", Path.Ordnance, "-30% self-damage from your own strike",
                b => b.AddPercent(StatKind.AirstrikeSelfDamage, 0.300f));
            Add("ord-coldblooded", "Cold-Blooded", Path.Ordnance, "-6% airstrike cooldown, +5% blast radius",
                b => b.AddPercent(StatKind.AirstrikeCooldown, 0.060f)
                      .AddPercent(StatKind.AirstrikeRadius, 0.050f));
            Add("ord-hardened", "Dug In", Path.Ordnance, "+8% max health, +2 armour",
                b => b.AddPercent(StatKind.MaxHealth, 0.075f).AddFlat(StatKind.Armour, 2f));
            Add("ord-standoff", "Stand Off", Path.Ordnance, "+10% airstrike damage, +4% move speed",
                b => b.AddPercent(StatKind.AirstrikeDamage, 0.100f).AddPercent(StatKind.MoveSpeed, 0.040f));
            Add("ord-capstone", "Danger Close", Path.Ordnance,
                "CAPSTONE  immune to your own strike, -10% cooldown",
                b => b.AddPercent(StatKind.AirstrikeSelfDamage, 0.500f)
                      .AddPercent(StatKind.AirstrikeCooldown, 0.100f),
                capstone: true);

            // ---- Doctrine: the emplacements ----
            Add("doc-overwatch", "Overwatch", Path.Doctrine, "+10% turret damage",
                b => b.AddPercent(StatKind.TurretDamage, 0.100f));
            Add("doc-reach", "Long Lanes", Path.Doctrine, "+9% turret range",
                b => b.AddPercent(StatKind.TurretRange, 0.090f));
            Add("doc-welds", "Fresh Welds", Path.Doctrine, "+18% repair speed",
                b => b.AddPercent(StatKind.RepairSpeed, 0.175f));
            Add("doc-supply", "Supply Line", Path.Doctrine, "+10% cash per kill",
                b => b.AddPercent(StatKind.CashPerKill, 0.100f));
            Add("doc-interlock", "Interlocking Fire", Path.Doctrine, "+6% turret damage, +5% turret range",
                b => b.AddPercent(StatKind.TurretDamage, 0.060f).AddPercent(StatKind.TurretRange, 0.050f));
            Add("doc-standfast", "Stand Fast", Path.Doctrine, "+10% max health, +12% repair speed",
                b => b.AddPercent(StatKind.MaxHealth, 0.100f).AddPercent(StatKind.RepairSpeed, 0.125f));
            Add("doc-plated", "Scrap Plate", Path.Doctrine, "+4 armour",
                b => b.AddFlat(StatKind.Armour, 4f));
            Add("doc-capstone", "Fields of Fire", Path.Doctrine,
                "CAPSTONE  +15% turret damage and +10% turret range",
                b => b.AddPercent(StatKind.TurretDamage, 0.150f).AddPercent(StatKind.TurretRange, 0.100f),
                capstone: true);

            return list;
        }
    }

    /// <summary>
    /// Runs the pick-one-of-three. Seeded, so an offer is reproducible.
    ///
    /// Commitment without a tree: taking three cards of one path unlocks that path's capstone into
    /// the pool. The player feels a build forming without ever navigating a node graph on a gamepad.
    /// </summary>
    public sealed class ImprovisationDeck
    {
        public const int OfferSize = 3;
        /// <summary>Cards of one path needed before its capstone can appear.</summary>
        public const int CapstoneThreshold = 3;

        private readonly List<Improvisation> _pool;
        private readonly List<Improvisation> _taken = new List<Improvisation>();
        private readonly int[] _pathCounts = new int[Enum.GetValues(typeof(Path)).Length];
        private XorShift64 _rng;
        private List<Improvisation> _offer = new List<Improvisation>();

        public ImprovisationDeck(ulong seed, IReadOnlyList<Improvisation>? catalogue = null)
        {
            _rng = new XorShift64(seed);
            _pool = new List<Improvisation>(catalogue ?? ImprovisationCatalogue.All);
        }

        public IReadOnlyList<Improvisation> CurrentOffer => _offer;
        public IReadOnlyList<Improvisation> Taken => _taken;
        public bool HasOffer => _offer.Count > 0;

        public int CountFor(Path path) => _pathCounts[(int)path];

        /// <summary>The path the player is actually committed to, or null while it is still even.</summary>
        public Path? DominantPath()
        {
            int best = -1, bestCount = 0;
            bool tie = false;
            for (int i = 0; i < _pathCounts.Length; i++)
            {
                if (_pathCounts[i] > bestCount) { best = i; bestCount = _pathCounts[i]; tie = false; }
                else if (_pathCounts[i] == bestCount && bestCount > 0) tie = true;
            }
            return (best < 0 || bestCount == 0 || tie) ? (Path?)null : (Path)best;
        }

        public bool CapstoneUnlocked(Path path) => _pathCounts[(int)path] >= CapstoneThreshold;

        /// <summary>
        /// Deals a fresh offer. Capstones only appear once their path is committed to, and a
        /// capstone never crowds out the whole row.
        /// </summary>
        public IReadOnlyList<Improvisation> Deal()
        {
            var eligible = new List<Improvisation>();
            for (int i = 0; i < _pool.Count; i++)
            {
                var card = _pool[i];
                if (card.IsCapstone && !CapstoneUnlocked(card.Path)) continue;
                eligible.Add(card);
            }

            var offer = new List<Improvisation>(OfferSize);
            int capstonesOffered = 0;
            while (offer.Count < OfferSize && eligible.Count > 0)
            {
                int pick = _rng.NextInt(eligible.Count);
                var card = eligible[pick];
                eligible.RemoveAt(pick);
                if (card.IsCapstone)
                {
                    if (capstonesOffered >= 1) continue;   // at most one capstone per row
                    capstonesOffered++;
                }
                offer.Add(card);
            }

            _offer = offer;
            return _offer;
        }

        /// <summary>Takes one card from the current offer by index. Clears the offer.</summary>
        public Improvisation? Take(int offerIndex)
        {
            if (offerIndex < 0 || offerIndex >= _offer.Count) return null;
            var card = _offer[offerIndex];

            _pool.Remove(card);                 // each card is offered once per run
            _taken.Add(card);
            _pathCounts[(int)card.Path]++;
            _offer = new List<Improvisation>();
            return card;
        }

        /// <summary>Declines the offer. Rare, but a player holding out for a capstone may.</summary>
        public void Skip() => _offer = new List<Improvisation>();

        /// <summary>Everything taken so far, totalled.</summary>
        public StatBlock TotalStats()
        {
            var block = new StatBlock();
            for (int i = 0; i < _taken.Count; i++) _taken[i].ApplyTo(block);
            return block;
        }
    }
}
