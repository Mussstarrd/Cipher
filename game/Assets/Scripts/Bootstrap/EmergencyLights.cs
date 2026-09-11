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
        public static EmergencyLights Attach(GameObject car, float roofHeight, System.Func<Color, Material> material)
        {
            var lights = car.AddComponent<EmergencyLights>();
            lights._left = material(Red);
            lights._right = material(Blue);

            // Its own materials, not shared ones: these two get written every frame.
            lights._left = new Material(lights._left);
            lights._right = new Material(lights._right);

            lights.Bar("LightBarL", car.transform, new Vector3(-0.30f, roofHeight, 0f), lights._left);
            lights.Bar("LightBarR", car.transform, new Vector3(0.30f, roofHeight, 0f), lights._right);
            lights._phase = Random.value * lights.Period;   // no two cars blink together
            return lights;
        }

        private void Bar(string name, Transform parent, Vector3 localPosition, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(0.55f, 0.16f, 0.34f);
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
            if (_right != null) Destroy(_right);
        }
    }
}
