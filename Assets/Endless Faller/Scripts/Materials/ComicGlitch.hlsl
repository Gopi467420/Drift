#ifndef COMIC_GLITCH_INCLUDED
#define COMIC_GLITCH_INCLUDED

// Shared by the ForwardLit pass and the ghost pass.
// Glitch is a short burst on one object now and then, not a constant effect.
// Set from the ComicStyle asset (ComicStyleSettings.Apply).

float _Comic_GlitchSpeed;
float _Comic_GlitchInterval;
float _Comic_GlitchDuration;
float _Comic_GlitchChance;
half4 _Comic_GlitchColorA;
half4 _Comic_GlitchColorB;
float _Comic_GlitchShardAmount;
float _Comic_GlitchShardHeight;
float _Comic_GlitchShardStrength;
float _Comic_GlitchDotScale;
float _Comic_GlitchSliceAmount;
float _Comic_GlitchSliceCount;
float _Comic_GlitchSliceShift;
float _Comic_GlitchGhostOffset;
float _Comic_SpawnDistance;
float _Comic_SpawnBand;
float _Comic_Hype;


float ComicHash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);

    return frac(p.x * p.y);
}


// Glitch time only changes a few times per second, so the pattern "jumps".
float ComicGlitchStep()
{
    return floor(_Time.y * _Comic_GlitchSpeed);
}


// A stable number per object. Uses X/Z because objects only travel along Y.
float ComicGlitchSeed()
{
    float2 origin = float2(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m23);
    return ComicHash(origin * 0.0173 + 0.31);
}


// 0 = calm. Otherwise the burst strength, stepping down 1, .75, .5, .25.
// Time is cut into windows of Interval seconds, offset per object. Each window
// rolls Chance, and a burst that wins starts at a random moment inside it.
float ComicGlitchBurst()
{
    float interval = max(_Comic_GlitchInterval, 0.1);
    float seed = ComicGlitchSeed();

    float time = _Time.y / interval + seed * 31.0;
    float window = floor(time);

    // The hotter the run, the more the world glitches.
    if (ComicHash(float2(window, seed)) >= _Comic_GlitchChance + _Comic_Hype * 0.35)
        return 0.0;

    float burstLength = max(saturate(_Comic_GlitchDuration / interval), 0.0001);
    float start = ComicHash(float2(seed, window) + 7.7) * (1.0 - burstLength);
    float progress = (frac(time) - start) / burstLength;

    if (progress < 0.0 || progress >= 1.0)
        return 0.0;

    return 1.0 - floor(progress * 4.0) * 0.25;
}


// Objects appear by distance, which needs no per-object state and works with pooling:
//   2 = still too far, not drawn at all
//   1 = glitching in, drawn as shapes for a moment
//   0 = real object (the jump from 1 to 0 is the abrupt snap)
float ComicSpawnPhase()
{
    if (_Comic_SpawnDistance <= 0.0)
        return 0.0;

    float3 origin = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
    float distanceToCamera = length(origin - _WorldSpaceCameraPos);
    float reveal = _Comic_SpawnDistance * (0.96 + ComicGlitchSeed() * 0.08);

    if (distanceToCamera < reveal)
        return 0.0;

    return distanceToCamera < reveal + _Comic_SpawnBand ? 1.0 : 2.0;
}


// Bands of the screen snap sideways. Built in view space so it reads the same
// from any camera angle. Only vertices move, so low-poly meshes bend, not tear.
float3 ComicGlitchDisplace(float3 positionWS, float burst)
{
    float3 positionVS = TransformWorldToView(positionWS);
    float slice = floor(positionVS.y / max(-positionVS.z, 0.001) * _Comic_GlitchSliceCount);

    float timeStep = ComicGlitchStep();

    if (ComicHash(float2(slice, timeStep)) < _Comic_GlitchSliceAmount)
    {
        float shift = ComicHash(float2(timeStep, slice) + 3.1) * 2.0 - 1.0;
        positionWS += UNITY_MATRIX_V[0].xyz * shift * _Comic_GlitchSliceShift * burst;
    }

    return positionWS;
}


// The moment before an object exists: complementary pink and green squares and
// triangles over a dark backdrop, re-rolled at the glitch rate.
half3 ComicSpawnShards(half3 color, float2 pixelPosition)
{
    float2 uv = pixelPosition / _ScreenParams.y;
    float size = max(_Comic_GlitchShardHeight * 1.6, 0.001);

    float2 cell = floor(uv / size);
    float2 inside = frac(uv / size);

    // Half of the cells split into two triangles, the rest stay squares.
    float split = step(0.5, ComicHash(cell + 4.3));
    float side = split * step(1.0, inside.x + inside.y);

    float pick = ComicHash(cell + side * 0.37 + frac(ComicGlitchStep() * 0.137));
    float tone = 0.55 + frac(pick * 9.7) * 0.45;

    if (pick < 0.3)
        return half3(0.04, 0.02, 0.08);

    if (pick > 0.9)
        return color;

    return (pick < 0.6 ? _Comic_GlitchColorA.rgb : _Comic_GlitchColorB.rgb) * tone;
}


// Ben-Day dots on a 45 degree grid, like comic print. uv is in screen heights.
float ComicGlitchDots(float2 uv, float radius)
{
    float2 rotated = float2(uv.x + uv.y, uv.x - uv.y) / max(_Comic_GlitchDotScale, 0.001);

    return step(length(frac(rotated) - 0.5), radius);
}

#endif
