
Shader "Hidden/ShadowHatching"
{
    Properties
    {
        [Header(Hatching)]
        _HatchColor ("Hatch Color", Color) = (0.02, 0.02, 0.025, 1)
        _HatchStrength ("Hatch Strength", Range(0, 1)) = 0.8
        _HatchSpacing ("Line Spacing (Pixels)", Range(2, 40)) = 12
        _HatchWidth ("Line Width", Range(0.01, 0.5)) = 0.12
        _HatchAngle ("Hatch Angle", Range(0, 180)) = 45

        [Header(Shadow Detection)]
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.25
        _ShadowSoftness ("Shadow Transition", Range(0.01, 1)) = 0.2

        [Header(Cross Hatching)]
        [Toggle] _EnableCrossHatching ("Enable Cross Hatching", Float) = 1
        _CrossHatchThreshold ("Cross Hatch Threshold", Range(0, 1)) = 0.65
        _CrossHatchStrength ("Cross Hatch Strength", Range(0, 1)) = 0.7

        [Header(Shadow Control)]
        _ShadowContribution ("Shadow Contribution", Range(0, 1)) = 1
        _UnlitContribution ("Unlit Surface Contribution", Range(0, 1)) = 0
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
            Name "ShadowHatching"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)

                half4 _HatchColor;

                float _HatchStrength;
                float _HatchSpacing;
                float _HatchWidth;
                float _HatchAngle;

                float _ShadowThreshold;
                float _ShadowSoftness;

                float _EnableCrossHatching;
                float _CrossHatchThreshold;
                float _CrossHatchStrength;

                float _ShadowContribution;
                float _UnlitContribution;

            CBUFFER_END


            // Generate a screen-space hatch pattern.
            float GetHatchLine(float2 pixelPosition, float angle)
            {
                float radians = angle * 0.0174532925;

                float2 direction = float2(
                    cos(radians),
                    sin(radians)
                );

                float projectedPosition = dot(
                    pixelPosition,
                    direction
                );

                float linePosition = frac(
                    projectedPosition / max(_HatchSpacing, 0.001)
                );

                float distanceToLine = abs(linePosition - 0.5);

                // Renamed from "line" to avoid HLSL parsing errors.
                float hatchValue = 1.0 - smoothstep(
                    _HatchWidth,
                    _HatchWidth + 0.04,
                    distanceToLine
                );

                return saturate(hatchValue);
            }


            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // Original camera color.
                half4 sourceColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv
                );

                // Scene depth.
                float depth = SampleSceneDepth(uv);

                // Reconstruct world position.
                float3 positionWS = ComputeWorldSpacePosition(
                    uv,
                    depth,
                    UNITY_MATRIX_I_VP
                );

                // Rebuild the normal from depth so no normals prepass is needed.
                // Must run before any early return (screen derivatives).
                float3 normalWS = normalize(
                    cross(ddy(positionWS), ddx(positionWS))
                );

                // Derivative sign depends on platform; face it toward the camera.
                if (dot(normalWS, GetCameraPositionWS() - positionWS) < 0.0)
                    normalWS = -normalWS;

                // Skip background pixels.
                #if UNITY_REVERSED_Z
                    if (depth <= 0.0001)
                        return sourceColor;
                #else
                    if (depth >= 0.9999)
                        return sourceColor;
                #endif

                // Main light shadow coordinate.
                float4 shadowCoord = TransformWorldToShadowCoord(
                    positionWS
                );

                Light mainLight = GetMainLight(shadowCoord);

                // Surface orientation relative to main light.
                float NdotL = saturate(
                    dot(normalWS, mainLight.direction)
                );

                // Cast shadow contribution.
                float castShadow = 1.0 - mainLight.shadowAttenuation;

                // Surface facing away from the light.
                float unlitSurface = 1.0 - NdotL;

                // Combine shadow contributions.
                float shadowAmount = saturate(
                    castShadow * _ShadowContribution +
                    unlitSurface * _UnlitContribution
                );

                // Smooth shadow transition.
                float shadowMask = smoothstep(
                    _ShadowThreshold,
                    _ShadowThreshold + _ShadowSoftness,
                    shadowAmount
                );

                if (shadowMask <= 0.001)
                    return sourceColor;

                // Screen-space pixel coordinates.
                float2 pixelPosition = uv * _ScreenParams.xy;

                // Primary hatch direction.
                float hatchA = GetHatchLine(
                    pixelPosition,
                    _HatchAngle
                );

                float hatchPattern = hatchA;

                // Secondary hatch direction.
                if (_EnableCrossHatching > 0.5)
                {
                    float crossMask = smoothstep(
                        _CrossHatchThreshold,
                        min(_CrossHatchThreshold + 0.2, 1.0),
                        shadowAmount
                    );

                    float hatchB = GetHatchLine(
                        pixelPosition,
                        _HatchAngle + 90.0
                    );

                    hatchPattern = max(
                        hatchA,
                        hatchB * crossMask * _CrossHatchStrength
                    );
                }

                // Final hatch opacity.
                float hatchOpacity =
                    hatchPattern *
                    shadowMask *
                    _HatchStrength *
                    _HatchColor.a;

                // Blend hatching over camera image.
                half3 finalColor = lerp(
                    sourceColor.rgb,
                    _HatchColor.rgb,
                    saturate(hatchOpacity)
                );

                return half4(finalColor, sourceColor.a);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
