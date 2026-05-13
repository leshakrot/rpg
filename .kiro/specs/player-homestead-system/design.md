# Design Document: Player Homestead System

## Overview

The Player Homestead System is a meta-progression feature that allows players to construct, upgrade, and manage buildings on their personal estate. The system integrates seamlessly with existing RPG systems (Inventory, Purse, QuestList, Experience, SavingSystem) without modifying them, providing a flexible and extensible architecture for long-term player engagement.

### Key Design Principles

1. **Non-Invasive Integration**: The system uses existing game systems through their public APIs without modification
2. **Data-Driven Configuration**: All building properties, requirements, and behaviors are defined in ScriptableObjects
3. **Extensible Architecture**: Abstract base classes and interfaces allow easy addition of new requirement types and building functionalities
4. **Robust State Management**: Full integration with the existing JSON-based save system, supporting WebGL and Yandex platforms
5. **Inspector-Friendly**: Maximum configurability through Unity Inspector for designers

### Core Functionality

- **Building Construction**: Players can construct buildings on predefined slots using resources, currency, and meeting various requirements
- **Building Upgrades**: Existing buildings can be upgraded to higher levels with increasing benefits
- **Building Demolition**: Buildings can be demolished and rebuilt for free (unlocked buildings)
- **Requirement Validation**: Flexible system for checking inventory, currency, quests, level, and prerequisite buildings
- **Save/Load Integration**: Automatic persistence of homestead state through existing SavingSystem
- **UI Management**: Intuitive construction menu for browsing and managing buildings

## Architecture

### System Architecture Diagram

```mermaid
graph TB
    subgraph "Player Interaction"
        Player[Player]
        TeleportItem[Teleport Item]
    end
    
    subgraph "Homestead Scene"
        BuildingSlot[Building Slot<br/>MonoBehaviour]
        ConstructionMenu[Construction Menu UI]
    end
    
    subgraph "Core Management"
        EstateManager[Estate Manager<br/>Singleton + ISaveable]
        RequirementValidator[Requirement Validator]
    end
    
    subgraph "Data Layer"
        BuildingData[Building Data<br/>ScriptableObject]
        BuildingRequirement[Building Requirement<br/>Abstract Base]
        InventoryReq[Inventory Requirement]
        CurrencyReq[Currency Requirement]
        QuestReq[Quest Requirement]
        LevelReq[Level Requirement]
        PrereqReq[Prerequisite Requirement]
        LogicalReq[Logical Requirement<br/>AND/OR]
    end
    
    subgraph "Existing Systems"
        Inventory[Inventory System]
        Purse[Purse System]
        QuestList[Quest System]
        BaseStats[Experience/Stats]
        SavingSystem[Saving System]
    end
    
    subgraph "Runtime Instances"
        BuildingInstance[Building Instance<br/>Runtime State]
        BuildingPrefab[Building Prefab<br/>GameObject]
    end
    
    Player -->|Uses| TeleportItem
    TeleportItem -->|Loads Scene| EstateManager
    Player -->|Interacts| BuildingSlot
    BuildingSlot -->|Opens| ConstructionMenu
    ConstructionMenu -->|Requests Action| EstateManager
    
    EstateManager -->|Validates| RequirementValidator
    RequirementValidator -->|Checks| BuildingRequirement
    
    BuildingRequirement <|-- InventoryReq
    BuildingRequirement <|-- CurrencyReq
    BuildingRequirement <|-- QuestReq
    BuildingRequirement <|-- LevelReq
    BuildingRequirement <|-- PrereqReq
    BuildingRequirement <|-- LogicalReq
    
    InventoryReq -->|Queries| Inventory
    CurrencyReq -->|Queries| Purse
    QuestReq -->|Queries| QuestList
    LevelReq -->|Queries| BaseStats
    PrereqReq -->|Queries| EstateManager
    
    EstateManager -->|Deducts Resources| Inventory
    EstateManager -->|Deducts Currency| Purse
    EstateManager -->|Instantiates| BuildingPrefab
    EstateManager -->|Manages| BuildingInstance
    EstateManager -->|Saves/Loads| SavingSystem
    
    BuildingSlot -->|References| BuildingData
    BuildingData -->|Defines| BuildingRequirement
    BuildingData -->|References| BuildingPrefab
    BuildingInstance -->|References| BuildingData
    BuildingInstance -->|Owns| BuildingPrefab
```

### Component Responsibilities

#### Estate Manager
- **Singleton** managing all homestead state
- Tracks all building slots and their current buildings
- Maintains list of unlocked buildings (can rebuild for free)
- Executes construction, upgrade, and demolition operations
- Implements ISaveable for state persistence
- Triggers autosave after state changes

#### Building Slot
- **MonoBehaviour** placed manually in homestead scene
- Defines allowed buildings for this specific slot
- Displays interaction prompt when player is nearby
- Opens Construction Menu on interaction
- Stores reference to current building instance (if any)

#### Building Data (ScriptableObject)
- Defines all properties of a building type
- Unique identifier, display name, description, icon
- References building prefab for each level
- Defines requirements for each upgrade level
- Configurable maximum level
- Optional category for UI organization
- Optional maximum instances limit

#### Building Requirement (Abstract)
- Base class for all requirement types
- Abstract `Validate()` method returns bool
- Abstract `GetDescription()` method returns string
- Abstract `GetMissingInfo()` method returns detailed failure info
- Extensible: new types can be added without modifying existing code

#### Requirement Validator
- Evaluates all requirements for a building/level
- Aggregates validation results
- Provides detailed feedback on missing requirements
- Handles logical operators (AND/OR)

#### Construction Menu UI
- Displays available buildings for selected slot
- Shows building details (name, description, icon, level)
- Displays all requirements with visual status indicators
- Provides Build, Upgrade, Demolish buttons based on state
- Supports keyboard and controller navigation

#### Building Instance (Runtime)
- Runtime representation of a constructed building
- Stores building data reference, current level, slot reference
- Maintains reference to instantiated prefab GameObject
- Serializable for save/load

## Components and Interfaces

### Core Classes

#### EstateManager.cs

```csharp
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
```


#### BuildingSlot.cs

```csharp
using UnityEngine;
using RPG.Control;

namespace RPG.Homestead
{
    /// <summary>
    /// Represents a location where buildings can be constructed.
    /// Placed manually in the homestead scene by designers.
    /// </summary>
    public class BuildingSlot : MonoBehaviour, IRaycastable
    {
        [Header("Slot Configuration")]
        [SerializeField] private string slotId; // Unique identifier for this slot
        [SerializeField] private BuildingData[] allowedBuildings; // Buildings that can be constructed here
        [SerializeField] private string slotCategory = "General"; // For UI organization
        
        [Header("Interaction")]
        [SerializeField] private float interactionRadius = 3f;
        [SerializeField] private string interactionPrompt = "Open Construction Menu";
        
        private BuildingInstance currentBuilding;
        
        private void Awake()
        {
            // Generate unique ID if not set
            if (string.IsNullOrEmpty(slotId))
            {
                slotId = System.Guid.NewGuid().ToString();
            }
            
            // Register with EstateManager
            if (EstateManager.Instance != null)
            {
                EstateManager.Instance.RegisterSlot(this);
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from EstateManager
            if (EstateManager.Instance != null)
            {
                EstateManager.Instance.UnregisterSlot(this);
            }
        }
        
        public string GetSlotId() => slotId;
        public string GetSlotCategory() => slotCategory;
        public BuildingData[] GetAllowedBuildings() => allowedBuildings;
        public BuildingInstance GetCurrentBuilding() => currentBuilding;
        public bool IsOccupied() => currentBuilding != null;
        
        public void SetCurrentBuilding(BuildingInstance building)
        {
            currentBuilding = building;
        }
        
        // IRaycastable implementation for player interaction
        public CursorType GetCursorType()
        {
            return CursorType.Interact;
        }
        
        public bool HandleRaycast(PlayerController callingController)
        {
            if (Vector3.Distance(callingController.transform.position, transform.position) > interactionRadius)
            {
                return false;
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                OpenConstructionMenu(callingController);
            }
            
            return true;
        }
        
        private void OpenConstructionMenu(PlayerController player)
        {
            var menu = FindObjectOfType<ConstructionMenuUI>();
            if (menu != null)
            {
                menu.Open(this, player);
            }
            else
            {
                Debug.LogError("BuildingSlot: ConstructionMenuUI not found in scene");
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Visualize interaction radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
            
            // Draw slot ID label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Slot: {slotId}\nCategory: {slotCategory}");
            #endif
        }
        
        private void OnValidate()
        {
            // Validate allowed buildings
            if (allowedBuildings != null)
            {
                foreach (var building in allowedBuildings)
                {
                    if (building == null)
                    {
                        Debug.LogWarning($"BuildingSlot {slotId}: Null building reference in allowed buildings list");
                    }
                }
            }
        }
    }
}
```

#### BuildingInstance.cs

```csharp
using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Runtime representation of a constructed building.
    /// Stores current state and references.
    /// </summary>
    [System.Serializable]
    public class BuildingInstance
    {
        public BuildingData buildingData;
        public int currentLevel;
        public string slotId;
        public GameObject buildingObject; // The instantiated prefab (not serialized)
        
        public bool IsMaxLevel()
        {
            return currentLevel >= buildingData.MaxLevel;
        }
        
        public bool CanUpgrade()
        {
            return !IsMaxLevel();
        }
        
        public string GetDisplayName()
        {
            return $"{buildingData.DisplayName} (Level {currentLevel})";
        }
    }
}
```

## Data Models

### BuildingData.cs (ScriptableObject)

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    
    [System.Serializable]
    public class BuildingLevelRequirements
    {
        public BuildingRequirement[] requirements;
    }
}
```

### Building Requirement System

#### BuildingRequirement.cs (Abstract Base)

```csharp
using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Abstract base class for all building requirements.
    /// Extensible: new requirement types can be added by inheriting from this class.
    /// </summary>
    public abstract class BuildingRequirement : ScriptableObject
    {
        /// <summary>
        /// Validate if the requirement is met.
        /// </summary>
        /// <param name="skipResourceChecks">If true, skip inventory/currency checks (for free reconstruction)</param>
        /// <returns>True if requirement is met, false otherwise</returns>
        public abstract bool Validate(bool skipResourceChecks = false);
        
        /// <summary>
        /// Get a human-readable description of the requirement.
        /// </summary>
        public abstract string GetDescription();
        
        /// <summary>
        /// Get detailed information about why the requirement failed.
        /// </summary>
        public abstract string GetMissingInfo();
        
        /// <summary>
        /// Deduct resources if this requirement consumes resources.
        /// Called after validation passes.
        /// </summary>
        /// <returns>True if deduction successful, false otherwise</returns>
        public virtual bool DeductResources()
        {
            return true; // Default: no resources to deduct
        }
    }
}
```

#### InventoryRequirement.cs

```csharp
using UnityEngine;
using GameDevTV.Inventories;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires specific items in player inventory.
    /// </summary>
    [CreateAssetMenu(fileName = "New Inventory Requirement", menuName = "Homestead/Requirements/Inventory", order = 0)]
    public class InventoryRequirement : BuildingRequirement
    {
        [SerializeField] private InventoryItem requiredItem;
        [SerializeField] private int requiredQuantity = 1;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (skipResourceChecks) return true;
            
            if (requiredItem == null)
            {
                Debug.LogError("InventoryRequirement: Required item is null");
                return false;
            }
            
            var inventory = Inventory.GetPlayerInventory();
            if (inventory == null)
            {
                Debug.LogError("InventoryRequirement: Player inventory not found");
                return false;
            }
            
            int currentCount = inventory.GetItemCount(requiredItem);
            return currentCount >= requiredQuantity;
        }
        
        public override string GetDescription()
        {
            if (requiredItem == null) return "Invalid item requirement";
            return $"{requiredItem.GetDisplayName()} x{requiredQuantity}";
        }
        
        public override string GetMissingInfo()
        {
            if (requiredItem == null) return "Invalid item";
            
            var inventory = Inventory.GetPlayerInventory();
            if (inventory == null) return "Inventory not found";
            
            int currentCount = inventory.GetItemCount(requiredItem);
            int missing = Mathf.Max(0, requiredQuantity - currentCount);
            
            return $"Need {missing} more {requiredItem.GetDisplayName()} (have {currentCount}/{requiredQuantity})";
        }
        
        public override bool DeductResources()
        {
            var inventory = Inventory.GetPlayerInventory();
            if (inventory == null) return false;
            
            // Find and remove items
            int remainingToRemove = requiredQuantity;
            for (int i = 0; i < inventory.GetSize() && remainingToRemove > 0; i++)
            {
                if (object.ReferenceEquals(inventory.GetItemInSlot(i), requiredItem))
                {
                    int inSlot = inventory.GetNumberInSlot(i);
                    int toRemove = Mathf.Min(inSlot, remainingToRemove);
                    inventory.RemoveFromSlot(i, toRemove);
                    remainingToRemove -= toRemove;
                }
            }
            
            return remainingToRemove == 0;
        }
    }
}
```

#### CurrencyRequirement.cs

```csharp
using UnityEngine;
using RPG.Inventories;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires specific amount of currency.
    /// </summary>
    [CreateAssetMenu(fileName = "New Currency Requirement", menuName = "Homestead/Requirements/Currency", order = 1)]
    public class CurrencyRequirement : BuildingRequirement
    {
        [SerializeField] private float requiredAmount;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (skipResourceChecks) return true;
            
            var purse = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Purse>();
            if (purse == null)
            {
                Debug.LogError("CurrencyRequirement: Player purse not found");
                return false;
            }
            
            return purse.GetBalance() >= requiredAmount;
        }
        
        public override string GetDescription()
        {
            return $"{requiredAmount} Gold";
        }
        
        public override string GetMissingInfo()
        {
            var purse = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Purse>();
            if (purse == null) return "Purse not found";
            
            float currentBalance = purse.GetBalance();
            float missing = Mathf.Max(0, requiredAmount - currentBalance);
            
            return $"Need {missing} more gold (have {currentBalance}/{requiredAmount})";
        }
        
        public override bool DeductResources()
        {
            var purse = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Purse>();
            if (purse == null) return false;
            
            purse.UpdateBalance(-requiredAmount);
            return true;
        }
    }
}
```

#### QuestRequirement.cs

```csharp
using UnityEngine;
using RPG.Quests;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires specific quest to be completed.
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest Requirement", menuName = "Homestead/Requirements/Quest", order = 2)]
    public class QuestRequirement : BuildingRequirement
    {
        [SerializeField] private Quest requiredQuest;
        [SerializeField] private string specificObjective; // Optional: require specific objective completion
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (requiredQuest == null)
            {
                Debug.LogError("QuestRequirement: Required quest is null");
                return false;
            }
            
            var questList = GameObject.FindGameObjectWithTag("Player")?.GetComponent<QuestList>();
            if (questList == null)
            {
                Debug.LogError("QuestRequirement: Player quest list not found");
                return false;
            }
            
            var status = questList.GetQuestStatus(requiredQuest);
            if (status == null) return false;
            
            // Check specific objective if specified
            if (!string.IsNullOrEmpty(specificObjective))
            {
                return status.IsObjectiveComplete(specificObjective);
            }
            
            // Otherwise check if entire quest is complete
            return status.IsComplete();
        }
        
        public override string GetDescription()
        {
            if (requiredQuest == null) return "Invalid quest requirement";
            
            if (!string.IsNullOrEmpty(specificObjective))
            {
                return $"Complete: {requiredQuest.GetTitle()} - {specificObjective}";
            }
            
            return $"Complete quest: {requiredQuest.GetTitle()}";
        }
        
        public override string GetMissingInfo()
        {
            if (requiredQuest == null) return "Invalid quest";
            
            var questList = GameObject.FindGameObjectWithTag("Player")?.GetComponent<QuestList>();
            if (questList == null) return "Quest list not found";
            
            var status = questList.GetQuestStatus(requiredQuest);
            if (status == null) return $"Quest '{requiredQuest.GetTitle()}' not started";
            
            if (!string.IsNullOrEmpty(specificObjective))
            {
                if (status.IsObjectiveComplete(specificObjective))
                {
                    return "Objective complete";
                }
                return $"Objective '{specificObjective}' not complete";
            }
            
            if (status.IsComplete())
            {
                return "Quest complete";
            }
            
            return $"Quest '{requiredQuest.GetTitle()}' not complete";
        }
    }
}
```


#### LevelRequirement.cs

```csharp
using UnityEngine;
using RPG.Stats;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires player to be at specific level.
    /// </summary>
    [CreateAssetMenu(fileName = "New Level Requirement", menuName = "Homestead/Requirements/Level", order = 3)]
    public class LevelRequirement : BuildingRequirement
    {
        [SerializeField] private int requiredLevel;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            var baseStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<BaseStats>();
            if (baseStats == null)
            {
                Debug.LogError("LevelRequirement: Player BaseStats not found");
                return false;
            }
            
            return baseStats.GetLevel() >= requiredLevel;
        }
        
        public override string GetDescription()
        {
            return $"Player Level {requiredLevel}";
        }
        
        public override string GetMissingInfo()
        {
            var baseStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<BaseStats>();
            if (baseStats == null) return "Player stats not found";
            
            int currentLevel = baseStats.GetLevel();
            if (currentLevel >= requiredLevel)
            {
                return "Level requirement met";
            }
            
            return $"Need level {requiredLevel} (current: {currentLevel})";
        }
    }
}
```

#### PrerequisiteBuildingRequirement.cs

```csharp
using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires another building to be constructed first.
    /// </summary>
    [CreateAssetMenu(fileName = "New Prerequisite Requirement", menuName = "Homestead/Requirements/Prerequisite Building", order = 4)]
    public class PrerequisiteBuildingRequirement : BuildingRequirement
    {
        [SerializeField] private BuildingData prerequisiteBuilding;
        [SerializeField] private int minimumLevel = 1;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (prerequisiteBuilding == null)
            {
                Debug.LogError("PrerequisiteBuildingRequirement: Prerequisite building is null");
                return false;
            }
            
            if (EstateManager.Instance == null)
            {
                Debug.LogError("PrerequisiteBuildingRequirement: EstateManager not found");
                return false;
            }
            
            return EstateManager.Instance.HasBuildingAtLevel(prerequisiteBuilding.BuildingId, minimumLevel);
        }
        
        public override string GetDescription()
        {
            if (prerequisiteBuilding == null) return "Invalid prerequisite";
            
            if (minimumLevel > 1)
            {
                return $"Requires: {prerequisiteBuilding.DisplayName} (Level {minimumLevel}+)";
            }
            
            return $"Requires: {prerequisiteBuilding.DisplayName}";
        }
        
        public override string GetMissingInfo()
        {
            if (prerequisiteBuilding == null) return "Invalid prerequisite";
            
            if (EstateManager.Instance == null) return "Estate manager not found";
            
            if (!EstateManager.Instance.HasBuilding(prerequisiteBuilding.BuildingId))
            {
                return $"{prerequisiteBuilding.DisplayName} not built";
            }
            
            var buildings = EstateManager.Instance.GetBuildingsOfType(prerequisiteBuilding.BuildingId);
            int maxLevel = 0;
            foreach (var building in buildings)
            {
                if (building.currentLevel > maxLevel)
                {
                    maxLevel = building.currentLevel;
                }
            }
            
            if (maxLevel < minimumLevel)
            {
                return $"{prerequisiteBuilding.DisplayName} needs to be level {minimumLevel} (current: {maxLevel})";
            }
            
            return "Prerequisite met";
        }
    }
}
```

#### LogicalRequirement.cs

```csharp
using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Combines multiple requirements with AND/OR logic.
    /// Supports nested logical operators.
    /// </summary>
    [CreateAssetMenu(fileName = "New Logical Requirement", menuName = "Homestead/Requirements/Logical (AND/OR)", order = 5)]
    public class LogicalRequirement : BuildingRequirement
    {
        public enum LogicalOperator
        {
            AND,
            OR
        }
        
        [SerializeField] private LogicalOperator logicalOperator = LogicalOperator.AND;
        [SerializeField] private BuildingRequirement[] subRequirements;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (subRequirements == null || subRequirements.Length == 0)
            {
                Debug.LogWarning("LogicalRequirement: No sub-requirements defined");
                return true; // Empty logical requirement is considered valid
            }
            
            if (logicalOperator == LogicalOperator.AND)
            {
                // All sub-requirements must pass
                foreach (var requirement in subRequirements)
                {
                    if (requirement == null)
                    {
                        Debug.LogWarning("LogicalRequirement: Null sub-requirement in AND group");
                        continue;
                    }
                    
                    if (!requirement.Validate(skipResourceChecks))
                    {
                        return false;
                    }
                }
                return true;
            }
            else // OR
            {
                // At least one sub-requirement must pass
                foreach (var requirement in subRequirements)
                {
                    if (requirement == null)
                    {
                        Debug.LogWarning("LogicalRequirement: Null sub-requirement in OR group");
                        continue;
                    }
                    
                    if (requirement.Validate(skipResourceChecks))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        
        public override string GetDescription()
        {
            if (subRequirements == null || subRequirements.Length == 0)
            {
                return "No requirements";
            }
            
            string operatorText = logicalOperator == LogicalOperator.AND ? "AND" : "OR";
            string result = $"({operatorText}): ";
            
            for (int i = 0; i < subRequirements.Length; i++)
            {
                if (subRequirements[i] != null)
                {
                    result += subRequirements[i].GetDescription();
                    if (i < subRequirements.Length - 1)
                    {
                        result += $" {operatorText} ";
                    }
                }
            }
            
            return result;
        }
        
        public override string GetMissingInfo()
        {
            if (subRequirements == null || subRequirements.Length == 0)
            {
                return "No requirements";
            }
            
            string result = "";
            
            if (logicalOperator == LogicalOperator.AND)
            {
                // Show all failed requirements
                foreach (var requirement in subRequirements)
                {
                    if (requirement != null && !requirement.Validate())
                    {
                        if (result.Length > 0) result += "\n";
                        result += "✗ " + requirement.GetMissingInfo();
                    }
                }
                
                if (string.IsNullOrEmpty(result))
                {
                    result = "All requirements met";
                }
            }
            else // OR
            {
                // Show all requirements with status
                bool anyMet = false;
                foreach (var requirement in subRequirements)
                {
                    if (requirement != null)
                    {
                        bool met = requirement.Validate();
                        if (met) anyMet = true;
                        
                        if (result.Length > 0) result += "\n";
                        result += (met ? "✓ " : "✗ ") + requirement.GetDescription();
                    }
                }
                
                if (anyMet)
                {
                    result = "At least one requirement met:\n" + result;
                }
                else
                {
                    result = "Need at least one:\n" + result;
                }
            }
            
            return result;
        }
        
        public override bool DeductResources()
        {
            if (subRequirements == null || subRequirements.Length == 0)
            {
                return true;
            }
            
            // For AND: deduct from all requirements
            // For OR: deduct only from requirements that passed validation
            foreach (var requirement in subRequirements)
            {
                if (requirement == null) continue;
                
                if (logicalOperator == LogicalOperator.AND)
                {
                    if (!requirement.DeductResources())
                    {
                        return false;
                    }
                }
                else // OR
                {
                    // Only deduct from the first requirement that validates
                    if (requirement.Validate())
                    {
                        return requirement.DeductResources();
                    }
                }
            }
            
            return true;
        }
    }
}
```

#### RequirementValidator.cs

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Validates building requirements and provides detailed feedback.
    /// </summary>
    public class RequirementValidator
    {
        /// <summary>
        /// Validate all requirements for a building/level.
        /// </summary>
        /// <param name="requirements">Array of requirements to validate</param>
        /// <param name="skipResourceChecks">If true, skip inventory/currency checks</param>
        /// <returns>Validation result with detailed information</returns>
        public RequirementValidationResult ValidateRequirements(BuildingRequirement[] requirements, bool skipResourceChecks = false)
        {
            var result = new RequirementValidationResult
            {
                IsValid = true,
                FailedRequirements = new List<RequirementFailureInfo>()
            };
            
            if (requirements == null || requirements.Length == 0)
            {
                return result; // No requirements = valid
            }
            
            foreach (var requirement in requirements)
            {
                if (requirement == null)
                {
                    Debug.LogWarning("RequirementValidator: Null requirement in array");
                    continue;
                }
                
                bool passed = requirement.Validate(skipResourceChecks);
                
                if (!passed)
                {
                    result.IsValid = false;
                    result.FailedRequirements.Add(new RequirementFailureInfo
                    {
                        requirement = requirement,
                        description = requirement.GetDescription(),
                        missingInfo = requirement.GetMissingInfo()
                    });
                }
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Result of requirement validation.
    /// </summary>
    public class RequirementValidationResult
    {
        public bool IsValid;
        public List<RequirementFailureInfo> FailedRequirements;
        
        public string GetFailureSummary()
        {
            if (IsValid) return "All requirements met";
            
            string summary = "Missing requirements:\n";
            foreach (var failure in FailedRequirements)
            {
                summary += $"• {failure.missingInfo}\n";
            }
            
            return summary;
        }
    }
    
    /// <summary>
    /// Information about a failed requirement.
    /// </summary>
    public class RequirementFailureInfo
    {
        public BuildingRequirement requirement;
        public string description;
        public string missingInfo;
    }
}
```

### UI Components

#### ConstructionMenuUI.cs

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Control;
using System.Collections.Generic;

namespace RPG.Homestead
{
    /// <summary>
    /// UI for browsing and constructing buildings.
    /// Displays building information, requirements, and action buttons.
    /// </summary>
    public class ConstructionMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Transform buildingListContainer;
        [SerializeField] private GameObject buildingButtonPrefab;
        
        [Header("Building Details Panel")]
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private TextMeshProUGUI buildingDescriptionText;
        [SerializeField] private TextMeshProUGUI buildingLevelText;
        [SerializeField] private Image buildingIconImage;
        [SerializeField] private Transform requirementsContainer;
        [SerializeField] private GameObject requirementItemPrefab;
        
        [Header("Action Buttons")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button demolishButton;
        [SerializeField] private Button closeButton;
        
        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        
        private BuildingSlot currentSlot;
        private PlayerController currentPlayer;
        private BuildingData selectedBuilding;
        private System.Action pendingAction;
        
        private void Awake()
        {
            // Setup button listeners
            buildButton.onClick.AddListener(OnBuildClicked);
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            demolishButton.onClick.AddListener(OnDemolishClicked);
            closeButton.onClick.AddListener(Close);
            
            confirmYesButton.onClick.AddListener(OnConfirmYes);
            confirmNoButton.onClick.AddListener(OnConfirmNo);
            
            // Hide menu initially
            menuPanel.SetActive(false);
            confirmationDialog.SetActive(false);
        }
        
        public void Open(BuildingSlot slot, PlayerController player)
        {
            currentSlot = slot;
            currentPlayer = player;
            
            menuPanel.SetActive(true);
            Time.timeScale = 0f; // Pause game
            
            PopulateBuildingList();
            
            // Select first building or current building
            if (slot.IsOccupied())
            {
                SelectBuilding(slot.GetCurrentBuilding().buildingData);
            }
            else if (slot.GetAllowedBuildings().Length > 0)
            {
                SelectBuilding(slot.GetAllowedBuildings()[0]);
            }
        }
        
        public void Close()
        {
            menuPanel.SetActive(false);
            Time.timeScale = 1f; // Resume game
            
            currentSlot = null;
            currentPlayer = null;
            selectedBuilding = null;
        }
        
        private void PopulateBuildingList()
        {
            // Clear existing buttons
            foreach (Transform child in buildingListContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create button for each allowed building
            foreach (var building in currentSlot.GetAllowedBuildings())
            {
                if (building == null) continue;
                
                GameObject buttonObj = Instantiate(buildingButtonPrefab, buildingListContainer);
                var button = buttonObj.GetComponent<Button>();
                var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                var icon = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
                
                if (text != null)
                {
                    text.text = building.DisplayName;
                    
                    // Add indicator for unlocked buildings
                    if (EstateManager.Instance.IsBuildingUnlocked(building.BuildingId))
                    {
                        text.text += " [FREE]";
                    }
                }
                
                if (icon != null && building.Icon != null)
                {
                    icon.sprite = building.Icon;
                }
                
                button.onClick.AddListener(() => SelectBuilding(building));
            }
        }
        
        private void SelectBuilding(BuildingData building)
        {
            selectedBuilding = building;
            UpdateBuildingDetails();
            UpdateActionButtons();
        }
        
        private void UpdateBuildingDetails()
        {
            if (selectedBuilding == null) return;
            
            // Update basic info
            buildingNameText.text = selectedBuilding.DisplayName;
            buildingDescriptionText.text = selectedBuilding.Description;
            
            if (buildingIconImage != null && selectedBuilding.Icon != null)
            {
                buildingIconImage.sprite = selectedBuilding.Icon;
                buildingIconImage.gameObject.SetActive(true);
            }
            else if (buildingIconImage != null)
            {
                buildingIconImage.gameObject.SetActive(false);
            }
            
            // Update level info
            if (currentSlot.IsOccupied() && currentSlot.GetCurrentBuilding().buildingData == selectedBuilding)
            {
                var instance = currentSlot.GetCurrentBuilding();
                buildingLevelText.text = $"Level {instance.currentLevel} / {selectedBuilding.MaxLevel}";
            }
            else
            {
                buildingLevelText.text = $"Not Built (Max Level: {selectedBuilding.MaxLevel})";
            }
            
            // Update requirements
            UpdateRequirementsList();
        }
        
        private void UpdateRequirementsList()
        {
            // Clear existing requirements
            foreach (Transform child in requirementsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Determine which level requirements to show
            int targetLevel = 1;
            bool isFreeReconstruction = false;
            
            if (currentSlot.IsOccupied() && currentSlot.GetCurrentBuilding().buildingData == selectedBuilding)
            {
                // Show upgrade requirements
                targetLevel = currentSlot.GetCurrentBuilding().currentLevel + 1;
            }
            else
            {
                // Show construction requirements
                targetLevel = 1;
                isFreeReconstruction = EstateManager.Instance.IsBuildingUnlocked(selectedBuilding.BuildingId);
            }
            
            if (targetLevel > selectedBuilding.MaxLevel)
            {
                // Max level reached
                GameObject item = Instantiate(requirementItemPrefab, requirementsContainer);
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "✓ Maximum level reached";
                    text.color = Color.green;
                }
                return;
            }
            
            // Get requirements for target level
            var requirements = selectedBuilding.GetRequirementsForLevel(targetLevel);
            
            if (isFreeReconstruction)
            {
                GameObject item = Instantiate(requirementItemPrefab, requirementsContainer);
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "✓ FREE RECONSTRUCTION (Building Unlocked)";
                    text.color = Color.cyan;
                }
            }
            
            // Validate and display requirements
            RequirementValidator validator = new RequirementValidator();
            var result = validator.ValidateRequirements(requirements, isFreeReconstruction);
            
            if (requirements.Length == 0)
            {
                GameObject item = Instantiate(requirementItemPrefab, requirementsContainer);
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "✓ No requirements";
                    text.color = Color.green;
                }
            }
            else
            {
                foreach (var requirement in requirements)
                {
                    if (requirement == null) continue;
                    
                    GameObject item = Instantiate(requirementItemPrefab, requirementsContainer);
                    var text = item.GetComponentInChildren<TextMeshProUGUI>();
                    
                    if (text != null)
                    {
                        bool met = requirement.Validate(isFreeReconstruction);
                        string prefix = met ? "✓" : "✗";
                        text.text = $"{prefix} {requirement.GetDescription()}";
                        text.color = met ? Color.green : Color.red;
                        
                        // Add detailed info on hover (could use tooltip system)
                        if (!met)
                        {
                            text.text += $"\n  {requirement.GetMissingInfo()}";
                        }
                    }
                }
            }
        }
        
        private void UpdateActionButtons()
        {
            if (selectedBuilding == null)
            {
                buildButton.gameObject.SetActive(false);
                upgradeButton.gameObject.SetActive(false);
                demolishButton.gameObject.SetActive(false);
                return;
            }
            
            bool isOccupied = currentSlot.IsOccupied();
            bool isSameBuilding = isOccupied && currentSlot.GetCurrentBuilding().buildingData == selectedBuilding;
            
            // Build button: show if slot is empty or different building
            buildButton.gameObject.SetActive(!isOccupied || !isSameBuilding);
            
            // Upgrade button: show if same building and not max level
            bool canUpgrade = isSameBuilding && currentSlot.GetCurrentBuilding().CanUpgrade();
            upgradeButton.gameObject.SetActive(canUpgrade);
            
            // Demolish button: show if slot is occupied
            demolishButton.gameObject.SetActive(isOccupied);
            
            // Enable/disable based on requirements
            if (buildButton.gameObject.activeSelf)
            {
                bool isFreeReconstruction = EstateManager.Instance.IsBuildingUnlocked(selectedBuilding.BuildingId);
                RequirementValidator validator = new RequirementValidator();
                var result = validator.ValidateRequirements(selectedBuilding.GetRequirementsForLevel(1), isFreeReconstruction);
                buildButton.interactable = result.IsValid;
            }
            
            if (upgradeButton.gameObject.activeSelf)
            {
                int nextLevel = currentSlot.GetCurrentBuilding().currentLevel + 1;
                RequirementValidator validator = new RequirementValidator();
                var result = validator.ValidateRequirements(selectedBuilding.GetRequirementsForLevel(nextLevel), false);
                upgradeButton.interactable = result.IsValid;
            }
        }
        
        private void OnBuildClicked()
        {
            if (currentSlot.IsOccupied())
            {
                // Need to demolish first
                ShowConfirmation(
                    $"Demolish {currentSlot.GetCurrentBuilding().buildingData.DisplayName} and build {selectedBuilding.DisplayName}?",
                    () => {
                        EstateManager.Instance.DemolishBuilding(currentSlot);
                        EstateManager.Instance.ConstructBuilding(currentSlot, selectedBuilding);
                        Close();
                    }
                );
            }
            else
            {
                bool success = EstateManager.Instance.ConstructBuilding(currentSlot, selectedBuilding);
                if (success)
                {
                    Close();
                }
                else
                {
                    Debug.LogWarning("Construction failed");
                }
            }
        }
        
        private void OnUpgradeClicked()
        {
            bool success = EstateManager.Instance.UpgradeBuilding(currentSlot);
            if (success)
            {
                // Refresh UI to show new level
                UpdateBuildingDetails();
                UpdateActionButtons();
            }
            else
            {
                Debug.LogWarning("Upgrade failed");
            }
        }
        
        private void OnDemolishClicked()
        {
            ShowConfirmation(
                $"Demolish {currentSlot.GetCurrentBuilding().buildingData.DisplayName}? (No refund, but can rebuild for free)",
                () => {
                    EstateManager.Instance.DemolishBuilding(currentSlot);
                    Close();
                }
            );
        }
        
        private void ShowConfirmation(string message, System.Action onConfirm)
        {
            confirmationText.text = message;
            confirmationDialog.SetActive(true);
            pendingAction = onConfirm;
        }
        
        private void OnConfirmYes()
        {
            confirmationDialog.SetActive(false);
            pendingAction?.Invoke();
            pendingAction = null;
        }
        
        private void OnConfirmNo()
        {
            confirmationDialog.SetActive(false);
            pendingAction = null;
        }
    }
}
```


### Teleport Item

#### HomesteadTeleportItem.cs

```csharp
using UnityEngine;
using GameDevTV.Inventories;
using UnityEngine.SceneManagement;

namespace RPG.Homestead
{
    /// <summary>
    /// Special inventory item that teleports player to homestead.
    /// Not consumed on use, supports cooldown.
    /// </summary>
    [CreateAssetMenu(fileName = "Homestead Teleport", menuName = "Homestead/Teleport Item", order = 0)]
    public class HomesteadTeleportItem : InventoryItem
    {
        [Header("Teleport Configuration")]
        [SerializeField] private string homesteadSceneName = "Homestead";
        [SerializeField] private float cooldownSeconds = 5f;
        
        private float lastUseTime = -999f;
        
        public override void Use(GameObject user)
        {
            // Check cooldown
            if (Time.time - lastUseTime < cooldownSeconds)
            {
                float remaining = cooldownSeconds - (Time.time - lastUseTime);
                Debug.Log($"Teleport on cooldown. Wait {remaining:F1} seconds.");
                return;
            }
            
            // Save current location for return teleport
            SaveReturnLocation();
            
            // Load homestead scene
            SceneManager.LoadScene(homesteadSceneName);
            
            lastUseTime = Time.time;
        }
        
        private void SaveReturnLocation()
        {
            // Store current scene for return teleport
            string currentScene = SceneManager.GetActiveScene().name;
            PlayerPrefs.SetString("HomesteadReturnScene", currentScene);
            PlayerPrefs.Save();
        }
        
        public static void ReturnToPreviousLocation()
        {
            string returnScene = PlayerPrefs.GetString("HomesteadReturnScene", "");
            if (!string.IsNullOrEmpty(returnScene))
            {
                SceneManager.LoadScene(returnScene);
            }
            else
            {
                Debug.LogWarning("No return location saved");
            }
        }
    }
}
```

## Algorithms

### Construction Algorithm

```
ALGORITHM: ConstructBuilding(slot, buildingData)
INPUT: BuildingSlot slot, BuildingData buildingData
OUTPUT: bool success

1. Validate inputs (slot != null, buildingData != null)
2. Check if slot is already occupied → return false if occupied
3. Determine if this is free reconstruction:
   - Check if buildingData.BuildingId is in unlockedBuildings set
4. Validate requirements:
   - Get requirements for level 1 from buildingData
   - Create RequirementValidator
   - Call validator.ValidateRequirements(requirements, isFreeReconstruction)
   - If validation fails → return false
5. Deduct resources (if not free reconstruction):
   - For each requirement in requirements:
     - Call requirement.DeductResources()
     - If any deduction fails → return false
6. Instantiate building prefab:
   - Get prefab for level 1 from buildingData
   - Instantiate at slot position and rotation
   - Set parent to buildingsParent transform
7. Create BuildingInstance:
   - Set buildingData reference
   - Set currentLevel = 1
   - Set slotId from slot
   - Set buildingObject reference
8. Update state:
   - Add instance to buildingsBySlotId dictionary
   - Add buildingData.BuildingId to unlockedBuildings set
   - Call slot.SetCurrentBuilding(instance)
9. Trigger events:
   - Invoke OnBuildingConstructed event
10. Trigger autosave
11. Return true
```

### Upgrade Algorithm

```
ALGORITHM: UpgradeBuilding(slot)
INPUT: BuildingSlot slot
OUTPUT: bool success

1. Validate input (slot != null)
2. Get current building instance from slot
   - If no building exists → return false
3. Calculate next level:
   - nextLevel = instance.currentLevel + 1
4. Check if upgrade is possible:
   - If nextLevel > buildingData.MaxLevel → return false
5. Validate requirements:
   - Get requirements for nextLevel from buildingData
   - Create RequirementValidator
   - Call validator.ValidateRequirements(requirements, false)
   - If validation fails → return false
6. Deduct resources:
   - For each requirement in requirements:
     - Call requirement.DeductResources()
     - If any deduction fails → return false
7. Replace building prefab:
   - Destroy current buildingObject
   - Get prefab for nextLevel from buildingData
   - Instantiate at slot position and rotation
   - Set parent to buildingsParent transform
8. Update instance:
   - Set instance.currentLevel = nextLevel
   - Set instance.buildingObject to new prefab
9. Trigger events:
   - Invoke OnBuildingUpgraded event
10. Trigger autosave
11. Return true
```

### Demolition Algorithm

```
ALGORITHM: DemolishBuilding(slot)
INPUT: BuildingSlot slot
OUTPUT: bool success

1. Validate input (slot != null)
2. Get current building instance from slot
   - If no building exists → return false
3. Destroy building prefab:
   - Destroy instance.buildingObject GameObject
4. Update state:
   - Remove instance from buildingsBySlotId dictionary
   - Keep buildingData.BuildingId in unlockedBuildings set (for free reconstruction)
   - Call slot.SetCurrentBuilding(null)
5. Trigger events:
   - Invoke OnBuildingDemolished event with slotId
6. Trigger autosave
7. Return true

NOTE: No resource refund is provided
```

### Requirement Validation Algorithm

```
ALGORITHM: ValidateRequirements(requirements, skipResourceChecks)
INPUT: BuildingRequirement[] requirements, bool skipResourceChecks
OUTPUT: RequirementValidationResult result

1. Initialize result:
   - result.IsValid = true
   - result.FailedRequirements = empty list
2. If requirements is null or empty → return result (valid)
3. For each requirement in requirements:
   - If requirement is null → skip (log warning)
   - Call requirement.Validate(skipResourceChecks)
   - If validation fails:
     - Set result.IsValid = false
     - Create RequirementFailureInfo:
       - Set requirement reference
       - Set description from requirement.GetDescription()
       - Set missingInfo from requirement.GetMissingInfo()
     - Add to result.FailedRequirements list
4. Return result
```

### Save State Algorithm

```
ALGORITHM: CaptureState()
OUTPUT: EstateManagerSaveData state

1. Create EstateManagerSaveData object
2. Set state.homesteadId = homesteadIdentifier
3. Convert unlockedBuildings HashSet to List:
   - state.unlockedBuildings = unlockedBuildings.ToList()
4. Initialize state.buildings as empty list
5. For each (slotId, instance) in buildingsBySlotId:
   - Create BuildingInstanceSaveData:
     - Set slotId
     - Set buildingId from instance.buildingData.BuildingId
     - Set currentLevel from instance.currentLevel
   - Add to state.buildings list
6. Return state
```

### Restore State Algorithm

```
ALGORITHM: RestoreState(state)
INPUT: object state

1. Validate state (not null, correct type)
2. Cast state to EstateManagerSaveData
3. Clear current state:
   - For each instance in buildingsBySlotId.Values:
     - Destroy instance.buildingObject
   - Clear buildingsBySlotId dictionary
4. Restore unlocked buildings:
   - unlockedBuildings = new HashSet(saveData.unlockedBuildings)
5. For each buildingData in saveData.buildings:
   - Find BuildingSlot with matching slotId in registeredSlots
   - If slot not found → log warning, continue
   - Get BuildingData by buildingId using BuildingData.GetBuildingById()
   - If buildingData not found → log error, continue
   - Get prefab for currentLevel from buildingData
   - If prefab is null → log error, continue
   - Instantiate prefab at slot position and rotation
   - Create BuildingInstance with all properties
   - Add to buildingsBySlotId dictionary
   - Call slot.SetCurrentBuilding(instance)
6. Log restoration summary
```

## Error Handling

### Validation Errors

**Null Reference Checks**
- All public methods validate input parameters for null
- Log error and return early if null detected
- Example: `if (slot == null) { Debug.LogError("..."); return false; }`

**Missing Component Checks**
- Check for required components (Inventory, Purse, QuestList, BaseStats)
- Log error with component name if not found
- Return false or default value

**Invalid Configuration Checks**
- BuildingData.OnValidate() checks for:
  - Empty building ID
  - Mismatched array lengths (prefabs, requirements vs maxLevel)
  - Null prefab references
- BuildingSlot.OnValidate() checks for:
  - Null building references in allowedBuildings array

### Runtime Errors

**Construction Failures**
- Slot already occupied → log warning, return false
- Requirements not met → log warning with details, return false
- Resource deduction fails → log error, return false
- Prefab instantiation fails → log error, return false

**Upgrade Failures**
- No building on slot → log warning, return false
- Already at max level → log warning, return false
- Requirements not met → log warning, return false
- Resource deduction fails → log error, return false

**Save/Load Errors**
- Corrupted save data → log error, initialize empty state
- Missing BuildingData during restore → log error, skip that building
- Missing BuildingSlot during restore → log warning, skip that building

### Error Recovery

**Graceful Degradation**
- If a building fails to restore, skip it and continue with others
- If a requirement validation throws exception, treat as failed requirement
- If autosave fails, log error but don't block gameplay

**User Feedback**
- Display error messages in UI when construction/upgrade fails
- Show detailed requirement information in Construction Menu
- Provide confirmation dialogs for destructive actions (demolish)

## Testing Strategy

### Unit Testing

The Player Homestead System is primarily a state management system with side effects (GameObject instantiation, scene management, save/load). Property-based testing is **not applicable** here. Instead, we use:

#### Unit Tests (Example-Based)

**Requirement Validation Tests**
- Test each requirement type with specific examples
- Test logical operators (AND/OR) with known inputs
- Test edge cases (empty requirements, null values)

Example test cases:
```csharp
[Test]
public void InventoryRequirement_WithSufficientItems_ReturnsTrue()
{
    // Arrange: Setup mock inventory with 10 wood
    // Act: Validate requirement for 5 wood
    // Assert: Validation passes
}

[Test]
public void InventoryRequirement_WithInsufficientItems_ReturnsFalse()
{
    // Arrange: Setup mock inventory with 3 wood
    // Act: Validate requirement for 5 wood
    // Assert: Validation fails
}

[Test]
public void LogicalRequirement_AND_AllMet_ReturnsTrue()
{
    // Arrange: Create AND requirement with 2 passing sub-requirements
    // Act: Validate
    // Assert: Passes
}

[Test]
public void LogicalRequirement_AND_OneFails_ReturnsFalse()
{
    // Arrange: Create AND requirement with 1 passing, 1 failing
    // Act: Validate
    // Assert: Fails
}

[Test]
public void LogicalRequirement_OR_OneMet_ReturnsTrue()
{
    // Arrange: Create OR requirement with 1 passing, 1 failing
    // Act: Validate
    // Assert: Passes
}
```

**State Management Tests**
- Test building construction updates state correctly
- Test upgrade increments level correctly
- Test demolition clears slot but keeps unlocked status
- Test free reconstruction skips resource checks

**Save/Load Tests**
- Test CaptureState serializes all buildings
- Test RestoreState recreates buildings correctly
- Test handling of corrupted save data
- Test handling of missing BuildingData references

#### Integration Tests

**System Integration Tests**
- Test construction deducts from actual Inventory component
- Test construction deducts from actual Purse component
- Test quest requirement checks actual QuestList component
- Test level requirement checks actual BaseStats component
- Test save/load integration with SavingSystem

**UI Integration Tests**
- Test Construction Menu displays correct buildings
- Test requirement UI updates when inventory changes
- Test button states update correctly
- Test confirmation dialogs work correctly

#### Mock-Based Tests

**Isolated Component Tests**
- Mock Inventory for testing InventoryRequirement
- Mock Purse for testing CurrencyRequirement
- Mock QuestList for testing QuestRequirement
- Mock BaseStats for testing LevelRequirement
- Mock EstateManager for testing PrerequisiteBuildingRequirement

### Manual Testing Checklist

**Construction Flow**
- [ ] Player can interact with empty building slot
- [ ] Construction menu opens and displays allowed buildings
- [ ] Requirements display correctly (met/unmet)
- [ ] Build button is disabled when requirements not met
- [ ] Build button is enabled when requirements met
- [ ] Construction deducts resources correctly
- [ ] Building prefab instantiates at correct position
- [ ] Building is added to unlocked list
- [ ] Autosave triggers after construction

**Upgrade Flow**
- [ ] Upgrade button appears for constructed buildings
- [ ] Upgrade button is disabled when requirements not met
- [ ] Upgrade button is disabled at max level
- [ ] Upgrade deducts resources correctly
- [ ] Building prefab is replaced with next level
- [ ] Level counter updates correctly
- [ ] Autosave triggers after upgrade

**Demolition Flow**
- [ ] Demolish button appears for constructed buildings
- [ ] Confirmation dialog appears when demolish clicked
- [ ] Building is removed from slot
- [ ] Building remains in unlocked list
- [ ] No resources are refunded
- [ ] Autosave triggers after demolition

**Free Reconstruction**
- [ ] Unlocked buildings show [FREE] indicator
- [ ] Free reconstruction skips resource requirements
- [ ] Free reconstruction still checks quest/level/prerequisite requirements
- [ ] Free reconstruction instantiates at level 1

**Save/Load**
- [ ] All buildings save correctly
- [ ] All buildings restore correctly after scene reload
- [ ] Unlocked buildings list persists
- [ ] Building levels persist
- [ ] Save works on WebGL platform
- [ ] Save works with Yandex integration

**Error Handling**
- [ ] Null building data is handled gracefully
- [ ] Missing prefabs are handled gracefully
- [ ] Corrupted save data initializes empty state
- [ ] Missing components log appropriate errors

### Performance Testing

**Instantiation Performance**
- Test with 10+ buildings instantiated simultaneously
- Measure frame time during building construction
- Ensure no frame drops during prefab instantiation

**Save/Load Performance**
- Test save/load with 20+ buildings
- Measure serialization time
- Ensure save/load completes within 1 second

**UI Performance**
- Test Construction Menu with 20+ allowed buildings
- Measure UI update time when requirements change
- Ensure smooth scrolling in building list

## Directory Structure

```
Assets/
├── Scripts/
│   └── Homestead/
│       ├── Core/
│       │   ├── EstateManager.cs
│       │   ├── BuildingSlot.cs
│       │   ├── BuildingInstance.cs
│       │   └── RequirementValidator.cs
│       ├── Data/
│       │   ├── BuildingData.cs
│       │   └── BuildingRequirement.cs
│       ├── Requirements/
│       │   ├── InventoryRequirement.cs
│       │   ├── CurrencyRequirement.cs
│       │   ├── QuestRequirement.cs
│       │   ├── LevelRequirement.cs
│       │   ├── PrerequisiteBuildingRequirement.cs
│       │   └── LogicalRequirement.cs
│       ├── UI/
│       │   ├── ConstructionMenuUI.cs
│       │   ├── BuildingButtonUI.cs (optional helper)
│       │   └── RequirementItemUI.cs (optional helper)
│       └── Items/
│           └── HomesteadTeleportItem.cs
│
└── Internal Assets/
    └── Homestead/
        ├── Buildings/
        │   ├── House_Data.asset
        │   ├── Workshop_Data.asset
        │   ├── Storage_Data.asset
        │   └── ... (other building ScriptableObjects)
        ├── Requirements/
        │   ├── House_Level1_Requirements.asset
        │   ├── House_Level2_Requirements.asset
        │   └── ... (requirement ScriptableObjects)
        ├── Prefabs/
        │   ├── Buildings/
        │   │   ├── House_Level1.prefab
        │   │   ├── House_Level2.prefab
        │   │   ├── House_Level3.prefab
        │   │   └── ... (building prefabs)
        │   └── UI/
        │       ├── ConstructionMenu.prefab
        │       ├── BuildingButton.prefab
        │       └── RequirementItem.prefab
        ├── Icons/
        │   ├── house_icon.png
        │   ├── workshop_icon.png
        │   └── ... (building icons)
        └── Scenes/
            └── Homestead.unity (homestead scene)
```

## Integration Points

### Existing Systems Integration

**Inventory System (GameDevTV.Inventories.Inventory)**
- Used by: InventoryRequirement
- Methods called:
  - `Inventory.GetPlayerInventory()` - Get player inventory instance
  - `inventory.GetItemCount(item)` - Check item quantity
  - `inventory.RemoveFromSlot(slot, number)` - Deduct items
- No modifications required

**Purse System (RPG.Inventories.Purse)**
- Used by: CurrencyRequirement
- Methods called:
  - `purse.GetBalance()` - Check currency amount
  - `purse.UpdateBalance(amount)` - Deduct currency (negative amount)
- No modifications required

**Quest System (RPG.Quests.QuestList)**
- Used by: QuestRequirement
- Methods called:
  - `questList.GetQuestStatus(quest)` - Get quest status
  - `status.IsComplete()` - Check if quest complete
  - `status.IsObjectiveComplete(objective)` - Check specific objective
- No modifications required

**Experience/Stats System (RPG.Stats.BaseStats)**
- Used by: LevelRequirement
- Methods called:
  - `baseStats.GetLevel()` - Get player level
- No modifications required

**Saving System (GameDevTV.Saving.SavingSystem)**
- Used by: EstateManager (implements ISaveable)
- Methods called:
  - `savingSystem.Save("autosave")` - Trigger autosave
- Interface implemented:
  - `ISaveable.CaptureState()` - Serialize homestead state
  - `ISaveable.RestoreState(state)` - Deserialize homestead state
- No modifications required

### Quest System Extension (Optional)

To support quest objectives for building construction, add new predicates to QuestList:

```csharp
// In QuestList.Evaluate() method
case "BuildingConstructed":
    if (parameters.Length < 1) return false;
    string buildingId = parameters[0];
    return EstateManager.Instance?.HasBuilding(buildingId) ?? false;

case "BuildingUpgraded":
    if (parameters.Length < 2) return false;
    string buildingId = parameters[0];
    int minLevel = int.Parse(parameters[1]);
    return EstateManager.Instance?.HasBuildingAtLevel(buildingId, minLevel) ?? false;
```

This allows quest objectives like:
- "BuildingConstructed, house_basic" - Requires player to build basic house
- "BuildingUpgraded, workshop, 2" - Requires workshop to be level 2+

## Future Extensibility

### Adding New Requirement Types

To add a new requirement type:

1. Create new class inheriting from `BuildingRequirement`
2. Implement abstract methods:
   - `Validate(bool skipResourceChecks)`
   - `GetDescription()`
   - `GetMissingInfo()`
   - Optionally override `DeductResources()`
3. Add `[CreateAssetMenu]` attribute
4. No changes to existing code required

Example:
```csharp
[CreateAssetMenu(fileName = "New Time Requirement", menuName = "Homestead/Requirements/Time of Day")]
public class TimeOfDayRequirement : BuildingRequirement
{
    [SerializeField] private float minHour = 6f;
    [SerializeField] private float maxHour = 18f;
    
    public override bool Validate(bool skipResourceChecks = false)
    {
        // Check game time system
        float currentHour = TimeManager.Instance.GetCurrentHour();
        return currentHour >= minHour && currentHour <= maxHour;
    }
    
    public override string GetDescription()
    {
        return $"Time: {minHour:F0}:00 - {maxHour:F0}:00";
    }
    
    public override string GetMissingInfo()
    {
        float currentHour = TimeManager.Instance.GetCurrentHour();
        return $"Current time: {currentHour:F0}:00 (need {minHour:F0}:00 - {maxHour:F0}:00)";
    }
}
```

### Adding Building Functionality

Buildings can have any functionality by adding components to their prefabs:

**Crafting Station**
- Add `CraftingStation` component to building prefab
- Configure recipes in Inspector
- No code changes required

**Storage Chest**
- Add `ChestInventory` component to building prefab
- Configure storage size in Inspector
- No code changes required

**NPC Interaction**
- Add `AIController` and `DialogueTrigger` components
- Configure dialogue and quests
- No code changes required

**Custom Functionality**
- Create custom MonoBehaviour
- Add to building prefab
- Access EstateManager for homestead state if needed

### Multiple Homesteads Support

The system is designed to support multiple homesteads:

1. Each EstateManager has unique `homesteadIdentifier`
2. Save system stores multiple homestead states by identifier
3. Teleport items can specify target homestead
4. EstateManager.GetBuildingsAcrossHomesteads() can query all homesteads

To implement:
- Create multiple homestead scenes
- Assign unique identifiers to each EstateManager
- Create teleport items for each homestead
- Extend save/load to handle multiple homesteads

## Summary

The Player Homestead System provides a robust, extensible foundation for meta-progression through building construction and upgrades. Key strengths:

- **Non-invasive**: Integrates with existing systems without modification
- **Data-driven**: All configuration through ScriptableObjects
- **Extensible**: Easy to add new requirement types and building functionalities
- **Robust**: Comprehensive error handling and validation
- **Persistent**: Full save/load support with WebGL compatibility
- **Designer-friendly**: Maximum Inspector configurability

The architecture supports future expansion including multiple homesteads, new requirement types, and complex building interactions while maintaining clean separation of concerns and testability.

