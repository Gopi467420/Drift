
Shader "Custom/ComicLighting"
{
    Properties
    {
        [Header(Base)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _DetailSuppression ("Detail Suppression", Range(0, 1)) = 0.8

        [Header(Graffiti)]
        [Toggle] _EnableGraffiti ("Enable Graffiti", Float) = 1
        _GraffitiOpacity ("Graffiti Opacity", Range(0, 1)) = 0.35
        _GraffitiScale ("Graffiti Patch Size", Range(1, 10)) = 2
        _GraffitiRoughness ("Paint Edge Roughness", Range(0, 1)) = 0.65
        _GraffitiDensity ("Graffiti Density", Range(0, 1)) = 0.5
        _GraffitiDarkness ("Graffiti Darkness", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ComicForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _DetailSuppression;

                float _EnableGraffiti;
                float _GraffitiOpacity;
                float _GraffitiScale;
                float _GraffitiRoughness;
                float _GraffitiDensity;
                float _GraffitiDarkness;

            CBUFFER_END


            // =================================================
            // STRUCTURES
            // =================================================

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };


            // =================================================
            // HASH
            // =================================================

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);

                return frac(p.x * p.y);
            }


            // =================================================
            // VERTEX
            // =================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                output.shadowCoord =
                    GetShadowCoord(positionInputs);

                return output;
            }


            // =================================================
            // BASE TEXTURE WITH DETAIL SUPPRESSION
            // =================================================

            half3 GetBaseTexture(float2 uv)
            {
                uint width;
                uint height;
                uint mipCount;

                _BaseMap.GetDimensions(
                    0,
                    width,
                    height,
                    mipCount
                );

                float maxMipLevel = max(
                    (float)mipCount - 1.0,
                    0.0
                );

                float mipLevel =
                    _DetailSuppression * maxMipLevel;

                half3 textureColor = SAMPLE_TEXTURE2D_LOD(
                    _BaseMap,
                    sampler_BaseMap,
                    uv,
                    mipLevel
                ).rgb;

                return textureColor * _BaseColor.rgb;
            }


            // =================================================
            // GRAFFITI
            // =================================================

            half3 ApplyGraffiti(float2 uv, half3 color)
            {
                if (_EnableGraffiti < 0.5)
                    return color;

                float2 gridUV =
                    uv * _GraffitiScale * 5.0;

                float2 cell = floor(gridUV);
                float2 localUV = frac(gridUV);

                float randomValue = Hash21(cell);

                if (randomValue > _GraffitiDensity)
                    return color;

                float roughness = Hash21(cell + 12.7);

                float edgeVariation =
                    _GraffitiRoughness * 0.25;

                float2 patchSize = lerp(
                    float2(0.25, 0.25),
                    float2(0.85, 0.85),
                    roughness
                );

                float2 center = 0.5 + float2(
                    Hash21(cell + 3.1) - 0.5,
                    Hash21(cell + 8.2) - 0.5
                ) * edgeVariation;

                float2 distanceFromCenter =
                    abs(localUV - center);

                float patch =
                    step(
                        distanceFromCenter.x,
                        patchSize.x * 0.5
                    ) *
                    step(
                        distanceFromCenter.y,
                        patchSize.y * 0.5
                    );

                half3 graffitiColor =
                    color * (1.0 - _GraffitiDarkness);

                return lerp(
                    color,
                    graffitiColor,
                    patch * _GraffitiOpacity
                );
            }


            // =================================================
            // FRAGMENT
            // =================================================

            half4 Frag(Varyings input) : SV_Target
            {
                // 1. Base texture
                half3 color = GetBaseTexture(input.uv);

                // 2. Original lighting calculation
                float3 normalWS = normalize(input.normalWS);

                Light mainLight =
                    GetMainLight(input.shadowCoord);

                float NdotL = saturate(
                    dot(normalWS, mainLight.direction)
                );

                float attenuation =
                    mainLight.distanceAttenuation *
                    mainLight.shadowAttenuation;

                half3 ambientColor = color * 0.2;

                half3 diffuseColor =
                    color *
                    mainLight.color *
                    NdotL *
                    attenuation;

                half3 finalColor =
                    ambientColor + diffuseColor;

                // 3. Graffiti is applied after lighting.
                // It does not modify the lighting or shadow calculations.
                half3 graffitiColor =
                    ApplyGraffiti(input.uv, color);

                half3 graffitiDifference =
                    graffitiColor - color;

                // Only show graffiti where the surface receives light.
                float lightingFactor =
                    saturate(NdotL * attenuation);

                finalColor +=
                    graffitiDifference * lightingFactor;

                return half4(finalColor, _BaseColor.a);
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
