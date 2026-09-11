#nullable enable
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// A light bar that is still running on a car nobody is coming back to.
    ///
    /// Owner asked for it, and it is worth more than it costs. In a level lit by flat overcast noon,
    /// two saturated colours alternating is the only moving light in the scene, and a cruiser
    /// abandoned with its bar still going says more about what happened here than a paragraph of
    /// brief would. It is also a landmark: players navigate by the thing that blinks.
    ///
    /// Deliberately not a real light. Two emissive bars, alternating, no shadow cost, no draw-call
    /// cost worth measuring, and it reads correctly under the ink shader because the shader bands
    /// colour rather than blending it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EmergencyLights : MonoBehaviour
    {
        /// <summary>Full cycle: red, then blue. Real bars run about this fast.</summary>
        public float Period { get; set; } = 0.9f;

        private static readonly Color Red = new Color(0.95f, 0.12f, 0.12f);
        private static readonly Color Blue = new Color(0.16f, 0.38f, 1f);
        private static readonly Color Dark = new Color(0.16f, 0.15f, 0.16f);

        private Material? _left;
        private Material? _right;
        private float _phase;

        /// <summary>
        /// Adds a bar to a vehicle. <paramref name="material"/> makes one material per colour so the
        /// two halves can be driven independently without touching anything else in the scene.
        /// </summary>
        /// <summary>
        /// Adds a bar to a vehicle, MEASURED onto its roof.
        ///
        /// The first version took a roof height as a number and I passed 1.55, which is a plausible
        /// roof height for a car and meaningless in this car's local space: an imported FBX carries
        /// its own scale, so the bar floated in the air above the cruiser with no visible connection
        /// to it. Same lesson as the road tile and the crowd height -- measure the model, never
        /// assume its units.
        /// </summary>
        public static EmergencyLights Attach(GameObject car, System.Func<Color, Material> material)
        {
            var lights = car.AddComponent<EmergencyLights>();

            // Mesh bounds in the CAR's own space, so the numbers are in the same units as the
            // localPosition we are about to set.
            var toLocal = car.transform.worldToLocalMatrix;
            bool any = false;
            var box = new Bounds();
            foreach (var filter in car.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (mesh == null) continue;
                var m = toLocal * filter.transform.localToWorldMatrix;
                var c = mesh.bounds.center;
                var e = mesh.bounds.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = m.MultiplyPoint3x4(new Vector3(
                        c.x + ((i & 1) == 0 ? -e.x : e.x),
                        c.y + ((i & 2) == 0 ? -e.y : e.y),
                        c.z + ((i & 4) == 0 ? -e.z : e.z)));
                    if (!any) { box = new Bounds(corner, Vector3.zero); any = true; }
                    else box.Encapsulate(corner);
                }
            }
            if (!any) box = new Bounds(Vector3.zero, Vector3.one);

            // THE MODEL ALREADY HAS A LIGHT BAR. Use it rather than building a second one.
            //
            // The owner reported "two sets of lights flashing" -- because there were. `Cop.fbx`
            // ships a node called `Lights` on its roof, and this method was bolting our own bar on
            // top of it. The first version of this bug was our bar floating in the air (fixed by
            // measuring the roof); the second was our bar sitting neatly beside the one that was
            // already there, which is harder to see in a screenshot and just as wrong.
            //
            // **Before adding geometry to an imported model, look for what the model already has.**
            var existing = FindLightBar(car.transform);
            if (existing != null)
            {
                // Its own material instances: these get written every frame, and the prop reskin
                // hands out SHARED materials that half the map is also using.
                var slots = existing.sharedMaterials;
                var owned = new Material[slots.Length];
                for (int i = 0; i < slots.Length; i++)
                    owned[i] = material(i % 2 == 0 ? Red : Blue);
                existing.sharedMaterials = owned;

                lights._left = owned[0];
                lights._right = owned.Length > 1 ? owned[1] : owned[0];
                // One slot means one bar that can only alternate as a whole, which still reads at
                // distance -- plenty of real bars do exactly that.
                lights._single = owned.Length <= 1;
                lights._phase = Random.value * lights.Period;
                return lights;
            }

            float roofHeight = box.max.y;
            float barWidth = box.size.x * 0.34f;
            float barSpan = box.size.x * 0.20f;
            float barDepth = box.size.z * 0.09f;
            float barThick = Mathf.Max(box.size.y * 0.07f, 0.02f);
            lights._left = material(Red);
            lights._right = material(Blue);

            // Its own materials, not shared ones: these two get written every frame.
            lights._left = new Material(lights._left);
            lights._right = new Material(lights._right);

            // Sized from the car as well: a fixed-size bar is a bar that fits exactly one model.
            var size = new Vector3(barWidth, barThick, barDepth);
            lights.Bar("LightBarL", car.transform, new Vector3(-barSpan, roofHeight + barThick * 0.5f, box.center.z), lights._left, size);
            lights.Bar("LightBarR", car.transform, new Vector3(barSpan, roofHeight + barThick * 0.5f, box.center.z), lights._right, size);
            lights._phase = Random.value * lights.Period;   // no two cars blink together
            return lights;
        }

        /// <summary>
        /// The light bar an imported vehicle already carries, or null. Matched by node name, which
        /// is the only handle a kit model reliably gives you -- there is no convention for which
        /// submesh is emissive and guessing by colour breaks the moment the prop reskin runs.
        /// </summary>
        private static Renderer? FindLightBar(Transform root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                string n = r.gameObject.name;
                if (n.IndexOf("light", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("siren", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return r;
            }
            return null;
        }

        /// <summary>True when the whole bar alternates together rather than half at a time.</summary>
        private bool _single;

        private void Bar(string name, Transform parent, Vector3 localPosition, Material mat, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void Update()
        {
            if (_left == null || _right == null) return;

            _phase += Time.deltaTime;
            if (_phase >= Period) _phase -= Period;

            // Hard alternation rather than a fade: a strobe is a square wave, and the cel shader
            // would band a fade into a square wave anyway.
            bool leftOn = _phase < Period * 0.5f;
            if (_single)
            {
                Paint(_left, leftOn ? Red : Blue);
                return;
            }
            Paint(_left, leftOn ? Red : Dark);
            Paint(_right, leftOn ? Dark : Blue);
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static void Paint(Material mat, Color colour)
        {
            mat.color = colour;
            if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, colour);
        }

        private void OnDestroy()
        {
            if (_left != null) Destroy(_left);
            if (_right != null && !ReferenceEquals(_right, _left)) Destroy(_right);
        }
    }
}
