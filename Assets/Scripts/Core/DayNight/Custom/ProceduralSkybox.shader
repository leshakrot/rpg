Shader "Custom/ProceduralSkybox"
{
    Properties
    {
        [Header(Sky)]
        _SkyColor ("Sky Color", Color) = (0.22,0.38,0.72,1)
        _MidSkyColor ("Mid Sky Color", Color) = (0.35,0.52,0.82,1)
        _HorizonColor ("Horizon Color", Color) = (0.82,0.72,0.55,1)
        _GroundColor ("Ground Color", Color) = (0.10,0.10,0.15,1)

        [Header(Atmosphere)]
        _HorizonBlend ("Horizon Blend", Range(0.5,8)) = 3
        _AtmospherePower ("Atmosphere Curve", Range(0.2,3)) = 0.75
        _HorizonGlow ("Horizon Glow", Range(0,1)) = 0.35
        _WarmthFactor ("Warmth", Range(-0.5,0.5)) = 0.08
        _AtmosphereIntensity ("Atmosphere Intensity", Range(0,2)) = 1

        [Header(Stars)]
        _StarBrightness ("Star Brightness", Range(0,1)) = 0.92
        _StarDensity ("Star Density", Range(30,220)) = 90
        _StarSize ("Star Size", Range(0.005,0.1)) = 0.022
        _StarTwinkle ("Star Twinkle", Range(0,2)) = 0.35
        _StarColorShift ("Star Color Variation", Range(0,1)) = 0.3

        [Header(Night)]
        _NightSkyIntensity ("Night Intensity", Range(0.5,2)) = 1.1
        _NightTint ("Night Tint", Color) = (0.52,0.58,1,1)
        _NightTintStrength ("Night Tint Strength", Range(0,0.6)) = 0.20
        _NightBoost ("Night Boost", Range(0.5,2.5)) = 1

        [Header(Celestial)]
        _SunDirection ("Sun Direction", Vector) = (0,1,0,0)
        _MoonDirection ("Moon Direction", Vector) = (0,-1,0,0)
        _CelestialGlow ("Celestial Glow", Range(0,2)) = 1
        _DuskGlow ("Dusk Glow", Range(0,2)) = 1

        [Header(Milky Way)]
        _MilkyWayIntensity ("Milky Way", Range(0,1)) = 0.28
        _MilkyWayRotation ("Milky Way Rotation", Range(0,6.28)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            Name "SkyboxPCPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _SkyColor, _MidSkyColor, _HorizonColor, _GroundColor, _NightTint;
            float _HorizonBlend, _AtmospherePower, _HorizonGlow, _WarmthFactor, _AtmosphereIntensity;
            float _StarBrightness, _StarDensity, _StarSize, _StarTwinkle, _StarColorShift;
            float _NightSkyIntensity, _NightTintStrength, _NightBoost;
            float4 _SunDirection, _MoonDirection;
            float _CelestialGlow, _DuskGlow;
            float _MilkyWayIntensity, _MilkyWayRotation;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 worldDir : TEXCOORD0; };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldDir = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(0.1031,0.1030));
                p += dot(p,p.yx + 33.33);
                return frac((p.x+p.y)*p.x);
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x*p.y*p.z*(p.x+p.y+p.z));
            }

            float Lum(half3 c) { return dot(c,half3(0.299,0.587,0.114)); }

            half3 ApplyWarmth(half3 c,float w)
            {
                if(w>0)
                {
                    c.r=min(1.0h,c.r*(1+w*0.30));
                    c.g=min(1.0h,c.g*(1+w*0.11));
                    c.b=max(0.0h,c.b*(1-w*0.14));
                }
                else
                {
                    float f=-w;
                    c.b=min(1.0h,c.b*(1+f*0.34));
                    c.g=max(0.0h,c.g*(1-f*0.06));
                    c.r=max(0.0h,c.r*(1-f*0.20));
                }
                return c;
            }

            half3 Atmosphere(float3 d)
            {
                float h=d.y;
                float up=saturate(h);
                float down=saturate(-h);

                half3 top=lerp(_MidSkyColor.rgb,_SkyColor.rgb,pow(up,_AtmospherePower));
                half3 bottom=lerp(_HorizonColor.rgb,_GroundColor.rgb,pow(down,_AtmospherePower*1.35));
                half3 col=lerp(bottom,top,saturate(h+0.10));

                float horizon=pow(saturate(1-abs(h)),_HorizonBlend);
                col=lerp(col,_HorizonColor.rgb,horizon*_HorizonGlow);
                col=ApplyWarmth(col,_WarmthFactor);

                // Subtle vertical atmospheric lift; nearly free.
                col *= (half)_AtmosphereIntensity;
                return col;
            }

            float MilkyWay(float3 d)
            {
                float s=sin(_MilkyWayRotation), c=cos(_MilkyWayRotation);
                float3 r=float3(d.x*c-d.z*s,d.y,d.x*s+d.z*c);
                float band=exp(-abs(r.y)*7.5)*0.52;
                float2 uv=float2(atan2(r.z,r.x)*0.15915,r.y);
                float n=Hash21(floor(uv*42))*0.58
                       +Hash21(floor(uv*95))*0.28
                       +Hash21(floor(uv*190))*0.14;
                // Dark dust lane breaks the band into a less synthetic shape.
                float dust=1.0-smoothstep(0.12,0.42,abs(r.y+0.08*sin(uv.x*11)));
                return band*n*(0.65+0.35*dust)*_MilkyWayIntensity;
            }

            half3 Stars(float3 d,float night)
            {
                if(d.y<0.02 || night<0.12) return 0;

                float2 uv=float2(atan2(d.z,d.x)*0.15915+0.5,
                                 acos(clamp(d.y,-1,1))*0.31831);
                float2 grid=uv*_StarDensity;
                float2 id=floor(grid);
                float2 cell=frac(grid)-0.5;
                float2 off=(float2(Hash21(id),Hash21(id+7.3))-0.5)*0.72;
                float dist=length(cell-off);

                float size=_StarSize*(0.72+0.55*Hash21(id+3.2));
                float core=1-smoothstep(0,size,dist);
                float halo=1-smoothstep(size*3.5,size,dist);
                float b=pow(Hash21(id+13.7),2.15);
                float phase=_Time.y*_StarTwinkle+Hash21(id+19.1)*6.28318;
                float tw=0.82+0.18*sin(phase);

                float hue=Hash21(id+21.3);
                half3 tint=lerp(half3(1.0,0.92,0.78),
                           lerp(half3(0.72,0.86,1.0),half3(1.0,0.70,0.82),step(0.5,hue)),
                           _StarColorShift*saturate((hue-0.25)*3.0));

                return tint*(core+halo*0.18)*b*tw;
            }

            half3 CelestialBodies(float3 d,float night)
            {
                half3 outCol=0;

                float sunDot=saturate(dot(d,normalize(_SunDirection.xyz)));
                float moonDot=saturate(dot(d,normalize(_MoonDirection.xyz)));

                // Tiny disk + broad atmospheric aureole. No texture, no particles.
                float sunDisk=smoothstep(0.9994,0.99992,sunDot);
                float sunHalo=pow(sunDot,150.0)*0.22 + pow(sunDot,600.0)*0.55;

                // Sun is strongest around day/golden hour.
                float sunVisible=saturate(1-night*1.4);
                outCol += half3(1.0,0.55,0.20)*(half)((sunDisk*1.6+sunHalo)*_CelestialGlow*sunVisible);

                float moonDisk=smoothstep(0.99945,0.99993,moonDot);
                float moonHalo=pow(moonDot,180.0)*0.12+pow(moonDot,700.0)*0.30;
                outCol += half3(0.55,0.70,1.0)*(half)((moonDisk*1.15+moonHalo)*_CelestialGlow*night);

                return outCol;
            }

            half4 Frag(Varyings IN):SV_Target
            {
                float3 d=normalize(IN.worldDir);
                half3 col=Atmosphere(d);

                float lum=Lum(col);
                float night=saturate(1-lum*2.2);
                night*=night;
                float twilight=saturate(1-abs(lum-0.20)*8.0);

                if(night>0.02)
                {
                    col*=lerp(1.0,_NightSkyIntensity*_NightBoost,night);
                    col=lerp(col,col*_NightTint.rgb,(half)(_NightTintStrength*night));
                }

                if(night>0.08 && d.y>0)
                {
                    col += Stars(d,night)*(half)(_StarBrightness*night);
                    col += half3(0.68,0.76,1.0)*(half)(MilkyWay(d)*night);
                }

                col += CelestialBodies(d,night);

                // Very restrained twilight magenta/amber lift around the horizon.
                float horizon=pow(saturate(1-abs(d.y)),4.0);
                float twilightMask=horizon*twilight*_DuskGlow;
                col += lerp(half3(0.45,0.08,0.12),half3(1.0,0.28,0.08),saturate(_WarmthFactor+0.2))*twilightMask*0.10;

                return half4(saturate(col),1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
