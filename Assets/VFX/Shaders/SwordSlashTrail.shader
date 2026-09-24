Shader "VFX/SwordSlashTrail"
{
    Properties
    {
        [HDR] _Color ("Energy Body Color", Color) = (10.0, 6.0, 1.0, 1.0)
        [HDR] _EdgeColor ("Cutting Rim Color", Color) = (15.0, 10.0, 3.0, 1.0)
        [HDR] _CoreColor ("White-Hot Core", Color) = (18.0, 16.0, 10.0, 1.0)
        _Brightness ("Brightness Multiplier", Float) = 2.8
        _NoiseStrength ("Distortion Strength", Float) = 0.12
        _NoiseSpeed ("Distortion Speed", Float) = 3.0
        _SharpEdgePower ("Cutting Edge Sharpness", Float) = 3.5
        _TrailingFadePower ("Trailing Fade Power", Float) = 1.8
        _MainTex ("Gradient Texture (Optional)", 2D) = "white" {}
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
                float4 _EdgeColor;
                float4 _CoreColor;
                float _Brightness;
                float _NoiseStrength;
                float _NoiseSpeed;
                float _SharpEdgePower;
                float _TrailingFadePower;
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
                // input.uv.x: 0 (old tail) to 1 (new blade head)
                // input.uv.y: 0 (hilt) to 1 (tip)
                // input.color.a: vertex lifetime fade

                // Sample noise distortion
                float2 noiseUV = float2(input.uv.x * 2.0 - _Time.y * _NoiseSpeed * 0.15, input.uv.y);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                float distortedU = saturate(input.uv.x + (noise - 0.5) * _NoiseStrength);

                // Sample optional main gradient
                float4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Trailing fade along length
                float lengthFade = pow(distortedU, _TrailingFadePower) * input.color.a;

                // Cutting tip sharpness across width (V = 1 is tip, V = 0 is hilt)
                float edgeGlow = pow(input.uv.y, _SharpEdgePower);
                float coreGlow = pow(input.uv.y, _SharpEdgePower * 2.2);

                // Blend colors
                float3 body = _Color.rgb * (1.0 - edgeGlow * 0.5);
                float3 rim = _EdgeColor.rgb * edgeGlow * 1.5;
                float3 core = _CoreColor.rgb * coreGlow * 2.5;

                float3 finalRGB = (body + rim + core) * texCol.rgb * _Brightness * lengthFade;
                float finalAlpha = saturate(lengthFade * (edgeGlow + 0.3));

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
            float4 _EdgeColor;
            float4 _CoreColor;
            float _Brightness;
            float _NoiseStrength;
            float _NoiseSpeed;
            float _SharpEdgePower;
            float _TrailingFadePower;
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
                float2 noiseUV = float2(i.uv.x * 2.0 - _Time.y * _NoiseSpeed * 0.15, i.uv.y);
                float noise = tex2D(_NoiseTex, noiseUV).r;
                float distortedU = saturate(i.uv.x + (noise - 0.5) * _NoiseStrength);

                float lengthFade = pow(distortedU, _TrailingFadePower) * i.color.a;
                float edgeGlow = pow(i.uv.y, _SharpEdgePower);
                float coreGlow = pow(i.uv.y, _SharpEdgePower * 2.2);

                float3 body = _Color.rgb * (1.0 - edgeGlow * 0.5);
                float3 rim = _EdgeColor.rgb * edgeGlow * 1.5;
                float3 core = _CoreColor.rgb * coreGlow * 2.5;

                float3 finalRGB = (body + rim + core) * _Brightness * lengthFade;
                return fixed4(finalRGB, saturate(lengthFade));
            }
            ENDCG
        }
    }
}
