#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// WHAT IS ACTUALLY IN THE VETERAN'S HANDS, one object per rung of the gun ladder.
    ///
    /// OWNER, 2026-09-12: "different gun upgrades should make my weapon skin different and it
    /// should also change the way my projectile goes". Until now the hero carried ONE black box
    /// on the end of his arm for the whole campaign and the four upgrades existed only as a string
    /// in the HUD. An upgrade the player cannot see is an upgrade the player does not feel -- the
    /// same argument that rebuilt the emplacements in <see cref="TurretProps"/>, and the same
    /// answer: primitives, assembled so the SILHOUETTE carries it.
    ///
    /// THE LADDER IS CRUDE -> SOPHISTICATED, NOT SMALL -> BIG (ADR-008). These are signal weapons
    /// carrying malware, built and scavenged by one man months after the Exodus. There is no
    /// armoury in this world, so nothing here may look purchased, and an upgrade is a better
    /// AERIAL and a better payload rather than a larger calibre:
    ///
    ///   FIELD JAMMER     A hardware-store bodge. A cordless drill's grip and body, a length of
    ///                    galvanised conduit taped to the front of it, a car amplifier cable-tied
    ///                    on top and a hand-bent wire loop for an aerial. Short, lumpy, asymmetric,
    ///                    and the loop is a HOLE in the silhouette -- you can see the field through
    ///                    his weapon, which is the most honest thing that could be said about it.
    ///   DECRYPTOR        The first one he built rather than bodged. A machined body on a rifle
    ///                    stock, finned to shed heat, ending in a stepped waveguide horn. Straight,
    ///                    symmetrical, deliberate. It reads as competence, not as power.
    ///   WORM LANCE       The longest and the thinnest. A slim lance with three repeater collars
    ///                    spaced down it and a forked tip, with a payload canister slung
    ///                    underneath. The forks and the collars are the fiction: something travels
    ///                    ALONG this and goes looking when it arrives.
    ///   CASCADE EMITTER  Named for the event in ADR-003 by the man who refused the chip. Shorter
    ///                    than the lance and twice as wide: a two-by-two block of horns on a
    ///                    manifold with a capacitor drum and a cooling stack behind it. The widest
    ///                    head in the game and the only rung allowed to look excessive.
    ///
    /// THE CONTRACT IS (<see cref="Reach"/>, <see cref="HeadSpan"/>) AND IT IS NOT A SIZE RAMP.
    /// Reach goes 0.72 -> 0.84 -> 1.18 -> 0.92 and span goes 0.35 -> 0.22 -> 0.18 -> 0.52, so no
    /// two rungs share a silhouette and the ladder cannot be mistaken for the same weapon getting
    /// bigger. This thing is seen SMALL, over a shoulder, against a busy field, and under the ink
    /// shader the outside edge is all that survives -- which makes those two numbers the whole
    /// design and everything else detailing. They are asserted in <c>HeroEmitterTests</c> for the
    /// same reason the emplacements' are: it is exactly the property a later pass erodes by
    /// accident.
    ///
    /// CALL SITE (FloodBootstrap.BuildSceneObjects, replacing the "Barrel" cube):
    ///     _emitter = HeroEmitter.Build(_heroT, MakeProp);
    ///     _barrelT = _emitter.Root.transform;
    /// and wherever the gun tier is read (beside the HUD's GunName, or after PickupSystem.Tick):
    ///     _emitter.SetTier(_pickups.GunTier);
    /// <see cref="Build"/> handles the hero capsule's non-uniform scale itself, so the emitter is
    /// built in metres and stays in metres whatever the body it is hung on.
    /// </summary>
    public sealed class HeroEmitter
    {
        /// <summary>Everything, parented to the hero. The bootstrap shows and hides this.</summary>
        public GameObject Root { get; }

        /// <summary>Where the pulse leaves, in world space. Follows the tier.</summary>
        public Transform? Muzzle => _muzzles[Mathf.Clamp(_tier, 0, MaxTier)];

        /// <summary>The rung currently in his hands.</summary>
        public int Tier => _tier;

        /// <summary>Rungs on the ladder, matching <c>GunTiers.MaxTier</c>.</summary>
        public const int MaxTier = 3;

        // ---- the silhouette contract -----------------------------------------------------------
        // Two numbers per rung, and they are the design. Everything else on this object is
        // detailing that the ink outline will throw away at the distance the weapon is seen from.

        /// <summary>
        /// How far in front of the grip each emitter ends, in metres. The Worm Lance is the
        /// longest thing he ever carries and the Field Jammer the shortest.
        /// </summary>
        public static float Reach(int tier) => tier switch
        {
            <= 0 => 0.72f,
            1 => 0.84f,
            2 => 1.18f,
            _ => 0.92f,
        };

        /// <summary>
        /// How wide each emitter's business end is, in metres. It goes DOWN as the weapon gets
        /// better at its job and up again only at the top, where the Cascade Emitter stops being
        /// one aerial and becomes four.
        /// </summary>
        public static float HeadSpan(int tier) => tier switch
        {
            <= 0 => 0.35f,
            1 => 0.22f,
            2 => 0.18f,
            _ => 0.52f,
        };

        /// <summary>
        /// Where the emitter hangs off the hero, in METRES from the capsule's centre: out to his
        /// right, at about chest height, far enough forward that the body does not eat the stock.
        /// </summary>
        public static readonly Vector3 Mount = new Vector3(0.20f, 0.16f, 0.34f);

        // ---- the salvage palette ---------------------------------------------------------------
        // Deliberately the same register as TurretProps: greys that do not match each other, one
        // piece of wood, one cool strap and one small warm accent. Salvage is recognisable because
        // its parts came from different places, and three matching greys read as manufactured.

        private static readonly Color Steel = new Color(0.34f, 0.35f, 0.37f);
        private static readonly Color Gunmetal = new Color(0.20f, 0.21f, 0.23f);
        private static readonly Color Galvanised = new Color(0.56f, 0.57f, 0.58f);
        private static readonly Color Copper = new Color(0.48f, 0.30f, 0.16f);
        private static readonly Color Strap = new Color(0.24f, 0.33f, 0.44f);
        private static readonly Color Stock = new Color(0.38f, 0.31f, 0.23f);
        private static readonly Color Battery = new Color(0.24f, 0.36f, 0.30f);

        /// <summary>Tool plastic. The one thing on the Field Jammer that was never a weapon part.</summary>
        private static readonly Color Tool = new Color(0.42f, 0.40f, 0.14f);

        /// <summary>
        /// What the emitter glows.
        ///
        /// TAKEN FROM <see cref="SignalFx.Emitter"/> AND NOT CHOSEN SEPARATELY, exactly as
        /// TurretProps takes its own from <see cref="SignalFx.Pulse"/>. What leaves the weapon and
        /// what the weapon looks like while it is leaving have to be one colour, or the gun reads
        /// as a prop with an effect happening near it. It also means the weapon in the hand is
        /// covered by the same WeaponGreenFloor rule the shot is, without a second decision.
        /// </summary>
        public static readonly Color Emission = SignalFx.Emitter;

        private readonly Func<Color, Material> _material;
        private readonly GameObject?[] _assemblies = new GameObject?[MaxTier + 1];
        private readonly Transform?[] _muzzles = new Transform?[MaxTier + 1];
        private int _tier = -1;

        private HeroEmitter(GameObject root, Func<Color, Material> material)
        {
            Root = root;
            _material = material;
        }

        /// <summary>
        /// Builds the emitter and hangs it on <paramref name="mount"/> -- the hero's transform.
        ///
        /// THE HERO CAPSULE IS SCALED (0.8, 0.9, 0.8) AND CHILDREN INHERIT THAT. A weapon authored
        /// in metres and parented straight to it comes out squashed on two axes and, worse,
        /// SHEARED wherever a part is rotated, because Unity composes a non-uniform parent scale
        /// through a child's rotation. The root cancels the mount's scale exactly, so everything
        /// below it is a clean rigid frame and the numbers in this file mean metres.
        /// </summary>
        public static HeroEmitter Build(Transform mount, Func<Color, Material> material)
        {
            var root = new GameObject("Emitter");
            root.transform.SetParent(mount, worldPositionStays: false);

            Vector3 s = mount.lossyScale;
            var inverse = new Vector3(Safe(s.x), Safe(s.y), Safe(s.z));
            root.transform.localScale = inverse;
            root.transform.localPosition = new Vector3(Mount.x * inverse.x, Mount.y * inverse.y,
                                                       Mount.z * inverse.z);

            var emitter = new HeroEmitter(root, material);
            emitter.SetTier(0);
            return emitter;

            static float Safe(float v) => Mathf.Abs(v) < 1e-4f ? 1f : 1f / v;
        }

        /// <summary>
        /// Puts rung <paramref name="tier"/> in his hands. Cheap to call every frame: it returns
        /// immediately unless the tier actually moved, and each rung is built the first time it is
        /// asked for rather than all four up front -- most matches never see the top of the ladder.
        ///
        /// CALL SITE: wherever <c>PickupSystem.GunTier</c> is read in the bootstrap's update.
        /// Without it the hero carries a Field Jammer through the whole campaign while the HUD
        /// tells him he is holding a Cascade Emitter, which is the bug this class exists to fix.
        /// </summary>
        public void SetTier(int tier)
        {
            tier = Mathf.Clamp(tier, 0, MaxTier);
            if (tier == _tier) return;

            if (_tier >= 0 && _assemblies[_tier] != null) _assemblies[_tier]!.SetActive(false);
            _tier = tier;

            if (_assemblies[tier] == null) _assemblies[tier] = BuildTier(tier);
            _assemblies[tier]!.SetActive(true);
        }

        // ------------------------------------------------------------------ the four weapons

        private GameObject BuildTier(int tier)
        {
            var go = new GameObject("Tier" + tier);
            go.transform.SetParent(Root.transform, worldPositionStays: false);
            var t = go.transform;

            switch (tier)
            {
                case 0: FieldJammer(t); break;
                case 1: Decryptor(t); break;
                case 2: WormLance(t); break;
                default: CascadeEmitter(t); break;
            }

            // The muzzle is an empty at the tip. Zero-scale rather than a hidden object so that
            // nothing has to remember to keep it invisible, and so a caller can parent a flash to
            // it. It sits exactly at Reach(tier), which is what makes the contract a fact about
            // the built object rather than a comment.
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(t, false);
            muzzle.localPosition = new Vector3(0f, 0.02f, Reach(tier));
            _muzzles[tier] = muzzle;

            // NO VertexAo.Bake HERE, deliberately. The bake clones a material per renderer to
            // switch vertex colours on, and this object is twenty centimetres of screen seen over
            // a shoulder: it would pay four material clones for contact shading nobody can
            // resolve. The emplacements are baked because they are looked AT; this is looked ALONG.
            return go;
        }

        /// <summary>
        /// TIER 0. A cordless drill, a length of conduit and a bent coathanger. The loop aerial is
        /// the whole silhouette: it is a hole, and a weapon you can see the field through is a
        /// weapon that is plainly not finished.
        /// </summary>
        private void FieldJammer(Transform t)
        {
            // The drill it used to be. Grip, body, battery under the hand -- all of it obviously
            // domestic, because this is the rung where the player should think "he made this
            // yesterday".
            Box(t, "Grip", new Vector3(0f, -0.11f, -0.01f), new Vector3(0.070f, 0.21f, 0.090f), Tool,
                Quaternion.Euler(9f, 0f, 0f));
            Box(t, "DrillBody", new Vector3(0f, 0.025f, 0.09f), new Vector3(0.115f, 0.130f, 0.24f), Tool);
            Box(t, "Battery", new Vector3(0f, -0.225f, 0.01f), new Vector3(0.110f, 0.075f, 0.135f), Battery,
                Quaternion.Euler(9f, 0f, 0f));
            Box(t, "Chuck", new Vector3(0f, 0.025f, 0.215f), new Vector3(0.085f, 0.085f, 0.045f), Gunmetal);

            // The conduit, which is what actually emits. Galvanised, so it is plainly a different
            // metal from the chuck it is jammed into.
            Cylinder(t, "Conduit", new Vector3(0f, 0.025f, 0.42f), new Vector3(0.072f, 0.40f, 0.072f),
                     Galvanised, Quaternion.Euler(90f, 0f, 0f));
            Box(t, "Tape", new Vector3(0f, 0.025f, 0.27f), new Vector3(0.092f, 0.092f, 0.040f), Strap);
            Box(t, "Tape", new Vector3(0f, 0.025f, 0.50f), new Vector3(0.092f, 0.092f, 0.040f), Strap);

            // A car amplifier cable-tied on top, sitting slightly crooked. The crookedness is the
            // point -- one part off-square does more for "improvised" than any amount of detailing.
            Box(t, "Amplifier", new Vector3(0.005f, 0.125f, 0.20f), new Vector3(0.130f, 0.070f, 0.175f),
                Steel, Quaternion.Euler(0f, 0f, -7f));
            Box(t, "Tie", new Vector3(0.005f, 0.115f, 0.15f), new Vector3(0.150f, 0.055f, 0.018f), Gunmetal);
            Box(t, "Lead", new Vector3(0.02f, 0.085f, 0.28f), new Vector3(0.022f, 0.022f, 0.12f), Gunmetal,
                Quaternion.Euler(38f, 0f, 0f));
            Box(t, "Lead", new Vector3(0.02f, 0.045f, 0.34f), new Vector3(0.022f, 0.022f, 0.10f), Gunmetal,
                Quaternion.Euler(74f, 0f, 0f));

            // THE LOOP. Eight tangential bars round a circle, open in the middle. A ring is the
            // one shape that stays legible when the ink outline is the only thing left, and the
            // hole in it is what says "aerial" rather than "barrel".
            float r = HeadSpan(0) * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                var turn = Quaternion.Euler(0f, 0f, a);
                Box(t, "Loop", turn * new Vector3(0f, r, 0f) + new Vector3(0f, 0.025f, 0.635f),
                    new Vector3(0.145f, 0.026f, 0.026f), Galvanised, turn);
            }
            // Two struts holding the loop off the conduit, and the throat between them. At this
            // rung there is exactly ONE lit thing on the weapon.
            Box(t, "Strut", new Vector3(-0.05f, 0.025f, 0.605f), new Vector3(0.020f, 0.020f, 0.10f), Steel,
                Quaternion.Euler(0f, -20f, 0f));
            Box(t, "Strut", new Vector3(0.05f, 0.025f, 0.605f), new Vector3(0.020f, 0.020f, 0.10f), Steel,
                Quaternion.Euler(0f, 20f, 0f));
            Box(t, "Throat", new Vector3(0f, 0.025f, 0.64f), new Vector3(0.055f, 0.055f, 0.035f), Emission);
        }

        /// <summary>
        /// TIER 1. The first one he BUILT. Everything is square to everything else, which after
        /// the Jammer is the loudest thing the object can say.
        /// </summary>
        private void Decryptor(Transform t)
        {
            // A rifle stock, because a man aiming something heavy braces it, and because wood is
            // the one warm material on the weapon. It also puts mass BEHIND his hand, so the
            // silhouette is not all out in front the way the Jammer's is.
            Box(t, "Stock", new Vector3(0f, -0.035f, -0.24f), new Vector3(0.072f, 0.135f, 0.28f), Stock,
                Quaternion.Euler(-7f, 0f, 0f));
            Box(t, "Cheek", new Vector3(0f, 0.045f, -0.16f), new Vector3(0.060f, 0.050f, 0.19f), Stock);
            Box(t, "Grip", new Vector3(0f, -0.115f, -0.035f), new Vector3(0.068f, 0.185f, 0.085f), Gunmetal,
                Quaternion.Euler(13f, 0f, 0f));

            Box(t, "Body", new Vector3(0f, 0.015f, 0.24f), new Vector3(0.112f, 0.140f, 0.58f), Steel);
            Box(t, "Collar", new Vector3(0f, 0.015f, -0.055f), new Vector3(0.130f, 0.150f, 0.055f), Gunmetal);

            // A heatsink, because the fiction is a transmitter that runs hot and because five thin
            // fins give the top edge a rhythm the ink outline can actually draw.
            for (int i = 0; i < 5; i++)
                Box(t, "Fin", new Vector3(0f, 0.105f, 0.07f + i * 0.060f),
                    new Vector3(0.098f, 0.048f, 0.016f), Galvanised);

            // A stepped waveguide horn. Three rings, each wider and shallower than the last: a
            // flare built out of boxes, which under cel shading reads as a horn and under the ink
            // pass reads as a wedge. Narrow -- this rung is about focus.
            float span = HeadSpan(1);
            Box(t, "Horn", new Vector3(0f, 0.015f, 0.575f), new Vector3(span * 0.56f, span * 0.56f, 0.070f),
                Galvanised);
            Box(t, "Horn", new Vector3(0f, 0.015f, 0.650f), new Vector3(span * 0.78f, span * 0.78f, 0.070f),
                Galvanised);
            Box(t, "Horn", new Vector3(0f, 0.015f, 0.730f), new Vector3(span, span, 0.080f), Galvanised);
            Box(t, "Throat", new Vector3(0f, 0.015f, 0.782f), new Vector3(span * 0.62f, span * 0.62f, 0.022f),
                Emission);

            // Two panel lamps on the body: a second and third thing to be lit, which is the cue
            // that the weapon is RUNNING rather than merely pointed.
            Box(t, "Lamp", new Vector3(0.060f, 0.060f, 0.34f), new Vector3(0.016f, 0.030f, 0.055f), Emission);
            Box(t, "Lamp", new Vector3(-0.060f, 0.060f, 0.34f), new Vector3(0.016f, 0.030f, 0.055f), Emission);
            Box(t, "Feed", new Vector3(0f, -0.070f, 0.12f), new Vector3(0.050f, 0.045f, 0.22f), Copper);
        }

        /// <summary>
        /// TIER 2. The longest and the thinnest thing he carries. Three repeater collars down the
        /// shaft and a forked tip: the payload travels ALONG this and splits when it lands, and the
        /// object has to say so before the shot does.
        /// </summary>
        private void WormLance(Transform t)
        {
            Box(t, "Stock", new Vector3(0f, -0.030f, -0.21f), new Vector3(0.062f, 0.115f, 0.24f), Stock,
                Quaternion.Euler(-6f, 0f, 0f));
            Box(t, "Grip", new Vector3(0f, -0.115f, -0.030f), new Vector3(0.062f, 0.180f, 0.080f), Gunmetal,
                Quaternion.Euler(13f, 0f, 0f));
            Box(t, "Breech", new Vector3(0f, 0.015f, 0.055f), new Vector3(0.105f, 0.125f, 0.26f), Steel);

            // The payload canister, slung under the body where a grenade launcher would be. It is
            // the only part of any rung that is plainly a CONSUMABLE, and that is the read: this
            // weapon carries something rather than merely emitting.
            Cylinder(t, "Canister", new Vector3(0f, -0.075f, 0.14f), new Vector3(0.085f, 0.20f, 0.085f),
                     Battery, Quaternion.Euler(90f, 0f, 0f));
            Box(t, "CanisterCap", new Vector3(0f, -0.075f, 0.245f), new Vector3(0.070f, 0.070f, 0.030f), Copper);

            // The shaft. Long, thin, and the whole reason this rung reads as a lance.
            Cylinder(t, "Shaft", new Vector3(0f, 0.020f, 0.62f), new Vector3(0.048f, 0.76f, 0.048f),
                     Galvanised, Quaternion.Euler(90f, 0f, 0f));

            // THREE REPEATER COLLARS, evenly spaced, each with a small amber eye. Evenly spaced on
            // purpose: a rhythm down the shaft is a thing to count, and counting is how a player
            // notices that the shot now has repeats in it too.
            for (int i = 0; i < 3; i++)
            {
                float z = 0.38f + i * 0.26f;
                Box(t, "Collar", new Vector3(0f, 0.020f, z), new Vector3(0.105f, 0.105f, 0.045f), Steel,
                    Quaternion.Euler(0f, 0f, 45f));
                Box(t, "CollarEye", new Vector3(0f, 0.020f, z + 0.026f), new Vector3(0.038f, 0.038f, 0.016f),
                    Emission);
            }

            // The fork. Two tines with the throat between them: the tip is OPEN, like the Jammer's
            // loop, but where the loop was a hole this is a gap that plainly focuses on something.
            float half = HeadSpan(2) * 0.5f;
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -half : half;
                Box(t, "Tine", new Vector3(x, 0.020f, 1.04f), new Vector3(0.024f, 0.024f, 0.20f), Galvanised,
                    Quaternion.Euler(0f, i == 0 ? 3.5f : -3.5f, 0f));
                Box(t, "TineTip", new Vector3(x, 0.020f, 1.145f), new Vector3(0.032f, 0.032f, 0.030f), Copper);
            }
            Box(t, "Throat", new Vector3(0f, 0.020f, 1.07f), new Vector3(0.044f, 0.044f, 0.12f), Emission);
            Box(t, "Fork", new Vector3(0f, 0.020f, 0.955f), new Vector3(HeadSpan(2) + 0.03f, 0.040f, 0.045f),
                Steel);
        }

        /// <summary>
        /// TIER 3. Four horns on a manifold with a capacitor drum behind them. The widest head in
        /// the game, and the only rung that is allowed to look like more than one man's work --
        /// because by this point it is: it is everything he has taken off everything he has passed.
        /// </summary>
        private void CascadeEmitter(Transform t)
        {
            Box(t, "Stock", new Vector3(0f, -0.040f, -0.26f), new Vector3(0.085f, 0.150f, 0.28f), Stock,
                Quaternion.Euler(-8f, 0f, 0f));
            Box(t, "Grip", new Vector3(0f, -0.125f, -0.040f), new Vector3(0.074f, 0.195f, 0.090f), Gunmetal,
                Quaternion.Euler(13f, 0f, 0f));

            // The capacitor drum, lying along the top of the body. Round against a weapon made of
            // boxes, which is what makes it the thing the eye lands on first.
            Cylinder(t, "Drum", new Vector3(0f, 0.130f, 0.06f), new Vector3(0.165f, 0.30f, 0.165f),
                     Galvanised, Quaternion.Euler(90f, 0f, 0f));
            Box(t, "DrumBand", new Vector3(0f, 0.130f, -0.02f), new Vector3(0.180f, 0.180f, 0.035f), Strap);
            Box(t, "DrumBand", new Vector3(0f, 0.130f, 0.15f), new Vector3(0.180f, 0.180f, 0.035f), Strap);
            for (int i = 0; i < 4; i++)
                Box(t, "CoolingFin", new Vector3(0f, 0.235f, -0.06f + i * 0.062f),
                    new Vector3(0.150f, 0.055f, 0.018f), Steel);

            Box(t, "Body", new Vector3(0f, 0.000f, 0.26f), new Vector3(0.150f, 0.150f, 0.56f), Steel);
            Box(t, "Bus", new Vector3(0f, 0.075f, 0.32f), new Vector3(0.060f, 0.055f, 0.34f), Copper);

            // THE ARRAY. Two by two, on a manifold plate, each horn stepped like the Decryptor's
            // so the family resemblance survives -- this is the same man's answer to the same
            // problem four years of scavenging later, not a different weapon.
            float half = HeadSpan(3) * 0.5f;
            Box(t, "Manifold", new Vector3(0f, 0.02f, 0.575f),
                new Vector3(HeadSpan(3), HeadSpan(3) * 0.86f, 0.070f), Gunmetal);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * half * 0.50f;
                float y = 0.02f + (i < 2 ? 0.095f : -0.095f);
                Box(t, "HornBase", new Vector3(x, y, 0.660f), new Vector3(0.150f, 0.150f, 0.075f),
                    Galvanised);
                Box(t, "HornMouth", new Vector3(x, y, 0.750f), new Vector3(0.205f, 0.185f, 0.080f),
                    Galvanised);
                Box(t, "Throat", new Vector3(x, y, 0.800f), new Vector3(0.120f, 0.105f, 0.020f), Emission);
            }

            // A lit strip down each flank. The Jammer had one lamp and this has ten lit faces: how
            // much of the weapon is GLOWING is the cheapest per-rung cue there is, and it is the
            // one that still works when the weapon is thirty pixels across.
            for (int i = 0; i < 3; i++)
            {
                float z = 0.11f + i * 0.145f;
                Box(t, "Strip", new Vector3(0.078f, -0.015f, z), new Vector3(0.014f, 0.040f, 0.085f), Emission);
                Box(t, "Strip", new Vector3(-0.078f, -0.015f, z), new Vector3(0.014f, 0.040f, 0.085f), Emission);
            }
        }

        // ------------------------------------------------------------------ primitives

        private Transform Box(Transform parent, string name, Vector3 localPosition, Vector3 size,
                              Color colour, Quaternion? rotation = null)
            => Primitive(PrimitiveType.Cube, parent, name, localPosition, size, colour, rotation);

        private Transform Cylinder(Transform parent, string name, Vector3 localPosition, Vector3 size,
                                   Color colour, Quaternion? rotation = null)
        {
            // The built-in cylinder is 2 units tall, so halve the height to make size mean size.
            var half = new Vector3(size.x, size.y * 0.5f, size.z);
            return Primitive(PrimitiveType.Cylinder, parent, name, localPosition, half, colour, rotation);
        }

        private Transform Primitive(PrimitiveType kind, Transform parent, string name,
                                    Vector3 localPosition, Vector3 size, Color colour,
                                    Quaternion? rotation)
        {
            var go = GameObject.CreatePrimitive(kind);
            go.name = name;
            DestroyNow(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = _material(colour);
            return go.transform;
        }

        /// <summary>
        /// <c>Object.Destroy</c> THROWS OUTSIDE PLAY MODE, which is the single reason TurretProps
        /// cannot be built inside an EditMode test. Two lines here buy a structural test of the
        /// tier switch -- the one piece of this class that can silently do nothing.
        /// </summary>
        private static void DestroyNow(UnityEngine.Object? victim)
        {
            if (victim == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(victim);
            else UnityEngine.Object.DestroyImmediate(victim);
        }
    }
}
