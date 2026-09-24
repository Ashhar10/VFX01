Shader "VFX/AdditiveParticle"
{
    Properties
    {
        [HDR] _Color ("Particle Color", Color) = (15.0, 10.0, 2.5, 1.0)
        _Brightness ("Brightness Multiplier", Float) = 3.5
        _StarIntensity ("Diamond Star Sparkle", Float) = 2.5
        _CoreSharpness ("Core Radial Sharpness", Float) = 5.0
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
                float _Brightness;
                float _StarIntensity;
                float _CoreSharpness;
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
                // Procedural 4-point diamond sparkle star
                float2 p = (input.uv - 0.5) * 2.0;
                float r = length(p);

                float rayH = saturate(1.0 - abs(p.y) * 7.0) * saturate(1.0 - abs(p.x));
                float rayV = saturate(1.0 - abs(p.x) * 7.0) * saturate(1.0 - abs(p.y));
                float star = max(rayH, rayV) * _StarIntensity;

                float core = pow(saturate(1.0 - r), _CoreSharpness) * 2.5;
                float glow = pow(saturate(1.0 - r), 2.0) * 0.5;

                float proceduralSpark = star + core + glow;

                float4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float3 finalRGB = _Color.rgb * input.color.rgb * texCol.rgb * proceduralSpark * _Brightness;
                float finalAlpha = saturate(proceduralSpark * input.color.a * texCol.a);

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
            float _Brightness;
            float _StarIntensity;
            float _CoreSharpness;
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
                float r = length(p);

                float rayH = saturate(1.0 - abs(p.y) * 7.0) * saturate(1.0 - abs(p.x));
                float rayV = saturate(1.0 - abs(p.x) * 7.0) * saturate(1.0 - abs(p.y));
                float star = max(rayH, rayV) * _StarIntensity;
                float core = pow(saturate(1.0 - r), _CoreSharpness) * 2.5;

                float proceduralSpark = star + core;
                float4 texCol = tex2D(_MainTex, i.uv);

                float3 finalRGB = _Color.rgb * i.color.rgb * texCol.rgb * proceduralSpark * _Brightness;
                return fixed4(finalRGB, saturate(proceduralSpark * i.color.a));
            }
            ENDCG
        }
    }
}
