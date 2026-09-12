// Rain, drawn as ink strokes rather than simulated water.
//
// The comic direction decides almost everything here. Photoreal rain is a refractive, motion-
// blurred, depth-sorted thing; a drawn rainstorm is a set of hard parallel diagonal lines with a
// soft cap at each end, and that is both what this game wants and what costs nothing. There is no
// texture: the streak is generated from the quad's own UV, so the whole effect ships as eight
// variants and zero bytes of asset.
//
// DELIBERATELY UNLIT. Rain that responds to the sun would need the lighting keyword set, which is
// the entire variant explosion described in CLAUDE.md, to produce a difference nobody can see
// through the fog. The streak colour comes from the instance, and the instance colour is chosen
// against the weather profile on the CPU where it is free.
//
// DELIBERATELY NOT OUTLINED. An inverted hull around a raindrop is a black rectangle.
//
// Variant budget: ONE pass, instancing and fog, nothing else. No shader_features, no lighting
// keywords, and -- the rule that has cost this project two sessions -- NO Fallback. A fallback to
// URP Lit drags its entire 589k-variant set into the build.
Shader "Exodus/Rain"
{
    Properties
    {
        [MainColor] _BaseColor("Streak Colour", Color) = (0.86, 0.89, 0.95, 0.35)
        _Softness("End Softness", Range(0.01, 0.5)) = 0.22
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "RainStreak"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            // No depth write. Hundreds of these overlap every frame and they are thin enough that
            // sorting them against each other is invisible; writing depth would z-fight and would
            // also punch holes in the characters standing behind them.
            ZWrite Off
            ZTest LEqual
            // Two-sided: the streaks are yawed to the camera once per frame for the whole field,
            // so an individual drop can end up facing away. A culled streak is a missing streak.
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
                float  _Softness;
            CBUFFER_END

            // Per-instance colour AND alpha: this is how nine hundred streaks get nine hundred
            // different opacities in one draw call. Same pattern BlobShadow and InstancedLit use.
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

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
                output.uv = input.uv;
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 instanced = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                // Alpha is the signature of "an instance set this", exactly as in BlobShadow:
                // zero means nobody wrote one and the material's own colour is correct.
                half4 tint = (instanced.a > 0.0h) ? half4(instanced) : half4(_BaseColor);

                // The stroke. Solid across the middle of the quad's width and faded at both ends
                // of its length, which is what gives a drawn streak its taper without a texture.
                half acrossW = 1.0h - saturate(abs(input.uv.x - 0.5h) * 2.0h);
                half acrossW2 = smoothstep(0.0h, 0.35h, acrossW);
                half alongL  = smoothstep(0.0h, (half)_Softness, input.uv.y)
                             * smoothstep(0.0h, (half)_Softness, 1.0h - input.uv.y);

                half alpha = tint.a * acrossW2 * alongL;
                clip(alpha - 0.004h);

                // Fade into the haze with everything else. A streak that stays bright at fifty
                // metres while the treeline behind it has dissolved reads as a scratch on the lens.
                half3 rgb = MixFog(tint.rgb, input.fogCoord);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
