Shader "VFX/GroundDecal"
{
    Properties
    {
        _MainTex ("Decal Texture", 2D) = "white" {}
        _NoiseTex ("Dissolve Noise", 2D) = "white" {}
        [HDR] _Color ("Glow Color (HDR)", Color) = (8, 3, 0.5, 1)
        [HDR] _EdgeColor ("Dissolve Edge Color (HDR)", Color) = (15, 5, 1, 1)
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0.0
        _EdgeWidth ("Dissolve Edge Width", Range(0, 0.2)) = 0.05
        _Brightness ("Brightness", Range(0, 10)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
        }

        Pass
        {
            Name "GroundDecal"
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            Offset -1, -1

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4 _Color;
                half4 _EdgeColor;
                half _DissolveAmount;
                half _EdgeWidth;
                half _Brightness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uv * _NoiseTex_ST.xy).r;

                // Dissolve
                half dissolve = noise - _DissolveAmount;
                clip(dissolve);

                // Edge glow at dissolve border
                half edgeFactor = 1.0 - smoothstep(0, _EdgeWidth, dissolve);
                half3 color = lerp(_Color.rgb, _EdgeColor.rgb, edgeFactor);

                half alpha = mainTex.a * IN.color.a * saturate(dissolve / max(_EdgeWidth, 0.001));

                color *= mainTex.rgb * _Brightness * IN.color.rgb;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
