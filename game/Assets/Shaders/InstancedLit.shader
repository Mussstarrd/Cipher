// Comic-book instanced shader for the swarm, the walls and the props.
//
// Two reasons this is hand-written rather than URP Lit:
//
// 1. Our materials are created at RUNTIME, so the variant stripper cannot see them. A full PBR
//    uber-shader with Keep All instancing stripping compiled 589,824 variants and never finished
//    a build (see CLAUDE.md). This one has a few hundred.
// 2. The art direction is inked comic book, which PBR actively fights. Flat banded light, a hard
//    ink outline and halftone in the shadows are cheaper AND more legible at a thousand agents
//    than physically based shading, because silhouettes survive where specular mush does not.
//
// The outline is an inverted hull in a pass tagged SRPDefaultUnlit, which URP draws in addition
// to UniversalForward. No ScriptableRendererFeature and no RenderGraph work needed, and the
// outline instances exactly the way the body does.
Shader "Exodus/InstancedLit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Colour", Color) = (0.6, 0.6, 0.6, 1)

        // Optional surface. Almost everything in this game is flat colour, but the GROUND is most
        // of the screen and a single flat fill across it is most of the screen doing nothing.
        // Defaults to white so a material that sets no texture behaves exactly as it did before.
        // [MainTexture] is what binds Material.mainTexture to this property. Without it the
        // setter looks for _MainTex, finds nothing, and fails SILENTLY -- the ground came out
        // pure white because the base colour was white and the texture was never applied.
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        _OutlineColor("Outline Colour", Color) = (0.05, 0.05, 0.07, 1)
        _OutlineWidth("Outline Width", Range(0, 0.4)) = 0.035
        [Toggle(_SMOOTH_OUTLINE)] _SmoothOutline("Use Smoothed Normals For Outline", Float) = 0

        _ShadowTint("Shadow Tint", Color) = (0.32, 0.35, 0.45, 1)
        _Bands("Light Bands", Range(2, 5)) = 3
        _RimPower("Rim Power", Range(1, 12)) = 4
        _RimStrength("Rim Strength", Range(0, 1)) = 0.35

        _HalftoneScale("Halftone Dot Spacing", Range(2, 24)) = 7
        _HalftoneStrength("Halftone Strength", Range(0, 1)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma shader_feature_local _SMOOTH_OUTLINE
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float4 _ShadowTint;
                float  _Bands;
                float  _RimPower;
                float  _RimStrength;
                float  _HalftoneScale;
                float  _HalftoneStrength;
            CBUFFER_END

            struct OutlineAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;    // carries the smoothed normal when baked
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
                float  fogCoord   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            OutlineVaryings OutlineVert(OutlineAttributes input)
            {
                OutlineVaryings output = (OutlineVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // Extrude along the SMOOTHED normal where one has been baked. A character mesh splits
                // vertices at every hard edge and UV seam, so extruding along the shading normal tears
                // the hull open at each seam and the line reads as blurred and smeared.
                float3 extrudeOS = input.normalOS;
                #if defined(_SMOOTH_OUTLINE)
                    // Use the smoothed normal only where SmoothNormals actually wrote one. It stamps
                    // w = 1 on every vertex it touches, so w is the signature.
                    //
                    // Two ways this channel lies. An untouched mesh has an EMPTY tangent channel, and
                    // normalizing (0,0,0) blows the hull to NaN, which rasterises as nothing: the
                    // character silently loses its outline. A mesh SmoothNormals bailed on still
                    // carries the importer's real Mikktspace tangents, which are non-zero and point
                    // along the surface, so extruding down one shears the hull instead of expanding
                    // it. The keyword is set per material, so either can ride in on a shared one.
                    float3 smoothed = input.tangentOS.xyz;
                    if (input.tangentOS.w > 0.5 && dot(smoothed, smoothed) > 1e-8) extrudeOS = smoothed;
                #endif
                float3 normalWS = normalize(TransformObjectToWorldNormal(extrudeOS));

                // Keep the ink at a roughly constant width ON SCREEN. Scaling with raw distance made
                // far-off silhouettes balloon into blobs; scaling with the projected size does not.
                float viewDist = max(0.5, length(GetCameraPositionWS() - positionWS));
                float width = _OutlineWidth * (1.0 + viewDist * 0.012);

                positionWS += normalWS * width;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 OutlineFrag(OutlineVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 ink = _OutlineColor.rgb;
                ink = MixFog(ink, input.fogCoord);
                return half4(ink, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float4 _ShadowTint;
                float  _Bands;
                float  _RimPower;
                float  _RimStrength;
                float  _HalftoneScale;
                float  _HalftoneStrength;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  fogCoord   : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            // Classic screentone: a 45-degree dot grid in SCREEN space, so the dots stay a fixed
            // size on the page the way printed halftone does rather than sliding around in 3D.
            half Halftone(float2 screenPixels, half coverage)
            {
                float2x2 rot = float2x2(0.7071, -0.7071, 0.7071, 0.7071);
                float2 grid = mul(rot, screenPixels) / max(_HalftoneScale, 1.0);
                float2 cell = frac(grid) - 0.5;
                float dist = length(cell);
                float radius = saturate(1.0 - coverage) * 0.62;
                return 1.0h - smoothstep(radius - 0.08, radius + 0.08, dist);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 instanced = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                half3 albedo = (instanced.a > 0.0h) ? instanced.rgb : _BaseColor.rgb;
                // White by default, so this multiply is a no-op for every flat-coloured thing.
                albedo *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);

                float4 shadowCoord = float4(0, 0, 0, 0);
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord);

                // Wrapped lambert keeps the dark side readable instead of crushing to black, then
                // quantise it into hard bands. That is the whole comic look, in two lines.
                half lambert = saturate(dot(normalWS, mainLight.direction) * 0.5h + 0.5h);
                half lit = lambert * lerp(0.55h, 1.0h, mainLight.shadowAttenuation);

                half bands = max(_Bands, 2.0h);
                half stepped = saturate(floor(lit * bands) / (bands - 1.0h));

                // Shadow is a printed tint, not an absence of light.
                half3 shaded = lerp(albedo * _ShadowTint.rgb, albedo, stepped);

                // Screentone in the darkest band only, so it reads as shading and not as noise.
                half darkness = saturate(1.0h - stepped * 1.6h);
                if (_HalftoneStrength > 0.001h && darkness > 0.001h)
                {
                    half dots = Halftone(input.positionCS.xy, 1.0h - darkness);
                    shaded = lerp(shaded, albedo * _ShadowTint.rgb * 0.72h,
                                  dots * darkness * _HalftoneStrength);
                }

                // A cool rim picks the silhouette off the background, which matters enormously
                // once a thousand of these overlap.
                half rim = pow(saturate(1.0h - saturate(dot(normalWS, viewDirWS))), _RimPower);
                shaded += rim * _RimStrength * half3(0.72h, 0.80h, 0.95h);

                half3 color = MixFog(shaded, input.fogCoord);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float4 _ShadowTint;
                float  _Bands;
                float  _RimPower;
                float  _RimStrength;
                float  _HalftoneScale;
                float  _HalftoneStrength;
            CBUFFER_END

            float3 _LightDirection;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float4 _ShadowTint;
                float  _Bands;
                float  _RimPower;
                float  _RimStrength;
                float  _HalftoneScale;
                float  _HalftoneStrength;
            CBUFFER_END

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(DepthVaryings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
