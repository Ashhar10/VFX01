Shader "VFX/VerticalCrescent"
{
    Properties
    {
        [HDR] _Color ("Blade Body Color", Color) = (8.0, 4.5, 1.0, 1.0)
        [HDR] _CoreColor ("White-Hot Leading Edge", Color) = (16.0, 14.0, 8.0, 1.0)
        [HDR] _HaloColor ("Soft Amber Halo", Color) = (4.0, 2.0, 0.4, 1.0)
        _Brightness ("Brightness Multiplier", Float) = 1.8
        _Sharpness ("Leading Edge Sharpness", Float) = 3.0
        _MainTex ("Energy Texture (Optional)", 2D) = "white" {}
        _NoiseTex ("Noise Texture (Optional)", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _CoreColor;
                float4 _HaloColor;
                float _Brightness;
                float _Sharpness;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Map UV to centered coordinate p in [-1, 1]
                // p.x: -1 (concave hollow inner bite) to +1 (convex cutting front)
                // p.y: -1 (bottom tip) to +1 (top tip)
                float2 p = (input.uv - 0.5) * 2.0;

                // 1. Crescent Moon Signed Distance Field (SDF)
                // Outer circle: radius rOut, centered at (0, 0)
                // Inner cutting circle: radius rIn, centered at (-0.42, 0)
                float rOut = 0.94;
                float rIn  = 0.88;
                float2 innerCenter = float2(-0.42, 0.0);

                float dOut = length(p);
                float dIn  = length(p - innerCenter);

                float distToOuter = rOut - dOut;
                float distToInner = dIn - rIn;

                // 2. Smooth Masks
                float bladeMask = smoothstep(0.0, 0.04, distToOuter) * smoothstep(0.0, 0.04, distToInner);
                float haloMask  = smoothstep(-0.16, 0.0, distToOuter) * smoothstep(-0.12, 0.0, distToInner);

                if (haloMask <= 0.001)
                    discard;

                // Blade thickness: 0 at tips and edges, peaks in middle of blade body
                float thickness = min(distToOuter, distToInner);
                float normThickness = saturate(thickness / 0.18);

                // 3. Razor-Sharp Leading Cutting Edge (at outer convex rim where p.x > -0.2)
                float cuttingEdgeZone = saturate((p.x + 0.3) * 1.2);
                float leadEdge = smoothstep(0.12, 0.01, distToOuter) * cuttingEdgeZone;
                float sharpCore = pow(leadEdge, _Sharpness);

                // 4. Energy Ripple Noise along the crescent arc
                float angle = atan2(p.y, p.x + 0.42);
                float arcRadius = length(p);
                float2 noiseCoord = float2(angle * 0.8 - _Time.y * 3.5, arcRadius * 3.0);
                float energyNoise = sin(noiseCoord.x * 4.0) * 0.15 + sin(noiseCoord.x * 9.0 + noiseCoord.y * 2.0) * 0.1 + 0.75;

                // 5. Color Blending
                float3 bodyCol = _Color.rgb * (normThickness * energyNoise);
                float3 coreCol = _CoreColor.rgb * (leadEdge * 1.5 + sharpCore * 2.0);
                float3 haloCol = _HaloColor.rgb * (haloMask - bladeMask * 0.7) * 0.5;

                float4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float3 finalRGB = (bladeMask * (bodyCol + coreCol) + haloCol) * texCol.rgb * _Brightness;
                float finalAlpha = saturate(bladeMask * (normThickness + leadEdge * 1.2) + haloMask * 0.4);

                return float4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }

    // Built-in Render Pipeline Fallback
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _Color;
            float4 _CoreColor;
            float4 _HaloColor;
            float _Brightness;
            float _Sharpness;
            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - 0.5) * 2.0;

                float rOut = 0.94;
                float rIn  = 0.88;
                float2 innerCenter = float2(-0.42, 0.0);

                float dOut = length(p);
                float dIn  = length(p - innerCenter);

                float distToOuter = rOut - dOut;
                float distToInner = dIn - rIn;

                float bladeMask = smoothstep(0.0, 0.04, distToOuter) * smoothstep(0.0, 0.04, distToInner);
                float haloMask  = smoothstep(-0.16, 0.0, distToOuter) * smoothstep(-0.12, 0.0, distToInner);

                if (haloMask <= 0.001)
                    discard;

                float thickness = min(distToOuter, distToInner);
                float normThickness = saturate(thickness / 0.18);

                float cuttingEdgeZone = saturate((p.x + 0.3) * 1.2);
                float leadEdge = smoothstep(0.12, 0.01, distToOuter) * cuttingEdgeZone;
                float sharpCore = pow(leadEdge, _Sharpness);

                float3 bodyCol = _Color.rgb * normThickness;
                float3 coreCol = _CoreColor.rgb * (leadEdge * 1.5 + sharpCore * 2.0);
                float3 haloCol = _HaloColor.rgb * (haloMask - bladeMask * 0.7) * 0.5;

                float3 finalRGB = (bladeMask * (bodyCol + coreCol) + haloCol) * _Brightness;
                float finalAlpha = saturate(bladeMask * (normThickness + leadEdge * 1.2) + haloMask * 0.4);

                return fixed4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
}
