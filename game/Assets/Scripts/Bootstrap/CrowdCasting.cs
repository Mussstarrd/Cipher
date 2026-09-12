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

        /// <summary>
        /// Brings its own body and must never be given a crowd slot. The Collector (ADR-011) is
        /// three times the mass of a person and is built by hand, one or two to a field.
        ///
        /// THIS IS THE DEFAULT FOR ANYTHING NEW. An archetype the casting has never heard of gets
        /// no crowd body at all rather than an ordinary citizen's, because a thing that is drawn
        /// TWICE -- once as its own model and once wearing somebody's coat -- is a bug you see
        /// immediately, and a thing that is drawn as the wrong species is a bug you argue about.
        /// </summary>
        Own = 4,
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
        ///
        /// **The simulation owns this number too** (<see cref="AgentWorld.MachineShare"/>) and the
        /// bootstrap pushes this value into it, because ADR-010's drone can only convert machines:
        /// the sim has to be able to answer "is that one a machine" for a rule, and the renderer
        /// has to give the same answer for a picture, or the player aims at a robot and is told
        /// there is nothing there.
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
                case Archetype.Runner:
                    return Fraction(id, seed) < HumanoidShare ? BodyClass.Humanoid : BodyClass.Signed;
                case Archetype.Sapper: return BodyClass.Sapper;
                case Archetype.Spitter: return BodyClass.Spitter;
                default: return BodyClass.Own;
            }
        }

        /// <summary>
        /// A stable value in [0,1) from an id and a seed. A hash rather than an RNG: nothing is
        /// sequenced, nothing is stored, and asking twice costs nothing.
        ///
        /// **THE IMPLEMENTATION LIVES IN THE SIMULATION** (<see cref="AgentWorld.MachineFraction"/>)
        /// and this forwards to it. It used to be a second copy of the same avalanche, sitting in
        /// the renderer, and the copies happened to agree -- which is the dangerous version of this
        /// bug, because nothing fails until somebody tunes one of them. Once ADR-010's drone could
        /// convert a machine, "which bodies are machines" stopped being a question about pictures
        /// and became a rule, and a rule has exactly one home.
        /// </summary>
        public static float Fraction(int id, int seed) => AgentWorld.MachineFraction(id, seed);

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

        /// <summary>
        /// True for classes the crowd has bodies for. <see cref="BodyClass.Own"/> is false, and
        /// because no slots are ever built for it the crowd excludes it by construction -- there is
        /// no flag anybody has to remember to check.
        /// </summary>
        public static bool HasCrowdBody(BodyClass c) => (int)c < Built.Length;
    }
}
