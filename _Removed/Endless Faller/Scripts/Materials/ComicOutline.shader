
Shader "Hidden/ComicOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
       _OutlineWidth ("Outline Width", Range(0,100)) = 1
       _WiggleSpeed ("Wiggle Speed", Range(0,20)) = 5
        _WiggleAmount ("Wiggle Amount", Range(0,20)) = 3
        _DistortionAmount ("Distortion Amount", Range(0, 0.02)) = 0.003
_DistortionSpeed ("Distortion Speed", Range(0, 20)) = 5
_DistortionFrequency ("Distortion Frequency", Range(1, 100)) = 25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ComicOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            ColorMask RGB

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _WiggleSpeed;
                float _WiggleAmount;
                float _DistortionAmount;
float _DistortionSpeed;
float _DistortionFrequency;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

float Hash(float n)
{
    return frac(sin(n) * 43758.5453);
}

float GetDistortion(float3 positionWS)
{
    float time = _Time.y * _DistortionSpeed;

    float noise = sin(
        positionWS.y * _DistortionFrequency + time
    );

    float randomNoise = Hash(
        floor(positionWS.y * _DistortionFrequency) + floor(time)
    );

    return (noise * 0.7 + randomNoise * 0.3)
        * _DistortionAmount;
}


Varyings Vert(Attributes input)
{
    Varyings output;

    float3 positionWS =
        TransformObjectToWorld(input.positionOS.xyz);

    float3 normalWS =
        TransformObjectToWorldNormal(input.normalOS);

    float4 positionCS =
        TransformWorldToHClip(positionWS);

    float3 normalVS =
        TransformWorldToViewDir(normalWS, true);

    float4 normalCS =
        mul(UNITY_MATRIX_P, float4(normalVS, 0));

    float2 direction = normalCS.xy;

    float directionLength = length(direction);

    if (directionLength > 0.0001)
        direction /= directionLength;
    else
        direction = float2(0, 0);

    // Animate outline width
    float wiggle =
        sin(_Time.y * _WiggleSpeed) * _WiggleAmount;

    float animatedWidth =
        clamp(_OutlineWidth + wiggle, 1.0, 20.0);

    // Calculate subtle distortion
    float distortion = GetDistortion(positionWS);

    // Final outline expansion
    float outlineWidth =
        animatedWidth * 0.0005 + distortion;

    positionCS.xy +=
        direction * outlineWidth * positionCS.w;

    output.positionHCS = positionCS;

    return output;
}



            half4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
