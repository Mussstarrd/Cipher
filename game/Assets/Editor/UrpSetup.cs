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

            // Soft shadows FIRST, and through SerializedObject, because supportsSoftShadows is
            // read-only in URP 17 -- there is no setter, only the serialized field. Done before the
            // plain property writes below so ApplyModifiedProperties cannot stamp a stale snapshot
            // back over them.
            //
            // ApplyOvercastWinter has set light.shadows = Soft since the winter pass, but the
            // PIPELINE has the final say and this asset shipped with soft shadows unsupported, so
            // every shadow in the game was a one-tap hard edge under an overcast sky that has no
            // hard edges in it. Setting it here as well as in the .asset keeps a regenerated
            // pipeline from quietly dropping back.
            var pipelineSo = new SerializedObject(pipeline);
            var soft = pipelineSo.FindProperty("m_SoftShadowsSupported");
            if (soft == null) Debug.LogWarning("[UrpSetup] m_SoftShadowsSupported not found; did URP rename it?");
            else if (!soft.boolValue)
            {
                soft.boolValue = true;
                pipelineSo.ApplyModifiedProperties();
                Debug.Log("[UrpSetup] enabled soft shadows");
            }

            // Overcast Virginia winter: one strong directional light, cascaded shadows out to the
            // far end of the play area, HDR on for the fires and muzzle flashes.
            pipeline.supportsHDR = true;
            pipeline.shadowDistance = 140f;
            pipeline.shadowCascadeCount = 3;
            pipeline.msaaSampleCount = 2;
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            // Keep our shaders compiled in players: every material in this game is made at runtime,
            // so the variant stripper cannot see any of them and a missing entry here is a silent
            // failure, not an error.
            AlwaysInclude("Exodus/InstancedLit", "Exodus/ComicSky", "Exodus/BlobShadow");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UrpSetup] URP configured.");
        }

        /// <summary>
        /// Adds shaders to Always Included, idempotently. Named rather than typed because the only
        /// handle a runtime-built material has on a shader is its name.
        /// </summary>
        private static void AlwaysInclude(params string[] names)
        {
            var so = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var always = so.FindProperty("m_AlwaysIncludedShaders");
            bool changed = false;

            foreach (string name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"[UrpSetup] {name} not found; is the shader importing?");
                    continue;
                }

                bool present = false;
                for (int i = 0; i < always.arraySize; i++)
                {
                    if (always.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
                }
                if (present) continue;

                always.InsertArrayElementAtIndex(always.arraySize);
                always.GetArrayElementAtIndex(always.arraySize - 1).objectReferenceValue = shader;
                changed = true;
                Debug.Log($"[UrpSetup] added {name} to Always Included Shaders");
            }

            if (changed) so.ApplyModifiedProperties();
        }
    }
}
