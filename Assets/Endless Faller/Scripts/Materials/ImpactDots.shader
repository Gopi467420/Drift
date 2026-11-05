// The power surge: a small black circle with black and purple dots flying out of it.
// Drawn full screen by a quad on the camera, only while it is playing. It sits in the
// world's transparent queue, so the player (a later queue) stays on top of it.
// _Grow opens it from the center, _Progress carries the dots outward as they shrink.
Shader "Custom/ImpactDots"
{
    Properties
    {
        _Grow("Grow", Range(0, 1)) = 1
        _Progress("Progress", Range(0, 1)) = 0
        _Color("Ink", Color) = (0, 0, 0, 1)
        _Violet("Purple", Color) = (0.42, 0.1, 0.95, 1)
        _HoleSize("Center Circle Size", Range(0, 0.3)) = 0.05
        _DotAmount("Dot Amount", Range(0, 1)) = 0.5
        _DotSize("Dot Size", Range(0.3, 3)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+50"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnityCG.cginc"

            float _Grow;
            float _Progress;
            float _HoleSize;
            float _DotAmount;
            float _DotSize;
            fixed4 _Color;
            fixed4 _Violet;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Fills the screen whatever the camera is doing.
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                float2 screen = input.uv * 2.0 - 1.0;
                output.vertex = float4(screen.x, screen.y * _ProjectionParams.x, UNITY_NEAR_CLIP_VALUE, 1.0);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // One grid of round dots. density is how likely a cell is to hold one.
            float Dots(float2 p, float cell, float density, float seed)
            {
                float2 grid = p / cell + seed;
                float2 id = floor(grid);

                if (density <= 0.0 || Hash(id + 5.3) >= density) return 0.0;

                float size = Hash(id + 1.3);
                float lifetime = 0.35 + Hash(id + 7.9) * 0.65;

                float shrink = saturate(1.0 - _Progress / lifetime);
                if (shrink <= 0.0) return 0.0;

                float2 center = (float2(Hash(id + 2.1), Hash(id + 4.7)) - 0.5) * 0.4;
                float2 offset = frac(grid) - 0.5 - center;

                float radius = (0.1 + size * size * 0.16) * _DotSize * sqrt(shrink);

                return 1.0 - smoothstep(radius - 0.03, radius, length(offset));
            }

            fixed4 Fragment(Varyings input) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 centered = (input.uv - 0.5) * float2(aspect, 1.0);

                // Dots keep traveling outward through the fade.
                float2 p = centered / (max(_Grow, 0.001) * (1.0 + _Progress * 0.6));

                float distanceToCenter = length(p);

                // Thickest near the circle, thinning toward the screen edges.
                float density = saturate(1.15 - distanceToCenter / 0.75) * _DotAmount;

                float purple = max(Dots(p, 0.075, density, 13.7), Dots(p, 0.034, density, 21.3));
                float black = max(Dots(p, 0.09, density, 2.9), Dots(p, 0.04, density, 3.7));

                float3 color = _Violet.rgb;
                float alpha = purple;

                color = lerp(color, _Color.rgb, black);
                alpha = max(alpha, black);

                // The small circle that everything came out of.
                float circle = 1.0 - smoothstep(-0.003, 0.003, length(centered) - _HoleSize * _Grow * (1.0 - _Progress));
                color = lerp(color, _Color.rgb, circle);
                alpha = max(alpha, circle);

                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
