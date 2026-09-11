#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// The thing that delivers the airstrike, and the reason it exists in this world at all.
    ///
    /// "Airstrike" was a placeholder borrowed from the retired pitch, where the player was a kingpin
    /// who could call a cartel gunship. ADR-003 deleted that world. Nobody is flying air support for
    /// an unregistered veteran in a lake community — there is no air force on his side, and pretending
    /// otherwise is the kind of hole that makes a premise stop paying rent.
    ///
    /// But ADR-003 already put the answer on the map: "large corporate delivery octocopters still
    /// flying their routes over the collapse because nobody switched them off. Some get weaponised."
    /// So the strike is a HIJACKED CARGO DRONE. It is his one piece of leverage over the network that
    /// took everything else, it explains the cooldown (you get one when one passes overhead), and it
    /// is the same silhouette the player will later learn to fear when HALCYON turns one around.
    ///
    /// Visually it is a slab with four rotor discs, a corporate livery stripe, and an amber underside
    /// light — the same amber as the implants, because it is the same network.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrikeDrone : MonoBehaviour
    {
        /// <summary>Cruise height. High enough to read as overflight, low enough to be a threat.</summary>
        public const float Altitude = 7f;

        /// <summary>How far up the axis it starts, and how far past the end it carries on.</summary>
        private const float Runway = 34f;

        private Transform[] _rotors = Array.Empty<Transform>();
        private Transform? _lamp;
        private Material? _lampMaterial;

        private static readonly Color Shell = new Color(0.78f, 0.78f, 0.76f);
        private static readonly Color Livery = new Color(0.16f, 0.32f, 0.46f);
        private static readonly Color Rotor = new Color(0.22f, 0.22f, 0.24f);
        private static readonly Color AmberOn = new Color(1f, 0.62f, 0.12f);
        private static readonly Color AmberOff = new Color(0.28f, 0.20f, 0.10f);

        public static StrikeDrone Build(Func<Color, Material> material)
        {
            var root = new GameObject("StrikeDrone");
            var drone = root.AddComponent<StrikeDrone>();

            Box(root.transform, "Hull", new Vector3(0f, 0f, 0f), new Vector3(1.5f, 0.42f, 2.6f), Shell, material);
            Box(root.transform, "Livery", new Vector3(0f, 0.23f, 0f), new Vector3(1.52f, 0.10f, 1.1f), Livery, material);
            Box(root.transform, "Pod", new Vector3(0f, -0.34f, 0.2f), new Vector3(0.9f, 0.34f, 1.5f), Livery, material);

            var rotors = new Transform[4];
            int n = 0;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    var arm = Box(root.transform, "Arm",
                                  new Vector3(sx * 0.9f, 0.06f, sz * 1.05f),
                                  new Vector3(0.8f, 0.10f, 0.18f), Shell, material);
                    var disc = new GameObject("Rotor").transform;
                    disc.SetParent(root.transform, false);
                    disc.localPosition = new Vector3(sx * 1.35f, 0.2f, sz * 1.05f);
                    Box(disc, "BladeA", Vector3.zero, new Vector3(1.05f, 0.04f, 0.1f), Rotor, material);
                    Box(disc, "BladeB", Vector3.zero, new Vector3(0.1f, 0.04f, 1.05f), Rotor, material);
                    rotors[n++] = disc;
                    _ = arm;
                }
            drone._rotors = rotors;

            // The amber underside light. Same colour as the implants, because it is the same network.
            drone._lampMaterial = new Material(material(AmberOff));
            var lamp = Box(root.transform, "Lamp", new Vector3(0f, -0.54f, 0.2f),
                           new Vector3(0.42f, 0.10f, 0.42f), AmberOff, material);
            lamp.GetComponent<Renderer>().sharedMaterial = drone._lampMaterial;
            drone._lamp = lamp;

            root.SetActive(false);
            return drone;
        }

        /// <summary>
        /// Places the drone for a run along <paramref name="axis"/> through <paramref name="centre"/>.
        /// <paramref name="t"/> is 0 at the start of the approach and 1 when it has passed.
        /// </summary>
        public void Fly(Vector3 centre, Vector3 axis, float t)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            // FAR END FIRST, toward the player. Two reasons, and the second is the important one.
            //
            // The bombs already walk that way (HeroModel starts at the far end and steps back), so
            // the drone and its payload agree. And the player aims this by LOOKING, which means the
            // line is always in front of them: flown the other way the drone comes in over their own
            // shoulder, passes overhead out of frame, and the delivery vehicle is never seen at all.
            // Coming down the line head-on, it appears small at the far end and grows.
            var dir = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.forward;
            float along = Mathf.Lerp(Runway, -Runway, Mathf.Clamp01(t));
            transform.position = centre + dir * along + Vector3.up * Altitude;
            transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

            float spin = 2600f * Time.deltaTime;
            for (int i = 0; i < _rotors.Length; i++)
                if (_rotors[i] != null)
                    _rotors[i].localRotation *= Quaternion.Euler(0f, (i % 2 == 0 ? spin : -spin), 0f);

            // The lamp strobes only while it is actually over the line and dropping.
            if (_lampMaterial != null)
            {
                bool dropping = t > 0.32f && t < 0.72f;
                var colour = dropping && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? AmberOn : AmberOff;
                _lampMaterial.color = colour;
                if (_lampMaterial.HasProperty(BaseColorId)) _lampMaterial.SetColor(BaseColorId, colour);
            }
            _ = _lamp;
        }

        public void Park()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static Transform Box(Transform parent, string name, Vector3 localPosition, Vector3 size,
                                     Color colour, Func<Color, Material> material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material(colour);
            return go.transform;
        }

        private void OnDestroy()
        {
            if (_lampMaterial != null) Destroy(_lampMaterial);
        }
    }
}
