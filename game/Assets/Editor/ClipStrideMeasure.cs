#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Reports how fast each locomotion clip actually moves the body, in metres per second.
    ///
    /// SECOND VERSION, because the first one measured nothing. It read the root bone's travel over
    /// the clip -- which is the right idea for a root-motion clip and reports exactly 0.00 m/s for
    /// these, because Synty's locomotion is animated IN PLACE. The tool printed a zero, nobody
    /// noticed that a zero is not a measurement, and the blend-tree thresholds (1.5 walk, 3.6 run)
    /// stayed the guesses they had always been. The owner's report was the exact symptom of a
    /// guessed threshold: "everybody was just constantly walking in place and sliding across the
    /// map".
    ///
    /// An in-place clip still contains its ground speed -- in the FEET. While a foot is planted
    /// the body moves over it, which in an in-place clip shows up as that foot sweeping BACKWARD
    /// relative to the hips at exactly the ground speed. So: sample the clip on a Humanoid body,
    /// track each foot's position along the body's forward axis relative to the hips, and read the
    /// stance-phase backward velocity. That number is the speed the clip was authored at, and it
    /// is the ONLY value the blend threshold for that clip can honestly take.
    ///
    /// Two estimates are printed. VELOCITY is the one to use: the plateau of backward foot speed
    /// during stance. EXCURSION (total foot sweep per cycle divided by cycle time) is a sanity
    /// check that runs a little high on walks because double-support gets counted twice.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.ClipStrideMeasure.Report -quit
    /// </summary>
    public static class ClipStrideMeasure
    {
        private const string AnimDir = "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine";
        private const int Samples = 120;

        [MenuItem("Cipher/Crowd/Measure Clip Stride")]
        public static void Report()
        {
            var body = FindHumanoidBody();
            if (body == null)
            {
                Debug.LogError("[Stride] no Humanoid civilian prefab in Resources/Civilians to sample on");
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(body);
            go.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var anim = go.GetComponentInChildren<Animator>(true);
                var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                var lf = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                var rf = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                if (hips == null || lf == null || rf == null)
                {
                    Debug.LogError($"[Stride] {body.name}: avatar is missing hips or a foot");
                    return;
                }

                foreach (string path in ForwardClips())
                {
                    var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                                            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                    if (clip == null) { Debug.LogWarning($"[Stride] no clip in {path}"); continue; }
                    Measure(go, anim.gameObject, clip, hips, lf, rf);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void Measure(GameObject root, GameObject animated, AnimationClip clip,
                                    Transform hips, Transform lf, Transform rf)
        {
            var zl = new float[Samples];
            var zr = new float[Samples];
            float dt = clip.length / Samples;

            AnimationMode.StartAnimationMode();
            try
            {
                for (int k = 0; k < Samples; k++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(animated, clip, k * dt);
                    AnimationMode.EndSampling();

                    // The ROOT's forward, not the hips': the pelvis yaws every step and would put
                    // a wobble on the axis we are measuring along.
                    Vector3 fwd = root.transform.forward;
                    zl[k] = Vector3.Dot(lf.position - hips.position, fwd);
                    zr[k] = Vector3.Dot(rf.position - hips.position, fwd);
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            float excL = zl.Max() - zl.Min();
            float excR = zr.Max() - zr.Min();
            float byExcursion = (excL + excR) / clip.length;

            // Backward foot velocity relative to hips. During stance the planted foot slides back at
            // the ground speed; during swing it comes forward fast. Keep the backward samples and
            // take the upper-middle of them as the stance plateau -- the median of the top half --
            // which ignores the slow start and end of each stance without being fooled by a spike.
            var back = new List<float>(Samples * 2);
            for (int k = 1; k < Samples; k++)
            {
                float vl = (zl[k] - zl[k - 1]) / dt;
                float vr = (zr[k] - zr[k - 1]) / dt;
                if (vl < 0f) back.Add(-vl);
                if (vr < 0f) back.Add(-vr);
            }
            float byVelocity = 0f;
            if (back.Count > 0)
            {
                back.Sort();
                int lo = back.Count / 2;
                byVelocity = back[lo + (back.Count - lo) / 2];
            }

            Debug.Log($"[Stride] {clip.name}: {clip.length:F2}s | VELOCITY {byVelocity:F2} m/s " +
                      $"| excursion {byExcursion:F2} m/s (L {excL:F2}m, R {excR:F2}m)");
        }

        /// <summary>Every forward-facing masculine locomotion clip, plus standing idle as a zero check.</summary>
        private static IEnumerable<string> ForwardClips()
        {
            yield return AnimDir + "/Idle/A_Idle_Standing_Masc.fbx";
            string loco = AnimDir + "/Locomotion";
            if (!Directory.Exists(loco)) yield break;
            foreach (string dir in Directory.GetDirectories(loco).OrderBy(d => d))
                foreach (string f in Directory.GetFiles(dir, "A_*_F_Masc.fbx").OrderBy(f => f))
                    yield return f.Replace('\\', '/');
        }

        private static GameObject? FindHumanoidBody()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Civilians" }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var a = go != null ? go.GetComponentInChildren<Animator>(true) : null;
                if (a != null && a.avatar != null && a.avatar.isHuman) return go;
            }
            return null;
        }
    }
}
