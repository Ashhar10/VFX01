Shader "VFX/GroundDecal"
{
    Properties
    {
        [HDR] _Color ("Scorch Color", Color) = (8.0, 3.0, 0.5, 1.0)
        [HDR] _EdgeColor ("Glow Edge Color", Color) = (15.0, 5.0, 1.0, 1.0)
        _DissolveAmount ("Dissolve (0-1)", Float) = 0.0
        _EdgeWidth ("Edge Glow Width", Float) = 0.08
        _Brightness ("Brightness Multiplier", Float) = 3.0
        _MainTex ("Radial Mask", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
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
                float4 _EdgeColor;
                float _DissolveAmount;
                float _EdgeWidth;
                float _Brightness;
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
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * 2.0).r;

                float scorch = mainTex.r * noise;
                float dissolveThreshold = _DissolveAmount;

                if (scorch < dissolveThreshold)
                    discard;

                float edge = smoothstep(dissolveThreshold + _EdgeWidth, dissolveThreshold, scorch);
                float3 finalRGB = lerp(_Color.rgb, _EdgeColor.rgb, edge) * _Brightness;
                float alpha = mainTex.a * saturate(1.0 - _DissolveAmount);

                return float4(finalRGB, alpha);
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
            float4 _EdgeColor;
            float _DissolveAmount;
            float _EdgeWidth;
            float _Brightness;
            sampler2D _MainTex;
            sampler2D _NoiseTex;

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
                float4 mainTex = tex2D(_MainTex, i.uv);
                float noise = tex2D(_NoiseTex, i.uv * 2.0).r;
                float scorch = mainTex.r * noise;

                if (scorch < _DissolveAmount)
                    discard;

                float edge = smoothstep(_DissolveAmount + _EdgeWidth, _DissolveAmount, scorch);
                float3 finalRGB = lerp(_Color.rgb, _EdgeColor.rgb, edge) * _Brightness;
                return fixed4(finalRGB, mainTex.a * saturate(1.0 - _DissolveAmount));
            }
            ENDCG
        }
    }
}
