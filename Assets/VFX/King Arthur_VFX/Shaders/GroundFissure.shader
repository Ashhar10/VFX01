Shader "VFX/GroundFissure"
{
    Properties
    {
        [HDR] _MoltenColor ("Molten Lava Core (HDR)", Color) = (18, 9, 1.5, 1)
        [HDR] _CrustColor ("Hot Crust Magma (HDR)", Color) = (10, 3.5, 0.6, 1)
        _RockColor ("Charred Rock Edge", Color) = (0.12, 0.09, 0.07, 1)
        _CrackIntensity ("Vein Crack Intensity", Range(1, 10)) = 5.0
        _CoreRadius ("White-Hot Core Radius", Range(0.05, 0.8)) = 0.35
        _Dissolve ("Cool / Dissolve (0 to 1)", Range(0, 1)) = 0.0
        _Brightness ("Brightness Multiplier", Range(0.5, 10)) = 2.8
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+12"
        }

        Pass
        {
            Name "GroundFissure"
            Blend SrcAlpha OneMinusSrcAlpha
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
                half4 _MoltenColor;
                half4 _CrustColor;
                half4 _RockColor;
                half _CrackIntensity;
                half _CoreRadius;
                half _Dissolve;
                half _Brightness;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                v += noise2D(p) * 0.65;
                v += noise2D(p * 2.5 + float2(1.7, 9.2)) * 0.35;
                return v;
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
                float2 centeredUV = (IN.uv - 0.5) * 2.0;
                float r = length(centeredUV);

                if (r > 1.0)
                    return half4(0, 0, 0, 0);

                // Fractal noise crack distortion
                float n = fbm(IN.uv * 7.0);
                float distortedR = r + (n - 0.5) * 0.45;
                distortedR = saturate(distortedR);

                // 1. White-hot center core
                float core = saturate(1.0 - (distortedR / max(_CoreRadius, 0.01)));
                core = pow(core, 2.0);

                // 2. Glowing molten crust & branching veins
                float crust = saturate(1.0 - distortedR);
                crust = pow(crust, 1.4);

                float veinNoise = abs(n - 0.5) * 2.0;
                float veins = saturate(1.0 - veinNoise * _CrackIntensity * 0.4) * crust;

                // 3. Charred rock border
                float rockBorder = saturate(1.0 - distortedR * 0.95);
                rockBorder = smoothstep(0.0, 0.35, rockBorder);

                // 4. Soft outer radial falloff
                float softEdge = smoothstep(1.0, 0.6, r);

                // 5. Differential Dissipation / Tail-End Cooling:
                // Edges fade out significantly faster, while the deep molten core lingers longer
                float edgeDissolve = saturate(_Dissolve * 2.2);
                float crustDissolve = saturate(_Dissolve * 1.4);
                float coreDissolve = saturate(_Dissolve * 0.85);

                float activeRock = saturate(rockBorder * (1.0 - edgeDissolve));
                float activeCrust = saturate((crust + veins * 0.5) * (1.0 - crustDissolve));
                float activeCore = saturate(core * (1.0 - coreDissolve));

                // 6. Color composition:
                half3 col = _RockColor.rgb * activeRock * 0.8;
                col += _CrustColor.rgb * activeCrust * 1.6;
                col += _MoltenColor.rgb * activeCore * _Brightness;
                col += half3(1.0, 0.95, 0.8) * (activeCore * activeCore * 3.0); // White-hot core

                float totalAlpha = (activeRock * 0.75 + activeCrust + activeCore) * softEdge * IN.color.a;
                totalAlpha = saturate(totalAlpha);

                col = MixFog(col, IN.fogFactor);

                return half4(col, totalAlpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Particles/Unlit"
}
