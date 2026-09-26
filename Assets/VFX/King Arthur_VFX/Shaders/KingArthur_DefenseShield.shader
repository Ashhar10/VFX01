Shader "VFX/King Arthur/Defense Shield"
{
    Properties
    {
        [Header(Base Colors and Theme)]
        [MainColor] _BaseColor("Shield Core Tint", Color) = (1.0, 0.82, 0.35, 0.12)
        [HDR] _RimColor("Rim & Grid Color (HDR)", Color) = (2.2, 1.7, 0.65, 1.0)
        _BloomMultiplier("Bloom Intensity Multiplier", Range(1.0, 10.0)) = 3.0

        [Header(Fresnel Silhouette Rim)]
        _FresnelPower("Fresnel Exponent (Sharpness)", Range(0.5, 8.0)) = 2.8
        _FresnelIntensity("Fresnel Rim Glow Intensity", Range(0.0, 5.0)) = 2.2
        _CenterAlpha("Center Transparency", Range(0.0, 0.5)) = 0.06
        _RimAlpha("Rim Silhouette Alpha", Range(0.2, 1.0)) = 0.85

        [Header(Hexagon Pentagon Grid Texture)]
        [MainTexture] _BaseMap("Hexagon/Pentagon Grid Texture", 2D) = "white" {}
        _Tiling("Grid Tiling Scale", Float) = 2.2
        _GridIntensity("Grid Line Brightness", Range(0.0, 5.0)) = 2.5
        _GridBloom("Grid Bloom Boost", Range(0.5, 5.0)) = 2.0
        _ScrollSpeed("Grid Pulse / Drift Speed", Vector) = (0.0, 0.02, 0.0, 0.0)
        [Toggle] _UseTriplanar("Use Seamless Triplanar Projection", Float) = 1.0
        _TriplanarSharpness("Triplanar Blend Sharpness", Range(1.0, 16.0)) = 4.0

        [Header(Soft Ground Intersection)]
        _DepthFadeDistance("Ground Soft Intersection Distance", Range(0.01, 1.0)) = 0.25

        [Header(Two Sided Hologram Depth)]
        _BackfaceMultiplier("Backface Interior Dimmer", Range(0.1, 1.0)) = 0.40
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 normalOS   : TEXCOORD3;
                float2 uv         : TEXCOORD4;
                float4 screenPos  : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float4 _ScrollSpeed;
                float _BloomMultiplier;
                float _FresnelPower;
                float _FresnelIntensity;
                float _CenterAlpha;
                float _RimAlpha;
                float _Tiling;
                float _GridIntensity;
                float _GridBloom;
                float _UseTriplanar;
                float _TriplanarSharpness;
                float _DepthFadeDistance;
                float _BackfaceMultiplier;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normalInput.normalWS;
                output.normalOS = input.normalOS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);

                return output;
            }

            half4 frag(Varyings input, float facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 worldNormal = normalize(input.normalWS);
                // Correct normal direction if backface
                if (facing < 0.0) worldNormal = -worldNormal;

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                float NdotV = saturate(dot(worldNormal, viewDir));

                // 1. Fresnel Rim Silhouette
                float fresnel = pow(1.0 - NdotV, _FresnelPower) * _FresnelIntensity;

                // 2. Honeycomb / Pentagon Grid Sampling
                float4 gridTex = float4(0, 0, 0, 0);
                if (_UseTriplanar > 0.5)
                {
                    float3 coord = input.positionOS * _Tiling;
                    float2 timeOffset = _Time.y * _ScrollSpeed.xy;

                    float3 blendWeights = pow(abs(normalize(input.normalOS)), _TriplanarSharpness);
                    blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z + 0.00001);

                    float4 colX = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, coord.yz + timeOffset);
                    float4 colY = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, coord.xz + timeOffset);
                    float4 colZ = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, coord.xy + timeOffset);

                    gridTex = colX * blendWeights.x + colY * blendWeights.y + colZ * blendWeights.z;
                }
                else
                {
                    float2 uv = input.uv * _Tiling + _Time.y * _ScrollSpeed.xy;
                    gridTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                }

                // 3. Grid line intensity & inner facet transparency
                float lineFactor = gridTex.a; // alpha channel carries line strength
                float innerGlow = gridTex.r * 0.15;

                // 4. Color & Emission Calculation
                float3 baseTint = _BaseColor.rgb;
                float3 rimGlow = _RimColor.rgb * _BloomMultiplier;

                // Combine rim and grid lines
                float3 finalColor = baseTint * _CenterAlpha + rimGlow * (fresnel + lineFactor * _GridIntensity * _GridBloom + innerGlow);

                // Backface dimming for natural holographic depth
                float faceDim = (facing > 0.0) ? 1.0 : _BackfaceMultiplier;
                finalColor *= faceDim;

                // 5. Alpha blending
                float finalAlpha = lerp(_CenterAlpha, _RimAlpha, saturate(fresnel + lineFactor * 0.85));
                finalAlpha *= faceDim;

                // 6. Soft ground depth intersection
                #if defined(_CAMERA_DEPTH_ATTACHED) || defined(REQUIRE_DEPTH_TEXTURE)
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneLinearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceLinearDepth = input.screenPos.w;
                float depthDifference = sceneLinearDepth - surfaceLinearDepth;
                float depthFade = saturate(depthDifference / max(_DepthFadeDistance, 0.001));
                finalAlpha *= depthFade;
                #endif

                // Breathing pulse
                float pulse = 1.0 + 0.08 * sin(_Time.y * 3.14159);
                finalColor *= pulse;

                return half4(finalColor, saturate(finalAlpha));
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Particles/Unlit"
}
