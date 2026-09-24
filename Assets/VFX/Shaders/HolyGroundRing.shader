Shader "VFX/HolyGroundRing"
{
    Properties
    {
        [HDR] _Color ("Ring Color", Color) = (14.0, 8.5, 2.0, 1.0)
        [HDR] _CoreColor ("Core Bright Color", Color) = (20.0, 18.0, 12.0, 1.0)
        _Radius ("Outer Ring Radius", Float) = 0.85
        _Thickness ("Border Thickness", Float) = 0.05
        _InnerRingRadius ("Inner Ring Radius", Float) = 0.4675
        _RaysCount ("Sunburst Rays Count", Float) = 32.0
        _NeedleSharpness ("Needle Sharpness", Float) = 6.0
        _NeedleLength ("Needle Length", Float) = 0.35
        _CenterDampening ("Center Dampening", Float) = 0.75
        _Brightness ("Brightness Multiplier", Float) = 3.2
        _ActivationProgress ("Activation Progress (0-1)", Float) = 1.0
        _Dissolve ("Dissolve Amount (0-1)", Float) = 0.0
        _MainTex ("Optional Texture", 2D) = "white" {}
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
                float _Radius;
                float _Thickness;
                float _InnerRingRadius;
                float _RaysCount;
                float _NeedleSharpness;
                float _NeedleLength;
                float _CenterDampening;
                float _Brightness;
                float _ActivationProgress;
                float _Dissolve;
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
                float2 centeredUV = (input.uv - 0.5) * 2.0;
                float dist = length(centeredUV);
                float angle = atan2(centeredUV.y, centeredUV.x);

                // 1. Activation radius limit
                float curMaxRadius = _Radius * saturate(_ActivationProgress * 4.0);
                if (dist > curMaxRadius + _NeedleLength && _ActivationProgress < 0.99)
                    discard;

                // 2. Level 1: Outer circle border
                float outerRingDist = abs(dist - _Radius);
                float outerRing = smoothstep(_Thickness, 0.0, outerRingDist);

                // 3. Level 2: Sacred Inner Arena Glow
                float innerProgress = saturate((_ActivationProgress - 0.25) / 0.20);
                float innerFloor = smoothstep(_Radius, _InnerRingRadius * 0.4, dist) * innerProgress * 0.45;
                // Dampen center under feet to protect character silhouette
                innerFloor *= lerp(1.0 - _CenterDampening, 1.0, smoothstep(0.0, _InnerRingRadius, dist));

                // 4. Level 3: Inner Concentric Ring & Geometric Sacred Design
                float designProgress = saturate((_ActivationProgress - 0.45) / 0.25);
                float innerRing = smoothstep(_Thickness * 0.8, 0.0, abs(dist - _InnerRingRadius)) * designProgress;

                // Intricate sacred geometric runes/lines between inner and outer ring
                float betweenRings = step(_InnerRingRadius - 0.02, dist) * step(dist, _Radius + 0.02);
                float radialFringe = abs(sin(angle * 8.0)) * 0.5 + abs(sin(angle * 16.0)) * 0.25;
                float concentricLines = sin(dist * 50.0) * 0.5 + 0.5;
                float runes = betweenRings * (radialFringe * concentricLines) * designProgress * 0.6;

                // 5. Level 4: Radiating Sunburst Needle Rays
                float rayProgress = saturate((_ActivationProgress - 0.70) / 0.30);
                float rayAngle = abs(sin(angle * (_RaysCount * 0.5)));
                float needleProfile = pow(rayAngle, _NeedleSharpness);
                float rayDist = dist - _Radius;
                float rayLengthMask = smoothstep(_NeedleLength, 0.0, rayDist) * step(0.0, rayDist);
                float sunburstRays = needleProfile * rayLengthMask * rayProgress;

                // Combine elements
                float totalShape = outerRing + innerFloor + innerRing + runes + sunburstRays;
                float coreHotspot = outerRing * 1.5 + innerRing * 0.8 + sunburstRays * 2.0;

                // Blend base color and white-hot core
                float3 finalRGB = _Color.rgb * totalShape + _CoreColor.rgb * coreHotspot;
                finalRGB *= _Brightness;

                // Dissolve / fade
                float fade = saturate(1.0 - _Dissolve);
                finalRGB *= fade;
                float finalAlpha = saturate(totalShape) * fade;

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
            float _Radius;
            float _Thickness;
            float _InnerRingRadius;
            float _RaysCount;
            float _NeedleSharpness;
            float _NeedleLength;
            float _CenterDampening;
            float _Brightness;
            float _ActivationProgress;
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
                float2 centeredUV = (i.uv - 0.5) * 2.0;
                float dist = length(centeredUV);
                float angle = atan2(centeredUV.y, centeredUV.x);

                float outerRing = smoothstep(_Thickness, 0.0, abs(dist - _Radius));
                float innerProgress = saturate((_ActivationProgress - 0.25) / 0.20);
                float innerFloor = smoothstep(_Radius, _InnerRingRadius * 0.4, dist) * innerProgress * 0.45;
                innerFloor *= lerp(1.0 - _CenterDampening, 1.0, smoothstep(0.0, _InnerRingRadius, dist));

                float designProgress = saturate((_ActivationProgress - 0.45) / 0.25);
                float innerRing = smoothstep(_Thickness * 0.8, 0.0, abs(dist - _InnerRingRadius)) * designProgress;

                float rayProgress = saturate((_ActivationProgress - 0.70) / 0.30);
                float rayAngle = abs(sin(angle * (_RaysCount * 0.5)));
                float needleProfile = pow(rayAngle, _NeedleSharpness);
                float rayDist = dist - _Radius;
                float rayLengthMask = smoothstep(_NeedleLength, 0.0, rayDist) * step(0.0, rayDist);
                float sunburstRays = needleProfile * rayLengthMask * rayProgress;

                float totalShape = outerRing + innerFloor + innerRing + sunburstRays;
                float coreHotspot = outerRing * 1.5 + sunburstRays * 2.0;

                float3 finalRGB = (_Color.rgb * totalShape + _CoreColor.rgb * coreHotspot) * _Brightness * saturate(1.0 - _Dissolve);
                return fixed4(finalRGB, saturate(totalShape) * saturate(1.0 - _Dissolve));
            }
            ENDCG
        }
    }
}
