#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// What an emplacement actually LOOKS like.
    ///
    /// Both families were a cylinder with a cube on top, told apart by colour. That is fine as a
    /// graybox and useless as a game: a player has to read what a thing does from across a field,
    /// and the two families do completely different jobs.
    ///
    /// The fiction decides the objects, and ADR-003 is strict about it. There is no armoury budget
    /// and no gold plating; this is a veteran in a lake community building from what a lake
    /// community has. So:
    ///
    ///   SENTRY   a belt-fed gun on a tripod behind a stack of sandbags, off the county fire
    ///            station. It TRACKS, which is most of what sells a turret as alive, and it has a
    ///            barrel you can see pointing at the thing it is about to kill.
    ///   BRUSH HOG a rotary mower deck on a post, blades at shin height, spun up off a generator.
    ///            A tractor implement, which is the single most plausible area weapon a rural
    ///            property owns, and it reads instantly because the blade disc is visibly spinning.
    ///
    /// Built from primitives like the rest of the art direction: the ink outline is what makes a
    /// silhouette read, and the silhouettes here are deliberately unlike each other.
    /// </summary>
    public sealed class TurretProps
    {
        /// <summary>The part that turns to face a target. Null for families that do not aim.</summary>
        public Transform? Head { get; private set; }

        /// <summary>Where a muzzle flash belongs, in the head's space.</summary>
        public Transform? Muzzle { get; private set; }

        /// <summary>The blade disc, for families that spin.</summary>
        public Transform? Rotor { get; private set; }

        public GameObject Root { get; }

        private static readonly Color Steel = new Color(0.34f, 0.35f, 0.37f);
        private static readonly Color Gunmetal = new Color(0.20f, 0.21f, 0.23f);
        private static readonly Color Sandbag = new Color(0.47f, 0.43f, 0.33f);
        private static readonly Color Tractor = new Color(0.52f, 0.17f, 0.14f);
        private static readonly Color Blade = new Color(0.66f, 0.68f, 0.70f);
        private static readonly Color MuzzleFire = new Color(1f, 0.86f, 0.42f);

        private readonly Func<Color, Material> _material;

        private TurretProps(GameObject root, Func<Color, Material> material)
        {
            Root = root;
            _material = material;
        }

        public static TurretProps Build(bool area, Func<Color, Material> material)
        {
            var root = new GameObject(area ? "BrushHog" : "Sentry");
            var props = new TurretProps(root, material);
            if (area) props.BrushHog(); else props.Sentry();

            // Sandbags on a tripod are exactly the case vertex AO was built for: the darkening
            // where the bags meet each other and the ground is what stops them reading as blocks.
            // Keyed by family, so every sentry on the board shares one bake.
            VertexAo.Bake(root, area ? "BrushHog" : "Sentry");

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

        /// <summary>Sandbags, a tripod, and a gun that points at what it is shooting.</summary>
        private void Sentry()
        {
            var t = Root.transform;

            // A ring of sandbags. Four squashed boxes, each turned a little, reads as a stack.
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 22f;
                var offset = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 0.42f);
                Box(t, "Sandbag", offset + new Vector3(0f, 0.14f, 0f),
                    new Vector3(0.62f, 0.26f, 0.34f), Sandbag, Quaternion.Euler(0f, a, 0f));
            }

            // Tripod. Three legs splayed from a hub, which is the shape that says "mounted weapon".
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                var lean = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(22f, 0f, 0f);
                Box(t, "Leg", Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.32f, 0.16f),
                    new Vector3(0.07f, 0.72f, 0.07f), Steel, lean);
            }

            // The head turns; everything above this point is aimed.
            var head = new GameObject("Head").transform;
            head.SetParent(t, false);
            head.localPosition = new Vector3(0f, 0.72f, 0f);
            Head = head;

            Box(head, "Receiver", new Vector3(0f, 0.04f, 0.06f), new Vector3(0.22f, 0.20f, 0.62f), Gunmetal);
            Box(head, "Barrel", new Vector3(0f, 0.06f, 0.62f), new Vector3(0.09f, 0.09f, 0.78f), Gunmetal);
            Box(head, "Shield", new Vector3(0f, 0.16f, 0.28f), new Vector3(0.62f, 0.34f, 0.05f), Steel);
            Box(head, "AmmoCan", new Vector3(0.22f, -0.02f, -0.06f), new Vector3(0.22f, 0.20f, 0.30f), Sandbag);

            // The flash IS the muzzle object, parked at zero scale. Popping one primitive for two
            // frames is cheaper and reads harder than any particle system would at this distance,
            // and the cel shader bands it into a flat white shape, which is exactly the look.
            Muzzle = Box(head, "Muzzle", new Vector3(0f, 0.06f, 1.10f), Vector3.zero, MuzzleFire);
            Muzzle.GetComponent<Renderer>().sharedMaterial = _material(MuzzleFire);
        }

        /// <summary>A mower deck on a post. The blades are the weapon and they are visible.</summary>
        private void BrushHog()
        {
            var t = Root.transform;

            Box(t, "Post", new Vector3(0f, 0.30f, 0f), new Vector3(0.22f, 0.60f, 0.22f), Steel);
            Box(t, "Motor", new Vector3(0f, 0.72f, -0.18f), new Vector3(0.30f, 0.26f, 0.34f), Gunmetal);

            // The deck: a wide shallow drum, painted like farm equipment because that is what it is.
            var deck = Cylinder(t, "Deck", new Vector3(0f, 0.62f, 0f), new Vector3(1.5f, 0.11f, 1.5f), Tractor);
            Box(deck.parent, "DeckRim", new Vector3(0f, 0.70f, 0f), new Vector3(1.55f, 0.06f, 1.55f), Tractor);

            // Blades, on their own transform so they can spin without the deck spinning.
            var rotor = new GameObject("Rotor").transform;
            rotor.SetParent(t, false);
            rotor.localPosition = new Vector3(0f, 0.50f, 0f);
            Rotor = rotor;

            Box(rotor, "BladeA", Vector3.zero, new Vector3(1.62f, 0.045f, 0.16f), Blade);
            Box(rotor, "BladeB", Vector3.zero, new Vector3(0.16f, 0.045f, 1.62f), Blade);
        }

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

        private Transform Cylinder(Transform parent, string name, Vector3 localPosition, Vector3 size, Color colour)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            // The built-in cylinder is 2 units tall, so halve the height to make size mean size.
            go.transform.localScale = new Vector3(size.x, size.y * 0.5f, size.z);
            go.GetComponent<Renderer>().sharedMaterial = _material(colour);
            return go.transform;
        }
    }
}
