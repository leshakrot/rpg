using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

#if UNITY_EDITOR
[CustomEditor(typeof(DayNightSystem))]
public class DayNightSystemEditor : Editor
{
    private SerializedProperty currentTime, timeSpeed, currentDay;
    private SerializedProperty dawnStart, duskStart, transitionSpeed;
    private SerializedProperty skyboxMaterial, skyColor, horizonColor, groundColor;
    private SerializedProperty sunLight, moonLight, sunColor, moonColor, sunIntensity, moonIntensity;
    private SerializedProperty starsBrightness;
    private SerializedProperty enableFog, fogColor, fogDensityDay, fogDensityNight;
    private SerializedProperty timeEvents, onDayCompleted;

    private bool showTimeSettings = true;
    private bool showSkySettings = true;
    private bool showLightSettings = true;
    private bool showFogSettings = true;
    private bool showEventSettings = true;

    private int newEventHour = 6;
    private int newEventMinute = 0;
    private string newEventName = "Новое событие";

    private void OnEnable()
    {
        currentTime = serializedObject.FindProperty("currentTime");
        timeSpeed = serializedObject.FindProperty("timeSpeed");
        currentDay = serializedObject.FindProperty("currentDay");
        dawnStart = serializedObject.FindProperty("dawnStart");
        duskStart = serializedObject.FindProperty("duskStart");
        transitionSpeed = serializedObject.FindProperty("transitionSpeed");
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

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Текущее время:", system.GetTimeString(), EditorStyles.boldLabel);
        EditorGUILayout.Space();

        showTimeSettings = EditorGUILayout.Foldout(showTimeSettings, "Время", true);
        if (showTimeSettings)
        {
            EditorGUI.indentLevel++;
            float time = currentTime.floatValue;
            int hours = Mathf.FloorToInt(time);
            int minutes = Mathf.FloorToInt((time - hours) * 60);

            EditorGUI.BeginChangeCheck();
            hours = EditorGUILayout.IntSlider("Часы", hours, 0, 23);
            minutes = EditorGUILayout.IntSlider("Минуты", minutes, 0, 59);
            if (EditorGUI.EndChangeCheck())
                currentTime.floatValue = hours + (minutes / 60.0f);

            EditorGUILayout.PropertyField(timeSpeed, new GUIContent("Скорость времени"));
            EditorGUILayout.PropertyField(currentDay, new GUIContent("Текущий день"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Границы дня/ночи", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(dawnStart, new GUIContent("Начало рассвета (часы)"));
            EditorGUILayout.PropertyField(duskStart, new GUIContent("Начало заката (часы)"));
            EditorGUILayout.PropertyField(transitionSpeed, new GUIContent("Скорость сглаживания"));

            EditorGUI.indentLevel--;
        }

        showSkySettings = EditorGUILayout.Foldout(showSkySettings, "Небо", true);
        if (showSkySettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(skyboxMaterial);
            EditorGUILayout.PropertyField(skyColor);
            EditorGUILayout.PropertyField(horizonColor);
            EditorGUILayout.PropertyField(groundColor);
            EditorGUILayout.PropertyField(starsBrightness);
            EditorGUI.indentLevel--;
        }

        showLightSettings = EditorGUILayout.Foldout(showLightSettings, "Свет", true);
        if (showLightSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sunLight);
            EditorGUILayout.PropertyField(moonLight);
            EditorGUILayout.PropertyField(sunColor);
            EditorGUILayout.PropertyField(moonColor);
            EditorGUILayout.PropertyField(sunIntensity);
            EditorGUILayout.PropertyField(moonIntensity);
            EditorGUI.indentLevel--;
        }

        showFogSettings = EditorGUILayout.Foldout(showFogSettings, "Туман", true);
        if (showFogSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(enableFog);
            if (enableFog.boolValue)
            {
                EditorGUILayout.PropertyField(fogColor);
                EditorGUILayout.PropertyField(fogDensityDay);
                EditorGUILayout.PropertyField(fogDensityNight);
            }
            EditorGUI.indentLevel--;
        }

        showEventSettings = EditorGUILayout.Foldout(showEventSettings, "События", true);
        if (showEventSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(timeEvents, true);
            EditorGUILayout.PropertyField(onDayCompleted);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Добавить событие", EditorStyles.boldLabel);
            newEventHour = EditorGUILayout.IntSlider("Часы", newEventHour, 0, 23);
            newEventMinute = EditorGUILayout.IntSlider("Минуты", newEventMinute, 0, 59);
            newEventName = EditorGUILayout.TextField("Название", newEventName);

            if (GUILayout.Button("Добавить"))
            {
                TimeEvent newEvent = new TimeEvent
                {
                    hour = newEventHour,
                    minute = newEventMinute,
                    onTimeReached = new UnityEvent()
                };
                system.timeEvents.Add(newEvent);
                newEventName = "Новое событие";
                serializedObject.Update();
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
