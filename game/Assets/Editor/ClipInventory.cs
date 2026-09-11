#nullable enable
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Lists every animation clip on every imported character.
    ///
    /// Exists because "what animations do we actually have" is a question worth answering with the
    /// importer rather than with a guess, and the FBXs are binary so you cannot grep them. It is
    /// also the question that decides whether we need to SPEND money on animation: the crowd walks
    /// and does nothing else, and whether that is a content gap or a pipeline gap is exactly this
    /// list.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.ClipInventory.List -quit
    /// </summary>
    public static class ClipInventory
    {
        [MenuItem("Cipher/Crowd/List Character Clips")]
        public static void List()
        {
            foreach (string dir in new[] { "Assets/Resources/Characters", "Assets/Resources/Civilians" })
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { dir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                                             .OfType<AnimationClip>()
                                             .Where(c => !c.name.StartsWith("__preview__"))
                                             .Select(c => $"{c.name}({c.length:F2}s{(c.legacy ? ",legacy" : "")})")
                                             .ToArray();
                    Debug.Log($"[Clips] {path}: {clips.Length} -> {string.Join(", ", clips)}");
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Resources" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx")) continue;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                    Debug.Log($"[Clips] standalone {path}: {clip.name} {clip.length:F2}s legacy={clip.legacy}");
            }
        }
    }
}
