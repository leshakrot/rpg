using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using System;

#if UNITY_EDITOR
[CustomEditor(typeof(DayNightSystem))]
public class DayNightSystemEditor : Editor
{
    private SerializedProperty currentTime;
    private SerializedProperty timeSpeed;
    private SerializedProperty currentDay;
    private SerializedProperty skyboxMaterial;
    private SerializedProperty skyColor;
    private SerializedProperty horizonColor;
    private SerializedProperty groundColor;
    private SerializedProperty sunLight;
    private SerializedProperty moonLight;
    private SerializedProperty sunColor;
    private SerializedProperty moonColor;
    private SerializedProperty sunIntensity;
    private SerializedProperty moonIntensity;
    private SerializedProperty starsBrightness;
    private SerializedProperty enableFog;
    private SerializedProperty fogColor;
    private SerializedProperty fogDensityDay;
    private SerializedProperty fogDensityNight;
    private SerializedProperty timeEvents;
    private SerializedProperty onDayCompleted;

    private bool showTimeSettings = true;
    private bool showSkySettings = true;
    private bool showLightSettings = true;
    private bool showFogSettings = true;
    private bool showEventSettings = true;

    // Временные переменные для нового события
    private int newEventHour = 6;
    private int newEventMinute = 0;
    private string newEventName = "Новое событие";

    private void OnEnable()
    {
        // Инициализация свойств
        currentTime = serializedObject.FindProperty("currentTime");
        timeSpeed = serializedObject.FindProperty("timeSpeed");
        currentDay = serializedObject.FindProperty("currentDay");
        skyboxMaterial = serializedObject.FindProperty("skyboxMaterial");
        skyColor = serializedObject.FindProperty("skyColor");
        horizonColor = serializedObject.FindProperty("horizonColor");
        groundColor = serializedObject.FindProperty("groundColor");
        sunLight = serializedObject.FindProperty("sunLight");
        moonLight = serializedObject.FindProperty("moonLight");
        sunColor = serializedObject.FindProperty("sunColor");
        moonColor = serializedObject.FindProperty("moonColor");
        sunIntensity = serializedObject.FindProperty("sunIntensity");
        moonIntensity = serializedObject.FindProperty("moonIntensity");
        starsBrightness = serializedObject.FindProperty("starsBrightness");
        enableFog = serializedObject.FindProperty("enableFog");
        fogColor = serializedObject.FindProperty("fogColor");
        fogDensityDay = serializedObject.FindProperty("fogDensityDay");
        fogDensityNight = serializedObject.FindProperty("fogDensityNight");
        timeEvents = serializedObject.FindProperty("timeEvents");
        onDayCompleted = serializedObject.FindProperty("onDayCompleted");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DayNightSystem system = (DayNightSystem)target;

        // Отображение текущего времени в формате HH:MM
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Текущее время:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(system.GetTimeString(), EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Настройки времени
        showTimeSettings = EditorGUILayout.Foldout(showTimeSettings, "Настройки времени", true);
        if (showTimeSettings)
        {
            EditorGUI.indentLevel++;

            // Конвертация времени в часы и минуты для более удобного интерфейса
            float time = currentTime.floatValue;
            int hours = Mathf.FloorToInt(time);
            int minutes = Mathf.FloorToInt((time - hours) * 60);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Время:", GUILayout.Width(50));

            EditorGUI.BeginChangeCheck();
            hours = EditorGUILayout.IntSlider(hours, 0, 23);
            minutes = EditorGUILayout.IntSlider(minutes, 0, 59);
            if (EditorGUI.EndChangeCheck())
            {
                currentTime.floatValue = hours + (minutes / 60.0f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(timeSpeed, new GUIContent("Скорость времени"));
            EditorGUILayout.PropertyField(currentDay, new GUIContent("Текущий день"));

            EditorGUI.indentLevel--;
        }

        // Настройки неба
        showSkySettings = EditorGUILayout.Foldout(showSkySettings, "Настройки неба", true);
        if (showSkySettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(skyboxMaterial, new GUIContent("Материал скайбокса"));
            EditorGUILayout.PropertyField(skyColor, new GUIContent("Цвет неба"));
            EditorGUILayout.PropertyField(horizonColor, new GUIContent("Цвет горизонта"));
            EditorGUILayout.PropertyField(groundColor, new GUIContent("Цвет земли"));
            EditorGUILayout.PropertyField(starsBrightness, new GUIContent("Яркость звезд"));
            EditorGUI.indentLevel--;
        }

        // Настройки освещения
        showLightSettings = EditorGUILayout.Foldout(showLightSettings, "Настройки освещения", true);
        if (showLightSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sunLight, new GUIContent("Источник света солнца"));
            EditorGUILayout.PropertyField(moonLight, new GUIContent("Источник света луны"));
            EditorGUILayout.PropertyField(sunColor, new GUIContent("Цвет солнца"));
            EditorGUILayout.PropertyField(moonColor, new GUIContent("Цвет луны"));
            EditorGUILayout.PropertyField(sunIntensity, new GUIContent("Интенсивность солнца"));
            EditorGUILayout.PropertyField(moonIntensity, new GUIContent("Интенсивность луны"));
            EditorGUI.indentLevel--;
        }

        // Настройки тумана
        showFogSettings = EditorGUILayout.Foldout(showFogSettings, "Настройки тумана", true);
        if (showFogSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(enableFog, new GUIContent("Включить туман"));
            if (enableFog.boolValue)
            {
                EditorGUILayout.PropertyField(fogColor, new GUIContent("Цвет тумана"));
                EditorGUILayout.PropertyField(fogDensityDay, new GUIContent("Плотность тумана (день)"));
                EditorGUILayout.PropertyField(fogDensityNight, new GUIContent("Плотность тумана (ночь)"));
            }
            EditorGUI.indentLevel--;
        }

        // Настройки событий
        showEventSettings = EditorGUILayout.Foldout(showEventSettings, "События времени", true);
        if (showEventSettings)
        {
            EditorGUI.indentLevel++;

            // Отображение существующих событий
            EditorGUILayout.PropertyField(timeEvents, new GUIContent("События времени"), true);

            // Отображение события завершения дня
            EditorGUILayout.PropertyField(onDayCompleted, new GUIContent("Событие завершения дня"));

            // Добавление нового события
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Добавить новое событие", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Время:", GUILayout.Width(50));
            newEventHour = EditorGUILayout.IntSlider(newEventHour, 0, 23);
            newEventMinute = EditorGUILayout.IntSlider(newEventMinute, 0, 59);
            EditorGUILayout.EndHorizontal();

            newEventName = EditorGUILayout.TextField("Название:", newEventName);

            if (GUILayout.Button("Добавить событие"))
            {
                // Создание нового события
                TimeEvent newEvent = new TimeEvent
                {
                    hour = newEventHour,
                    minute = newEventMinute,
                    onTimeReached = new UnityEvent()
                };

                // Добавление события в список
                system.timeEvents.Add(newEvent);

                // Сброс значений
                newEventName = "Новое событие";

                // Обновление сериализованного объекта
                serializedObject.Update();
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif