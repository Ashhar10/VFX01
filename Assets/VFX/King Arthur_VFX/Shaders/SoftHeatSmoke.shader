Shader "VFX/SoftHeatSmoke"
{
    Properties
    {
        _MainTex ("Smoke Texture (Optional)", 2D) = "white" {}
        [HDR] _Color ("Smoke / Vapor Tint (HDR)", Color) = (1.5, 1.0, 0.4, 0.3)
        _CoreBrightness ("Core Brightness", Range(0.5, 5)) = 1.8
        _Softness ("Edge Softness", Range(0.1, 1)) = 0.5
        _Dissolve ("Dissolve / Fade", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+20"
        }

        Pass
        {
            Name "SoftHeatSmoke"
            Blend SrcAlpha One
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
                half _CoreBrightness;
                half _Softness;
                half _Dissolve;
            CBUFFER_END

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

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
                // Centered coordinates [-1, 1]
                float2 centeredUV = (IN.uv - 0.5) * 2.0;
                float dist = length(centeredUV);

                if (dist >= 1.0)
                    return half4(0, 0, 0, 0);

                // Sample texture falloff if provided (e.g. SoftCircle.png)
                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Procedural smoke turbulence
                float n = smoothNoise(IN.uv * 4.0 + float2(_Time.y * 0.4, _Time.y * 0.7));
                float distortedDist = dist + (n - 0.5) * 0.25;

                // Soft radial billow: strictly 0 at edges
                float radialFalloff = saturate(1.0 - dist);
                float billow = 1.0 - smoothstep(1.0 - _Softness, 1.0, distortedDist);
                float alpha = saturate(billow * radialFalloff);
                alpha = pow(alpha, 1.5);

                // Texture alpha & color
                alpha *= texCol.a * texCol.r;

                // Dissolve / lifetime fade
                alpha *= saturate(1.0 - _Dissolve);
                alpha *= IN.color.a;

                if (alpha <= 0.001)
                    return half4(0, 0, 0, 0);

                // Warm glowing vapor color
                half3 col = _Color.rgb * _CoreBrightness * IN.color.rgb;
                col = MixFog(col, IN.fogFactor);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Particles/Unlit"
}
