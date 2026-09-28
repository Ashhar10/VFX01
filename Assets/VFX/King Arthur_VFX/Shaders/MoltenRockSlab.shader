Shader "VFX/MoltenRockSlab"
{
    Properties
    {
        _BaseColor ("Charred Rock Color", Color) = (0.13, 0.10, 0.08, 1.0)
        [HDR] _MoltenColor ("Molten Lava Glow (HDR)", Color) = (14, 5.5, 0.8, 1.0)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
        _MoltenHeight ("Molten Glow Cutoff Height", Range(-0.2, 0.8)) = 0.12
        _MoltenSoftness ("Molten Softness", Range(0.01, 0.4)) = 0.10
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float4 color : COLOR;
                float fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _MoltenColor;
                half _Smoothness;
                half _MoltenHeight;
                half _MoltenSoftness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionOS = IN.positionOS.xyz;
                OUT.color = IN.color;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);

                // Main Directional Light
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = _BaseColor.rgb * (mainLight.color * (NdotL * 0.8 + 0.2));

                // Molten magma emission restricted to the base of the rock slab
                float h = IN.positionOS.y;
                float moltenGrad = 1.0 - smoothstep(_MoltenHeight - _MoltenSoftness, _MoltenHeight + _MoltenSoftness, h);
                float vertMask = any(IN.color) ? IN.color.r : 1.0;
                float moltenFactor = saturate(moltenGrad * vertMask);

                half3 moltenEmission = _MoltenColor.rgb * (moltenFactor * 1.6);
                half3 finalColor = diffuse + moltenEmission;

                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
