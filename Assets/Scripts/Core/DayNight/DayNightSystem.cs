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

    [Tooltip("Начальное состояние времени при запуске новой игровой сессии. " +
             "После запуска это значение больше не переопределяет состояние времени. " +
             "Снять паузу можно только явным вызовом ResumeTime() или SetTimePaused(false).")]
    [SerializeField] private bool startTimePaused = false;

    [Tooltip("Текущее состояние паузы времени. Сохраняется между сценами и в сохранении игры.")]
    [SerializeField] private bool timePaused = false;

    // Состояние паузы живёт отдельно от конкретного экземпляра сцены.
    // Это важно, если при загрузке новой сцены создаётся новый DayNightSystem:
    // его StartTimePaused не должен заново переопределять состояние,
    // которое уже было изменено квестом.
    private static bool sessionTimePaused;
    private static bool sessionTimePausedInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        sessionTimePaused = false;
        sessionTimePausedInitialized = false;
    }

    /// <summary>
    /// Возвращает true, если игровое время сейчас остановлено.
    /// Удобно для других систем, которым нужно проверить состояние времени.
    /// </summary>
    public bool IsTimePaused => timePaused;

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
    [Range(0, 3)] public float maxSunIntensity = 1.9f;
    [Tooltip("Максимальная интенсивность луны")]
    [Range(0, 1)] public float maxMoonIntensity = 0.22f;

    [Header("Звёзды")]
    [Range(0, 1)] public float starsBrightness = 1.0f;

    // Визуальные параметры skybox. НЕ влияют на игровое время, события или сохранения.
    [Header("Визуальная атмосфера")]
    [Range(0f, 1f)] public float skyAtmosphereIntensity = 1f;
    [Range(0f, 1f)] public float celestialGlow = 1f;
    [Range(0f, 1f)] public float duskGlow = 1f;

    // Не сериализуются: используются только для дешёвой оптимизации DynamicGI.
    private Color _lastGIColor = Color.clear;
    private float _giTimer;

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

        // StartTimePaused используется ТОЛЬКО один раз — при старте новой
        // игровой сессии. При смене сцены он больше никогда не переопределяет
        // состояние, установленное PauseTime / ResumeTime / SetTimePaused.
        if (!sessionTimePausedInitialized)
        {
            sessionTimePaused = startTimePaused;
            sessionTimePausedInitialized = true;
        }

        timePaused = sessionTimePaused;

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
        // Движение времени.
        // При паузе само игровое время и день не меняются, но визуальная часть
        // системы продолжает обновляться, поэтому состояние освещения остаётся корректным.
        if (!timePaused)
        {
            currentTime += Time.deltaTime * timeSpeed / 86400f * 24f;
            if (currentTime >= 24f)
            {
                currentTime -= 24f;
                currentDay++;
                dayCompletedFired = false;
                foreach (var e in timeEvents) e.ResetTrigger();
            }
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

        float t = currentTime / 24f;

        // Это ТОЛЬКО визуальные коэффициенты. Игровое время и его логика не меняются.
        float dawnEnd = dawnStart + atmosphericSettings.dawnDuration;
        float duskEnd = duskStart + atmosphericSettings.duskDuration;

        float dawn = 0f;
        if (currentTime >= dawnStart && currentTime <= dawnEnd)
            dawn = Mathf.SmoothStep(1f, 0f, (currentTime - dawnStart) / atmosphericSettings.dawnDuration);

        float dusk = 0f;
        if (currentTime >= duskStart && currentTime <= duskEnd)
            dusk = Mathf.SmoothStep(0f, 1f, (currentTime - duskStart) / atmosphericSettings.duskDuration);

        float night = 0f;
        if (currentTime < dawnStart)
            night = 1f;
        else if (currentTime >= dawnStart && currentTime < dawnEnd)
            night = Mathf.SmoothStep(1f, 0f, (currentTime - dawnStart) / atmosphericSettings.dawnDuration);
        else if (currentTime > duskStart && currentTime < duskEnd)
            night = Mathf.SmoothStep(0f, 1f, (currentTime - duskStart) / atmosphericSettings.duskDuration);
        else if (currentTime >= duskEnd)
            night = 1f;

        float day = 1f - Mathf.Max(dawn, dusk, night);

        // Основные цвета.
        SetSkyboxColor("_SkyColor", skyColor, t);
        SetSkyboxColor("_HorizonColor", horizonColor, t);
        SetSkyboxColor("_GroundColor", groundColor, t);

        if (skyboxMaterial.HasProperty("_MidSkyColor"))
        {
            Color upper = skyColor != null ? skyColor.Evaluate(t) : Color.blue;
            Color horizon = horizonColor != null ? horizonColor.Evaluate(t) : Color.gray;
            Color mid = Color.Lerp(upper, horizon, 0.20f);
            skyboxMaterial.SetColor("_MidSkyColor", mid);
        }

        if (skyboxMaterial.HasProperty("_StarBrightness"))
            skyboxMaterial.SetFloat("_StarBrightness", starsBrightness);

        if (skyboxMaterial.HasProperty("_NightFactor"))
            skyboxMaterial.SetFloat("_NightFactor", night);

        if (skyboxMaterial.HasProperty("_DawnFactor"))
            skyboxMaterial.SetFloat("_DawnFactor", dawn);

        if (skyboxMaterial.HasProperty("_DuskFactor"))
            skyboxMaterial.SetFloat("_DuskFactor", dusk);

        if (skyboxMaterial.HasProperty("_DayIntensity"))
            skyboxMaterial.SetFloat("_DayIntensity", day);

        if (skyboxMaterial.HasProperty("_WarmthFactor"))
        {
            // Тёплый только рассвет/закат. В полдень цвет не превращается в сепию.
            float golden = Mathf.Max(dawn, dusk);
            skyboxMaterial.SetFloat("_WarmthFactor", golden);
        }

        if (skyboxMaterial.HasProperty("_NightBoost"))
            skyboxMaterial.SetFloat("_NightBoost", Mathf.Lerp(1.0f, 2.05f, night));

        if (sunLight != null && skyboxMaterial.HasProperty("_SunDirection"))
            skyboxMaterial.SetVector("_SunDirection", -sunLight.transform.forward);

        if (moonLight != null && skyboxMaterial.HasProperty("_MoonDirection"))
            skyboxMaterial.SetVector("_MoonDirection", -moonLight.transform.forward);

        if (skyboxMaterial.HasProperty("_CelestialGlow"))
            skyboxMaterial.SetFloat("_CelestialGlow", celestialGlow);

        if (skyboxMaterial.HasProperty("_DuskGlow"))
            skyboxMaterial.SetFloat("_DuskGlow", duskGlow);

        if (skyboxMaterial.HasProperty("_AtmosphereIntensity"))
            skyboxMaterial.SetFloat("_AtmosphereIntensity", skyAtmosphereIntensity);

        _giTimer += Time.deltaTime;
        Color giColor = skyColor != null ? skyColor.Evaluate(t) : Color.gray;
        if (_giTimer >= 0.25f || ColorDifference(_lastGIColor, giColor) > 0.018f)
        {
            DynamicGI.UpdateEnvironment();
            _lastGIColor = giColor;
            _giTimer = 0f;
        }
    }

    private float GetGoldenHourFactor()
    {
        if (atmosphericSettings == null) return 0f;

        float dawnEnd = dawnStart + atmosphericSettings.dawnDuration;
        float duskEnd = duskStart + atmosphericSettings.duskDuration;

        float dawn = 0f;
        if (currentTime >= dawnStart && currentTime <= dawnEnd)
            dawn = Mathf.SmoothStep(0f, 1f, (currentTime - dawnStart) / atmosphericSettings.dawnDuration);

        float dusk = 0f;
        if (currentTime >= duskStart && currentTime <= duskEnd)
            dusk = Mathf.SmoothStep(1f, 0f, (currentTime - duskStart) / atmosphericSettings.duskDuration);

        return Mathf.Max(dawn * (1f - dawn), dusk * (1f - dusk)) * 4f;
    }

    private float ColorDifference(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
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
        float golden = GetGoldenHourFactor();
        sunLight.shadowStrength = Mathf.Lerp(0.12f, 0.72f, sunFactor);
        if (golden > 0f)
            sunLight.shadowStrength = Mathf.Lerp(sunLight.shadowStrength, 0.62f, golden * 0.65f);

        moonLight.color     = currentMoonColorSmooth;
        moonLight.intensity = currentMoonIntensitySmooth;
        moonLight.shadowStrength = Mathf.Lerp(0.02f, 0.28f, moonFactor);
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

        float visualAmbient = Mathf.Lerp(0.30f, 0.72f, dayProg);
        RenderSettings.ambientLight = currentAmbientSmooth * atmosphericSettings.ambientIntensity * visualAmbient;
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
        atmosphericSettings.nightSaturation    = 0.62f;
        atmosphericSettings.dayTemperature     = 0.03f;
        atmosphericSettings.nightTemperature   = -0.18f;
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
        return MakeGradient(
            (0.00f, new Color(0.012f, 0.015f, 0.040f)),
            (0.18f, new Color(0.025f, 0.018f, 0.065f)),
            (0.23f, new Color(0.16f, 0.035f, 0.055f)),
            (0.28f, new Color(0.55f, 0.22f, 0.10f)),
            (0.34f, new Color(0.62f, 0.65f, 0.68f)),
            (0.50f, new Color(0.72f, 0.76f, 0.78f)),
            (0.66f, new Color(0.62f, 0.64f, 0.65f)),
            (0.76f, new Color(0.55f, 0.16f, 0.07f)),
            (0.82f, new Color(0.16f, 0.025f, 0.075f)),
            (0.90f, new Color(0.020f, 0.014f, 0.050f)),
            (1.00f, new Color(0.012f, 0.015f, 0.040f))
        );
    }

    private Gradient MakeSunGradient()
    {
        return MakeGradient(
            (0.00f, new Color(0.18f, 0.24f, 0.58f)),
            (0.18f, new Color(0.70f, 0.28f, 0.16f)),
            (0.23f, new Color(1.00f, 0.38f, 0.12f)),
            (0.30f, new Color(1.00f, 0.72f, 0.42f)),
            (0.42f, new Color(1.00f, 0.95f, 0.82f)),
            (0.50f, new Color(1.00f, 0.98f, 0.90f)),
            (0.62f, new Color(1.00f, 0.93f, 0.78f)),
            (0.75f, new Color(1.00f, 0.62f, 0.25f)),
            (0.81f, new Color(1.00f, 0.30f, 0.08f)),
            (0.88f, new Color(0.58f, 0.28f, 0.62f)),
            (1.00f, new Color(0.18f, 0.24f, 0.58f))
        );
    }

    private Gradient MakeMoonGradient()
    {
        return MakeGradient(
            (0.00f, new Color(0.58f, 0.70f, 1.00f)),
            (0.25f, new Color(0.68f, 0.78f, 1.00f)),
            (0.50f, new Color(0.74f, 0.82f, 1.00f)),
            (0.75f, new Color(0.64f, 0.74f, 1.00f)),
            (1.00f, new Color(0.58f, 0.70f, 1.00f))
        );
    }

    private Gradient MakeFogGradient()
    {
        return MakeGradient(
            (0.00f, new Color(0.045f, 0.055f, 0.16f)),
            (0.16f, new Color(0.07f, 0.055f, 0.18f)),
            (0.23f, new Color(0.42f, 0.16f, 0.16f)),
            (0.30f, new Color(0.78f, 0.47f, 0.31f)),
            (0.40f, new Color(0.72f, 0.77f, 0.84f)),
            (0.58f, new Color(0.78f, 0.82f, 0.88f)),
            (0.75f, new Color(0.86f, 0.38f, 0.18f)),
            (0.82f, new Color(0.30f, 0.10f, 0.23f)),
            (0.92f, new Color(0.055f, 0.045f, 0.15f)),
            (1.00f, new Color(0.045f, 0.055f, 0.16f))
        );
    }

    private Gradient MakeSkyGradient()
    {
        return MakeGradient(
            (0.00f, new Color(0.008f, 0.010f, 0.035f)),
            (0.18f, new Color(0.018f, 0.012f, 0.060f)),
            (0.22f, new Color(0.070f, 0.020f, 0.085f)),
            (0.25f, new Color(0.30f, 0.070f, 0.115f)),
            (0.30f, new Color(0.30f, 0.34f, 0.55f)),
            (0.38f, new Color(0.16f, 0.43f, 0.82f)),
            (0.50f, new Color(0.16f, 0.50f, 0.96f)),
            (0.62f, new Color(0.18f, 0.46f, 0.88f)),
            (0.72f, new Color(0.32f, 0.20f, 0.48f)),
            (0.77f, new Color(0.42f, 0.045f, 0.12f)),
            (0.81f, new Color(0.20f, 0.025f, 0.095f)),
            (0.88f, new Color(0.035f, 0.012f, 0.055f)),
            (1.00f, new Color(0.008f, 0.010f, 0.035f))
        );
    }

    private Gradient MakeHorizonGradient()
    {
        return MakeGradient(
            (0.00f, new Color(0.025f, 0.025f, 0.075f)),
            (0.18f, new Color(0.045f, 0.025f, 0.095f)),
            (0.22f, new Color(0.30f, 0.055f, 0.10f)),
            (0.245f, new Color(1.00f, 0.16f, 0.025f)),
            (0.275f, new Color(1.00f, 0.48f, 0.08f)),
            (0.31f, new Color(1.00f, 0.76f, 0.34f)),
            (0.38f, new Color(0.84f, 0.82f, 0.74f)),
            (0.50f, new Color(0.78f, 0.82f, 0.84f)),
            (0.66f, new Color(0.72f, 0.75f, 0.78f)),
            (0.75f, new Color(1.00f, 0.22f, 0.035f)),
            (0.785f, new Color(1.00f, 0.42f, 0.06f)),
            (0.82f, new Color(0.48f, 0.055f, 0.18f)),
            (0.89f, new Color(0.065f, 0.018f, 0.085f)),
            (1.00f, new Color(0.025f, 0.025f, 0.075f))
        );
    }

    private Gradient MakeGroundGradient()
    {
        return MakeGradient(
            (0.00f, new Color(0.006f, 0.006f, 0.018f)),
            (0.20f, new Color(0.018f, 0.008f, 0.028f)),
            (0.25f, new Color(0.085f, 0.018f, 0.022f)),
            (0.50f, new Color(0.16f, 0.18f, 0.15f)),
            (0.66f, new Color(0.14f, 0.13f, 0.11f)),
            (0.78f, new Color(0.065f, 0.008f, 0.022f)),
            (1.00f, new Color(0.006f, 0.006f, 0.018f))
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

    /// <summary>
    /// Ставит игровое время на паузу.
    /// Можно напрямую вызывать из UnityEvent / Objective Reactor.
    /// </summary>
    public void PauseTime()
    {
        timePaused = true;
        sessionTimePaused = true;
        sessionTimePausedInitialized = true;
    }

    /// <summary>
    /// Снимает игровое время с паузы.
    /// Можно напрямую вызывать из UnityEvent / Objective Reactor.
    /// </summary>
    public void ResumeTime()
    {
        timePaused = false;
        sessionTimePaused = false;
        sessionTimePausedInitialized = true;
    }

    /// <summary>
    /// Устанавливает состояние паузы явно.
    /// true = остановить время, false = продолжить.
    /// Удобно для UnityEvent и других систем.
    /// </summary>
    public void SetTimePaused(bool paused)
    {
        timePaused = paused;
        sessionTimePaused = paused;
        sessionTimePausedInitialized = true;
    }

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
        public bool  timePaused;
    }

    public object CaptureState() => new DayNightSaveData
    {
        currentTime = currentTime,
        currentDay  = currentDay,
        timePaused  = timePaused
    };

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
        timePaused  = d.timePaused;

        // Сохранённое состояние имеет приоритет над настройкой StartTimePaused
        // и должно пережить последующие переходы между сценами.
        sessionTimePaused = timePaused;
        sessionTimePausedInitialized = true;

        UpdateAll(force: true);
    }
}
