#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Turns the Quaternius robot FBX into a prefab the crowd can wear, with its own walk on it.
    ///
    /// This is <see cref="CrowdPrefabBuilder"/> and <see cref="LegacyClipMaker"/> collapsed into one
    /// pass, because for this pack they ARE one pass: the civilian pipeline needs two scripts only
    /// because the civilian clips live in a separate library FBX that has to be matched up with each
    /// body. The robot carries its own clips, so the source and the target are the same file.
    ///
    /// The legacy conversion is NOT optional and it is not superstition. CLAUDE.md records five
    /// approaches that failed silently here, each leaving a bind pose that looks exactly like a
    /// working import nobody told to move. The two that matter:
    ///  - a non-legacy clip does not play through an Animation component, and SampleAnimation on one
    ///    works in the editor and does nothing in a player;
    ///  - a clip copied and then flagged legacy keeps its data and animates nothing, because legacy
    ///    and non-legacy clips bind curves through DIFFERENT systems. The curves must be copied one
    ///    binding at a time, which is what <see cref="CopyToLegacy"/> does.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.MachinePrefabBuilder.BuildAll -quit
    /// </summary>
    public static class MachinePrefabBuilder
    {
        private const string Dir = "Assets/Resources/Machines";

        /// <summary>
        /// The clips we actually drive, and what the game calls them.
        ///
        /// Deliberately a SHORT list out of the fourteen the pack ships. Dance, ThumbsUp, Wave, Yes
        /// and No are social animations for a friendly robot and there is no state in this game that
        /// would ever play them; carrying them into the build would be weight for nothing. Death is
        /// taken because ADR-008 gave us somewhere to put it -- a chip finishing its decrypt is a
        /// body going down, and until now that was a capsule tipping over.
        /// </summary>
        private static readonly (string clip, string game, WrapMode wrap)[] Wanted =
        {
            ("Robot_Walking", "machine_walk", WrapMode.Loop),
            ("Robot_Running", "machine_run", WrapMode.Loop),
            ("Robot_Idle", "machine_idle", WrapMode.Loop),
            ("Robot_Punch", "machine_attack", WrapMode.Loop),
            ("Robot_Death", "machine_death", WrapMode.ClampForever),
        };

        [MenuItem("Cipher/Crowd/Build Machine Prefabs")]
        public static void BuildAll()
        {
            var model = FindModelWithClips();
            if (model == null) { Debug.LogError($"[Machines] no model with clips under {Dir}"); return; }

            var legacy = BuildLegacyClips(model);
            if (legacy.Count == 0) { Debug.LogError("[Machines] no legacy clips built; aborting"); return; }

            string name = Path.GetFileNameWithoutExtension(model);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(model);
            if (source == null) { Debug.LogError($"[Machines] cannot load {model}"); return; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                // The importer's Animator binds through an avatar and would suppress the legacy
                // component entirely. CLAUDE.md: it must be DESTROYED, not disabled -- a disabled
                // Animator still wins.
                foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
                    Object.DestroyImmediate(animator);

                var anim = instance.AddComponent<Animation>();
                foreach (var clip in legacy) anim.AddClip(clip, clip.name);
                anim.clip = legacy.FirstOrDefault(c => c.name == "machine_walk") ?? legacy[0];
                anim.playAutomatically = false;   // the gait decides, not the importer
                anim.wrapMode = WrapMode.Loop;
                anim.cullingType = AnimationCullingType.BasedOnRenderers;

                // Without this the inverted-hull outline tears open at every hard edge and UV seam,
                // and a crisp robot reads as a smudged pencil sketch. Same treatment the civilians get.
                SmoothNormals.ApplyToHierarchy(instance, $"{Dir}/{name}_smoothmesh.asset");

                // THE SCALE GOES ON A WRAPPER, NOT ON THE MODEL.
                //
                // The first build of this shipped the robot at its import scale and it stood about
                // eight metres tall, filling the frame. The obvious fix -- scale the model root --
                // is the trap CLAUDE.md already records: the clips animate scale on the armature, so
                // a character measures one height standing still and another once it starts walking.
                // A parent the animation cannot reach is the only stable place to put it.
                var root = new GameObject($"{name}_Machine");
                instance.transform.SetParent(root.transform, false);
                instance.transform.localPosition = Vector3.zero;

                float measured = MeasureHeight(instance);
                if (measured > 0.001f)
                {
                    float k = MachineBody.Height / measured;
                    root.transform.localScale = Vector3.one * k;
                    Debug.Log($"[Machines] {name}: measured {measured:F2}m, scaling x{k:F3} " +
                              $"to MachineBody.Height {MachineBody.Height:F2}m");
                }
                else
                {
                    Debug.LogWarning($"[Machines] {name}: could not measure a height; left at import scale");
                }

                string outPath = $"{Dir}/{name}_Machine.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, outPath);
                Object.DestroyImmediate(root);
                Debug.Log($"[Machines] saved {outPath} with {legacy.Count} clips: " +
                          string.Join(", ", legacy.Select(c => c.name)));
            }
            finally
            {
                // Only if it never made it under the wrapper; otherwise the wrapper took it with it.
                if (instance != null) Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<AnimationClip> BuildLegacyClips(string modelPath)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                                     .OfType<AnimationClip>()
                                     .Where(c => !c.name.StartsWith("__preview__"))
                                     .ToList();

            var built = new List<AnimationClip>();
            foreach (var (clipName, gameName, wrap) in Wanted)
            {
                // Names arrive as "RobotArmature|Robot_Walking", so match on the tail and EXACTLY --
                // a prefix match on "Robot_Walk" also catches "Robot_WalkJump" and the crowd hops.
                var match = clips.FirstOrDefault(c => Tail(c.name) == clipName);
                if (match == null)
                {
                    Debug.LogWarning($"[Machines] '{clipName}' is not in this pack; skipped");
                    continue;
                }

                var copy = CopyToLegacy(match, gameName, wrap);
                if (copy != null) built.Add(copy);
            }
            return built;
        }

        private static AnimationClip? CopyToLegacy(AnimationClip source, string gameName, WrapMode wrap)
        {
            var copy = new AnimationClip
            {
                name = gameName,
                legacy = true,
                frameRate = source.frameRate,
                wrapMode = wrap,
            };

            int curves = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve == null) continue;
                copy.SetCurve(binding.path, binding.type, binding.propertyName, curve);
                curves++;
            }

            if (curves == 0)
            {
                Debug.LogWarning($"[Machines] {source.name} had no curves; '{gameName}' would be a bind pose");
                return null;
            }

            string path = $"{Dir}/{gameName}.anim";
            AssetDatabase.CreateAsset(copy, path);
            Debug.Log($"[Machines] {gameName}: {curves} curves from {source.name} ({source.length:F2}s)");
            return copy;
        }

        /// <summary>
        /// The model's height in metres, measured the one way that works on a fresh instance.
        ///
        /// CLAUDE.md, twice over: renderer bounds on a prefab that has never been through a frame
        /// are not real (they produced a sixty-metre pedestrian and then a quarter-size one), and an
        /// FBX root carries an import scale that naive matrix round-trips do not cancel (a six-metre
        /// road tile measured as two centimetres). So: take the MESH's own bounds, push its eight
        /// corners through the renderer's transform into the root's space, and union them.
        /// </summary>
        private static float MeasureHeight(GameObject root)
        {
            var toRoot = root.transform.worldToLocalMatrix;
            bool any = false;
            float lo = float.MaxValue, hi = float.MinValue;

            void Accumulate(Mesh? mesh, Transform t)
            {
                if (mesh == null) return;
                var m = toRoot * t.localToWorldMatrix;
                var b = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);
                    float y = m.MultiplyPoint3x4(corner).y;
                    if (y < lo) lo = y;
                    if (y > hi) hi = y;
                    any = true;
                }
            }

            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Accumulate(smr.sharedMesh, smr.transform);
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                Accumulate(mf.sharedMesh, mf.transform);

            return any ? hi - lo : 0f;
        }

        private static string Tail(string name)
        {
            int bar = name.LastIndexOf('|');
            return bar >= 0 ? name.Substring(bar + 1) : name;
        }

        private static string? FindModelWithClips()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool hasClips = AssetDatabase.LoadAllAssetsAtPath(path)
                                             .OfType<AnimationClip>()
                                             .Any(c => !c.name.StartsWith("__preview__"));
                if (hasClips) return path;
            }
            return null;
        }
    }
}
