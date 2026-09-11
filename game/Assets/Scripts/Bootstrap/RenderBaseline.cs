#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Puts the renderer back the way it was before the Phase D pass, so a before/after pair can be
    /// captured from ONE build.
    ///
    ///     ProjectExodus.exe -exodus-render-baseline -exodus-screenshot before.png
    ///
    /// Two builds of two different commits would differ in more than the thing being judged: a
    /// different frame, a different spawn, a different drone overhead. Same exe, same seed, same
    /// delay, one switch, and the only thing that moves between the two images is the feature.
    ///
    /// It reverses, rather than removes:
    ///   - the sun goes back to hard shadows (URP takes the soft keyword from the light when the
    ///     platform supports per-light quality, which desktop does),
    ///   - _AmbientStrength goes to zero on every material, which is exactly what "InstancedLit
    ///     never samples SH" meant,
    ///   - _VertexAoStrength goes to zero, which is the shader's own default,
    ///   - and -exodus-no-groundmarks stops the blob shadows and decals being installed at all.
    ///
    /// DIAGNOSTIC ONLY. Nothing runs unless the flag is on the command line, and nothing here is
    /// reachable from gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RenderBaseline : MonoBehaviour
    {
        /// <summary>Reverts everything this class knows how to revert.</summary>
        public const string Arg = "-exodus-render-baseline";

        /// <summary>
        /// And one flag per fix, because a combined before/after cannot say which change did what.
        /// Judging four things from one pair of images is how a regression rides in behind an
        /// improvement.
        /// </summary>
        public const string ShadowsArg = "-exodus-baseline-shadows";
        public const string AmbientArg = "-exodus-baseline-ambient";
        public const string AoArg = "-exodus-baseline-ao";

        /// <summary>
        /// Materials are created all through startup and again when a position loads, so one pass
        /// at frame zero would miss most of them. Re-applied for this long instead.
        /// </summary>
        private const float ReapplyFor = 25f;

        private static readonly int AmbientStrengthId = Shader.PropertyToID("_AmbientStrength");
        private static readonly int VertexAoStrengthId = Shader.PropertyToID("_VertexAoStrength");

        private float _elapsed;
        private float _nextPass;
        private bool _shadows;
        private bool _ambient;
        private bool _ao;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            bool all = Has(args, Arg);
            bool shadows = all || Has(args, ShadowsArg);
            bool ambient = all || Has(args, AmbientArg);
            bool ao = all || Has(args, AoArg);
            if (!shadows && !ambient && !ao) return;

            var host = new GameObject("RenderBaseline");
            DontDestroyOnLoad(host);
            var baseline = host.AddComponent<RenderBaseline>();
            baseline._shadows = shadows;
            baseline._ambient = ambient;
            baseline._ao = ao;
            Debug.Log($"[Baseline] reverted: shadows={shadows} ambient={ambient} vertexAo={ao}");
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed > ReapplyFor) { enabled = false; return; }
            if (_elapsed < _nextPass) return;
            _nextPass = _elapsed + 0.5f;

            if (_shadows)
                foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (light.shadows == LightShadows.Soft) light.shadows = LightShadows.Hard;

            if (!_ambient && !_ao) return;

            // FindObjectsOfTypeAll rather than a scene search: every material in this game is made
            // at runtime and assigned to a shared renderer or handed straight to DrawMeshInstanced,
            // so most of them are not reachable by walking the hierarchy.
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (mat == null) continue;
                if (_ambient && mat.HasProperty(AmbientStrengthId)) mat.SetFloat(AmbientStrengthId, 0f);
                if (_ao && mat.HasProperty(VertexAoStrengthId)) mat.SetFloat(VertexAoStrengthId, 0f);
            }
        }

        private static bool Has(string[] args, string flag)
        {
            if (args == null) return false;
            foreach (var a in args)
                if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
