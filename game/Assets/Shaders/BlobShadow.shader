// The flat marks that sit ON the ground: contact shadows under every body and prop, and the
// scorch rings, stains and drag marks the fight leaves behind.
//
// One shader for both, on purpose. They are the same drawing problem -- an unlit alpha stamp,
// projected flat, written without depth so a thousand of them can overlap for free -- and a
// second shader would be a second set of variants for no visual difference.
//
// Deliberately NOT lit. A contact shadow that responds to the sun is a lighting effect; the
// comic direction wants a shape that was drawn under the character, which is why the texture
// carries a hard-edged two-tone falloff rather than a soft gradient. Deliberately NOT outlined
// either: an ink contour around a shadow reads as a puddle.
//
// Variant budget: one pass, instancing and fog, nothing else. No shadow keywords, no lighting
// keywords, no shader_features. This costs single-digit variants.
Shader "Exodus/BlobShadow"
{
    Properties
    {
        [MainColor] _BaseColor("Ink Colour", Color) = (0.07, 0.08, 0.11, 0.55)
        [MainTexture] _BaseMap("Alpha Stamp", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "GroundStamp"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            // No depth write: these lie flat on the ground, they overlap constantly, and a stamp
            // that wrote depth would z-fight with the next one and clip the agents standing on it.
            ZWrite Off
            ZTest LEqual
            // Two-sided so a stamp is still visible from the tactical camera if the quad ends up
            // facing away after a yaw; a single quad culled to nothing is a silent disappearance.
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            // Per-instance tint AND alpha, which is how a decal fades without a draw call of its
            // own. Same pattern InstancedLit uses for the hit flash.
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  fogCoord   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 instanced = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                // Alpha is the signature: an instance that never set a colour reads zero and falls
                // back to the material, which is what a blob shadow wants. A decal always sets one.
                half4 tint = (instanced.a > 0.0h) ? half4(instanced) : half4(_BaseColor);

                half stamp = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                half alpha = stamp * tint.a;
                clip(alpha - 0.004h);

                // Fade to the fog colour rather than out of existence, so a distant mark settles
                // into the haze with everything else instead of staying inkier than the props.
                half3 rgb = MixFog(tint.rgb, input.fogCoord);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
