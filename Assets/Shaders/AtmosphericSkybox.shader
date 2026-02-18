Shader "Custom/AtmosphericSkybox"
{
    Properties
    {
        [Header(Sky Colors)]
        _SkyColor("Sky Color", Color) = (0.22, 0.38, 0.72, 1)
        _HorizonColor("Horizon Color", Color) = (0.82, 0.68, 0.50, 1)
        _GroundColor("Ground Color", Color) = (0.06, 0.06, 0.12, 1)

        [Header(Atmospheric Settings)]
        _AtmosphereThickness("Atmosphere Thickness", Range(0, 5)) = 0.6
        _SunSize("Sun Size", Range(0, 0.1)) = 0.04
        _SunSizeConvergence("Sun Size Convergence", Range(1, 10)) = 5

        [Header(Stars)]
        _StarBrightness("Star Brightness", Range(0, 1)) = 0.85
        _StarDensity("Star Density", Range(50, 500)) = 180
        _StarTwinkleSpeed("Star Twinkle Speed", Range(0, 5)) = 0.8

        [Header(Horizon Effects)]
        _HorizonBlend("Horizon Blend", Range(0.1, 5)) = 0.6
        _HorizonGlow("Horizon Glow", Range(0, 2)) = 0.55

        [Header(Night Sky)]
        _NightTintColor("Night Tint Color", Color) = (0.45, 0.50, 1.0, 1)
        _NightTintStr("Night Tint Strength", Range(0, 1)) = 0.30
        _NightBoost("Night Sky Boost", Range(1, 3)) = 1.55

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
                float3 viewDir  : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _SkyColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            fixed4 _CloudColor;
            fixed4 _NightTintColor;

            float _AtmosphereThickness;
            float _SunSize;
            float _SunSizeConvergence;
            float _StarBrightness;
            float _StarDensity;
            float _StarTwinkleSpeed;
            float _HorizonBlend;
            float _HorizonGlow;
            float _CloudCoverage;
            float _CloudSpeed;
            float _NightTintStr;
            float _NightBoost;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir  = normalize(o.worldPos - _WorldSpaceCameraPos);
                return o;
            }

            // --- Hash / Noise ---
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
                    lerp(lerp(hash(p+float3(0,0,0)), hash(p+float3(1,0,0)), f.x),
                         lerp(hash(p+float3(0,1,0)), hash(p+float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash(p+float3(0,0,1)), hash(p+float3(1,0,1)), f.x),
                         lerp(hash(p+float3(0,1,1)), hash(p+float3(1,1,1)), f.x), f.y), f.z);
            }

            // --- Stars ---
            // UV-based grid: more stable and cheaper than 3D voronoi
            float stars(float3 dir)
            {
                if (dir.y < 0.02) return 0.0;

                // Fade stars near horizon
                float horizFade = smoothstep(0.02, 0.18, dir.y);

                // Spherical UV
                float2 uv = float2(
                    atan2(dir.z, dir.x) * 0.15915 + 0.5,
                    acos(clamp(dir.y, 0.001, 1.0)) * 0.31831
                );

                float  density = _StarDensity * 0.5;
                float2 grid    = uv * density;
                float2 cellId  = floor(grid);
                float2 cellUV  = frac(grid) - 0.5;

                float2 jitter    = (float2(hash2(cellId), hash2(cellId + 7.3)) - 0.5) * 0.65;
                float  dist      = length(cellUV - jitter);
                float  starSize  = 0.018 + hash2(cellId + 3.1) * 0.012;
                float  mask      = 1.0 - smoothstep(0.0, starSize, dist);

                // Power distribution: most stars dim
                float bright   = pow(hash2(cellId + 13.7), 2.2);
                float phase    = _Time.y * _StarTwinkleSpeed + hash2(cellId) * 6.2832;
                float twinkle  = 0.80 + 0.20 * sin(phase);

                // Warm/cool chromatic variation
                float hue = hash2(cellId + 21.3);
                fixed3 tint = (hue < 0.4)
                    ? lerp(fixed3(1.0, 0.97, 0.92), fixed3(1.0, 0.85, 0.68), hue * 2.5)
                    : lerp(fixed3(1.0, 0.97, 0.92), fixed3(0.82, 0.90, 1.00), (hue - 0.4) * 1.67);

                return mask * bright * twinkle * horizFade * _StarBrightness;
            }

            // Separate tint for star color
            fixed3 starTint(float3 dir)
            {
                float2 uv = float2(
                    atan2(dir.z, dir.x) * 0.15915 + 0.5,
                    acos(clamp(dir.y, 0.001, 1.0)) * 0.31831
                );
                float2 cellId = floor(uv * _StarDensity * 0.5);
                float  hue    = hash2(cellId + 21.3);
                return (hue < 0.4)
                    ? lerp(fixed3(1.0, 0.97, 0.92), fixed3(1.0, 0.85, 0.68), hue * 2.5)
                    : lerp(fixed3(1.0, 0.97, 0.92), fixed3(0.82, 0.90, 1.00), (hue - 0.4) * 1.67);
            }

            // --- Clouds ---
            float clouds(float3 dir, float time)
            {
                float3 cloudPos = dir * 50 + float3(time * _CloudSpeed, 0, time * _CloudSpeed * 0.5);
                float cloudNoise  = noise(cloudPos * 0.5) * 0.50;
                      cloudNoise += noise(cloudPos * 1.0) * 0.25;
                      cloudNoise += noise(cloudPos * 2.0) * 0.125;
                float cloudMask = smoothstep(1.0 - _CloudCoverage, 1.0, cloudNoise + 0.25);
                return cloudMask * saturate(dir.y * 3.0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDir);

                // --- Sky gradient ---
                float upFactor   = saturate(viewDir.y);
                float downFactor = saturate(-viewDir.y);
                float absY       = abs(viewDir.y);

                // Sky: horizon -> zenith with bend
                fixed3 skyCol    = lerp(_HorizonColor.rgb, _SkyColor.rgb,
                                        pow(upFactor, _HorizonBlend));
                // Ground: horizon -> dark ground
                fixed3 groundCol = lerp(_HorizonColor.rgb, _GroundColor.rgb,
                                        pow(downFactor, _HorizonBlend * 1.5));

                fixed3 finalColor = lerp(groundCol, skyCol, saturate(viewDir.y + 0.06));

                // --- Horizon glow (wider, softer) ---
                float hGlow = pow(1.0 - absY, 4.0) * _HorizonGlow;
                finalColor += hGlow * _HorizonColor.rgb * _LightColor0.rgb * 0.8;

                // --- Atmosphere thickness (subtle) ---
                finalColor = lerp(finalColor, _HorizonColor.rgb,
                                  (1.0 - absY) * _AtmosphereThickness * 0.06);

                // --- Night factor from sky luminance ---
                // Works with DayNightSystem: night _SkyColor is dark (lum < 0.12)
                float skyLum     = dot(_SkyColor.rgb, float3(0.299, 0.587, 0.114));
                float nightFactor = 1.0 - smoothstep(0.08, 0.45, skyLum);
                nightFactor       = nightFactor * nightFactor;

                // --- Night: boost darkness + violet-blue tint ---
                if (nightFactor > 0.02)
                {
                    finalColor *= lerp(1.0, _NightBoost, nightFactor);
                    finalColor  = lerp(finalColor,
                                       finalColor * _NightTintColor.rgb,
                                       nightFactor * _NightTintStr);
                }

                // --- Sun disk ---
                float sunDot  = dot(viewDir, _WorldSpaceLightPos0.xyz);
                float sunDisk = pow(saturate(sunDot), 50.0 / _SunSize);
                finalColor += sunDisk * _LightColor0.rgb * (1.0 - nightFactor);

                // --- Stars ---
                if (viewDir.y > 0.02 && nightFactor > 0.05)
                {
                    float  starVal = stars(viewDir);
                    fixed3 sTint   = starTint(viewDir);
                    finalColor += starVal * sTint * nightFactor;

                    // Milky Way band (simple)
                    float3 mwAxis = normalize(float3(0.6, 0.25, 0.8));
                    float  proj   = dot(viewDir, mwAxis);
                    float  band   = exp(-proj * proj * 8.0);
                    float  mwN    = noise(viewDir * 35.0) * 0.6
                                  + noise(viewDir * 70.0) * 0.3
                                  + noise(viewDir * 140.0) * 0.1;
                    float  mw     = band * mwN * 0.28 * saturate(viewDir.y * 3.0);
                    finalColor += fixed3(0.68, 0.74, 1.0) * mw * nightFactor;
                }

                // --- Clouds (day only) ---
                float cloudVis = saturate(_LightColor0.a) * (1.0 - nightFactor);
                if (viewDir.y > 0 && _CloudCoverage > 0.01 && cloudVis > 0.01)
                {
                    float cloudMask = clouds(viewDir, _Time.y);
                    // Soft cloud blend - slightly lit by horizon
                    fixed3 cloudLitColor = _CloudColor.rgb * lerp(1.0, _HorizonColor.rgb, 0.15);
                    finalColor = lerp(finalColor, cloudLitColor, cloudMask * cloudVis * 0.7);
                }

                // Warm tint for day (cozy feel)
                finalColor = lerp(finalColor,
                                  finalColor * fixed3(1.02, 1.004, 0.985),
                                  (1.0 - nightFactor) * 0.06);

                return fixed4(saturate(finalColor), 1.0);
            }
            ENDCG
        }
    }

    Fallback "Skybox/Procedural"
}
