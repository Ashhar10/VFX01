Shader "VFX/AdditiveParticle"
{
    Properties
    {
        _MainTex ("Particle Texture (Optional)", 2D) = "white" {}
        [HDR] _Color ("Color (HDR)", Color) = (5, 2, 8, 1)
        _Brightness ("Brightness", Range(0.1, 20)) = 3.0
        _CoreSharpness ("Core Sharpness", Range(1, 10)) = 4.0
        _GlowRadius ("Glow Radius", Range(0.1, 2)) = 1.0
        _StarIntensity ("Star Ray Intensity", Range(0, 5)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
        }

        Pass
        {
            Name "AdditiveParticle"
            Blend One One // Pure Additive
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Brightness;
                half _CoreSharpness;
                half _GlowRadius;
                half _StarIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = any(IN.color) ? IN.color : float4(1, 1, 1, 1);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Remap UV to centered coordinates [-1, 1]
                float2 centerUV = (IN.uv - 0.5) * 2.0;
                float dist = length(centerUV);

                if (dist >= 1.0)
                    return half4(0, 0, 0, 0);

                // 1. Soft radial glow: strictly 0 at dist == 1
                float radialGlow = saturate(1.0 - dist / max(_GlowRadius, 0.01));
                radialGlow = radialGlow * radialGlow;

                // 2. Intense white-hot center core
                float core = saturate(1.0 - dist * _CoreSharpness);
                core = pow(core, 2.5);

                // 3. Delicate 4-point diamond star rays
                float rayH = saturate(1.0 - abs(centerUV.y) * 5.0) * saturate(1.0 - abs(centerUV.x));
                float rayV = saturate(1.0 - abs(centerUV.x) * 5.0) * saturate(1.0 - abs(centerUV.y));
                float starRays = max(rayH, rayV) * _StarIntensity;

                // 4. Combined procedural shape
                float proceduralAlpha = saturate(radialGlow * 0.7 + core * 2.0 + starRays * 0.8);

                // 5. Multiply with texture if assigned (e.g. Spark.png)
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                proceduralAlpha *= texColor.a * (texColor.r * 0.8 + 0.2);

                // 6. Color composition: white-hot core transitioning to rich HDR edge
                half3 coreColor = half3(1.0, 1.0, 1.0) * (core * 2.0);
                half3 auraColor = _Color.rgb * (radialGlow + starRays);
                half3 finalColor = (coreColor + auraColor) * _Brightness * IN.color.rgb;

                // Apply vertex color alpha (particle fade over lifetime)
                float alpha = proceduralAlpha * IN.color.a;
                finalColor *= alpha;

                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Particles/Unlit"
}
