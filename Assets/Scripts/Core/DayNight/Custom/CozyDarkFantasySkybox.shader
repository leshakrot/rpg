// CozyDarkFantasySkybox - Universal Render Pipeline (URP)
// Cozy Lowpoly Dark Fantasy style skybox.
// DayNightSystem controls colors via SetColor("_SkyColor", ...) etc.
//
// NightFactor is computed from _SkyColor luminance directly:
//   dot(skyColor, float3(0.299, 0.587, 0.114)) < 0.10 = night
// So keep night _SkyColor dark (lum < 0.10) for stars to appear.

Shader "Custom/CozyDarkFantasySkybox"
{
    Properties
    {
        [Header(Sky Colors - set by DayNightSystem gradients)]
        _SkyColor      ("Sky Color Zenith",  Color) = (0.08, 0.06, 0.20, 1.0)
        _HorizonColor  ("Horizon Color",     Color) = (0.15, 0.12, 0.35, 1.0)
        _GroundColor   ("Ground Color",      Color) = (0.04, 0.04, 0.10, 1.0)

        [Header(Atmosphere Shape)]
        _HorizonSharpness ("Horizon Sharpness",  Range(1, 12))  = 4.0
        _HorizonGlow      ("Horizon Glow",       Range(0, 1))   = 0.45
        _SkyBend          ("Sky Gradient Bend",  Range(0.2, 3)) = 0.7

        [Header(Stars)]
        _StarBrightness  ("Star Brightness",     Range(0, 1))        = 0.95
        _StarDensity     ("Star Density",        Range(20, 200))     = 70
        _StarSize        ("Star Size",           Range(0.003, 0.08)) = 0.020
        _StarTwinkle     ("Star Twinkle",        Range(0, 2))        = 0.4
        _StarColorShift  ("Star Color Shift",    Range(0, 1))        = 0.25

        [Header(Milky Way)]
        _MilkyWayStrength ("Milky Way Strength", Range(0, 1))    = 0.30
        _MilkyWayAxis     ("Milky Way Axis Y",   Range(-1, 1))   = 0.25

        [Header(Night Tint)]
        _NightTint    ("Night Tint Color",    Color)           = (0.45, 0.50, 1.0, 1)
        _NightTintStr ("Night Tint Strength", Range(0, 0.8))   = 0.25
        _NightBoost   ("Night Sky Boost",     Range(1, 3))     = 1.6
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Background"
            "RenderType"     = "Background"
            "PreviewType"    = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "CozyDarkFantasySkyboxPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _NightTint;

                float _HorizonSharpness;
                float _HorizonGlow;
                float _SkyBend;

                float _StarBrightness;
                float _StarDensity;
                float _StarSize;
                float _StarTwinkle;
                float _StarColorShift;

                float _MilkyWayStrength;
                float _MilkyWayAxis;

                float _NightTintStr;
                float _NightBoost;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldDir   : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldDir   = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            // --- Hash ---
            float H21(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            // --- Night factor from _SkyColor luminance ---
            // Reliable: DayNightSystem sets dark color at night, bright at day.
            float NightFactor()
            {
                float lum = dot(_SkyColor.rgb, half3(0.299, 0.587, 0.114));
                float nf  = 1.0 - smoothstep(0.08, 0.42, lum);
                return nf * nf;
            }

            // --- Stars ---
            half3 StarField(float3 dir, float nf)
            {
                if (dir.y < 0.02 || nf < 0.08) return 0;

                float horizFade = smoothstep(0.02, 0.20, dir.y);

                float2 uv = float2(
                    atan2(dir.z, dir.x) * 0.15915 + 0.5,
                    acos(clamp(dir.y, 0.001, 1.0)) * 0.31831
                );

                float2 grid   = uv * _StarDensity;
                float2 cellId = floor(grid);
                float2 cellUV = frac(grid) - 0.5;

                float2 jitter    = (float2(H21(cellId), H21(cellId + 7.3)) - 0.5) * 0.65;
                float  dist      = length(cellUV - jitter);
                float  mask      = 1.0 - smoothstep(0.0, _StarSize, dist);
                float  bright    = pow(H21(cellId + 13.7), 2.0);
                float  phase     = _Time.y * _StarTwinkle + H21(cellId) * 6.2832;
                float  twinkle   = 0.82 + 0.18 * sin(phase);
                float  starVal   = mask * bright * twinkle * horizFade;

                // Chromatic variation: warm / neutral / cool
                float hue = H21(cellId + 21.3);
                half3 warmTint    = half3(1.0,  0.88, 0.72);
                half3 neutralTint = half3(1.0,  0.97, 0.94);
                half3 coolTint    = half3(0.82, 0.90, 1.0);

                half3 tint = (hue < 0.33)
                    ? lerp(neutralTint, warmTint, (half)(hue * 3.0))
                    : (hue < 0.66)
                        ? neutralTint
                        : lerp(neutralTint, coolTint, (half)((hue - 0.66) * 3.0));

                tint = lerp(neutralTint, tint, (half)_StarColorShift);

                return (half)starVal * tint;
            }

            // --- Milky Way ---
            float MilkyWay(float3 dir)
            {
                if (_MilkyWayStrength < 0.01 || dir.y < 0.0) return 0;

                float3 axis = normalize(float3(0.6, _MilkyWayAxis, 0.8));
                float  proj = dot(dir, axis);
                float  band = exp(-proj * proj * 8.0);

                float2 uv = float2(
                    atan2(dir.z, dir.x) * 0.15915 + 0.5,
                    dir.y * 0.5 + 0.5
                );
                float n = H21(floor(uv * 35.0)) * 0.55
                        + H21(floor(uv * 70.0)) * 0.30
                        + H21(floor(uv * 140.0)) * 0.15;

                return band * n * _MilkyWayStrength * saturate(dir.y * 3.0);
            }

            // --- Atmosphere ---
            half3 Atmosphere(float3 dir)
            {
                float h     = dir.y;
                float absH  = abs(h);
                float upF   = saturate(h);
                float downF = saturate(-h);

                half3 skyPart    = lerp(_HorizonColor.rgb, _SkyColor.rgb,
                                        pow(upF, max(0.01, _SkyBend)));
                half3 groundPart = lerp(_HorizonColor.rgb, _GroundColor.rgb,
                                        pow(downF, max(0.01, _SkyBend * 1.4)));

                half3 base = lerp(groundPart, skyPart, saturate(h + 0.05));

                float glow = pow(max(0.0, 1.0 - absH), _HorizonSharpness);
                base = lerp(base, _HorizonColor.rgb, (half)(glow * _HorizonGlow));

                return base;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.worldDir);
                half3  col = Atmosphere(dir);
                float  nf  = NightFactor();

                // Night: boost + tint
                if (nf > 0.02)
                {
                    col *= (half)lerp(1.0, _NightBoost, nf);
                    col  = lerp(col, col * _NightTint.rgb, (half)(_NightTintStr * nf));
                }

                // Stars + Milky Way (upper hemisphere only)
                if (nf > 0.05 && dir.y > 0.0)
                {
                    col += StarField(dir, nf) * (half)(_StarBrightness * nf);
                    col += half3(0.70, 0.76, 1.0) * (half)(MilkyWay(dir) * nf);
                }

                // Subtle warm tint during day
                float dayness = 1.0 - nf;
                col = lerp(col, col * half3(1.022, 1.004, 0.984), (half)(dayness * 0.07));

                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
