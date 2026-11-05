
Shader "Hidden/ComicHalftone"
{
    Properties
    {
        _DotScale ("Dot Scale", Range(1, 100)) = 35
        _EffectStrength ("Effect Strength", Range(0, 1)) = 0.65
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.65
        _ShadowSoftness ("Shadow Softness", Range(0.01, 1)) = 0.3
        _Contrast ("Contrast", Range(0.5, 2)) = 1
        _DotDarkness ("Dot Darkness", Range(0, 1)) = 0.8
        _DotColor ("Dot Color", Color) = (0.05, 0.03, 0.08, 1)
        _PaperColor ("Paper Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "ComicHalftone"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_HazardMask);

            CBUFFER_START(UnityPerMaterial)
                float _DotScale;
                float _EffectStrength;
                float _ShadowThreshold;
                float _ShadowSoftness;
                float _Contrast;
                float _DotDarkness;
                float4 _DotColor;
                float4 _PaperColor;
            CBUFFER_END

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float3 sceneColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                ).rgb;

                float luminance = dot(
                    sceneColor,
                    float3(0.299, 0.587, 0.114)
                );

                float adjustedLuminance = saturate(
                    (luminance - 0.5) * _Contrast + 0.5
                );

                float2 pixelPosition = uv * _ScreenParams.xy / _DotScale;
                float2 gridPosition = frac(pixelPosition) - 0.5;
                float distanceFromCenter = length(gridPosition);

                float shadowAmount = 1.0 - adjustedLuminance;

                float dotRadius = lerp(0.08, 0.48, shadowAmount);

                float dots = 1.0 - smoothstep(
                    dotRadius - 0.035,
                    dotRadius,
                    distanceFromCenter
                );

                float shadowMask = 1.0 - smoothstep(
                    _ShadowThreshold - _ShadowSoftness,
                    _ShadowThreshold,
                    shadowAmount
                );

                float3 comicColor = lerp(
                    _PaperColor.rgb,
                    _DotColor.rgb,
                    dots * _DotDarkness * shadowMask
                );

                float4 mask = SAMPLE_TEXTURE2D_X(
                    _HazardMask,
                    sampler_LinearClamp,
                    uv
                );

                float hazardMask = mask.r;

                float3 finalColor = lerp(
                    sceneColor,
                    sceneColor * comicColor,
                    hazardMask * _EffectStrength
                );

                return float4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
