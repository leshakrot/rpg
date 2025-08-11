using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

#if UNITY_EDITOR
[CustomEditor(typeof(DayNightSystem))]
public class DayNightSystemEditor : Editor
{
    private SerializedProperty currentTime, timeSpeed, currentDay;
    private SerializedProperty dawnStart, duskStart, transitionSpeed;
    private SerializedProperty atmosphericSettings;
    private SerializedProperty skyboxMaterial, skyColor, horizonColor, groundColor;
    private SerializedProperty sunLight, moonLight, sunColor, moonColor;
    private SerializedProperty maxSunIntensity, maxMoonIntensity, sunShadowSoftness, moonShadowSoftness;
    private SerializedProperty starsBrightness;
    private SerializedProperty enableFog, fogColor, fogDensityDay, fogDensityNight, fogStartDistance, fogEndDistance;
    private SerializedProperty timeEvents, onDayCompleted;

    private bool showTimeSettings = true;
    private bool showAtmosphericSettings = true;
    private bool showSkySettings = false;
    private bool showLightSettings = true;
    private bool showFogSettings = true;
    private bool showEventSettings = false;

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
        
        atmosphericSettings = serializedObject.FindProperty("atmosphericSettings");
        
        skyboxMaterial = serializedObject.FindProperty("skyboxMaterial");
        skyColor = serializedObject.FindProperty("skyColor");
        horizonColor = serializedObject.FindProperty("horizonColor");
        groundColor = serializedObject.FindProperty("groundColor");
        
        sunLight = serializedObject.FindProperty("sunLight");
        moonLight = serializedObject.FindProperty("moonLight");
        sunColor = serializedObject.FindProperty("sunColor");
        moonColor = serializedObject.FindProperty("moonColor");
        maxSunIntensity = serializedObject.FindProperty("maxSunIntensity");
        maxMoonIntensity = serializedObject.FindProperty("maxMoonIntensity");
        sunShadowSoftness = serializedObject.FindProperty("sunShadowSoftness");
        moonShadowSoftness = serializedObject.FindProperty("moonShadowSoftness");
        
        starsBrightness = serializedObject.FindProperty("starsBrightness");
        
        enableFog = serializedObject.FindProperty("enableFog");
        fogColor = serializedObject.FindProperty("fogColor");
        fogDensityDay = serializedObject.FindProperty("fogDensityDay");
        fogDensityNight = serializedObject.FindProperty("fogDensityNight");
        fogStartDistance = serializedObject.FindProperty("fogStartDistance");
        fogEndDistance = serializedObject.FindProperty("fogEndDistance");
        
        timeEvents = serializedObject.FindProperty("timeEvents");
        onDayCompleted = serializedObject.FindProperty("onDayCompleted");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DayNightSystem system = (DayNightSystem)target;

        EditorGUILayout.Space();
        
        // Информационная панель
        DrawInfoPanel(system);
        
        EditorGUILayout.Space();

        // Основные настройки времени
        DrawTimeSettings();
        
        // Атмосферные настройки
        DrawAtmosphericSettings();

        // Настройки освещения
        DrawLightSettings();

        // Настройки тумана
        DrawFogSettings();

        // Настройки неба (опциональные)
        DrawSkySettings();

        // События
        DrawEventSettings(system);

        // Кнопки быстрых действий
        DrawQuickActions(system);

        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawInfoPanel(DayNightSystem system)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Информация о системе", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Текущее время:", system.GetTimeString());
        
        float dayProgress = 0f;
        if (system.currentTime >= system.dawnStart && system.currentTime <= system.duskStart)
        {
            dayProgress = (system.currentTime - system.dawnStart) / (system.duskStart - system.dawnStart);
        }
        
        EditorGUILayout.LabelField("Прогресс дня:", $"{dayProgress * 100:F1}%");
        EditorGUILayout.LabelField("День:", system.currentDay.ToString());
        EditorGUILayout.EndVertical();
    }

    private void DrawTimeSettings()
    {
        showTimeSettings = EditorGUILayout.Foldout(showTimeSettings, "⏰ Время", true, EditorStyles.foldoutHeader);
        if (showTimeSettings)
        {
            EditorGUI.indentLevel++;
            
            // Удобный контроль времени
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
            EditorGUILayout.PropertyField(transitionSpeed, new GUIContent("Скорость переходов"));

            EditorGUI.indentLevel--;
        }
    }
    
    private void DrawAtmosphericSettings()
    {
        showAtmosphericSettings = EditorGUILayout.Foldout(showAtmosphericSettings, "🌅 Атмосферные настройки", true, EditorStyles.foldoutHeader);
        if (showAtmosphericSettings)
        {
            if (atmosphericSettings != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(atmosphericSettings, true);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Атмосферные настройки не инициализированы. Нажмите 'Инициализировать' для создания.", MessageType.Warning);
            }
        }
    }

    private void DrawLightSettings()
    {
        showLightSettings = EditorGUILayout.Foldout(showLightSettings, "💡 Освещение", true, EditorStyles.foldoutHeader);
        if (showLightSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(sunLight, new GUIContent("Солнечный свет"));
            EditorGUILayout.PropertyField(moonLight, new GUIContent("Лунный свет"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Цвета света", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sunColor, new GUIContent("Цвета солнца"));
            EditorGUILayout.PropertyField(moonColor, new GUIContent("Цвета луны"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Интенсивность", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxSunIntensity, new GUIContent("Макс. интенсивность солнца"));
            EditorGUILayout.PropertyField(maxMoonIntensity, new GUIContent("Макс. интенсивность луны"));
            
            if (sunShadowSoftness != null && moonShadowSoftness != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Тени", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(sunShadowSoftness, new GUIContent("Мягкость теней солнца"));
                EditorGUILayout.PropertyField(moonShadowSoftness, new GUIContent("Мягкость теней луны"));
            }
            
            EditorGUI.indentLevel--;
        }
    }

    private void DrawSkySettings()
    {
        showSkySettings = EditorGUILayout.Foldout(showSkySettings, "🌌 Небо (опционально)", true, EditorStyles.foldoutHeader);
        if (showSkySettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("Эти настройки используются только если у вас есть Skybox материал", MessageType.Info);
            EditorGUILayout.PropertyField(skyboxMaterial);
            
            if (skyColor != null) EditorGUILayout.PropertyField(skyColor);
            if (horizonColor != null) EditorGUILayout.PropertyField(horizonColor);
            if (groundColor != null) EditorGUILayout.PropertyField(groundColor);
            if (starsBrightness != null) EditorGUILayout.PropertyField(starsBrightness);
            
            EditorGUI.indentLevel--;
        }
    }

    private void DrawFogSettings()
    {
        showFogSettings = EditorGUILayout.Foldout(showFogSettings, "🌫️ Туман", true, EditorStyles.foldoutHeader);
        if (showFogSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(enableFog, new GUIContent("Включить туман"));
            if (enableFog.boolValue)
            {
                EditorGUILayout.PropertyField(fogColor, new GUIContent("Цвет тумана"));
                EditorGUILayout.PropertyField(fogDensityDay, new GUIContent("Плотность днем"));
                EditorGUILayout.PropertyField(fogDensityNight, new GUIContent("Плотность ночью"));
                
                if (fogStartDistance != null && fogEndDistance != null)
                {
                    EditorGUILayout.PropertyField(fogStartDistance, new GUIContent("Начальное расстояние"));
                    EditorGUILayout.PropertyField(fogEndDistance, new GUIContent("Конечное расстояние"));
                }
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawEventSettings(DayNightSystem system)
    {
        showEventSettings = EditorGUILayout.Foldout(showEventSettings, "⚡ События", true, EditorStyles.foldoutHeader);
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

            if (GUILayout.Button("Добавить событие"))
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
    }
    
    private void DrawQuickActions(DayNightSystem system)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Быстрые действия", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Установить рассвет"))
        {
            currentTime.floatValue = dawnStart.floatValue;
        }
        if (GUILayout.Button("Установить полдень"))
        {
            currentTime.floatValue = (dawnStart.floatValue + duskStart.floatValue) / 2f;
        }
        if (GUILayout.Button("Установить закат"))
        {
            currentTime.floatValue = duskStart.floatValue;
        }
        if (GUILayout.Button("Установить полночь"))
        {
            currentTime.floatValue = 0f;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Инициализировать систему"))
        {
            // Принудительная инициализация
            if (Application.isPlaying)
            {
                system.SetTime(system.currentTime);
            }
        }
        
        if (GUILayout.Button("Открыть Setup Helper"))
        {
            EditorWindow.GetWindow(System.Type.GetType("DayNightSetupHelper"), false, "Day/Night Setup");
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
