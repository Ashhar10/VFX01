Shader "VFX/ScreenDistortionRing"
{
    Properties
    {
        [Header(Distortion)]
        _DistortionStrength ("Refraction / Distortion Strength", Range(0, 0.2)) = 0.05
        _RingWidth ("Ring Width", Range(0.01, 0.3)) = 0.08
        _ChromaticAberration ("Chromatic Aberration Spread", Range(0, 0.05)) = 0.015

        [Header(Glow Edge)]
        [HDR] _GlowColor ("Shockwave Rim Glow (HDR)", Color) = (15, 10, 3, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.5

        [Header(Animation)]
        _Radius ("Ring Current Radius (0 to 1)", Range(0, 1)) = 0.5
        _Dissolve ("Fade / Dissolve (0 to 1)", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
        }

        Pass
        {
            Name "ScreenDistortionRing"
            Blend One One // Pure Additive Shockwave: Immune to missing opaque texture or black artifacts
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float fogFactor : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half _DistortionStrength;
                half _RingWidth;
                half _ChromaticAberration;
                half4 _GlowColor;
                half _GlowIntensity;
                half _Radius;
                half _Dissolve;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = any(IN.color) ? IN.color : float4(1, 1, 1, 1);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Centered coordinates [-1, 1]
                float2 centeredUV = (IN.uv - 0.5) * 2.0;
                float r = length(centeredUV);

                if (r >= 1.0)
                    return half4(0, 0, 0, 0);

                // Animated shockwave ring expanding
                float currentR = _Radius;
                float distFromRing = abs(r - currentR);

                // Wave profile: Gaussian bell curve across shockwave ridge
                float wave = 1.0 - smoothstep(0.0, _RingWidth, distFromRing);
                wave = pow(saturate(wave), 2.2);

                // Fade out as ring expands and dissolves
                float lifeFade = saturate(1.0 - _Dissolve) * saturate(1.0 - currentR * 0.85);
                float wavePower = wave * lifeFade * IN.color.a;

                if (wavePower <= 0.001)
                    return half4(0, 0, 0, 0);

                // Golden radiant rim glow along the shockwave ridge
                half3 glow = _GlowColor.rgb * (wavePower * _GlowIntensity);
                glow = MixFog(glow, IN.fogFactor);

                return half4(glow, wavePower);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Particles/Unlit"
}
