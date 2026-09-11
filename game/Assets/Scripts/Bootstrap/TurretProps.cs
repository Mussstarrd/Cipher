#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// What an emplacement actually LOOKS like.
    ///
    /// OWNER, 2026-09-11: "the turrets look very generic and fake". He was right twice over. They
    /// were a gun on a tripod and a mower deck on a post -- built when the weapons still fired
    /// bullets -- and ADR-008 had since taken the bullets away entirely. A belt-fed barrel that
    /// emits malware is not a stylisation, it is a leftover, and the reason it read as fake is
    /// that it was: nothing about the object explained the thing coming out of it.
    ///
    /// So both families are rebuilt as what the fiction actually says they are. ADR-003 and ADR-009
    /// are strict about the budget: this is ONE VETERAN, months after the Exodus, in a fortified
    /// lake community, building emitters out of what a lake community has in its garages. No
    /// armoury, no sci-fi, nothing that looks purchased.
    ///
    ///   SENTRY     A satellite dish off somebody's roof, bolted to a surveyor's tripod, aimed by
    ///              hand, fed by a car battery on a pallet with the cable still coiled beside it.
    ///              It POINTS, and a thing that visibly points is a thing that is about to do
    ///              something to what it is pointing at. Tall and thin.
    ///   BRUSH HOG  A pole transformer dragged off a downed line onto a skid, with a ring of horns
    ///              on a collar turning round it, throwing the field out flat in every direction.
    ///              No barrel, nothing to aim, and it cannot be wrong-footed. Wide and low.
    ///
    /// THE SILHOUETTES ARE THE DELIVERABLE, not the detailing. A player must tell the two families
    /// apart across a golf course at a glance, and the ink outline only draws the outside edge, so
    /// the only thing that survives that distance is the overall shape. Tall-and-pointing against
    /// wide-and-flat is a contrast that survives fog, a 45-degree camera and a body standing in
    /// front of it -- and it is asserted in the tests (<see cref="SentryHeight"/> /
    /// <see cref="BrushHogSpan"/>) so that a later detailing pass cannot quietly erode it.
    ///
    /// Primitives, like <see cref="SiteProps"/>: CLAUDE.md's "BUILT SCENERY IS BOXES AND THAT IS
    /// THE DECISION" applies here for the same reason. The free kits have no hardware in them, and
    /// a photoreal turret standing next to a boxed guardhouse would make the guardhouse look worse
    /// rather than making the turret look better.
    /// </summary>
    public sealed class TurretProps
    {
        /// <summary>The part that turns to face a target. Null for families that do not aim.</summary>
        public Transform? Head { get; private set; }

        /// <summary>Where the emission belongs, in the head's space. The dish's feed horn.</summary>
        public Transform? Muzzle { get; private set; }

        /// <summary>The collar of horns, for families that broadcast by sweeping.</summary>
        public Transform? Rotor { get; private set; }

        public GameObject Root { get; }

        // ---- the silhouette contract -----------------------------------------------------------
        // Two numbers, named, because they are the whole design and everything else is detailing.

        /// <summary>Roughly how tall a sentry stands, dish included. It is the TALL one.</summary>
        public const float SentryHeight = 1.85f;

        /// <summary>And how narrow. Its pallet is the whole footprint.</summary>
        public const float SentrySpan = 1.20f;

        /// <summary>Roughly how wide a Brush Hog's skid is. It is the WIDE one.</summary>
        public const float BrushHogSpan = 2.00f;

        /// <summary>And how low. A player looks down on this one and along the other.</summary>
        public const float BrushHogHeight = 1.02f;

        /// <summary>Horns on a Brush Hog's collar at tier 0, and how many each tier adds.</summary>
        public const int BaseHornCount = 6;
        public const int HornsPerTier = 2;

        /// <summary>The most the collar is ever built with. Extra horns are built and hidden.</summary>
        public const int MaxHornCount = 12;

        /// <summary>Sandbags round a sentry's feet. Damage takes them away one at a time.</summary>
        public const int SandbagCount = 5;

        private static readonly Color Steel = new Color(0.34f, 0.35f, 0.37f);
        private static readonly Color Gunmetal = new Color(0.20f, 0.21f, 0.23f);
        private static readonly Color Sandbag = new Color(0.47f, 0.43f, 0.33f);
        private static readonly Color Plank = new Color(0.38f, 0.31f, 0.23f);

        /// <summary>
        /// The dish's face. Domestic white-grey gone grubby -- this came off a house, and a dish
        /// that is the same steel as its own tripod stops reading as salvage and starts reading as
        /// a manufactured weapon, which is precisely the note the owner objected to.
        /// </summary>
        private static readonly Color DishFace = new Color(0.72f, 0.71f, 0.67f);

        /// <summary>Battery case. The one saturated thing on either emplacement, and it is small.</summary>
        private static readonly Color Battery = new Color(0.24f, 0.36f, 0.30f);

        /// <summary>Transformer can: galvanised, lighter than the steel so the drum reads out.</summary>
        private static readonly Color Galvanised = new Color(0.56f, 0.57f, 0.58f);

        /// <summary>Copper. Bushings and cable ends. Warm, tiny, and deliberate.</summary>
        private static readonly Color Copper = new Color(0.48f, 0.30f, 0.16f);

        /// <summary>
        /// A ratchet strap. The one COOL accent on either emplacement, and its whole job is to be
        /// a colour that was not chosen by whoever built the hardware -- salvage is recognisable
        /// because its parts do not match, and two greys and a brown match far too well.
        /// </summary>
        private static readonly Color Strap = new Color(0.24f, 0.33f, 0.44f);

        /// <summary>
        /// The colour the horn and the feed throat glow.
        ///
        /// TAKEN FROM <see cref="SignalFx.Pulse"/> AND NOT CHOSEN SEPARATELY. What comes out of a
        /// gun and what the gun looks like while it is coming out have to be the same colour or
        /// the emplacement reads as a prop with an effect happening near it.
        /// </summary>
        private static readonly Color Emission = SignalFx.Pulse;

        private readonly Func<Color, Material> _material;

        /// <summary>Parts shown or hidden by tier. Index 0 is wanted from tier 1, and so on.</summary>
        private readonly List<GameObject> _tierParts = new List<GameObject>(4);

        /// <summary>Parts taken away by damage, worst-hit first.</summary>
        private readonly List<GameObject> _fragile = new List<GameObject>(8);

        /// <summary>What a hit emplacement leans on. Never the root -- the bootstrap owns that.</summary>
        private Transform? _lean;

        private int _tier = -1;
        private int _missing = -1;

        private TurretProps(GameObject root, Func<Color, Material> material)
        {
            Root = root;
            _material = material;
        }

        public static TurretProps Build(bool area, Func<Color, Material> material)
        {
            var root = new GameObject(area ? "BrushHog" : "Sentry");
            var props = new TurretProps(root, material);

            // Everything hangs off a lean pivot so damage can tip the whole emplacement without
            // touching Root's transform, which the bootstrap rewrites every frame for position,
            // tier scale and its own damage sag.
            var lean = new GameObject("Lean").transform;
            lean.SetParent(root.transform, false);
            props._lean = lean;

            if (area) props.BrushHog(lean); else props.Sentry(lean);

            // Sandbags, a pallet and a stack of horns are exactly the case vertex AO was built
            // for: the darkening where parts meet each other and the ground is what stops them
            // reading as loose boxes. Keyed by family, so every sentry shares one bake.
            VertexAo.Bake(root, area ? "BrushHog" : "Sentry");

            // Tier and damage are applied AFTER the bake, and only ever by enabling or disabling
            // whole objects, so nothing here can invalidate it.
            props.SetTier(0);
            props.SetIntegrity(1f);

            // Measured once, here, rather than every frame: an emplacement can be sold or fall
            // back, so its shadow is QUEUED by the caller rather than registered as permanent.
            props.GroundRadius = Mathf.Clamp(BlobShadows.MeasureRadius(root), 0.35f, 1.4f);
            return props;
        }

        /// <summary>
        /// Radius of the contact shadow this emplacement wants. The caller queues it each frame --
        /// <c>blobs.Queue(props.Root.transform.position, props.GroundRadius)</c> -- because turrets
        /// are sold, upgraded and abandoned, and a permanent registration would outlive them.
        /// </summary>
        public float GroundRadius { get; private set; } = 0.7f;

        // ------------------------------------------------------------------ tier and damage

        /// <summary>
        /// How many horns a Brush Hog's collar carries at this tier, and -- the same idea on the
        /// sentry -- how many aerials it has grown. Tier has to be visible from where the player
        /// stands, because an upgrade the player cannot see is an upgrade he stops buying.
        /// </summary>
        public static int PartsForTier(int tier)
            => Mathf.Clamp(BaseHornCount + Mathf.Max(tier, 0) * HornsPerTier, BaseHornCount, MaxHornCount);

        /// <summary>
        /// How far a hurt emplacement leans, in degrees.
        ///
        /// A LEAN, NOT A SHRINK. The bootstrap already sags a damaged turret vertically and that is
        /// as far as scale should ever go -- something that gets smaller as it is hurt reads as
        /// being further away. A thing off its own axis reads as a thing that has been hit.
        /// </summary>
        public static float LeanDegrees(float hp01) => 9f * (1f - Mathf.Clamp01(hp01));

        /// <summary>
        /// How many pieces have been knocked off at this much health. Stepped rather than
        /// continuous: a part is either there or it is not, and quantising it means the player
        /// reads three distinct states rather than watching a slider.
        /// </summary>
        public static int MissingParts(float hp01)
        {
            float h = Mathf.Clamp01(hp01);
            if (h > 0.75f) return 0;
            if (h > 0.50f) return 1;
            if (h > 0.25f) return 2;
            return 3;
        }

        /// <summary>
        /// Shows the hardware this tier has paid for.
        ///
        /// CALL SITE: <c>SyncTurretObjects</c> in FloodBootstrap, beside the existing
        /// <c>props.Root.transform.localScale</c> line -- <c>props.SetTier(t.Tier)</c>. Without it
        /// every emplacement stays at tier 0 and the only tier cue is the 10%-per-tier scale the
        /// bootstrap already applies, which nobody can read on a lone turret.
        /// </summary>
        public void SetTier(int tier)
        {
            tier = Mathf.Max(tier, 0);
            if (tier == _tier) return;
            _tier = tier;
            Reconcile();
        }

        /// <summary>
        /// Takes pieces off an emplacement that has been chewed on, and tips what is left.
        ///
        /// CALL SITE: <c>SyncTurretObjects</c>, where <c>hp</c> is already computed --
        /// <c>props.SetIntegrity(hp)</c>. Without it a wrecked emplacement looks exactly like a
        /// fresh one apart from the bootstrap's vertical sag.
        /// </summary>
        public void SetIntegrity(float hp01)
        {
            int missing = MissingParts(hp01);
            if (_lean != null) _lean.localRotation = Quaternion.Euler(LeanDegrees(hp01), 0f, LeanDegrees(hp01) * 0.4f);
            if (missing == _missing) return;
            _missing = missing;
            Reconcile();
        }

        private void Reconcile()
        {
            int tier = Mathf.Max(_tier, 0);
            int missing = Mathf.Max(_missing, 0);

            for (int i = 0; i < _tierParts.Count; i++)
            {
                var part = _tierParts[i];
                if (part != null) part.SetActive(i < tier * HornsPerTier);
            }

            // Damage eats from the END of the fragile list, so the pieces that vanish are the ones
            // authored as expendable -- a sandbag, an outrigger horn -- and never the dish or the
            // transformer, which are the silhouette.
            for (int i = 0; i < _fragile.Count; i++)
            {
                var part = _fragile[i];
                if (part != null) part.SetActive(i < _fragile.Count - missing);
            }
        }

        // ------------------------------------------------------------------ the sentry

        /// <summary>A dish off a roof, on a tripod, wired to a car battery.</summary>
        private void Sentry(Transform t)
        {
            // ---- the ground it stands on -------------------------------------------------------
            // A pallet, because everything a veteran moves between positions arrives on one, and
            // because a flat plane under the legs stops a tripod looking like it is floating.
            Box(t, "Pallet", new Vector3(0f, 0.05f, -0.06f), new Vector3(1.12f, 0.10f, 0.92f), Plank);
            for (int i = 0; i < 3; i++)
                Box(t, "Plank", new Vector3(0f, 0.11f, -0.06f + (i - 1) * 0.30f),
                    new Vector3(1.12f, 0.035f, 0.16f), Plank);

            // Sandbags. Five, ringed and each turned a little so the stack has no repeating edge;
            // the last two are the first things damage takes away.
            for (int i = 0; i < SandbagCount; i++)
            {
                float a = i * (360f / SandbagCount) + 24f;
                var turn = Quaternion.Euler(0f, a, 0f);
                var bag = Box(t, "Sandbag", turn * new Vector3(0f, 0f, 0.56f) + new Vector3(0f, 0.13f, 0f),
                              new Vector3(0.58f, 0.24f, 0.32f), Sandbag, turn);
                if (i >= SandbagCount - 2) _fragile.Add(bag.gameObject);
            }

            // ---- power -------------------------------------------------------------------------
            // A car battery and a coil of cable. Small, low, and the one thing on the emplacement
            // that is obviously domestic; it is what tells the player nobody issued this.
            Box(t, "Battery", new Vector3(-0.46f, 0.22f, -0.34f), new Vector3(0.34f, 0.22f, 0.22f), Battery);
            Box(t, "Terminal", new Vector3(-0.53f, 0.34f, -0.34f), new Vector3(0.06f, 0.05f, 0.06f), Copper);
            Box(t, "Terminal", new Vector3(-0.39f, 0.34f, -0.34f), new Vector3(0.06f, 0.05f, 0.06f), Copper);
            // The run up to the hub, in three straight pieces. A cable drawn as one diagonal is a
            // strut; drawn as a slack broken line it is a cable.
            Box(t, "Cable", new Vector3(-0.34f, 0.16f, -0.28f), new Vector3(0.30f, 0.05f, 0.05f), Gunmetal,
                Quaternion.Euler(0f, 0f, -22f));
            Box(t, "Cable", new Vector3(-0.14f, 0.30f, -0.20f), new Vector3(0.34f, 0.05f, 0.05f), Gunmetal,
                Quaternion.Euler(0f, -28f, -52f));

            // ---- the tripod --------------------------------------------------------------------
            // Three splayed legs to a hub. A surveyor's tripod, which is the most plausible stand
            // in a rural garage and reads as "set up here on purpose" rather than "bolted down".
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f + 30f;
                var lean = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(19f, 0f, 0f);
                Box(t, "Leg", Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.38f, 0.15f),
                    new Vector3(0.065f, 0.84f, 0.065f), Steel, lean);
                Box(t, "LegFoot", Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.03f, 0.29f),
                    new Vector3(0.16f, 0.05f, 0.16f), Gunmetal, Quaternion.Euler(0f, a, 0f));
            }
            // A tie ring round the legs at knee height: it braces them and, more usefully, gives
            // the middle of the silhouette something in it so the stand is not two lines and a gap.
            Cylinder(t, "Brace", new Vector3(0f, 0.36f, 0f), new Vector3(0.44f, 0.035f, 0.44f), Gunmetal);
            Box(t, "Hub", new Vector3(0f, 0.80f, 0f), new Vector3(0.20f, 0.16f, 0.20f), Gunmetal);
            // A length of fence post clamped into the tripod's head to get the dish up. Lighter
            // than the legs on purpose: it is plainly a different piece of metal from a different
            // pile, which is most of what makes a thing read as built rather than bought.
            Box(t, "Mast", new Vector3(0f, 1.02f, 0f), new Vector3(0.10f, 0.48f, 0.10f), Galvanised);
            Box(t, "Clamp", new Vector3(0f, 0.84f, 0f), new Vector3(0.17f, 0.06f, 0.17f), Strap);
            Box(t, "Clamp", new Vector3(0f, 1.20f, 0f), new Vector3(0.17f, 0.06f, 0.17f), Strap);

            // ---- the head: everything above here is aimed ---------------------------------------
            var head = new GameObject("Head").transform;
            head.SetParent(t, false);
            // HIGH ENOUGH THAT THE TRIPOD IS STILL IN THE SILHOUETTE. At 0.92 the dish sat down over
            // its own legs and the whole emplacement photographed as a disc lying on the ground --
            // which throws away the tall, standing, obviously-aimed read the family exists for.
            head.localPosition = new Vector3(0f, 1.28f, 0f);
            Head = head;

            // The yoke the dish swings in. Two plates and a pin; without them the dish looks glued
            // to the top of a pole, and the pin is what says a person aimed this by hand.
            Box(head, "YokeL", new Vector3(-0.24f, -0.02f, -0.06f), new Vector3(0.05f, 0.30f, 0.20f), Steel);
            Box(head, "YokeR", new Vector3(0.24f, -0.02f, -0.06f), new Vector3(0.05f, 0.30f, 0.20f), Steel);
            Box(head, "Pin", new Vector3(0f, 0.08f, -0.06f), new Vector3(0.54f, 0.05f, 0.05f), Gunmetal);
            // The strap that actually holds the elevation. A dish aimed by a man with a ratchet
            // strap is the whole of ADR-009's budget in one object.
            Box(head, "Strap", new Vector3(0f, -0.20f, 0.06f), new Vector3(0.05f, 0.34f, 0.05f), Strap,
                Quaternion.Euler(34f, 0f, 0f));

            // THE DISH. The cylinder primitive's axis is Y, so a 90-degree pitch lays it across the
            // line of fire and the flat face points at what the head is pointing at. Two discs, the
            // back one fractionally wider, which gives the rim a lip -- and the lip is what the ink
            // outline catches. A single flat disc inks as a plain circle and reads as a sign.
            var dishTilt = Quaternion.Euler(90f, 0f, 0f);
            Cylinder(head, "DishRim", new Vector3(0f, 0.10f, 0.13f), new Vector3(1.14f, 0.07f, 1.14f),
                     Steel, dishTilt);
            Cylinder(head, "DishFace", new Vector3(0f, 0.10f, 0.19f), new Vector3(1.01f, 0.06f, 1.01f),
                     DishFace, dishTilt);
            Cylinder(head, "DishInner", new Vector3(0f, 0.10f, 0.24f), new Vector3(0.66f, 0.05f, 0.66f),
                     DishFace, dishTilt);

            // The amplifier can bolted to the back of the dish, and the two struts holding the feed
            // arm out in front of it. The struts are the detail that makes a disc read as a DISH.
            Box(head, "Amplifier", new Vector3(0f, 0.10f, -0.02f), new Vector3(0.30f, 0.26f, 0.22f), Gunmetal);
            Box(head, "FeedArm", new Vector3(-0.20f, -0.14f, 0.38f), new Vector3(0.035f, 0.035f, 0.60f), Steel,
                Quaternion.Euler(-22f, -14f, 0f));
            Box(head, "FeedArm", new Vector3(0.20f, -0.14f, 0.38f), new Vector3(0.035f, 0.035f, 0.60f), Steel,
                Quaternion.Euler(-22f, 14f, 0f));
            Box(head, "FeedArm", new Vector3(0f, 0.42f, 0.38f), new Vector3(0.035f, 0.035f, 0.60f), Steel,
                Quaternion.Euler(20f, 0f, 0f));

            // The feed horn at the focus, and the throat inside it. The throat is lit in the
            // weapon's own amber so that even an idle sentry says which end the energy leaves by.
            Box(head, "FeedHorn", new Vector3(0f, 0.10f, 0.66f), new Vector3(0.17f, 0.17f, 0.20f), Gunmetal);
            Box(head, "FeedThroat", new Vector3(0f, 0.10f, 0.76f), new Vector3(0.11f, 0.11f, 0.05f), Emission);

            // Tier: whip aerials off the hub. Thin, tall and unmistakable at range -- a tier-2
            // sentry is visibly bristling from the far side of the golf course, which is the only
            // distance at which reading tier actually matters.
            for (int i = 0; i < (MaxHornCount - BaseHornCount); i++)
            {
                float a = 40f + i * 47f;
                var turn = Quaternion.Euler(0f, a, 0f);
                var aerial = Box(t, "Aerial",
                                 turn * new Vector3(0f, 1.06f, 0.13f),
                                 new Vector3(0.026f, 0.56f, 0.026f), Gunmetal,
                                 turn * Quaternion.Euler(-13f, 0f, 0f));
                // A child's local offset is in the PARENT'S scaled units, so 0.5 is the top of the
                // rod whatever the rod's length. Writing 0.56 here -- the rod's world length --
                // would put the tip a whole rod above where it belongs.
                Box(aerial, "AerialTip", new Vector3(0f, 0.5f, 0f), new Vector3(1.9f, 0.07f, 1.9f), Copper);
                _tierParts.Add(aerial.gameObject);
            }

            // The emission IS the muzzle object, parked at zero scale. Popping one primitive for a
            // couple of frames is cheaper and reads harder than any particle system at this
            // distance, and the cel shader bands it into a flat shape, which is exactly the look.
            Muzzle = Box(head, "Muzzle", new Vector3(0f, 0.10f, 0.84f), Vector3.zero, Emission);
            Muzzle.GetComponent<Renderer>().sharedMaterial = _material(Emission);
        }

        // ------------------------------------------------------------------ the brush hog

        /// <summary>A pole transformer on a skid with a collar of horns turning round it.</summary>
        private void BrushHog(Transform t)
        {
            // ---- the skid ----------------------------------------------------------------------
            // Wide, flat, and the single most important thing in the silhouette: it is what makes
            // this family read as GROUND the player owns rather than as a weapon on a stand.
            float half = BrushHogSpan * 0.5f;
            Box(t, "Skid", new Vector3(0f, 0.06f, 0f), new Vector3(BrushHogSpan, 0.12f, BrushHogSpan * 0.78f), Plank);
            for (int i = 0; i < 4; i++)
                Box(t, "SkidPlank", new Vector3(0f, 0.13f, (i - 1.5f) * 0.36f),
                    new Vector3(BrushHogSpan, 0.04f, 0.24f), Plank);
            // Two runners under it, so the skid has a shadow line and is plainly a thing that was
            // DRAGGED here behind a truck.
            Box(t, "Runner", new Vector3(-half + 0.18f, 0.03f, 0f), new Vector3(0.16f, 0.07f, BrushHogSpan * 0.84f), Gunmetal);
            Box(t, "Runner", new Vector3(half - 0.18f, 0.03f, 0f), new Vector3(0.16f, 0.07f, BrushHogSpan * 0.84f), Gunmetal);

            // ---- the transformer ---------------------------------------------------------------
            // The grey can off a downed pole. A drum with cooling ribs, a lid and two copper
            // bushings on top. Everyone has seen one of these on a pole at the end of their street,
            // which is the entire reason it works: it is instantly a piece of the power grid and
            // not a piece of kit anybody manufactured for a war.
            Cylinder(t, "Can", new Vector3(0f, 0.46f, 0f), new Vector3(0.60f, 0.62f, 0.60f), Galvanised);
            for (int i = 0; i < 3; i++)
                Cylinder(t, "Rib", new Vector3(0f, 0.30f + i * 0.16f, 0f),
                         new Vector3(0.66f, 0.035f, 0.66f), Steel);
            Cylinder(t, "Lid", new Vector3(0f, 0.79f, 0f), new Vector3(0.66f, 0.07f, 0.66f), Steel);
            Box(t, "Bushing", new Vector3(-0.15f, 0.88f, 0f), new Vector3(0.09f, 0.16f, 0.09f), Copper);
            Box(t, "Bushing", new Vector3(0.15f, 0.88f, 0f), new Vector3(0.09f, 0.16f, 0.09f), Copper);
            Box(t, "Lightning", new Vector3(0f, 0.95f, 0f), new Vector3(0.30f, 0.05f, 0.05f), Gunmetal);

            // A generator crate and its cable at the edge of the skid. Same job as the sentry's car
            // battery: it says a person set this up out of parts, and it breaks the symmetry so the
            // emplacement has a front.
            Box(t, "Generator", new Vector3(-0.62f, 0.28f, -0.52f), new Vector3(0.46f, 0.26f, 0.34f), Gunmetal);
            Box(t, "Exhaust", new Vector3(-0.62f, 0.48f, -0.62f), new Vector3(0.07f, 0.16f, 0.07f), Steel);
            Box(t, "Cable", new Vector3(-0.36f, 0.24f, -0.34f), new Vector3(0.40f, 0.05f, 0.05f), Gunmetal,
                Quaternion.Euler(0f, -34f, -16f));

            // ---- the collar ---------------------------------------------------------------------
            // The horns live on their own transform so they can turn without the can turning. The
            // bootstrap already drives this (idle slow, fast when something is in range), so a
            // Brush Hog tells the player it has found a target before the first body arrives.
            var rotor = new GameObject("Rotor").transform;
            rotor.SetParent(t, false);
            // Set LOW and kept NARROW, so the transformer's lid and bushings stand proud above the
            // ring. With the collar up at chest height and a metre across it hid the can entirely
            // and the emplacement photographed as a flying saucer -- the one silhouette a thing
            // built out of a downed power line must not have.
            rotor.localPosition = new Vector3(0f, 0.44f, 0f);
            Rotor = rotor;

            Cylinder(rotor, "Collar", Vector3.zero, new Vector3(0.74f, 0.06f, 0.74f), Steel);

            var baseArms = new Transform[BaseHornCount];
            for (int i = 0; i < MaxHornCount; i++)
            {
                // THE SLOT IS NOT THE INDEX. Horns are built at twelve fixed stations round the
                // collar, but the first six -- the ones a tier-0 emplacement has -- take every
                // OTHER station, so a base Brush Hog is a complete evenly spaced ring and a tier
                // fills the gaps in it. Numbering them 0..5 round the circle would have given the
                // cheapest emplacement six horns along one side and none on the other.
                float a = HornSlot(i) * (360f / MaxHornCount);
                var turn = Quaternion.Euler(0f, a, 0f);

                var arm = new GameObject("HornArm").transform;
                arm.SetParent(rotor, false);
                arm.localRotation = turn;

                // TIER HORNS REACH FURTHER OUT, and that is the point of them. Counting six horns
                // against eight across a field is not something anybody does; a ring that visibly
                // grows WIDER and spikier with every tier is read in the same glance that finds
                // the emplacement at all.
                bool outrigger = i >= BaseHornCount;
                float reach = outrigger ? 0.82f : 0.50f;
                Box(arm, "Arm", new Vector3(0f, 0.02f, reach * 0.68f), new Vector3(0.05f, 0.05f, reach), Steel);
                var horn = Box(arm, "Horn", new Vector3(0f, 0.10f, reach * 1.24f),
                               new Vector3(0.19f, 0.19f, 0.24f),
                               Galvanised, Quaternion.Euler(-16f, 0f, 0f));
                Box(horn, "Throat", new Vector3(0f, 0f, 0.56f), new Vector3(0.62f, 0.62f, 0.12f), Emission);

                if (i < BaseHornCount) baseArms[i] = arm;
                else _tierParts.Add(arm.gameObject);
            }

            // Damage eats the END of the fragile list, and the pieces it eats must be spread round
            // the ring or a chewed-up emplacement reads as having been BUILT lopsided rather than
            // as having been hit. Ordering the base horns 0,2,4,1,3,5 means the first three losses
            // are horns 5, 3 and 1 -- a hundred and twenty degrees apart.
            int[] order = { 0, 2, 4, 1, 3, 5 };
            for (int j = 0; j < order.Length; j++) _fragile.Add(baseArms[order[j]].gameObject);
        }

        /// <summary>
        /// Which of the twelve collar stations horn <paramref name="index"/> stands at. The first
        /// <see cref="BaseHornCount"/> take the even stations so the cheapest emplacement is still
        /// a complete ring; the rest fill in between.
        /// </summary>
        public static int HornSlot(int index)
            => index < BaseHornCount ? index * 2 : (index - BaseHornCount) * 2 + 1;

        // ------------------------------------------------------------------ primitives

        private Transform Box(Transform parent, string name, Vector3 localPosition, Vector3 size,
                              Color colour, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = _material(colour);
            return go.transform;
        }

        private Transform Cylinder(Transform parent, string name, Vector3 localPosition, Vector3 size,
                                   Color colour, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            // The built-in cylinder is 2 units tall, so halve the height to make size mean size.
            go.transform.localScale = new Vector3(size.x, size.y * 0.5f, size.z);
            go.GetComponent<Renderer>().sharedMaterial = _material(colour);
            return go.transform;
        }
    }
}
