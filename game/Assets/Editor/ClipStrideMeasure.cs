#nullable enable
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Reports how fast a locomotion clip actually walks, in metres per second.
    ///
    /// Exists because feet were sliding and the cause was a GUESS. SkinnedGait scales playback by
    /// <c>travelSpeed / WalkReference</c>, and WalkReference was set to 1.45 because that seemed
    /// like a walking pace. If the clip was really authored at some other speed, every body in the
    /// game plays its stride at the wrong rate for how far it is actually moving, and the feet skate
    /// -- which is exactly what the owner is looking at.
    ///
    /// The number is not a matter of taste and should never have been typed by hand: it is the root
    /// bone's total forward travel divided by the clip's length, and Unity will tell us both.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.ClipStrideMeasure.Report -quit
    /// </summary>
    public static class ClipStrideMeasure
    {
        private static readonly string[] Clips =
        {
            "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine/Locomotion/Walk/A_Walk_F_Masc.fbx",
            "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine/Locomotion/Run/A_Run_F_Masc.fbx",
        };

        [MenuItem("Cipher/Crowd/Measure Clip Stride")]
        public static void Report()
        {
            foreach (string path in Clips)
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(path)
                                        .OfType<AnimationClip>()
                                        .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (clip == null) { Debug.LogWarning($"[Stride] no clip in {path}"); continue; }

                // averageSpeed is Unity's own answer for a clip's root motion, in metres per second,
                // and it is populated for Humanoid clips whether or not root motion is switched on
                // at runtime. We keep root motion OFF -- the sim owns where a body is -- but the
                // clip's authored pace is still exactly the rate its feet were animated against.
                var v = clip.averageSpeed;
                float planar = new Vector2(v.x, v.z).magnitude;

                Debug.Log($"[Stride] {clip.name}: {clip.length:F2}s, averageSpeed {planar:F2} m/s " +
                          $"(vector {v.x:F2},{v.y:F2},{v.z:F2}), humanMotion={clip.humanMotion}");
            }
        }
    }
}
