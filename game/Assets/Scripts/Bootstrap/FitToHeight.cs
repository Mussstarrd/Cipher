#nullable enable
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Scales a spawned character to a human height, measured a few frames AFTER it starts moving.
    ///
    /// Timing is the whole point. These clips animate scale on the armature, and the imported models
    /// arrive at a 100x transform scale, so a character measures one height in its bind pose and a
    /// completely different one once the walk is driving it. Every attempt to size them at import
    /// time or on the spawn frame produced either a sixty-metre pedestrian or a quarter-size one,
    /// because the measurement was taken before the thing being measured existed in its real form.
    ///
    /// So: wait for the animation to actually pose the mesh, measure the renderers then, apply the
    /// correction to a PARENT the animation cannot touch, and switch off.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FitToHeight : MonoBehaviour
    {
        [SerializeField] private Transform? scaleTarget;
        [SerializeField] private float targetHeight = 1.8f;

        /// <summary>Frames to let the animation settle before measuring. Two is enough.</summary>
        [SerializeField] private int warmupFrames = 2;

        private int _frames;
        private bool _done;

        public void Configure(Transform target, float height)
        {
            scaleTarget = target;
            targetHeight = Mathf.Max(0.1f, height);
            _done = false;
            _frames = 0;
        }

        private void LateUpdate()
        {
            if (_done || scaleTarget == null) return;
            if (_frames++ < warmupFrames) return;

            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { _done = true; return; }

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            float height = bounds.size.y;
            if (height <= 1e-4f || !float.IsFinite(height)) { _done = true; return; }

            float factor = targetHeight / height;
            if (!float.IsFinite(factor) || factor <= 0f) { _done = true; return; }

            scaleTarget.localScale *= factor;
            _done = true;
        }
    }
}
