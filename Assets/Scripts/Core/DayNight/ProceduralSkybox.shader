Shader "Custom/ProceduralSkybox"
{
    Properties
    {
        _SkyColor ("Sky Color", Color) = (0.4, 0.6, 0.9, 1.0)
        _HorizonColor ("Horizon Color", Color) = (0.9, 0.85, 0.8, 1.0)
        _GroundColor ("Ground Color", Color) = (0.3, 0.25, 0.2, 1.0)
        _StarBrightness ("Star Brightness", Range(0, 1)) = 0.5
        _StarDensity ("Star Density", Range(10, 500)) = 100
        _HorizonBlend ("Horizon Blend", Range(0.1, 10)) = 1.0
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
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };
            
            fixed4 _SkyColor;
            fixed4 _HorizonColor;
            fixed4 _GroundColor;
            float _StarBrightness;
            float _StarDensity;
            float _HorizonBlend;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = normalize(mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }
            
            // Хэш-функция для генерации псевдослучайных чисел
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            // Функция для создания звезд
            float stars(float3 dir)
            {
                float3 p = dir * _StarDensity;
                float3 cellPos = floor(p);
                float3 cellCenter = cellPos + 0.5;
                
                float minDist = 1.0;
                
                // Проверяем текущую и соседние ячейки
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            float3 offset = float3(x, y, z);
                            float3 cell = cellPos + offset;
                            
                            // Используем хэш для создания случайной позиции звезды в ячейке
                            float3 starPos = cell + hash(cell) * 0.9 + 0.05;
                            float dist = length(p - starPos);
                            
                            minDist = min(minDist, dist);
                        }
                    }
                }
                
                // Создаем звезду с мягким краем
                float starVal = 1.0 - smoothstep(0.0, 0.3, minDist);
                
                // Случайная яркость
                float brightness = hash(cellCenter) * 0.8 + 0.2;
                
                return starVal * brightness * _StarBrightness;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldPos);
                
                // Определение направления: вверх или вниз
                float upFactor = dir.y * 0.5 + 0.5; // Преобразуем [-1,1] в [0,1]
                
                // Маска для горизонта
                float horizonMask = 1.0 - pow(abs(dir.y), _HorizonBlend);
                
                // Основные цвета неба и земли
                fixed4 skyGround = lerp(_GroundColor, _SkyColor, upFactor);
                
                // Смешивание с горизонтом
                fixed4 baseColor = lerp(skyGround, _HorizonColor, horizonMask);
                
                // Добавление звезд только для верхней части (небо)
                float star = 0;
                if (dir.y > 0.0) // Только на верхней полусфере
                {
                    star = stars(dir);
                }
                
                // Цвет звезд
                fixed4 starColor = fixed4(1.0, 1.0, 1.0, 1.0);
                
                // Финальный цвет с добавлением звезд
                fixed4 finalColor = lerp(baseColor, starColor, star);
                
                return finalColor;
            }
            ENDCG
        }
    }
}