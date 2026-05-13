using UnityEngine;
using GameDevTV.Saving;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Homestead
{
    /// <summary>
    /// Singleton manager for the player's homestead.
    /// Manages building construction, upgrades, demolition, and state persistence.
    /// </summary>
    public class EstateManager : MonoBehaviour, ISaveable
    {
        [Header("Homestead Configuration")]
        [SerializeField] private string homesteadIdentifier = "main_homestead";
        [SerializeField] private Transform buildingsParent; // Parent transform for instantiated buildings
        
        // Runtime state
        private Dictionary<string, BuildingInstance> buildingsBySlotId = new Dictionary<string, BuildingInstance>();
        private HashSet<string> unlockedBuildings = new HashSet<string>(); // Building IDs that can be rebuilt for free
        private List<BuildingSlot> registeredSlots = new List<BuildingSlot>();
        
        // Singleton
        private static EstateManager instance;
        public static EstateManager Instance => instance;
        
        // Events
        public event System.Action<BuildingInstance> OnBuildingConstructed;
        public event System.Action<BuildingInstance> OnBuildingUpgraded;
        public event System.Action<string> OnBuildingDemolished; // Passes slot ID
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
        
        /// <summary>
        /// Register a building slot with the manager.
        /// Called by BuildingSlot.Awake()
        /// </summary>
        public void RegisterSlot(BuildingSlot slot)
        {
            if (!registeredSlots.Contains(slot))
            {
                registeredSlots.Add(slot);
            }
        }
        
        /// <summary>
        /// Unregister a building slot.
        /// Called by BuildingSlot.OnDestroy()
        /// </summary>
        public void UnregisterSlot(BuildingSlot slot)
        {
            registeredSlots.Remove(slot);
        }
        
        /// <summary>
        /// Attempt to construct a building on a slot.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public bool ConstructBuilding(BuildingSlot slot, BuildingData buildingData)
        {
            if (slot == null || buildingData == null)
            {
                Debug.LogError("EstateManager: Cannot construct building - slot or data is null");
                return false;
            }
            
            string slotId = slot.GetSlotId();
            
            // Check if slot is already occupied
            if (buildingsBySlotId.ContainsKey(slotId))
            {
                Debug.LogWarning($"EstateManager: Slot {slotId} is already occupied");
                return false;
            }
            
            // Check if this is a free reconstruction
            bool isFreeReconstruction = unlockedBuildings.Contains(buildingData.BuildingId);
            
            // Validate requirements
            RequirementValidator validator = new RequirementValidator();
            RequirementValidationResult result = validator.ValidateRequirements(
                buildingData.GetRequirementsForLevel(1), 
                isFreeReconstruction
            );
            
            if (!result.IsValid)
            {
                Debug.LogWarning($"EstateManager: Requirements not met for {buildingData.DisplayName}");
                return false;
            }
            
            // Deduct resources if not free reconstruction
            if (!isFreeReconstruction)
            {
                if (!DeductResources(buildingData.GetRequirementsForLevel(1)))
                {
                    Debug.LogError("EstateManager: Failed to deduct resources");
                    return false;
                }
            }
            
            // Instantiate building prefab
            GameObject prefab = buildingData.GetPrefabForLevel(1);
            if (prefab == null)
            {
                Debug.LogError($"EstateManager: No prefab defined for {buildingData.DisplayName} level 1");
                return false;
            }
            
            GameObject buildingObject = Instantiate(prefab, slot.transform.position, slot.transform.rotation, buildingsParent);
            
            // Create building instance
            BuildingInstance instance = new BuildingInstance
            {
                buildingData = buildingData,
                currentLevel = 1,
                slotId = slotId,
                buildingObject = buildingObject
            };
            
            buildingsBySlotId[slotId] = instance;
            unlockedBuildings.Add(buildingData.BuildingId);
            slot.SetCurrentBuilding(instance);
            
            // Trigger events
            OnBuildingConstructed?.Invoke(instance);
            
            // Autosave
            TriggerAutosave();
            
            Debug.Log($"EstateManager: Successfully constructed {buildingData.DisplayName} on slot {slotId}");
            return true;
        }
        
        /// <summary>
        /// Attempt to upgrade an existing building.
        /// Returns true if successful, false otherwise.
        /// </summary>
        public bool UpgradeBuilding(BuildingSlot slot)
        {
            if (slot == null)
            {
                Debug.LogError("EstateManager: Cannot upgrade - slot is null");
                return false;
            }
            
            string slotId = slot.GetSlotId();
            
            if (!buildingsBySlotId.TryGetValue(slotId, out BuildingInstance instance))
            {
                Debug.LogWarning($"EstateManager: No building on slot {slotId} to upgrade");
                return false;
            }
            
            int nextLevel = instance.currentLevel + 1;
            
            if (nextLevel > instance.buildingData.MaxLevel)
            {
                Debug.LogWarning($"EstateManager: Building {instance.buildingData.DisplayName} is already at max level");
                return false;
            }
            
            // Validate requirements for next level
            RequirementValidator validator = new RequirementValidator();
            RequirementValidationResult result = validator.ValidateRequirements(
                instance.buildingData.GetRequirementsForLevel(nextLevel), 
                false // Upgrades are never free
            );
            
            if (!result.IsValid)
            {
                Debug.LogWarning($"EstateManager: Requirements not met for upgrading to level {nextLevel}");
                return false;
            }
            
            // Deduct resources
            if (!DeductResources(instance.buildingData.GetRequirementsForLevel(nextLevel)))
            {
                Debug.LogError("EstateManager: Failed to deduct resources for upgrade");
                return false;
            }
            
            // Destroy old prefab
            if (instance.buildingObject != null)
            {
                Destroy(instance.buildingObject);
            }
            
            // Instantiate new level prefab
            GameObject prefab = instance.buildingData.GetPrefabForLevel(nextLevel);
            if (prefab == null)
            {
                Debug.LogError($"EstateManager: No prefab defined for level {nextLevel}");
                return false;
            }
            
            GameObject buildingObject = Instantiate(prefab, slot.transform.position, slot.transform.rotation, buildingsParent);
            
            // Update instance
            instance.currentLevel = nextLevel;
            instance.buildingObject = buildingObject;
            
            // Trigger events
            OnBuildingUpgraded?.Invoke(instance);
            
            // Autosave
            TriggerAutosave();
            
            Debug.Log($"EstateManager: Successfully upgraded {instance.buildingData.DisplayName} to level {nextLevel}");
            return true;
        }
        
        /// <summary>
        /// Demolish a building on a slot.
        /// Building remains unlocked for free reconstruction.
        /// No resource refund.
        /// </summary>
        public bool DemolishBuilding(BuildingSlot slot)
        {
            if (slot == null)
            {
                Debug.LogError("EstateManager: Cannot demolish - slot is null");
                return false;
            }
            
            string slotId = slot.GetSlotId();
            
            if (!buildingsBySlotId.TryGetValue(slotId, out BuildingInstance instance))
            {
                Debug.LogWarning($"EstateManager: No building on slot {slotId} to demolish");
                return false;
            }
            
            // Destroy prefab
            if (instance.buildingObject != null)
            {
                Destroy(instance.buildingObject);
            }
            
            // Remove from dictionary but keep in unlocked list
            buildingsBySlotId.Remove(slotId);
            slot.SetCurrentBuilding(null);
            
            // Trigger events
            OnBuildingDemolished?.Invoke(slotId);
            
            // Autosave
            TriggerAutosave();
            
            Debug.Log($"EstateManager: Successfully demolished {instance.buildingData.DisplayName} on slot {slotId}");
            return true;
        }
        
        /// <summary>
        /// Check if a building is unlocked (can be rebuilt for free).
        /// </summary>
        public bool IsBuildingUnlocked(string buildingId)
        {
            return unlockedBuildings.Contains(buildingId);
        }
        
        /// <summary>
        /// Get the building instance on a specific slot.
        /// Returns null if slot is empty.
        /// </summary>
        public BuildingInstance GetBuildingOnSlot(string slotId)
        {
            buildingsBySlotId.TryGetValue(slotId, out BuildingInstance instance);
            return instance;
        }
        
        /// <summary>
        /// Check if a specific building type exists anywhere on the homestead.
        /// </summary>
        public bool HasBuilding(string buildingId)
        {
            return buildingsBySlotId.Values.Any(b => b.buildingData.BuildingId == buildingId);
        }
        
        /// <summary>
        /// Get all instances of a specific building type.
        /// </summary>
        public List<BuildingInstance> GetBuildingsOfType(string buildingId)
        {
            return buildingsBySlotId.Values.Where(b => b.buildingData.BuildingId == buildingId).ToList();
        }
        
        /// <summary>
        /// Count how many buildings of a specific type exist.
        /// </summary>
        public int GetBuildingCount(string buildingId)
        {
            return buildingsBySlotId.Values.Count(b => b.buildingData.BuildingId == buildingId);
        }
        
        /// <summary>
        /// Check if a building meets minimum level requirement.
        /// </summary>
        public bool HasBuildingAtLevel(string buildingId, int minLevel)
        {
            return buildingsBySlotId.Values.Any(b => 
                b.buildingData.BuildingId == buildingId && b.currentLevel >= minLevel
            );
        }
        
        private bool DeductResources(BuildingRequirement[] requirements)
        {
            if (requirements == null) return true;
            
            foreach (var requirement in requirements)
            {
                if (!requirement.DeductResources())
                {
                    return false;
                }
            }
            
            return true;
        }
        
        private void TriggerAutosave()
        {
            var savingSystem = FindObjectOfType<GameDevTV.Saving.SavingSystem>();
            if (savingSystem != null)
            {
                savingSystem.Save("autosave");
            }
        }
        
        // ISaveable implementation
        public object CaptureState()
        {
            var state = new EstateManagerSaveData
            {
                homesteadId = homesteadIdentifier,
                unlockedBuildings = unlockedBuildings.ToList(),
                buildings = new List<BuildingInstanceSaveData>()
            };
            
            foreach (var kvp in buildingsBySlotId)
            {
                state.buildings.Add(new BuildingInstanceSaveData
                {
                    slotId = kvp.Key,
                    buildingId = kvp.Value.buildingData.BuildingId,
                    currentLevel = kvp.Value.currentLevel
                });
            }
            
            return state;
        }
        
        public void RestoreState(object state)
        {
            if (state == null) return;
            
            var saveData = state as EstateManagerSaveData;
            if (saveData == null)
            {
                Debug.LogError("EstateManager: Invalid save data format");
                return;
            }
            
            // Clear current state
            foreach (var instance in buildingsBySlotId.Values)
            {
                if (instance.buildingObject != null)
                {
                    Destroy(instance.buildingObject);
                }
            }
            buildingsBySlotId.Clear();
            
            // Restore unlocked buildings
            unlockedBuildings = new HashSet<string>(saveData.unlockedBuildings);
            
            // Restore buildings
            foreach (var buildingData in saveData.buildings)
            {
                BuildingSlot slot = registeredSlots.FirstOrDefault(s => s.GetSlotId() == buildingData.slotId);
                if (slot == null)
                {
                    Debug.LogWarning($"EstateManager: Slot {buildingData.slotId} not found during restore");
                    continue;
                }
                
                BuildingData data = BuildingData.GetBuildingById(buildingData.buildingId);
                if (data == null)
                {
                    Debug.LogError($"EstateManager: Building data not found for ID {buildingData.buildingId}");
                    continue;
                }
                
                GameObject prefab = data.GetPrefabForLevel(buildingData.currentLevel);
                if (prefab == null)
                {
                    Debug.LogError($"EstateManager: No prefab for {data.DisplayName} level {buildingData.currentLevel}");
                    continue;
                }
                
                GameObject buildingObject = Instantiate(prefab, slot.transform.position, slot.transform.rotation, buildingsParent);
                
                BuildingInstance instance = new BuildingInstance
                {
                    buildingData = data,
                    currentLevel = buildingData.currentLevel,
                    slotId = buildingData.slotId,
                    buildingObject = buildingObject
                };
                
                buildingsBySlotId[buildingData.slotId] = instance;
                slot.SetCurrentBuilding(instance);
            }
            
            Debug.Log($"EstateManager: Restored {buildingsBySlotId.Count} buildings");
        }
    }
    
    [System.Serializable]
    public class EstateManagerSaveData
    {
        public string homesteadId;
        public List<string> unlockedBuildings;
        public List<BuildingInstanceSaveData> buildings;
    }
    
    [System.Serializable]
    public class BuildingInstanceSaveData
    {
        public string slotId;
        public string buildingId;
        public int currentLevel;
    }
}
