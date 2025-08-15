using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

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
    [Range(0f, 3f)] public float ambientIntensity = 1f;

    [Header("Атмосферные переходы")]
    [Tooltip("Длительность рассвета в часах")]
    [Range(0.5f, 3f)] public float dawnDuration = 1.5f;
    [Tooltip("Длительность заката в часах")]
    [Range(0.5f, 3f)] public float duskDuration = 1.5f;
    
    [Header("Цветовая коррекция")]
    [Tooltip("Насыщенность цветов днем")]
    [Range(0.5f, 1.5f)] public float daySaturation = 1.1f;
    [Tooltip("Насыщенность цветов ночью")]
    [Range(0.3f, 1.2f)] public float nightSaturation = 0.8f;
    [Tooltip("Температура цвета днем (теплый/холодный)")]
    [Range(-0.3f, 0.3f)] public float dayTemperature = 0.1f;
    [Tooltip("Температура цвета ночью")]
    [Range(-0.3f, 0.3f)] public float nightTemperature = -0.15f;
}

public class DayNightSystem : MonoBehaviour
{
    [Header("Время")]
    [Range(0, 24)] public float currentTime = 8.0f;
    public float timeSpeed = 0.5f;
    public int currentDay = 1;

    [Header("Временные границы (часы)")]
    [Tooltip("Когда начинается рассвет (например 5.0 = 5:00)")]
    public float dawnStart = 5.0f;
    [Tooltip("Когда начинается закат (например 19.5 = 19:30)")]
    public float duskStart = 19.5f;
    [Tooltip("Скорость сглаживания переходов")]
    [Range(0.1f, 10f)] public float transitionSpeed = 2f;

    [Header("Атмосферные настройки")]
    public AtmosphericSettings atmosphericSettings;

    [Header("Небо (если используется Skybox)")]
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
    [Range(0, 3)] public float maxSunIntensity = 1.8f;
    [Tooltip("Максимальная интенсивность луны")]
    [Range(0, 1)] public float maxMoonIntensity = 0.8f;
    [Space]
    [Tooltip("Мягкость теней солнца")]
    [Range(0, 1)] public float sunShadowSoftness = 0.3f;
    [Tooltip("Мягкость теней луны")]
    [Range(0, 1)] public float moonShadowSoftness = 0.7f;

    [Header("Звёзды (если используется Skybox)")]
    [Range(0, 1)] public float starsBrightness = 0.7f;

    [Header("Туман")]
    public bool enableFog = true;
    public Gradient fogColor;
    [Range(0, 0.1f)] public float fogDensityDay = 0.01f;
    [Range(0, 0.1f)] public float fogDensityNight = 0.035f;
    [Tooltip("Начальное расстояние тумана")]
    [Range(10f, 200f)] public float fogStartDistance = 50f;
    [Tooltip("Конечное расстояние тумана")]
    [Range(100f, 1000f)] public float fogEndDistance = 300f;

    [Header("События")]
    public List<TimeEvent> timeEvents = new List<TimeEvent>();
    public DayCompletedEvent onDayCompleted = new DayCompletedEvent();

    private int lastHour = -1;
    private int lastMinute = -1;
    private bool dayCompletedFired = false;

    // Плавные значения
    private Color currentSunColor;
    private Color currentMoonColor;
    private float currentSunIntensitySmooth;
    private float currentMoonIntensitySmooth;
    private Color currentAmbientColor;
    private Color currentFogColor;
    private float currentFogDensitySmooth;
    private float currentSaturation = 1f;
    private float currentTemperature = 0f;
    
    // Кэш для оптимизации
    private Color baseAmbientColor;
    private Volume globalVolume;

    private static readonly int SkyColorProperty = Shader.PropertyToID("_SkyColor");
    private static readonly int HorizonColorProperty = Shader.PropertyToID("_HorizonColor");
    private static readonly int GroundColorProperty = Shader.PropertyToID("_GroundColor");
    private static readonly int StarBrightnessProperty = Shader.PropertyToID("_StarBrightness");

    private void Start()
    {
        // Инициализация настроек освещения
        InitializeLightingSettings();
        
        // Инициализация атмосферных настроек для лунной голубой ночи
        InitializeAtmosphericSettings();
        
        // Если используется Skybox материал
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
        }

        // Установка режима окружающего освещения на Color
        RenderSettings.ambientMode = AmbientMode.Flat;
        
        // Поиск глобального Volume для post-processing эффектов
        globalVolume = FindObjectOfType<Volume>();

        float dayNorm = currentTime / 24f;
        currentSunColor = sunColor.Evaluate(dayNorm);
        currentMoonColor = moonColor.Evaluate(dayNorm);
        currentFogColor = fogColor.Evaluate(dayNorm);
        currentAmbientColor = atmosphericSettings.ambientLightColor.Evaluate(dayNorm);
        currentSunIntensitySmooth = 0f;
        currentMoonIntensitySmooth = 0f;
        currentFogDensitySmooth = (currentTime > dawnStart && currentTime < duskStart) ? fogDensityDay : fogDensityNight;

        UpdateTime(true);
    }

    private void InitializeLightingSettings()
    {
        // Настройка качества теней для атмосферного освещения
        if (sunLight != null)
        {
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.8f;
            sunLight.shadowBias = 0.05f;
            sunLight.shadowNormalBias = 0.4f;
        }

        if (moonLight != null)
        {
            moonLight.shadows = LightShadows.Soft;
            moonLight.shadowStrength = 0.6f;
            moonLight.shadowBias = 0.1f;
            moonLight.shadowNormalBias = 0.6f;
        }

        // Инициализация градиентов если они пустые
        InitializeDefaultGradients();
    }

    private void InitializeAtmosphericSettings()
    {
        // Инициализация атмосферных настроек для лунной голубой ночи (как в Ведьмак 3)
        if (atmosphericSettings == null)
        {
            atmosphericSettings = new AtmosphericSettings();
        }

        // Настройки для создания атмосферы лунной ночи
        atmosphericSettings.ambientIntensity = 1.2f;
        atmosphericSettings.dawnDuration = 1.5f;
        atmosphericSettings.duskDuration = 1.5f;
        atmosphericSettings.daySaturation = 1.1f;
        atmosphericSettings.nightSaturation = 0.7f; // Приглушенная насыщенность ночью
        atmosphericSettings.dayTemperature = 0.1f;
        atmosphericSettings.nightTemperature = -0.2f; // Более холодная температура для голубого оттенка
    }

    private void InitializeDefaultGradients()
    {
        if (atmosphericSettings.ambientLightColor == null || atmosphericSettings.ambientLightColor.colorKeys.Length == 0)
        {
            atmosphericSettings.ambientLightColor = CreateDefaultAmbientGradient();
        }

        if (sunColor == null || sunColor.colorKeys.Length == 0)
        {
            sunColor = CreateDefaultSunGradient();
        }

        if (moonColor == null || moonColor.colorKeys.Length == 0)
        {
            moonColor = CreateDefaultMoonGradient();
        }

        if (fogColor == null || fogColor.colorKeys.Length == 0)
        {
            fogColor = CreateDefaultFogGradient();
        }
    }

    private Gradient CreateDefaultAmbientGradient()
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        // Ночь (0:00) - глубокий лунный голубой (как в Ведьмак 3)
        colorKeys[0] = new GradientColorKey(new Color(0.08f, 0.15f, 0.28f), 0f);
        // Рассвет (6:00) - теплый оранжевый
        colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.6f, 0.4f), 0.25f);
        // День (12:00) - яркий теплый белый
        colorKeys[2] = new GradientColorKey(new Color(0.95f, 0.9f, 0.8f), 0.5f);
        // Закат (18:00) - теплый оранжево-розовый
        colorKeys[3] = new GradientColorKey(new Color(0.9f, 0.5f, 0.3f), 0.75f);
        // Ночь (24:00) - глубокий лунный голубой
        colorKeys[4] = new GradientColorKey(new Color(0.08f, 0.15f, 0.28f), 1f);

        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private Gradient CreateDefaultSunGradient()
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.3f, 0.6f), 0f);     // Ночь
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.7f, 0.4f), 0.25f);    // Рассвет
        colorKeys[2] = new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.5f);    // День
        colorKeys[3] = new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0.75f);    // Закат
        colorKeys[4] = new GradientColorKey(new Color(0.2f, 0.3f, 0.6f), 1f);     // Ночь

        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private Gradient CreateDefaultMoonGradient()
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        // Холодный лунный свет с голубоватым оттенком (как в Ведьмак 3: Каменные сердца)
        colorKeys[0] = new GradientColorKey(new Color(0.6f, 0.75f, 1f), 0f);       // Более насыщенный голубой
        colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.5f);      // Глубокий лунный голубой
        colorKeys[2] = new GradientColorKey(new Color(0.6f, 0.75f, 1f), 1f);       // Холодный голубоватый

        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private Gradient CreateDefaultFogGradient()
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        // Ночной туман с лунным голубоватым оттенком (атмосфера Ведьмак 3)
        colorKeys[0] = new GradientColorKey(new Color(0.12f, 0.18f, 0.35f), 0f);   // Глубокий лунный туман
        colorKeys[1] = new GradientColorKey(new Color(0.9f, 0.7f, 0.5f), 0.25f);   // Утренний туман
        colorKeys[2] = new GradientColorKey(new Color(0.8f, 0.85f, 0.9f), 0.5f);   // Дневной туман
        colorKeys[3] = new GradientColorKey(new Color(0.8f, 0.6f, 0.4f), 0.75f);   // Вечерний туман
        colorKeys[4] = new GradientColorKey(new Color(0.12f, 0.18f, 0.35f), 1f);   // Глубокий лунный туман

        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private void Update()
    {
        UpdateTime(false);
    }

    private void UpdateTime(bool force)
    {
        // Увеличиваем текущее время (формула сохранилась для совместимости
        currentTime += Time.deltaTime * timeSpeed / 86400.0f * 24.0f;

        if (currentTime >= 24.0f)
        {
            currentTime -= 24.0f;
            currentDay++;
            dayCompletedFired = false;

            foreach (var timeEvent in timeEvents)
                timeEvent.ResetTrigger();
        }

        int hour = Mathf.FloorToInt(currentTime);
        int minute = Mathf.FloorToInt((currentTime - hour) * 60);

        if (force || lastHour != hour || lastMinute != minute)
        {
            lastHour = hour;
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
        UpdateAmbientLighting();
        UpdateFog();
    }

    private void CheckTimeEvents(int hour, int minute)
    {
        foreach (var timeEvent in timeEvents)
        {
            if (!timeEvent.hasTriggered && timeEvent.CheckTime(hour, minute))
            {
                timeEvent.onTimeReached.Invoke();
                timeEvent.hasTriggered = true;
            }
        }
    }

    private void UpdateSkybox()
    {
        if (skyboxMaterial == null) return;

        float dayTime = currentTime / 24.0f;
        
        // Получаем прогресс дня для определения ночи
        float dayProgress = GetDayProgress();
        float nightProgress = 1.0f - dayProgress;

        // Обновляем основные цвета с учетом времени суток
        if (skyColor != null && skyboxMaterial.HasProperty("_SkyColor"))
        {
            skyboxMaterial.SetColor("_SkyColor", skyColor.Evaluate(dayTime));
        }
        
        if (horizonColor != null && skyboxMaterial.HasProperty("_HorizonColor"))
        {
            skyboxMaterial.SetColor("_HorizonColor", horizonColor.Evaluate(dayTime));
        }
        
        if (groundColor != null && skyboxMaterial.HasProperty("_GroundColor"))
        {
            skyboxMaterial.SetColor("_GroundColor", groundColor.Evaluate(dayTime));
        }

        // Звезды появляются только ночью (автоматически через шейдер)
        if (skyboxMaterial.HasProperty("_StarBrightness"))
        {
            skyboxMaterial.SetFloat("_StarBrightness", starsBrightness);
        }
        
        // Теплота атмосферы - теплее днем, прохладнее ночью
        if (skyboxMaterial.HasProperty("_WarmthFactor"))
        {
            float warmth = Mathf.Lerp(-0.1f, 0.2f, dayProgress);
            skyboxMaterial.SetFloat("_WarmthFactor", warmth);
        }
        
        // Интенсивность ночного неба
        if (skyboxMaterial.HasProperty("_NightSkyIntensity"))
        {
            float nightIntensity = Mathf.Lerp(1.0f, 1.4f, atmosphericSettings.nightSaturation);
            skyboxMaterial.SetFloat("_NightSkyIntensity", nightIntensity);
        }
    }

    private void UpdateLighting()
    {
        if (sunLight == null || moonLight == null) return;

        // Вращение источников света с улучшенной формулой
        float sunAngle = (currentTime / 24.0f) * 360.0f - 90f; // Смещение для правильного положения в полдень
        float moonAngle = sunAngle + 180.0f;

        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 30f, 0); // Небольшой наклон для более естественного света
        moonLight.transform.rotation = Quaternion.Euler(moonAngle, -15f, 0);

        float dayTime = currentTime / 24.0f;

        // Улучшенные расчеты интенсивности с учетом атмосферных переходов
        float dawnEnd = dawnStart + atmosphericSettings.dawnDuration;
        float duskEnd = duskStart + atmosphericSettings.duskDuration;

        // Солнечный свет
        float sunFactor = 0f;
        if (currentTime >= dawnStart && currentTime <= dawnEnd)
        {
            // Рассвет
            sunFactor = Mathf.SmoothStep(0f, 1f, (currentTime - dawnStart) / atmosphericSettings.dawnDuration);
        }
        else if (currentTime > dawnEnd && currentTime < duskStart)
        {
            // День
            sunFactor = 1f;
        }
        else if (currentTime >= duskStart && currentTime <= duskEnd)
        {
            // Закат
            sunFactor = Mathf.SmoothStep(1f, 0f, (currentTime - duskStart) / atmosphericSettings.duskDuration);
        }

        // Лунный свет (противоположный цикл)
        float moonFactor = 0f;
        if (currentTime >= duskEnd || currentTime <= dawnStart)
        {
            // Глубокая ночь
            moonFactor = 1f;
        }
        else if (currentTime > dawnStart && currentTime < dawnEnd)
        {
            // Утренние сумерки
            moonFactor = Mathf.SmoothStep(1f, 0f, (currentTime - dawnStart) / atmosphericSettings.dawnDuration);
        }
        else if (currentTime > duskStart && currentTime < duskEnd)
        {
            // Вечерние сумерки
            moonFactor = Mathf.SmoothStep(0f, 1f, (currentTime - duskStart) / atmosphericSettings.duskDuration);
        }

        float targetSunIntensity = sunFactor * maxSunIntensity;
        float targetMoonIntensity = moonFactor * maxMoonIntensity;

        // Плавные переходы
        currentSunColor = Color.Lerp(currentSunColor, sunColor.Evaluate(dayTime), Time.deltaTime * transitionSpeed);
        currentMoonColor = Color.Lerp(currentMoonColor, moonColor.Evaluate(dayTime), Time.deltaTime * transitionSpeed);
        currentSunIntensitySmooth = Mathf.Lerp(currentSunIntensitySmooth, targetSunIntensity, Time.deltaTime * transitionSpeed);
        currentMoonIntensitySmooth = Mathf.Lerp(currentMoonIntensitySmooth, targetMoonIntensity, Time.deltaTime * transitionSpeed);

        // Применение к источникам света
        sunLight.color = currentSunColor;
        sunLight.intensity = currentSunIntensitySmooth;
        moonLight.color = currentMoonColor;
        moonLight.intensity = currentMoonIntensitySmooth;

        // Настройка мягкости теней в зависимости от интенсивности
        if (sunLight.shadows != LightShadows.None)
        {
            sunLight.shadowStrength = Mathf.Lerp(0.3f, 0.8f, sunFactor);
        }
        
        if (moonLight.shadows != LightShadows.None)
        {
            moonLight.shadowStrength = Mathf.Lerp(0.1f, 0.6f, moonFactor);
        }
    }

    private void UpdateAmbientLighting()
    {
        if (atmosphericSettings.ambientLightColor == null) return;

        float dayTime = currentTime / 24.0f;
        
        // Базовый цвет окружающего освещения
        Color targetAmbientColor = atmosphericSettings.ambientLightColor.Evaluate(dayTime);
        
        // Применение температуры цвета
        float dayProgress = GetDayProgress();
        float targetTemperature = Mathf.Lerp(atmosphericSettings.nightTemperature, atmosphericSettings.dayTemperature, dayProgress);
        targetAmbientColor = ApplyColorTemperature(targetAmbientColor, targetTemperature);
        
        // Плавное изменение цвета
        currentAmbientColor = Color.Lerp(currentAmbientColor, targetAmbientColor, Time.deltaTime * transitionSpeed);
        
        // Применение интенсивности
        Color finalAmbientColor = currentAmbientColor * atmosphericSettings.ambientIntensity;
        
        // Установка окружающего освещения
        RenderSettings.ambientLight = finalAmbientColor;
    }

    private float GetDayProgress()
    {
        // Возвращает 0 ночью, 1 днем с плавными переходами
        if (currentTime >= dawnStart && currentTime <= duskStart)
        {
            return Mathf.SmoothStep(0f, 1f, MapTimeTo01Range(dawnStart, duskStart, currentTime));
        }
        return 0f;
    }

    private Color ApplyColorTemperature(Color baseColor, float temperature)
    {
        // Простая симуляция цветовой температуры
        if (temperature > 0)
        {
            // Теплее (больше красного/желтого)
            baseColor.r = Mathf.Min(1f, baseColor.r * (1f + temperature));
            baseColor.g = Mathf.Min(1f, baseColor.g * (1f + temperature * 0.5f));
        }
        else if (temperature < 0)
        {
            // Холоднее (больше синего)
            baseColor.b = Mathf.Min(1f, baseColor.b * (1f - temperature));
            baseColor.g = Mathf.Max(0f, baseColor.g * (1f + temperature * 0.3f));
        }
        
        return baseColor;
    }

    private void UpdateFog()
    {
        if (!enableFog)
        {
            RenderSettings.fog = false;
            return;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared; // Более реалистичный туман

        float dayTime = currentTime / 24.0f;
        float dayProgress = GetDayProgress();

        // Плавное смешивание плотности тумана
        float targetFogDensity = Mathf.Lerp(fogDensityNight, fogDensityDay, dayProgress);

        // Цвет тумана с учетом атмосферных условий
        Color targetFogColor = fogColor.Evaluate(dayTime);
        
        // Применение температуры цвета к туману
        float targetTemperature = Mathf.Lerp(atmosphericSettings.nightTemperature, atmosphericSettings.dayTemperature, dayProgress);
        targetFogColor = ApplyColorTemperature(targetFogColor, targetTemperature * 0.5f); // Меньшее влияние на туман

        // Плавные переходы
        currentFogColor = Color.Lerp(currentFogColor, targetFogColor, Time.deltaTime * transitionSpeed);
        currentFogDensitySmooth = Mathf.Lerp(currentFogDensitySmooth, targetFogDensity, Time.deltaTime * transitionSpeed);

        // Применение настроек тумана
        RenderSettings.fogColor = currentFogColor;
        RenderSettings.fogDensity = currentFogDensitySmooth;
        
        // Если используется Linear туман (для особых случаев)
        if (RenderSettings.fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;
        }
    }

    // Универсальная функция: маппит время суток в 0..1 в интервале [start, end].
    // Поддерживает интервалы, которые проходят через полночь (end < start).
    private float MapTimeTo01Range(float start, float end, float time)
    {
        start = Mathf.Repeat(start, 24f);
        end = Mathf.Repeat(end, 24f);
        time = Mathf.Repeat(time, 24f);

        if (Mathf.Approximately(start, end)) return 0f; // защищаем от деления на ноль

        if (start < end)
        {
            // простой случай, например 6 -> 18
            if (time < start || time > end) return 0f;
            return Mathf.InverseLerp(start, end, time);
        }
        else
        {
            // переход через полночь, например 21 -> 4.5
            float length = (24f - start) + end;
            float t = (time >= start) ? (time - start) : (time + (24f - start));
            t = Mathf.Clamp(t, 0f, length);
            return t / length;
        }
    }

    // Управление через API
    public void SetTime(float time)
    {
        currentTime = Mathf.Clamp(time, 0, 24);
        UpdateTime(true);
    }

    public void SetTimeSpeed(float speed)
    {
        timeSpeed = Mathf.Max(0, speed);
    }

    public void AddTimeEvent(int hour, int minute, UnityEvent onTimeReached)
    {
        TimeEvent timeEvent = new TimeEvent
        {
            hour = hour,
            minute = minute,
            onTimeReached = onTimeReached
        };
        timeEvents.Add(timeEvent);
    }

    public string GetTimeString()
    {
        int hour = Mathf.FloorToInt(currentTime);
        int minute = Mathf.FloorToInt((currentTime - hour) * 60);
        return string.Format("{0:00}:{1:00}", hour, minute);
    }
}
