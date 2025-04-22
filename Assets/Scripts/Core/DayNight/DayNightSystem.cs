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
    [Range(0, 24)]
    public float currentTime = 8.0f; // Утреннее время для начала
    public float timeSpeed = 0.5f; // Более медленная скорость для заметной смены времени суток
    public int currentDay = 1;

    [Header("Небо")]
    public Material skyboxMaterial; // Установить в инспекторе
    public Gradient skyColor; // Настроить в инспекторе с переходами от тёмно-синего (ночь) через светло-голубой (день) к оранжевому (закат)
    public Gradient horizonColor; // Настроить с переходами от тёмно-синего к оранжево-розовому (рассвет/закат) и голубому (день)
    public Gradient groundColor; // Темнее неба, с переходами от почти чёрного к тёмно-синему и коричневатому

    [Header("Освещение")]
    public Light sunLight; // Установить в инспекторе направленный свет
    public Light moonLight; // Установить в инспекторе второй направленный свет
    public Gradient sunColor; // От тёплого жёлтого до оранжевого
    public Gradient moonColor; // Холодный голубовато-белый цвет
    [Range(0, 2)]
    public float sunIntensity = 1.2f; // Слегка увеличенная интенсивность для более яркого дня
    [Range(0, 2)]
    public float moonIntensity = 0.4f; // Умеренное лунное освещение

    [Header("Звезды")]
    [Range(0, 1)]
    public float starsBrightness = 0.7f; // Более яркие звёзды для впечатляющего ночного неба

    [Header("Туман")]
    public bool enableFog = true;
    public Gradient fogColor; // От темно-синего (ночь) к светло-голубому (день)
    [Range(0, 0.05f)]
    public float fogDensityDay = 0.008f; // Лёгкий туман днём для глубины
    [Range(0, 0.05f)]
    public float fogDensityNight = 0.015f; // Более плотный туман ночью для атмосферности

    [Header("События")]
    public List<TimeEvent> timeEvents = new List<TimeEvent>();
    public DayCompletedEvent onDayCompleted = new DayCompletedEvent();

    private int lastHour = -1;
    private int lastMinute = -1;
    private bool dayCompletedFired = false;

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

        // Установка материала скайбокса
        RenderSettings.skybox = skyboxMaterial;

        // Инициализация времени
        UpdateTime(true);
    }

    private void Update()
    {
        UpdateTime(false);
    }

    private void UpdateTime(bool force)
    {
        // Обновление времени
        currentTime += Time.deltaTime * timeSpeed / 86400.0f * 24.0f; // Конвертация в суточное время

        if (currentTime >= 24.0f)
        {
            currentTime -= 24.0f;
            currentDay++;
            dayCompletedFired = false;

            // Сбросить все триггеры событий
            foreach (var timeEvent in timeEvents)
            {
                timeEvent.ResetTrigger();
            }
        }

        // Получение целочисленных часов и минут
        int hour = Mathf.FloorToInt(currentTime);
        int minute = Mathf.FloorToInt((currentTime - hour) * 60);

        // Проверка, изменилось ли время или это первый запуск
        if (force || lastHour != hour || lastMinute != minute)
        {
            lastHour = hour;
            lastMinute = minute;

            // Проверка событий времени
            CheckTimeEvents(hour, minute);

            // Событие завершения дня (в 23:59)
            if (!dayCompletedFired && hour == 23 && minute == 59)
            {
                onDayCompleted.Invoke(currentDay);
                dayCompletedFired = true;
            }
        }

        // Обновление визуальных эффектов
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
        // Нормализованное время дня (от 0 до 1)
        float dayTime = currentTime / 24.0f;

        // Обновление цветов неба
        skyboxMaterial.SetColor(SkyColorProperty, skyColor.Evaluate(dayTime));
        skyboxMaterial.SetColor(HorizonColorProperty, horizonColor.Evaluate(dayTime));
        skyboxMaterial.SetColor(GroundColorProperty, groundColor.Evaluate(dayTime));

        // Яркость звезд зависит от времени суток
        float stars = 0;
        if (currentTime > 18.0f || currentTime < 6.0f)
        {
            float nightProgress = (currentTime > 18.0f) ?
                (currentTime - 18.0f) / 6.0f :
                1.0f - (currentTime / 6.0f);
            stars = nightProgress * starsBrightness;
        }
        skyboxMaterial.SetFloat(StarBrightnessProperty, stars);
    }

    private void UpdateLighting()
    {
        if (sunLight == null || moonLight == null) return;

        // Угол вращения солнца и луны
        float sunAngle = (currentTime / 24.0f) * 360.0f;
        float moonAngle = sunAngle + 180.0f;

        // Обновление направления солнца
        Quaternion sunRotation = Quaternion.Euler(sunAngle - 90.0f, 0, 0);
        sunLight.transform.rotation = sunRotation;

        // Обновление направления луны
        Quaternion moonRotation = Quaternion.Euler(moonAngle - 90.0f, 0, 0);
        moonLight.transform.rotation = moonRotation;

        // Нормализованное время дня для интенсивности света
        float dayTime = currentTime / 24.0f;

        // Интенсивность солнечного света
        float normalizedSunIntensity;
        if (currentTime > 6.0f && currentTime < 18.0f)
        {
            // День
            float dayProgress = (currentTime - 6.0f) / 12.0f;
            normalizedSunIntensity = Mathf.Sin(dayProgress * Mathf.PI) * sunIntensity;
        }
        else
        {
            normalizedSunIntensity = 0;
        }
        sunLight.intensity = normalizedSunIntensity;
        sunLight.color = sunColor.Evaluate(dayTime);

        // Интенсивность лунного света
        float normalizedMoonIntensity;
        if (currentTime < 6.0f || currentTime > 18.0f)
        {
            // Ночь
            float nightProgress = (currentTime < 6.0f) ?
                (6.0f - currentTime) / 6.0f :
                (currentTime - 18.0f) / 6.0f;
            normalizedMoonIntensity = Mathf.Sin(nightProgress * Mathf.PI) * moonIntensity;
        }
        else
        {
            normalizedMoonIntensity = 0;
        }
        moonLight.intensity = normalizedMoonIntensity;
        moonLight.color = moonColor.Evaluate(dayTime);
    }

    private void UpdateFog()
    {
        if (!enableFog)
        {
            RenderSettings.fog = false;
            return;
        }

        RenderSettings.fog = true;

        // Нормализованное время дня
        float dayTime = currentTime / 24.0f;

        // Цвет тумана
        RenderSettings.fogColor = fogColor.Evaluate(dayTime);

        // Плотность тумана в зависимости от времени суток
        if (currentTime > 6.0f && currentTime < 18.0f)
        {
            // День
            RenderSettings.fogDensity = fogDensityDay;
        }
        else
        {
            // Ночь
            RenderSettings.fogDensity = fogDensityNight;
        }
    }

    // Публичные методы для управления временем
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

    // Получение текущего времени в виде строки
    public string GetTimeString()
    {
        int hour = Mathf.FloorToInt(currentTime);
        int minute = Mathf.FloorToInt((currentTime - hour) * 60);
        return string.Format("{0:00}:{1:00}", hour, minute);
    }
}