Shader "VFX/NecromanticSmoke"
{
    Properties
    {
        [Header(Color and Shading)]
        _Color ("Smoke Deep Color (Charcoal Purple)", Color) = (0.12, 0.05, 0.18, 0.35)
        _RimColor ("Rim Highlight (Luminous Violet)", Color) = (0.60, 0.30, 0.85, 0.60)
        _Brightness ("Brightness Multiplier", Float) = 1.0
        _MainTex ("Smoke Texture (RGBA)", 2D) = "white" {}
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
            Blend SrcAlpha OneMinusSrcAlpha
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
                float4 _RimColor;
                float _Brightness;
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
                float4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // texCol.r = density, texCol.g = billow folds, texCol.b = rim mask
                float3 baseCol = _Color.rgb * input.color.rgb;
                float3 finalRGB = lerp(baseCol, _RimColor.rgb, texCol.b * 0.85) * _Brightness;
                float finalAlpha = texCol.a * _Color.a * input.color.a;

                return float4(finalRGB, saturate(finalAlpha));
            }
            ENDHLSL
        }
    }

    // Built-in Render Pipeline & Material Preview Fallback
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
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
            float4 _RimColor;
            float _Brightness;
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
                float4 texCol = tex2D(_MainTex, i.uv);
                float3 baseCol = _Color.rgb * i.color.rgb;
                float3 finalRGB = lerp(baseCol, _RimColor.rgb, texCol.b * 0.85) * _Brightness;
                float finalAlpha = texCol.a * _Color.a * i.color.a;
                return fixed4(finalRGB, saturate(finalAlpha));
            }
            ENDCG
        }
    }
}
