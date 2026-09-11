#nullable enable
using System.IO;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Creates and assigns the URP pipeline assets, headlessly. Run via
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.UrpSetup.Configure -quit
    ///
    /// These are .asset files that would normally be made by clicking through the editor; doing it
    /// in code keeps the migration reproducible and lets CI rebuild it from a clean checkout.
    /// Idempotent: safe to rerun.
    /// </summary>
    public static class UrpSetup
    {
        private const string Dir = "Assets/Rendering";
        private const string RendererPath = Dir + "/ExodusRenderer.asset";
        private const string PipelinePath = Dir + "/ExodusPipeline.asset";

        [MenuItem("Cipher/Rendering/Configure URP")]
        public static void Configure()
        {
            Directory.CreateDirectory(Dir);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                Debug.Log($"[UrpSetup] created {RendererPath}");
            }

            // Depth priming off: we draw a lot of instanced geometry and the prepass costs more
            // than it saves at our overdraw. Revisit when real character meshes land.
            renderer.depthPrimingMode = DepthPrimingMode.Disabled;
            EditorUtility.SetDirty(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                Debug.Log($"[UrpSetup] created {PipelinePath}");
            }

            // Overcast Virginia winter: one strong directional light, soft cascaded shadows out to
            // the far end of the play area, HDR on for the fires and muzzle flashes.
            pipeline.supportsHDR = true;
            pipeline.shadowDistance = 140f;
            pipeline.shadowCascadeCount = 3;
            pipeline.msaaSampleCount = 2;
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            // Keep the instanced shader compiled in players: our materials are made at runtime, so
            // the variant stripper cannot see them. This is cheap now that we are not using Standard.
            var instanced = Shader.Find("Exodus/InstancedLit");
            if (instanced != null)
            {
                var included = GraphicsSettings.GetGraphicsSettings();
                var so = new SerializedObject(included);
                var always = so.FindProperty("m_AlwaysIncludedShaders");
                bool present = false;
                for (int i = 0; i < always.arraySize; i++)
                {
                    if (always.GetArrayElementAtIndex(i).objectReferenceValue == instanced) { present = true; break; }
                }
                if (!present)
                {
                    always.InsertArrayElementAtIndex(always.arraySize);
                    always.GetArrayElementAtIndex(always.arraySize - 1).objectReferenceValue = instanced;
                    so.ApplyModifiedProperties();
                    Debug.Log("[UrpSetup] added Exodus/InstancedLit to Always Included Shaders");
                }
            }
            else
            {
                Debug.LogWarning("[UrpSetup] Exodus/InstancedLit not found; is the shader importing?");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UrpSetup] URP configured.");
        }
    }
}
