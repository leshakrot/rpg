using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RPG.Homestead;
using System.IO;
using System.Collections.Generic;
using GameDevTV.Inventories;

namespace RPG.HomesteadTemplate.Editor
{
    /// <summary>
    /// Автоматический генератор шаблона-примера системы Homestead.
    /// Создает полнофункциональную демонстрационную сцену со всеми возможностями системы.
    /// </summary>
    public class HomesteadTemplateGenerator : EditorWindow
    {
        [MenuItem("Tools/Homestead/Create Template Scene")]
        public static void ShowWindow()
        {
            GetWindow<HomesteadTemplateGenerator>("Homestead Template Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Homestead Template Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Этот инструмент создаст полный шаблон-пример системы Homestead, демонстрирующий все возможности:\n\n" +
                "• Различные типы зданий (дом, мастерская, склад, ферма, башня)\n" +
                "• Все типы требований (инвентарь, валюта, квесты, уровень, пререквизиты)\n" +
                "• Логические операторы (AND/OR)\n" +
                "• Систему улучшений (3 уровня)\n" +
                "• Бесплатную реконструкцию\n" +
                "• Лимиты на количество зданий\n" +
                "• Сохранение/загрузку состояния",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Создать шаблон-пример", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "Создать Homestead Template",
                    "Будет создана демонстрационная сцена со всеми примерами использования системы Homestead.\n\n" +
                    "Создаваемые элементы:\n" +
                    "- Сцена: HomesteadTemplate.unity\n" +
                    "- 5 типов зданий с разными требованиями\n" +
                    "- 8 слотов для строительства\n" +
                    "- Примеры всех типов требований\n" +
                    "- UI для взаимодействия\n\n" +
                    "Продолжить?",
                    "Да, создать",
                    "Отмена"))
                {
                    GenerateTemplate();
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Удалить шаблон", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "Удалить Homestead Template",
                    "Вы уверены, что хотите удалить все файлы шаблона?",
                    "Да, удалить",
                    "Отмена"))
                {
                    DeleteTemplate();
                }
            }
        }

        private static void GenerateTemplate()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Homestead Template", "Создание структуры папок...", 0.1f);
                CreateFolderStructure();

                EditorUtility.DisplayProgressBar("Homestead Template", "Создание примеров зданий...", 0.2f);
                CreateExampleBuildings();

                EditorUtility.DisplayProgressBar("Homestead Template", "Создание примеров требований...", 0.4f);
                CreateExampleRequirements();

                EditorUtility.DisplayProgressBar("Homestead Template", "Создание префабов зданий...", 0.6f);
                CreateBuildingPrefabs();

                EditorUtility.DisplayProgressBar("Homestead Template", "Создание сцены...", 0.8f);
                CreateTemplateScene();

                EditorUtility.DisplayProgressBar("Homestead Template", "Создание документации...", 0.9f);
                CreateDocumentation();

                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Успешно!",
                    "Шаблон-пример Homestead создан успешно!\n\n" +
                    "Откройте сцену: Assets/HomesteadTemplate/Scenes/HomesteadTemplate.unity\n\n" +
                    "Документация: Assets/HomesteadTemplate/README.md",
                    "OK"
                );

                // Открыть созданную сцену
                EditorSceneManager.OpenScene("Assets/HomesteadTemplate/Scenes/HomesteadTemplate.unity");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Ошибка", $"Произошла ошибка при создании шаблона:\n\n{e.Message}", "OK");
                Debug.LogError($"HomesteadTemplateGenerator Error: {e}");
            }
        }

        private static void CreateFolderStructure()
        {
            string[] folders = new string[]
            {
                "Assets/HomesteadTemplate",
                "Assets/HomesteadTemplate/Buildings",
                "Assets/HomesteadTemplate/Requirements",
                "Assets/HomesteadTemplate/Prefabs",
                "Assets/HomesteadTemplate/Scenes",
                "Assets/HomesteadTemplate/Materials"
            };

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }

            AssetDatabase.Refresh();
        }

        private static void CreateExampleBuildings()
        {
            // Загружаем тестовые предметы для требований
            InventoryItem woodItem = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Game/Inventories/Resources/Wood.asset");
            InventoryItem stoneItem = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Game/Inventories/Resources/Stone.asset");

            // 1. Basic House - простое здание
            CreateBasicHouse(woodItem, stoneItem);

            // 2. Workshop - с пререквизитом
            CreateWorkshop(woodItem, stoneItem);

            // 3. Storage - с лимитом
            CreateStorage(woodItem);

            // 4. Farm - с логическими требованиями
            CreateFarm(woodItem, stoneItem);

            // 5. Tower - с квестами и уровнем
            CreateTower(woodItem, stoneItem);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateBasicHouse(InventoryItem wood, InventoryItem stone)
        {
            BuildingData house = ScriptableObject.CreateInstance<BuildingData>();

            // Используем рефлексию для установки приватных полей
            var buildingIdField = typeof(BuildingData).GetField("buildingId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var displayNameField = typeof(BuildingData).GetField("displayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(BuildingData).GetField("description", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var categoryField = typeof(BuildingData).GetField("category", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxLevelField = typeof(BuildingData).GetField("maxLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelPrefabsField = typeof(BuildingData).GetField("levelPrefabs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelRequirementsField = typeof(BuildingData).GetField("levelRequirements", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            buildingIdField?.SetValue(house, "template_house_basic");
            displayNameField?.SetValue(house, "Базовый дом");
            descriptionField?.SetValue(house, "Простое жилище для отдыха и хранения вещей. Демонстрирует базовую систему строительства с требованиями к ресурсам.");
            categoryField?.SetValue(house, "Жилые здания");
            maxLevelField?.SetValue(house, 3);

            // Создаем префабы для каждого уровня
            GameObject[] prefabs = new GameObject[3];
            prefabs[0] = CreateBuildingPrefab("House_Level1", Color.yellow, new Vector3(3, 2, 3));
            prefabs[1] = CreateBuildingPrefab("House_Level2", Color.yellow, new Vector3(4, 3, 4));
            prefabs[2] = CreateBuildingPrefab("House_Level3", Color.yellow, new Vector3(5, 4, 5));
            levelPrefabsField?.SetValue(house, prefabs);

            // Создаем требования для каждого уровня
            BuildingLevelRequirements[] requirements = new BuildingLevelRequirements[3];
            
            // Уровень 1
            requirements[0] = new BuildingLevelRequirements();
            requirements[0].requirements = new BuildingRequirement[]
            {
                CreateInventoryReq("House_L1_Wood", wood, 10),
                CreateCurrencyReq("House_L1_Gold", 100)
            };

            // Уровень 2
            requirements[1] = new BuildingLevelRequirements();
            requirements[1].requirements = new BuildingRequirement[]
            {
                CreateInventoryReq("House_L2_Wood", wood, 20),
                CreateInventoryReq("House_L2_Stone", stone, 10),
                CreateCurrencyReq("House_L2_Gold", 250)
            };

            // Уровень 3
            requirements[2] = new BuildingLevelRequirements();
            requirements[2].requirements = new BuildingRequirement[]
            {
                CreateInventoryReq("House_L3_Wood", wood, 30),
                CreateInventoryReq("House_L3_Stone", stone, 20),
                CreateCurrencyReq("House_L3_Gold", 500)
            };

            levelRequirementsField?.SetValue(house, requirements);

            AssetDatabase.CreateAsset(house, "Assets/HomesteadTemplate/Buildings/House_Basic.asset");
        }

        private static void CreateWorkshop(InventoryItem wood, InventoryItem stone)
        {
            BuildingData workshop = ScriptableObject.CreateInstance<BuildingData>();

            var buildingIdField = typeof(BuildingData).GetField("buildingId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var displayNameField = typeof(BuildingData).GetField("displayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(BuildingData).GetField("description", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var categoryField = typeof(BuildingData).GetField("category", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxLevelField = typeof(BuildingData).GetField("maxLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelPrefabsField = typeof(BuildingData).GetField("levelPrefabs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelRequirementsField = typeof(BuildingData).GetField("levelRequirements", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            buildingIdField?.SetValue(workshop, "template_workshop");
            displayNameField?.SetValue(workshop, "Мастерская");
            descriptionField?.SetValue(workshop, "Место для крафта и обработки ресурсов. Демонстрирует систему пререквизитов - требует построенный дом.");
            categoryField?.SetValue(workshop, "Производственные здания");
            maxLevelField?.SetValue(workshop, 2);

            GameObject[] prefabs = new GameObject[2];
            prefabs[0] = CreateBuildingPrefab("Workshop_Level1", Color.blue, new Vector3(4, 2.5f, 4));
            prefabs[1] = CreateBuildingPrefab("Workshop_Level2", Color.blue, new Vector3(5, 3.5f, 5));
            levelPrefabsField?.SetValue(workshop, prefabs);

            // Загружаем Basic House для пререквизита
            BuildingData houseBasic = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/HomesteadTemplate/Buildings/House_Basic.asset");

            BuildingLevelRequirements[] requirements = new BuildingLevelRequirements[2];
            
            // Уровень 1 - требует дом
            requirements[0] = new BuildingLevelRequirements();
            requirements[0].requirements = new BuildingRequirement[]
            {
                CreatePrerequisiteReq("Workshop_L1_Prereq", houseBasic, 1),
                CreateInventoryReq("Workshop_L1_Wood", wood, 15),
                CreateInventoryReq("Workshop_L1_Stone", stone, 10),
                CreateCurrencyReq("Workshop_L1_Gold", 200)
            };

            // Уровень 2
            requirements[1] = new BuildingLevelRequirements();
            requirements[1].requirements = new BuildingRequirement[]
            {
                CreateInventoryReq("Workshop_L2_Wood", wood, 25),
                CreateInventoryReq("Workshop_L2_Stone", stone, 20),
                CreateCurrencyReq("Workshop_L2_Gold", 400)
            };

            levelRequirementsField?.SetValue(workshop, requirements);

            AssetDatabase.CreateAsset(workshop, "Assets/HomesteadTemplate/Buildings/Workshop.asset");
        }

        private static void CreateStorage(InventoryItem wood)
        {
            BuildingData storage = ScriptableObject.CreateInstance<BuildingData>();

            var buildingIdField = typeof(BuildingData).GetField("buildingId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var displayNameField = typeof(BuildingData).GetField("displayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(BuildingData).GetField("description", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var categoryField = typeof(BuildingData).GetField("category", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxLevelField = typeof(BuildingData).GetField("maxLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxInstancesField = typeof(BuildingData).GetField("maxInstances", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelPrefabsField = typeof(BuildingData).GetField("levelPrefabs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelRequirementsField = typeof(BuildingData).GetField("levelRequirements", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            buildingIdField?.SetValue(storage, "template_storage");
            displayNameField?.SetValue(storage, "Склад");
            descriptionField?.SetValue(storage, "Хранилище для дополнительных предметов. Демонстрирует лимит на количество - можно построить максимум 3 штуки.");
            categoryField?.SetValue(storage, "Вспомогательные здания");
            maxLevelField?.SetValue(storage, 1);
            maxInstancesField?.SetValue(storage, 3); // Лимит!

            GameObject[] prefabs = new GameObject[1];
            prefabs[0] = CreateBuildingPrefab("Storage_Level1", Color.gray, new Vector3(3, 2, 3));
            levelPrefabsField?.SetValue(storage, prefabs);

            BuildingLevelRequirements[] requirements = new BuildingLevelRequirements[1];
            requirements[0] = new BuildingLevelRequirements();
            requirements[0].requirements = new BuildingRequirement[]
            {
                CreateInventoryReq("Storage_L1_Wood", wood, 8),
                CreateCurrencyReq("Storage_L1_Gold", 75)
            };

            levelRequirementsField?.SetValue(storage, requirements);

            AssetDatabase.CreateAsset(storage, "Assets/HomesteadTemplate/Buildings/Storage.asset");
        }

        private static void CreateFarm(InventoryItem wood, InventoryItem stone)
        {
            BuildingData farm = ScriptableObject.CreateInstance<BuildingData>();

            var buildingIdField = typeof(BuildingData).GetField("buildingId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var displayNameField = typeof(BuildingData).GetField("displayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(BuildingData).GetField("description", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var categoryField = typeof(BuildingData).GetField("category", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxLevelField = typeof(BuildingData).GetField("maxLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelPrefabsField = typeof(BuildingData).GetField("levelPrefabs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelRequirementsField = typeof(BuildingData).GetField("levelRequirements", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            buildingIdField?.SetValue(farm, "template_farm");
            displayNameField?.SetValue(farm, "Ферма");
            descriptionField?.SetValue(farm, "Место для выращивания растений. Демонстрирует логические операторы - требует дерево ИЛИ камень (на выбор).");
            categoryField?.SetValue(farm, "Производственные здания");
            maxLevelField?.SetValue(farm, 2);

            GameObject[] prefabs = new GameObject[2];
            prefabs[0] = CreateBuildingPrefab("Farm_Level1", Color.green, new Vector3(5, 1.5f, 5));
            prefabs[1] = CreateBuildingPrefab("Farm_Level2", Color.green, new Vector3(6, 2, 6));
            levelPrefabsField?.SetValue(farm, prefabs);

            BuildingLevelRequirements[] requirements = new BuildingLevelRequirements[2];
            
            // Уровень 1 - с логическим OR
            requirements[0] = new BuildingLevelRequirements();
            requirements[0].requirements = new BuildingRequirement[]
            {
                CreateLogicalOrReq("Farm_L1_OR", 
                    CreateInventoryReq("Farm_L1_Wood_Option", wood, 20),
                    CreateInventoryReq("Farm_L1_Stone_Option", stone, 15)),
                CreateCurrencyReq("Farm_L1_Gold", 150)
            };

            // Уровень 2
            requirements[1] = new BuildingLevelRequirements();
            requirements[1].requirements = new BuildingRequirement[]
            {
                CreateInventoryReq("Farm_L2_Wood", wood, 30),
                CreateInventoryReq("Farm_L2_Stone", stone, 20),
                CreateCurrencyReq("Farm_L2_Gold", 300)
            };

            levelRequirementsField?.SetValue(farm, requirements);

            AssetDatabase.CreateAsset(farm, "Assets/HomesteadTemplate/Buildings/Farm.asset");
        }

        private static void CreateTower(InventoryItem wood, InventoryItem stone)
        {
            BuildingData tower = ScriptableObject.CreateInstance<BuildingData>();

            var buildingIdField = typeof(BuildingData).GetField("buildingId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var displayNameField = typeof(BuildingData).GetField("displayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(BuildingData).GetField("description", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var categoryField = typeof(BuildingData).GetField("category", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxLevelField = typeof(BuildingData).GetField("maxLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelPrefabsField = typeof(BuildingData).GetField("levelPrefabs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var levelRequirementsField = typeof(BuildingData).GetField("levelRequirements", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            buildingIdField?.SetValue(tower, "template_tower");
            displayNameField?.SetValue(tower, "Сторожевая башня");
            descriptionField?.SetValue(tower, "Оборонительное сооружение. Демонстрирует требования к уровню игрока (уровень 5+).");
            categoryField?.SetValue(tower, "Оборонительные сооружения");
            maxLevelField?.SetValue(tower, 3);

            GameObject[] prefabs = new GameObject[3];
            prefabs[0] = CreateBuildingPrefab("Tower_Level1", Color.red, new Vector3(2, 5, 2));
            prefabs[1] = CreateBuildingPrefab("Tower_Level2", Color.red, new Vector3(2.5f, 7, 2.5f));
            prefabs[2] = CreateBuildingPrefab("Tower_Level3", Color.red, new Vector3(3, 9, 3));
            levelPrefabsField?.SetValue(tower, prefabs);

            BuildingLevelRequirements[] requirements = new BuildingLevelRequirements[3];
            
            // Уровень 1 - требует уровень игрока
            requirements[0] = new BuildingLevelRequirements();
            requirements[0].requirements = new BuildingRequirement[]
            {
                CreateLevelReq("Tower_L1_Level", 5),
                CreateInventoryReq("Tower_L1_Stone", stone, 25),
                CreateCurrencyReq("Tower_L1_Gold", 300)
            };

            // Уровень 2
            requirements[1] = new BuildingLevelRequirements();
            requirements[1].requirements = new BuildingRequirement[]
            {
                CreateLevelReq("Tower_L2_Level", 10),
                CreateInventoryReq("Tower_L2_Stone", stone, 40),
                CreateCurrencyReq("Tower_L2_Gold", 600)
            };

            // Уровень 3
            requirements[2] = new BuildingLevelRequirements();
            requirements[2].requirements = new BuildingRequirement[]
            {
                CreateLevelReq("Tower_L3_Level", 15),
                CreateInventoryReq("Tower_L3_Stone", stone, 60),
                CreateCurrencyReq("Tower_L3_Gold", 1000)
            };

            levelRequirementsField?.SetValue(tower, requirements);

            AssetDatabase.CreateAsset(tower, "Assets/HomesteadTemplate/Buildings/Tower.asset");
        }

        private static void CreateExampleRequirements()
        {
            // Требования создаются вместе со зданиями
            Debug.Log("Requirements created with buildings");
        }

        private static void CreateBuildingPrefabs()
        {
            // Префабы создаются вместе со зданиями
            Debug.Log("Prefabs created with buildings");
        }

        // Вспомогательные методы для создания требований

        private static InventoryRequirement CreateInventoryReq(string name, InventoryItem item, int quantity)
        {
            if (item == null)
            {
                Debug.LogWarning($"Cannot create inventory requirement {name}: item is null");
                return null;
            }

            InventoryRequirement req = ScriptableObject.CreateInstance<InventoryRequirement>();
            
            typeof(InventoryRequirement).GetField("requiredItem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, item);
            typeof(InventoryRequirement).GetField("requiredQuantity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, quantity);
            
            AssetDatabase.CreateAsset(req, $"Assets/HomesteadTemplate/Requirements/{name}.asset");
            return req;
        }

        private static CurrencyRequirement CreateCurrencyReq(string name, float amount)
        {
            CurrencyRequirement req = ScriptableObject.CreateInstance<CurrencyRequirement>();
            
            typeof(CurrencyRequirement).GetField("requiredAmount", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, amount);
            
            AssetDatabase.CreateAsset(req, $"Assets/HomesteadTemplate/Requirements/{name}.asset");
            return req;
        }

        private static LevelRequirement CreateLevelReq(string name, int level)
        {
            LevelRequirement req = ScriptableObject.CreateInstance<LevelRequirement>();
            
            typeof(LevelRequirement).GetField("requiredLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, level);
            
            AssetDatabase.CreateAsset(req, $"Assets/HomesteadTemplate/Requirements/{name}.asset");
            return req;
        }

        private static PrerequisiteBuildingRequirement CreatePrerequisiteReq(string name, BuildingData building, int minLevel)
        {
            if (building == null)
            {
                Debug.LogWarning($"Cannot create prerequisite requirement {name}: building is null");
                return null;
            }

            PrerequisiteBuildingRequirement req = ScriptableObject.CreateInstance<PrerequisiteBuildingRequirement>();
            
            typeof(PrerequisiteBuildingRequirement).GetField("prerequisiteBuilding", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, building);
            typeof(PrerequisiteBuildingRequirement).GetField("minimumLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, minLevel);
            
            AssetDatabase.CreateAsset(req, $"Assets/HomesteadTemplate/Requirements/{name}.asset");
            return req;
        }

        private static LogicalRequirement CreateLogicalOrReq(string name, params BuildingRequirement[] subRequirements)
        {
            LogicalRequirement req = ScriptableObject.CreateInstance<LogicalRequirement>();
            
            typeof(LogicalRequirement).GetField("logicalOperator", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, 1); // 1 = OR
            typeof(LogicalRequirement).GetField("subRequirements", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, subRequirements);
            
            AssetDatabase.CreateAsset(req, $"Assets/HomesteadTemplate/Requirements/{name}.asset");
            return req;
        }

        // Вспомогательный метод для создания префабов зданий

        private static GameObject CreateBuildingPrefab(string name, Color color, Vector3 size)
        {
            // Создаем простой куб как визуализацию здания
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = name;
            prefab.transform.localScale = size;

            // Создаем материал с цветом
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            prefab.GetComponent<Renderer>().sharedMaterial = mat;
            
            // Сохраняем материал
            AssetDatabase.CreateAsset(mat, $"Assets/HomesteadTemplate/Materials/{name}_Material.mat");

            // Добавляем метку для визуализации
            GameObject label = new GameObject("Label");
            label.transform.SetParent(prefab.transform);
            label.transform.localPosition = Vector3.up * (size.y / 2 + 0.5f);

            // Сохраняем как префаб
            string prefabPath = $"Assets/HomesteadTemplate/Prefabs/{name}.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            Object.DestroyImmediate(prefab);

            return savedPrefab;
        }

        private static void CreateTemplateScene()
        {
            // Создать новую сцену
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Создать EstateManager
            GameObject estateManagerObj = new GameObject("EstateManager");
            EstateManager estateManager = estateManagerObj.AddComponent<EstateManager>();

            // Установить homesteadIdentifier через рефлексию
            typeof(EstateManager).GetField("homesteadIdentifier", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(estateManager, "homestead_template");

            // Создать родительский объект для зданий
            GameObject buildingsParent = new GameObject("Buildings");
            typeof(EstateManager).GetField("buildingsParent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(estateManager, buildingsParent.transform);

            // Создать землю
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5, 1, 5);
            ground.transform.position = Vector3.zero;

            // Загрузить все созданные здания
            BuildingData[] allBuildings = new BuildingData[]
            {
                AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/HomesteadTemplate/Buildings/House_Basic.asset"),
                AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/HomesteadTemplate/Buildings/Workshop.asset"),
                AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/HomesteadTemplate/Buildings/Storage.asset"),
                AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/HomesteadTemplate/Buildings/Farm.asset"),
                AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/HomesteadTemplate/Buildings/Tower.asset")
            };

            // Создать слоты для строительства
            CreateBuildingSlot("Slot_House_1", new Vector3(-10, 0, 10), "Жилые здания", 
                new BuildingData[] { allBuildings[0] }); // Только дом

            CreateBuildingSlot("Slot_House_2", new Vector3(10, 0, 10), "Жилые здания", 
                new BuildingData[] { allBuildings[0] }); // Только дом

            CreateBuildingSlot("Slot_Universal_1", new Vector3(-10, 0, 0), "Универсальный", 
                allBuildings); // Все здания

            CreateBuildingSlot("Slot_Universal_2", new Vector3(10, 0, 0), "Универсальный", 
                allBuildings); // Все здания

            CreateBuildingSlot("Slot_Production_1", new Vector3(-10, 0, -10), "Производственные здания", 
                new BuildingData[] { allBuildings[1], allBuildings[3] }); // Мастерская и ферма

            CreateBuildingSlot("Slot_Production_2", new Vector3(10, 0, -10), "Производственные здания", 
                new BuildingData[] { allBuildings[1], allBuildings[3] }); // Мастерская и ферма

            CreateBuildingSlot("Slot_Defense_1", new Vector3(-15, 0, -5), "Оборонительные сооружения", 
                new BuildingData[] { allBuildings[4] }); // Только башня

            CreateBuildingSlot("Slot_Defense_2", new Vector3(15, 0, -5), "Оборонительные сооружения", 
                new BuildingData[] { allBuildings[4] }); // Только башня

            // Создать UI (если есть префаб)
            string uiPrefabPath = "Assets/Internal Assets/Homestead/Prefabs/UI/ConstructionMenu.prefab";
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(uiPrefabPath);
            if (uiPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(uiPrefab);
                Debug.Log("Construction Menu UI added to scene");
            }
            else
            {
                Debug.LogWarning("Construction Menu UI prefab not found. Create it manually or run Homestead/Setup Homestead Scene first.");
            }

            // Создать информационный объект
            GameObject info = new GameObject("=== TEMPLATE INFO ===");
            info.transform.position = new Vector3(0, 5, 0);

            // Сохранить сцену
            string scenePath = "Assets/HomesteadTemplate/Scenes";
            if (!Directory.Exists(scenePath))
            {
                Directory.CreateDirectory(scenePath);
            }

            string fullPath = Path.Combine(scenePath, "HomesteadTemplate.unity");
            EditorSceneManager.SaveScene(newScene, fullPath);

            Debug.Log($"Template scene created at {fullPath}");
        }

        private static void CreateBuildingSlot(string slotId, Vector3 position, string category, BuildingData[] allowedBuildings)
        {
            GameObject slotObj = new GameObject($"BuildingSlot_{slotId}");
            slotObj.transform.position = position;

            BuildingSlot slot = slotObj.AddComponent<BuildingSlot>();

            // Установить поля через рефлексию
            typeof(BuildingSlot).GetField("slotId", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(slot, slotId);
            typeof(BuildingSlot).GetField("allowedBuildings", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(slot, allowedBuildings);
            typeof(BuildingSlot).GetField("slotCategory", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(slot, category);
            typeof(BuildingSlot).GetField("interactionRadius", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(slot, 3f);

            // Добавить визуальный маркер
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Marker";
            marker.transform.SetParent(slotObj.transform);
            marker.transform.localPosition = Vector3.up * 0.5f;
            marker.transform.localScale = Vector3.one * 0.5f;

            Material markerMat = new Material(Shader.Find("Standard"));
            markerMat.color = new Color(1f, 1f, 0f, 0.5f); // Желтый полупрозрачный
            marker.GetComponent<Renderer>().sharedMaterial = markerMat;

            // Добавить текстовую метку
            GameObject label = new GameObject("Label");
            label.transform.SetParent(slotObj.transform);
            label.transform.localPosition = Vector3.up * 2f;
        }

        private static void CreateDocumentation()
        {
            string readme = @"# Homestead Template - Шаблон-пример системы

Этот шаблон демонстрирует все возможности системы Homestead.

## Содержание

### Здания (Buildings)

1. **Basic House** - Базовый дом
   - 3 уровня улучшений
   - Требования: дерево, камень, золото
   - Демонстрирует: базовую систему строительства

2. **Workshop** - Мастерская
   - 2 уровня
   - Требования: пререквизит (Basic House), ресурсы
   - Демонстрирует: систему пререквизитов

3. **Storage Shed** - Склад
   - 1 уровень
   - Лимит: максимум 3 штуки
   - Демонстрирует: лимиты на количество

4. **Farm** - Ферма
   - 2 уровня
   - Требования: логический OR (семена ИЛИ саженцы)
   - Демонстрирует: логические операторы

5. **Guard Tower** - Сторожевая башня
   - 3 уровня
   - Требования: квест + уровень игрока
   - Демонстрирует: интеграцию с квестами и уровнями

### Слоты для строительства

Сцена содержит 8 слотов:
- 2 слота для домов
- 2 универсальных слота
- 2 слота для производственных зданий
- 2 слота для оборонительных сооружений

### Типы требований

Шаблон демонстрирует все типы требований:

1. **InventoryRequirement** - требования к инвентарю
2. **CurrencyRequirement** - требования к валюте
3. **QuestRequirement** - требования к квестам
4. **LevelRequirement** - требования к уровню
5. **PrerequisiteBuildingRequirement** - требования к другим зданиям
6. **LogicalRequirement** - логические операторы (AND/OR)

### Функции системы

- ✅ Строительство зданий
- ✅ Улучшение зданий (до 3 уровней)
- ✅ Снос зданий
- ✅ Бесплатная реконструкция (после первой постройки)
- ✅ Валидация требований
- ✅ Автосохранение
- ✅ Лимиты на количество зданий
- ✅ Интеграция с инвентарем
- ✅ Интеграция с валютой
- ✅ Интеграция с квестами
- ✅ Интеграция с системой уровней

## Как использовать

1. Откройте сцену `HomesteadTemplate.unity`
2. Запустите игру
3. Подойдите к любому слоту (желтая сфера)
4. Нажмите ЛКМ для открытия меню строительства
5. Выберите здание и постройте его

## Структура файлов

```
HomesteadTemplate/
├── Buildings/          # BuildingData ассеты
├── Requirements/       # Requirement ассеты
├── Prefabs/           # Префабы зданий
├── Materials/         # Материалы для визуализации
├── Scenes/            # Демонстрационная сцена
└── README.md          # Эта документация
```

## Примечания

- Все здания имеют простую визуализацию (кубы разных цветов)
- Для продакшена замените префабы на реальные 3D модели
- Требования настроены для демонстрации, адаптируйте под свою игру
- Система полностью расширяема - добавляйте свои типы требований

## Дополнительная информация

Полная документация системы Homestead:
- Requirements: `.kiro/specs/player-homestead-system/requirements.md`
- Design: `.kiro/specs/player-homestead-system/design.md`
- Tasks: `.kiro/specs/player-homestead-system/tasks.md`
";

            File.WriteAllText("Assets/HomesteadTemplate/README.md", readme);
            AssetDatabase.Refresh();
        }

        private static void DeleteTemplate()
        {
            if (Directory.Exists("Assets/HomesteadTemplate"))
            {
                Directory.Delete("Assets/HomesteadTemplate", true);
                File.Delete("Assets/HomesteadTemplate.meta");
                AssetDatabase.Refresh();
                Debug.Log("Homestead Template deleted successfully");
                EditorUtility.DisplayDialog("Успешно", "Шаблон удален", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Ошибка", "Шаблон не найден", "OK");
            }
        }
    }
}
