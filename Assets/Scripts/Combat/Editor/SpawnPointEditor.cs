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
    SerializedProperty spawnConditionsProp;
    SerializedProperty componentsToAddProp;
    SerializedProperty patrolPathObjectProp;

    private bool showAdvancedSettings = false;
    private bool showConditionalSpawning = true;

    private void OnEnable()
    {
        enemyPrefabsProp = serializedObject.FindProperty("enemyPrefabs");
        spawnRadiusProp = serializedObject.FindProperty("spawnRadius");
        spawnChanceProp = serializedObject.FindProperty("spawnChance");
        isOccupiedProp = serializedObject.FindProperty("isOccupied");
        alwaysShowGizmoProp = serializedObject.FindProperty("alwaysShowGizmo");
        gizmoColorProp = serializedObject.FindProperty("gizmoColor");
        occupiedGizmoColorProp = serializedObject.FindProperty("occupiedGizmoColor");
        spawnConditionsProp = serializedObject.FindProperty("spawnConditions");
        componentsToAddProp = serializedObject.FindProperty("componentsToAdd");
        patrolPathObjectProp = serializedObject.FindProperty("patrolPathObject");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Настройки спавна", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enemyPrefabsProp, new GUIContent("Список врагов"), true);
        EditorGUILayout.PropertyField(spawnRadiusProp, new GUIContent("Радиус спавна"));
        EditorGUILayout.PropertyField(spawnChanceProp, new GUIContent("Шанс спавна"));

        EditorGUILayout.Space();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(isOccupiedProp, new GUIContent("Точка занята"));
        GUI.enabled = true;

        EditorGUILayout.Space();

        // ─── Патруль ────────────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Патруль", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(patrolPathObjectProp, new GUIContent("Путь патрулирования"));
        EditorGUILayout.HelpBox("Перетащи сюда GameObject с компонентом PatrolPath. Если оставить пустым — враг будет стоять на месте.", MessageType.Info);

        EditorGUILayout.Space();

        // ─── Условный спавн и динамические компоненты ───────────────────────────
        showConditionalSpawning = EditorGUILayout.Foldout(showConditionalSpawning, "Условный спавн и динамические компоненты", true);
        if (showConditionalSpawning)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Условный спавн", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spawnConditionsProp, new GUIContent("Условия спавна"));
            EditorGUILayout.HelpBox("Условия для спавна врага (квесты, диалоги и т.д.). Оставьте пустым для безусловного спавна.", MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Динамические компоненты", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(componentsToAddProp, new GUIContent("Динамические компоненты"), true);
            EditorGUILayout.HelpBox("Компоненты, которые будут добавлены на врага при спавне (например, QuestProgress).", MessageType.Info);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // ─── Визуализация и отладка ──────────────────────────────────────────────
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Визуализация и отладка", true);
        if (showAdvancedSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(alwaysShowGizmoProp, new GUIContent("Всегда показывать Gizmo"));
            EditorGUILayout.PropertyField(gizmoColorProp, new GUIContent("Цвет точки"));
            EditorGUILayout.PropertyField(occupiedGizmoColorProp, new GUIContent("Цвет занятой точки"));

            if (GUILayout.Button("Сбросить цвета"))
            {
                gizmoColorProp.colorValue = Color.green;
                occupiedGizmoColorProp.colorValue = Color.red;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        if (enemyPrefabsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Добавьте хотя бы одного врага в список!", MessageType.Warning);
        }

        if (spawnRadiusProp.floatValue < 0.5f)
        {
            EditorGUILayout.HelpBox("Радиус спавна слишком мал. Рекомендуется значение не менее 0.5.", MessageType.Info);
        }

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Тестирование", EditorStyles.boldLabel);

            SpawnPoint spawnPoint = (SpawnPoint)target;

            EditorGUILayout.BeginHorizontal();

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

        if (SceneView.lastActiveSceneView != null)
        {
            spawnPoint.transform.position = SceneView.lastActiveSceneView.pivot;
        }

        Selection.activeGameObject = spawnPoint;

        Undo.RegisterCreatedObjectUndo(spawnPoint, "Create Enemy Spawn Point");
    }
}
