// Cheap comic clouds: one quad, one texture, no lighting.
// Texture: R = cloud shape, G = how lit it is (drawn as print dots).
// Billboard on (reward clouds): the object's X/Z picks one of the 4 clouds, its
// size and its angle, so one prefab gives every look with no per-object data.
// Billboard off (far clouds): for the particle system, which handles all of that.
Shader "Custom/DreamCloud"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Cloud Atlas", 2D) = "black" {}
        [Toggle(_DREAM_BILLBOARD)] _Billboard("Billboard", Float) = 0
        [Toggle(_DREAM_SOFT)] _Soft("Soft Photo Cloud", Float) = 0
        [ToggleUI] _Crisp("Crisp Poster Edges", Float) = 0
        _Reward("Reward Glow", Range(0, 1)) = 0
        _Opacity("Opacity", Range(0, 1)) = 1
        _FadeNear("Vanish Distance", Float) = 110
        _FadeRange("Fade Range", Float) = 420
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local_vertex _DREAM_BILLBOARD
            #pragma shader_feature_local _DREAM_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "DreamPalette.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _Crisp;
                float _Reward;
                float _Opacity;
                float _FadeNear;
                float _FadeRange;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half alpha : TEXCOORD1;
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(_DREAM_BILLBOARD)
                    float3 center = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                    float size = length(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20));

                    float seed = Hash(center.xz * 0.0173 + 0.31);
                    float pick = floor(seed * 4.0);
                    float2 cell = float2(fmod(pick, 2.0), floor(pick * 0.5));

                    float angle = frac(seed * 13.7) * 6.2832;
                    float s = sin(angle);
                    float c = cos(angle);
                    float2 corner = input.positionOS.xy * size * lerp(0.8, 1.45, frac(seed * 7.3));
                    corner = float2(corner.x * c - corner.y * s, corner.x * s + corner.y * c);

                    float3 centerVS = TransformWorldToView(center);
                    float depth = -centerVS.z;
                    float fade = saturate((depth - _FadeNear) / max(_FadeRange, 1.0));

                    // Gone before it reaches the camera, so a huge close cloud costs no pixels.
                    if (fade > 0.0)
                    {
                        output.positionCS = TransformWViewToHClip(centerVS + float3(corner, 0.0));
                        #if defined(_DREAM_SOFT)
                            output.uv = input.uv;
                        #else
                            output.uv = (input.uv + cell) * 0.5;
                        #endif
                        output.alpha = fade;
                    }
                #else
                    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                    output.uv = input.uv;
                    output.alpha = input.color.a;
                #endif

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 cloud = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                #if defined(_DREAM_SOFT)
                    float value = cloud.r;

                    half3 lit = lerp(_Dream_CloudLight.rgb, _Dream_RewardCloud.rgb, _Reward);

                    // Default: soft shading and a gentle fade at the edges.
                    if (_Crisp < 0.5)
                    {
                        half3 soft = lerp(_Dream_CloudShadow.rgb, lit * 1.4, value);
                        soft += _Dream_RewardCloud.rgb * _Reward * _Dream_Flash * 0.6;

                        return half4(soft, smoothstep(0.04, 0.5, value) * input.alpha * _Opacity);
                    }

                    // Crisp: the texture is cut into flat bands of color with a hard edge
                    // and a bright rim, like ink and screen print.
                    half3 poster = _Dream_CloudShadow.rgb;

                    if (value > 0.3) poster = lit;
                    if (value > 0.62) poster = lit * 1.5 + 0.06;
                    if (value < 0.15) poster = _Dream_Swirl.rgb;

                    poster += _Dream_RewardCloud.rgb * _Reward * _Dream_Flash * 0.6;

                    return half4(poster, smoothstep(0.1, 0.125, value) * input.alpha * _Opacity);
                #endif

// The texture stays crisp up close because the edge is a threshold.
                float shape = cloud.r;
                float alpha = smoothstep(0.46, 0.5, shape) * input.alpha * _Opacity;

                float2 screen = input.positionCS.xy / _ScreenParams.y;
                half3 light = lerp(_Dream_CloudLight.rgb, _Dream_RewardCloud.rgb, _Reward);
                half3 color = lerp(_Dream_CloudShadow.rgb, light, DreamDots(screen, cloud.g * 0.95));

                // Ink line around the edge, like a drawn panel.
                color = lerp(_Dream_CloudShadow.rgb * 0.6, color, smoothstep(0.5, 0.56, shape));

                color += _Dream_RewardCloud.rgb * _Reward * _Dream_Flash * 0.5;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
