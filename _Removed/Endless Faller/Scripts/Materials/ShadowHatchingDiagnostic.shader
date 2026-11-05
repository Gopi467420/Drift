
Shader "Hidden/ShadowHatchingDiagnostic"
{
    Properties
    {
        [Enum(Red,0,CameraColor,1,Depth,2,Normals,3)]
        _TestMode ("Diagnostic Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ShadowHatchingDiagnostic"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _TestMode;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // TEST 0: Is the renderer pass executing?
                if (_TestMode < 0.5)
                {
                    return half4(1, 0, 0, 1);
                }

                // TEST 1: Is camera color reaching the shader?
                if (_TestMode < 1.5)
                {
                    return SAMPLE_TEXTURE2D_X(
                        _BlitTexture,
                        sampler_LinearClamp,
                        input.texcoord
                    );
                }

                // TEST 2: Is scene depth available?
                if (_TestMode < 2.5)
                {
                    float depth = SampleSceneDepth(input.texcoord);

                    #if UNITY_REVERSED_Z
                        depth = 1.0 - depth;
                    #endif

                    return half4(depth, depth, depth, 1);
                }

                // TEST 3: Are scene normals available?
                float3 normalWS = normalize(
                    SampleSceneNormals(input.texcoord)
                );

                return half4(
                    normalWS * 0.5 + 0.5,
                    1
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
