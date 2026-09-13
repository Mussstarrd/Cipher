#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Turns the bought Synty characters into the crowd this game already knows how to drive.
    ///
    /// The crowd is not rewritten for them. <c>CivilianCrowd</c> loads whatever prefabs sit in
    /// Resources/Civilians and plays legacy clips of fixed names out of the same folder, so the job
    /// here is entirely one of CONVERSION: put Synty's meshes and Synty's motion into the two shapes
    /// the existing system reads.
    ///
    /// THE MATERIAL SWAP IS THE WHOLE POINT. Synty's look is a shared texture ATLAS per pack -- one
    /// material, and every part gets its colour from which cell of the grid its UVs land in. Their
    /// own shader is a Shader Graph that knows nothing about our banding or our inverted hull, so a
    /// character imported as-is renders correctly and looks like it came from a different game,
    /// because it is the only thing on screen without an ink line around it. Rebuilding each
    /// material on Exodus/InstancedLit with their atlas in _BaseMap keeps every bit of their colour
    /// work AND puts them under our light. _BaseMap already existed for the ground, which is the
    /// only reason this is a converter rather than a shader project.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.SyntyCrowdBuilder.BuildAll -quit
    /// </summary>
    public static class SyntyCrowdBuilder
    {
        private const string CharDir = "Assets/Synty/PolygonCityCharacters/Prefabs";
        private const string OutDir = "Assets/Resources/Civilians";
        private const string Retired = "Assets/RetiredQuaterniusCivilians";
        private const string AnimDir = "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine";

        /// <summary>
        /// Which Synty clip becomes which of the names <c>FloodBootstrap.BodyClips</c> asks for.
        ///
        /// Base Locomotion is locomotion ONLY -- there is no death, no attack and no firearm set in
        /// Synty's whole ANIMATION line. So this covers walk/idle/run, which is what a crowd spends
        /// almost all of its time doing, and the rest keep warning until they are sourced. A missing
        /// clip logs and is skipped rather than breaking the crowd.
        /// </summary>
        private static readonly (string src, string game, WrapMode wrap)[] Wanted =
        {
            ("Locomotion/Walk/A_Walk_F_Masc", "walk", WrapMode.Loop),
            ("Locomotion/Run/A_Run_F_Masc", "run", WrapMode.Loop),
            ("Idle/A_Idle_Standing_Masc", "idle", WrapMode.Loop),
        };

        [MenuItem("Cipher/Crowd/Build Synty Crowd")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);
            RetireQuaternius();
            int clips = BuildClips();
            int people = BuildCharacters();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Synty] done: {people} characters, {clips} legacy clips");
        }

        /// <summary>
        /// Moves the CC0 civilians OUT of Resources/Civilians, without deleting them.
        ///
        /// CivilianCrowd calls Resources.LoadAll on that folder, so leaving both packs in it would
        /// deal a crowd that is half Quaternius and half Synty -- which is precisely the
        /// genre-smashing the owner objected to, and it would be our own doing rather than a bad
        /// model. They move rather than die because they are a working fallback and because
        /// CrowdPrefabBuilder can rebuild them from Resources/Characters at any time.
        /// </summary>
        private static void RetireQuaternius()
        {
            // AssetDatabase.CreateFolder, not Directory.CreateDirectory: a folder made behind the
            // importer's back has no GUID, and MoveAsset into it fails with "Could not find parent
            // directory GUID" -- which it does quietly, per file, leaving BOTH packs in the crowd.
            if (!AssetDatabase.IsValidFolder(Retired))
            {
                AssetDatabase.CreateFolder("Assets", Path.GetFileName(Retired));
                AssetDatabase.Refresh();
            }
            int moved = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { OutDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileName(path);
                if (!name.EndsWith("_Civilian.prefab")) continue;
                if (name.StartsWith("SM_Chr_")) continue;   // already ours

                string dest = $"{Retired}/{name}";
                string err = AssetDatabase.MoveAsset(path, dest);
                if (string.IsNullOrEmpty(err)) moved++;
                else Debug.LogWarning($"[Synty] could not retire {name}: {err}");
            }

            if (moved > 0) Debug.Log($"[Synty] retired {moved} Quaternius civilians to {Retired}");
        }

        /// <summary>
        /// Reimports the locomotion FBXs as GENERIC and copies their curves into legacy clips.
        ///
        /// Generic, not Humanoid, and this is the subtle part: a Humanoid clip stores MUSCLE values,
        /// not transform curves, so AnimationUtility hands back muscle bindings that a legacy clip
        /// cannot bind to anything. Reimporting as Generic produces real per-bone transform curves
        /// addressed by path -- which is exactly what the crowd's Animation components play, and
        /// exactly what LegacyClipMaker already does for the CC0 pack.
        /// </summary>
        private static int BuildClips()
        {
            int built = 0;

            foreach (var (src, game, wrap) in Wanted)
            {
                string path = $"{AnimDir}/{src}.fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) { Debug.LogWarning($"[Synty] no importer for {path}"); continue; }

                if (importer.animationType != ModelImporterAnimationType.Generic || !importer.importAnimation)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation = true;
                    importer.SaveAndReimport();
                }

                var source = AssetDatabase.LoadAllAssetsAtPath(path)
                                          .OfType<AnimationClip>()
                                          .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (source == null) { Debug.LogWarning($"[Synty] {src}: no clip after reimport"); continue; }

                var copy = new AnimationClip
                {
                    name = game,
                    legacy = true,
                    frameRate = source.frameRate,
                    wrapMode = wrap,
                };

                int curves = 0, dropped = 0;
                string sample = "";
                foreach (var binding in AnimationUtility.GetCurveBindings(source))
                {
                    // THE ROOT TRACK IS DROPPED, AND IT IS THE WHOLE FIX FOR "HE IS LYING DOWN".
                    //
                    // These clips carry root motion: the Root node is animated in position AND
                    // rotation so the character walks forward under its own power. Our crowd does
                    // not work that way -- the sim owns where a body is and which way it faces, and
                    // the view follows. Play the root track anyway and the clip fights the placement
                    // for the same transform every frame; the first build of this put the hero flat
                    // on his back in the middle of the road with his arms out.
                    //
                    // Everything BELOW the root is kept, so the walk itself is untouched.
                    if (binding.path == "" || binding.path == "Root") { dropped++; continue; }

                    var curve = AnimationUtility.GetEditorCurve(source, binding);
                    if (curve == null) continue;
                    copy.SetCurve(binding.path, binding.type, binding.propertyName, curve);
                    if (curves == 0) sample = binding.path;
                    curves++;
                }

                if (curves == 0)
                {
                    Debug.LogError($"[Synty] {game}: ZERO curves -- the clip is still Humanoid, " +
                                   "so every body would stand in a bind pose. Not written.");
                    continue;
                }

                AssetDatabase.CreateAsset(copy, $"{OutDir}/{game}.anim");
                Debug.Log($"[Synty] {game}: {curves} curves kept, {dropped} root curves dropped, first bone '{sample}' ({source.length:F2}s)");
                built++;
            }

            return built;
        }

        /// <summary>Rebuilds every Synty character onto our shader and drops it in the crowd pool.</summary>
        private static int BuildCharacters()
        {
            var shader = Shader.Find("Exodus/InstancedLit");
            if (shader == null) { Debug.LogError("[Synty] Exodus/InstancedLit not found"); return 0; }

            // One material per atlas, not one per character. Shared, so 19 people cost a handful of
            // materials and the instancing the swarm depends on is not broken by 19 lookalikes.
            var byTexture = new Dictionary<Texture, Material>();
            int built = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { CharDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null) continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                try
                {
                    var smr = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr == null) { Debug.LogWarning($"[Synty] {name}: no skinned mesh"); continue; }

                    Reskin(instance, shader, byTexture);

                    // The ink hull tears at every hard edge without this, and a character is nothing
                    // but hard edges around a silhouette.
                    SmoothNormals.ApplyToHierarchy(instance, $"{OutDir}/{name}_smoothmesh.asset");

                    string outPath = $"{OutDir}/{name}_Civilian.prefab";
                    PrefabUtility.SaveAsPrefabAsset(instance, outPath);
                    built++;
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

            Debug.Log($"[Synty] {built} characters on Exodus/InstancedLit, {byTexture.Count} shared materials");
            return built;
        }

        private static void Reskin(GameObject root, Shader shader, Dictionary<Texture, Material> cache)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];

                for (int i = 0; i < src.Length; i++)
                {
                    // SYNTY'S ATLAS LIVES ON `_Albedo_Map`, and that is the name that matters.
                    // Their materials are Shader Graph, so none of the conventional names resolve:
                    // the first build of this checked _BaseMap, _MainTex and mainTexture, found
                    // nothing on all three, and produced nineteen characters sharing one untextured
                    // material -- which would have rendered a crowd of flat grey mannequins and
                    // looked like a shader bug rather than a lookup miss.
                    Texture? tex = null;
                    var m = src[i];
                    if (m != null)
                    {
                        foreach (var prop in new[] { "_Albedo_Map", "_BaseMap", "_MainTex", "_Texture2D" })
                        {
                            if (!m.HasProperty(prop)) continue;
                            tex = m.GetTexture(prop);
                            if (tex != null) break;
                        }
                        if (tex == null) tex = m.mainTexture;
                        if (tex == null) Debug.LogWarning($"[Synty] {m.name}: no atlas found on any known property");
                    }

                    if (tex != null && cache.TryGetValue(tex, out var got)) { dst[i] = got; continue; }

                    var made = new Material(shader) { name = tex != null ? $"Synty_{tex.name}" : "Synty_Untextured" };

                    // White base colour so the atlas is the colour. _BaseColor multiplies the map,
                    // and the shader's default is grey -- which would quietly darken every Synty
                    // character by 40% and read as "their art looks muddy in our game".
                    made.SetColor("_BaseColor", Color.white);
                    if (tex != null) made.SetTexture("_BaseMap", tex);
                    made.SetFloat("_SmoothOutline", 1f);
                    made.EnableKeyword("_SMOOTH_OUTLINE");
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
