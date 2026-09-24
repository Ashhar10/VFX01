Shader "VFX/VerticalSunNeedles"
{
    Properties
    {
        [HDR] _Color ("Sunbeam Color", Color) = (16.0, 9.5, 2.0, 1.0)
        [HDR] _CoreColor ("White-Hot Base Core", Color) = (22.0, 20.0, 14.0, 1.0)
        _Brightness ("Brightness Multiplier", Float) = 3.5
        _NeedleSharpness ("Needle Sharpness", Float) = 3.0
        _HeightProgress ("Height Shoot Progress (0-1)", Float) = 1.0
        _Dissolve ("Dissolve Amount (0-1)", Float) = 0.0
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
                float _Brightness;
                float _NeedleSharpness;
                float _HeightProgress;
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
                // uv.y = 0 at needle base, 1 at needle tip
                // uv.x = 0 to 1 across needle width

                // 1. Height shoot-up progress mask
                if (input.uv.y > _HeightProgress)
                    discard;

                float heightFade = smoothstep(_HeightProgress, _HeightProgress - 0.08, input.uv.y);

                // 2. Horizontal taper: wider at base, needle-sharp at top
                float uDist = abs(input.uv.x - 0.5) * 2.0;
                float allowedWidth = lerp(1.0, 0.15, pow(input.uv.y, 0.7));
                float needleProfile = smoothstep(allowedWidth, 0.0, uDist);
                needleProfile = pow(needleProfile, _NeedleSharpness);

                // 3. Vertical Core Glow (white-hot base touching the floor, fading upward)
                float baseHotspot = smoothstep(0.4, 0.0, input.uv.y) * needleProfile;
                float tipFalloff = smoothstep(1.0, 0.7, input.uv.y);

                // 4. Combine colors
                float3 finalRGB = _Color.rgb * needleProfile + _CoreColor.rgb * (baseHotspot * 2.0);
                finalRGB *= _Brightness;

                float fade = saturate(1.0 - _Dissolve) * heightFade * (1.0 - tipFalloff * 0.7);
                finalRGB *= fade;
                float finalAlpha = saturate(needleProfile) * fade;

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
            float _Brightness;
            float _NeedleSharpness;
            float _HeightProgress;
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
                if (i.uv.y > _HeightProgress)
                    discard;

                float heightFade = smoothstep(_HeightProgress, _HeightProgress - 0.08, i.uv.y);
                float uDist = abs(i.uv.x - 0.5) * 2.0;
                float allowedWidth = lerp(1.0, 0.15, pow(i.uv.y, 0.7));
                float needleProfile = pow(smoothstep(allowedWidth, 0.0, uDist), _NeedleSharpness);

                float baseHotspot = smoothstep(0.4, 0.0, i.uv.y) * needleProfile;
                float3 finalRGB = (_Color.rgb * needleProfile + _CoreColor.rgb * baseHotspot * 2.0) * _Brightness;
                float fade = saturate(1.0 - _Dissolve) * heightFade;

                return fixed4(finalRGB * fade, needleProfile * fade);
            }
            ENDCG
        }
    }
}
