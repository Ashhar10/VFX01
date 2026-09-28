Shader "VFX/VerticalCrescent"
{
    Properties
    {
        [HDR] _Color ("Energy Color (HDR)", Color) = (16, 7, 1, 1)
        [HDR] _CoreColor ("Leading Blade Edge (HDR)", Color) = (20, 16, 8, 1)
        _Brightness ("Brightness", Range(0.5, 10)) = 3.0
        _Sharpness ("Blade Sharpness", Range(1, 10)) = 3.5
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
            Name "VerticalCrescent"
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

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                half _Brightness;
                half _Sharpness;
            CBUFFER_END

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
                float2 uv = (IN.uv - 0.5) * 2.0;

                // Crescent arc formula: circle with an offset circular cutout
                float r = length(uv);
                if (r > 1.0) return half4(0, 0, 0, 0);

                // Offset circle to carve out crescent
                float2 cutCenter = float2(-0.35, 0.0);
                float cutDist = length(uv - cutCenter);

                // Crescent body: inside outer circle (r <= 1) and outside cut circle (cutDist >= 0.7)
                float outer = 1.0 - smoothstep(0.85, 1.0, r);
                float inner = smoothstep(0.65, 0.9, cutDist);
                float crescentShape = outer * inner;

                // Leading forward edge (right side of crescent): white-hot
                float leadingEdge = saturate(uv.x);
                leadingEdge = pow(leadingEdge, _Sharpness);

                // Vertical taper (thicker in middle, needle-sharp at top/bottom tips)
                float tipTaper = saturate(1.0 - abs(uv.y));
                tipTaper = pow(tipTaper, 1.5);

                float alpha = crescentShape * tipTaper * IN.color.a;

                // Color composition
                half3 coreEmission = _CoreColor.rgb * (leadingEdge * 2.0);
                half3 bodyEmission = _Color.rgb * crescentShape;
                half3 finalColor = (coreEmission + bodyEmission) * _Brightness * alpha * IN.color.rgb;

                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Particles/Unlit"
}
