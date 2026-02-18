using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

#if UNITY_EDITOR
[CustomEditor(typeof(DayNightSystem))]
public class DayNightSystemEditor : Editor
{
    // ─── Сериализованные свойства ───────────────────────────────────────────
    private SerializedProperty currentTime, timeSpeed, currentDay;
    private SerializedProperty dawnStart, duskStart, transitionSpeed;
    private SerializedProperty atmosphericSettings;
    private SerializedProperty skyboxMaterial, skyColor, horizonColor, groundColor;
    private SerializedProperty sunLight, moonLight, sunColor, moonColor;
    private SerializedProperty maxSunIntensity, maxMoonIntensity;
    private SerializedProperty starsBrightness;
    private SerializedProperty enableFog, fogColor, fogDensityDay, fogDensityNight, fogStartDistance, fogEndDistance;
    private SerializedProperty timeEvents, onDayCompleted;

    // ─── Состояние фолдаутов ────────────────────────────────────────────────
    private bool showTime        = true;
    private bool showAtmosphere  = true;
    private bool showLights      = true;
    private bool showFog         = true;
    private bool showSky         = false;
    private bool showEvents      = false;

    // Параметры для добавления события
    private int    newEventHour   = 6;
    private int    newEventMinute = 0;
    private string newEventName   = "Новое событие";

    // Цвета секций
    private static readonly Color ColorTime       = new Color(0.22f, 0.55f, 0.88f, 0.18f);
    private static readonly Color ColorAtmosphere = new Color(0.88f, 0.72f, 0.30f, 0.18f);
    private static readonly Color ColorLights     = new Color(0.88f, 0.88f, 0.30f, 0.18f);
    private static readonly Color ColorFog        = new Color(0.55f, 0.75f, 0.88f, 0.18f);
    private static readonly Color ColorSky        = new Color(0.30f, 0.50f, 0.88f, 0.18f);
    private static readonly Color ColorEvents     = new Color(0.88f, 0.45f, 0.25f, 0.18f);

    private void OnEnable()
    {
        currentTime   = serializedObject.FindProperty("currentTime");
        timeSpeed     = serializedObject.FindProperty("timeSpeed");
        currentDay    = serializedObject.FindProperty("currentDay");
        dawnStart     = serializedObject.FindProperty("dawnStart");
        duskStart     = serializedObject.FindProperty("duskStart");
        transitionSpeed = serializedObject.FindProperty("transitionSpeed");

        atmosphericSettings = serializedObject.FindProperty("atmosphericSettings");

        skyboxMaterial = serializedObject.FindProperty("skyboxMaterial");
        skyColor       = serializedObject.FindProperty("skyColor");
        horizonColor   = serializedObject.FindProperty("horizonColor");
        groundColor    = serializedObject.FindProperty("groundColor");

        sunLight        = serializedObject.FindProperty("sunLight");
        moonLight       = serializedObject.FindProperty("moonLight");
        sunColor        = serializedObject.FindProperty("sunColor");
        moonColor       = serializedObject.FindProperty("moonColor");
        maxSunIntensity = serializedObject.FindProperty("maxSunIntensity");
        maxMoonIntensity = serializedObject.FindProperty("maxMoonIntensity");

        starsBrightness  = serializedObject.FindProperty("starsBrightness");

        enableFog        = serializedObject.FindProperty("enableFog");
        fogColor         = serializedObject.FindProperty("fogColor");
        fogDensityDay    = serializedObject.FindProperty("fogDensityDay");
        fogDensityNight  = serializedObject.FindProperty("fogDensityNight");
        fogStartDistance = serializedObject.FindProperty("fogStartDistance");
        fogEndDistance   = serializedObject.FindProperty("fogEndDistance");

        timeEvents     = serializedObject.FindProperty("timeEvents");
        onDayCompleted = serializedObject.FindProperty("onDayCompleted");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var system = (DayNightSystem)target;

        DrawStatusPanel(system);
        EditorGUILayout.Space(4);

        DrawSection("⏰  Время",          ColorTime,       ref showTime,       () => DrawTimeSection());
        DrawSection("🌅  Атмосфера",      ColorAtmosphere, ref showAtmosphere, () => DrawAtmosphereSection());
        DrawSection("💡  Освещение",       ColorLights,     ref showLights,     () => DrawLightsSection());
        DrawSection("🌫️  Туман",           ColorFog,        ref showFog,        () => DrawFogSection());
        DrawSection("🌌  Небо (Skybox)",   ColorSky,        ref showSky,        () => DrawSkySection());
        DrawSection("⚡  События",         ColorEvents,     ref showEvents,     () => DrawEventsSection(system));

        EditorGUILayout.Space(6);
        DrawQuickActions(system);

        serializedObject.ApplyModifiedProperties();
    }

    // ─── Панель статуса ─────────────────────────────────────────────────────
    private void DrawStatusPanel(DayNightSystem system)
    {
        var style = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(10, 10, 8, 8)
        };

        EditorGUILayout.BeginVertical(style);

        // Заголовок
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
        EditorGUILayout.LabelField("🌍  Day/Night System  —  Cozy Lowpoly Dark Fantasy", titleStyle);

        EditorGUILayout.Space(2);

        // Статус времени
        float dayProg = 0f;
        if (system.currentTime >= system.dawnStart && system.currentTime <= system.duskStart)
            dayProg = (system.currentTime - system.dawnStart) / (system.duskStart - system.dawnStart);

        string phase = system.currentTime < system.dawnStart ? "🌙 Ночь"
                     : system.currentTime < system.dawnStart + 1.5f ? "🌄 Рассвет"
                     : system.currentTime < system.duskStart - 1f   ? "☀️ День"
                     : system.currentTime < system.duskStart + 1.5f ? "🌇 Закат"
                     : "🌙 Ночь";

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Время:", GUILayout.Width(60));
        EditorGUILayout.LabelField($"{system.GetTimeString()}  {phase}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"День {system.currentDay}", GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        // Прогресс-бар дня
        Rect barRect = GUILayoutUtility.GetRect(0, 14, GUILayout.ExpandWidth(true));
        barRect.x     += 2; barRect.width -= 4;
        EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));
        var fillRect = new Rect(barRect.x, barRect.y, barRect.width * dayProg, barRect.height);
        EditorGUI.DrawRect(fillRect, new Color(0.90f, 0.72f, 0.28f));
        GUI.Label(barRect, $"  {dayProg * 100f:F0}% дня", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    // ─── Секция ─────────────────────────────────────────────────────────────
    private void DrawSection(string title, Color bgColor, ref bool foldout, System.Action draw)
    {
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = oldColor;

        foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
        if (foldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(2);
            draw?.Invoke();
            EditorGUILayout.Space(2);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    // ─── Контент секций ─────────────────────────────────────────────────────
    private void DrawTimeSection()
    {
        float time  = currentTime.floatValue;
        int   hours = Mathf.FloorToInt(time);
        int   mins  = Mathf.FloorToInt((time - hours) * 60);

        EditorGUI.BeginChangeCheck();
        hours = EditorGUILayout.IntSlider("Часы",   hours, 0, 23);
        mins  = EditorGUILayout.IntSlider("Минуты", mins,  0, 59);
        if (EditorGUI.EndChangeCheck())
            currentTime.floatValue = hours + mins / 60f;

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(timeSpeed,     new GUIContent("Скорость времени",     "1 = 86400 сек/игровой день"));
        EditorGUILayout.PropertyField(currentDay,    new GUIContent("Текущий день"));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Границы суток", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(dawnStart,     new GUIContent("Начало рассвета (ч)"));
        EditorGUILayout.PropertyField(duskStart,     new GUIContent("Начало заката (ч)"));
        EditorGUILayout.PropertyField(transitionSpeed, new GUIContent("Скорость сглаживания"));
    }

    private void DrawAtmosphereSection()
    {
        EditorGUILayout.HelpBox(
            "Ambient — базовое окружающее освещение. Настройте градиент под вайб сцены: " +
            "тёмный индиго ночью, янтарный на рассвете/закате, молочный днём.",
            MessageType.None);

        if (atmosphericSettings != null)
            EditorGUILayout.PropertyField(atmosphericSettings, true);
    }

    private void DrawLightsSection()
    {
        EditorGUILayout.PropertyField(sunLight,  new GUIContent("Источник солнца"));
        EditorGUILayout.PropertyField(moonLight, new GUIContent("Источник луны"));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Цвета", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sunColor,  new GUIContent("Градиент солнца"));
        EditorGUILayout.PropertyField(moonColor, new GUIContent("Градиент луны"));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Интенсивность", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(maxSunIntensity,  new GUIContent("Макс. интенсивность солнца"));
        EditorGUILayout.PropertyField(maxMoonIntensity, new GUIContent("Макс. интенсивность луны"));
    }

    private void DrawFogSection()
    {
        EditorGUILayout.HelpBox(
            "Для lowpoly стиля рекомендуется лёгкий туман: день 0.003–0.006, ночь 0.010–0.015.",
            MessageType.None);

        EditorGUILayout.PropertyField(enableFog, new GUIContent("Включить туман"));
        if (enableFog.boolValue)
        {
            EditorGUILayout.PropertyField(fogColor,        new GUIContent("Градиент цвета тумана"));
            EditorGUILayout.PropertyField(fogDensityDay,   new GUIContent("Плотность днём"));
            EditorGUILayout.PropertyField(fogDensityNight, new GUIContent("Плотность ночью"));
        }
    }

    private void DrawSkySection()
    {
        EditorGUILayout.HelpBox("Используется только если задан Skybox Material со свойствами _SkyColor, _HorizonColor, _GroundColor.", MessageType.Info);
        EditorGUILayout.PropertyField(skyboxMaterial);
        if (skyColor     != null) EditorGUILayout.PropertyField(skyColor,     new GUIContent("Небо"));
        if (horizonColor != null) EditorGUILayout.PropertyField(horizonColor, new GUIContent("Горизонт"));
        if (groundColor  != null) EditorGUILayout.PropertyField(groundColor,  new GUIContent("Земля"));
        if (starsBrightness != null) EditorGUILayout.PropertyField(starsBrightness, new GUIContent("Яркость звёзд"));
    }

    private void DrawEventsSection(DayNightSystem system)
    {
        EditorGUILayout.PropertyField(timeEvents,     true);
        EditorGUILayout.PropertyField(onDayCompleted, new GUIContent("Событие конца дня"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Добавить событие", EditorStyles.boldLabel);
        newEventHour   = EditorGUILayout.IntSlider("Час",      newEventHour,   0, 23);
        newEventMinute = EditorGUILayout.IntSlider("Минута",   newEventMinute, 0, 59);
        newEventName   = EditorGUILayout.TextField("Название", newEventName);

        if (GUILayout.Button("➕  Добавить событие"))
        {
            system.timeEvents.Add(new TimeEvent { hour = newEventHour, minute = newEventMinute, onTimeReached = new UnityEvent() });
            newEventName = "Новое событие";
            serializedObject.Update();
        }
    }

    // ─── Быстрые действия ───────────────────────────────────────────────────
    private void DrawQuickActions(DayNightSystem system)
    {
        EditorGUILayout.LabelField("Быстрые действия", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🌄 Рассвет"))   currentTime.floatValue = dawnStart.floatValue;
        if (GUILayout.Button("☀️ Полдень"))   currentTime.floatValue = (dawnStart.floatValue + duskStart.floatValue) / 2f;
        if (GUILayout.Button("🌇 Закат"))     currentTime.floatValue = duskStart.floatValue;
        if (GUILayout.Button("🌙 Полночь"))   currentTime.floatValue = 0f;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🕐 +1 час"))    currentTime.floatValue = Mathf.Repeat(currentTime.floatValue + 1f, 24f);
        if (GUILayout.Button("🕐 -1 час"))    currentTime.floatValue = Mathf.Repeat(currentTime.floatValue - 1f, 24f);
        if (GUILayout.Button("▶ Применить") && Application.isPlaying) system.SetTime(system.currentTime);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "💡 Советы по настройке Cozy Dark Fantasy:\n" +
            "• Ambient ночью: тёмный индиго (#10122E), не чёрный\n" +
            "• Ambient днём: молочно-бежевый (#E0D6C2), не белый\n" +
            "• Туман днём: очень лёгкий (0.004), ночью умеренный (0.012)\n" +
            "• Тени солнца: strength 0.55–0.65 (мягкие, не резкие)\n" +
            "• Интенсивность солнца: 1.3–1.6 для lowpoly материалов",
            MessageType.None);
    }
}
#endif
