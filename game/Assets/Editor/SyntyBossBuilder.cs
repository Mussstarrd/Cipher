#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Builds the Collector (ADR-011) out of the Synty Boss Zombies pack.
    ///
    /// THE PLAN WAS LEGACY CLIPS AND THE PACK COULD NOT SUPPLY THEM. The bosses were expected to
    /// carry their own animation in the same FBX, the way the CC0 robot does, which needs no
    /// retargeting because the clips and the skeleton are the same file. Between four bosses the
    /// pack ships one empty "Take 001". Synty sells characters and animation separately, and their
    /// ANIMATION line has no monster set at all.
    ///
    /// So the Collector borrows the CROWD's walk instead, through a Humanoid avatar. Retargeting is
    /// exactly the tool for this: it plays one skeleton's motion on another's PROPORTIONS, which is
    /// the whole difference between a civilian and a three-metre monster on Synty's oversized rig.
    /// All four bosses build a valid avatar, so a clip authored for a man drives them for free.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.SyntyBossBuilder.BuildAll -quit
    /// </summary>
    public static class SyntyBossBuilder
    {
        private const string BossDir = "Assets/Synty/PolygonBossZombies";
        private const string OutDir = "Assets/Resources/Collector";

        /// <summary>
        /// Which boss becomes the Collector.
        ///
        /// The Brute, for ADR-011's description: "the butcher from Diablo, all huge belly and like
        /// three times the size of a person". Blobber is the other candidate and is kept buildable
        /// so the owner can see both rather than be told.
        /// </summary>
        private const string Preferred = "Brute";

        [MenuItem("Cipher/Crowd/Build Collector")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);

            var models = AssetDatabase.FindAssets("t:Model", new[] { BossDir })
                                      .Select(AssetDatabase.GUIDToAssetPath)
                                      .Where(p => Path.GetFileName(p).StartsWith("ZombieBoss"))
                                      .ToList();

            if (models.Count == 0) { Debug.LogError($"[Boss] no ZombieBoss models under {BossDir}"); return; }

            foreach (string path in models) Configure(path);

            // Report EVERYTHING the pack brought before choosing. "Which animations do we actually
            // have" is a question to answer with the importer, never with a guess -- the same reason
            // ClipInventory exists.
            foreach (string path in models)
            {
                var names = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                                         .Where(c => !c.name.StartsWith("__preview__"))
                                         .Select(c => $"{c.name}({c.length:F2}s)");
                Debug.Log($"[Boss] {Path.GetFileName(path)}: {string.Join(", ", names)}");
            }

            var shader = Shader.Find("Exodus/InstancedLit");
            if (shader == null) { Debug.LogError("[Boss] Exodus/InstancedLit missing"); return; }

            int built = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { BossDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (!name.Contains("ZombieBoss")) continue;

                if (BuildOne(path, name, shader)) built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Boss] built {built} boss prefabs into {OutDir}");
        }

        private static void Configure(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter im) return;
            if (im.animationType == ModelImporterAnimationType.Human && im.importAnimation) return;

            // HUMANOID, after the pack turned out to ship no usable clips of its own.
            //
            // The plan was Generic plus the boss's own animation, which is how the CC0 robot works.
            // The pack has none -- one empty "Take 001" between four bosses -- so the Collector had
            // nowhere to get motion from. Humanoid is the way out: retargeting exists precisely to
            // play one skeleton's animation on another's PROPORTIONS, which is the whole difference
            // between a boss on Synty's oversized rig and the civilians on the standard one. If the
            // avatar builds, the crowd's own walk drives a three-metre monster for free.
            im.animationType = ModelImporterAnimationType.Human;
            im.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            im.importAnimation = true;
            im.SaveAndReimport();
            Debug.Log($"[Boss] configured {path}");
        }

        private static bool BuildOne(string prefabPath, string name, Shader shader)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (source == null) return false;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                if (instance.GetComponentInChildren<SkinnedMeshRenderer>() == null)
                {
                    Debug.LogWarning($"[Boss] {name}: no skinned mesh");
                    return false;
                }

                Reskin(instance, shader);
                SmoothNormals.ApplyToHierarchy(instance, $"{OutDir}/{name}_smoothmesh.asset");

                // The crowd's controller, retargeted. One walk clip, authored for a human, played
                // on a monster twice its size -- which is exactly what an avatar is for.
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/Resources/Civilians/SyntyCrowd.controller");

                var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullCompletely;

                if (animator.avatar == null)
                {
                    var srcAnimator = source.GetComponentInChildren<Animator>();
                    if (srcAnimator != null) animator.avatar = srcAnimator.avatar;
                }

                if (controller == null) Debug.LogWarning($"[Boss] {name}: no crowd controller to share");
                if (animator.avatar == null)
                    Debug.LogWarning($"[Boss] {name}: NO AVATAR -- the big rig refused Humanoid, it will stand still");
                else
                    Debug.Log($"[Boss] {name}: avatar '{animator.avatar.name}', valid={animator.avatar.isValid}");

                // Height normalised on a WRAPPER, for the reason the robot needed one: a clip
                // animates scale on the armature, so the model measures one height standing still
                // and another once it walks. A parent the animation never writes to is the only
                // stable place for a constant.
                var root = new GameObject(name);
                instance.transform.SetParent(root.transform, false);

                float measured = MeasureHeight(instance);
                if (measured > 0.001f)
                {
                    float k = TargetHeight / measured;
                    root.transform.localScale = Vector3.one * k;
                    Debug.Log($"[Boss] {name}: measured {measured:F2}m, scaled x{k:F3} to {TargetHeight:F2}m");
                }

                PrefabUtility.SaveAsPrefabAsset(root, $"{OutDir}/{name}.prefab");
                Object.DestroyImmediate(root);
                Debug.Log($"[Boss] saved {name}");
                return true;
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }
        }

        /// <summary>Must match CollectorBody.Height: the sim's reach and the picture agree or neither is right.</summary>
        private const float TargetHeight = 2.75f;

        /// <summary>
        /// Height in metres, measured the one way that works on a fresh instance: the MESH's own
        /// bounds pushed through the renderer's transform. Renderer bounds on a prefab that has
        /// never been through a frame are not real -- CLAUDE.md has the sixty-metre pedestrian.
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

        /// <summary>
        /// Copies this boss's own clips into legacy clips, CURVE BY CURVE.
        ///
        /// Copying a clip and flagging it legacy afterwards keeps its data and animates nothing --
        /// legacy and non-legacy clips bind through different systems. CLAUDE.md has the full list
        /// of five approaches that failed silently before this one worked.
        /// </summary>
        private static List<AnimationClip> FindClipsFor(GameObject instance)
        {
            var built = new List<AnimationClip>();

            // The clips live on the MODEL this prefab is built from, not on the prefab.
            var smr = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null || smr.sharedMesh == null) return built;

            string modelPath = AssetDatabase.GetAssetPath(smr.sharedMesh);
            if (string.IsNullOrEmpty(modelPath)) return built;

            foreach (var src in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>())
            {
                if (src.name.StartsWith("__preview__")) continue;

                string game = Rename(src.name);
                var copy = new AnimationClip
                {
                    name = game,
                    legacy = true,
                    frameRate = src.frameRate,
                    wrapMode = WrapMode.Loop,
                };

                int curves = 0;
                foreach (var b in AnimationUtility.GetCurveBindings(src))
                {
                    var curve = AnimationUtility.GetEditorCurve(src, b);
                    if (curve == null) continue;
                    copy.SetCurve(b.path, b.type, b.propertyName, curve);
                    curves++;
                }
                if (curves == 0) continue;

                string outPath = $"{OutDir}/{game}.anim";
                AssetDatabase.CreateAsset(copy, outPath);
                built.Add(copy);
                Debug.Log($"[Boss] clip {game}: {curves} curves ({src.length:F2}s)");
            }

            // Walk first if there is one: it is what a Collector does for most of its life.
            built.Sort((a, b) => Score(b.name).CompareTo(Score(a.name)));
            return built;

            static int Score(string n) => n.Contains("walk") ? 3 : n.Contains("idle") ? 2 : 1;
        }

        private static string Rename(string raw)
        {
            int bar = raw.LastIndexOf('|');
            string tail = (bar >= 0 ? raw.Substring(bar + 1) : raw).ToLowerInvariant();
            return "collector_" + tail.Replace(" ", "_");
        }

        private static void Reskin(GameObject root, Shader shader)
        {
            var cache = new Dictionary<Texture, Material>();

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];

                for (int i = 0; i < src.Length; i++)
                {
                    Texture? tex = null;
                    var m = src[i];
                    if (m != null)
                    {
                        foreach (var prop in new[] { "_Albedo_Map", "_BaseMap", "_MainTex" })
                        {
                            if (!m.HasProperty(prop)) continue;
                            tex = m.GetTexture(prop);
                            if (tex != null) break;
                        }
                        if (tex == null) tex = m.mainTexture;
                    }

                    if (tex != null && cache.TryGetValue(tex, out var got)) { dst[i] = got; continue; }

                    var made = new Material(shader) { name = tex != null ? $"Boss_{tex.name}" : "Boss_Untextured" };
                    made.SetColor("_BaseColor", Color.white);
                    if (tex != null) made.SetTexture("_BaseMap", tex);
                    made.SetFloat("_SmoothOutline", 1f);
                    made.EnableKeyword("_SMOOTH_OUTLINE");

                    // A HEAVIER INK LINE THAN A CIVILIAN GETS. The Collector's whole job is to be
                    // read instantly, at distance, as something that is not one of the crowd, and
                    // silhouette is the only channel that survives a golf course at dusk.
                    made.SetFloat("_OutlineWidth", 0.06f);
                    made.enableInstancing = true;

                    AssetDatabase.CreateAsset(made, $"{OutDir}/{made.name}.mat");
                    if (tex != null) cache[tex] = made;
                    dst[i] = made;
                }

                r.sharedMaterials = dst;
            }
        }
    }
}
