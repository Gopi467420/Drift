#ifndef COMIC_OUTLINE_INCLUDED
#define COMIC_OUTLINE_INCLUDED

#include "ComicGlitch.hlsl"

// The object drawn a second time, slightly fatter and pushed back in depth, so only
// a rim shows around it. It draws the same faces as the object itself, which keeps
// single-sided meshes correct. The width is a share of the screen, so distance
// doesn't change it.
// Black over the bright center of the sky, white over the dark outer sky.

float _Comic_OutlineWidth;
float _Comic_OutlineSwitch;
float _Comic_OutlineNear;

struct OutlineAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct OutlineVaryings
{
    float4 positionCS : SV_POSITION;
};

OutlineVaryings ComicOutlineVertex(OutlineAttributes input)
{
    OutlineVaryings output = (OutlineVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);

    float phase = 0.0;

    #if defined(_COMIC_GLITCH)
        phase = ComicSpawnPhase();
    #endif

    if (phase > 1.5 || _Comic_OutlineWidth <= 0.0)
        return output;

    // Right up close the rim is only noise, and big objects are about to leave anyway.
    float3 origin = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
    if (length(origin - _WorldSpaceCameraPos) < _Comic_OutlineNear)
        return output;

    // Blending toward the center direction closes the gaps at hard corners of boxy meshes.
    float3 direction = normalize(lerp(input.normalOS, normalize(input.positionOS.xyz + 0.0001), 0.35));
    float3 normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(direction));
    float2 push = normalize(normalVS.xy + 0.0001);

    float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
    positionCS.x += push.x * _Comic_OutlineWidth * 2.0 * positionCS.w * _ScreenParams.y / _ScreenParams.x;
    positionCS.y += push.y * _Comic_OutlineWidth * 2.0 * positionCS.w * _ProjectionParams.x;

    // Behind the object, so the object always wins except along its edge.
    #if UNITY_REVERSED_Z
        positionCS.z *= 0.985;
    #else
        positionCS.z = lerp(positionCS.z, positionCS.w, 0.015);
    #endif

    output.positionCS = positionCS;
    return output;
}

half4 ComicOutlineFragment(OutlineVaryings input) : SV_Target
{
    float2 centered = input.positionCS.xy / _ScreenParams.y - float2(0.5 * _ScreenParams.x / _ScreenParams.y, 0.5);

    return half4(step(_Comic_OutlineSwitch, length(centered)) * half3(1.0, 1.0, 1.0), 1.0);
}

#endif
