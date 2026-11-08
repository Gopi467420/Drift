// A see-through shell: empty in the middle, glowing where the surface curves away from
// the camera. Made for the spheres around collectables. No lighting, and colors above 1
// so bloom catches them.
Shader "Custom/EdgeGlow"
{
    Properties
    {
        [HDR] _GlowColor("Glow Color", Color) = (1.5, 1, 0.1, 1)
        _EdgeWidth("Edge Width", Range(0.5, 8)) = 3
        _Strength("Strength", Range(0, 4)) = 1.5
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 2.5
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                float _EdgeWidth;
                float _Strength;
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
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float facing = saturate(dot(normalize(input.normalWS), normalize(input.viewWS)));
                float edge = pow(1.0 - facing, _EdgeWidth);
                float pulse = 1.0 - _PulseAmount * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));

                return half4(_GlowColor.rgb, saturate(edge * _Strength * pulse));
            }
            ENDHLSL
        }
    }
}
