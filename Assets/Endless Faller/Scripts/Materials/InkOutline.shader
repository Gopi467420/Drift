// A solid outline of one color, for anything that must always stand out, like the player.
// Add it as a second material: it draws the mesh's back faces slightly fatter, so only a
// rim shows around the object. The width is a share of the screen, so distance doesn't
// change it. (ComicLit's own outline flips to white on dark sky, which this one never does.)
Shader "Custom/InkOutline"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)
        _Width("Width", Range(0, 0.05)) = 0.012
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            Cull Front

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vertex(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 position = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float2 direction = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;

                // Multiplied by w to cancel perspective, and squeezed so wide screens match.
                float2 push = normalize(direction + 0.00001) * _Width * position.w;
                push.x *= _ScreenParams.y / _ScreenParams.x;

                position.xy += push;
                return position;
            }

            half4 Fragment() : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
