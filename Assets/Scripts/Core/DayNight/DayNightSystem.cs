using UnityEngine;
using UnityEngine.Events;
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

public class DayNightSystem : MonoBehaviour
{
    [Header("Время")]
    [Range(0, 24)] public float currentTime = 8.0f;
    public float timeSpeed = 0.5f;
    public int currentDay = 1;

    [Header("Временные границы (часы)")]
    [Tooltip("Когда начинается рассвет (например 4.5 = 4:30)")]
    public float dawnStart = 4.5f;
    [Tooltip("Когда начинается закат / темнеть (например 21 = 21:00)")]
    public float duskStart = 21f;
    [Tooltip("Скорость сглаживания переходов")]
    [Range(0.1f, 10f)] public float transitionSpeed = 2f;

    [Header("Небо")]
    public Material skyboxMaterial;
    public Gradient skyColor;
    public Gradient horizonColor;
    public Gradient groundColor;

    [Header("Свет")]
    public Light sunLight;
    public Light moonLight;
    public Gradient sunColor;
    public Gradient moonColor;
    [Range(0, 2)] public float sunIntensity = 1.2f;
    [Range(0, 2)] public float moonIntensity = 0.4f;

    [Header("Звёзды")]
    [Range(0, 1)] public float starsBrightness = 0.7f;

    [Header("Туман")]
    public bool enableFog = true;
    public Gradient fogColor;
    [Range(0, 0.05f)] public float fogDensityDay = 0.008f;
    [Range(0, 0.05f)] public float fogDensityNight = 0.015f;

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
    private Color currentFogColor;
    private float currentFogDensitySmooth;

    private static readonly int SkyColorProperty = Shader.PropertyToID("_SkyColor");
    private static readonly int HorizonColorProperty = Shader.PropertyToID("_HorizonColor");
    private static readonly int GroundColorProperty = Shader.PropertyToID("_GroundColor");
    private static readonly int StarBrightnessProperty = Shader.PropertyToID("_StarBrightness");

    private void Start()
    {
        if (skyboxMaterial == null)
        {
            Debug.LogError("Skybox material is not assigned!");
            return;
        }

        RenderSettings.skybox = skyboxMaterial;

        float dayNorm = currentTime / 24f;
        currentSunColor = sunColor.Evaluate(dayNorm);
        currentMoonColor = moonColor.Evaluate(dayNorm);
        currentFogColor = fogColor.Evaluate(dayNorm);
        currentSunIntensitySmooth = 0f;
        currentMoonIntensitySmooth = 0f;
        currentFogDensitySmooth = (currentTime > dawnStart && currentTime < duskStart) ? fogDensityDay : fogDensityNight;

        UpdateTime(true);
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
        float dayTime = currentTime / 24.0f;

        skyboxMaterial.SetColor(SkyColorProperty, skyColor.Evaluate(dayTime));
        skyboxMaterial.SetColor(HorizonColorProperty, horizonColor.Evaluate(dayTime));
        skyboxMaterial.SetColor(GroundColorProperty, groundColor.Evaluate(dayTime));

        // Звёзды: плавное появление после duskStart и исчезание к dawnStart (учитывает переход через полночь)
        float starFactor = MapTimeTo01Range(duskStart, dawnStart, currentTime); // 0..1 over night period
        float stars = Mathf.SmoothStep(0f, 1f, starFactor) * starsBrightness;
        skyboxMaterial.SetFloat(StarBrightnessProperty, stars);
    }

    private void UpdateLighting()
    {
        if (sunLight == null || moonLight == null) return;

        // Вращение источников света (простейшая модель по времени суток)
        float sunAngle = (currentTime / 24.0f) * 360.0f;
        float moonAngle = sunAngle + 180.0f;

        sunLight.transform.rotation = Quaternion.Euler(sunAngle - 90.0f, 0, 0);
        moonLight.transform.rotation = Quaternion.Euler(moonAngle - 90.0f, 0, 0);

        float dayTime = currentTime / 24.0f;

        // dayFactor: 0 в начале рассвета (dawnStart), 1 в начале заката (duskStart)
        float dayFactor = MapTimeTo01Range(dawnStart, duskStart, currentTime); // 0..1 through the "day" span
        float normalizedSunIntensity = Mathf.Sin(Mathf.Clamp01(dayFactor) * Mathf.PI) * sunIntensity;

        // moonFactor: противоположный интервал (duskStart -> dawnStart через полночь)
        float moonFactor = MapTimeTo01Range(duskStart, dawnStart, currentTime);
        float normalizedMoonIntensity = Mathf.Sin(Mathf.Clamp01(moonFactor) * Mathf.PI) * moonIntensity;

        // Плавные переходы цвета и интенсивности
        currentSunColor = Color.Lerp(currentSunColor, sunColor.Evaluate(dayTime), Time.deltaTime * transitionSpeed);
        currentMoonColor = Color.Lerp(currentMoonColor, moonColor.Evaluate(dayTime), Time.deltaTime * transitionSpeed);
        currentSunIntensitySmooth = Mathf.Lerp(currentSunIntensitySmooth, normalizedSunIntensity, Time.deltaTime * transitionSpeed);
        currentMoonIntensitySmooth = Mathf.Lerp(currentMoonIntensitySmooth, normalizedMoonIntensity, Time.deltaTime * transitionSpeed);

        sunLight.color = currentSunColor;
        sunLight.intensity = currentSunIntensitySmooth;
        moonLight.color = currentMoonColor;
        moonLight.intensity = currentMoonIntensitySmooth;
    }

    private void UpdateFog()
    {
        if (!enableFog)
        {
            RenderSettings.fog = false;
            return;
        }

        RenderSettings.fog = true;

        float dayTime = currentTime / 24.0f;
        float dayFactor = MapTimeTo01Range(dawnStart, duskStart, currentTime);

        // Плавно смешиваем дневную и ночную плотность тумана в зависимости от "дня"
        float targetFogDensity = Mathf.Lerp(fogDensityNight, fogDensityDay, Mathf.SmoothStep(0f, 1f, dayFactor));

        currentFogColor = Color.Lerp(currentFogColor, fogColor.Evaluate(dayTime), Time.deltaTime * transitionSpeed);
        currentFogDensitySmooth = Mathf.Lerp(currentFogDensitySmooth, targetFogDensity, Time.deltaTime * transitionSpeed);

        RenderSettings.fogColor = currentFogColor;
        RenderSettings.fogDensity = currentFogDensitySmooth;
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
