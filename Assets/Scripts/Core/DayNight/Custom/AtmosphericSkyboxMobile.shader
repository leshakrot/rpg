// ═══════════════════════════════════════════════════════════════════════
//  AtmosphericSkyboxMobile — Cozy Lowpoly Dark Fantasy
//  ✅ Universal Render Pipeline (URP)
//  Оптимизирован для мобильных устройств.
// ═══════════════════════════════════════════════════════════════════════
Shader "Custom/AtmosphericSkyboxMobile"
{
    Properties
    {
        [Header(Sky Colors)]
        _SkyColor       ("Sky Color (Zenith)",    Color) = (0.32, 0.52, 0.82, 1.0)
        _HorizonColor   ("Horizon Color",         Color) = (0.82, 0.72, 0.55, 1.0)
        _GroundColor    ("Ground Color",          Color) = (0.10, 0.10, 0.15, 1.0)

        [Header(Atmosphere)]
        _HorizonBlend    ("Horizon Blend Width",    Range(0.5, 8))    = 2.5
        _AtmospherePower ("Atmosphere Curve",       Range(0.2, 3))    = 0.85
        _HorizonGlow     ("Horizon Glow Amount",    Range(0.0, 0.8))  = 0.28
        _WarmthFactor    ("Warmth (day+ / night-)", Range(-0.5, 0.5)) = 0.08

        [Header(Stars — Night Only)]
        _StarBrightness ("Star Brightness",    Range(0, 1))        = 0.90
        _StarDensity    ("Star Density",       Range(20, 150))     = 55
        _StarSize       ("Star Size",          Range(0.005, 0.07)) = 0.018
        _StarTwinkle    ("Star Twinkle Speed", Range(0, 1.5))      = 0.25

        [Header(Night Sky)]
        _NightSkyIntensity    ("Night Intensity",     Range(0.8, 2.0)) = 1.15
        _NightSkyDesaturation ("Night Desaturation",  Range(0.0, 0.8)) = 0.25
        _NightTint            ("Night Tint Color",    Color)           = (0.55, 0.60, 1.0, 1.0)
        _NightTintStrength    ("Night Tint Strength", Range(0.0, 0.5)) = 0.18
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
            Name "SkyboxMobilePass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyColor;
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

                float _NightSkyIntensity;
                float _NightSkyDesaturation;
                float _NightTintStrength;
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
                // Направление в мировом пространстве
                OUT.worldDir   = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            // ── Хеш ──────────────────────────────────────────────────
            float Hash2(float2 p)
            {
                p  = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            // ── Утилиты ───────────────────────────────────────────────
            float Lum(half3 c) { return dot(c, half3(0.299, 0.587, 0.114)); }

            half3 Desat(half3 c, float t)
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
                    c.g = min(1.0h, c.g * (1.0 + w * 0.12));
                    c.b = max(0.0h, c.b * (1.0 - w * 0.10));
                }
                else
                {
                    float wf = -w;
                    c.b = min(1.0h, c.b * (1.0 + wf * 0.28));
                    c.g = max(0.0h, c.g * (1.0 - wf * 0.08));
                    c.r = max(0.0h, c.r * (1.0 - wf * 0.15));
                }
                return c;
            }

            // ── Звёзды ────────────────────────────────────────────────
            float Stars(float3 dir, float nightFactor)
            {
                if (dir.y < 0.04 || nightFactor < 0.25) return 0.0;

                float2 uv = float2(
                    atan2(dir.z, dir.x) * 0.15915 + 0.5,
                    acos(clamp(dir.y, -1.0, 1.0)) * 0.31831
                );

                float2 grid   = uv * _StarDensity;
                float2 cellId = floor(grid);
                float2 cellUV = frac(grid) - 0.5;

                float2 offset    = (float2(Hash2(cellId), Hash2(cellId + 7.3)) - 0.5) * 0.65;
                float  dist      = length(cellUV - offset);
                float  mask      = 1.0 - smoothstep(0.0, _StarSize, dist);
                float  brightness = pow(Hash2(cellId + 13.7), 1.8);
                float  phase     = _Time.y * _StarTwinkle + Hash2(cellId) * 6.2832;
                float  twinkle   = 0.88 + 0.12 * sin(phase);

                return mask * brightness * twinkle;
            }

            // ── Атмосфера ─────────────────────────────────────────────
            half3 Atmosphere(float3 dir)
            {
                float h     = dir.y;
                float absH  = abs(h);
                float upF   = saturate(h);
                float downF = saturate(-h);

                half3 skyPart    = lerp(_HorizonColor.rgb, _SkyColor.rgb,
                                        pow(upF,    _AtmospherePower));
                half3 groundPart = lerp(_HorizonColor.rgb, _GroundColor.rgb,
                                        pow(downF,  _AtmospherePower * 1.2));

                half3 base = lerp(groundPart, skyPart, saturate(h + 0.08));

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
                float nightFactor = saturate(1.0 - lum * 2.2);
                nightFactor       = nightFactor * nightFactor;

                half3 col = atmo;

                // Ночные улучшения
                if (nightFactor > 0.05)
                {
                    col *= (half)lerp(1.0, _NightSkyIntensity, nightFactor);
                    col  = Desat(col, _NightSkyDesaturation * nightFactor);
                    col  = lerp(col, col * _NightTint.rgb, (half)(_NightTintStrength * nightFactor));
                }

                // Звёзды
                if (nightFactor > 0.2 && dir.y > 0.0)
                {
                    float star    = Stars(dir, nightFactor);
                    half3 starCol = half3(1.0, 0.97, 0.90);
                    col += (half)(star * _StarBrightness * nightFactor) * starCol;
                }

                // Лёгкий тёплый тинт
                col = lerp(col, col * half3(1.025, 1.005, 0.985), 0.06h);

                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
