Shader "Custom/AtmosphericSkybox"
{
    Properties
    {
        [Header(Sky Colors)]
        _SkyColor("Sky Color", Color) = (0.4, 0.6, 0.9, 1)
        _HorizonColor("Horizon Color", Color) = (0.9, 0.85, 0.8, 1)
        _GroundColor("Ground Color", Color) = (0.3, 0.25, 0.2, 1)
        
        [Header(Atmospheric Settings)]
        _AtmosphereThickness("Atmosphere Thickness", Range(0, 5)) = 1.0
        _SunSize("Sun Size", Range(0, 0.1)) = 0.04
        _SunSizeConvergence("Sun Size Convergence", Range(1, 10)) = 5
        
        [Header(Stars)]
        _StarBrightness("Star Brightness", Range(0, 1)) = 0.5
        _StarDensity("Star Density", Range(50, 500)) = 200
        _StarTwinkleSpeed("Star Twinkle Speed", Range(0, 5)) = 1
        
        [Header(Horizon Effects)]
        _HorizonBlend("Horizon Blend", Range(0.1, 5)) = 1
        _HorizonGlow("Horizon Glow", Range(0, 2)) = 0.3
        
        [Header(Cloud Settings)]
        _CloudCoverage("Cloud Coverage", Range(0, 1)) = 0.5
        _CloudSpeed("Cloud Speed", Range(0, 2)) = 0.1
        _CloudColor("Cloud Color", Color) = (1, 1, 1, 1)
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
            
            // Properties
            fixed4 _SkyColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            fixed4 _CloudColor;
            
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
            
            // Noise functions
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            float noise(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(lerp(hash(p + float3(0,0,0)), 
                                    hash(p + float3(1,0,0)), f.x),
                               lerp(hash(p + float3(0,1,0)), 
                                    hash(p + float3(1,1,0)), f.x), f.y),
                           lerp(lerp(hash(p + float3(0,0,1)), 
                                    hash(p + float3(1,0,1)), f.x),
                               lerp(hash(p + float3(0,1,1)), 
                                    hash(p + float3(1,1,1)), f.x), f.y), f.z);
            }
            
            // Stars generation
            float stars(float3 dir)
            {
                float3 starPos = dir * _StarDensity;
                float3 id = floor(starPos);
                float3 localPos = frac(starPos) - 0.5;
                
                float starHash = hash(id);
                float starSize = 0.02 * starHash;
                float starBrightness = starHash * starHash;
                
                // Twinkle effect
                float twinkle = sin(_Time.y * _StarTwinkleSpeed + starHash * 6.28) * 0.5 + 0.5;
                starBrightness *= twinkle;
                
                float star = 1.0 - smoothstep(0.0, starSize, length(localPos));
                return star * starBrightness * _StarBrightness;
            }
            
            // Simple cloud function
            float clouds(float3 dir, float time)
            {
                float3 cloudPos = dir * 50 + float3(time * _CloudSpeed, 0, time * _CloudSpeed * 0.5);
                float cloudNoise = noise(cloudPos * 0.5) * 0.5 + 0.25;
                cloudNoise += noise(cloudPos * 1.0) * 0.25;
                cloudNoise += noise(cloudPos * 2.0) * 0.125;
                
                float cloudMask = smoothstep(1.0 - _CloudCoverage, 1.0, cloudNoise);
                return cloudMask * saturate(dir.y * 2);
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDir);
                float sunDot = dot(viewDir, _WorldSpaceLightPos0.xyz);
                
                // Atmospheric gradient
                float horizon = abs(viewDir.y);
                float horizonMask = 1.0 - horizon;
                
                // Main gradient from ground to sky
                float skyGradient = saturate(viewDir.y);
                float groundGradient = saturate(-viewDir.y);
                
                // Base sky color
                fixed3 skyColor = lerp(_HorizonColor.rgb, _SkyColor.rgb, pow(skyGradient, _HorizonBlend));
                fixed3 groundColor = lerp(_HorizonColor.rgb, _GroundColor.rgb, groundGradient);
                
                fixed3 finalColor = lerp(groundColor, skyColor, saturate(viewDir.y + 0.1));
                
                // Atmospheric scattering effect
                float scattering = pow(saturate(sunDot), _SunSizeConvergence);
                float sunSize = 1.0 - saturate(distance(viewDir, _WorldSpaceLightPos0.xyz) / _SunSize);
                sunSize = pow(sunSize, 50);
                
                // Add sun disk
                finalColor += sunSize * _LightColor0.rgb;
                
                // Add atmospheric glow around horizon
                float horizonGlow = pow(horizonMask, 2) * _HorizonGlow;
                finalColor += horizonGlow * _HorizonColor.rgb * _LightColor0.rgb;
                
                // Add stars (only visible when sun is down)
                float starVisibility = saturate(1.0 - _LightColor0.a * 2);
                if (viewDir.y > 0)
                {
                    finalColor += stars(viewDir) * starVisibility;
                }
                
                // Add simple clouds during day
                float cloudVisibility = saturate(_LightColor0.a);
                if (viewDir.y > 0 && _CloudCoverage > 0.01)
                {
                    float cloudMask = clouds(viewDir, _Time.y);
                    finalColor = lerp(finalColor, _CloudColor.rgb * finalColor, cloudMask * cloudVisibility);
                }
                
                // Apply atmospheric thickness
                finalColor = lerp(finalColor, _HorizonColor.rgb, horizonMask * _AtmosphereThickness * 0.1);
                
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    
    Fallback "Skybox/Procedural"
}