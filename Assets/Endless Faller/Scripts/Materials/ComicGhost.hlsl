#ifndef COMIC_GHOST_INCLUDED
#define COMIC_GHOST_INCLUDED

#include "ComicGlitch.hlsl"

// The misprinted color layer: the object drawn again, offset on screen, in a
// flat dotted color. This is what breaks the silhouette during a burst.
// Outside a burst every vertex collapses to one point, so no pixels are drawn.

struct GhostAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GhostVaryings
{
    float4 positionCS : SV_POSITION;
    half3 tint : COLOR0;
};

GhostVaryings ComicGhostVertex(GhostAttributes input)
{
    GhostVaryings output = (GhostVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);

    #if defined(_COMIC_GLITCH)
        float phase = ComicSpawnPhase();
        float burst = phase > 1.5 ? 0.0 : max(ComicGlitchBurst(), phase);

        if (burst > 0.0)
        {
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            positionWS = ComicGlitchDisplace(positionWS, burst);

            float4 positionCS = TransformWorldToHClip(positionWS);

            // Flips side and color every step, so the eye sees both layers.
            float flip = ComicHash(float2(ComicGlitchSeed(), ComicGlitchStep())) < 0.5 ? -1.0 : 1.0;
            float offset = flip * _Comic_GlitchGhostOffset * burst * 2.0 * positionCS.w;

            // The offset is in screen heights, so it matches at any resolution.
            positionCS.x += offset * _ScreenParams.y / _ScreenParams.x;
            positionCS.y += offset * 0.35;

            // Pushed back slightly, so the object itself always draws on top.
            #if UNITY_REVERSED_Z
                positionCS.z *= 0.98;
            #else
                positionCS.z = lerp(positionCS.z, positionCS.w, 0.02);
            #endif

            output.positionCS = positionCS;
            output.tint = flip < 0.0 ? _Comic_GlitchColorA.rgb : _Comic_GlitchColorB.rgb;
        }
    #endif

    return output;
}

half4 ComicGhostFragment(GhostVaryings input) : SV_Target
{
    // Big dots that nearly touch: reads as a solid layer with a print texture.
    float2 uv = input.positionCS.xy / _ScreenParams.y;
    clip(ComicGlitchDots(uv, 0.5) - 0.5);

    return half4(input.tint, 1.0);
}

#endif
