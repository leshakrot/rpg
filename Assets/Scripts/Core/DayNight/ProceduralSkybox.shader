Shader "Custom/ProceduralSkybox"
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
        
        [Header(Stars Night Only)]
        _StarBrightness ("Star Brightness", Range(0, 1)) = 0.8
        _StarDensity ("Star Density", Range(20, 200)) = 80
        _StarSize ("Star Size", Range(0.01, 0.1)) = 0.03
        _StarTwinkle ("Star Twinkle", Range(0, 2)) = 0.5
        
        [Header(Night Sky Enhancement)]
        _NightSkyIntensity ("Night Sky Intensity", Range(0, 2)) = 1.2
        _MilkyWayIntensity ("Milky Way Intensity", Range(0, 1)) = 0.3
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
            float _StarBrightness;
            float _StarDensity;
            float _StarSize;
            float _StarTwinkle;
            float _NightSkyIntensity;
            float _MilkyWayIntensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = normalize(mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }
            
            // ���-������� ��� ��������� ��������������� �����
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            // ������� ��� �������� �����
            float stars(float3 dir)
            {
                float3 p = dir * _StarDensity;
                float3 cellPos = floor(p);
                float3 cellCenter = cellPos + 0.5;
                
                float minDist = 1.0;
                
                // ��������� ������� � �������� ������
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            float3 offset = float3(x, y, z);
                            float3 cell = cellPos + offset;
                            
                            // ���������� ��� ��� �������� ��������� ������� ������ � ������
                            float3 starPos = cell + hash(cell) * 0.9 + 0.05;
                            float dist = length(p - starPos);
                            
                            minDist = min(minDist, dist);
                        }
                    }
                }
                
                // ������� ������ � ������ �����
                float starVal = 1.0 - smoothstep(0.0, 0.3, minDist);
                
                // ��������� �������
                float brightness = hash(cellCenter) * 0.8 + 0.2;
                
                return starVal * brightness * _StarBrightness;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldPos);
                
                // ����������� �����������: ����� ��� ����
                float upFactor = dir.y * 0.5 + 0.5; // ����������� [-1,1] � [0,1]
                
                // ����� ��� ���������
                float horizonMask = 1.0 - pow(abs(dir.y), _HorizonBlend);
                
                // �������� ����� ���� � �����
                fixed4 skyGround = lerp(_GroundColor, _SkyColor, upFactor);
                
                // ���������� � ����������
                fixed4 baseColor = lerp(skyGround, _HorizonColor, horizonMask);
                
                // ���������� ����� ������ ��� ������� ����� (����)
                float star = 0;
                if (dir.y > 0.0) // ������ �� ������� ���������
                {
                    star = stars(dir);
                }
                
                // ���� �����
                fixed4 starColor = fixed4(1.0, 1.0, 1.0, 1.0);
                
                // ��������� ���� � ����������� �����
                fixed4 finalColor = lerp(baseColor, starColor, star);
                
                return finalColor;
            }
            ENDCG
        }
    }
}