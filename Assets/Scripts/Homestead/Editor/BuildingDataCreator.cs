using UnityEngine;
using UnityEditor;
using RPG.Homestead;
using GameDevTV.Inventories;

namespace RPG.Homestead.Editor
{
    /// <summary>
    /// Editor utility to create example BuildingData assets for Task 10.
    /// Usage: In Unity Editor, go to Tools > Homestead > Create Example Buildings
    /// </summary>
    public class BuildingDataCreator : EditorWindow
    {
        [MenuItem("Tools/Homestead/Create Example Buildings")]
        public static void CreateExampleBuildings()
        {
            if (EditorUtility.DisplayDialog("Create Example Buildings",
                "This will create example BuildingData assets for:\n" +
                "- Basic House (3 levels)\n" +
                "- Workshop (2 levels)\n" +
                "- Storage Shed (1 level)\n\n" +
                "Continue?", "Yes", "Cancel"))
            {
                CreateBasicHouse();
                CreateWorkshop();
                CreateStorageShed();
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("Success", 
                    "Example buildings created successfully!\n\n" +
                    "Check: Assets/Internal Assets/Homestead/Buildings/", "OK");
            }
        }
        
        private static void CreateBasicHouse()
        {
            // Create placeholder prefabs for Basic House (Levels 1-3)
            GameObject houseLevel1 = CreatePlaceholderPrefab("House_Basic_Level1", new Vector3(2f, 2f, 2f), new Color(0.8f, 0.6f, 0.4f));
            GameObject houseLevel2 = CreatePlaceholderPrefab("House_Basic_Level2", new Vector3(3f, 3f, 3f), new Color(0.7f, 0.5f, 0.3f));
            GameObject houseLevel3 = CreatePlaceholderPrefab("House_Basic_Level3", new Vector3(4f, 4f, 4f), new Color(0.6f, 0.4f, 0.2f));
            
            // Load existing inventory items
            InventoryItem woodItem = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Internal Assets/Harvesting/Trees/Resources/WoodItem T1.asset");
            InventoryItem stoneItem = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Internal Assets/Harvesting/Stones/Resources/StoneItem.asset");
            
            if (woodItem == null || stoneItem == null)
            {
                Debug.LogError("Could not find WoodItem T1 or StoneItem. Please ensure they exist.");
                return;
            }
            
            // Create requirement assets for Basic House
            // Level 1 requirements
            var houseL1InventoryReq = CreateInventoryRequirement("House_Basic_L1_Inventory", woodItem, 10);
            var houseL1CurrencyReq = CreateCurrencyRequirement("House_Basic_L1_Currency", 100f);
            
            // Level 2 requirements
            var houseL2InventoryReq = CreateInventoryRequirement("House_Basic_L2_Inventory", stoneItem, 15);
            var houseL2CurrencyReq = CreateCurrencyRequirement("House_Basic_L2_Currency", 250f);
            
            // Level 3 requirements
            var houseL3InventoryReq = CreateInventoryRequirement("House_Basic_L3_Inventory", woodItem, 20);
            var houseL3CurrencyReq = CreateCurrencyRequirement("House_Basic_L3_Currency", 500f);
            
            // Create BuildingData for Basic House
            BuildingData houseBasic = ScriptableObject.CreateInstance<BuildingData>();
            
            // Use reflection to set private fields
            var buildingDataType = typeof(BuildingData);
            buildingDataType.GetField("buildingId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(houseBasic, "house_basic");
            buildingDataType.GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(houseBasic, "Basic House");
            buildingDataType.GetField("description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(houseBasic, "A simple dwelling for rest and storage");
            buildingDataType.GetField("maxLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(houseBasic, 3);
            buildingDataType.GetField("category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(houseBasic, "Residential");
            
            // Set prefabs array
            GameObject[] housePrefabs = new GameObject[] { houseLevel1, houseLevel2, houseLevel3 };
            buildingDataType.GetField("levelPrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(houseBasic, housePrefabs);
            
            // Set requirements array
            BuildingLevelRequirements[] houseRequirements = new BuildingLevelRequirements[]
            {
                CreateLevelRequirements(new BuildingRequirement[] { houseL1InventoryReq, houseL1CurrencyReq }),
                CreateLevelRequirements(new BuildingRequirement[] { houseL2InventoryReq, houseL2CurrencyReq }),
                CreateLevelRequirements(new BuildingRequirement[] { houseL3InventoryReq, houseL3CurrencyReq })
            };
            buildingDataType.GetField("levelRequirements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(houseBasic, houseRequirements);
            
            AssetDatabase.CreateAsset(houseBasic, "Assets/Internal Assets/Homestead/Buildings/House_Basic.asset");
            Debug.Log("Created Basic House building with 3 levels");
        }
        
        private static void CreateWorkshop()
        {
            // Create placeholder prefabs for Workshop (Levels 1-2)
            GameObject workshopLevel1 = CreatePlaceholderPrefab("Workshop_Level1", new Vector3(3f, 2.5f, 3f), new Color(0.5f, 0.4f, 0.3f));
            GameObject workshopLevel2 = CreatePlaceholderPrefab("Workshop_Level2", new Vector3(4f, 3f, 4f), new Color(0.4f, 0.3f, 0.2f));
            
            // Load existing inventory items
            InventoryItem woodItem = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Internal Assets/Harvesting/Trees/Resources/WoodItem T1.asset");
            InventoryItem stoneItem = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Internal Assets/Harvesting/Stones/Resources/StoneItem.asset");
            
            // Load Basic House for prerequisite
            BuildingData houseBasic = AssetDatabase.LoadAssetAtPath<BuildingData>("Assets/Internal Assets/Homestead/Buildings/House_Basic.asset");
            
            if (woodItem == null || stoneItem == null || houseBasic == null)
            {
                Debug.LogError("Could not find required assets for Workshop. Ensure Basic House is created first.");
                return;
            }
            
            // Create requirement assets for Workshop
            // Level 1 requirements
            var workshopL1InventoryReq = CreateInventoryRequirement("Workshop_L1_Inventory", woodItem, 15);
            var workshopL1CurrencyReq = CreateCurrencyRequirement("Workshop_L1_Currency", 200f);
            var workshopL1PrereqReq = CreatePrerequisiteRequirement("Workshop_L1_Prerequisite", houseBasic, 1);
            
            // Level 2 requirements
            var workshopL2InventoryReq = CreateInventoryRequirement("Workshop_L2_Inventory", stoneItem, 20);
            var workshopL2CurrencyReq = CreateCurrencyRequirement("Workshop_L2_Currency", 400f);
            
            // Create BuildingData for Workshop
            BuildingData workshop = ScriptableObject.CreateInstance<BuildingData>();
            
            var buildingDataType = typeof(BuildingData);
            buildingDataType.GetField("buildingId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(workshop, "workshop");
            buildingDataType.GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(workshop, "Workshop");
            buildingDataType.GetField("description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(workshop, "Craft items and process resources");
            buildingDataType.GetField("maxLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(workshop, 2);
            buildingDataType.GetField("category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(workshop, "Production");
            
            // Set prefabs array
            GameObject[] workshopPrefabs = new GameObject[] { workshopLevel1, workshopLevel2 };
            buildingDataType.GetField("levelPrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(workshop, workshopPrefabs);
            
            // Set requirements array
            BuildingLevelRequirements[] workshopRequirements = new BuildingLevelRequirements[]
            {
                CreateLevelRequirements(new BuildingRequirement[] { workshopL1InventoryReq, workshopL1CurrencyReq, workshopL1PrereqReq }),
                CreateLevelRequirements(new BuildingRequirement[] { workshopL2InventoryReq, workshopL2CurrencyReq })
            };
            buildingDataType.GetField("levelRequirements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(workshop, workshopRequirements);
            
            AssetDatabase.CreateAsset(workshop, "Assets/Internal Assets/Homestead/Buildings/Workshop.asset");
            Debug.Log("Created Workshop building with 2 levels");
        }
        
        private static void CreateStorageShed()
        {
            // Create placeholder prefab for Storage Shed (Level 1 only)
            GameObject storageLevel1 = CreatePlaceholderPrefab("Storage_Level1", new Vector3(2f, 2f, 2f), new Color(0.6f, 0.5f, 0.4f));
            
            // Load existing inventory items
            InventoryItem woodItem = AssetDatabase.LoadAssetAtPath<InventoryItem>("Assets/Internal Assets/Harvesting/Trees/Resources/WoodItem T1.asset");
            
            if (woodItem == null)
            {
                Debug.LogError("Could not find WoodItem T1 for Storage Shed.");
                return;
            }
            
            // Create requirement assets for Storage Shed
            var storageL1InventoryReq = CreateInventoryRequirement("Storage_L1_Inventory", woodItem, 8);
            var storageL1CurrencyReq = CreateCurrencyRequirement("Storage_L1_Currency", 50f);
            
            // Create BuildingData for Storage Shed
            BuildingData storage = ScriptableObject.CreateInstance<BuildingData>();
            
            var buildingDataType = typeof(BuildingData);
            buildingDataType.GetField("buildingId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(storage, "storage");
            buildingDataType.GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(storage, "Storage Shed");
            buildingDataType.GetField("description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(storage, "Store extra items and resources");
            buildingDataType.GetField("maxLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(storage, 1);
            buildingDataType.GetField("maxInstances", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(storage, 3); // Test building limit feature
            buildingDataType.GetField("category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(storage, "Storage");
            
            // Set prefabs array
            GameObject[] storagePrefabs = new GameObject[] { storageLevel1 };
            buildingDataType.GetField("levelPrefabs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(storage, storagePrefabs);
            
            // Set requirements array
            BuildingLevelRequirements[] storageRequirements = new BuildingLevelRequirements[]
            {
                CreateLevelRequirements(new BuildingRequirement[] { storageL1InventoryReq, storageL1CurrencyReq })
            };
            buildingDataType.GetField("levelRequirements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(storage, storageRequirements);
            
            AssetDatabase.CreateAsset(storage, "Assets/Internal Assets/Homestead/Buildings/Storage.asset");
            Debug.Log("Created Storage Shed building with 1 level (max 3 instances)");
        }
        
        private static GameObject CreatePlaceholderPrefab(string name, Vector3 size, Color color)
        {
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = name;
            prefab.transform.localScale = size;
            
            // Set color
            var renderer = prefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.sharedMaterial = mat;
                AssetDatabase.CreateAsset(mat, $"Assets/Internal Assets/Homestead/Prefabs/{name}_Material.mat");
            }
            
            // Save as prefab
            string prefabPath = $"Assets/Internal Assets/Homestead/Prefabs/{name}.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            Object.DestroyImmediate(prefab);
            
            return savedPrefab;
        }
        
        private static InventoryRequirement CreateInventoryRequirement(string name, InventoryItem item, int quantity)
        {
            InventoryRequirement req = ScriptableObject.CreateInstance<InventoryRequirement>();
            
            // Set fields using reflection
            var reqType = typeof(InventoryRequirement);
            reqType.GetField("requiredItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, item);
            reqType.GetField("requiredQuantity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, quantity);
            
            AssetDatabase.CreateAsset(req, $"Assets/Internal Assets/Homestead/Requirements/{name}.asset");
            return req;
        }
        
        private static CurrencyRequirement CreateCurrencyRequirement(string name, float amount)
        {
            CurrencyRequirement req = ScriptableObject.CreateInstance<CurrencyRequirement>();
            
            // Set fields using reflection
            var reqType = typeof(CurrencyRequirement);
            reqType.GetField("requiredAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, amount);
            
            AssetDatabase.CreateAsset(req, $"Assets/Internal Assets/Homestead/Requirements/{name}.asset");
            return req;
        }
        
        private static PrerequisiteBuildingRequirement CreatePrerequisiteRequirement(string name, BuildingData prerequisite, int minLevel)
        {
            PrerequisiteBuildingRequirement req = ScriptableObject.CreateInstance<PrerequisiteBuildingRequirement>();
            
            // Set fields using reflection
            var reqType = typeof(PrerequisiteBuildingRequirement);
            reqType.GetField("prerequisiteBuilding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, prerequisite);
            reqType.GetField("minimumLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(req, minLevel);
            
            AssetDatabase.CreateAsset(req, $"Assets/Internal Assets/Homestead/Requirements/{name}.asset");
            return req;
        }
        
        private static BuildingLevelRequirements CreateLevelRequirements(BuildingRequirement[] requirements)
        {
            BuildingLevelRequirements levelReq = new BuildingLevelRequirements();
            var levelReqType = typeof(BuildingLevelRequirements);
            levelReqType.GetField("requirements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(levelReq, requirements);
            return levelReq;
        }
    }
}
