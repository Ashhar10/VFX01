Shader "VFX/SoftHeatSmoke"
{
    Properties
    {
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
            // Soft Additive: Blend SrcAlpha One. Mathematically CANNOT darken the screen or produce black boxes.
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

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _CoreBrightness;
                half _Softness;
                half _Dissolve;
            CBUFFER_END

            // Simple fast procedural noise for smoke turbulence
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
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Centered coordinates [-1, 1]
                float2 centeredUV = (IN.uv - 0.5) * 2.0;
                float dist = length(centeredUV);

                if (dist > 1.0)
                    return half4(0, 0, 0, 0);

                // Procedural smoke turbulence
                float n = smoothNoise(IN.uv * 5.0 + float2(_Time.y * 0.5, _Time.y * 0.8));
                float distortedDist = dist + (n - 0.5) * 0.3;

                // Soft radial billow
                float alpha = 1.0 - smoothstep(1.0 - _Softness, 1.0, distortedDist);
                alpha = pow(saturate(alpha), 1.8);

                // Dissolve / fade
                alpha *= saturate(1.0 - _Dissolve);
                alpha *= IN.color.a;

                // Warm glowing vapor color
                half3 col = _Color.rgb * _CoreBrightness * IN.color.rgb;
                col = MixFog(col, IN.fogFactor);

                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
