#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Turns the bought Synty characters into the crowd this game already knows how to drive.
    ///
    /// TWO THINGS ARE CONVERTED AND THEY HAVE DIFFERENT ANSWERS.
    ///
    /// The MATERIALS are converted, and that part is ours. Synty's look is a shared texture atlas
    /// per pack: one material, and every part takes its colour from which cell of the grid its UVs
    /// land in. Their Shader Graph knows nothing about our banding or our inverted hull, so a
    /// character imported as-is renders correctly and looks like it came from another game -- the
    /// only thing on screen with no ink line around it. Rebuilding each material on
    /// Exodus/InstancedLit with their atlas in _BaseMap keeps all of their colour work and puts them
    /// under our light. The atlas hangs off `_Albedo_Map`, which is the name that mattered.
    ///
    /// The ANIMATION is NOT converted, and that is the lesson. Three builds tried to copy Synty's
    /// clips into legacy clips the way <see cref="LegacyClipMaker"/> does for the CC0 pack, and
    /// every one put the crowd on its back in the road. Synty's clips are authored for HUMANOID
    /// RETARGETING: their raw transform curves are not in the character's rest-pose space, and
    /// Mecanim reconciling the two is not an optimisation, it is the mechanism. Legacy playback
    /// skips it. Dropping the root track, keeping its rotation, and a corrective pitch on a wrapper
    /// were all tried -- all of them guesses at a number that should never have been guessed,
    /// because the offset lives between two REST POSES and is not in the animation at all.
    ///
    /// So these characters keep their Humanoid rig and get an Animator with a real controller, which
    /// is the path the art was built for. The crowd destroys Animators on purpose, for cost; that
    /// now skips any Animator which actually has a controller, so the CC0 path is untouched.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.SyntyCrowdBuilder.BuildAll -quit
    /// </summary>
    public static class SyntyCrowdBuilder
    {
        private const string CharDir = "Assets/Synty/PolygonCityCharacters/Prefabs";
        private const string CharModels = "Assets/Synty/PolygonCityCharacters";
        private const string OutDir = "Assets/Resources/Civilians";
        private const string Retired = "Assets/RetiredQuaterniusCivilians";
        private const string AnimDir = "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine";
        private const string IdleFbx = AnimDir + "/Idle/A_Idle_Standing_Masc.fbx";
        private const string WalkFbx = AnimDir + "/Locomotion/Walk/A_Walk_F_Masc.fbx";
        private const string RunFbx = AnimDir + "/Locomotion/Run/A_Run_F_Masc.fbx";

        /// <summary>
        /// Metres per second each clip looks right at, and the whole cure for skating feet.
        ///
        /// These clips carry NO root motion -- measured, averageSpeed is zero on all of them -- so
        /// there is no authored pace to read off them and the blend thresholds are the only place
        /// the information can live. They are set against the sim, not against taste: SimConfig's
        /// MoveSpeed is 3 m/s, which is a jog, so a body crossing the map sits most of the way
        /// toward Run and a body pressed against a wall sits at Idle instead of scrubbing a walk
        /// cycle in place.
        /// </summary>
        private const float IdleAt = 0f;
        private const float WalkAt = 1.5f;
        private const float RunAt = 3.6f;
        private const string ControllerPath = OutDir + "/SyntyCrowd.controller";

        [MenuItem("Cipher/Crowd/Build Synty Crowd")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);

            EnsureHumanoid(CharModels);
            EnsureHumanoid("Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine/Locomotion/Walk");
            EnsureHumanoid("Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine/Locomotion/Run");
            EnsureHumanoid("Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine/Idle");
            RetireQuaternius();

            var controller = BuildController();
            if (controller == null) { Debug.LogError("[Synty] no controller; aborting"); return; }

            int people = BuildCharacters(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Synty] done: {people} characters on an Animator");
        }

        /// <summary>
        /// Puts a folder's models on the Humanoid rig.
        ///
        /// An earlier build forced both sides to Generic, chasing transform curves a legacy clip
        /// could bind. Generic is the WRONG answer for this art: it discards the avatar, which is
        /// the only thing that knows how to map one Synty skeleton's pose onto another's.
        /// </summary>
        private static void EnsureHumanoid(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir)) { Debug.LogWarning($"[Synty] no folder {dir}"); return; }

            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not ModelImporter im) continue;
                if (im.animationType == ModelImporterAnimationType.Human && im.importAnimation) continue;

                im.animationType = ModelImporterAnimationType.Human;
                im.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                im.importAnimation = true;
                im.SaveAndReimport();
                Debug.Log($"[Synty] rig -> Humanoid: {path}");
            }
        }

        /// <summary>
        /// Idle, walk and run on a 1D blend tree driven by a Speed parameter in metres per second.
        /// </summary>
        private static AnimatorController? BuildController()
        {
            var idle = Clip(IdleFbx);
            var walk = Clip(WalkFbx);
            var run = Clip(RunFbx);
            if (idle == null || walk == null || run == null)
            {
                Debug.LogError("[Synty] idle/walk/run not all present; cannot build a blend tree");
                return null;
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);

            // A ONE-DIMENSIONAL BLEND TREE, NOT A SINGLE STATE WITH ITS PLAYBACK SCALED.
            //
            // The first version had one walk state and scaled animator.speed by
            // travelSpeed / a constant, and it skated visibly. It could not do anything else: these
            // clips have no root motion, so scaling playback changes how fast the legs cycle without
            // changing how far a stride covers, and the two only agree at exactly one speed. Worse,
            // a stationary body got a very slow walk rather than an idle -- the "walking in place"
            // half of the same bug.
            //
            // A blend tree fixes both by asking the right question. Instead of "how fast should this
            // clip play", it asks "which clip is this body's speed", and at zero that answer is
            // standing still.
            var tree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = SpeedParam,
                useAutomaticThresholds = false,
            };
            tree.AddChild(idle, IdleAt);
            tree.AddChild(walk, WalkAt);
            tree.AddChild(run, RunAt);

            AssetDatabase.AddObjectToAsset(tree, controller);

            var machine = controller.layers[0].stateMachine;
            var state = machine.AddState("Locomotion");
            state.motion = tree;
            machine.defaultState = state;

            EditorUtility.SetDirty(controller);
            Debug.Log($"[Synty] blend tree: idle '{idle.name}' @{IdleAt}, walk '{walk.name}' @{WalkAt}, " +
                      $"run '{run.name}' @{RunAt} m/s");
            return controller;
        }

        /// <summary>The parameter SkinnedGait writes each frame. Metres per second, not a 0..1 ratio.</summary>
        public const string SpeedParam = "Speed";

        private static AnimationClip? Clip(string fbx) =>
            AssetDatabase.LoadAllAssetsAtPath(fbx)
                         .OfType<AnimationClip>()
                         .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        /// <summary>
        /// Moves the CC0 civilians OUT of Resources/Civilians, without deleting them.
        ///
        /// CivilianCrowd calls Resources.LoadAll on that folder, so leaving both packs there would
        /// deal a crowd half Quaternius and half Synty -- the exact genre-smashing the owner
        /// objected to, except self-inflicted. CrowdPrefabBuilder rebuilds them any time.
        /// </summary>
        private static void RetireQuaternius()
        {
            // AssetDatabase.CreateFolder, not Directory.CreateDirectory: a folder made behind the
            // importer's back has no GUID, and MoveAsset into it fails with "Could not find parent
            // directory GUID" -- quietly, per file, leaving BOTH packs in the crowd.
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
                if (name.StartsWith("SM_Chr_")) continue;

                string err = AssetDatabase.MoveAsset(path, $"{Retired}/{name}");
                if (string.IsNullOrEmpty(err)) moved++;
            }
            if (moved > 0) Debug.Log($"[Synty] retired {moved} Quaternius civilians");
        }

        private static int BuildCharacters(AnimatorController controller)
        {
            var shader = Shader.Find("Exodus/InstancedLit");
            if (shader == null) { Debug.LogError("[Synty] Exodus/InstancedLit not found"); return 0; }

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
                    if (instance.GetComponentInChildren<SkinnedMeshRenderer>() == null)
                    {
                        Debug.LogWarning($"[Synty] {name}: no skinned mesh");
                        continue;
                    }

                    Reskin(instance, shader, byTexture);

                    // The ink hull tears at every hard edge without this, and a character is nothing
                    // but hard edges around a silhouette.
                    SmoothNormals.ApplyToHierarchy(instance, $"{OutDir}/{name}_smoothmesh.asset");

                    // The Animator must carry the AVATAR as well as the controller. Without an avatar
                    // there is no retargeting, and no retargeting is the bug this rewrite exists to
                    // stop repeating.
                    var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;              // the sim owns where a body is
                    animator.cullingMode = AnimatorCullingMode.CullCompletely;

                    if (animator.avatar == null)
                    {
                        var srcAnimator = source.GetComponentInChildren<Animator>();
                        if (srcAnimator != null) animator.avatar = srcAnimator.avatar;
                    }
                    if (animator.avatar == null)
                        Debug.LogWarning($"[Synty] {name}: NO AVATAR -- this one cannot retarget");

                    PrefabUtility.SaveAsPrefabAsset(instance, $"{OutDir}/{name}_Civilian.prefab");
                    built++;
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

            Debug.Log($"[Synty] {built} characters, {byTexture.Count} shared materials");
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
                    // SYNTY'S ATLAS LIVES ON `_Albedo_Map`. Their materials are Shader Graph, so none
                    // of the conventional names resolve: probing _BaseMap, _MainTex and mainTexture
                    // missed on all three and produced nineteen characters sharing one untextured
                    // material, which reads as a shader bug rather than a lookup miss.
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

                    var made = new Material(shader)
                    {
                        name = tex != null ? $"Synty_{tex.name}" : "Synty_Untextured",
                    };

                    // White base colour so the atlas IS the colour. _BaseColor multiplies the map and
                    // the shader default is grey, which would quietly darken every Synty character by
                    // 40% and read as "their art looks muddy in our game".
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
