Shader "VFX/SoftHeatSmoke"
{
    Properties
    {
        [HDR] _Color ("Vapor Color", Color) = (1.8, 1.2, 0.4, 0.3)
        _CoreBrightness ("Core Brightness", Float) = 1.8
        _Softness ("Edge Softness", Float) = 0.6
        _MainTex ("Particle Texture (Optional)", 2D) = "white" {}
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
                float _CoreBrightness;
                float _Softness;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

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
                float dist = length((input.uv - 0.5) * 2.0);
                float softPuff = 1.0 - smoothstep(_Softness * 0.4, 1.0, dist);
                softPuff = pow(saturate(softPuff), 1.5);

                float4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float3 finalRGB = _Color.rgb * input.color.rgb * texCol.rgb * softPuff * _CoreBrightness;
                float finalAlpha = saturate(softPuff * input.color.a * _Color.a * texCol.a);

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
            float _CoreBrightness;
            float _Softness;
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
                float dist = length((i.uv - 0.5) * 2.0);
                float softPuff = pow(saturate(1.0 - smoothstep(_Softness * 0.4, 1.0, dist)), 1.5);
                float4 texCol = tex2D(_MainTex, i.uv);

                float3 finalRGB = _Color.rgb * i.color.rgb * texCol.rgb * softPuff * _CoreBrightness;
                return fixed4(finalRGB, saturate(softPuff * i.color.a * _Color.a));
            }
            ENDCG
        }
    }
}
