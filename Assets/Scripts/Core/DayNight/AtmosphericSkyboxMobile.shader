Shader "Custom/AtmosphericSkyboxMobile"
{
    Properties
    {
        [Header(Sky Colors)]
        _SkyColor ("Sky Color", Color) = (0.4, 0.6, 0.9, 1.0)
        _HorizonColor ("Horizon Color", Color) = (0.9, 0.85, 0.8, 1.0)
        _GroundColor ("Ground Color", Color) = (0.3, 0.25, 0.2, 1.0)
        
        [Header(Atmospheric Settings)]
        _HorizonBlend ("Horizon Blend", Range(0.1, 5)) = 1.5
        _AtmospherePower ("Atmosphere Power", Range(0.5, 3)) = 1.2
        _WarmthFactor ("Warmth Factor", Range(-0.5, 0.5)) = 0.1
        
        [Header(Stars Night Only)]
        _StarBrightness ("Star Brightness", Range(0, 1)) = 0.8
        _StarDensity ("Star Density", Range(20, 150)) = 60
        _StarSize ("Star Size", Range(0.01, 0.08)) = 0.025
        _StarTwinkle ("Star Twinkle", Range(0, 1.5)) = 0.3
        
        [Header(Night Sky Enhancement)]
        _NightSkyIntensity ("Night Sky Intensity", Range(0.8, 2)) = 1.3
        _NightSkyDesaturation ("Night Desaturation", Range(0, 1)) = 0.4
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
            #pragma target 2.0
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };
            
            // Properties
            fixed4 _SkyColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            
            float _HorizonBlend;
            float _AtmospherePower;
            float _WarmthFactor;
            float _StarBrightness;
            float _StarDensity;
            float _StarSize;
            float _StarTwinkle;
            float _NightSkyIntensity;
            float _NightSkyDesaturation;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = normalize(mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }
            
            // Simple hash function optimized for mobile
            float hash12(float2 p)
            {
                p = frac(p * 0.3183099);
                p += dot(p, p.yx + 19.19);
                return frac(p.x * p.y);
            }
            
            // Mobile-optimized star generation
            float generateStars(float3 dir, float nightFactor)
            {
                // Only generate stars if it's night and we're looking up
                if (dir.y <= 0.05 || nightFactor < 0.3) return 0.0;
                
                // Simple sphere UV mapping
                float2 uv = float2(
                    atan2(dir.z, dir.x) * 0.159155 + 0.5,
                    acos(dir.y) * 0.318310
                );
                
                float2 grid = uv * _StarDensity;
                float2 cellId = floor(grid);
                float2 cellPos = frac(grid) - 0.5;
                
                // Single star per cell
                float2 offset = (hash12(cellId) - 0.5) * 0.7;
                float2 starPos = offset;
                
                float dist = length(cellPos - starPos);
                float starMask = 1.0 - smoothstep(0.0, _StarSize, dist);
                
                // Star brightness with realistic distribution
                float brightness = pow(hash12(cellId + 13.7), 2.0);
                
                // Simple twinkle
                float twinkle = 1.0;
                if (_StarTwinkle > 0.0)
                {
                    float phase = _Time.y * _StarTwinkle + hash12(cellId) * 6.28;
                    twinkle = 0.85 + 0.15 * sin(phase);
                }
                
                return starMask * brightness * twinkle;
            }
            
            // Atmospheric calculation with warmth
            fixed3 calculateAtmosphere(float3 dir)
            {
                float height = dir.y;
                float absHeight = abs(height);
                
                // Smooth transitions
                float upFactor = saturate(height);
                float downFactor = saturate(-height);
                
                // Main gradient with atmospheric power
                fixed3 skyColor = lerp(_HorizonColor.rgb, _SkyColor.rgb, pow(upFactor, _AtmospherePower));
                fixed3 groundColor = lerp(_HorizonColor.rgb, _GroundColor.rgb, pow(downFactor, _AtmospherePower));
                
                // Blend sky and ground
                fixed3 baseColor = lerp(groundColor, skyColor, saturate(height + 0.1));
                
                // Horizon glow effect
                float horizonGlow = pow(1.0 - absHeight, _HorizonBlend);
                baseColor = lerp(baseColor, _HorizonColor.rgb, horizonGlow * 0.4);
                
                // Apply warmth factor for cozy atmosphere
                if (_WarmthFactor != 0.0)
                {
                    fixed3 warmColor = baseColor;
                    if (_WarmthFactor > 0.0)
                    {
                        // Warmer - shift towards orange/red
                        warmColor.r *= (1.0 + _WarmthFactor * 0.3);
                        warmColor.g *= (1.0 + _WarmthFactor * 0.15);
                        warmColor.b *= (1.0 - _WarmthFactor * 0.1);
                    }
                    else
                    {
                        // Cooler - shift towards blue
                        warmColor.r *= (1.0 + _WarmthFactor * 0.2);
                        warmColor.g *= (1.0 + _WarmthFactor * 0.1);
                        warmColor.b *= (1.0 - _WarmthFactor * 0.3);
                    }
                    baseColor = warmColor;
                }
                
                return baseColor;
            }
            
            // Convert RGB to luminance
            float getLuminance(fixed3 color)
            {
                return dot(color, float3(0.299, 0.587, 0.114));
            }
            
            // Simple desaturation
            fixed3 desaturate(fixed3 color, float amount)
            {
                float lum = getLuminance(color);
                return lerp(color, fixed3(lum, lum, lum), amount);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldPos);
                
                // Calculate base atmosphere
                fixed3 atmosphere = calculateAtmosphere(dir);
                
                // Determine night factor based on sky brightness
                float skyLuminance = getLuminance(atmosphere);
                float nightFactor = 1.0 - saturate(skyLuminance * 1.8);
                
                // Night sky enhancement
                if (nightFactor > 0.1)
                {
                    // Intensify night colors
                    atmosphere *= lerp(1.0, _NightSkyIntensity, nightFactor * nightFactor);
                    
                    // Slightly desaturate for more realistic night look
                    atmosphere = desaturate(atmosphere, _NightSkyDesaturation * nightFactor);
                }
                
                fixed3 finalColor = atmosphere;
                
                // Add stars only during night
                if (nightFactor > 0.3 && dir.y > 0.0)
                {
                    float stars = generateStars(dir, nightFactor);
                    
                    // Warm white star color
                    fixed3 starColor = fixed3(1.0, 0.98, 0.94);
                    
                    // Apply stars with night visibility
                    finalColor += stars * starColor * _StarBrightness * nightFactor;
                }
                
                // Subtle atmospheric tinting for cozy feel
                finalColor = lerp(finalColor, finalColor * fixed3(1.03, 1.01, 0.99), 0.08);
                
                // Ensure we don't exceed 1.0
                finalColor = saturate(finalColor);
                
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    
    Fallback "Skybox/Procedural"
}