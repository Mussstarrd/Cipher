#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game.Progression
{
    /// <summary>
    /// Levels and a permanent skill tree. Owner's call, 2026-09-11: "Don't forget level ups and
    /// skill tree and shit."
    ///
    /// This does NOT replace the pick-one-of-three Improvisations, it sits above them. The split is
    /// the Hades split, and it is the reason both can exist without competing:
    ///
    ///   Improvisations  one mission, discarded at extraction, no save file, dealt at a wave clear.
    ///   Skill tree      permanent, spent between missions, the thing that makes mission 9 easier
    ///                   than mission 1 even though the horde got worse.
    ///
    /// A retreating campaign needs the second one badly. You lose ground in every mission by design,
    /// so the player has to be able to point at something that only ever goes up.
    /// </summary>
    public sealed class SkillNode
    {
        public string Id { get; }
        public string Name { get; }
        public Path Path { get; }
        public string Text { get; }
        /// <summary>Node that must be bought first, or null for a root.</summary>
        public string? Requires { get; }
        /// <summary>Skill points this node costs. Deeper nodes cost more.</summary>
        public int Cost { get; }
        /// <summary>How many times it can be bought. Small nodes stack; keystones do not.</summary>
        public int MaxRank { get; }
        public bool IsKeystone { get; }

        private readonly Action<StatBlock> _apply;

        public SkillNode(string id, string name, Path path, string text, Action<StatBlock> apply,
                         string? requires = null, int cost = 1, int maxRank = 1, bool isKeystone = false)
        {
            Id = id; Name = name; Path = path; Text = text;
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            Requires = requires;
            Cost = Math.Max(1, cost);
            MaxRank = Math.Max(1, maxRank);
            IsKeystone = isKeystone;
        }

        public void ApplyTo(StatBlock block, int rank)
        {
            for (int i = 0; i < rank; i++) _apply(block);
        }

        public override string ToString() => $"{Name} ({Path}, {Cost}pt)";
    }

    /// <summary>The experience curve. Deliberately shallow: this is a campaign, not a grind.</summary>
    public static class LevelCurve
    {
        public const int MaxLevel = 30;

        /// <summary>Total experience needed to reach a level. Quadratic, so later levels take longer.</summary>
        public static int TotalXpFor(int level)
        {
            if (level <= 1) return 0;
            int l = Math.Min(level, MaxLevel) - 1;
            return 120 * l + 40 * l * l;
        }

        public static int XpToNext(int level)
            => level >= MaxLevel ? 0 : TotalXpFor(level + 1) - TotalXpFor(level);

        /// <summary>Level implied by a total experience figure.</summary>
        public static int LevelFor(int totalXp)
        {
            int level = 1;
            while (level < MaxLevel && totalXp >= TotalXpFor(level + 1)) level++;
            return level;
        }

        /// <summary>Skill points granted at a level. Every level gives one; every fifth gives two.</summary>
        public static int PointsForLevel(int level) => level % 5 == 0 ? 2 : 1;

        /// <summary>Experience for a kill, by what it was.</summary>
        public static int XpForKill(bool wasSapper, bool wasSpitter)
            => wasSapper ? 40 : wasSpitter ? 25 : 2;

        public static int XpForWaveCleared(int waveNumber) => 60 * Math.Max(1, waveNumber);

        /// <summary>
        /// Extraction pays, and it pays more the longer you held. This is what stops the optimal
        /// play from being "call the last wave immediately every time".
        /// </summary>
        public static int XpForExtraction(int wavesHeld) => 150 + 90 * Math.Max(0, wavesHeld);
    }

    public static class SkillCatalogue
    {
        public static IReadOnlyList<SkillNode> All { get; } = Build();

        private static List<SkillNode> Build()
        {
            var l = new List<SkillNode>();
            void N(string id, string name, Path p, string text, Action<StatBlock> a,
                   string? req = null, int cost = 1, int maxRank = 1, bool key = false)
                => l.Add(new SkillNode(id, name, p, text, a, req, cost, maxRank, key));

            // ---- Trigger ----
            N("t-marks", "Marksmanship", Path.Trigger, "+4% gun damage per rank",
              b => b.AddPercent(StatKind.GunDamage, 0.04f), null, 1, 5);
            N("t-cadence", "Cadence", Path.Trigger, "+3% fire rate per rank",
              b => b.AddPercent(StatKind.FireRate, 0.03f), "t-marks", 1, 5);
            N("t-reach", "Long Barrel", Path.Trigger, "+10% gun damage",
              b => b.AddPercent(StatKind.GunDamage, 0.10f), "t-cadence", 2);
            N("t-key", "Steady Hands", Path.Trigger,
              "KEYSTONE  +15% gun damage and +15% fire rate",
              b => b.AddPercent(StatKind.GunDamage, 0.15f).AddPercent(StatKind.FireRate, 0.15f),
              "t-reach", 3, 1, key: true);

            // ---- Ordnance ----
            N("o-fuse", "Fuse Work", Path.Ordnance, "-4% airstrike cooldown per rank",
              b => b.AddPercent(StatKind.AirstrikeCooldown, 0.04f), null, 1, 5);
            N("o-pattern", "Pattern Work", Path.Ordnance, "+5% blast radius per rank",
              b => b.AddPercent(StatKind.AirstrikeRadius, 0.05f), "o-fuse", 1, 4);
            N("o-charge", "Heavier Charges", Path.Ordnance, "+12% airstrike damage",
              b => b.AddPercent(StatKind.AirstrikeDamage, 0.12f), "o-pattern", 2);
            N("o-key", "Danger Close", Path.Ordnance,
              "KEYSTONE  -50% self-damage and -10% cooldown",
              b => b.AddPercent(StatKind.AirstrikeSelfDamage, 0.50f)
                    .AddPercent(StatKind.AirstrikeCooldown, 0.10f),
              "o-charge", 3, 1, key: true);

            // ---- Doctrine ----
            N("d-lanes", "Fields of Fire", Path.Doctrine, "+4% turret damage per rank",
              b => b.AddPercent(StatKind.TurretDamage, 0.04f), null, 1, 5);
            N("d-reach", "Long Lanes", Path.Doctrine, "+4% turret range per rank",
              b => b.AddPercent(StatKind.TurretRange, 0.04f), "d-lanes", 1, 4);
            N("d-welds", "Fresh Welds", Path.Doctrine, "+15% repair speed",
              b => b.AddPercent(StatKind.RepairSpeed, 0.15f), "d-reach", 2);
            N("d-key", "Interlock", Path.Doctrine,
              "KEYSTONE  +18% turret damage and +12% turret range",
              b => b.AddPercent(StatKind.TurretDamage, 0.18f).AddPercent(StatKind.TurretRange, 0.12f),
              "d-welds", 3, 1, key: true);

            // ---- Survival, open to everyone ----
            N("s-cond", "Conditioning", Path.Trigger, "+5% max health per rank",
              b => b.AddPercent(StatKind.MaxHealth, 0.05f), null, 1, 5);
            N("s-plate", "Scrap Plate", Path.Doctrine, "+1 armour per rank",
              b => b.AddFlat(StatKind.Armour, 1f), null, 1, 5);
            N("s-legs", "Road Legs", Path.Ordnance, "+3% move speed per rank",
              b => b.AddPercent(StatKind.MoveSpeed, 0.03f), null, 1, 4);

            return l;
        }

        public static SkillNode? Find(string id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].Id == id) return All[i];
            return null;
        }
    }

    public enum SpendResult { Ok, UnknownNode, NotEnoughPoints, PrerequisiteMissing, AtMaxRank }

    /// <summary>
    /// The player's permanent progression: experience, level, unspent points, and what they bought.
    /// Serialisable by shape (ids and ranks), so a save file is a dictionary and nothing else.
    /// </summary>
    public sealed class SkillState
    {
        private readonly Dictionary<string, int> _ranks = new Dictionary<string, int>();

        public int TotalXp { get; private set; }
        public int Level { get; private set; } = 1;
        public int UnspentPoints { get; private set; }
        public int LifetimePoints { get; private set; }

        public IReadOnlyDictionary<string, int> Ranks => _ranks;
        public int RankOf(string nodeId) => _ranks.TryGetValue(nodeId, out int r) ? r : 0;
        public bool IsBought(string nodeId) => RankOf(nodeId) > 0;

        public int XpIntoLevel => TotalXp - LevelCurve.TotalXpFor(Level);
        public int XpNeededForNext => LevelCurve.XpToNext(Level);
        public float LevelProgress => XpNeededForNext <= 0
            ? 1f
            : Math.Clamp(XpIntoLevel / (float)XpNeededForNext, 0f, 1f);

        /// <summary>Adds experience and returns how many levels were gained.</summary>
        public int AddXp(int amount)
        {
            if (amount <= 0) return 0;
            TotalXp += amount;

            int newLevel = LevelCurve.LevelFor(TotalXp);
            int gained = newLevel - Level;
            for (int l = Level + 1; l <= newLevel; l++)
            {
                int pts = LevelCurve.PointsForLevel(l);
                UnspentPoints += pts;
                LifetimePoints += pts;
            }
            Level = newLevel;
            return gained;
        }

        public SpendResult CanSpend(string nodeId)
        {
            var node = SkillCatalogue.Find(nodeId);
            if (node == null) return SpendResult.UnknownNode;
            if (RankOf(nodeId) >= node.MaxRank) return SpendResult.AtMaxRank;
            if (node.Requires != null && !IsBought(node.Requires)) return SpendResult.PrerequisiteMissing;
            if (UnspentPoints < node.Cost) return SpendResult.NotEnoughPoints;
            return SpendResult.Ok;
        }

        public SpendResult Spend(string nodeId)
        {
            var check = CanSpend(nodeId);
            if (check != SpendResult.Ok) return check;

            var node = SkillCatalogue.Find(nodeId)!;
            UnspentPoints -= node.Cost;
            _ranks[nodeId] = RankOf(nodeId) + 1;
            return SpendResult.Ok;
        }

        /// <summary>
        /// Gives every point back. A respec exists because the campaign is a retreat and the player
        /// should be able to answer a mission they keep failing by rebuilding, not by grinding.
        /// </summary>
        public void Respec()
        {
            _ranks.Clear();
            UnspentPoints = LifetimePoints;
        }

        /// <summary>Everything bought, totalled.</summary>
        public StatBlock TotalStats()
        {
            var block = new StatBlock();
            foreach (var pair in _ranks)
            {
                var node = SkillCatalogue.Find(pair.Key);
                node?.ApplyTo(block, pair.Value);
            }
            return block;
        }
    }
}
