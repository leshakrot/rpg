using UnityEngine;
using UnityEditor;
using RPG.Combat;
using System.Collections.Generic;

[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor
{
    SerializedProperty minGuaranteedSpawnsProp;
    SerializedProperty maxEnemiesPerSceneProp;
    SerializedProperty respawnOnRevisitProp;
    SerializedProperty respawnDelayProp;
    SerializedProperty respawnDeadEnemiesProp;
    SerializedProperty deadEnemyRespawnTimeProp;
    SerializedProperty maxSimultaneousRespawnsProp;
    SerializedProperty useSpawnZonesProp;
    SerializedProperty maxEnemiesPerZoneProp;
    SerializedProperty minDistanceBetweenEnemiesProp;
    SerializedProperty maxSpawnAttemptsProp;
    SerializedProperty showDebugInfoProp;
    SerializedProperty drawLinesInGameProp;
    SerializedProperty debugLineColorProp;

    private bool showZoneSettings = true;
    private bool showRespawnSettings = false;
    private bool showSpawnSettings = false;
    private bool showDebugSettings = false;

    private void OnEnable()
    {
        minGuaranteedSpawnsProp = serializedObject.FindProperty("minGuaranteedSpawns");
        maxEnemiesPerSceneProp = serializedObject.FindProperty("maxEnemiesPerScene");
        respawnOnRevisitProp = serializedObject.FindProperty("respawnOnRevisit");
        respawnDelayProp = serializedObject.FindProperty("respawnDelay");
        respawnDeadEnemiesProp = serializedObject.FindProperty("respawnDeadEnemies");
        deadEnemyRespawnTimeProp = serializedObject.FindProperty("deadEnemyRespawnTime");
        maxSimultaneousRespawnsProp = serializedObject.FindProperty("maxSimultaneousRespawns");
        useSpawnZonesProp = serializedObject.FindProperty("useSpawnZones");
        maxEnemiesPerZoneProp = serializedObject.FindProperty("maxEnemiesPerZone");
        minDistanceBetweenEnemiesProp = serializedObject.FindProperty("minDistanceBetweenEnemies");
        maxSpawnAttemptsProp = serializedObject.FindProperty("maxSpawnAttempts");
        showDebugInfoProp = serializedObject.FindProperty("showDebugInfo");
        drawLinesInGameProp = serializedObject.FindProperty("drawLinesInGame");
        debugLineColorProp = serializedObject.FindProperty("debugLineColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Главные настройки
        EditorGUILayout.LabelField("Основные настройки", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(minGuaranteedSpawnsProp, new GUIContent("Мин. гарантированных врагов"));
        EditorGUILayout.PropertyField(maxEnemiesPerSceneProp, new GUIContent("Макс. врагов на сцене"));
        EditorGUILayout.PropertyField(respawnOnRevisitProp, new GUIContent("Респавн при возвращении"));
        
        if (respawnOnRevisitProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(respawnDelayProp, new GUIContent("Задержка респавна"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        
        // Настройки респавна убитых врагов
        showRespawnSettings = EditorGUILayout.Foldout(showRespawnSettings, "Респавн убитых врагов", true);
        if (showRespawnSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(respawnDeadEnemiesProp, new GUIContent("Респавнить убитых врагов"));
            
            if (respawnDeadEnemiesProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(deadEnemyRespawnTimeProp, new GUIContent("Время респавна (сек)"));
                EditorGUILayout.PropertyField(maxSimultaneousRespawnsProp, new GUIContent("Макс. одновременных респавнов"));
                
                EditorGUILayout.HelpBox(
                    "Убитые враги будут автоматически респавниться через указанное время. " +
                    "Система учитывает общий лимит врагов на сцене и ограничение одновременных респавнов.",
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        
        // Настройки зон
        showZoneSettings = EditorGUILayout.Foldout(showZoneSettings, "Настройки зон спавна", true);
        if (showZoneSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(useSpawnZonesProp, new GUIContent("Использовать зоны спавна"));
            if (useSpawnZonesProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(maxEnemiesPerZoneProp, new GUIContent("Макс. врагов на зону"));
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        
        // Настройки спавна
        showSpawnSettings = EditorGUILayout.Foldout(showSpawnSettings, "Настройки спавна", true);
        if (showSpawnSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(minDistanceBetweenEnemiesProp, new GUIContent("Мин. дистанция между врагами"));
            EditorGUILayout.PropertyField(maxSpawnAttemptsProp, new GUIContent("Макс. попыток спавна"));
            
            if (minDistanceBetweenEnemiesProp.floatValue < 0.5f)
            {
                EditorGUILayout.HelpBox(
                    "Слишком малая дистанция может привести к проблемам с размещением врагов. " +
                    "Рекомендуется значение не менее 0.5.",
                    MessageType.Warning);
            }
            
            EditorGUILayout.HelpBox(
                "Эти настройки помогают избежать спавна врагов слишком близко друг к другу. " +
                "Система будет пытаться найти подходящую позицию в радиусе точки спавна.",
                MessageType.Info);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Настройки автолевелинга
        EditorGUILayout.LabelField("Автолевелинг врагов", EditorStyles.boldLabel);
        
        SerializedProperty useAutoLevelingProp = serializedObject.FindProperty("useAutoLeveling");
        EditorGUILayout.PropertyField(useAutoLevelingProp, new GUIContent("Использовать автолевелинг"));
        
        if (useAutoLevelingProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minLevelOffset"), 
                new GUIContent("Мин. смещение уровня", 
                "Минимальное смещение от уровня игрока (может быть отрицательным)"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxLevelOffset"), 
                new GUIContent("Макс. смещение уровня", 
                "Максимальное смещение от уровня игрока"));
            
            // Добавляем информационное сообщение для разработчика
            EditorGUILayout.HelpBox(
                "Уровень врага будет выбран случайно в диапазоне:\n" +
                "УровеньИгрока + МинСмещение до УровеньИгрока + МаксСмещение\n\n" +
                "Например, если уровень игрока 5, минимальное смещение -1, " +
                "максимальное смещение 2, то уровень врага будет от 4 до 7.", 
                MessageType.Info);
            
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        
        // Настройки отладки и визуализации
        showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Отладка и визуализация", true);
        if (showDebugSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(showDebugInfoProp, new GUIContent("Показывать отладочный вывод"));
            EditorGUILayout.PropertyField(drawLinesInGameProp, new GUIContent("Отрисовывать линии к врагам"));
            
            if (drawLinesInGameProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(debugLineColorProp, new GUIContent("Цвет линий"));
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        
        // Статистика точек спавна на сцене
        EditorGUILayout.LabelField("Статистика", EditorStyles.boldLabel);
        
        SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();
        SpawnZone[] spawnZones = FindObjectsOfType<SpawnZone>();
        
        EditorGUILayout.LabelField($"Точек спавна на сцене: {spawnPoints.Length}");
        EditorGUILayout.LabelField($"Зон спавна на сцене: {spawnZones.Length}");

        if (spawnPoints.Length > 0)
        {
            // Подсчет точек без врагов
            int emptyPoints = 0;
            int occupiedPoints = 0;
            HashSet<GameObject> uniqueEnemies = new HashSet<GameObject>();
            
            foreach (SpawnPoint point in spawnPoints)
            {
                if (point.IsOccupied())
                {
                    occupiedPoints++;
                }
                
                List<GameObject> prefabs = point.GetEnemyPrefabs();
                if (prefabs.Count == 0)
                {
                    emptyPoints++;
                }
                else
                {
                    foreach (GameObject enemy in prefabs)
                    {
                        if (enemy != null) uniqueEnemies.Add(enemy);
                    }
                }
            }

            EditorGUILayout.LabelField($"Точек без врагов: {emptyPoints}");
            EditorGUILayout.LabelField($"Занятых точек: {occupiedPoints}");
            EditorGUILayout.LabelField($"Уникальных типов врагов: {uniqueEnemies.Count}");

            if (emptyPoints > 0)
            {
                EditorGUILayout.HelpBox($"Внимание: {emptyPoints} точек не имеют назначенных врагов!", MessageType.Warning);
                
                if (GUILayout.Button("Выбрать точки без врагов"))
                {
                    List<GameObject> emptyPointsList = new List<GameObject>();
                    foreach (SpawnPoint point in spawnPoints)
                    {
                        if (point.GetEnemyPrefabs().Count == 0)
                        {
                            emptyPointsList.Add(point.gameObject);
                        }
                    }
                    
                    if (emptyPointsList.Count > 0)
                    {
                        Selection.objects = emptyPointsList.ToArray();
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("На сцене нет точек спавна. Добавьте их через меню RPG -> Create -> Enemy Spawn Point или через зоны спавна.", MessageType.Warning);
        }

        // Кнопки быстрого доступа
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Инструменты", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Создать точку спавна"))
        {
            SpawnPointEditor.CreateSpawnPoint();
        }
        
        if (GUILayout.Button("Создать зону спавна"))
        {
            SpawnZoneEditor.CreateSpawnZone();
        }
        EditorGUILayout.EndHorizontal();

        // Кнопка для тестового спавна врагов в игровом режиме
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Тестирование", EditorStyles.boldLabel);
        
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("Тест спавна врагов"))
        {
            if (Application.isPlaying)
            {
                EnemySpawner spawner = (EnemySpawner)target;
                spawner.SpawnEnemies();
            }
            else
            {
                EditorUtility.DisplayDialog("Ошибка", "Тестирование спавна доступно только в режиме игры!", "Понятно");
            }
        }
        
        // Кнопка для очистки сцены от врагов в игровом режиме
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Очистить всех врагов"))
            {
                // Находим всех врагов с AIController и удаляем их
                RPG.Control.AIController[] enemies = FindObjectsOfType<RPG.Control.AIController>();
                foreach (var enemy in enemies)
                {
                    DestroyImmediate(enemy.gameObject);
                }
                
                // Сбрасываем статус всех точек
                foreach (SpawnPoint point in spawnPoints)
                {
                    point.SetOccupied(false);
                }
                
                Debug.Log($"Очищено {enemies.Length} врагов со сцены");
            }
        }
        GUI.enabled = true;

        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("RPG/Create/Enemy Spawner")]
    public static void CreateEnemySpawner()
    {
        // Проверяем, существует ли уже спавнер на сцене
        EnemySpawner[] existingSpawners = FindObjectsOfType<EnemySpawner>();
        if (existingSpawners.Length > 0)
        {
            Selection.activeGameObject = existingSpawners[0].gameObject;
            EditorUtility.DisplayDialog("Внимание", "На сцене уже существует система спавна врагов!", "Понятно");
            return;
        }

        GameObject spawnerObj = new GameObject("Enemy Spawner");
        spawnerObj.AddComponent<EnemySpawner>();
        spawnerObj.AddComponent<GameDevTV.Saving.SaveableEntity>();
        
        // Выделяем созданный объект
        Selection.activeGameObject = spawnerObj;
        
        // Регистрируем создание для Undo
        Undo.RegisterCreatedObjectUndo(spawnerObj, "Create Enemy Spawner");
    }
}