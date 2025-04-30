using UnityEngine;
using UnityEditor;
using RPG.Combat;

[CustomEditor(typeof(SpawnPoint))]
public class SpawnPointEditor : Editor
{
    SerializedProperty enemyPrefabsProp;
    SerializedProperty spawnRadiusProp;
    SerializedProperty spawnChanceProp;
    SerializedProperty isOccupiedProp;
    SerializedProperty alwaysShowGizmoProp;
    SerializedProperty gizmoColorProp;
    SerializedProperty occupiedGizmoColorProp;

    private bool showAdvancedSettings = false;

    private void OnEnable()
    {
        enemyPrefabsProp = serializedObject.FindProperty("enemyPrefabs");
        spawnRadiusProp = serializedObject.FindProperty("spawnRadius");
        spawnChanceProp = serializedObject.FindProperty("spawnChance");
        isOccupiedProp = serializedObject.FindProperty("isOccupied");
        alwaysShowGizmoProp = serializedObject.FindProperty("alwaysShowGizmo");
        gizmoColorProp = serializedObject.FindProperty("gizmoColor");
        occupiedGizmoColorProp = serializedObject.FindProperty("occupiedGizmoColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Основные настройки
        EditorGUILayout.LabelField("Настройки спавна", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enemyPrefabsProp, new GUIContent("Список врагов"), true);
        EditorGUILayout.PropertyField(spawnRadiusProp, new GUIContent("Радиус спавна"));
        EditorGUILayout.PropertyField(spawnChanceProp, new GUIContent("Шанс спавна"));
        
        EditorGUILayout.Space();
        
        // Состояние точки спавна
        GUI.enabled = false; // Делаем поле isOccupied только для чтения
        EditorGUILayout.PropertyField(isOccupiedProp, new GUIContent("Точка занята"));
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // Фолдаут для расширенных настроек
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Визуализация и отладка", true);
        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(alwaysShowGizmoProp, new GUIContent("Всегда показывать Gizmo"));
            EditorGUILayout.PropertyField(gizmoColorProp, new GUIContent("Цвет точки"));
            EditorGUILayout.PropertyField(occupiedGizmoColorProp, new GUIContent("Цвет занятой точки"));
            
            // Кнопка сброса цветов к стандартным
            if (GUILayout.Button("Сбросить цвета"))
            {
                gizmoColorProp.colorValue = Color.green;
                occupiedGizmoColorProp.colorValue = Color.red;
            }
            
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        
        // Вывод предупреждений
        if (enemyPrefabsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Добавьте хотя бы одного врага в список!", MessageType.Warning);
        }
        
        if (spawnRadiusProp.floatValue < 0.5f)
        {
            EditorGUILayout.HelpBox("Радиус спавна слишком мал. Рекомендуется значение не менее 0.5.", MessageType.Info);
        }
        
        // Кнопки тестирования в режиме игры
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Тестирование", EditorStyles.boldLabel);
            
            SpawnPoint spawnPoint = (SpawnPoint)target;
            
            EditorGUILayout.BeginHorizontal();
            
            // Кнопка для ручного спавна врага
            if (GUILayout.Button("Тестовый спавн"))
            {
                GameObject prefab = spawnPoint.GetRandomEnemyPrefab();
                if (prefab != null)
                {
                    Vector3 pos = spawnPoint.GetSpawnPosition();
                    GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
                    Debug.Log($"Создан тестовый враг: {enemy.name} в позиции {pos}");
                }
                else
                {
                    Debug.LogWarning("Нет префабов врагов для спавна!");
                }
            }
            
            // Кнопка для изменения статуса занятости
            string statusButtonText = spawnPoint.IsOccupied() ? "Освободить точку" : "Занять точку";
            if (GUILayout.Button(statusButtonText))
            {
                spawnPoint.SetOccupied(!spawnPoint.IsOccupied());
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    [MenuItem("RPG/Create/Enemy Spawn Point")]
    public static void CreateSpawnPoint()
    {
        GameObject spawnPoint = new GameObject("Enemy Spawn Point");
        spawnPoint.AddComponent<SpawnPoint>();
        
        // Размещаем точку спавна в текущей позиции сцены
        if (SceneView.lastActiveSceneView != null)
        {
            spawnPoint.transform.position = SceneView.lastActiveSceneView.pivot;
        }
        
        // Выделяем созданный объект
        Selection.activeGameObject = spawnPoint;
        
        // Регистрируем создание для Undo
        Undo.RegisterCreatedObjectUndo(spawnPoint, "Create Enemy Spawn Point");
    }
} 