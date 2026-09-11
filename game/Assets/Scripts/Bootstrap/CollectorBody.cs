#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// A Collector, built from primitives. ADR-011.
    ///
    /// The owner's reference is the one the very first concept pass landed and he picked out: "a dud
    /// that looked like the butcher from Diablo, all huge belly and like 3 times the size of a
    /// person". So: enormous, distended, top-heavy, a head taller than the tallest civilian, and
    /// **wide** — width is what actually sells mass at distance, because the ink outline only gives
    /// you a silhouette and a tall thin thing reads as a tall person.
    ///
    /// Primitives rather than a model, for the reason CLAUDE.md already gives about built scenery:
    /// the free kits have nothing like this, and a photoreal monster standing next to a boxy
    /// guardhouse would look worse than a well-shaped box does. What carries it is proportion.
    ///
    /// **Red eyes.** ADR-003 reserved them for lab releases and said we spend them almost never.
    /// This is the almost-never. Nothing else in this game has them, and nothing else should.
    ///
    /// A GameObject rather than an instanced draw on purpose: there are one or two of these on a
    /// field, they are the thing the player is looking at, and they want real shadows.
    /// </summary>
    public sealed class CollectorBody : MonoBehaviour
    {
        /// <summary>Roughly a head taller than a civilian, and far wider.</summary>
        public const float Height = 2.75f;

        private Transform? _belly;
        private Material? _eyeMaterial;
        private float _bob;

        public static CollectorBody Build(Func<Color, Material> material)
        {
            var root = new GameObject("Collector");
            var body = root.AddComponent<CollectorBody>();

            var hide = material(new Color(0.47f, 0.40f, 0.36f));   // grey, bloodless, wrong
            var dark = material(new Color(0.20f, 0.18f, 0.18f));
            body._eyeMaterial = material(new Color(1f, 0.10f, 0.08f));

            // The belly is the silhouette. Everything else hangs off it.
            var belly = Part(root.transform, PrimitiveType.Sphere, "Belly",
                             new Vector3(0f, Height * 0.42f, 0f),
                             new Vector3(1.55f, 1.30f, 1.45f), hide);
            body._belly = belly;

            // Shoulders far wider than a person's, set low and forward so it reads as hunched.
            Part(root.transform, PrimitiveType.Sphere, "ShoulderL",
                 new Vector3(-0.86f, Height * 0.70f, -0.05f), new Vector3(0.62f, 0.52f, 0.62f), hide);
            Part(root.transform, PrimitiveType.Sphere, "ShoulderR",
                 new Vector3(0.86f, Height * 0.70f, -0.05f), new Vector3(0.62f, 0.52f, 0.62f), hide);

            // Arms long enough to hang past the belly: the proportion that says "not a man".
            Part(root.transform, PrimitiveType.Capsule, "ArmL",
                 new Vector3(-0.96f, Height * 0.40f, 0.02f), new Vector3(0.30f, 0.60f, 0.30f), hide);
            Part(root.transform, PrimitiveType.Capsule, "ArmR",
                 new Vector3(0.96f, Height * 0.40f, 0.02f), new Vector3(0.30f, 0.60f, 0.30f), hide);

            // Short, thick, planted. A heavy thing on small legs is what makes a gait read as slow.
            Part(root.transform, PrimitiveType.Capsule, "LegL",
                 new Vector3(-0.34f, Height * 0.17f, 0f), new Vector3(0.36f, 0.46f, 0.36f), dark);
            Part(root.transform, PrimitiveType.Capsule, "LegR",
                 new Vector3(0.34f, Height * 0.17f, 0f), new Vector3(0.36f, 0.46f, 0.36f), dark);

            // A small head on a big body. Sunk between the shoulders, almost no neck.
            var head = Part(root.transform, PrimitiveType.Sphere, "Head",
                            new Vector3(0f, Height * 0.83f, 0.06f),
                            new Vector3(0.46f, 0.44f, 0.46f), hide);

            // The eyes. Small, forward, and the only saturated red in the game.
            Part(head, PrimitiveType.Sphere, "EyeL", new Vector3(-0.26f, 0.10f, 0.42f),
                 new Vector3(0.22f, 0.22f, 0.16f), body._eyeMaterial);
            Part(head, PrimitiveType.Sphere, "EyeR", new Vector3(0.26f, 0.10f, 0.42f),
                 new Vector3(0.22f, 0.22f, 0.16f), body._eyeMaterial);

            root.SetActive(false);
            return body;
        }

        private static Transform Part(Transform parent, PrimitiveType type, string name,
                                      Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        /// <summary>
        /// Places it and gives it a heavy, slow roll. Called every frame from the bootstrap with the
        /// agent's position, so the body owns none of the simulation.
        /// </summary>
        public void Follow(Vector3 position, Vector3 facing, float integrity01)
        {
            transform.position = position;
            if (facing.sqrMagnitude > 1e-5f)
                transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);

            // A slow lateral roll rather than a bounce: weight shifting from one leg to the other.
            _bob += Time.deltaTime;
            float roll = Mathf.Sin(_bob * 2.6f) * 4.5f;
            transform.rotation *= Quaternion.Euler(0f, 0f, roll);

            // The belly swings a beat behind the body, which is most of what sells the mass.
            if (_belly != null)
                _belly.localPosition = new Vector3(Mathf.Sin(_bob * 2.6f - 0.7f) * 0.07f,
                                                   Height * 0.42f, 0f);

            // The eyes go out as it dies. Nothing else in the game has red in it, so this is a
            // legible health read from any distance without drawing a bar.
            if (_eyeMaterial != null)
            {
                var lit = Color.Lerp(new Color(0.24f, 0.05f, 0.05f), new Color(1f, 0.10f, 0.08f),
                                     Mathf.Clamp01(integrity01));
                _eyeMaterial.color = lit;
                if (_eyeMaterial.HasProperty(BaseColorId)) _eyeMaterial.SetColor(BaseColorId, lit);
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    }
}
