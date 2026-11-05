// What the player falls toward: soft streaks of colored light pouring out of a glowing
// center and fading into black, with hexagon rings spinning up from the center toward
// the player. No texture: everything is worked out per pixel.
// All speeds use _Time, which follows Time.timeScale, so slow motion slows the whole sky.
// All color comes from DreamPalette.
Shader "Custom/DreamSky"
{
    Properties
    {
        _FlowSpeed("Flow Speed", Range(0, 3)) = 0.5
        _Fade("Fade To Black Radius", Range(0.2, 1.5)) = 0.62
        _SunGlow("Center Glow Size", Range(0, 0.6)) = 0.24
        _Brightness("Streak Brightness", Range(0, 2)) = 1

        [Header(Hexagons)]
        _HexSpeed("Rings Per Second", Range(0, 1)) = 0.1
        _HexSpin("Spin Speed", Range(0, 2)) = 0.25
        _HexWidth("Dot Band Width", Range(0.02, 0.4)) = 0.14
        _HexGlitch("Glitch Amount", Range(0, 1)) = 0.3
        _HexStrength("Strength", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "DreamPalette.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _FlowSpeed;
                float _Fade;
                float _SunGlow;
                float _Brightness;
                float _HexSpeed;
                float _HexSpin;
                float _HexWidth;
                float _HexGlitch;
                float _HexStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth noise around the circle. The wrap keeps it seamless at the join.
            float Streak(float around, float columns, float seed)
            {
                float x = around * columns;
                float column = floor(x);
                float blend = frac(x);
                blend = blend * blend * (3.0 - 2.0 * blend);

                return lerp(
                    Hash(float2(fmod(column, columns), seed)),
                    Hash(float2(fmod(column + 1.0, columns), seed)),
                    blend);
            }

            half3 SoftStreaks(float around, float outward, float radius)
            {
                float time = _Time.y * _FlowSpeed;

                float wide = Streak(around, 11.0, 1.3);
                float colorPatch = Streak(around, 7.0, 5.1);
                float fine = Streak(around, 53.0, 9.7);
                float thin = Streak(around, 137.0, 3.3);

                // Wide colors melt into each other, so no hard edge survives.
                half3 color = lerp(_Dream_Swirl.rgb, _Dream_Mid.rgb, smoothstep(0.3, 0.7, wide));
                color = lerp(color, _Dream_Accent.rgb, smoothstep(0.6, 0.82, colorPatch));

                // Light travels outward along each streak.
                float flow = sin(outward * 5.0 - time * 6.0 + fine * 12.0) * 0.5 + 0.5;
                color *= 0.3 + 0.7 * fine * fine * (0.45 + 0.55 * flow);

                // A few thin bright lines over the top.
                float ringLine = thin * thin * thin * thin;
                float pulse = sin(outward * 9.0 - time * 16.0 + thin * 40.0) * 0.5 + 0.5;
                color += _Dream_Center.rgb * ringLine * pulse * 0.9;

                return color * _Brightness;
            }

            // One hexagon outline value: 0 on the center, growing evenly toward the corners.
            float HexDistance(float2 p)
            {
                p = abs(p);
                return max(p.x * 0.866 + p.y * 0.5, p.y);
            }

            half3 Hexagons(half3 color, float2 sky, float2 screen)
            {
                const float Rings = 5.0;

                float travel = _Time.y * _HexSpeed;
                float glitchStep = floor(_Time.y * 12.0);

                for (float i = 0.0; i < Rings; i += 1.0)
                {
                    // 0 = at the center, 1 = reached the player.
                    float progress = frac(travel + i / Rings);

                    // Growing exponentially reads as flying through them at constant speed.
                    float size = 0.012 * exp(progress * 4.3);

                    float direction = fmod(i, 2.0) < 0.5 ? 1.0 : -1.0;
                    float angle = _Time.y * _HexSpin * direction + i * 1.1;
                    float s = sin(angle);
                    float c = cos(angle);

                    float2 p = float2(sky.x * c - sky.y * s, sky.x * s + sky.y * c);

                    // Bands of the ring snap sideways, like a bad signal.
                    float slice = floor(p.y / size * 9.0);
                    float tear = Hash(float2(slice + i * 7.0, glitchStep));
                    if (tear > 1.0 - _HexGlitch * 0.35)
                        p.x += (Hash(float2(slice, glitchStep + i)) - 0.5) * size * 0.5;

                    float away = abs(HexDistance(p) - size);

                    float ringLine = 1.0 - smoothstep(size * 0.012, size * 0.028, away);
                    float band = 1.0 - smoothstep(size * _HexWidth * 0.85, size * _HexWidth, away);

                    // Dots grow larger toward the solid line.
                    float dotSize = 0.5 * saturate(1.0 - away / (size * _HexWidth));
                    float dots = band * DreamDots(screen, dotSize);

                    float fade = smoothstep(0.0, 0.2, progress) * (1.0 - smoothstep(0.72, 1.0, progress));
                    float mask = saturate(ringLine + dots) * fade * _HexStrength;

                    half3 tint = fmod(i, 2.0) < 0.5 ? _Dream_Accent.rgb : _Dream_Mid.rgb;
                    tint = lerp(tint, half3(1.0, 1.0, 1.0), 0.45 + ringLine * 0.45);

                    color = lerp(color, tint * 1.3, mask);
                }

                return color;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);

                // Flat coordinates with (0,0) straight below the player.
                float2 sky = direction.xz / max(1.0 - direction.y, 0.001);
                float radius = length(sky);
                float angle = atan2(sky.y, sky.x);

                half3 color = _Dream_Edge.rgb;

                // Past the fade everything is black, so the streak work is skipped out there.
                if (radius < _Fade)
                {
                    float around = angle / 6.2831853 + 0.5;
                    float outward = log(radius + 0.0001);

                    color = SoftStreaks(around, outward, radius);

                    float glow = saturate(1.0 - radius / max(_SunGlow, 0.001));
                    color = lerp(color, _Dream_Mid.rgb, glow * glow * 0.7);
                    color += _Dream_Center.rgb * glow * glow * glow * 1.5;

                    color *= 1.0 - smoothstep(_Fade * 0.45, _Fade, radius);
                }

                color = Hexagons(color, sky, input.positionCS.xy / _ScreenParams.y);

                color += _Dream_RewardCloud.rgb * _Dream_Flash * 0.3;

                // Black screen: the sky breaks back through in glitching blocks.
                if (_Comic_Blackout > 0.0)
                {
                    float2 block = floor(input.positionCS.xy / _ScreenParams.y * float2(9.0, 26.0));
                    float roll = Hash(block + floor(_Time.y * 24.0) * 0.137);

                    if (roll < _Comic_Blackout)
                    {
                        // Not empty black: faint print dots and hatching keep it a comic panel.
                        float2 screen = input.positionCS.xy / _ScreenParams.y;
                        float hatch = step(frac((screen.x + screen.y) * 38.0), 0.12);

                        color = _Dream_Swirl.rgb * (DreamDots(screen, 0.3) * 0.16 + hatch * 0.08);
                    }
                }

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
