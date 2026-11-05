#ifndef COMIC_LIT_FORWARD_INCLUDED
#define COMIC_LIT_FORWARD_INCLUDED

// Hatching, halftone and glitch drawn inside the object's own forward pass.
// No depth texture, normals texture or extra blits are needed.
//
// These are globals (not in UnityPerMaterial) so the material layout
// stays identical to URP Lit and the SRP Batcher keeps working.
// They are set from the ComicStyle asset (ComicStyleSettings.Apply).

// Hatching
half4 _Comic_HatchColor;
float _Comic_HatchStrength;
float _Comic_HatchSpacing;
float _Comic_HatchWidth;
float _Comic_HatchAngle;
float _Comic_HatchShadowThreshold;
float _Comic_HatchShadowSoftness;
float _Comic_EnableCrossHatching;
float _Comic_CrossHatchThreshold;
float _Comic_CrossHatchStrength;
float _Comic_ShadowContribution;
float _Comic_UnlitContribution;

// A test texture laid over every object
TEXTURE2D(_Comic_SurfaceMap);
SAMPLER(sampler_Comic_SurfaceMap);
float _Comic_SurfaceStrength;
float _Comic_SurfaceTiling;

// Neon rim light
half4 _Comic_RimColorA;
half4 _Comic_RimColorB;
float _Comic_RimStrength;
float _Comic_RimPower;

// Halftone
float _Comic_DotScale;
float _Comic_HalftoneStrength;
float _Comic_HalftoneShadowThreshold;
float _Comic_HalftoneShadowSoftness;
float _Comic_Contrast;
float _Comic_DotDarkness;
half4 _Comic_DotColor;
half4 _Comic_PaperColor;

#include "ComicGlitch.hlsl"


// Glitch slices: bands of the mesh snap sideways during a burst.
Varyings ComicLitVertex(Attributes input)
{
    #if defined(_COMIC_GLITCH)
        UNITY_SETUP_INSTANCE_ID(input);

        float phase = ComicSpawnPhase();
        float burst = max(ComicGlitchBurst(), phase == 1.0 ? 1.0 : 0.0);

        if (phase < 1.5 && burst > 0.0)
        {
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            positionWS = ComicGlitchDisplace(positionWS, burst);
            input.positionOS.xyz = TransformWorldToObject(positionWS);
        }

        Varyings output = LitPassVertex(input);

        // Not spawned yet: every vertex lands on one point, so nothing is drawn.
        if (phase > 1.5)
            output.positionCS = float4(0.0, 0.0, 0.0, 0.0);

        return output;
    #else
        return LitPassVertex(input);
    #endif
}


// Everything lit by two neon colors from the edges in, left and right of the view.
half3 ApplyComicRim(half3 color, float3 normalVS)
{
    float rim = pow(1.0 - saturate(normalVS.z), max(_Comic_RimPower, 0.1));
    half3 tint = lerp(_Comic_RimColorA.rgb, _Comic_RimColorB.rgb, normalVS.x * 0.5 + 0.5);

    return color * (1.0 + _Comic_Hype * 0.18) + tint * rim * _Comic_RimStrength * (1.0 + _Comic_Hype * 2.0);
}


// Glitch shards: slanted slivers filled like a comic panel, not plain tinted boxes.
// Sizes are in screen heights, so the look is the same at any resolution.
half3 ApplyComicGlitch(half3 color, float2 pixelPosition, float burst)
{
    float2 uv = pixelPosition / _ScreenParams.y;
    float timeStep = ComicGlitchStep();

    float rowPosition = (uv.y + uv.x * 0.22) / max(_Comic_GlitchShardHeight, 0.001);
    float row = floor(rowPosition);
    float rowHash = ComicHash(float2(row, timeStep));

    // Every row has its own shard length, and rows slide against each other.
    float cellWidth = _Comic_GlitchShardHeight * lerp(2.0, 9.0, rowHash);
    float cell = floor((uv.x + uv.y * 0.4 + rowHash * 5.0) / max(cellWidth, 0.001));

    float stepOffset = frac(timeStep * 0.137);

    if (ComicHash(float2(cell, row) + stepOffset) >= _Comic_GlitchShardAmount * burst)
        return color;

    float pick = ComicHash(float2(row, cell) + stepOffset + 1.7);
    half3 tint = frac(pick * 7.0) < 0.5 ? _Comic_GlitchColorA.rgb : _Comic_GlitchColorB.rgb;
    half3 filled;

    if (pick < 0.45)
    {
        // Ben-Day dots.
        filled = lerp(color, tint, ComicGlitchDots(uv, 0.36));
    }
    else if (pick < 0.8)
    {
        // Two-tone: light parts take the tint, the rest goes to ink.
        float luminance = dot(color, float3(0.299, 0.587, 0.114));
        filled = lerp(half3(0.04, 0.02, 0.08), tint, step(0.35, luminance));
    }
    else
    {
        // Diagonal speed lines.
        float lines = step(frac((uv.x - uv.y) / max(_Comic_GlitchDotScale * 2.0, 0.001)), 0.5);
        filled = lerp(color * 0.3, tint, lines);
    }

    // A solid edge along the bottom, like a misaligned print layer.
    if (frac(rowPosition) < 0.14)
        filled = tint;

    return lerp(color, filled, _Comic_GlitchShardStrength);
}

// Screen-space hatch line.
float ComicHatchLine(float2 pixelPosition, float angle)
{
    float radians = angle * 0.0174532925;

    float2 direction = float2(
        cos(radians),
        sin(radians)
    );

    float linePosition = frac(
        dot(pixelPosition, direction) / max(_Comic_HatchSpacing, 0.001)
    );

    float distanceToLine = abs(linePosition - 0.5);

    return saturate(1.0 - smoothstep(
        _Comic_HatchWidth,
        _Comic_HatchWidth + 0.04,
        distanceToLine
    ));
}


// Halftone dots sized by how dark the lit color is.
half3 ApplyComicHalftone(half3 color, float2 pixelPosition)
{
    float luminance = dot(color, float3(0.299, 0.587, 0.114));

    float adjustedLuminance = saturate(
        (luminance - 0.5) * _Comic_Contrast + 0.5
    );

    float2 gridPosition = frac(pixelPosition / _Comic_DotScale) - 0.5;
    float distanceFromCenter = length(gridPosition);

    float shadowAmount = 1.0 - adjustedLuminance;

    float dotRadius = lerp(0.08, 0.48, shadowAmount);

    float dots = 1.0 - smoothstep(
        dotRadius - 0.035,
        dotRadius,
        distanceFromCenter
    );

    float shadowMask = 1.0 - smoothstep(
        _Comic_HalftoneShadowThreshold - _Comic_HalftoneShadowSoftness,
        _Comic_HalftoneShadowThreshold,
        shadowAmount
    );

    half3 comicColor = lerp(
        _Comic_PaperColor.rgb,
        _Comic_DotColor.rgb,
        dots * _Comic_DotDarkness * shadowMask
    );

    return lerp(color, color * comicColor, _Comic_HalftoneStrength);
}


// Hatching where this pixel is in shadow or faces away from the light.
half3 ApplyComicHatching(half3 color, float2 pixelPosition, Varyings input)
{
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    #else
        float4 shadowCoord = float4(0, 0, 0, 0);
    #endif

    Light mainLight = GetMainLight(shadowCoord);

    float NdotL = saturate(
        dot(normalize(input.normalWS), mainLight.direction)
    );

    float castShadow = 1.0 - mainLight.shadowAttenuation;
    float unlitSurface = 1.0 - NdotL;

    float shadowAmount = saturate(
        castShadow * _Comic_ShadowContribution +
        unlitSurface * _Comic_UnlitContribution
    );

    float shadowMask = smoothstep(
        _Comic_HatchShadowThreshold,
        _Comic_HatchShadowThreshold + _Comic_HatchShadowSoftness,
        shadowAmount
    );

    float hatchPattern = ComicHatchLine(pixelPosition, _Comic_HatchAngle);

    if (_Comic_EnableCrossHatching > 0.5)
    {
        float crossMask = smoothstep(
            _Comic_CrossHatchThreshold,
            min(_Comic_CrossHatchThreshold + 0.2, 1.0),
            shadowAmount
        );

        float hatchB = ComicHatchLine(pixelPosition, _Comic_HatchAngle + 90.0);

        hatchPattern = max(
            hatchPattern,
            hatchB * crossMask * _Comic_CrossHatchStrength
        );
    }

    float hatchOpacity =
        hatchPattern *
        shadowMask *
        _Comic_HatchStrength *
        _Comic_HatchColor.a;

    return lerp(color, _Comic_HatchColor.rgb, saturate(hatchOpacity));
}


void ComicLitFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    LitPassFragment(
        input
        , outColor
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );

    float2 pixelPosition = input.positionCS.xy;

    // Dot and line sizes are in "pixels of a 1080p screen", so a render scale of 3
    // or a small phone screen doesn't shrink them.
    float2 printPosition = pixelPosition * 1080.0 / _ScreenParams.y;

    #if defined(_COMIC_HALFTONE)
        outColor.rgb = ApplyComicHalftone(outColor.rgb, printPosition);
    #endif

    outColor.rgb = ApplyComicHatching(outColor.rgb, printPosition, input);

    if (_Comic_SurfaceStrength > 0.0)
    {
        half3 surface = SAMPLE_TEXTURE2D(_Comic_SurfaceMap, sampler_Comic_SurfaceMap, input.uv * _Comic_SurfaceTiling).rgb;

        // Dark parts of the texture only dim, so nothing goes fully black.
        outColor.rgb = lerp(outColor.rgb, outColor.rgb * (0.35 + surface * 1.5), _Comic_SurfaceStrength);
    }

    float3 normalVS = TransformWorldToViewDir(normalize(input.normalWS));

    outColor.rgb = ApplyComicRim(outColor.rgb, normalVS);

    #if defined(_COMIC_GLITCH)
        // These read UNITY_MATRIX_M, which needs the instance set up here too.
        UNITY_SETUP_INSTANCE_ID(input);

        if (ComicSpawnPhase() > 0.5)
        {
            outColor.rgb = ComicSpawnShards(outColor.rgb, pixelPosition);
        }
        else
        {
            float burst = ComicGlitchBurst();

            if (burst > 0.0)
                outColor.rgb = ApplyComicGlitch(outColor.rgb, pixelPosition, burst);
        }
    #endif
}

#endif
