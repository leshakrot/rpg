// ═══════════════════════════════════════════════════════════════════════
//  ProceduralSkybox — Cozy Lowpoly Dark Fantasy (PC / High Quality)
//  ✅ Universal Render Pipeline (URP)
//  Двухслойное небо, Млечный путь, хроматические звёзды.
// ═══════════════════════════════════════════════════════════════════════
Shader "Custom/ProceduralSkybox"
{
    Properties
    {
        [Header(Sky Colors)]
        _SkyColor       ("Sky Color (Zenith)",  Color) = (0.22, 0.38, 0.72, 1.0)
        _MidSkyColor    ("Mid Sky Color",       Color) = (0.35, 0.52, 0.82, 1.0)
        _HorizonColor   ("Horizon Color",       Color) = (0.82, 0.72, 0.55, 1.0)
        _GroundColor    ("Ground Color",        Color) = (0.10, 0.10, 0.15, 1.0)

        [Header(Atmosphere)]
        _HorizonBlend    ("Horizon Blend Width",    Range(0.5, 8))    = 3.0
        _AtmospherePower ("Atmosphere Curve",       Range(0.2, 3))    = 0.75
        _HorizonGlow     ("Horizon Glow Amount",    Range(0.0, 1.0))  = 0.35
        _WarmthFactor    ("Warmth (day+ / night-)", Range(-0.5, 0.5)) = 0.08

        [Header(Stars — Night Only)]
        _StarBrightness  ("Star Brightness",     Range(0, 1))        = 0.90
        _StarDensity     ("Star Density",        Range(30, 200))     = 75
        _StarSize        ("Star Size",           Range(0.005, 0.1))  = 0.022
        _StarTwinkle     ("Star Twinkle Speed",  Range(0, 2))        = 0.35
        _StarColorShift  ("Star Color Variation",Range(0, 1))        = 0.30

        [Header(Night Sky)]
        _NightSkyIntensity  ("Night Intensity",     Range(0.5, 2.0)) = 1.10
        _NightTint          ("Night Tint Color",    Color)           = (0.52, 0.58, 1.0, 1.0)
        _NightTintStrength  ("Night Tint Strength", Range(0.0, 0.6)) = 0.20

        [Header(Milky Way)]
        _MilkyWayIntensity ("Milky Way Intensity", Range(0, 1))    = 0.28
        _MilkyWayRotation  ("Milky Way Rotation",  Range(0, 6.28)) = 0.5
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
            Name "SkyboxPCPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyColor;
                half4 _MidSkyColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _NightTint;

                float _HorizonBlend;
                float _AtmospherePower;
                float _HorizonGlow;
                float _WarmthFactor;

                float _StarBrightness;
                float _StarDensity;
                float _StarSize;
                float _StarTwinkle;
                float _StarColorShift;

                float _NightSkyIntensity;
                float _NightTintStrength;

                float _MilkyWayIntensity;
                float _MilkyWayRotation;
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

            // ── Хеш-функции ───────────────────────────────────────────
            float Hash21(float2 p)
            {
                p  = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float Hash31(float3 p)
            {
                p  = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // ── Утилиты ───────────────────────────────────────────────
            float  Lum(half3 c)  { return dot(c, half3(0.299, 0.587, 0.114)); }
            half3  Desat(half3 c, float t)
            {
                half l = (half)Lum(c);
                return lerp(c, half3(l, l, l), t);
            }

            // ── Температура ───────────────────────────────────────────
            half3 ApplyWarmth(half3 c, float w)
            {
                if (w > 0.0)
                {
                    c.r = min(1.0h, c.r * (1.0 + w * 0.28));
                    c.g = min(1.0h, c.g * (1.0 + w * 0.10));
                    c.b = max(0.0h, c.b * (1.0 - w * 0.12));
                }
                else
                {
                    float wf = -w;
                    c.b = min(1.0h, c.b * (1.0 + wf * 0.30));
                    c.g = max(0.0h, c.g * (1.0 - wf * 0.06));
                    c.r = max(0.0h, c.r * (1.0 - wf * 0.18));
                }
                return c;
            }

            // ── Млечный путь ──────────────────────────────────────────
            float MilkyWay(float3 dir)
            {
                float s  = sin(_MilkyWayRotation);
                float cc = cos(_MilkyWayRotation);
                float3 rd = float3(
                    dir.x * cc - dir.z * s,
                    dir.y,
                    dir.x * s  + dir.z * cc
                );

                float band = exp(-abs(rd.y) * 6.0) * 0.5;

                float2 uv = float2(atan2(rd.z, rd.x) * 0.15915, rd.y);
                float n = Hash21(floor(uv * 40.0)) * 0.6
                        + Hash21(floor(uv * 80.0)) * 0.3
                        + Hash21(floor(uv * 160.0)) * 0.1;

                return band * n * _MilkyWayIntensity;
            }

            // ── Звёзды ────────────────────────────────────────────────
            half3 Stars(float3 dir, float nightFactor)
            {
                if (dir.y < 0.03 || nightFactor < 0.15) return 0;

                float2 uv = float2(
                    atan2(dir.z, dir.x) * 0.15915 + 0.5,
                    acos(clamp(dir.y, -1.0, 1.0)) * 0.31831
                );

                float2 grid   = uv * _StarDensity;
                float2 cellId = floor(grid);
                float2 cellUV = frac(grid) - 0.5;

                float2 offset    = (float2(Hash21(cellId), Hash21(cellId + 5.7)) - 0.5) * 0.65;
                float  dist      = length(cellUV - offset);
                float  mask      = 1.0 - smoothstep(0.0, _StarSize, dist);
                float  brightness = pow(Hash21(cellId + 13.7), 2.2);
                float  phase     = _Time.y * _StarTwinkle + Hash21(cellId) * 6.2832;
                float  twinkle   = 0.85 + 0.15 * sin(phase);

                float hue = Hash21(cellId + 21.3);
                half3 tint = lerp(
                    half3(1.0, 0.97, 0.88),
                    lerp(
                        half3(0.85, 0.90, 1.00),
                        half3(1.00, 0.85, 0.90),
                        (half)step(0.5, hue)
                    ),
                    (half)(_StarColorShift * saturate((hue - 0.3) * 3.0))
                );

                return (half)(mask * brightness * twinkle) * tint;
            }

            // ── Атмосфера ─────────────────────────────────────────────
            half3 Atmosphere(float3 dir)
            {
                float h     = dir.y;
                float absH  = abs(h);
                float upF   = saturate(h);
                float downF = saturate(-h);

                float midBlend = saturate(1.0 - pow(upF, _AtmospherePower * 0.5));
                half3 skyTop   = lerp(_MidSkyColor.rgb, _SkyColor.rgb, pow(upF, _AtmospherePower));
                skyTop         = lerp(skyTop, _MidSkyColor.rgb, (half)(midBlend * 0.3));

                half3 groundCol = lerp(_HorizonColor.rgb, _GroundColor.rgb,
                                       pow(downF, _AtmospherePower * 1.3));

                half3 base = lerp(groundCol, skyTop, saturate(h + 0.08));

                float glow = pow(max(0.0, 1.0 - absH), _HorizonBlend);
                base = lerp(base, _HorizonColor.rgb, (half)(glow * _HorizonGlow));

                base = ApplyWarmth(base, _WarmthFactor);
                return base;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.worldDir);

                half3 atmo = Atmosphere(dir);

                float lum         = Lum(atmo);
                float nightFactor = saturate(1.0 - lum * 2.0);
                nightFactor       = nightFactor * nightFactor;

                half3 col = atmo;

                if (nightFactor > 0.05)
                {
                    col *= (half)lerp(1.0, _NightSkyIntensity, nightFactor);
                    col  = lerp(col, col * _NightTint.rgb, (half)(_NightTintStrength * nightFactor));
                }

                if (nightFactor > 0.15 && dir.y > 0.0)
                {
                    half3 starContrib = Stars(dir, nightFactor);
                    col += starContrib * (half)(_StarBrightness * nightFactor);

                    float mw = MilkyWay(dir);
                    col += half3(0.72, 0.78, 1.0) * (half)(mw * nightFactor);
                }

                // Финальный тёплый тинт
                col = lerp(col, col * half3(1.025, 1.005, 0.982), 0.05h);

                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
