#nullable enable
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Turns animation import ON for the character FBXs, so their clips exist as assets at all.
    ///
    /// That is the only thing this still does, and it is a prerequisite rather than the fix: the
    /// models import with avatarSetup NoAvatar and no animation, so before this runs there is no
    /// clip anywhere for <see cref="LegacyClipMaker"/> to copy curves out of.
    ///
    /// The avatar it creates is NOT what makes a civilian walk. The runtime destroys every Animator
    /// and plays a legacy clip instead (see <see cref="CrowdPrefabBuilder"/>), so re-running this
    /// will not fix a broken walk cycle. Run it after adding a character to the pack, then rerun
    /// LegacyClipMaker and CrowdPrefabBuilder.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.ModelRigSetup.Configure -quit
    /// </summary>
    public static class ModelRigSetup
    {
        private const string Dir = "Assets/Resources/Characters";
        private const string AnimationsPath = Dir + "/_Animations.fbx";

        [MenuItem("Cipher/Crowd/Configure Character Rigs")]
        public static void Configure()
        {
            // 1. The clip library owns the rig: give it an avatar of its own first.
            var animImporter = AssetImporter.GetAtPath(AnimationsPath) as ModelImporter;
            if (animImporter == null)
            {
                Debug.LogError($"[RigSetup] {AnimationsPath} not found");
                return;
            }

            animImporter.animationType = ModelImporterAnimationType.Generic;
            animImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            animImporter.importAnimation = true;
            EditorUtility.SetDirty(animImporter);
            animImporter.SaveAndReimport();

            var sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(AnimationsPath)
                                            .OfType<Avatar>()
                                            .FirstOrDefault();
            if (sourceAvatar == null)
            {
                Debug.LogError("[RigSetup] the animation model produced no avatar");
                return;
            }
            Debug.Log($"[RigSetup] source avatar '{sourceAvatar.name}', valid={sourceAvatar.isValid}");

            // 2. Every character copies that avatar, so one clip library drives all of them.
            int done = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == AnimationsPath) continue;

                if (AssetImporter.GetAtPath(path) is not ModelImporter importer) continue;

                // Each character now ships its own clips, so it owns its own rig. Copying a foreign
                // avatar was only ever a workaround for a character set that had no animation in it.
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                done++;
                Debug.Log($"[RigSetup] {Path.GetFileName(path)}: generic rig, animation import on");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RigSetup] configured {done} character rigs");
        }
    }
}
