#nullable enable
using Cipher.Sim.Agents;

namespace Cipher.Game
{
    /// <summary>
    /// What KIND of body an agent wears. A visual class, not a sim archetype.
    ///
    /// ADR-003 names three enemy classes and we had only ever built one. The owner, 2026-09-11:
    /// "all I'm seeing is humans I'm thinking that like 30% of them should be those humanoid
    /// robots". He is describing the second row of ADR-003's table, which has existed in the
    /// fiction since the premise was written and never existed on screen.
    ///
    /// This is deliberately NOT an <see cref="Archetype"/>. The sim has three -- Runner, Sapper,
    /// Spitter -- and adding a fourth is a design decision about how the game plays, which this is
    /// not: a hacked delivery walker and a chipped accountant path the same, hit the same and die
    /// the same. What differs is what the player SEES, so the split lives entirely in the view.
    /// </summary>
    public enum BodyClass
    {
        /// <summary>An ordinary chipped citizen. ADR-003's "the signed". The wave is mostly this.</summary>
        Signed = 0,

        /// <summary>A hacked service humanoid: delivery walker, groundskeeper, clubhouse attendant.</summary>
        Humanoid = 1,

        /// <summary>The breacher. ADR-003: "a person with a toolbox who used to be a contractor".</summary>
        Sapper = 2,

        /// <summary>The tower-hunter. ADR-003: "a hacked humanoid with an industrial sprayer".</summary>
        Spitter = 3,
    }

    /// <summary>
    /// Casting: which body class an agent wears, and how many bodies of each class the pool holds.
    ///
    /// Pure and deterministic on purpose. An agent's class is a function of its id alone, so it can
    /// be asked for again on any frame and in any order and give the same answer. The alternative --
    /// rolling a class when an agent is promoted -- is the same mistake as assigning slots by
    /// distance rank, which the owner saw as bodies "scan switching from skin to skin to skin": an
    /// agent that walks out of range and back would come back a different species.
    ///
    /// ADR-003 also settles the two named archetypes for us, so neither is a judgement call:
    ///   Sapper  "The Sapper is a person with a toolbox who used to be a contractor."
    ///   Spitter "The Spitter becomes a hacked humanoid with an industrial sprayer."
    /// which is why the breacher stays human and the tower-hunter becomes a machine. That happens
    /// to be the best possible reading too: the two have opposite counter-play, and they now differ
    /// in silhouette as well as colour rather than being an orange pill and a green one.
    /// </summary>
    public static class CrowdCasting
    {
        /// <summary>
        /// The owner's number. Of the ordinary wave, roughly this fraction are machines.
        ///
        /// Applied to Runners only. Spitters are machines unconditionally, so the share of the
        /// crowd that reads as mechanical ends up slightly above this, which is correct: "like 30%"
        /// is a texture, not a quota.
        /// </summary>
        public const float HumanoidShare = 0.30f;

        /// <summary>
        /// The body class for an agent. <paramref name="seed"/> is the match seed, so two matches
        /// do not put the delivery walkers in exactly the same places.
        /// </summary>
        public static BodyClass ClassOf(Archetype archetype, int id, int seed)
        {
            switch (archetype)
            {
                case Archetype.Sapper: return BodyClass.Sapper;
                case Archetype.Spitter: return BodyClass.Spitter;
                default:
                    return Fraction(id, seed) < HumanoidShare ? BodyClass.Humanoid : BodyClass.Signed;
            }
        }

        /// <summary>
        /// A stable value in [0,1) from an id and a seed. A hash rather than an RNG: nothing is
        /// sequenced, nothing is stored, and asking twice costs nothing.
        /// </summary>
        public static float Fraction(int id, int seed)
        {
            // A 32-bit avalanche (Murmur3's finaliser). The point is that consecutive ids -- which
            // is exactly how the spawn director hands them out -- do not come out in runs, so a
            // wave does not arrive as eleven machines followed by twenty people.
            unchecked
            {
                uint h = (uint)id * 2654435761u ^ (uint)seed * 2246822519u;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                h *= 0xC2B2AE35u;
                h ^= h >> 16;
                return (h >> 8) * (1f / 16777216f);   // top 24 bits, so the float is exact
            }
        }

        // ---- the pool ------------------------------------------------------------------------
        //
        // Bodies are built per class and a slot cannot change class, so the pool has to be
        // partitioned up front. Two numbers govern it and they are deliberately different:
        //
        //   BUILT   how many bodies exist. Costs load time and memory and nothing else, because a
        //           body with no occupant is inactive and never reaches the renderer.
        //   ACTIVE  how many may be on screen at once. This is the frame budget.
        //
        // They used to be the same number (110) because there was one class. Keeping them the same
        // under four classes would mean sizing every class for its worst case and then wasting the
        // difference every other wave: The Gate's first wave is 100% Runner, so a third of a
        // strictly-partitioned pool would sit idle while agents twenty metres away stayed capsules.
        // Overbuilding and capping instead costs some startup and no frames.

        /// <summary>Bodies built per class, indexed by <see cref="BodyClass"/>.</summary>
        public static readonly int[] Built = { 72, 32, 14, 18 };

        /// <summary>
        /// The frame budget: the most bodies that may be promoted at once, across all classes.
        ///
        /// Unchanged from the single-class pool, and it is the number to argue with if the crowd
        /// ever costs too much. Note that it is now a MIXED hundred and ten: fifty of the built
        /// bodies are box-built machines rather than skinned characters, so the same cap buys
        /// strictly less work than it used to.
        /// </summary>
        public const int ActiveBudget = 110;

        /// <summary>Total bodies built. More than <see cref="ActiveBudget"/>, on purpose.</summary>
        public static int TotalBuilt
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Built.Length; i++) n += Built[i];
                return n;
            }
        }

        /// <summary>True for the classes drawn as machines rather than as skinned people.</summary>
        public static bool IsMachine(BodyClass c) => c == BodyClass.Humanoid || c == BodyClass.Spitter;
    }
}
