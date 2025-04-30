using UnityEngine;
using UnityEditor;
using RPG.Combat;
using System.Collections.Generic;

[CustomEditor(typeof(SpawnZone))]
public class SpawnZoneEditor : Editor
{
    SerializedProperty zoneNameProp;
    SerializedProperty zoneColorProp;
    SerializedProperty zoneSizeProp;
    SerializedProperty spawnModeProp;
    SerializedProperty activationRateProp;
    SerializedProperty exactEnemyCountProp;
    SerializedProperty minEnemyCountProp;
    SerializedProperty maxEnemyCountProp;
    SerializedProperty autoAssignPointsProp;
    SerializedProperty alwaysShowGizmoProp;
    SerializedProperty showContainedSpawnPointsProp;
    SerializedProperty showZoneInfoProp;
    
    private bool showVisualizationSettings = false;
    private int pointsToCreate = 5;

    private void OnEnable()
    {
        zoneNameProp = serializedObject.FindProperty("zoneName");
        zoneColorProp = serializedObject.FindProperty("zoneColor");
        zoneSizeProp = serializedObject.FindProperty("zoneSize");
        spawnModeProp = serializedObject.FindProperty("spawnMode");
        activationRateProp = serializedObject.FindProperty("activationRate");
        exactEnemyCountProp = serializedObject.FindProperty("exactEnemyCount");
        minEnemyCountProp = serializedObject.FindProperty("minEnemyCount");
        maxEnemyCountProp = serializedObject.FindProperty("maxEnemyCount");
        autoAssignPointsProp = serializedObject.FindProperty("autoAssignPoints");
        alwaysShowGizmoProp = serializedObject.FindProperty("alwaysShowGizmo");
        showContainedSpawnPointsProp = serializedObject.FindProperty("showContainedSpawnPoints");
        showZoneInfoProp = serializedObject.FindProperty("showZoneInfo");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Основные настройки зоны
        EditorGUILayout.LabelField("Настройки зоны", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(zoneNameProp, new GUIContent("Название зоны"));
        EditorGUILayout.PropertyField(zoneColorProp, new GUIContent("Цвет зоны"));
        EditorGUILayout.PropertyField(zoneSizeProp, new GUIContent("Размер зоны"));
        
        EditorGUILayout.Space();
        
        // Настройки режима спавна
        EditorGUILayout.LabelField("Настройки спавна", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnModeProp, new GUIContent("Режим спавна"));
        
        // Показываем соответствующие настройки в зависимости от выбранного режима
        SpawnZone.SpawnMode currentMode = (SpawnZone.SpawnMode)spawnModeProp.enumValueIndex;
        
        switch (currentMode)
        {
            case SpawnZone.SpawnMode.Random:
                EditorGUILayout.PropertyField(activationRateProp, new GUIContent("Коэффициент активации"));
                break;
                
            case SpawnZone.SpawnMode.Exact:
                EditorGUILayout.PropertyField(exactEnemyCountProp, new GUIContent("Точное количество врагов"));
                break;
                
            case SpawnZone.SpawnMode.RandomRange:
                EditorGUILayout.PropertyField(minEnemyCountProp, new GUIContent("Минимум врагов"));
                EditorGUILayout.PropertyField(maxEnemyCountProp, new GUIContent("Максимум врагов"));
                
                // Проверка валидности диапазона
                if (minEnemyCountProp.intValue > maxEnemyCountProp.intValue)
                {
                    EditorGUILayout.HelpBox("Минимальное количество не может быть больше максимального!", MessageType.Error);
                }
                break;
        }
        
        EditorGUILayout.PropertyField(autoAssignPointsProp, new GUIContent("Автоназначение точек"));

        EditorGUILayout.Space();
        
        // Настройки визуализации (в фолдауте)
        showVisualizationSettings = EditorGUILayout.Foldout(showVisualizationSettings, "Настройки визуализации", true);
        if (showVisualizationSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(alwaysShowGizmoProp, new GUIContent("Всегда показывать Gizmo"));
            EditorGUILayout.PropertyField(showContainedSpawnPointsProp, new GUIContent("Показывать точки спавна"));
            EditorGUILayout.PropertyField(showZoneInfoProp, new GUIContent("Показывать информацию"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Подсчет количества точек в зоне
        SpawnZone spawnZone = (SpawnZone)target;
        spawnZone.RefreshSpawnPoints();
        List<SpawnPoint> zonePoints = spawnZone.GetSpawnPoints();
        
        EditorGUILayout.LabelField("Информация о зоне", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Точек спавна в зоне: {zonePoints.Count}");
        
        // Статистика в зависимости от режима
        switch (currentMode)
        {
            case SpawnZone.SpawnMode.Random:
                int activeCount = Mathf.Max(1, Mathf.RoundToInt(zonePoints.Count * activationRateProp.floatValue));
                EditorGUILayout.LabelField($"Активных точек: {activeCount} из {zonePoints.Count} " +
                                        $"({activationRateProp.floatValue * 100}%)");
                break;
                
            case SpawnZone.SpawnMode.Exact:
                int exactCount = Mathf.Min(exactEnemyCountProp.intValue, zonePoints.Count);
                if (exactEnemyCountProp.intValue > zonePoints.Count)
                {
                    EditorGUILayout.HelpBox($"Внимание: указано {exactEnemyCountProp.intValue} врагов, " +
                                          $"но в зоне только {zonePoints.Count} точек спавна!", MessageType.Warning);
                }
                EditorGUILayout.LabelField($"Будет использовано точек: {exactCount}");
                break;
                
            case SpawnZone.SpawnMode.RandomRange:
                if (zonePoints.Count < minEnemyCountProp.intValue)
                {
                    EditorGUILayout.HelpBox($"Внимание: минимум {minEnemyCountProp.intValue} врагов, " +
                                          $"но в зоне только {zonePoints.Count} точек спавна!", MessageType.Warning);
                }
                EditorGUILayout.LabelField($"Случайное количество: от {Mathf.Min(minEnemyCountProp.intValue, zonePoints.Count)} " +
                                        $"до {Mathf.Min(maxEnemyCountProp.intValue, zonePoints.Count)}");
                break;
        }
        
        // Статистика по врагам в точках
        if (zonePoints.Count > 0)
        {
            int pointsWithEnemies = 0;
            HashSet<GameObject> uniqueEnemies = new HashSet<GameObject>();
            
            foreach (SpawnPoint point in zonePoints)
            {
                var prefabs = point.GetEnemyPrefabs();
                if (prefabs.Count > 0)
                {
                    pointsWithEnemies++;
                    foreach (GameObject enemy in prefabs)
                    {
                        if (enemy != null) uniqueEnemies.Add(enemy);
                    }
                }
            }
            
            EditorGUILayout.LabelField($"Точек с врагами: {pointsWithEnemies} из {zonePoints.Count}");
            EditorGUILayout.LabelField($"Уникальных типов врагов: {uniqueEnemies.Count}");
            
            // Кнопка для выбора точек без врагов
            if (pointsWithEnemies < zonePoints.Count)
            {
                if (GUILayout.Button("Выбрать точки без врагов"))
                {
                    List<GameObject> emptyPoints = new List<GameObject>();
                    foreach (SpawnPoint point in zonePoints)
                    {
                        if (point.GetEnemyPrefabs().Count == 0)
                        {
                            emptyPoints.Add(point.gameObject);
                        }
                    }
                    
                    if (emptyPoints.Count > 0)
                    {
                        Selection.objects = emptyPoints.ToArray();
                    }
                }
            }
            
            // Кнопка для выбора всех точек в зоне
            if (GUILayout.Button("Выбрать все точки в зоне"))
            {
                Selection.objects = zonePoints.ConvertAll(p => p.gameObject).ToArray();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("В зоне нет точек спавна. Создайте их с помощью кнопки ниже.", MessageType.Info);
        }
        
        // Система создания точек
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Создание точек спавна", EditorStyles.boldLabel);
        
        pointsToCreate = EditorGUILayout.IntSlider("Количество точек", pointsToCreate, 1, 20);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Создать точки"))
        {
            spawnZone.CreateSpawnPoints(pointsToCreate);
        }
        
        if (GUILayout.Button("Очистить зону"))
        {
            spawnZone.ClearZone();
        }
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("RPG/Create/Spawn Zone")]
    public static void CreateSpawnZone()
    {
        GameObject zoneObj = new GameObject("Spawn Zone");
        zoneObj.AddComponent<SpawnZone>();
        
        // Размещаем зону в текущей позиции сцены
        if (SceneView.lastActiveSceneView != null)
        {
            zoneObj.transform.position = SceneView.lastActiveSceneView.pivot;
        }
        
        // Выделяем созданный объект
        Selection.activeGameObject = zoneObj;
        
        // Регистрируем создание для Undo
        Undo.RegisterCreatedObjectUndo(zoneObj, "Create Spawn Zone");
    }
    
    // Добавляем опцию для создания точек спавна при выбранной зоне
    [MenuItem("RPG/Add Spawn Points to Selected Zone")]
    public static void AddSpawnPointsToSelectedZone()
    {
        SpawnZone selectedZone = Selection.activeGameObject?.GetComponent<SpawnZone>();
        if (selectedZone != null)
        {
            int count = EditorUtility.DisplayDialogComplex("Добавление точек спавна",
                "Сколько точек спавна вы хотите добавить?",
                "5", "10", "Другое");
            
            int pointsToCreate = count == 0 ? 5 : count == 1 ? 10 : 0;
            
            if (count == 2) // Выбрали "Другое"
            {
                string input = EditorInputDialog.Show("Количество точек", "Введите количество точек спавна:", "3");
                if (int.TryParse(input, out int result))
                {
                    pointsToCreate = Mathf.Clamp(result, 1, 50);
                }
                else
                {
                    return; // Отмена
                }
            }
            
            selectedZone.CreateSpawnPoints(pointsToCreate);
        }
        else
        {
            EditorUtility.DisplayDialog("Ошибка", "Сначала выберите объект с компонентом SpawnZone!", "ОК");
        }
    }
    
    // Проверка видимости пункта меню
    [MenuItem("RPG/Add Spawn Points to Selected Zone", true)]
    public static bool ValidateAddSpawnPointsToSelectedZone()
    {
        return Selection.activeGameObject?.GetComponent<SpawnZone>() != null;
    }
    
    // Меню для быстрого заполнения всех точек врагами из первой точки
    [MenuItem("RPG/Copy Enemies to All Points in Zone")]
    public static void CopyEnemiesToAllPoints()
    {
        SpawnZone selectedZone = Selection.activeGameObject?.GetComponent<SpawnZone>();
        if (selectedZone != null)
        {
            List<SpawnPoint> points = selectedZone.GetSpawnPoints();
            if (points.Count < 2)
            {
                EditorUtility.DisplayDialog("Ошибка", "В зоне меньше 2 точек спавна!", "ОК");
                return;
            }
            
            // Находим точку с врагами
            SpawnPoint sourcePoint = null;
            foreach (SpawnPoint point in points)
            {
                if (point.GetEnemyPrefabs().Count > 0)
                {
                    sourcePoint = point;
                    break;
                }
            }
            
            if (sourcePoint == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Ни одна точка в зоне не содержит врагов!", "ОК");
                return;
            }
            
            if (EditorUtility.DisplayDialog("Копирование врагов", 
                $"Скопировать список врагов из точки {sourcePoint.name} во все остальные точки в зоне?",
                "Да", "Отмена"))
            {
                List<GameObject> sourcePrefabs = sourcePoint.GetEnemyPrefabs();
                int updatedPoints = 0;
                
                foreach (SpawnPoint point in points)
                {
                    if (point == sourcePoint) continue;
                    
                    SerializedObject pointObj = new SerializedObject(point);
                    SerializedProperty enemyPrefabsProp = pointObj.FindProperty("enemyPrefabs");
                    
                    // Очищаем существующий список
                    enemyPrefabsProp.ClearArray();
                    
                    // Добавляем врагов из исходной точки
                    for (int i = 0; i < sourcePrefabs.Count; i++)
                    {
                        enemyPrefabsProp.arraySize++;
                        enemyPrefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = sourcePrefabs[i];
                    }
                    
                    pointObj.ApplyModifiedProperties();
                    updatedPoints++;
                }
                
                EditorUtility.DisplayDialog("Готово", $"Враги скопированы в {updatedPoints} точек спавна.", "ОК");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Ошибка", "Сначала выберите объект с компонентом SpawnZone!", "ОК");
        }
    }
    
    // Проверка видимости пункта меню
    [MenuItem("RPG/Copy Enemies to All Points in Zone", true)]
    public static bool ValidateCopyEnemiesToAllPoints()
    {
        return Selection.activeGameObject?.GetComponent<SpawnZone>() != null;
    }
}

// Простое окно ввода для запроса текстовых данных
public class EditorInputDialog : EditorWindow
{
    public static string Show(string title, string message, string defaultText)
    {
        EditorInputDialog window = CreateInstance<EditorInputDialog>();
        window.titleContent = new GUIContent(title);
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
        window.minSize = new Vector2(300, 100);
        window._message = message;
        window._inputText = defaultText;
        window.ShowModal();
        return window._result;
    }

    private string _message = "";
    private string _inputText = "";
    private string _result = null;

    private void OnGUI()
    {
        EditorGUILayout.LabelField(_message);
        _inputText = EditorGUILayout.TextField(_inputText);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("ОК"))
        {
            _result = _inputText;
            Close();
        }
        
        if (GUILayout.Button("Отмена"))
        {
            _result = null;
            Close();
        }
        
        EditorGUILayout.EndHorizontal();
    }
} 