// An afterimage of the player: a red see-through shell, brightest at its edges, with a
// cyan copy split off to one side. It glitches: rows drop out, scanlines run through it,
// the whole echo jumps now and then, and it flickers. GhostTrail sets _Fade and _Seed.
Shader "Custom/GhostEcho"
{
    Properties
    {
        [HDR] _Color("Echo Color", Color) = (0.45, 0, 0.08, 1)
        [HDR] _SplitColor("Split Color", Color) = (0, 0.9, 1, 0.7)
        _Split("Split Distance", Range(0, 0.05)) = 0.02
        _RimPower("Edge Sharpness", Range(0.5, 6)) = 2
        _Glitch("Glitch", Range(0, 1)) = 0.7
        _Bands("Glitch Rows", Range(4, 80)) = 36
        _Scanlines("Scanlines", Range(0, 600)) = 240
        _Fade("Fade", Range(0, 1)) = 1
        _Seed("Seed", Float) = 0
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

        HLSLINCLUDE
            #pragma target 2.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _SplitColor;
                float _Split;
                float _RimPower;
                float _Glitch;
                float _Bands;
                float _Scanlines;
                float _Fade;
                float _Seed;
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
                float4 screen : TEXCOORD2;
            };

            float Hash(float value)
            {
                return frac(sin(value * 91.3458) * 47453.5453);
            }

            // Changes a few times a second, which gives the glitch its stutter.
            float GlitchStep()
            {
                return floor(_Time.y * 14.0);
            }

            Varyings EchoVertex(Attributes input, float shift)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float4 position = TransformWorldToHClip(positionWS);

                // Now and then the whole echo jumps sideways.
                float tick = GlitchStep();
                float jumps = step(0.7, Hash(tick * 1.7 + _Seed));
                float jump = (Hash(tick + _Seed * 5.0) - 0.5) * 0.05 * _Glitch * jumps;
                position.x += (jump + shift) * position.w;

                output.positionCS = position;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);
                output.screen = ComputeScreenPos(position);
                return output;
            }

            half4 EchoColor(Varyings input, half4 tint)
            {
                float screenY = input.screen.y / input.screen.w;
                float tick = GlitchStep();

                // Some rows drop out for a moment.
                float row = floor(screenY * _Bands);
                float dropped = step(1.0 - 0.3 * _Glitch, Hash(row + tick * 3.1 + _Seed));

                float scan = 0.8 + 0.2 * sin(screenY * _Scanlines * 6.2832);
                float flicker = lerp(1.0, 0.35, step(0.85, Hash(tick * 0.7 + _Seed)));

                float facing = saturate(dot(normalize(input.normalWS), normalize(input.viewWS)));
                float edge = pow(1.0 - facing, _RimPower);

                float alpha = saturate((0.85 + edge * 0.5) * scan * flicker * (1.0 - dropped));
                return half4(lerp(tint.rgb, 1.0, edge * 0.25), alpha * tint.a * _Fade);
            }
        ENDHLSL

        // The cyan copy, a little to the side. URP draws this pass first, so it sits behind.
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            Varyings Vertex(Attributes input) { return EchoVertex(input, -_Split); }
            half4 Fragment(Varyings input) : SV_Target { return EchoColor(input, _SplitColor); }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            Varyings Vertex(Attributes input) { return EchoVertex(input, 0.0); }
            half4 Fragment(Varyings input) : SV_Target { return EchoColor(input, _Color); }
            ENDHLSL
        }
    }
}
