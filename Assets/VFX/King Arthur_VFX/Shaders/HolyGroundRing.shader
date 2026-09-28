Shader "VFX/HolyGroundRing"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _Color ("Golden Aura Tint (HDR)", Color) = (14, 8.5, 2, 1)
        [HDR] _CoreColor ("White-Hot Needle Core (HDR)", Color) = (20, 18, 12, 1)

        [Header(Ring Dimensions)]
        _Radius ("Ring Outer Radius", Range(0.2, 1.0)) = 0.85
        _Thickness ("Outer Ring Thickness", Range(0.01, 0.25)) = 0.05
        _InnerRingRadius ("Inner Ring Radius", Range(0.1, 0.85)) = 0.52

        [Header(Sunburst Needles)]
        _RaysCount ("Floor Ray Count", Range(8, 64)) = 32
        _NeedleSharpness ("Needle Sharpness", Range(1, 12)) = 6.0
        _NeedleLength ("Needle Length", Range(0.05, 0.8)) = 0.35

        [Header(Silhouette Protection)]
        _CenterDampening ("Center Glow Dampening (Keeps Character Readable)", Range(0, 1)) = 0.75

        [Header(Level by Level Activation)]
        _ActivationProgress ("Activation Progress (0 to 1)", Range(0, 1)) = 1.0
        _Dissolve ("Fade / Dissolve (0 to 1)", Range(0, 1)) = 0.0
        _Brightness ("Overall Brightness", Range(0.5, 10)) = 2.8
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
        }

        Pass
        {
            Name "HolyGroundRing"
            Blend One One // Pure Additive
            ZWrite Off
            Cull Off
            Offset -2, -2

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                half _Radius;
                half _Thickness;
                half _InnerRingRadius;
                half _RaysCount;
                half _NeedleSharpness;
                half _NeedleLength;
                half _CenterDampening;
                half _ActivationProgress;
                half _Dissolve;
                half _Brightness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 centeredUV = (IN.uv - 0.5) * 2.0;
                float r = length(centeredUV);

                if (r > 1.0)
                    return half4(0, 0, 0, 0);

                float angle = atan2(centeredUV.y, centeredUV.x);

                float p = saturate(_ActivationProgress);
                float s1 = saturate(p / 0.25);
                float s2 = saturate((p - 0.20) / 0.25);
                float s3 = saturate((p - 0.40) / 0.30);
                float s4 = saturate((p - 0.65) / 0.35);

                float currentOuterRadius = _Radius * max(s1, 0.05);

                // ==========================================
                // LEVEL 1: OUTER GOLDEN PERIMETER RING
                // ==========================================
                float distOuter = abs(r - currentOuterRadius);
                float outerRing = 1.0 - smoothstep(0.0, _Thickness, distOuter);
                outerRing = pow(saturate(outerRing), 1.8) * s1;

                // ==========================================
                // LEVEL 2: INNER SACRED AURA (DAMPENED UNDER CHARACTER)
                // ==========================================
                float innerGlow = saturate(1.0 - (r / max(currentOuterRadius, 0.01)));
                innerGlow = pow(innerGlow, 3.0) * s2 * 0.35;
                // Dampen center directly beneath character feet to preserve dark armor contrast
                float centerMask = smoothstep(0.0, currentOuterRadius * 0.5, r);
                innerGlow *= lerp(1.0 - _CenterDampening, 1.0, centerMask);

                // ==========================================
                // LEVEL 3: SACRED GEOMETRIC DESIGN BETWEEN THE RINGS
                // ==========================================
                float currentInnerRadius = _InnerRingRadius * max(s1, 0.05);
                float distInner = abs(r - currentInnerRadius);
                float innerRing = (1.0 - smoothstep(0.0, _Thickness * 0.7, distInner)) * s3;

                float spokeAngle = angle * (_RaysCount * 0.5);
                float spokes = pow(sin(spokeAngle) * 0.5 + 0.5, 6.0);
                float betweenRings = smoothstep(currentInnerRadius, currentInnerRadius + 0.03, r) *
                                     smoothstep(currentOuterRadius, currentOuterRadius - 0.03, r);
                float sacredDesign = spokes * betweenRings * s3 * 1.6;

                float pipAngle = angle * 12.0;
                float pips = pow(cos(pipAngle) * 0.5 + 0.5, 10.0) * innerRing * s3 * 2.0;

                // ==========================================
                // LEVEL 4: RADIANT NEEDLE RAYS (CONCENTRATED ON PERIMETER)
                // ==========================================
                float needleWave = sin(angle * _RaysCount) * 0.5 + 0.5;
                needleWave = pow(needleWave, _NeedleSharpness);

                float needleBaseDist = abs(r - currentOuterRadius);
                float needleFade = 1.0 - smoothstep(0.0, _NeedleLength, needleBaseDist);
                float needles = needleWave * needleFade * s4 * 4.0;

                float burstCore = outerRing * s4 * 2.5;

                // ==========================================
                // DISSOLVE & COLOR ACCUMULATION
                // ==========================================
                float dissolveFactor = saturate(1.0 - _Dissolve);

                float goldenElements = (sacredDesign + innerRing + pips + innerGlow + needles * 0.5) * dissolveFactor;
                half3 goldenAura = _Color.rgb * goldenElements;

                float whiteHotElements = (outerRing * 1.8 + burstCore + needles * 1.5) * dissolveFactor;
                half3 whiteHotCore = _CoreColor.rgb * whiteHotElements;

                // Center dampening to protect character silhouette from any blooming washout
                float charCenterMask = smoothstep(0.0, currentOuterRadius * 0.45, r);
                float charDamp = lerp(1.0 - _CenterDampening, 1.0, charCenterMask);
                goldenAura *= charDamp;
                whiteHotCore *= charDamp;

                half3 finalColor = (goldenAura + whiteHotCore) * _Brightness;
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Particles/Unlit"
}
