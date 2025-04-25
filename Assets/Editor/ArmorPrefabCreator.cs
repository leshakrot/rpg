using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Combat;

public class ArmorPrefabCreator : EditorWindow
{
    private GameObject armorGameObject;
    private string selectedArmorType = "BodyArmor";
    private List<string> armorTypesList = new List<string>();
    private Dictionary<string, string> armorPathMapping = new Dictionary<string, string>();
    private Dictionary<string, Type> armorTypeMapping = new Dictionary<string, Type>();

    [MenuItem("RPG/Armor/Create Armor Prefab")]
    public static void ShowWindow()
    {
        GetWindow<ArmorPrefabCreator>("Armor Prefab Creator");
    }

    private void OnEnable()
    {
        Initialize();
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

    private void OnGUI()
    {
        GUILayout.Label("Создание префаба брони", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);
        
        // Шаг 1: Выбор GameObject
        GUILayout.Label("Шаг 1: Выберите GameObject брони", EditorStyles.boldLabel);
        armorGameObject = (GameObject)EditorGUILayout.ObjectField("GameObject брони:", armorGameObject, typeof(GameObject), true);
        
        EditorGUILayout.Space(10);
        
        // Шаг 2: Выбор типа брони
        GUILayout.Label("Шаг 2: Выберите тип брони", EditorStyles.boldLabel);
        int selectedIndex = armorTypesList.IndexOf(selectedArmorType);
        selectedIndex = EditorGUILayout.Popup("Тип брони:", selectedIndex, armorTypesList.ToArray());
        if (selectedIndex >= 0 && selectedIndex < armorTypesList.Count)
        {
            selectedArmorType = armorTypesList[selectedIndex];
        }
        
        EditorGUILayout.Space(20);
        
        // Кнопка для создания префаба и ScriptableObject
        GUI.enabled = (armorGameObject != null);
        if (GUILayout.Button("Создать префаб и ScriptableObject"))
        {
            CreateArmorPrefab();
        }
        GUI.enabled = true;
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
                
                // Шаг 3: Создание ScriptableObject
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
            if (categoryProperty != null)
            {
                categoryProperty.enumValueIndex = GetEnumIndex(categoryProperty, "Armor");
            }
            
            // Установка EquipLocation
            var equipLocationProperty = serializedObject.FindProperty("_equipLocation");
            if (equipLocationProperty != null)
            {
                string locationName = GetEquipLocationFromArmorType(selectedArmorType);
                equipLocationProperty.enumValueIndex = GetEnumIndex(equipLocationProperty, locationName);
            }
            
            // Установка AllowedEquipLocation
            var allowedEquipLocationProperty = serializedObject.FindProperty("_allowedEquipLocation");
            if (allowedEquipLocationProperty != null)
            {
                string locationName = GetEquipLocationFromArmorType(selectedArmorType);
                allowedEquipLocationProperty.enumValueIndex = GetEnumIndex(allowedEquipLocationProperty, locationName);
            }
            
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            
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
            case "BodyArmor": return "Torso";
            case "HelmetArmor": return "Head";
            case "UpperArmLeftArmor": return "UpperArmLeft";
            case "UpperArmRightArmor": return "UpperArmRight";
            case "LowerArmLeftArmor": return "LowerArmLeft";
            case "LowerArmRightArmor": return "LowerArmRight";
            case "GloveLeftArmor": return "HandLeft";
            case "GloveRightArmor": return "HandRight";
            case "TrousersArmor": return "Legs";
            case "BootLeftArmor": return "FootLeft";
            case "BootRightArmor": return "FootRight";
            default: return "None";
        }
    }
} 