// A collectable that reads as pure energy: flat bright color, a hotter edge, and a
// slow pulse. No lighting or textures, and colors above 1 so bloom catches them.
Shader "Custom/CollectableGlow"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (2.4, 1.6, 0.25, 1)
        [HDR] _EdgeColor("Edge Color", Color) = (4, 3.4, 1.6, 1)
        _EdgePower("Edge Width", Range(0.5, 6)) = 2
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 3
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EdgeColor;
                float _EdgePower;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

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
    }

    FallBack Off
}
