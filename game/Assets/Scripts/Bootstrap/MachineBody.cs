#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>Which machine a body is. Chassis and livery; the gait is shared.</summary>
    public enum MachineKind
    {
        /// <summary>Kerbside parcel walker. Cargo box on its back. The broad one.</summary>
        DeliveryWalker = 0,

        /// <summary>Groundskeeping unit off the golf course. Tool rack across its shoulders.</summary>
        Groundskeeper = 1,

        /// <summary>Clubhouse service unit. Narrow, a tray held out in front. The slim one.</summary>
        Attendant = 2,

        /// <summary>The Spitter: a groundskeeper with its herbicide tank still on. Green.</summary>
        Sprayer = 3,
    }

    /// <summary>
    /// The second enemy class, built out of boxes.
    ///
    /// ADR-003 has always had three of these and the game had one. Owner, 2026-09-11: "all I'm
    /// seeing is humans I'm thinking that like 30% of them should be those humanoid robots".
    ///
    /// WHY BOXES AND NOT A MODEL. The free CC0 characters we ship are all one rig, and the walk
    /// cycle that drives them is a legacy clip built by copying 630 curves off that rig by hand --
    /// CLAUDE.md records the five approaches that failed silently before it worked, each leaving a
    /// bind pose that looks exactly like a working import nobody told to move. A robot from any
    /// other pack is a different skeleton, so it would arrive with none of that and cost another
    /// session in the same swamp. A machine does not need a human walk cycle; it needs a machine
    /// one, which is twenty lines of arithmetic. So this follows CLAUDE.md's "BUILT SCENERY IS
    /// BOXES AND THAT IS THE DECISION" and <see cref="TurretProps"/>, and it gets a better result
    /// for it: a hard-surface thing made of hard surfaces, under an ink outline, next to a
    /// guardhouse made the same way.
    ///
    /// WHAT THE PLAYER ACTUALLY HAS TO SEE. At the distance this game is played, the ink outline is
    /// the entire read -- colour greys out in the haze and detail is sub-pixel. So every decision
    /// here is a silhouette decision and they are asserted in <c>MachineBodyTests</c>:
    ///
    ///   NO HEAD.       A person's silhouette is a round head on a narrow neck. A machine's is a
    ///                  flat sensor bar sitting straight on the shoulders. This is the single
    ///                  strongest tell and it survives to a handful of pixels.
    ///   TALLER.        <see cref="Height"/> against a person's 1.8 m. Built to carry, not born.
    ///   SQUARER.       A wide shoulder yoke and a boxy torso, where a person tapers.
    ///   ARMS DOWN.     Straight, parallel, never swinging. Half of "that one is a machine" is
    ///                  that the arms do not move while the legs do.
    ///   LONG SHINS.    A full-length leg pivoting at the hip with no knee.
    ///
    /// The amber sensor bar is ADR-003's, and it is the machines' version of the implant light at
    /// the temple: the same amber, the same "this one is on the network", one pinprick per body.
    /// </summary>
    public static class MachineBody
    {
        // ---- the silhouette contract -----------------------------------------------------------

        /// <summary>Torso: hip to shoulder yoke. Chunky, because this one was built to carry.</summary>
        public const float TorsoHeight = 0.74f;

        /// <summary>The shoulder yoke the sensor block sits straight on top of.</summary>
        public const float YokeHeight = 0.13f;

        /// <summary>Shoulder yoke width. A person's shoulders are nearer 0.45 m.</summary>
        public const float ShoulderSpan = 0.60f;

        /// <summary>Hip height: where the legs pivot. Over half its own height, which a person is not.</summary>
        public const float HipHeight = 1.00f;

        /// <summary>The sensor head is WIDER THAN IT IS TALL. That is the anti-head.</summary>
        public const float HeadWidth = 0.32f;
        public const float HeadHeight = 0.19f;

        /// <summary>The gap between the shoulder yoke and the head. There is no neck.</summary>
        public const float NeckLength = 0f;

        /// <summary>
        /// How tall a service humanoid stands. A person in this game is 1.8 m -- FitToHeight sizes
        /// every civilian to exactly that -- so this is a quarter of a metre of pure silhouette.
        ///
        /// DERIVED, not declared. It was a hand-written 2.06 next to parts that added up to 1.92,
        /// which is the sort of number that is right in the comment and wrong on screen.
        /// </summary>
        public const float Height = HipHeight + TorsoHeight + YokeHeight + HeadHeight;

        // ---- colour ----------------------------------------------------------------------------
        //
        // ADR-003: "scuffed white polymer shells, corporate livery, amber sensor bars". Desaturated
        // on purpose. Orange and green are SPOKEN FOR -- they are the Sapper and the Spitter, the
        // two colours a player has already been taught to read across a map -- so the ordinary
        // machines must not compete for them.

        private static readonly Color Polymer = new Color(0.69f, 0.68f, 0.65f);
        private static readonly Color Scuff = new Color(0.52f, 0.51f, 0.49f);
        private static readonly Color Joint = new Color(0.26f, 0.26f, 0.28f);
        private static readonly Color Livery = new Color(0.42f, 0.20f, 0.22f);
        private static readonly Color Municipal = new Color(0.50f, 0.54f, 0.50f);
        private static readonly Color Cream = new Color(0.72f, 0.67f, 0.57f);

        /// <summary>
        /// The amber. Same idea as the implant at the temple: small, bright, one per body, and the
        /// only warm thing on an otherwise grey object.
        /// </summary>
        public static readonly Color Amber = new Color(1f, 0.66f, 0.12f);

        /// <summary>
        /// The Spitter's green, and it is <em>the capsule's</em> green. The pill it replaces was
        /// (0.35, 0.9, 0.25) and a player has spent five waves learning that green means "this one
        /// is coming for your emplacements". Changing the hue while changing the shape would throw
        /// that away for nothing.
        /// </summary>
        public static readonly Color SprayerGreen = new Color(0.35f, 0.9f, 0.25f);

        private static Color ShellOf(MachineKind kind) => kind switch
        {
            MachineKind.Groundskeeper => Municipal,
            MachineKind.Attendant => Cream,
            MachineKind.Sprayer => SprayerGreen * 0.62f,
            _ => Polymer,
        };

        /// <summary>Torso width. The Attendant is the slim one; the Walker is the broad one.</summary>
        public static float TorsoWidthOf(MachineKind kind) => kind switch
        {
            MachineKind.Attendant => 0.34f,
            MachineKind.DeliveryWalker => 0.46f,
            _ => 0.40f,
        };

        /// <summary>
        /// Builds one machine under <paramref name="parent"/> and returns its root.
        ///
        /// Three children, named, because <see cref="MachineGait"/> finds them by name after the
        /// prototype is instantiated: <c>Chassis</c>, <c>LegL</c>, <c>LegR</c>. Everything rigid
        /// hangs off Chassis so the whole upper body is one transform to bob and roll.
        /// </summary>
        public static GameObject Build(MachineKind kind, Transform? parent, Func<Color, Material> material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));

            var root = new GameObject("Machine_" + kind);
            if (parent != null) root.transform.SetParent(parent, false);

            Color shell = ShellOf(kind);
            float torso = TorsoWidthOf(kind);

            // ---- chassis: everything above the hips, one rigid piece --------------------------
            var chassis = new GameObject("Chassis").transform;
            chassis.SetParent(root.transform, false);
            chassis.localPosition = new Vector3(0f, HipHeight, 0f);

            // Heights below are RELATIVE TO THE HIP, so the chassis pivot is the hip and a bob or
            // a roll happens about the hip the way a walking machine's does.
            const float torsoH = TorsoHeight;
            const float yokeH = YokeHeight;
            float torsoMid = torsoH * 0.5f;
            float yokeMid = torsoH + yokeH * 0.5f;
            float headMid = torsoH + yokeH + HeadHeight * 0.5f + NeckLength;

            Box(chassis, "Torso", new Vector3(0f, torsoMid, 0f),
                new Vector3(torso, torsoH, 0.28f), shell, material);

            // A dark chest panel and a dark waist. WITHOUT THESE THE WHOLE UPPER BODY IS ONE PALE
            // VALUE and the machine reads as a filing cabinet on stilts -- which is what the first
            // render looked like. Under a cel shader the only modelling available is a change of
            // colour, so the panel lines ARE the detailing.
            Box(chassis, "Panel", new Vector3(0f, torsoMid + 0.07f, 0.145f),
                new Vector3(torso * 0.62f, torsoH * 0.42f, 0.02f), Joint, material);
            Box(chassis, "Waist", new Vector3(0f, 0.05f, 0f),
                new Vector3(torso * 0.94f, 0.10f, 0.30f), Joint, material);

            Box(chassis, "Yoke", new Vector3(0f, yokeMid, 0f),
                new Vector3(ShoulderSpan, yokeH, 0.30f), Scuff, material);

            // THE ANTI-HEAD. Wider than tall, flat, straight onto the shoulders.
            Box(chassis, "Sensor", new Vector3(0f, headMid, 0.01f),
                new Vector3(HeadWidth, HeadHeight, 0.21f), Scuff, material);

            // A dark face under the bar, so the head is a device with a lens rather than a blank
            // brick, and the amber has something to sit against.
            Box(chassis, "Face", new Vector3(0f, headMid - 0.04f, 0.108f),
                new Vector3(HeadWidth * 0.86f, HeadHeight * 0.42f, 0.02f), Joint, material);

            // The one warm pinprick, on the front face of the sensor block.
            Box(chassis, "Bar", new Vector3(0f, headMid + 0.045f, 0.115f),
                new Vector3(HeadWidth * 0.80f, 0.045f, 0.03f), Amber, material);

            // Arms: straight, parallel, and they never move. A machine that swings its arms reads
            // as a man in a costume. Set OUTSIDE the torso with daylight between, or the arm and
            // the body merge into one wide slab and the silhouette loses its waist.
            float armX = ShoulderSpan * 0.5f + 0.055f;
            for (int s = -1; s <= 1; s += 2)
            {
                Box(chassis, s < 0 ? "ArmL" : "ArmR",
                    new Vector3(armX * s, torsoH - 0.34f, 0f),
                    new Vector3(0.085f, 0.62f, 0.11f), Joint, material);
                Box(chassis, s < 0 ? "ShoulderL" : "ShoulderR",
                    new Vector3(armX * s, torsoH - 0.02f, 0f),
                    new Vector3(0.13f, 0.13f, 0.16f), Scuff, material);
            }

            Dress(kind, chassis, torsoH, material);

            // ---- legs: full length, pivoting at the hip, no knee -------------------------------
            float legLength = HipHeight - 0.06f;         // the foot takes the last 6 cm
            for (int s = -1; s <= 1; s += 2)
            {
                var leg = new GameObject(s < 0 ? "LegL" : "LegR").transform;
                leg.SetParent(root.transform, false);
                leg.localPosition = new Vector3(0.13f * s, HipHeight, 0f);

                Box(leg, "Shin", new Vector3(0f, -legLength * 0.5f, 0f),
                    new Vector3(0.15f, legLength, 0.17f), Joint, material);
                Box(leg, "Foot", new Vector3(0f, -legLength - 0.03f, 0.05f),
                    new Vector3(0.20f, 0.07f, 0.32f), Scuff, material);
            }

            // Every machine of a kind is geometrically identical, so one bake serves all of them.
            VertexAo.Bake(root, "Machine_" + kind);
            return root;
        }

        /// <summary>The one thing that tells the four chassis apart up close.</summary>
        private static void Dress(MachineKind kind, Transform chassis, float torsoH,
                                  Func<Color, Material> material)
        {
            switch (kind)
            {
                case MachineKind.DeliveryWalker:
                    // A parcel box still strapped on. Broadens the silhouette from behind.
                    Box(chassis, "Cargo", new Vector3(0f, torsoH * 0.62f, -0.26f),
                        new Vector3(0.44f, 0.50f, 0.24f), Scuff, material);
                    Box(chassis, "Stripe", new Vector3(0f, torsoH * 0.62f, -0.385f),
                        new Vector3(0.46f, 0.10f, 0.02f), Livery, material);
                    break;

                case MachineKind.Groundskeeper:
                    // A tool rack across the shoulders: a horizontal bar where a person has none.
                    Box(chassis, "Rack", new Vector3(0f, torsoH + 0.20f, -0.20f),
                        new Vector3(0.74f, 0.06f, 0.06f), Joint, material);
                    Box(chassis, "Hopper", new Vector3(0f, torsoH * 0.45f, -0.24f),
                        new Vector3(0.38f, 0.34f, 0.20f), Scuff, material);
                    break;

                case MachineKind.Attendant:
                    // A tray held out in front, waist high. Reads instantly and reads as service.
                    Box(chassis, "Tray", new Vector3(0f, torsoH * 0.52f, 0.30f),
                        new Vector3(0.42f, 0.03f, 0.30f), Scuff, material);
                    break;

                case MachineKind.Sprayer:
                    // THE SPITTER. A pressure tank on its back and a wand slung forward, both in
                    // the capsule's green, so the thing the player learned to run from is still
                    // the thing he sees -- now with a shape attached to it.
                    Cylinder(chassis, "Tank", new Vector3(0f, torsoH * 0.60f, -0.27f),
                             new Vector3(0.30f, 0.54f, 0.30f), SprayerGreen, material);
                    Box(chassis, "Wand", new Vector3(0.10f, torsoH * 0.30f, 0.30f),
                        new Vector3(0.06f, 0.06f, 0.62f), SprayerGreen, material,
                        Quaternion.Euler(14f, 0f, 0f));
                    Box(chassis, "Nozzle", new Vector3(0.10f, torsoH * 0.30f + 0.08f, 0.60f),
                        new Vector3(0.10f, 0.10f, 0.10f), Amber, material);
                    break;
            }
        }

        // ---- primitives ------------------------------------------------------------------------
        //
        // Same shape as TurretProps', deliberately: colliders off (nothing here is ever touched by
        // physics, and 50 machines' worth of box colliders is a physics scene rebuild for nothing),
        // and size means size.

        private static Transform Box(Transform parent, string name, Vector3 localPosition,
                                     Vector3 size, Color colour, Func<Color, Material> material,
                                     Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Kill(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material(colour);
            return go.transform;
        }

        private static Transform Cylinder(Transform parent, string name, Vector3 localPosition,
                                          Vector3 size, Color colour, Func<Color, Material> material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Kill(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            // The built-in cylinder is two units tall, so halve it and size means size.
            go.transform.localScale = new Vector3(size.x, size.y * 0.5f, size.z);
            go.GetComponent<Renderer>().sharedMaterial = material(colour);
            return go.transform;
        }

        private static void Kill(UnityEngine.Object? o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }
    }

    /// <summary>
    /// Makes machines, one prototype per kind, and clones the rest.
    ///
    /// Two reasons this is not a bare static call per body. First, MATERIALS: the bootstrap's
    /// material factory mints a new <see cref="Material"/> every call, and fifty machines built
    /// independently would be six hundred materials that the SRP batcher cannot group -- so the
    /// colours are cached here and shared across every machine in the pool. Second, BUILD COST:
    /// a machine is fourteen primitives, and <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>
    /// of a finished prototype is far cheaper than fourteen <c>CreatePrimitive</c> calls, shares the
    /// meshes, and -- the part that matters -- shares the vertex-AO bake.
    /// </summary>
    public sealed class MachineFactory
    {
        private readonly Func<Color, Material> _source;
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();
        private readonly Dictionary<MachineKind, GameObject> _prototypes =
            new Dictionary<MachineKind, GameObject>();

        /// <summary>Where the disabled prototypes live, out of the way of everything.</summary>
        private readonly Transform? _attic;

        public MachineFactory(Func<Color, Material> material, Transform? attic = null)
        {
            _source = material ?? throw new ArgumentNullException(nameof(material));
            _attic = attic;
        }

        /// <summary>Distinct materials minted so far. One per colour, not one per part.</summary>
        public int MaterialCount => _materials.Count;

        /// <summary>Prototypes built so far. At most one per <see cref="MachineKind"/>.</summary>
        public int PrototypeCount => _prototypes.Count;

        private Material Shared(Color c)
        {
            if (!_materials.TryGetValue(c, out var m)) _materials[c] = m = _source(c);
            return m;
        }

        /// <summary>
        /// One machine, parented to <paramref name="parent"/>, ready to be driven by
        /// <see cref="MachineGait"/>.
        /// </summary>
        public GameObject Create(MachineKind kind, Transform parent)
        {
            if (!_prototypes.TryGetValue(kind, out var prototype))
            {
                prototype = MachineBody.Build(kind, _attic, Shared);
                prototype.SetActive(false);
                _prototypes[kind] = prototype;
            }

            var clone = UnityEngine.Object.Instantiate(prototype, parent);
            clone.name = prototype.name;
            clone.SetActive(true);
            return clone;
        }
    }
}
