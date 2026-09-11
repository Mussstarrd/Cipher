Shader "Exodus/ComicSky"
{
    // An overcast winter sky, drawn the way the rest of the game is drawn: in flat bands with hard
    // edges, not a smooth gradient. A photoreal sky over cel-shaded ground is the single fastest way
    // to make stylised art look unfinished, because the eye reads the mismatch before anything else.
    //
    // Everything here is procedural. There are no textures in this project and this keeps it that
    // way: a value-noise cloud field, quantised into two or three tones, drifting slowly.
    Properties
    {
        _SkyTop      ("Sky Top",        Color) = (0.42, 0.50, 0.60, 1)
        _SkyHorizon  ("Sky Horizon",    Color) = (0.74, 0.77, 0.80, 1)
        _CloudLight  ("Cloud Light",    Color) = (0.86, 0.87, 0.88, 1)
        _CloudDark   ("Cloud Dark",     Color) = (0.55, 0.58, 0.64, 1)
        _GroundHaze  ("Below Horizon",  Color) = (0.58, 0.55, 0.50, 1)

        _Bands       ("Sky Bands",      Range(2, 12)) = 6
        _CloudCover  ("Cloud Cover",    Range(0, 1))  = 0.62
        _CloudScale  ("Cloud Scale",    Range(0.5, 8)) = 2.4
        _CloudDrift  ("Cloud Drift",    Range(0, 0.2)) = 0.012
        _HorizonSoft ("Horizon Softness", Range(0.01, 0.6)) = 0.18
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SkyTop;
                float4 _SkyHorizon;
                float4 _CloudLight;
                float4 _CloudDark;
                float4 _GroundHaze;
                float  _Bands;
                float  _CloudCover;
                float  _CloudScale;
                float  _CloudDrift;
                float  _HorizonSoft;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS      : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.dirWS = input.positionOS.xyz;
                // A skybox is drawn on a unit cube around the camera; push it to the far plane.
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionCS.z = o.positionCS.w * UNITY_RAW_FAR_CLIP_VALUE;
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);           // smoothstep, so cells do not show
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float Clouds(float2 p)
            {
                // Three octaves is enough for overcast; more just turns to grey mush once banded.
                float n  = ValueNoise(p) * 0.55;
                n += ValueNoise(p * 2.1 + 7.3) * 0.30;
                n += ValueNoise(p * 4.3 + 19.7) * 0.15;
                return n;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.dirWS);

                // Vertical gradient, QUANTISED. This is the whole look: a sky in four or five flat
                // steps reads as printed, the same way the banded lambert on the characters does.
                float up = saturate(dir.y);
                float bands = max(2.0, floor(_Bands));
                float stepped = floor(up * bands) / (bands - 1.0);
                float3 sky = lerp(_SkyHorizon.rgb, _SkyTop.rgb, saturate(stepped));

                // Clouds, projected onto a dome. Dividing by y spreads them toward the horizon,
                // which is what gives an overcast sky its sense of distance.
                float2 uv = dir.xz / max(0.22, dir.y + 0.28) * _CloudScale;
                uv += _Time.y * _CloudDrift * float2(1.0, 0.35);

                float n = Clouds(uv);
                float cover = 1.0 - _CloudCover;
                // Two hard thresholds: a light body and a darker underside. Flat shapes, no feather.
                float body  = step(cover, n);
                float under = step(cover + 0.14, n);
                float3 cloud = lerp(_CloudDark.rgb, _CloudLight.rgb, under);

                // Clouds thin out at the horizon rather than stopping at a line.
                float horizonFade = smoothstep(0.0, _HorizonSoft, dir.y);
                float3 colour = lerp(sky, cloud, body * horizonFade);

                // Below the horizon the world is haze, so the ground plane's edge does not read as a
                // cliff into the void.
                float below = smoothstep(0.0, -_HorizonSoft, dir.y);
                colour = lerp(colour, _GroundHaze.rgb, below);

                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
