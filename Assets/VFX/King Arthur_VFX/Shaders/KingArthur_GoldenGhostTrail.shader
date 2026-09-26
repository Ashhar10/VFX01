Shader "VFX/King Arthur/Golden Ghost Trail"
{
    Properties
    {
        [Header(Blending and Render State)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 5.0 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 1.0 // One (Additive) or 10 (OneMinusSrcAlpha)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0.0 // Off (Double-Sided for dynamic motion/cape)

        [Header(Golden Glow and Holy Aura)]
        [HDR] _BaseColor("Ghost Golden Color (HDR)", Color) = (1.0, 0.85, 0.38, 0.85)
        [HDR] _EmissionColor("Silhouette Rim Emission (HDR)", Color) = (3.2, 2.5, 0.85, 1.0)
        _BloomMultiplier("Bloom Intensity Multiplier", Range(1.0, 10.0)) = 3.5

        [Header(Fresnel Silhouette Rim)]
        _FresnelPower("Fresnel Rim Exponent", Range(0.5, 8.0)) = 2.2
        _FresnelIntensity("Fresnel Rim Intensity", Range(0.0, 5.0)) = 2.4
        _CoreOpacity("Core Body Base Opacity", Range(0.0, 1.0)) = 0.35
        _RimOpacity("Rim Silhouette Max Opacity", Range(0.0, 1.0)) = 0.95

        [Header(Dynamic Lifetime Control)]
        _Opacity("Master Opacity (Fade)", Range(0.0, 1.0)) = 1.0
        _DissolveProgress("Dissolve Progress", Range(0.0, 1.0)) = 0.0

        [Header(Dissolve and Sacred Erosion)]
        [HDR] _DissolveBurnColor("Dissolve Burn Edge (HDR)", Color) = (4.0, 3.0, 1.2, 1.0)
        _DissolveEdgeWidth("Dissolve Edge Width", Range(0.01, 0.3)) = 0.08
        _NoiseScale("Dissolve Noise Scale", Float) = 5.5
        _VerticalBias("Dissolve Vertical Bias (Ground Up)", Range(-1.0, 2.0)) = 0.4

        [Header(Holographic Celestial Scanlines)]
        _ScanlineIntensity("Scanline Ripple Intensity", Range(0.0, 1.0)) = 0.20
        _ScanlineFrequency("Scanline Ripple Frequency", Float) = 35.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+25"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GhostTrailForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float2 uv         : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Procedural 3D Hash & Value Noise for seamless, textureless organic dissolve
            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Hermite cubic smoothstep

                float n000 = hash31(i + float3(0.0, 0.0, 0.0));
                float n100 = hash31(i + float3(1.0, 0.0, 0.0));
                float n010 = hash31(i + float3(0.0, 1.0, 0.0));
                float n110 = hash31(i + float3(1.0, 1.0, 0.0));
                float n001 = hash31(i + float3(0.0, 0.0, 1.0));
                float n101 = hash31(i + float3(1.0, 0.0, 1.0));
                float n011 = hash31(i + float3(0.0, 1.0, 1.0));
                float n111 = hash31(i + float3(1.0, 1.0, 1.0));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z);
            }

            float fbm3D(float3 p)
            {
                float val = 0.0;
                val += 0.500 * noise3D(p);
                val += 0.250 * noise3D(p * 2.02);
                val += 0.125 * noise3D(p * 4.05);
                return val;
            }

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _DissolveBurnColor)
                UNITY_DEFINE_INSTANCED_PROP(float,  _BloomMultiplier)
                UNITY_DEFINE_INSTANCED_PROP(float,  _FresnelPower)
                UNITY_DEFINE_INSTANCED_PROP(float,  _FresnelIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float,  _CoreOpacity)
                UNITY_DEFINE_INSTANCED_PROP(float,  _RimOpacity)
                UNITY_DEFINE_INSTANCED_PROP(float,  _Opacity)
                UNITY_DEFINE_INSTANCED_PROP(float,  _DissolveProgress)
                UNITY_DEFINE_INSTANCED_PROP(float,  _DissolveEdgeWidth)
                UNITY_DEFINE_INSTANCED_PROP(float,  _NoiseScale)
                UNITY_DEFINE_INSTANCED_PROP(float,  _VerticalBias)
                UNITY_DEFINE_INSTANCED_PROP(float,  _ScanlineIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float,  _ScanlineFrequency)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

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
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input, float facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float opacity = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Opacity);
                float dissolve = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DissolveProgress);

                // Early exit if completely invisible
                if (opacity <= 0.001 || dissolve >= 0.999)
                {
                    discard;
                }

                // 1. Procedural 3D Simplex FBM Noise across the character's geometry
                float noiseScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NoiseScale);
                float noise = fbm3D(input.positionOS * noiseScale);

                // Vertical gradient bias (higher parts linger, lower parts dissolve first into motes)
                float vertBias = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _VerticalBias);
                float vertGrad = saturate(input.positionOS.y * 0.5 + 0.3);
                float dissolveMap = lerp(noise, noise * 0.65 + vertGrad * 0.35, saturate(vertBias));

                // Dissolve clipping (only active when dissolve > 0.005)
                float dissolveEdge = dissolveMap - dissolve;
                if (dissolve > 0.005 && dissolveEdge <= 0.0)
                {
                    discard;
                }

                // Searing golden burn edge along the dissolving boundary
                float edgeWidth = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DissolveEdgeWidth);
                float edgeFactor = (dissolve > 0.005) ? (1.0 - saturate(dissolveEdge / max(edgeWidth, 0.001))) : 0.0;
                float4 burnCol = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DissolveBurnColor);
                float3 burnGlow = burnCol.rgb * (edgeFactor * edgeFactor * 4.5);

                // 2. Safe Normal & View Direction for Fresnel
                float lenSq = dot(input.normalWS, input.normalWS);
                float3 worldNormal = (lenSq > 0.0001) ? normalize(input.normalWS) : float3(0.0, 1.0, 0.0);
                if (facing < 0.0) worldNormal = -worldNormal;

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                float NdotV = saturate(dot(worldNormal, viewDir));

                // 3. Fresnel Silhouette Rim
                float fresnelPower = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _FresnelPower);
                float fresnelIntensity = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _FresnelIntensity);
                float fresnel = pow(1.0 - NdotV, fresnelPower) * fresnelIntensity;

                // 4. Subtle Celestial Holographic Scanlines
                float scanIntensity = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ScanlineIntensity);
                float scanFreq = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ScanlineFrequency);
                float scanline = 1.0 - scanIntensity * (0.5 + 0.5 * sin(input.positionWS.y * scanFreq + _Time.y * 4.0));

                // 5. Shading Assembly
                float4 baseCol = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
                float4 emissionCol = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor);
                float bloomMult = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BloomMultiplier);
                float coreOpacity = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CoreOpacity);
                float rimOpacity = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimOpacity);

                float3 coreGlow = baseCol.rgb;
                float3 rimGlow = emissionCol.rgb * bloomMult * fresnel;

                float3 finalColor = (coreGlow + rimGlow) * scanline + burnGlow;
                finalColor *= opacity;

                float finalAlpha = saturate(lerp(coreOpacity, rimOpacity, saturate(fresnel)) * opacity);

                // Dim backfaces slightly for holographic depth
                if (facing < 0.0)
                {
                    finalColor *= 0.65;
                    finalAlpha *= 0.65;
                }

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Particles/Unlit"
}
