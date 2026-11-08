// A UI image that can glitch: bands of it slide sideways while cyan and pink copies
// split away from it. GlitchLogo drives _Glitch and _Seed. At 0 it draws the plain image.
Shader "Custom/GlitchLogo"
{
    Properties
    {
        [PerRendererData] _MainTex("Image", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _Glitch("Glitch", Range(0, 1)) = 0
        _Seed("Seed", Float) = 0
        _Bands("Bands", Range(4, 40)) = 14
        _MaxShift("Largest Slide", Range(0, 0.2)) = 0.08
        _Split("Color Split", Range(0, 0.05)) = 0.02
        _SplitColorA("Split Color A", Color) = (0, 0.95, 1, 1)
        _SplitColorB("Split Color B", Color) = (1, 0.15, 0.65, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _Glitch;
            float _Seed;
            float _Bands;
            float _MaxShift;
            float _Split;
            fixed4 _SplitColorA;
            fixed4 _SplitColorB;

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            float Hash(float value)
            {
                return frac(sin(value * 91.3458) * 47453.5453);
            }

            fixed4 Fragment(Varyings input) : SV_Target
            {
                // Only some bands slide, so the image tears rather than shakes.
                float band = floor(input.uv.y * _Bands);
                float slides = step(0.5, Hash(band + _Seed));
                float slide = (Hash(band * 1.7 + _Seed + 3.0) - 0.5) * 2.0 * _MaxShift * slides * _Glitch;

                float2 uv = input.uv + float2(slide, 0.0);
                float2 split = float2(_Split * _Glitch, 0.0);

                fixed4 image = tex2D(_MainTex, uv);
                fixed alphaA = tex2D(_MainTex, uv + split).a;
                fixed alphaB = tex2D(_MainTex, uv - split).a;

                // Painted back to front: cyan, pink, then the image on top.
                fixed3 color = _SplitColorA.rgb * alphaA;
                color = lerp(color, _SplitColorB.rgb, alphaB);
                fixed alpha = max(alphaA, alphaB);

                color = lerp(color, image.rgb, image.a);
                alpha = max(alpha, image.a);

                return fixed4(color, alpha) * input.color.a;
            }
            ENDCG
        }
    }
}
