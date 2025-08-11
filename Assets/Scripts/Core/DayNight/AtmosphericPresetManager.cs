using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class GraphicsPreset
{
    [Header("Основные настройки")]
    public string presetName = "Default";
    
    [Header("Освещение")]
    [Range(0.5f, 2f)] public float lightIntensityMultiplier = 1f;
    [Range(0.3f, 1.5f)] public float ambientIntensityMultiplier = 1f;
    public bool enableSoftShadows = true;
    [Range(0.1f, 2f)] public float shadowDistanceMultiplier = 1f;
    
    [Header("Туман")]
    public bool enableAdvancedFog = true;
    [Range(0.5f, 2f)] public float fogDensityMultiplier = 1f;
    
    [Header("Атмосферные эффекты")]
    [Range(0f, 1f)] public float atmosphericScatteringStrength = 0.7f;
    [Range(0.5f, 1.5f)] public float colorSaturationMultiplier = 1f;
    [Range(0.8f, 1.2f)] public float contrastMultiplier = 1f;
    
    [Header("Оптимизация")]
    public bool enableRealTimeReflections = true;
    [Range(1, 4)] public int lightmapResolutionScale = 1;
    public bool enableVolumetricFog = true;
}

public class AtmosphericPresetManager : MonoBehaviour
{
    [Header("Пресеты графики")]
    public GraphicsPreset lowPreset;
    public GraphicsPreset mediumPreset;
    public GraphicsPreset highPreset;
    public GraphicsPreset ultraPreset;
    
    [Header("Автоматическое определение")]
    public bool autoDetectQuality = true;
    
    private DayNightSystem dayNightSystem;
    private GraphicsPreset currentPreset;
    
    private void Start()
    {
        dayNightSystem = GetComponent<DayNightSystem>();
        if (dayNightSystem == null)
        {
            Debug.LogError("AtmosphericPresetManager требует DayNightSystem на том же GameObject!");
            return;
        }
        
        InitializePresets();
        
        if (autoDetectQuality)
        {
            ApplyPresetBasedOnQuality();
        }
        else
        {
            ApplyPreset(mediumPreset);
        }
    }
    
    private void InitializePresets()
    {
        // Низкие настройки
        if (lowPreset.presetName == "Default")
        {
            lowPreset.presetName = "Low";
            lowPreset.lightIntensityMultiplier = 0.8f;
            lowPreset.ambientIntensityMultiplier = 1.2f;
            lowPreset.enableSoftShadows = false;
            lowPreset.shadowDistanceMultiplier = 0.5f;
            lowPreset.enableAdvancedFog = false;
            lowPreset.fogDensityMultiplier = 0.7f;
            lowPreset.atmosphericScatteringStrength = 0.3f;
            lowPreset.colorSaturationMultiplier = 0.9f;
            lowPreset.contrastMultiplier = 1.1f;
            lowPreset.enableRealTimeReflections = false;
            lowPreset.lightmapResolutionScale = 1;
            lowPreset.enableVolumetricFog = false;
        }
        
        // Средние настройки
        if (mediumPreset.presetName == "Default")
        {
            mediumPreset.presetName = "Medium";
            mediumPreset.lightIntensityMultiplier = 1f;
            mediumPreset.ambientIntensityMultiplier = 1f;
            mediumPreset.enableSoftShadows = true;
            mediumPreset.shadowDistanceMultiplier = 0.8f;
            mediumPreset.enableAdvancedFog = true;
            mediumPreset.fogDensityMultiplier = 1f;
            mediumPreset.atmosphericScatteringStrength = 0.5f;
            mediumPreset.colorSaturationMultiplier = 1f;
            mediumPreset.contrastMultiplier = 1f;
            mediumPreset.enableRealTimeReflections = true;
            mediumPreset.lightmapResolutionScale = 2;
            mediumPreset.enableVolumetricFog = false;
        }
        
        // Высокие настройки
        if (highPreset.presetName == "Default")
        {
            highPreset.presetName = "High";
            highPreset.lightIntensityMultiplier = 1.1f;
            highPreset.ambientIntensityMultiplier = 0.9f;
            highPreset.enableSoftShadows = true;
            highPreset.shadowDistanceMultiplier = 1f;
            highPreset.enableAdvancedFog = true;
            highPreset.fogDensityMultiplier = 1.2f;
            highPreset.atmosphericScatteringStrength = 0.7f;
            highPreset.colorSaturationMultiplier = 1.1f;
            highPreset.contrastMultiplier = 1f;
            highPreset.enableRealTimeReflections = true;
            highPreset.lightmapResolutionScale = 3;
            highPreset.enableVolumetricFog = true;
        }
        
        // Ультра настройки
        if (ultraPreset.presetName == "Default")
        {
            ultraPreset.presetName = "Ultra";
            ultraPreset.lightIntensityMultiplier = 1.2f;
            ultraPreset.ambientIntensityMultiplier = 0.8f;
            ultraPreset.enableSoftShadows = true;
            ultraPreset.shadowDistanceMultiplier = 1.5f;
            ultraPreset.enableAdvancedFog = true;
            ultraPreset.fogDensityMultiplier = 1.5f;
            ultraPreset.atmosphericScatteringStrength = 1f;
            ultraPreset.colorSaturationMultiplier = 1.2f;
            ultraPreset.contrastMultiplier = 0.95f;
            ultraPreset.enableRealTimeReflections = true;
            ultraPreset.lightmapResolutionScale = 4;
            ultraPreset.enableVolumetricFog = true;
        }
    }
    
    private void ApplyPresetBasedOnQuality()
    {
        int qualityLevel = QualitySettings.GetQualityLevel();
        int totalLevels = QualitySettings.names.Length;
        
        GraphicsPreset selectedPreset;
        
        if (qualityLevel == 0)
        {
            selectedPreset = lowPreset;
        }
        else if (qualityLevel <= totalLevels / 3)
        {
            selectedPreset = mediumPreset;
        }
        else if (qualityLevel <= (totalLevels * 2) / 3)
        {
            selectedPreset = highPreset;
        }
        else
        {
            selectedPreset = ultraPreset;
        }
        
        ApplyPreset(selectedPreset);
    }
    
    public void ApplyPreset(GraphicsPreset preset)
    {
        if (preset == null || dayNightSystem == null) return;
        
        currentPreset = preset;
        
        // Применение настроек освещения
        dayNightSystem.maxSunIntensity *= preset.lightIntensityMultiplier;
        dayNightSystem.maxMoonIntensity *= preset.lightIntensityMultiplier;
        
        if (dayNightSystem.atmosphericSettings != null)
        {
            dayNightSystem.atmosphericSettings.ambientIntensity *= preset.ambientIntensityMultiplier;
        }
        
        // Настройка теней
        ConfigureShadows(preset);
        
        // Настройка тумана
        dayNightSystem.fogDensityDay *= preset.fogDensityMultiplier;
        dayNightSystem.fogDensityNight *= preset.fogDensityMultiplier;
        
        // Применение атмосферных эффектов
        ApplyAtmosphericEffects(preset);
        
        Debug.Log($"Применен пресет графики: {preset.presetName}");
    }
    
    private void ConfigureShadows(GraphicsPreset preset)
    {
        if (dayNightSystem.sunLight != null)
        {
            dayNightSystem.sunLight.shadows = preset.enableSoftShadows ? LightShadows.Soft : LightShadows.Hard;
        }
        
        if (dayNightSystem.moonLight != null)
        {
            dayNightSystem.moonLight.shadows = preset.enableSoftShadows ? LightShadows.Soft : LightShadows.Hard;
        }
        
        // Настройка расстояния теней
        QualitySettings.shadowDistance *= preset.shadowDistanceMultiplier;
    }
    
    private void ApplyAtmosphericEffects(GraphicsPreset preset)
    {
        if (dayNightSystem.atmosphericSettings != null)
        {
            // Применение насыщенности цвета
            dayNightSystem.atmosphericSettings.daySaturation *= preset.colorSaturationMultiplier;
            dayNightSystem.atmosphericSettings.nightSaturation *= preset.colorSaturationMultiplier;
        }
        
        // Настройка отражений
        if (!preset.enableRealTimeReflections)
        {
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }
        
        // Дополнительные настройки URP (если доступны)
        ApplyURPSettings(preset);
    }
    
    private void ApplyURPSettings(GraphicsPreset preset)
    {
        // Поиск URP Asset для дополнительных настроек
        var urpAsset = GraphicsSettings.defaultRenderPipeline;
        if (urpAsset != null)
        {
            // Здесь можно добавить специфичные для URP настройки
            // Например, через рефлексию или кастинг к конкретному типу URP Asset
        }
    }
    
    // Публичные методы для ручного применения пресетов
    public void ApplyLowPreset() => ApplyPreset(lowPreset);
    public void ApplyMediumPreset() => ApplyPreset(mediumPreset);
    public void ApplyHighPreset() => ApplyPreset(highPreset);
    public void ApplyUltraPreset() => ApplyPreset(ultraPreset);
    
    // Метод для создания пользовательского пресета
    public GraphicsPreset CreateCustomPreset(string name)
    {
        GraphicsPreset customPreset = new GraphicsPreset();
        customPreset.presetName = name;
        // Копируем настройки из текущего пресета
        if (currentPreset != null)
        {
            customPreset = JsonUtility.FromJson<GraphicsPreset>(JsonUtility.ToJson(currentPreset));
            customPreset.presetName = name;
        }
        return customPreset;
    }
    
    // Методы для runtime изменения качества
    private void OnValidate()
    {
        if (Application.isPlaying && autoDetectQuality)
        {
            ApplyPresetBasedOnQuality();
        }
    }
}