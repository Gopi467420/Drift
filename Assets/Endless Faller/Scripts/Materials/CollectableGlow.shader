// A collectable that stands out from any sky: flat bright color, a hotter edge, a slow
// pulse, and a dark comic rim drawn just outside its shape. No lighting or textures.
Shader "Custom/CollectableGlow"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (2.4, 1.6, 0.25, 1)
        [HDR] _EdgeColor("Edge Color", Color) = (4, 3.4, 1.6, 1)
        _EdgePower("Edge Width", Range(0.5, 6)) = 2
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 3
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.3
        _InkColor("Ink Color", Color) = (0.03, 0.02, 0.05, 1)
        _InkScale("Ink Size", Range(1, 1.6)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
            #pragma target 2.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EdgeColor;
                half4 _InkColor;
                float _EdgePower;
                float _PulseSpeed;
                float _PulseAmount;
                float _InkScale;
            CBUFFER_END
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalVS : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS));
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float facing = saturate(normalize(input.normalVS).z);
                float edge = pow(1.0 - facing, _EdgePower);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                return half4(lerp(_Color.rgb, _EdgeColor.rgb, edge) * pulse, 1.0);
            }
            ENDHLSL
        }

        // The rim: a larger copy, showing only its far side, so it sits behind the shape.
        Pass
        {
            Name "Ink"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vertex(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz * _InkScale);
            }

            half4 Fragment() : SV_Target
            {
                return _InkColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
