Shader "Custom/AtmosphericSkybox"
{
    Properties
    {
        [Header(Sky Colors)]
        _SkyColor("Sky Color", Color) = (0.22, 0.38, 0.72, 1)
        _MidSkyColor("Mid Sky Color", Color) = (0.38, 0.52, 0.82, 1)
        _HorizonColor("Horizon Color", Color) = (0.82, 0.68, 0.50, 1)
        _GroundColor("Ground Color", Color) = (0.06, 0.06, 0.12, 1)

        [Header(Atmospheric Settings)]
        _AtmosphereThickness("Atmosphere Thickness", Range(0, 5)) = 0.6
        _AtmospherePower("Atmosphere Power", Range(0.25, 2)) = 0.8
        _AtmosphereIntensity("Atmosphere Intensity", Range(0.2, 2)) = 1.0
        _HorizonBlend("Horizon Blend", Range(0.1, 5)) = 1.8
        _HorizonGlow("Horizon Glow", Range(0, 2)) = 0.55
        _DuskGlow("Dusk Glow", Range(0, 2)) = 1.35
        _WarmthFactor("Warmth Factor", Range(0, 1)) = 0
        _NightFactor("Night Factor", Range(0, 1)) = 0
        _DawnFactor("Dawn Factor", Range(0, 1)) = 0
        _DuskFactor("Dusk Factor", Range(0, 1)) = 0
        _DayIntensity("Day Intensity", Range(0, 1)) = 1

        [Header(Sun)]
        _SunSize("Sun Size", Range(0, 0.1)) = 0.04
        _SunSizeConvergence("Sun Size Convergence", Range(1, 10)) = 5
        _SunDirection("Sun Direction", Vector) = (0, 1, 0, 0)
        _CelestialGlow("Celestial Glow", Range(0, 3)) = 1.0

        [Header(Moon)]
        _MoonDirection("Moon Direction", Vector) = (0, 1, 0, 0)
        _MoonColor("Moon Color", Color) = (0.68, 0.76, 1.0, 1)

        [Header(Stars)]
        _StarBrightness("Star Brightness", Range(0, 1.5)) = 0.92
        _StarDensity("Star Density", Range(30, 500)) = 180
        _StarTwinkleSpeed("Star Twinkle Speed", Range(0, 5)) = 0.8
        _StarSize("Star Size", Range(0.005, 0.04)) = 0.018

        [Header(Night Sky)]
        _NightTintColor("Night Tint Color", Color) = (0.35, 0.42, 1.0, 1)
        _NightTintStr("Night Tint Strength", Range(0, 1)) = 0.30
        _NightBoost("Night Sky Boost", Range(1, 3)) = 1.55
        _NightSkyIntensity("Night Sky Intensity", Range(0, 2)) = 1.0

        [Header(Milky Way)]
        _MilkyWayIntensity("Milky Way Intensity", Range(0, 2)) = 0.42
        _MilkyWayRotation("Milky Way Rotation", Range(0, 6.28318)) = 0.72

        [Header(Cloud Settings)]
        _CloudCoverage("Cloud Coverage", Range(0, 1)) = 0.35
        _CloudSpeed("Cloud Speed", Range(0, 2)) = 0.05
        _CloudColor("Cloud Color", Color) = (0.95, 0.92, 0.88, 1)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _SkyColor;
            fixed4 _MidSkyColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            fixed4 _CloudColor;
            fixed4 _NightTintColor;
            fixed4 _MoonColor;

            float _AtmosphereThickness;
            float _AtmospherePower;
            float _AtmosphereIntensity;
            float _SunSize;
            float _SunSizeConvergence;
            float _StarBrightness;
            float _StarDensity;
            float _StarTwinkleSpeed;
            float _StarSize;
            float _HorizonBlend;
            float _HorizonGlow;
            float _DuskGlow;
            float _WarmthFactor;
            float _NightFactor;
            float _DawnFactor;
            float _DuskFactor;
            float _DayIntensity;
            float _CloudCoverage;
            float _CloudSpeed;
            float _NightTintStr;
            float _NightBoost;
            float _NightSkyIntensity;
            float _MilkyWayIntensity;
            float _MilkyWayRotation;
            float4 _SunDirection;
            float4 _MoonDirection;
            float _CelestialGlow;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.position = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(o.worldPos - _WorldSpaceCameraPos);

                return o;
            }

            // ------------------------------------------------------------
            // Cheap deterministic hashes/noise.
            // No textures, no extra passes, no extra cameras.
            // ------------------------------------------------------------

            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float hash2(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float noise(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(
                        lerp(hash(p), hash(p + float3(1,0,0)), f.x),
                        lerp(hash(p + float3(0,1,0)), hash(p + float3(1,1,0)), f.x),
                        f.y
                    ),
                    lerp(
                        lerp(hash(p + float3(0,0,1)), hash(p + float3(1,0,1)), f.x),
                        lerp(hash(p + float3(0,1,1)), hash(p + float3(1,1,1)), f.x),
                        f.y
                    ),
                    f.z
                );
            }

            float2 sphereUV(float3 dir)
            {
                return float2(
                    atan2(dir.z, dir.x) * 0.15915494 + 0.5,
                    acos(clamp(dir.y, -1.0, 1.0)) * 0.31830989
                );
            }

            // ------------------------------------------------------------
            // Stars
            // Stable spherical grid, with a small number of operations.
            // High quality comes from layering shape/color rather than
            // multiplying the number of procedural samples.
            // ------------------------------------------------------------

            float stars(float3 dir)
            {
                if (dir.y < 0.015)
                    return 0.0;

                float horizonFade = smoothstep(0.015, 0.24, dir.y);
                float2 uv = sphereUV(dir);

                float density = _StarDensity * 0.5;
                float2 grid = uv * density;
                float2 cell = floor(grid);
                float2 local = frac(grid) - 0.5;

                float2 jitter = (float2(
                    hash2(cell + 1.7),
                    hash2(cell + 8.9)
                ) - 0.5) * 0.70;

                float dist = length(local - jitter);

                float sizeVariation = lerp(0.72, 1.35, hash2(cell + 3.1));
                float size = _StarSize * sizeVariation;

                float core = 1.0 - smoothstep(0.0, size, dist);
                float halo = 1.0 - smoothstep(size * 0.15, size * 3.5, dist);

                float brightness = pow(hash2(cell + 13.7), 2.15);
                brightness = lerp(0.10, 1.0, brightness);

                float phase = _Time.y * _StarTwinkleSpeed
                            + hash2(cell + 19.1) * 6.28318;

                float twinkle = 0.84 + 0.16 * sin(phase);
                float rarePulse = smoothstep(0.94, 1.0, hash2(cell + 42.2))
                                * (0.5 + 0.5 * sin(phase * 0.45));

                return (core + halo * 0.12)
                     * brightness
                     * twinkle
                     * (1.0 + rarePulse * 0.7)
                     * horizonFade
                     * _StarBrightness;
            }

            fixed3 starTint(float3 dir)
            {
                float2 cell = floor(sphereUV(dir) * (_StarDensity * 0.5));
                float hue = hash2(cell + 21.3);

                fixed3 warm = lerp(
                    fixed3(1.0, 0.98, 0.94),
                    fixed3(1.0, 0.82, 0.62),
                    hue * 2.5
                );

                fixed3 cool = lerp(
                    fixed3(1.0, 0.98, 0.94),
                    fixed3(0.72, 0.84, 1.0),
                    (hue - 0.4) * 1.67
                );

                return hue < 0.4 ? warm : cool;
            }

            // ------------------------------------------------------------
            // Milky Way
            // Several cheap noise frequencies give a broken dusty band
            // instead of a flat glowing stripe.
            // ------------------------------------------------------------

            fixed3 milkyWay(float3 dir)
            {
                float a = _MilkyWayRotation;
                float ca = cos(a);
                float sa = sin(a);

                float3 axis = normalize(float3(
                    ca * 0.62 - sa * 0.28,
                    0.24,
                    sa * 0.62 + ca * 0.28
                ));

                float bandCoord = dot(dir, axis);

                float band = exp(-bandCoord * bandCoord * 13.0);
                band *= smoothstep(-0.08, 0.18, dir.y);

                float n1 = noise(dir * 28.0);
                float n2 = noise(dir * 57.0);
                float n3 = noise(dir * 112.0);

                float dust = saturate(
                    n1 * 0.55 +
                    n2 * 0.30 +
                    n3 * 0.15
                );

                // Dark dust lanes break up the luminous band.
                float dustLane = noise(dir * 19.0 + 7.3);
                float structure = saturate(dust * 1.45 - dustLane * 0.48);

                float glow = band * structure
                           * _MilkyWayIntensity
                           * _NightSkyIntensity;

                return fixed3(0.56, 0.67, 1.0) * glow;
            }

            // ------------------------------------------------------------
            // Celestial bodies
            // Directions are supplied by DayNightSystem, so this shader
            // contains no time/gameplay logic.
            // ------------------------------------------------------------

            fixed3 celestialBodies(
                float3 dir,
                float nightFactor,
                float duskFactor
            )
            {
                fixed3 result = 0;

                float3 sunDir = normalize(_SunDirection.xyz);
                float3 moonDir = normalize(_MoonDirection.xyz);

                float sunDot = saturate(dot(dir, sunDir));
                float moonDot = saturate(dot(dir, moonDir));

                float sunRadius = max(_SunSize, 0.006);

                float sunCore = smoothstep(
                    1.0 - sunRadius * 18.0,
                    1.0 - sunRadius * 4.0,
                    sunDot
                );

                float sunHalo = pow(sunDot, 80.0 / max(_SunSizeConvergence, 1.0));
                sunHalo *= 0.38 + duskFactor * 2.8;

                fixed3 sunWarm = lerp(
                    fixed3(1.0, 0.94, 0.78),
                    fixed3(1.0, 0.56, 0.20),
                    duskFactor
                );

                float sunVisibility = 1.0 - nightFactor;

                result += sunWarm
                       * (sunCore * 2.15 + sunHalo * 0.72)
                       * _CelestialGlow
                       * sunVisibility;

                // Moon: soft disc + broad blue halo.
                float moonCore = smoothstep(0.9982, 0.99975, moonDot);
                float moonHalo = pow(moonDot, 145.0) * 0.75;

                result += _MoonColor.rgb
                       * (moonCore * 0.72 + moonHalo * 0.55)
                       * _CelestialGlow
                       * nightFactor;

                return result;
            }

            // ------------------------------------------------------------
            // Clouds
            // Kept from the original shader, but made softer and warmer
            // near sunrise/sunset.
            // ------------------------------------------------------------

            float clouds(float3 dir, float time)
            {
                float3 cloudPos = dir * 50.0
                                + float3(
                                    time * _CloudSpeed,
                                    0,
                                    time * _CloudSpeed * 0.5
                                  );

                float cloudNoise = noise(cloudPos * 0.5) * 0.50;
                cloudNoise += noise(cloudPos * 1.0) * 0.25;
                cloudNoise += noise(cloudPos * 2.0) * 0.125;

                float coverage = smoothstep(
                    1.0 - _CloudCoverage,
                    1.0,
                    cloudNoise + 0.25
                );

                return coverage * saturate(dir.y * 3.0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDir);

                float up = saturate(viewDir.y);
                float down = saturate(-viewDir.y);
                float absY = abs(viewDir.y);

                // --------------------------------------------------------
                // Deep multi-layer sky gradient
                // --------------------------------------------------------

                float skyBlend = pow(up, max(_HorizonBlend, 0.1));

                fixed3 upperSky = lerp(
                    _MidSkyColor.rgb,
                    _SkyColor.rgb,
                    smoothstep(0.15, 0.82, skyBlend)
                );

                fixed3 skyCol = lerp(
                    _HorizonColor.rgb,
                    upperSky,
                    skyBlend
                );

                fixed3 groundCol = lerp(
                    _HorizonColor.rgb,
                    _GroundColor.rgb,
                    pow(down, max(_HorizonBlend * 1.35, 0.1))
                );

                fixed3 finalColor = lerp(
                    groundCol,
                    skyCol,
                    saturate(viewDir.y + 0.055)
                );

                // --------------------------------------------------------
                // Atmospheric depth
                // --------------------------------------------------------

                float horizon = 1.0 - absY;
                float softHorizon = pow(saturate(horizon), 2.6);

                float atmosphere = softHorizon
                                 * _AtmosphereThickness
                                 * 0.075
                                 * _AtmosphereIntensity;

                finalColor = lerp(
                    finalColor,
                    _HorizonColor.rgb,
                    saturate(atmosphere)
                );

                float horizonGlow = pow(saturate(horizon), 4.0)
                                  * _HorizonGlow
                                  * _AtmosphereIntensity;

                finalColor += _HorizonColor.rgb
                            * horizonGlow
                            * 0.72;

                // --------------------------------------------------------
                // Night factor is still derived from the visual colors.
                // No game time is calculated here.
                // --------------------------------------------------------

                // DayNightSystem supplies the actual visual state.
                // This avoids guessing "night" from color luminance, which
                // made dark sunsets look like daytime and vice versa.
                float nightFactor = saturate(_NightFactor);
                float dawnFactor = saturate(_DawnFactor);
                float duskFactor = saturate(_DuskFactor) * _DuskGlow;
                float warmth = saturate(_WarmthFactor);

                float twilightFactor = saturate(max(dawnFactor, duskFactor));

                fixed3 goldenLight = fixed3(1.0, 0.54, 0.20);
                fixed3 warmSky = lerp(
                    finalColor,
                    finalColor * lerp(
                        fixed3(1.0, 0.90, 0.72),
                        goldenLight,
                        0.38
                    ),
                    warmth * 0.28
                );

                finalColor = lerp(finalColor, warmSky, warmth);

                // Deep blue-violet night.
                if (nightFactor > 0.005)
                {
                    fixed3 nightTinted = finalColor * _NightTintColor.rgb;

                    finalColor = lerp(
                        finalColor,
                        nightTinted,
                        nightFactor * _NightTintStr
                    );

                    // Night must actually lose luminance. The boost is
                    // reserved for preserving blue/violet colour, not
                    // making the whole sky brighter.
                    float nightLift = lerp(
                        1.0,
                        0.42,
                        nightFactor
                    );

                    finalColor *= nightLift;

                    // Cool nocturnal veil.
                    finalColor = lerp(
                        finalColor,
                        finalColor * fixed3(0.42, 0.50, 0.82),
                        nightFactor * 0.48
                    );
                }

                // A clear, hard daytime sky: high-frequency brightness
                // comes from the sky itself, while scene lighting is handled
                // by the directional sun.
                finalColor *= lerp(
                    0.92,
                    1.16,
                    _DayIntensity
                );

                // Dawn is intentionally bright-but-muted: clear sky with a
                // soft overcast veil rather than a saturated orange blast.
                float morningVeil = dawnFactor * (1.0 - duskFactor);
                finalColor = lerp(
                    finalColor,
                    finalColor * fixed3(0.78, 0.82, 0.88),
                    morningVeil * 0.28
                );

                // --------------------------------------------------------
                // Sun + moon
                // --------------------------------------------------------

                finalColor += celestialBodies(
                    viewDir,
                    nightFactor,
                    duskFactor
                );

                // --------------------------------------------------------
                // Stars
                // --------------------------------------------------------

                if (viewDir.y > 0.015 && nightFactor > 0.025)
                {
                    float starValue = stars(viewDir);

                    // Stars become much more visible at deep night than
                    // during blue hour.
                    float starVisibility = smoothstep(
                        0.025,
                        0.78,
                        nightFactor
                    );

                    fixed3 starColor = starTint(viewDir);

                    finalColor += starValue
                                * starColor
                                * starVisibility
                                * _NightSkyIntensity;

                    finalColor += milkyWay(viewDir)
                                * starVisibility;
                }

                // --------------------------------------------------------
                // Soft twilight atmospheric glow.
                // --------------------------------------------------------

                if (duskFactor > 0.001)
                {
                    float twilightHorizon = pow(
                        saturate(1.0 - absY),
                        3.2
                    );

                    fixed3 duskColor = lerp(
                        fixed3(1.0, 0.28, 0.10),
                        fixed3(0.34, 0.20, 0.60),
                        nightFactor
                    );

                    finalColor += duskColor
                                * twilightHorizon
                                * duskFactor
                                * 0.34;
                }

                // --------------------------------------------------------
                // Clouds: original feature preserved.
                // --------------------------------------------------------

                float cloudVis = saturate(_LightColor0.a)
                               * (1.0 - nightFactor * 0.92);

                if (viewDir.y > 0.0
                    && _CloudCoverage > 0.01
                    && cloudVis > 0.01)
                {
                    float cloudMask = clouds(viewDir, _Time.y);

                    fixed3 dayCloud = _CloudColor.rgb;

                    fixed3 sunsetCloud = lerp(
                        dayCloud,
                        fixed3(1.0, 0.42, 0.20),
                        warmth * 0.52
                    );

                    fixed3 cloudLitColor = lerp(
                        sunsetCloud,
                        _HorizonColor.rgb,
                        0.14
                    );

                    finalColor = lerp(
                        finalColor,
                        cloudLitColor,
                        cloudMask * cloudVis * 0.68
                    );
                }

                // Very subtle high-altitude cool lift. Helps the zenith
                // retain depth instead of becoming a flat color.
                float zenith = smoothstep(0.25, 1.0, up);
                finalColor += fixed3(0.015, 0.025, 0.06)
                            * zenith
                            * (1.0 - warmth * 0.7)
                            * _AtmosphereIntensity;

                return fixed4(
                    max(saturate(finalColor), 0.0),
                    1.0
                );
            }
            ENDCG
        }
    }

    Fallback "Skybox/Procedural"
}
