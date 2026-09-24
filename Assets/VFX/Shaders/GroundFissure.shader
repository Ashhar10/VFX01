Shader "VFX/GroundFissure"
{
    Properties
    {
        [HDR] _MoltenColor ("White-Hot Lava Core", Color) = (18.0, 9.0, 1.5, 1.0)
        [HDR] _CrustColor ("Glowing Magma Crust", Color) = (10.0, 3.5, 0.6, 1.0)
        _RockColor ("Charred Rock Edge", Color) = (0.12, 0.09, 0.07, 1.0)
        _CrackIntensity ("Crack Intensity", Float) = 5.0
        _CoreRadius ("Molten Core Radius", Float) = 0.35
        _Brightness ("Brightness Multiplier", Float) = 3.0
        _Dissolve ("Cool / Dissolve (0-1)", Float) = 0.0
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
                float4 _MoltenColor;
                float4 _CrustColor;
                float4 _RockColor;
                float _CrackIntensity;
                float _CoreRadius;
                float _Brightness;
                float _Dissolve;
            CBUFFER_END

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
                float2 centeredUV = (input.uv - 0.5) * 2.0;
                float dist = length(centeredUV);

                // Sample noise for fractured magma crack edge
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv * 2.0).r;
                float noisyDist = dist + (noise - 0.5) * 0.35;

                // Circular falloff
                float edgeMask = smoothstep(1.0, 0.6, noisyDist);

                // Molten Core, Crust, and Stone zones
                float coreMask = smoothstep(_CoreRadius, 0.0, noisyDist);
                float crustMask = smoothstep(0.75, _CoreRadius * 0.8, noisyDist) * (1.0 - coreMask);
                float rockMask = edgeMask * (1.0 - coreMask - crustMask);

                // Cooling & Dissolve: Core cools down to crust, then crust cools to rock, then dissolves
                float coolFactor = saturate(1.0 - _Dissolve * 1.5);
                float3 moltenEmission = _MoltenColor.rgb * (coreMask * coolFactor);
                float3 crustEmission = _CrustColor.rgb * (crustMask * saturate(coolFactor * 1.5));
                float3 rockBase = _RockColor.rgb * (rockMask + (1.0 - coolFactor) * (coreMask + crustMask));

                float3 finalRGB = (moltenEmission + crustEmission) * _Brightness + rockBase;
                float alpha = edgeMask * saturate(1.0 - _Dissolve);

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

            float4 _MoltenColor;
            float4 _CrustColor;
            float4 _RockColor;
            float _CrackIntensity;
            float _CoreRadius;
            float _Brightness;
            float _Dissolve;
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
                float2 centeredUV = (i.uv - 0.5) * 2.0;
                float dist = length(centeredUV);
                float noise = tex2D(_NoiseTex, i.uv * 2.0).r;
                float noisyDist = dist + (noise - 0.5) * 0.35;

                float edgeMask = smoothstep(1.0, 0.6, noisyDist);
                float coreMask = smoothstep(_CoreRadius, 0.0, noisyDist);
                float crustMask = smoothstep(0.75, _CoreRadius * 0.8, noisyDist) * (1.0 - coreMask);

                float coolFactor = saturate(1.0 - _Dissolve * 1.5);
                float3 finalRGB = (_MoltenColor.rgb * (coreMask * coolFactor) + _CrustColor.rgb * crustMask) * _Brightness + _RockColor.rgb * edgeMask;
                return fixed4(finalRGB, edgeMask * saturate(1.0 - _Dissolve));
            }
            ENDCG
        }
    }
}
