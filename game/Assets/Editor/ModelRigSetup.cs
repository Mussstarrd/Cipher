#nullable enable
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Configures the character FBX rigs so imported clips can actually drive them.
    ///
    /// The bug this fixes: the models imported with animationType Generic but avatarSetup NoAvatar.
    /// A generic rig with no avatar cannot be animated by an AnimatorController at all, so every
    /// character sat in its bind pose looking exactly like a working import that simply had not been
    /// told to move. Nothing errors; they just stand there in a T-pose.
    ///
    /// The characters also need the SAME avatar as the shared Animations.fbx, because Quaternius
    /// ships one clip library for the whole pack rather than clips per character. Copying the avatar
    /// across is what lets one Walk clip drive all seven.
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
                Debug.Log($"[RigSetup] {Path.GetFileName(path)}: generic rig, avatar copied");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RigSetup] configured {done} character rigs");
        }
    }
}
