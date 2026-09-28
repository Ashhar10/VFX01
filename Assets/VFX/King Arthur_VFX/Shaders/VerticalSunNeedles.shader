Shader "VFX/VerticalSunNeedles"
{
    Properties
    {
        [HDR] _Color ("Golden Sunbeam Aura (HDR)", Color) = (15, 9, 1.5, 1)
        [HDR] _CoreColor ("White-Hot Base Core (HDR)", Color) = (20, 18, 12, 1)
        _Brightness ("Overall Brightness", Range(0.5, 15)) = 3.5
        _NeedleSharpness ("Horizontal Sharpness", Range(1, 10)) = 3.0
        _HeightProgress ("Height Shoot Progress (0 to 1)", Range(0, 1)) = 1.0
        _Dissolve ("Fade / Dissolve", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+25"
        }

        Pass
        {
            Name "VerticalSunNeedles"
            Blend One One // Pure Additive: radiant sun rays
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
                half4 _Color;
                half4 _CoreColor;
                half _Brightness;
                half _NeedleSharpness;
                half _HeightProgress;
                half _Dissolve;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Animate height scaling dynamically in vertex shader if desired
                float3 pos = IN.positionOS.xyz;
                pos.y *= saturate(_HeightProgress);

                OUT.positionCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // uv.x: across width (0 to 1, center at 0.5)
                // uv.y: along height (0 at base, 1 at top tip)

                // 1. Horizontal cross-section: razor sharp core in middle, smooth falloff to edges
                float xDist = abs(IN.uv.x - 0.5) * 2.0;
                float horizProfile = saturate(1.0 - xDist);
                horizProfile = pow(horizProfile, _NeedleSharpness);

                // 2. Vertical height profile:
                // Needle tapers to sharp point at the top
                float vertProgress = saturate(IN.uv.y / max(_HeightProgress, 0.01));
                if (vertProgress > 1.0)
                    return half4(0, 0, 0, 0);

                // Taper to sharp tip
                float vertFade = saturate(1.0 - vertProgress);
                vertFade = pow(vertFade, 1.2);

                // Intense white-hot grounding at the base touching the floor ring
                float baseCore = saturate(1.0 - vertProgress * 3.0);
                baseCore = pow(baseCore, 2.0) * horizProfile;

                // Overall needle shape
                float needleIntensity = horizProfile * vertFade;

                // Dissolve / fade
                float dissolveFactor = saturate(1.0 - _Dissolve);
                needleIntensity *= dissolveFactor;

                // Color accumulation: White-hot base core + golden radiant sunbeam body
                half3 coreEmission = _CoreColor.rgb * (baseCore * 2.5 + needleIntensity * 0.8);
                half3 auraEmission = _Color.rgb * needleIntensity;

                half3 finalRGB = (coreEmission + auraEmission) * _Brightness * IN.color.rgb;
                finalRGB = MixFog(finalRGB, IN.fogFactor);

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Particles/Unlit"
}
