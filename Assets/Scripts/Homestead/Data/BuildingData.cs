using UnityEngine;
using System.Collections.Generic;

namespace RPG.Homestead
{
    /// <summary>
    /// ScriptableObject defining all properties of a building type.
    /// Created in Assets/Internal Assets/Homestead/Buildings/
    /// </summary>
    [CreateAssetMenu(fileName = "New Building", menuName = "Homestead/Building Data", order = 0)]
    public class BuildingData : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private string buildingId; // Unique identifier
        [SerializeField] private string displayName;
        [TextArea(3, 6)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private string category = "General";
        
        [Header("Building Configuration")]
        [SerializeField] private int maxLevel = 3;
        [SerializeField] private int maxInstances = -1; // -1 = unlimited
        
        [Header("Prefabs")]
        [SerializeField] private GameObject[] levelPrefabs; // Index 0 = Level 1, Index 1 = Level 2, etc.
        
        [Header("Requirements Per Level")]
        [SerializeField] private BuildingLevelRequirements[] levelRequirements;
        
        // Static registry for lookup
        private static Dictionary<string, BuildingData> buildingRegistry;
        
        public string BuildingId => buildingId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public string Category => category;
        public int MaxLevel => maxLevel;
        public int MaxInstances => maxInstances;
        
        private void OnEnable()
        {
            RegisterBuilding();
        }
        
        private void RegisterBuilding()
        {
            if (buildingRegistry == null)
            {
                buildingRegistry = new Dictionary<string, BuildingData>();
            }
            
            if (!string.IsNullOrEmpty(buildingId))
            {
                buildingRegistry[buildingId] = this;
            }
        }
        
        /// <summary>
        /// Get a BuildingData by its unique ID.
        /// </summary>
        public static BuildingData GetBuildingById(string id)
        {
            if (buildingRegistry == null)
            {
                // Initialize registry by loading all BuildingData assets
                var allBuildings = Resources.LoadAll<BuildingData>("");
                buildingRegistry = new Dictionary<string, BuildingData>();
                foreach (var building in allBuildings)
                {
                    if (!string.IsNullOrEmpty(building.buildingId))
                    {
                        buildingRegistry[building.buildingId] = building;
                    }
                }
            }
            
            buildingRegistry.TryGetValue(id, out BuildingData data);
            return data;
        }
        
        /// <summary>
        /// Get the prefab for a specific level.
        /// </summary>
        public GameObject GetPrefabForLevel(int level)
        {
            if (level < 1 || level > maxLevel)
            {
                Debug.LogError($"BuildingData {displayName}: Invalid level {level}");
                return null;
            }
            
            int index = level - 1;
            if (index >= levelPrefabs.Length)
            {
                Debug.LogError($"BuildingData {displayName}: No prefab defined for level {level}");
                return null;
            }
            
            return levelPrefabs[index];
        }
        
        /// <summary>
        /// Get the requirements for a specific level.
        /// </summary>
        public BuildingRequirement[] GetRequirementsForLevel(int level)
        {
            if (level < 1 || level > maxLevel)
            {
                Debug.LogError($"BuildingData {displayName}: Invalid level {level}");
                return new BuildingRequirement[0];
            }
            
            int index = level - 1;
            if (index >= levelRequirements.Length)
            {
                return new BuildingRequirement[0];
            }
            
            return levelRequirements[index].requirements;
        }
        
        /// <summary>
        /// Check if this building has a maximum instances limit.
        /// </summary>
        public bool HasMaxInstancesLimit()
        {
            return maxInstances > 0;
        }
        
        private void OnValidate()
        {
            // Validate building ID
            if (string.IsNullOrEmpty(buildingId))
            {
                Debug.LogWarning($"BuildingData {name}: Building ID is not set");
            }
            
            // Validate prefabs array length
            if (levelPrefabs.Length != maxLevel)
            {
                Debug.LogWarning($"BuildingData {displayName}: Prefabs array length ({levelPrefabs.Length}) doesn't match max level ({maxLevel})");
            }
            
            // Validate requirements array length
            if (levelRequirements.Length != maxLevel)
            {
                Debug.LogWarning($"BuildingData {displayName}: Requirements array length ({levelRequirements.Length}) doesn't match max level ({maxLevel})");
            }
            
            // Check for null prefabs
            for (int i = 0; i < levelPrefabs.Length; i++)
            {
                if (levelPrefabs[i] == null)
                {
                    Debug.LogWarning($"BuildingData {displayName}: Prefab for level {i + 1} is null");
                }
            }
        }
    }
    
    /// <summary>
    /// Container for requirements at a specific building level.
    /// </summary>
    [System.Serializable]
    public class BuildingLevelRequirements
    {
        public BuildingRequirement[] requirements;
    }
}
