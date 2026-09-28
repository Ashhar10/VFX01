Shader "VFX/SwordSlashTrail"
{
    Properties
    {
        _MainTex ("Trail Texture (Optional)", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        [HDR] _Color ("Main Energy Color (HDR)", Color) = (3, 0.6, 6, 1)
        [HDR] _EdgeColor ("Outer Rim / Tip Color (HDR)", Color) = (8, 3, 12, 1)
        [HDR] _CoreColor ("Inner Hot Core (HDR)", Color) = (10, 8, 12, 1)
        _NoiseStrength ("Distortion Strength", Range(0, 0.5)) = 0.12
        _NoiseSpeed ("Distortion Speed", Range(0, 10)) = 3.0
        _SharpEdgePower ("Cutting Edge Sharpness", Range(1, 10)) = 3.0
        _TrailingFadePower ("Trailing Fade Power", Range(0.5, 5)) = 1.8
        _Brightness ("Brightness Multiplier", Range(0.5, 10)) = 2.5
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
            Name "SwordSlashTrail"
            Blend One One // Additive
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
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4 _Color;
                half4 _EdgeColor;
                half4 _CoreColor;
                half _NoiseStrength;
                half _NoiseSpeed;
                half _SharpEdgePower;
                half _TrailingFadePower;
                half _Brightness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Noise scrolling across the trail
                float2 noiseUV = IN.uv * _NoiseTex_ST.xy + float2(-_Time.y * _NoiseSpeed, _Time.y * 0.5);
                half noiseVal = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // Distort UVs
                float2 distortedUV = IN.uv;
                distortedUV.y += (noiseVal - 0.5) * _NoiseStrength;
                distortedUV.x += (noiseVal - 0.5) * _NoiseStrength * 0.5;

                // Sample main texture (if assigned)
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                // Across blade cross-section (V: 0 = base, 1 = tip)
                // Tip (outer edge) has the sharpest, brightest cutting energy
                float bladePos = saturate(distortedUV.y);
                float cuttingEdge = pow(bladePos, _SharpEdgePower);

                // Hot core in the outer third of the blade
                float coreGlow = saturate(1.0 - abs(bladePos - 0.85) * 4.0);
                coreGlow = pow(coreGlow, 2.0);

                // Trailing fade along length (U: 0 = oldest/tail, 1 = newest/cutting blade)
                float trailAge = saturate(distortedUV.x);
                float lengthFade = pow(trailAge, _TrailingFadePower);

                // Blade root falloff (fade near the hilt so it doesn't clip awkwardly)
                float rootFade = smoothstep(0.0, 0.15, bladePos);

                // Combine alpha
                float alpha = lengthFade * rootFade * IN.color.a * mainTex.a;

                // Dissolve trailing edge with noise
                float dissolve = saturate((lengthFade - (1.0 - noiseVal) * 0.3) / 0.7);
                alpha *= dissolve;

                // Triple-color blending: base aura -> edge rim -> burning white core
                half3 aura = _Color.rgb;
                half3 rim = _EdgeColor.rgb * cuttingEdge;
                half3 core = _CoreColor.rgb * (coreGlow * 1.5 + cuttingEdge * 0.5);

                half3 finalColor = (aura + rim + core) * _Brightness * IN.color.rgb * alpha;

                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Particles/Unlit"
}
