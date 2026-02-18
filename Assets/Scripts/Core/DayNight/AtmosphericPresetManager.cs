using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class GraphicsPreset
{
    [Header("Основные настройки")]
    public string presetName = "Default";

    [Header("Освещение")]
    [Range(0.5f, 2f)]   public float lightIntensityMultiplier    = 1f;
    [Range(0.3f, 1.5f)] public float ambientIntensityMultiplier  = 1f;
    public bool enableSoftShadows = true;
    [Range(0.3f, 1f)]   public float shadowStrengthSun           = 0.65f;
    [Range(0.1f, 0.7f)] public float shadowStrengthMoon          = 0.40f;
    [Range(0.3f, 1.5f)] public float shadowDistanceMultiplier    = 1f;

    [Header("Туман")]
    public bool enableFog = true;
    [Range(0.3f, 2f)]   public float fogDensityMultiplier        = 1f;

    [Header("Атмосфера")]
    [Range(0.5f, 1.5f)] public float colorSaturationMultiplier   = 1f;
}

/// <summary>
/// Применяет пресет графики к DayNightSystem.
/// ВАЖНО: пресеты задают АБСОЛЮТНЫЕ множители, применяемые один раз при старте.
/// Повторный вызов ApplyPreset() корректно сбрасывает предыдущий.
/// </summary>
public class AtmosphericPresetManager : MonoBehaviour
{
    [Header("Пресеты")]
    public GraphicsPreset lowPreset;
    public GraphicsPreset mediumPreset;
    public GraphicsPreset highPreset;
    public GraphicsPreset ultraPreset;

    [Header("Авто-определение качества")]
    public bool autoDetectQuality = true;

    // ── Базовые значения DayNightSystem (сохраняем до применения пресета) ──
    private float _baseSunIntensity;
    private float _baseMoonIntensity;
    private float _baseAmbientIntensity;
    private float _baseFogDay;
    private float _baseFogNight;
    private float _baseDaySaturation;
    private float _baseNightSaturation;
    private float _baseShadowDistance;

    private DayNightSystem  _dns;
    private GraphicsPreset  _currentPreset;
    private bool            _baselinesSaved = false;

    private void Start()
    {
        _dns = GetComponent<DayNightSystem>();
        if (_dns == null)
        {
            Debug.LogError("[AtmosphericPresetManager] DayNightSystem не найден на этом GameObject!");
            return;
        }

        InitializePresets();
        SaveBaselines();

        if (autoDetectQuality)
            ApplyPresetByQuality();
        else
            ApplyPreset(mediumPreset);
    }

    // ─── Сохранение базовых значений до применения пресета ─────────────────
    private void SaveBaselines()
    {
        _baseSunIntensity     = _dns.maxSunIntensity;
        _baseMoonIntensity    = _dns.maxMoonIntensity;
        _baseAmbientIntensity = _dns.atmosphericSettings?.ambientIntensity ?? 1f;
        _baseFogDay           = _dns.fogDensityDay;
        _baseFogNight         = _dns.fogDensityNight;
        _baseDaySaturation    = _dns.atmosphericSettings?.daySaturation    ?? 1.05f;
        _baseNightSaturation  = _dns.atmosphericSettings?.nightSaturation  ?? 0.75f;
        _baseShadowDistance   = QualitySettings.shadowDistance;
        _baselinesSaved       = true;
    }

    // ─── Автовыбор по Unity Quality Level ──────────────────────────────────
    private void ApplyPresetByQuality()
    {
        int lvl   = QualitySettings.GetQualityLevel();
        int total = QualitySettings.names.Length;

        GraphicsPreset selected =
            lvl == 0                        ? lowPreset    :
            lvl <= total / 3                ? mediumPreset :
            lvl <= (total * 2) / 3          ? highPreset   :
                                              ultraPreset;

        ApplyPreset(selected);
    }

    // ─── Применение пресета ─────────────────────────────────────────────────
    public void ApplyPreset(GraphicsPreset preset)
    {
        if (preset == null || _dns == null) return;
        if (!_baselinesSaved) SaveBaselines();

        _currentPreset = preset;

        // Освещение — применяем к БАЗОВЫМ значениям (не накапливаем)
        _dns.maxSunIntensity  = _baseSunIntensity  * preset.lightIntensityMultiplier;
        _dns.maxMoonIntensity = _baseMoonIntensity * preset.lightIntensityMultiplier;

        if (_dns.atmosphericSettings != null)
        {
            _dns.atmosphericSettings.ambientIntensity  = _baseAmbientIntensity
                                                        * preset.ambientIntensityMultiplier;
            _dns.atmosphericSettings.daySaturation     = _baseDaySaturation
                                                        * preset.colorSaturationMultiplier;
            _dns.atmosphericSettings.nightSaturation   = _baseNightSaturation
                                                        * preset.colorSaturationMultiplier;
        }

        // Тени
        ConfigureShadows(preset);

        // Туман
        _dns.fogDensityDay   = _baseFogDay   * preset.fogDensityMultiplier;
        _dns.fogDensityNight = _baseFogNight * preset.fogDensityMultiplier;
        _dns.enableFog       = preset.enableFog;

        // Отражения
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

        Debug.Log($"[AtmosphericPresetManager] Применён пресет: {preset.presetName}");
    }

    private void ConfigureShadows(GraphicsPreset preset)
    {
        LightShadows mode = preset.enableSoftShadows ? LightShadows.Soft : LightShadows.Hard;

        if (_dns.sunLight != null)
        {
            _dns.sunLight.shadows        = mode;
            _dns.sunLight.shadowStrength = preset.shadowStrengthSun;
        }

        if (_dns.moonLight != null)
        {
            _dns.moonLight.shadows        = mode;
            _dns.moonLight.shadowStrength = preset.shadowStrengthMoon;
        }

        QualitySettings.shadowDistance = _baseShadowDistance * preset.shadowDistanceMultiplier;
    }

    // ─── Инициализация дефолтных пресетов ───────────────────────────────────
    // Все значения подобраны под стиль Cozy Lowpoly Dark Fantasy.
    // Ключевые принципы:
    //   - Тени мягче, чем в реализме (lowpoly плохо выглядит с резкими тенями)
    //   - Туман лёгкий — не заливает сцену серостью
    //   - Ambient не перегружен — даёт материалам "дышать"
    private void InitializePresets()
    {
        // ── Low ────────────────────────────────────────────────────────────
        if (IsDefault(lowPreset))
        {
            lowPreset.presetName                 = "Low";
            lowPreset.lightIntensityMultiplier   = 0.90f;
            lowPreset.ambientIntensityMultiplier = 1.10f;  // чуть светлее без теней
            lowPreset.enableSoftShadows          = false;
            lowPreset.shadowStrengthSun          = 0.50f;
            lowPreset.shadowStrengthMoon         = 0.20f;
            lowPreset.shadowDistanceMultiplier   = 0.55f;
            lowPreset.enableFog                  = true;
            lowPreset.fogDensityMultiplier       = 0.75f;  // лёгкий туман даже на Low
            lowPreset.colorSaturationMultiplier  = 0.95f;
        }

        // ── Medium ─────────────────────────────────────────────────────────
        if (IsDefault(mediumPreset))
        {
            mediumPreset.presetName                 = "Medium";
            mediumPreset.lightIntensityMultiplier   = 1.00f;
            mediumPreset.ambientIntensityMultiplier = 1.00f;
            mediumPreset.enableSoftShadows          = true;
            mediumPreset.shadowStrengthSun          = 0.60f;
            mediumPreset.shadowStrengthMoon         = 0.35f;
            mediumPreset.shadowDistanceMultiplier   = 0.80f;
            mediumPreset.enableFog                  = true;
            mediumPreset.fogDensityMultiplier       = 1.00f;
            mediumPreset.colorSaturationMultiplier  = 1.00f;
        }

        // ── High ───────────────────────────────────────────────────────────
        if (IsDefault(highPreset))
        {
            highPreset.presetName                 = "High";
            highPreset.lightIntensityMultiplier   = 1.05f;
            highPreset.ambientIntensityMultiplier = 0.95f;
            highPreset.enableSoftShadows          = true;
            highPreset.shadowStrengthSun          = 0.65f;
            highPreset.shadowStrengthMoon         = 0.40f;
            highPreset.shadowDistanceMultiplier   = 1.00f;
            highPreset.enableFog                  = true;
            highPreset.fogDensityMultiplier       = 1.10f;
            highPreset.colorSaturationMultiplier  = 1.05f;
        }

        // ── Ultra ──────────────────────────────────────────────────────────
        if (IsDefault(ultraPreset))
        {
            ultraPreset.presetName                 = "Ultra";
            ultraPreset.lightIntensityMultiplier   = 1.10f;
            ultraPreset.ambientIntensityMultiplier = 0.90f;
            ultraPreset.enableSoftShadows          = true;
            ultraPreset.shadowStrengthSun          = 0.68f;
            ultraPreset.shadowStrengthMoon         = 0.45f;
            ultraPreset.shadowDistanceMultiplier   = 1.30f;
            ultraPreset.enableFog                  = true;
            ultraPreset.fogDensityMultiplier       = 1.20f;
            ultraPreset.colorSaturationMultiplier  = 1.08f;
        }
    }

    private bool IsDefault(GraphicsPreset p) => p == null || p.presetName == "Default";

    // ─── Публичное API ───────────────────────────────────────────────────────
    public void ApplyLow()    => ApplyPreset(lowPreset);
    public void ApplyMedium() => ApplyPreset(mediumPreset);
    public void ApplyHigh()   => ApplyPreset(highPreset);
    public void ApplyUltra()  => ApplyPreset(ultraPreset);

    public GraphicsPreset GetCurrentPreset() => _currentPreset;

    /// <summary>Создаёт кастомный пресет на основе текущего.</summary>
    public GraphicsPreset CreateCustomPreset(string name)
    {
        var src  = _currentPreset ?? mediumPreset;
        var copy = JsonUtility.FromJson<GraphicsPreset>(JsonUtility.ToJson(src));
        copy.presetName = name;
        return copy;
    }
}
