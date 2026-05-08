using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using GameDevTV.Saving;

[Serializable]
public class TimeEvent
{
    public int hour;
    public int minute;
    public UnityEvent onTimeReached;
    public bool hasTriggered = false;

    public bool CheckTime(int currentHour, int currentMinute)
    {
        return currentHour == hour && currentMinute == minute;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}

[Serializable]
public class DayCompletedEvent : UnityEvent<int> { }

[Serializable]
public class AtmosphericSettings
{
    [Header("Освещение окружения")]
    [Tooltip("Цвета окружающего освещения в течение дня")]
    public Gradient ambientLightColor;

    [Tooltip("Интенсивность окружающего освещения")]
    [Range(0f, 3f)] public float ambientIntensity = 1.0f;

    [Header("Атмосферные переходы")]
    [Tooltip("Длительность рассвета в часах")]
    [Range(0.5f, 3f)] public float dawnDuration = 1.2f;
    [Tooltip("Длительность заката в часах")]
    [Range(0.5f, 3f)] public float duskDuration = 1.2f;

    [Header("Цветовая коррекция")]
    [Tooltip("Насыщенность цветов днем")]
    [Range(0.5f, 1.5f)] public float daySaturation = 1.05f;
    [Tooltip("Насыщенность цветов ночью")]
    [Range(0.3f, 1.2f)] public float nightSaturation = 0.75f;
    [Tooltip("Температура цвета днем (теплый/холодный)")]
    [Range(-0.3f, 0.3f)] public float dayTemperature = 0.08f;
    [Tooltip("Температура цвета ночью")]
    [Range(-0.3f, 0.3f)] public float nightTemperature = -0.12f;
}

/// <summary>
/// Система дня/ночи с визуальным стилем Cozy Lowpoly Dark Fantasy.
/// Тёплое мягкое освещение днём, уютное голубовато-фиолетовое — ночью.
/// Без грязных переходов и агрессивной насыщенности.
/// </summary>
public class DayNightSystem : MonoBehaviour, ISaveable
{
    [Header("Время")]
    [Range(0, 24)] public float currentTime = 8.0f;
    public float timeSpeed = 0.5f;
    public int currentDay = 1;

    [Header("Временные границы (часы)")]
    [Tooltip("Когда начинается рассвет (например 5.5 = 5:30)")]
    public float dawnStart = 5.5f;
    [Tooltip("Когда начинается закат (например 19.0 = 19:00)")]
    public float duskStart = 19.0f;
    [Tooltip("Скорость сглаживания переходов (рекомендуется 1.5–3)")]
    [Range(0.1f, 10f)] public float transitionSpeed = 2f;

    [Header("Атмосферные настройки")]
    public AtmosphericSettings atmosphericSettings;

    [Header("Небо (Skybox Material)")]
    public Material skyboxMaterial;
    public Gradient skyColor;
    public Gradient horizonColor;
    public Gradient groundColor;

    [Header("Освещение")]
    public Light sunLight;
    public Light moonLight;
    [Space]
    [Tooltip("Цвета солнечного света в течение дня")]
    public Gradient sunColor;
    [Tooltip("Цвета лунного света в течение ночи")]
    public Gradient moonColor;
    [Space]
    [Tooltip("Максимальная интенсивность солнца")]
    [Range(0, 3)] public float maxSunIntensity = 1.5f;
    [Tooltip("Максимальная интенсивность луны")]
    [Range(0, 1)] public float maxMoonIntensity = 0.55f;

    [Header("Звёзды")]
    [Range(0, 1)] public float starsBrightness = 0.85f;

    [Header("Туман (лёгкий, атмосферный)")]
    public bool enableFog = true;
    public Gradient fogColor;
    [Range(0, 0.05f)] public float fogDensityDay   = 0.004f;
    [Range(0, 0.05f)] public float fogDensityNight  = 0.012f;
    [Range(10f,  200f)] public float fogStartDistance = 60f;
    [Range(100f, 800f)] public float fogEndDistance   = 450f;

    [Header("События")]
    public List<TimeEvent> timeEvents = new List<TimeEvent>();
    public DayCompletedEvent onDayCompleted = new DayCompletedEvent();

    // ─── Приватные переменные ───────────────────────────────────────────────
    private int lastHour   = -1;
    private int lastMinute = -1;
    private bool dayCompletedFired = false;

    // Плавные значения (lerp targets)
    private Color currentSunColorSmooth;
    private Color currentMoonColorSmooth;
    private float currentSunIntensitySmooth;
    private float currentMoonIntensitySmooth;
    private Color currentAmbientSmooth;
    private Color currentFogColorSmooth;
    private float currentFogDensitySmooth;

    // ─── Lifecycle ──────────────────────────────────────────────────────────
    private void Start()
    {
        if (GetComponent<GameDevTV.Saving.SaveableEntity>() == null)
            gameObject.AddComponent<GameDevTV.Saving.SaveableEntity>();

        DontDestroyOnLoad(gameObject);

        // Singleton: уничтожаем дубликаты
        var existing = FindObjectsOfType<DayNightSystem>();
        if (existing.Length > 1)
        {
            foreach (var s in existing)
                if (s != this) { Destroy(gameObject); return; }
        }

        InitializeAtmosphericSettings();
        InitializeDefaultGradients();
        ConfigureLights();

        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;

        RenderSettings.ambientMode = AmbientMode.Flat;

        // Снапшот начального состояния (без lerp-задержки)
        float t = currentTime / 24f;
        currentSunColorSmooth      = sunColor   != null ? sunColor.Evaluate(t)    : Color.white;
        currentMoonColorSmooth     = moonColor  != null ? moonColor.Evaluate(t)   : new Color(0.6f, 0.75f, 1f);
        currentAmbientSmooth       = atmosphericSettings.ambientLightColor != null
                                     ? atmosphericSettings.ambientLightColor.Evaluate(t)
                                     : Color.grey;
        currentFogColorSmooth      = fogColor   != null ? fogColor.Evaluate(t)    : Color.grey;
        currentSunIntensitySmooth  = 0f;
        currentMoonIntensitySmooth = 0f;
        currentFogDensitySmooth    = (currentTime > dawnStart && currentTime < duskStart)
                                     ? fogDensityDay : fogDensityNight;

        UpdateAll(force: true);
    }

    private void Update() => UpdateAll(force: false);

    // ─── Основной цикл обновления ───────────────────────────────────────────
    private void UpdateAll(bool force)
    {
        // Движение времени
        currentTime += Time.deltaTime * timeSpeed / 86400f * 24f;
        if (currentTime >= 24f)
        {
            currentTime -= 24f;
            currentDay++;
            dayCompletedFired = false;
            foreach (var e in timeEvents) e.ResetTrigger();
        }

        int hour   = Mathf.FloorToInt(currentTime);
        int minute = Mathf.FloorToInt((currentTime - hour) * 60);

        if (force || lastHour != hour || lastMinute != minute)
        {
            lastHour   = hour;
            lastMinute = minute;
            CheckTimeEvents(hour, minute);

            if (!dayCompletedFired && hour == 23 && minute == 59)
            {
                onDayCompleted.Invoke(currentDay);
                dayCompletedFired = true;
            }
        }

        UpdateSkybox();
        UpdateLighting();
        UpdateAmbient();
        UpdateFog();
    }

    // ─── Skybox ─────────────────────────────────────────────────────────────
    private void UpdateSkybox()
    {
        if (skyboxMaterial == null) return;
        float t        = currentTime / 24f;
        float dayProg  = GetDayProgress();

        // Основные цвета неба
        SetSkyboxColor("_SkyColor",      skyColor,      t);
        SetSkyboxColor("_HorizonColor",  horizonColor,  t);
        SetSkyboxColor("_GroundColor",   groundColor,   t);

        // Звёзды: показываем ночью через _StarBrightness
        // Шейдер CozyDarkFantasySkybox определяет nightFactor сам через яркость _SkyColor,
        // но мы также управляем через _StarBrightness для гибкости
        if (skyboxMaterial.HasProperty("_StarBrightness"))
            skyboxMaterial.SetFloat("_StarBrightness", starsBrightness);

        // Warmth для старых шейдеров (если используется)
        if (skyboxMaterial.HasProperty("_WarmthFactor"))
            skyboxMaterial.SetFloat("_WarmthFactor", Mathf.Lerp(-0.08f, 0.12f, dayProg));

        // Динамический boost ночи (для CozyDarkFantasySkybox)
        if (skyboxMaterial.HasProperty("_NightBoost"))
        {
            float boost = Mathf.Lerp(1.6f, 1.0f, dayProg);
            skyboxMaterial.SetFloat("_NightBoost", boost);
        }

        // DynamicGI: пересчёт после смены цветов скайбокса
        DynamicGI.UpdateEnvironment();
    }

    private void SetSkyboxColor(string prop, Gradient grad, float t)
    {
        if (grad != null && skyboxMaterial.HasProperty(prop))
            skyboxMaterial.SetColor(prop, grad.Evaluate(t));
    }

    // ─── Направленный свет (Солнце / Луна) ─────────────────────────────────
    private void UpdateLighting()
    {
        if (sunLight == null || moonLight == null) return;

        float sunAngle  = (currentTime / 24f) * 360f - 90f;
        float moonAngle = sunAngle + 180f;

        sunLight.transform.rotation  = Quaternion.Euler(sunAngle,   25f,  0f);
        moonLight.transform.rotation = Quaternion.Euler(moonAngle, -10f,  0f);

        float t   = currentTime / 24f;
        float day = GetDayProgress();

        float dawnEnd = dawnStart + atmosphericSettings.dawnDuration;
        float duskEnd = duskStart + atmosphericSettings.duskDuration;

        // Солнечный фактор
        float sunFactor = 0f;
        if (currentTime >= dawnStart && currentTime <= dawnEnd)
            sunFactor = Mathf.SmoothStep(0f, 1f, (currentTime - dawnStart) / atmosphericSettings.dawnDuration);
        else if (currentTime > dawnEnd && currentTime < duskStart)
            sunFactor = 1f;
        else if (currentTime >= duskStart && currentTime <= duskEnd)
            sunFactor = Mathf.SmoothStep(1f, 0f, (currentTime - duskStart) / atmosphericSettings.duskDuration);

        // Лунный фактор (когда солнца нет)
        float moonFactor = 0f;
        if (currentTime >= duskEnd || currentTime <= dawnStart)
            moonFactor = 1f;
        else if (currentTime > dawnStart && currentTime < dawnEnd)
            moonFactor = Mathf.SmoothStep(1f, 0f, (currentTime - dawnStart) / atmosphericSettings.dawnDuration);
        else if (currentTime > duskStart && currentTime < duskEnd)
            moonFactor = Mathf.SmoothStep(0f, 1f, (currentTime - duskStart) / atmosphericSettings.duskDuration);

        float speed = Time.deltaTime * transitionSpeed;

        // Плавные цвета
        Color targetSun  = sunColor  != null ? sunColor.Evaluate(t)  : Color.white;
        Color targetMoon = moonColor != null ? moonColor.Evaluate(t) : new Color(0.65f, 0.78f, 1f);

        currentSunColorSmooth      = Color.Lerp(currentSunColorSmooth,      targetSun,              speed);
        currentMoonColorSmooth     = Color.Lerp(currentMoonColorSmooth,     targetMoon,             speed);
        currentSunIntensitySmooth  = Mathf.Lerp(currentSunIntensitySmooth,  sunFactor * maxSunIntensity,  speed);
        currentMoonIntensitySmooth = Mathf.Lerp(currentMoonIntensitySmooth, moonFactor * maxMoonIntensity, speed);

        sunLight.color     = currentSunColorSmooth;
        sunLight.intensity = currentSunIntensitySmooth;
        sunLight.shadowStrength = Mathf.Lerp(0.25f, 0.65f, sunFactor); // мягкие тени

        moonLight.color     = currentMoonColorSmooth;
        moonLight.intensity = currentMoonIntensitySmooth;
        moonLight.shadowStrength = Mathf.Lerp(0.05f, 0.4f, moonFactor);
    }

    // ─── Ambient (окружающий свет) ──────────────────────────────────────────
    private void UpdateAmbient()
    {
        if (atmosphericSettings?.ambientLightColor == null) return;

        float t       = currentTime / 24f;
        float dayProg = GetDayProgress();

        Color targetAmbient = atmosphericSettings.ambientLightColor.Evaluate(t);
        targetAmbient = ApplyTemperature(targetAmbient,
            Mathf.Lerp(atmosphericSettings.nightTemperature,
                       atmosphericSettings.dayTemperature, dayProg));

        currentAmbientSmooth = Color.Lerp(currentAmbientSmooth, targetAmbient,
                                          Time.deltaTime * transitionSpeed);

        RenderSettings.ambientLight = currentAmbientSmooth * atmosphericSettings.ambientIntensity;
    }

    // ─── Туман ──────────────────────────────────────────────────────────────
    private void UpdateFog()
    {
        if (!enableFog)
        {
            RenderSettings.fog = false;
            return;
        }

        RenderSettings.fog     = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        float t       = currentTime / 24f;
        float dayProg = GetDayProgress();
        float speed   = Time.deltaTime * transitionSpeed;

        Color targetFog = fogColor != null ? fogColor.Evaluate(t) : Color.grey;
        targetFog = ApplyTemperature(targetFog,
            Mathf.Lerp(atmosphericSettings.nightTemperature,
                       atmosphericSettings.dayTemperature, dayProg) * 0.4f);

        float targetDensity = Mathf.Lerp(fogDensityNight, fogDensityDay, dayProg);

        currentFogColorSmooth   = Color.Lerp(currentFogColorSmooth,   targetFog,     speed);
        currentFogDensitySmooth = Mathf.Lerp(currentFogDensitySmooth, targetDensity, speed);

        RenderSettings.fogColor   = currentFogColorSmooth;
        RenderSettings.fogDensity = currentFogDensitySmooth;
    }

    // ─── Утилиты ────────────────────────────────────────────────────────────

    /// <summary>Возвращает 0 ночью, 1 днём с плавными переходами.</summary>
    private float GetDayProgress()
    {
        if (currentTime >= dawnStart && currentTime <= duskStart)
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(dawnStart, duskStart, currentTime));
        return 0f;
    }

    private Color ApplyTemperature(Color c, float temp)
    {
        if (temp > 0f)
        {
            c.r = Mathf.Min(1f, c.r * (1f + temp));
            c.g = Mathf.Min(1f, c.g * (1f + temp * 0.4f));
        }
        else if (temp < 0f)
        {
            c.b = Mathf.Min(1f, c.b * (1f - temp));
            c.g = Mathf.Max(0f, c.g * (1f + temp * 0.25f));
        }
        return c;
    }

    private void CheckTimeEvents(int hour, int minute)
    {
        foreach (var e in timeEvents)
            if (!e.hasTriggered && e.CheckTime(hour, minute))
            { e.onTimeReached.Invoke(); e.hasTriggered = true; }
    }

    // ─── Инициализация дефолтных настроек ──────────────────────────────────

    private void ConfigureLights()
    {
        if (sunLight != null)
        {
            sunLight.shadows           = LightShadows.Soft;
            sunLight.shadowStrength    = 0.65f;
            sunLight.shadowBias        = 0.04f;
            sunLight.shadowNormalBias  = 0.35f;
        }
        if (moonLight != null)
        {
            moonLight.shadows           = LightShadows.Soft;
            moonLight.shadowStrength    = 0.4f;
            moonLight.shadowBias        = 0.08f;
            moonLight.shadowNormalBias  = 0.5f;
        }
    }

    private void InitializeAtmosphericSettings()
    {
        if (atmosphericSettings == null) atmosphericSettings = new AtmosphericSettings();
        atmosphericSettings.ambientIntensity   = 1.0f;
        atmosphericSettings.dawnDuration       = 1.2f;
        atmosphericSettings.duskDuration       = 1.2f;
        atmosphericSettings.daySaturation      = 1.05f;
        atmosphericSettings.nightSaturation    = 0.75f;
        atmosphericSettings.dayTemperature     = 0.08f;
        atmosphericSettings.nightTemperature   = -0.12f;
    }

    private void InitializeDefaultGradients()
    {
        if (IsGradientEmpty(atmosphericSettings.ambientLightColor))
            atmosphericSettings.ambientLightColor = MakeAmbientGradient();

        if (IsGradientEmpty(sunColor))   sunColor   = MakeSunGradient();
        if (IsGradientEmpty(moonColor))  moonColor  = MakeMoonGradient();
        if (IsGradientEmpty(fogColor))   fogColor   = MakeFogGradient();
        if (IsGradientEmpty(skyColor))   skyColor   = MakeSkyGradient();
        if (IsGradientEmpty(horizonColor)) horizonColor = MakeHorizonGradient();
        if (IsGradientEmpty(groundColor))  groundColor  = MakeGroundGradient();
    }

    private bool IsGradientEmpty(Gradient g) => g == null || g.colorKeys.Length == 0;

    // ═══════════════════════════════════════════════════════════════════════
    //  COZY LOWPOLY DARK FANTASY — Цветовые градиенты
    //  Стиль: тёплый янтарный день, нежный золотой закат, мягкая фиолетово-синяя ночь.
    //  Без агрессивных насыщенных оттенков, без «грязи».
    // ═══════════════════════════════════════════════════════════════════════

    private Gradient MakeAmbientGradient()
    {
        // ключи по времени: 0=полночь, 0.23=рассвет, 0.5=полдень, 0.79=закат, 1=полночь
        return MakeGradient(
            (0f,    new Color(0.06f, 0.07f, 0.18f)),  // полночь — тёмный индиго
            (0.22f, new Color(0.55f, 0.38f, 0.28f)),  // рассвет — тёплый терракот
            (0.33f, new Color(0.82f, 0.72f, 0.58f)),  // раннее утро — мягкий бежевый
            (0.50f, new Color(0.88f, 0.84f, 0.76f)),  // полдень — тёплый молочный
            (0.67f, new Color(0.82f, 0.72f, 0.58f)),  // послеполудень
            (0.79f, new Color(0.75f, 0.42f, 0.22f)),  // закат — янтарный
            (0.87f, new Color(0.22f, 0.16f, 0.30f)),  // сумерки — фиолетовый
            (1f,    new Color(0.06f, 0.07f, 0.18f))   // полночь
        );
    }

    private Gradient MakeSunGradient()
    {
        return MakeGradient(
            (0f,    new Color(0.18f, 0.22f, 0.5f)),   // ночь — не используется
            (0.22f, new Color(1f,   0.65f, 0.35f)),   // рассвет — персиковый
            (0.37f, new Color(1f,   0.92f, 0.75f)),   // утро — тёплый белый
            (0.50f, new Color(1f,   0.96f, 0.85f)),   // полдень — чистый
            (0.63f, new Color(1f,   0.92f, 0.75f)),   // день
            (0.79f, new Color(1f,   0.55f, 0.20f)),   // закат — глубокий янтарь
            (0.87f, new Color(0.6f, 0.35f, 0.55f)),   // сумерки — розово-фиолетовый
            (1f,    new Color(0.18f, 0.22f, 0.5f))
        );
    }

    private Gradient MakeMoonGradient()
    {
        return MakeGradient(
            (0f,    new Color(0.65f, 0.78f, 1.00f)),  // холодный лунно-голубой
            (0.5f,  new Color(0.58f, 0.72f, 0.98f)),  // чуть теплее
            (1f,    new Color(0.65f, 0.78f, 1.00f))
        );
    }

    private Gradient MakeFogGradient()
    {
        return MakeGradient(
            (0f,    new Color(0.10f, 0.12f, 0.28f)),  // ночной туман — тёмный индиго
            (0.22f, new Color(0.80f, 0.62f, 0.45f)),  // утренний — персиковый
            (0.37f, new Color(0.78f, 0.82f, 0.88f)),  // утро — серо-голубой
            (0.50f, new Color(0.80f, 0.83f, 0.88f)),  // день — светло-серый
            (0.63f, new Color(0.78f, 0.82f, 0.88f)),
            (0.79f, new Color(0.72f, 0.48f, 0.30f)),  // закат — янтарный туман
            (0.87f, new Color(0.18f, 0.14f, 0.28f)),  // сумерки
            (1f,    new Color(0.10f, 0.12f, 0.28f))
        );
    }

    private Gradient MakeSkyGradient()
    {
        // ВАЖНО: ночные значения должны быть ТЁМНЫМИ (яркость < 0.10)
        // чтобы шейдер правильно определял NightFactor и показывал звёзды.
        // Формула: dot(color, float3(0.299, 0.587, 0.114)) < 0.10 = ночь
        return MakeGradient(
            (0.00f, new Color(0.05f, 0.04f, 0.16f)),  // полночь — тёмный индиго       (lum≈0.05)
            (0.20f, new Color(0.08f, 0.06f, 0.20f)),  // 4:48 — ещё ночь               (lum≈0.07)
            (0.23f, new Color(0.30f, 0.18f, 0.30f)),  // рассвет — пурпурный           (lum≈0.23)
            (0.30f, new Color(0.38f, 0.52f, 0.78f)),  // утро — синее небо             (lum≈0.49)
            (0.42f, new Color(0.32f, 0.58f, 0.88f)),  // позднее утро                  (lum≈0.52)
            (0.50f, new Color(0.28f, 0.60f, 0.92f)),  // полдень — чистый голубой      (lum≈0.54)
            (0.62f, new Color(0.32f, 0.58f, 0.88f)),  // день
            (0.72f, new Color(0.45f, 0.38f, 0.65f)),  // предзакатный
            (0.80f, new Color(0.20f, 0.12f, 0.35f)),  // ранние сумерки                (lum≈0.15)
            (0.88f, new Color(0.08f, 0.06f, 0.20f)),  // поздние сумерки               (lum≈0.07)
            (1.00f, new Color(0.05f, 0.04f, 0.16f))   // полночь
        );
    }

    private Gradient MakeHorizonGradient()
    {
        return MakeGradient(
            (0.00f, new Color(0.10f, 0.10f, 0.28f)),  // ночной горизонт — тёмно-синий
            (0.20f, new Color(0.12f, 0.10f, 0.30f)),
            (0.23f, new Color(0.85f, 0.50f, 0.30f)),  // рассвет — тёплый оранжевый
            (0.30f, new Color(0.88f, 0.80f, 0.68f)),  // утро
            (0.50f, new Color(0.92f, 0.88f, 0.80f)),  // день — молочно-бежевый
            (0.70f, new Color(0.88f, 0.80f, 0.65f)),  // послеполудень
            (0.78f, new Color(1.00f, 0.52f, 0.18f)),  // закат — яркий янтарь
            (0.84f, new Color(0.55f, 0.22f, 0.40f)),  // сумерки — розово-фиолетовый
            (0.90f, new Color(0.14f, 0.10f, 0.30f)),  // ночь
            (1.00f, new Color(0.10f, 0.10f, 0.28f))
        );
    }

    private Gradient MakeGroundGradient()
    {
        return MakeGradient(
            (0.00f, new Color(0.03f, 0.03f, 0.08f)),  // ночь — почти чёрный
            (0.23f, new Color(0.18f, 0.12f, 0.12f)),  // рассвет
            (0.50f, new Color(0.25f, 0.28f, 0.22f)),  // день
            (0.80f, new Color(0.14f, 0.08f, 0.10f)),  // закат
            (1.00f, new Color(0.03f, 0.03f, 0.08f))
        );
    }

    // ─── Хелпер для построения Gradient ────────────────────────────────────
    private Gradient MakeGradient(params (float time, Color color)[] keys)
    {
        var colorKeys = new GradientColorKey[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            colorKeys[i] = new GradientColorKey(keys[i].color, keys[i].time);

        var alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        var g = new Gradient();
        g.SetKeys(colorKeys, alphaKeys);
        return g;
    }

    // ─── Публичное API ──────────────────────────────────────────────────────
    public void SetTime(float time)
    {
        currentTime = Mathf.Clamp(time, 0f, 24f);
        UpdateAll(force: true);
    }

    public void SetTimeSpeed(float speed) => timeSpeed = Mathf.Max(0f, speed);

    public void AddTimeEvent(int hour, int minute, UnityEvent onTimeReached)
    {
        timeEvents.Add(new TimeEvent { hour = hour, minute = minute, onTimeReached = onTimeReached });
    }

    public string GetTimeString()
    {
        int h = Mathf.FloorToInt(currentTime);
        int m = Mathf.FloorToInt((currentTime - h) * 60);
        return $"{h:00}:{m:00}";
    }

    // ─── ISaveable ──────────────────────────────────────────────────────────
    [System.Serializable]
    public struct DayNightSaveData
    {
        public float currentTime;
        public int   currentDay;
    }

    public object CaptureState() => new DayNightSaveData { currentTime = currentTime, currentDay = currentDay };

    public void RestoreState(object state)
    {
        DayNightSaveData d = state switch
        {
            DayNightSaveData data => data,
            Newtonsoft.Json.Linq.JObject jo => jo.ToObject<DayNightSaveData>(),
            _ => default
        };

        currentTime = d.currentTime;
        currentDay  = d.currentDay;
        UpdateAll(force: true);
    }
}
