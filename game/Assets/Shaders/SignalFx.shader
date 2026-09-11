// Everything the SIGNAL weapons draw: the EMP burst, the emitter lance, the dying implant on a
// chipped citizen, and the jammer field a Brush Hog lays on the ground.
//
// ADR-003 took the bullets away. The player is not shooting people, he is killing the chip in
// their head, and that has to be legible in one frame or the premise never reaches the screen.
// So this is a shader for LIGHT rather than for matter: unlit, depth-read but never depth-write,
// and blended premultiplied so one material can draw both a hard white core that hides what is
// behind it and a faint outer glow that only adds to it.
//
// THE HOLLOW SHELL IS THE WHOLE REASON THIS EXISTS. An expanding solid sphere is a balloon, and
// a balloon is what the old orange fireball was. A shell that is bright at its silhouette and
// empty through the middle is a shockwave -- you see the far side of it through the near side,
// which is what tells the eye it is a surface of energy rather than an object. That is a
// view-dependent rim term, which the cel-lit InstancedLit shader has no way to express, and it
// is the one thing worth a second shader for.
//
// The rim is QUANTISED into flat bands before it is used. A smooth fresnel is a photographic
// glow and sits badly beside an inked, banded world; three or four hard steps read as something
// somebody drew. Same argument as the two-tone blob shadow.
//
// VARIANT BUDGET. One pass, instancing and fog, nothing else. No Fallback -- a fallback to URP
// Lit drags that shader's entire uber-variant set into the build and the build stops finishing
// (CLAUDE.md, learned twice). No shader_feature, no lighting keywords, no shadow keywords.
// Everything that varies per effect varies PER INSTANCE, not per keyword, which is why the rim
// power, the band count and the occlusion all arrive in an instanced vector.
Shader "Exodus/SignalFx"
{
    Properties
    {
        [MainColor] _BaseColor("Colour", Color) = (0.45, 0.86, 1.0, 1.0)
        // Defaults describe a solid, unbanded, fully occluding blob -- the safe shape for anything
        // drawn without per-instance data (a non-instanced Graphics.DrawMesh falls back to these).
        _RimPower("Rim Power", Float) = 2.5
        _RimStrength("Rim Strength", Range(0, 1)) = 0
        _Bands("Rim Bands", Float) = 3
        _Occlude("Occlusion", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+60"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "SignalFx"
            Tags { "LightMode" = "UniversalForward" }

            // Premultiplied alpha. The alpha channel is how much of the background this HIDES and
            // the colour channel is how much light it ADDS, so the same shader covers the opaque
            // white core (alpha 1) and the additive outer glow (alpha 0) without a second blend
            // state or a second material.
            Blend One OneMinusSrcAlpha
            // No depth write: these overlap constantly, several layers of one burst occupy the
            // same metre of air, and a depth-writing shell would clip its own arcs.
            ZWrite Off
            // ZTest on, though: a burst behind a wall must be behind the wall. An effect that
            // ignores the world is the tell that separates a game effect from a screen overlay.
            ZTest LEqual
            // Two-sided. A shell wants its far hemisphere drawn -- seeing through the near side to
            // the rim on the other is exactly what makes it read as hollow rather than as a ball.
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
                float  _RimPower;
                float  _RimStrength;
                float  _Bands;
                float  _Occlude;
            CBUFFER_END

            // Colour AND shape per instance, so a whole burst -- core, shell, echo, ring, arcs --
            // is a handful of draw calls rather than a material per stage per colour.
            //   _InstanceColor : rgb = emitted colour, a = strength (0 kills the instance)
            //   _InstanceParams: x = rim power, y = rim strength, z = bands, w = occlusion
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceParams)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float  fogCoord   : TEXCOORD2;
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
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                float4 p    = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceParams);

                // Alpha is the signature for "this instance was given data", the same trick the
                // blob shadows use: an instance that set nothing reads zero and falls back to the
                // material, which keeps a plain Graphics.DrawMesh working for one-off debugging.
                float  strength  = tint.a;
                float3 colour    = tint.rgb;
                float  rimPower  = p.x;
                float  rimAmount = p.y;
                float  bands     = p.z;
                float  occlude   = p.w;
                if (strength <= 0.0)
                {
                    colour    = _BaseColor.rgb;
                    strength  = _BaseColor.a;
                    rimPower  = _RimPower;
                    rimAmount = _RimStrength;
                    bands     = _Bands;
                    occlude   = _Occlude;
                }

                float3 N = normalize(input.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(input.positionWS));
                // abs(), not saturate(): with Cull Off the far hemisphere arrives with its normals
                // pointing away and would otherwise come back fully rimmed -- a solid disc instead
                // of a shell, which is the exact failure this shader exists to avoid.
                float ndv = saturate(abs(dot(N, V)));
                float rim = pow(1.0 - ndv, max(rimPower, 0.01));

                // Flat steps, not a gradient. Rounded rather than floored so the brightest band
                // actually reaches full rather than topping out one step short.
                float b = max(bands, 1.0);
                rim = saturate(floor(rim * b + 0.5) / b);

                // rimAmount 0 -> a solid shape; 1 -> a pure shell that is empty in the middle.
                float shape = lerp(1.0, rim, saturate(rimAmount));

                // The rim is not just more opaque, it is HOTTER: energy piles up at a silhouette.
                // Pushing the colour toward white at the edge is what stops a blue shell reading
                // as blue plastic.
                //
                // SCALED BY rimAmount, which matters more than it looks. Without that factor a
                // SOLID instance still takes the hot rim, and every thin bar in the game -- each
                // arc, each beam dash -- is seen close to edge-on, lands at rim 1 and comes out
                // WHITE. The first build drew a cyan weapon as a row of white sticks for exactly
                // this reason. Only shapes that asked to be shells get the hot edge.
                float3 emissive = lerp(colour, min(colour * 1.9 + 0.35, 1.6), rim * saturate(rimAmount));

                float a = saturate(strength * shape);

                // Additive effects should fade toward NOTHING in the haze, not toward the haze's
                // own grey -- MixFog would turn a distant burst into a pale grey box in the air.
                //
                // GUARDED, AND THE GUARD IS THE POINT. ComputeFogIntensity returns 0, not 1, when
                // no fog keyword is defined -- so an unguarded multiply does not merely skip the
                // fog, it multiplies the entire effect by zero. The first build of this shader
                // drew every lance and every dash as a solid BLACK shape with correct geometry,
                // which looks like a colour bug and is actually a missing #if. MixFog has the
                // opposite default and would have been safe; it is just wrong for additive.
                real fogIntensity = 1.0;
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    fogIntensity = ComputeFogIntensity(input.fogCoord);
                #endif
                emissive *= fogIntensity;

                clip(a - 0.004);
                return half4(emissive * a, a * saturate(occlude));
            }
            ENDHLSL
        }
    }
}
