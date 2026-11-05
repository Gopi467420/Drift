#ifndef DREAM_PALETTE_INCLUDED
#define DREAM_PALETTE_INCLUDED

// Colors shared by the sky, clouds and sparkles. Set by the DreamSky component,
// so one palette change recolors everything at once.
half4 _Dream_Center;
half4 _Dream_Mid;
half4 _Dream_Edge;
half4 _Dream_Swirl;
half4 _Dream_CloudLight;
half4 _Dream_CloudShadow;
half4 _Dream_Accent;
half4 _Dream_RewardCloud;
float _Dream_Flash;
float _Dream_DotScale;
float _Comic_Blackout;


// Comic print dots on a 45 degree grid. uv is in screen heights. radius 0-1.
float DreamDots(float2 uv, float radius)
{
    float2 rotated = float2(uv.x + uv.y, uv.x - uv.y) / max(_Dream_DotScale, 0.0005);

    return step(length(frac(rotated) - 0.5), radius);
}

#endif
