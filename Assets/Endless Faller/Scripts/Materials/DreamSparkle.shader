// Small glowing streaks that rush past the camera and sell the speed.
// No texture: the shape is worked out from the particle's UV.
Shader "Custom/DreamSparkle"
{
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "DreamPalette.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * 2.0 - 1.0;
                output.color = input.color;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float glow = saturate(1.0 - length(input.uv));

                // Over-bright on purpose, so bloom catches the streaks against the dark sky.
                return half4((_Dream_CloudLight.rgb + 0.6) * 2.5 * glow * glow * input.color.rgb * input.color.a, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
