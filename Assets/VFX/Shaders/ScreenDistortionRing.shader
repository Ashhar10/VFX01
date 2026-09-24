Shader "VFX/ScreenDistortionRing"
{
    Properties
    {
        _DistortionStrength ("Distortion Strength", Float) = 0.06
        _ChromaticAberration ("Chromatic Aberration", Float) = 0.015
        [HDR] _GlowColor ("Rim Glow Color", Color) = (15.0, 10.0, 3.0, 1.0)
        _GlowIntensity ("Glow Intensity", Float) = 1.5
        _RingWidth ("Ring Width", Float) = 0.08
        _Radius ("Ring Radius (0-1)", Float) = 0.5
        _Dissolve ("Dissolve (0-1)", Float) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+100" 
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
                float _DistortionStrength;
                float _ChromaticAberration;
                float4 _GlowColor;
                float _GlowIntensity;
                float _RingWidth;
                float _Radius;
                float _Dissolve;
            CBUFFER_END

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
                float ringDist = abs(dist - _Radius);
                float ringMask = smoothstep(_RingWidth, 0.0, ringDist);

                float fade = saturate(1.0 - _Dissolve);
                float3 finalRGB = _GlowColor.rgb * ringMask * _GlowIntensity * fade;
                float finalAlpha = ringMask * fade;

                return float4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }

    // Built-in Render Pipeline Fallback
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
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

            float _DistortionStrength;
            float _ChromaticAberration;
            float4 _GlowColor;
            float _GlowIntensity;
            float _RingWidth;
            float _Radius;
            float _Dissolve;

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
                float ringDist = abs(dist - _Radius);
                float ringMask = smoothstep(_RingWidth, 0.0, ringDist);
                float fade = saturate(1.0 - _Dissolve);

                return fixed4(_GlowColor.rgb * ringMask * _GlowIntensity * fade, ringMask * fade);
            }
            ENDCG
        }
    }
}
