using UnityEngine;
using System.Collections.Generic;

namespace RPG.Control
{
    /// <summary>
    /// Компонент для подсветки интерактивных объектов при наведении курсора.
    /// Поддерживает несколько режимов подсветки для совместимости с разными материалами.
    /// </summary>
    public class InteractableHighlight : MonoBehaviour
    {
        public enum HighlightMode
        {
            Emission,           // Использует Emission (требует поддержки в шейдере)
            ColorTint,          // Изменяет основной цвет материала
            EmissionOrColorTint // Пробует Emission, если не работает - использует ColorTint (рекомендуется)
        }

        [Header("Настройки подсветки")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.4f, 1f);
        [SerializeField] [Range(0.1f, 5f)] private float highlightIntensity = 0.15f;
        [SerializeField] private bool includeChildren = true;
        [SerializeField] private HighlightMode mode = HighlightMode.EmissionOrColorTint;
        [SerializeField] [Range(0f, 1f)] private float colorTintStrength = 0.4f;

        private List<RendererData> rendererDataList = new List<RendererData>();
        private bool isHighlighted = false;
        
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissiveColorProperty = Shader.PropertyToID("_EmissiveColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int MainColorProperty = Shader.PropertyToID("_MainColor");

        private class RendererData
        {
            public Renderer renderer;
            public Material[] originalMaterials;
            public Material[] instanceMaterials;
            public MaterialInfo[] materialInfos;
        }

        private class MaterialInfo
        {
            public bool hadEmission;
            public Color originalEmissionColor;
            public bool supportsEmission;
            public Color originalColor;
            public bool supportsColor;
            public string colorPropertyName;
        }

        private void Awake()
        {
            CacheMaterials();
        }

        private void OnEnable()
        {
            HighlightManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (HighlightManager.Instance != null)
            {
                HighlightManager.Instance.Unregister(this);
            }
            
            if (isHighlighted)
            {
                DisableHighlight();
            }
        }

        private void CacheMaterials()
        {
            rendererDataList.Clear();

            Renderer[] renderers = includeChildren 
                ? GetComponentsInChildren<Renderer>() 
                : GetComponents<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                Material[] sharedMats = renderer.sharedMaterials;
                if (sharedMats == null || sharedMats.Length == 0) continue;

                RendererData data = new RendererData
                {
                    renderer = renderer,
                    originalMaterials = sharedMats,
                    materialInfos = new MaterialInfo[sharedMats.Length]
                };

                // Сохраняем информацию о каждом материале
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    Material mat = sharedMats[i];
                    MaterialInfo info = new MaterialInfo();

                    if (mat != null)
                    {
                        // Проверяем поддержку Emission
                        if (mat.HasProperty(EmissionColorProperty))
                        {
                            info.supportsEmission = true;
                            info.hadEmission = mat.IsKeywordEnabled("_EMISSION");
                            info.originalEmissionColor = mat.GetColor(EmissionColorProperty);
                        }

                        // Проверяем поддержку Color (для ColorTint режима)
                        if (mat.HasProperty(BaseColorProperty))
                        {
                            info.supportsColor = true;
                            info.originalColor = mat.GetColor(BaseColorProperty);
                            info.colorPropertyName = "_BaseColor";
                        }
                        else if (mat.HasProperty(ColorProperty))
                        {
                            info.supportsColor = true;
                            info.originalColor = mat.GetColor(ColorProperty);
                            info.colorPropertyName = "_Color";
                        }
                        else if (mat.HasProperty(MainColorProperty))
                        {
                            info.supportsColor = true;
                            info.originalColor = mat.GetColor(MainColorProperty);
                            info.colorPropertyName = "_MainColor";
                        }
                    }

                    data.materialInfos[i] = info;
                }

                rendererDataList.Add(data);
            }
        }

        public void EnableHighlight()
        {
            if (isHighlighted) return;
            
            isHighlighted = true;

            foreach (RendererData data in rendererDataList)
            {
                if (data.renderer == null) continue;

                // Создаем инстансы материалов только при первой подсветке
                if (data.instanceMaterials == null)
                {
                    data.instanceMaterials = new Material[data.originalMaterials.Length];
                    for (int i = 0; i < data.originalMaterials.Length; i++)
                    {
                        if (data.originalMaterials[i] != null)
                        {
                            data.instanceMaterials[i] = new Material(data.originalMaterials[i]);
                        }
                    }
                    data.renderer.materials = data.instanceMaterials;
                }

                // Применяем подсветку
                for (int i = 0; i < data.instanceMaterials.Length; i++)
                {
                    Material mat = data.instanceMaterials[i];
                    MaterialInfo info = data.materialInfos[i];
                    
                    if (mat == null) continue;

                    bool highlightApplied = false;

                    // Пробуем применить Emission
                    if ((mode == HighlightMode.Emission || mode == HighlightMode.EmissionOrColorTint) && info.supportsEmission)
                    {
                        Color emissionColor = highlightColor * highlightIntensity;
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor(EmissionColorProperty, emissionColor);
                        
                        if (mat.HasProperty(EmissiveColorProperty))
                        {
                            mat.SetColor(EmissiveColorProperty, emissionColor);
                        }
                        
                        highlightApplied = true;
                    }

                    // Если Emission не сработал или режим ColorTint, применяем ColorTint
                    if ((mode == HighlightMode.ColorTint || (mode == HighlightMode.EmissionOrColorTint && !highlightApplied)) && info.supportsColor)
                    {
                        Color tintedColor = Color.Lerp(info.originalColor, highlightColor, colorTintStrength);
                        mat.SetColor(info.colorPropertyName, tintedColor);
                        highlightApplied = true;
                    }
                }
            }
        }

        public void DisableHighlight()
        {
            if (!isHighlighted) return;
            
            isHighlighted = false;

            foreach (RendererData data in rendererDataList)
            {
                if (data.renderer == null || data.instanceMaterials == null) continue;

                // Восстанавливаем оригинальное состояние
                for (int i = 0; i < data.instanceMaterials.Length; i++)
                {
                    Material mat = data.instanceMaterials[i];
                    MaterialInfo info = data.materialInfos[i];

                    if (mat == null) continue;

                    // Восстанавливаем Emission
                    if (info.supportsEmission)
                    {
                        if (info.hadEmission)
                        {
                            mat.SetColor(EmissionColorProperty, info.originalEmissionColor);
                            if (mat.HasProperty(EmissiveColorProperty))
                            {
                                mat.SetColor(EmissiveColorProperty, info.originalEmissionColor);
                            }
                        }
                        else
                        {
                            mat.DisableKeyword("_EMISSION");
                            mat.SetColor(EmissionColorProperty, Color.black);
                            if (mat.HasProperty(EmissiveColorProperty))
                            {
                                mat.SetColor(EmissiveColorProperty, Color.black);
                            }
                        }
                    }

                    // Восстанавливаем Color
                    if (info.supportsColor)
                    {
                        mat.SetColor(info.colorPropertyName, info.originalColor);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (isHighlighted)
            {
                DisableHighlight();
            }

            // Очищаем инстансы материалов
            foreach (RendererData data in rendererDataList)
            {
                if (data.renderer != null && data.instanceMaterials != null)
                {
                    data.renderer.sharedMaterials = data.originalMaterials;
                    
                    foreach (Material mat in data.instanceMaterials)
                    {
                        if (mat != null)
                        {
                            Destroy(mat);
                        }
                    }
                }
            }
        }
    }
}
