using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Combat;
using UnityEngine.SceneManagement;

public class ArmorPrefabCreator : EditorWindow
{
    private GameObject armorGameObject;
    private string selectedArmorType = "BodyArmor";
    private List<string> armorTypesList = new List<string>();
    private Dictionary<string, string> armorPathMapping = new Dictionary<string, string>();
    private Dictionary<string, Type> armorTypeMapping = new Dictionary<string, Type>();
    
    // Префаб пикапа
    private GameObject rootArmorPickupPrefab;
    private List<GameObject> lootRayPrefabs = new List<GameObject>();
    private int selectedLootRayIndex = 0;
    private GameObject playerPrefab;
    
    // Настройки трансформа для Static модели
    private Vector3 staticModelPosition = Vector3.zero;
    private Vector3 staticModelRotation = Vector3.zero;
    private Vector3 staticModelScale = Vector3.one;
    
    // Предпросмотр
    private GameObject previewInstance;
    private GameObject playerPreviewInstance;
    private Editor gameObjectPreviewEditor;
    private bool showPreview = true;
    
    // Скроллинг
    private Vector2 scrollPosition;

    // Сохранение настроек
    private const string LootRayPrefabsKey = "ArmorPrefabCreator_LootRayPrefabs";
    private const string StaticModelPositionKey = "ArmorPrefabCreator_StaticModelPosition";
    private const string StaticModelRotationKey = "ArmorPrefabCreator_StaticModelRotation";
    private const string StaticModelScaleKey = "ArmorPrefabCreator_StaticModelScale";

    [MenuItem("RPG/Armor/Create Armor Prefab")]
    public static void ShowWindow()
    {
        GetWindow<ArmorPrefabCreator>("Armor Prefab Creator");
    }

    private void OnEnable()
    {
        Initialize();
        LoadSettings();
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    private void OnDisable() 
    {
        SaveSettings();
        CleanupPreview();
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (previewInstance != null)
        {
            Handles.Label(previewInstance.transform.position + Vector3.up * 2, "Armor Pickup Preview");
        }
    }
    
    private void LoadSettings()
    {
        // Загрузка префабов LootRay
        string lootRayPrefabsData = EditorPrefs.GetString(LootRayPrefabsKey, "");
        if (!string.IsNullOrEmpty(lootRayPrefabsData))
        {
            string[] assetPaths = lootRayPrefabsData.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            lootRayPrefabs.Clear();
            
            foreach (string path in assetPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    lootRayPrefabs.Add(prefab);
                }
            }
        }
        
        // Загрузка настроек трансформа
        string positionData = EditorPrefs.GetString(StaticModelPositionKey, "");
        if (!string.IsNullOrEmpty(positionData))
        {
            string[] values = positionData.Split(',');
            if (values.Length == 3)
            {
                float x = float.Parse(values[0]);
                float y = float.Parse(values[1]);
                float z = float.Parse(values[2]);
                staticModelPosition = new Vector3(x, y, z);
            }
        }
        
        string rotationData = EditorPrefs.GetString(StaticModelRotationKey, "");
        if (!string.IsNullOrEmpty(rotationData))
        {
            string[] values = rotationData.Split(',');
            if (values.Length == 3)
            {
                float x = float.Parse(values[0]);
                float y = float.Parse(values[1]);
                float z = float.Parse(values[2]);
                staticModelRotation = new Vector3(x, y, z);
            }
        }
        
        string scaleData = EditorPrefs.GetString(StaticModelScaleKey, "");
        if (!string.IsNullOrEmpty(scaleData))
        {
            string[] values = scaleData.Split(',');
            if (values.Length == 3)
            {
                float x = float.Parse(values[0]);
                float y = float.Parse(values[1]);
                float z = float.Parse(values[2]);
                staticModelScale = new Vector3(x, y, z);
            }
        }
        
        // Загрузка Root Armor Pickup префаба
        rootArmorPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Internal Assets/Prefabs/Pickups/Root Armor Pickup.prefab");
    }
    
    private void SaveSettings()
    {
        // Сохранение префабов LootRay
        List<string> paths = new List<string>();
        foreach (var prefab in lootRayPrefabs)
        {
            if (prefab != null)
            {
                paths.Add(AssetDatabase.GetAssetPath(prefab));
            }
        }
        EditorPrefs.SetString(LootRayPrefabsKey, string.Join("|", paths));
        
        // Сохранение настроек трансформа
        EditorPrefs.SetString(StaticModelPositionKey, $"{staticModelPosition.x},{staticModelPosition.y},{staticModelPosition.z}");
        EditorPrefs.SetString(StaticModelRotationKey, $"{staticModelRotation.x},{staticModelRotation.y},{staticModelRotation.z}");
        EditorPrefs.SetString(StaticModelScaleKey, $"{staticModelScale.x},{staticModelScale.y},{staticModelScale.z}");
    }

    private void Initialize()
    {
        // Получение всех типов брони из директории
        armorTypesList.Clear();
        armorPathMapping.Clear();
        armorTypeMapping.Clear();
        
        // Сопоставление типа брони с путем для сохранения префаба
        armorPathMapping.Add("BodyArmor", "Assets/Internal Assets/Armor/Body");
        armorPathMapping.Add("HelmetArmor", "Assets/Internal Assets/Armor/Helmet");
        armorPathMapping.Add("UpperArmLeftArmor", "Assets/Internal Assets/Armor/UpperArms");
        armorPathMapping.Add("UpperArmRightArmor", "Assets/Internal Assets/Armor/UpperArms");
        armorPathMapping.Add("LowerArmLeftArmor", "Assets/Internal Assets/Armor/LowerArms");
        armorPathMapping.Add("LowerArmRightArmor", "Assets/Internal Assets/Armor/LowerArms");
        armorPathMapping.Add("GloveLeftArmor", "Assets/Internal Assets/Armor/Gloves");
        armorPathMapping.Add("GloveRightArmor", "Assets/Internal Assets/Armor/Gloves");
        armorPathMapping.Add("TrousersArmor", "Assets/Internal Assets/Armor/Trousers");
        armorPathMapping.Add("BootLeftArmor", "Assets/Internal Assets/Armor/Boots");
        armorPathMapping.Add("BootRightArmor", "Assets/Internal Assets/Armor/Boots");
        
        // Сопоставление строковых имен с типами компонентов
        armorTypeMapping.Add("BodyArmor", typeof(BodyArmor));
        armorTypeMapping.Add("HelmetArmor", typeof(HelmetArmor));
        armorTypeMapping.Add("UpperArmLeftArmor", typeof(UpperArmLeftArmor));
        armorTypeMapping.Add("UpperArmRightArmor", typeof(UpperArmRightArmor));
        armorTypeMapping.Add("LowerArmLeftArmor", typeof(LowerArmLeftArmor));
        armorTypeMapping.Add("LowerArmRightArmor", typeof(LowerArmRightArmor));
        armorTypeMapping.Add("GloveLeftArmor", typeof(GloveLeftArmor));
        armorTypeMapping.Add("GloveRightArmor", typeof(GloveRightArmor));
        armorTypeMapping.Add("TrousersArmor", typeof(TrousersArmor));
        armorTypeMapping.Add("BootLeftArmor", typeof(BootLeftArmor));
        armorTypeMapping.Add("BootRightArmor", typeof(BootRightArmor));
        
        // Добавляем типы брони в список для отображения в выпадающем меню
        foreach (var key in armorTypeMapping.Keys)
        {
            armorTypesList.Add(key);
        }
    }
    
    private void UpdatePreview()
    {
        CleanupPreview();
        
        if (armorGameObject == null || rootArmorPickupPrefab == null) return;
        
        // Находим префаб Static
        string staticPrefabName = armorGameObject.name + "_Static";
        GameObject staticPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonFantasyHeroCharacters/Prefabs/Characters_ModularParts_Static/" + staticPrefabName + ".prefab");
        
        if (staticPrefab == null)
        {
            Debug.LogWarning("Не найден статичный префаб: " + staticPrefabName);
            return;
        }
        
        // Создаем превью пикапа
        previewInstance = Instantiate(rootArmorPickupPrefab);
        previewInstance.name = "Preview_Pickup";
        previewInstance.transform.position = Vector3.zero;
        
        // Добавляем статичную модель
        GameObject staticInstance = Instantiate(staticPrefab, previewInstance.transform);
        staticInstance.transform.localPosition = staticModelPosition;
        staticInstance.transform.localRotation = Quaternion.Euler(staticModelRotation);
        staticInstance.transform.localScale = staticModelScale;
        
        // Добавляем LootRay если есть
        if (lootRayPrefabs.Count > 0 && selectedLootRayIndex < lootRayPrefabs.Count && lootRayPrefabs[selectedLootRayIndex] != null)
        {
            Instantiate(lootRayPrefabs[selectedLootRayIndex], previewInstance.transform);
        }
        
        // Добавляем игрока для масштаба если он задан
        if (playerPrefab != null)
        {
            playerPreviewInstance = Instantiate(playerPrefab);
            playerPreviewInstance.name = "Preview_Player";
            playerPreviewInstance.transform.position = previewInstance.transform.position + Vector3.right * 1.5f;
        }
        
        // Обновляем редактор предпросмотра
        if (gameObjectPreviewEditor != null)
        {
            DestroyImmediate(gameObjectPreviewEditor);
        }
        
        // Используем класс-контейнер для предпросмотра
        GameObject previewContainer = new GameObject("PreviewContainer");
        previewContainer.hideFlags = HideFlags.HideAndDontSave;
        
        if (previewInstance != null)
            previewInstance.transform.SetParent(previewContainer.transform, true);
            
        if (playerPreviewInstance != null)
            playerPreviewInstance.transform.SetParent(previewContainer.transform, true);
        
        gameObjectPreviewEditor = Editor.CreateEditor(previewContainer);
    }
    
    private void CleanupPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
        
        if (playerPreviewInstance != null)
        {
            DestroyImmediate(playerPreviewInstance);
            playerPreviewInstance = null;
        }
        
        if (gameObjectPreviewEditor != null)
        {
            DestroyImmediate(gameObjectPreviewEditor);
            gameObjectPreviewEditor = null;
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Создание префаба брони", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);
        
        // Шаг 1: Выбор GameObject
        GUILayout.Label("Шаг 1: Выберите GameObject брони", EditorStyles.boldLabel);
        GameObject prevObject = armorGameObject;
        armorGameObject = (GameObject)EditorGUILayout.ObjectField("GameObject брони:", armorGameObject, typeof(GameObject), true);
        
        if (prevObject != armorGameObject)
        {
            UpdatePreview();
        }
        
        EditorGUILayout.Space(10);
        
        // Шаг 2: Выбор типа брони
        GUILayout.Label("Шаг 2: Выберите тип брони", EditorStyles.boldLabel);
        int selectedIndex = armorTypesList.IndexOf(selectedArmorType);
        int newSelectedIndex = EditorGUILayout.Popup("Тип брони:", selectedIndex, armorTypesList.ToArray());
        if (newSelectedIndex != selectedIndex && newSelectedIndex >= 0 && newSelectedIndex < armorTypesList.Count)
        {
            selectedArmorType = armorTypesList[newSelectedIndex];
            UpdatePreview();
        }
        
        EditorGUILayout.Space(10);
        
        // Шаг 3: Настройка пикапа брони
        GUILayout.Label("Шаг 3: Настройка пикапа брони", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        rootArmorPickupPrefab = (GameObject)EditorGUILayout.ObjectField("Root Armor Pickup префаб:", rootArmorPickupPrefab, typeof(GameObject), false);
        
        GUILayout.Label("LootRay префабы:", EditorStyles.boldLabel);
        
        // Список LootRay префабов
        for (int i = 0; i < lootRayPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            lootRayPrefabs[i] = (GameObject)EditorGUILayout.ObjectField($"LootRay {i+1}:", lootRayPrefabs[i], typeof(GameObject), false);
            
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                lootRayPrefabs.RemoveAt(i);
                i--;
            }
            
            if (GUILayout.Button("Выбрать", GUILayout.Width(70)))
            {
                selectedLootRayIndex = i;
                UpdatePreview();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUILayout.Button("Добавить LootRay префаб"))
        {
            lootRayPrefabs.Add(null);
        }
        
        GUILayout.Space(10);
        
        // Выбор используемого LootRay
        string[] lootRayNames = lootRayPrefabs.Select((prefab, index) => 
            prefab != null ? $"LootRay {index+1}: {prefab.name}" : $"LootRay {index+1}: <Не задан>").ToArray();
            
        if (lootRayNames.Length > 0)
        {
            int newLootRayIndex = EditorGUILayout.Popup("Использовать LootRay:", selectedLootRayIndex, lootRayNames);
            if (newLootRayIndex != selectedLootRayIndex)
            {
                selectedLootRayIndex = newLootRayIndex;
                UpdatePreview();
            }
        }
        
        GUILayout.Space(10);
        
        // Настройки трансформа для статичной модели
        GUILayout.Label("Настройка трансформа для Static модели:", EditorStyles.boldLabel);
        
        Vector3 newPosition = EditorGUILayout.Vector3Field("Позиция:", staticModelPosition);
        Vector3 newRotation = EditorGUILayout.Vector3Field("Поворот:", staticModelRotation);
        Vector3 newScale = EditorGUILayout.Vector3Field("Масштаб:", staticModelScale);
        
        if (newPosition != staticModelPosition || newRotation != staticModelRotation || newScale != staticModelScale)
        {
            staticModelPosition = newPosition;
            staticModelRotation = newRotation;
            staticModelScale = newScale;
            UpdatePreview();
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }
        
        GUILayout.Space(20);
        
        // Предпросмотр
        GUILayout.Label("Предпросмотр пикапа:", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        playerPrefab = (GameObject)EditorGUILayout.ObjectField("Префаб игрока (для масштаба):", playerPrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            UpdatePreview();
        }
        
        showPreview = EditorGUILayout.Foldout(showPreview, "Показать предпросмотр");
        
        if (showPreview && gameObjectPreviewEditor != null)
        {
            // Увеличиваем высоту окна предпросмотра
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(400));
            Rect previewRect = GUILayoutUtility.GetLastRect();
            
            // Масштабирование и центрирование объектов в окне предпросмотра
            gameObjectPreviewEditor.OnPreviewSettings();
            gameObjectPreviewEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.whiteLabel);
            
            // Добавляем поясняющий текст
            Handles.BeginGUI();
            GUI.Label(new Rect(previewRect.x + 10, previewRect.y + 10, 200, 20), "←Пикап   Игрок→", EditorStyles.boldLabel);
            Handles.EndGUI();
        }
        
        EditorGUILayout.Space(20);
        
        // Кнопка для создания префаба и ScriptableObject
        GUI.enabled = (armorGameObject != null && rootArmorPickupPrefab != null);
        if (GUILayout.Button("Создать префаб и ScriptableObject"))
        {
            CreateArmorPrefab();
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndScrollView();
    }

    private void CreateArmorPrefab()
    {
        // Проверка наличия GameObject
        if (armorGameObject == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите GameObject брони", "OK");
            return;
        }

        try
        {
            // Шаг 1: Добавление компонента брони к GameObject
            Type armorType = armorTypeMapping[selectedArmorType];
            if (!armorGameObject.GetComponent(armorType))
            {
                armorGameObject.AddComponent(armorType);
            }

            // Шаг 2: Создание префаба
            string prefabPath = armorPathMapping[selectedArmorType];
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
            }

            string prefabName = $"{armorGameObject.name}_{selectedArmorType}";
            string fullPrefabPath = $"{prefabPath}/{prefabName}.prefab";
            
            // Создаем префаб
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(armorGameObject, fullPrefabPath);
            
            if (prefabAsset != null)
            {
                Debug.Log($"Префаб создан: {fullPrefabPath}");
                
                // Шаг 3: Создание пикапа брони
                if (rootArmorPickupPrefab != null)
                {
                    GameObject pickupPrefab = CreateArmorPickup(prefabName);
                    if (pickupPrefab != null)
                    {
                        Debug.Log($"Префаб пикапа создан: {AssetDatabase.GetAssetPath(pickupPrefab)}");
                    }
                }
                
                // Шаг 4: Создание ScriptableObject
                CreateArmorScriptableObject(prefabAsset, prefabName);
            }
            else
            {
                EditorUtility.DisplayDialog("Ошибка", "Не удалось создать префаб брони", "OK");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка при создании префаба: {e.Message}");
            EditorUtility.DisplayDialog("Ошибка", $"Произошла ошибка: {e.Message}", "OK");
        }
    }

    private GameObject CreateArmorPickup(string prefabName)
    {
        try
        {
            // Проверка наличия корневого префаба пикапа
            if (rootArmorPickupPrefab == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Не найден корневой префаб пикапа", "OK");
                return null;
            }
            
            // Находим префаб Static
            string staticPrefabName = armorGameObject.name + "_Static";
            string staticPrefabPath = "Assets/PolygonFantasyHeroCharacters/Prefabs/Characters_ModularParts_Static/" + staticPrefabName + ".prefab";
            GameObject staticPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(staticPrefabPath);
            
            if (staticPrefab == null)
            {
                EditorUtility.DisplayDialog("Ошибка", $"Не найден статичный префаб: {staticPrefabName}", "OK");
                return null;
            }
            
            // Создаем Prefab Variant
            string pickupPrefabPath = $"{armorPathMapping[selectedArmorType]}/Pickups";
            if (!Directory.Exists(pickupPrefabPath))
            {
                Directory.CreateDirectory(pickupPrefabPath);
            }
            
            string pickupPrefabName = $"{prefabName}_Pickup";
            string fullPickupPrefabPath = $"{pickupPrefabPath}/{pickupPrefabName}.prefab";
            
            // Сначала создаем временный объект из корневого префаба
            GameObject pickupInstance = PrefabUtility.InstantiatePrefab(rootArmorPickupPrefab) as GameObject;
            pickupInstance.name = pickupPrefabName;
            
            // Добавляем статичную модель
            GameObject staticInstance = PrefabUtility.InstantiatePrefab(staticPrefab, pickupInstance.transform) as GameObject;
            staticInstance.transform.localPosition = staticModelPosition;
            staticInstance.transform.localRotation = Quaternion.Euler(staticModelRotation);
            staticInstance.transform.localScale = staticModelScale;
            
            // Добавляем LootRay если выбран
            if (lootRayPrefabs.Count > 0 && selectedLootRayIndex < lootRayPrefabs.Count && lootRayPrefabs[selectedLootRayIndex] != null)
            {
                PrefabUtility.InstantiatePrefab(lootRayPrefabs[selectedLootRayIndex], pickupInstance.transform);
            }
            
            // Создаем префаб пикапа
            GameObject pickupPrefabAsset = PrefabUtility.SaveAsPrefabAsset(pickupInstance, fullPickupPrefabPath);
            
            // Удаляем временный объект
            DestroyImmediate(pickupInstance);
            
            return pickupPrefabAsset;
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка при создании пикапа: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Ошибка", $"Произошла ошибка при создании пикапа: {e.Message}", "OK");
            return null;
        }
    }

    private void CreateArmorScriptableObject(GameObject prefabAsset, string prefabName)
    {
        try
        {
            // Получаем тип ScriptableObject на основе выбранного типа брони
            string configTypeName = $"RPG.Combat.{selectedArmorType}Config";
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type configType = null;
            
            foreach (var assembly in assemblies)
            {
                configType = assembly.GetTypes()
                    .FirstOrDefault(t => t.FullName == configTypeName);
                
                if (configType != null)
                    break;
            }
            
            if (configType == null)
            {
                EditorUtility.DisplayDialog("Ошибка", $"Не найден тип {configTypeName}", "OK");
                return;
            }

            // Создание ScriptableObject
            var scriptableObject = ScriptableObject.CreateInstance(configType);
            
            // Получаем директорию на основе типа брони
            string basePath = armorPathMapping[selectedArmorType];
            string soPath = $"{basePath}/Resources";
            
            // Создаем директорию если она не существует
            if (!Directory.Exists(soPath))
            {
                Directory.CreateDirectory(soPath);
            }
            
            string assetPath = $"{soPath}/{prefabName}.asset";
            AssetDatabase.CreateAsset(scriptableObject, assetPath);
            
            // Путь к пикапу
            string pickupPath = $"{armorPathMapping[selectedArmorType]}/Pickups/{prefabName}_Pickup.prefab";
            GameObject pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pickupPath);
            
            // Настройка свойств ScriptableObject
            var serializedObject = new SerializedObject(scriptableObject);
            
            // Установка префаба
            var equippedPrefabProperty = serializedObject.FindProperty("_equippedPrefab");
            if (equippedPrefabProperty != null)
            {
                equippedPrefabProperty.objectReferenceValue = prefabAsset.GetComponent(armorTypeMapping[selectedArmorType]);
            }
            
            // Установка категории Item (Armor)
            var categoryProperty = serializedObject.FindProperty("_category");
            if (categoryProperty == null) 
                categoryProperty = serializedObject.FindProperty("category");
                
            if (categoryProperty != null)
            {
                Debug.Log($"Устанавливаем Category на Armor. Типы enum: {string.Join(", ", categoryProperty.enumNames)}");
                categoryProperty.enumValueIndex = GetEnumIndex(categoryProperty, "Armor");
            }
            else
            {
                Debug.LogWarning("Свойство _category/category не найдено!");
            }
            
            // Установка EquipLocation
            var equipLocationProperty = serializedObject.FindProperty("_equipLocation");
            if (equipLocationProperty == null)
                equipLocationProperty = serializedObject.FindProperty("equipLocation");
                
            if (equipLocationProperty != null)
            {
                string locationName = GetEquipLocationFromArmorType(selectedArmorType);
                Debug.Log($"Устанавливаем EquipLocation на {locationName}. Типы enum: {string.Join(", ", equipLocationProperty.enumNames)}");
                
                // Если не удалось найти значение в enum, выведем все доступные значения
                int enumIndex = GetEnumIndex(equipLocationProperty, locationName);
                if (enumIndex == 0 && locationName != "None" && !equipLocationProperty.enumNames.Contains(locationName))
                {
                    Debug.LogWarning($"Не найдено значение '{locationName}' в перечислении. Доступные значения: {string.Join(", ", equipLocationProperty.enumNames)}");
                    
                    // Попытка подобрать подходящее значение
                    string altValue = GetAlternativeEnumValue(equipLocationProperty.enumNames, locationName);
                    if (!string.IsNullOrEmpty(altValue))
                    {
                        Debug.Log($"Используем альтернативное значение для EquipLocation: {altValue}");
                        enumIndex = GetEnumIndex(equipLocationProperty, altValue);
                    }
                }
                
                equipLocationProperty.enumValueIndex = enumIndex;
            }
            else
            {
                Debug.LogWarning("Свойство _equipLocation/equipLocation не найдено!");
            }
            
            // Установка AllowedEquipLocation
            var allowedEquipLocationProperty = serializedObject.FindProperty("_allowedEquipLocation");
            if (allowedEquipLocationProperty == null)
                allowedEquipLocationProperty = serializedObject.FindProperty("allowedEquipLocation");
                
            if (allowedEquipLocationProperty != null)
            {
                string locationName = GetEquipLocationFromArmorType(selectedArmorType);
                Debug.Log($"Устанавливаем AllowedEquipLocation на {locationName}. Типы enum: {string.Join(", ", allowedEquipLocationProperty.enumNames)}");
                
                // Если не удалось найти значение в enum, выведем все доступные значения
                int enumIndex = GetEnumIndex(allowedEquipLocationProperty, locationName);
                if (enumIndex == 0 && locationName != "None" && !allowedEquipLocationProperty.enumNames.Contains(locationName))
                {
                    Debug.LogWarning($"Не найдено значение '{locationName}' в перечислении. Доступные значения: {string.Join(", ", allowedEquipLocationProperty.enumNames)}");
                    
                    // Попытка подобрать подходящее значение
                    string altValue = GetAlternativeEnumValue(allowedEquipLocationProperty.enumNames, locationName);
                    if (!string.IsNullOrEmpty(altValue))
                    {
                        Debug.Log($"Используем альтернативное значение для AllowedEquipLocation: {altValue}");
                        enumIndex = GetEnumIndex(allowedEquipLocationProperty, altValue);
                    }
                }
                
                allowedEquipLocationProperty.enumValueIndex = enumIndex;
            }
            else
            {
                Debug.LogWarning("Свойство _allowedEquipLocation/allowedEquipLocation не найдено!");
            }
            
            // Установка пикапа
            var pickupProperty = serializedObject.FindProperty("_pickup");
            if (pickupProperty == null)
                pickupProperty = serializedObject.FindProperty("pickup");
                
            if (pickupProperty != null)
            {
                if (pickupPrefab != null)
                {
                    Debug.Log($"Устанавливаем Pickup: {pickupPrefab.name}");
                    pickupProperty.objectReferenceValue = pickupPrefab;
                }
                else
                {
                    Debug.LogWarning($"Пикап не найден по пути: {pickupPath}");
                }
            }
            else
            {
                Debug.LogWarning("Свойство _pickup/pickup не найдено!");
            }
            
            // Отладочная информация
            Debug.Log("Свойства ScriptableObject:");
            SerializedProperty prop = serializedObject.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                Debug.Log($"- {prop.propertyPath}: {prop.propertyType}");
            }
            
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = scriptableObject;
            
            Debug.Log($"ScriptableObject создан: {assetPath}");
            EditorUtility.DisplayDialog("Успех", $"Префаб и ScriptableObject созданы успешно!\nПрефаб: {prefabName}\nScriptableObject: {assetPath}", "OK");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка при создании ScriptableObject: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Ошибка", $"Произошла ошибка при создании ScriptableObject: {e.Message}", "OK");
        }
    }
    
    private int GetEnumIndex(SerializedProperty property, string enumValueName)
    {
        for (int i = 0; i < property.enumNames.Length; i++)
        {
            if (property.enumNames[i] == enumValueName)
            {
                return i;
            }
        }
        return 0;
    }
    
    private string GetEquipLocationFromArmorType(string armorType)
    {
        switch (armorType)
        {
            case "BodyArmor": return "Body";
            case "HelmetArmor": return "Helmet";
            case "UpperArmLeftArmor": return "UpperArmL";
            case "UpperArmRightArmor": return "UpperArmR";
            case "LowerArmLeftArmor": return "LowerArmL";
            case "LowerArmRightArmor": return "LowerArmR";
            case "GloveLeftArmor": return "HandL";
            case "GloveRightArmor": return "HandR";
            case "TrousersArmor": return "Legs";
            case "BootLeftArmor": return "FootL";
            case "BootRightArmor": return "FootR";
            default: return "None";
        }
    }

    // Поиск альтернативного значения для enum, если точное не найдено
    private string GetAlternativeEnumValue(string[] enumNames, string searchValue)
    {
        // Таблица соответствий между нашими значениями и возможными значениями в enum
        Dictionary<string, string[]> alternativeValues = new Dictionary<string, string[]>
        {
            { "Body", new[] { "Torso", "Chest", "Armor" } },
            { "Helmet", new[] { "Head", "HeadArmor", "Hat" } },
            { "UpperArmL", new[] { "UpperArmLeft", "LeftUpperArm", "ArmL", "LeftArm" } },
            { "UpperArmR", new[] { "UpperArmRight", "RightUpperArm", "ArmR", "RightArm" } },
            { "LowerArmL", new[] { "LowerArmLeft", "LeftLowerArm", "ForearmL", "LeftForearm" } },
            { "LowerArmR", new[] { "LowerArmRight", "RightLowerArm", "ForearmR", "RightForearm" } },
            { "HandL", new[] { "HandLeft", "LeftHand", "GloveL", "LeftGlove" } },
            { "HandR", new[] { "HandRight", "RightHand", "GloveR", "RightGlove" } },
            { "Legs", new[] { "Trousers", "Pants", "LowerBody" } },
            { "FootL", new[] { "FootLeft", "LeftFoot", "BootL", "LeftBoot" } },
            { "FootR", new[] { "FootRight", "RightFoot", "BootR", "RightBoot" } }
        };
        
        // Попытка найти точное совпадение
        foreach (string enumValue in enumNames)
        {
            if (enumValue.Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                return enumValue;
        }
        
        // Попытка найти альтернативное значение
        if (alternativeValues.ContainsKey(searchValue))
        {
            foreach (string altValue in alternativeValues[searchValue])
            {
                foreach (string enumValue in enumNames)
                {
                    if (enumValue.Equals(altValue, StringComparison.OrdinalIgnoreCase))
                        return enumValue;
                }
            }
        }
        
        // Если совпадений не найдено, попробуем найти что-то похожее
        foreach (string enumValue in enumNames)
        {
            if (enumValue.Contains(searchValue) || searchValue.Contains(enumValue))
                return enumValue;
        }
        
        return null;
    }
} 