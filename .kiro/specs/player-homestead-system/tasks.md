# Implementation Plan: Player Homestead System

## Overview

This implementation plan breaks down the Player Homestead System into discrete, sequential coding tasks. The system allows players to construct, upgrade, and manage buildings on their personal estate, integrating with existing RPG systems (Inventory, Purse, QuestList, Experience, SavingSystem) without modifying them.

**Implementation Language:** C# (Unity)

**Key Integration Points:**
- GameDevTV.Inventories (Inventory system)
- RPG.Inventories (Purse system)
- RPG.Quests (Quest system)
- RPG.Stats (Experience/BaseStats system)
- GameDevTV.Saving (SavingSystem)
- RPG.Control (PlayerController, IRaycastable)

## Tasks

- [x] 1. Create project structure and namespace
  - Create directory: `Assets/Scripts/Homestead/`
  - Create subdirectories: `Core/`, `Data/`, `Requirements/`, `UI/`, `Items/`
  - Create directory: `Assets/Internal Assets/Homestead/`
  - Create subdirectories: `Buildings/`, `Requirements/`, `Prefabs/`, `Icons/`
  - _Requirements: 2.1, 3.1_

- [x] 2. Implement core data models
  - [x] 2.1 Create BuildingInstance.cs runtime data class
    - Implement serializable class with buildingData, currentLevel, slotId, buildingObject fields
    - Implement IsMaxLevel(), CanUpgrade(), GetDisplayName() helper methods
    - _Requirements: 2.3, 3.1_
  
  - [x] 2.2 Create BuildingData.cs ScriptableObject
    - Implement ScriptableObject with identification fields (buildingId, displayName, description, icon, category)
    - Implement configuration fields (maxLevel, maxInstances, levelPrefabs array, levelRequirements array)
    - Implement static registry dictionary for BuildingData lookup by ID
    - Implement GetBuildingById() static method
    - Implement GetPrefabForLevel() and GetRequirementsForLevel() methods
    - Implement OnValidate() for editor validation
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 25.2_

- [x] 3. Implement requirement validation system
  - [x] 3.1 Create BuildingRequirement.cs abstract base class
    - Implement abstract Validate(bool skipResourceChecks) method
    - Implement abstract GetDescription() method
    - Implement abstract GetMissingInfo() method
    - Implement virtual DeductResources() method with default implementation
    - _Requirements: 4.1, 17.1, 17.2_
  
  - [x] 3.2 Create InventoryRequirement.cs
    - Inherit from BuildingRequirement
    - Implement Validate() using Inventory.GetPlayerInventory() and GetItemCount()
    - Implement GetDescription() and GetMissingInfo()
    - Implement DeductResources() using Inventory.RemoveFromSlot()
    - Add CreateAssetMenu attribute
    - _Requirements: 4.1, 4.2, 4.3, 9.4, 9.5_
  
  - [x] 3.3 Create CurrencyRequirement.cs
    - Inherit from BuildingRequirement
    - Implement Validate() using Purse.GetBalance()
    - Implement GetDescription() and GetMissingInfo()
    - Implement DeductResources() using Purse.UpdateBalance()
    - Add CreateAssetMenu attribute
    - _Requirements: 4.4, 4.5, 9.5_
  
  - [x] 3.4 Create QuestRequirement.cs
    - Inherit from BuildingRequirement
    - Implement Validate() using QuestList.GetQuestStatus() and IsComplete()
    - Support specific objective checking with IsObjectiveComplete()
    - Implement GetDescription() and GetMissingInfo()
    - Add CreateAssetMenu attribute
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  
  - [x] 3.5 Create LevelRequirement.cs
    - Inherit from BuildingRequirement
    - Implement Validate() using BaseStats.GetLevel()
    - Implement GetDescription() and GetMissingInfo()
    - Add CreateAssetMenu attribute
    - _Requirements: 6.1, 6.2, 6.3, 6.4_
  
  - [x] 3.6 Create PrerequisiteBuildingRequirement.cs
    - Inherit from BuildingRequirement
    - Implement Validate() using EstateManager.HasBuildingAtLevel()
    - Support minimum level checking
    - Implement GetDescription() and GetMissingInfo()
    - Add CreateAssetMenu attribute
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
  
  - [x] 3.7 Create LogicalRequirement.cs
    - Inherit from BuildingRequirement
    - Implement AND/OR logical operators enum
    - Implement Validate() with AND logic (all must pass) and OR logic (at least one must pass)
    - Support nested logical requirements
    - Implement GetDescription() and GetMissingInfo() with operator display
    - Implement DeductResources() with operator-specific logic
    - Add CreateAssetMenu attribute
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_
  
  - [x] 3.8 Create RequirementValidator.cs
    - Implement ValidateRequirements() method accepting BuildingRequirement array
    - Return RequirementValidationResult with IsValid flag and FailedRequirements list
    - Implement RequirementValidationResult class with GetFailureSummary() method
    - Implement RequirementFailureInfo class for detailed failure information
    - _Requirements: 4.6, 4.7, 17.5_

- [x] 4. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement BuildingSlot component
  - [x] 5.1 Create BuildingSlot.cs MonoBehaviour
    - Implement IRaycastable interface for player interaction
    - Add serialized fields: slotId, allowedBuildings array, slotCategory, interactionRadius, interactionPrompt
    - Implement Awake() to generate unique ID if not set and register with EstateManager
    - Implement OnDestroy() to unregister from EstateManager
    - Implement GetSlotId(), GetSlotCategory(), GetAllowedBuildings(), GetCurrentBuilding(), IsOccupied() accessors
    - Implement SetCurrentBuilding() mutator
    - Implement HandleRaycast() to open ConstructionMenuUI on interaction
    - Implement GetCursorType() to return Interact cursor
    - Implement OnDrawGizmosSelected() for editor visualization
    - Implement OnValidate() for editor validation
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 25.2_

- [x] 6. Implement EstateManager singleton
  - [x] 6.1 Create EstateManager.cs core manager
    - Implement MonoBehaviour and ISaveable interface
    - Add serialized fields: homesteadIdentifier, buildingsParent transform
    - Implement singleton pattern with Instance property
    - Implement runtime state: buildingsBySlotId dictionary, unlockedBuildings HashSet, registeredSlots list
    - Implement events: OnBuildingConstructed, OnBuildingUpgraded, OnBuildingDemolished
    - Implement RegisterSlot() and UnregisterSlot() methods
    - _Requirements: 1.2, 14.1, 20.1_
  
  - [x] 6.2 Implement ConstructBuilding() method
    - Validate inputs (slot, buildingData not null)
    - Check if slot is already occupied
    - Determine if free reconstruction (check unlockedBuildings)
    - Validate requirements using RequirementValidator
    - Deduct resources if not free reconstruction
    - Instantiate building prefab at slot position
    - Create BuildingInstance and add to buildingsBySlotId
    - Add building to unlockedBuildings set
    - Update slot reference
    - Trigger OnBuildingConstructed event
    - Trigger autosave
    - _Requirements: 9.1, 9.2, 9.3, 9.6, 9.7, 9.8, 9.9_
  
  - [x] 6.3 Implement UpgradeBuilding() method
    - Validate slot has building
    - Calculate next level
    - Check if already at max level
    - Validate next level requirements
    - Deduct resources
    - Destroy old prefab and instantiate new level prefab
    - Update BuildingInstance level
    - Trigger OnBuildingUpgraded event
    - Trigger autosave
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8_
  
  - [x] 6.4 Implement DemolishBuilding() method
    - Validate slot has building
    - Destroy building prefab GameObject
    - Remove from buildingsBySlotId but keep in unlockedBuildings
    - Clear slot reference
    - Trigger OnBuildingDemolished event
    - Trigger autosave
    - No resource refund
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7_
  
  - [x] 6.5 Implement query methods
    - Implement IsBuildingUnlocked() to check unlockedBuildings set
    - Implement GetBuildingOnSlot() to query buildingsBySlotId
    - Implement HasBuilding() to check if building type exists
    - Implement GetBuildingsOfType() to get all instances of a type
    - Implement GetBuildingCount() to count instances of a type
    - Implement HasBuildingAtLevel() to check building with minimum level
    - _Requirements: 7.1, 12.1, 21.2_
  
  - [x] 6.6 Implement ISaveable interface
    - Implement CaptureState() to serialize buildingsBySlotId and unlockedBuildings
    - Create EstateManagerSaveData and BuildingInstanceSaveData classes
    - Implement RestoreState() to deserialize and recreate buildings
    - Handle missing BuildingData and BuildingSlot gracefully
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 14.9, 25.3_
  
  - [x] 6.7 Implement helper methods
    - Implement DeductResources() private method to call DeductResources() on all requirements
    - Implement TriggerAutosave() to find SavingSystem and call Save()
    - _Requirements: 14.8_

- [x] 7. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Implement ConstructionMenuUI
  - [x] 8.1 Create ConstructionMenuUI.cs UI controller
    - Add serialized fields for UI references: menuPanel, buildingListContainer, buildingButtonPrefab
    - Add serialized fields for details panel: buildingNameText, buildingDescriptionText, buildingLevelText, buildingIconImage, requirementsContainer, requirementItemPrefab
    - Add serialized fields for action buttons: buildButton, upgradeButton, demolishButton, closeButton
    - Add serialized fields for confirmation dialog: confirmationDialog, confirmationText, confirmYesButton, confirmNoButton
    - Implement Awake() to setup button listeners and hide menu
    - Implement Open() to display menu, pause game, populate building list
    - Implement Close() to hide menu and resume game
    - _Requirements: 15.1, 15.9_
  
  - [x] 8.2 Implement building list population
    - Implement PopulateBuildingList() to create buttons for allowed buildings
    - Display building name and icon on each button
    - Add [FREE] indicator for unlocked buildings
    - Wire button click to SelectBuilding()
    - _Requirements: 15.1, 15.2, 12.5_
  
  - [x] 8.3 Implement building details display
    - Implement SelectBuilding() to update selected building
    - Implement UpdateBuildingDetails() to display name, description, icon, level
    - Show current level for occupied slots
    - Show "Not Built" for empty slots
    - _Requirements: 15.2, 15.3, 15.4, 15.5_
  
  - [x] 8.4 Implement requirements display
    - Implement UpdateRequirementsList() to display all requirements
    - Determine target level (1 for construction, currentLevel+1 for upgrade)
    - Check if free reconstruction
    - Display [FREE RECONSTRUCTION] indicator for unlocked buildings
    - Validate each requirement and display with ✓/✗ indicator
    - Display requirement description and missing info
    - Color code: green for met, red for unmet, cyan for free
    - _Requirements: 15.3, 4.7, 12.2, 12.3_
  
  - [x] 8.5 Implement action buttons
    - Implement UpdateActionButtons() to show/hide Build, Upgrade, Demolish buttons
    - Show Build button if slot empty or different building
    - Show Upgrade button if same building and not max level
    - Show Demolish button if slot occupied
    - Enable/disable buttons based on requirement validation
    - _Requirements: 15.6, 15.7, 10.8_
  
  - [x] 8.6 Implement action handlers
    - Implement OnBuildClicked() to construct building (with demolish confirmation if needed)
    - Implement OnUpgradeClicked() to upgrade building and refresh UI
    - Implement OnDemolishClicked() to show confirmation dialog
    - Implement ShowConfirmation() to display confirmation dialog
    - Implement OnConfirmYes() and OnConfirmNo() for confirmation handling
    - Close menu after successful action
    - _Requirements: 15.8, 15.9, 11.2_

- [x] 9. Implement HomesteadTeleportItem
  - [x] 9.1 Create HomesteadTeleportItem.cs inventory item
    - Inherit from InventoryItem
    - Add serialized fields: homesteadSceneName, cooldownSeconds
    - Implement Use() method to check cooldown and load homestead scene
    - Implement SaveReturnLocation() to store current scene in PlayerPrefs
    - Implement static ReturnToPreviousLocation() to load return scene
    - Add CreateAssetMenu attribute
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.7_

- [x] 10. Create example BuildingData assets
  - [x] 10.1 Create example building: Basic House
    - Create BuildingData asset: `Assets/Internal Assets/Homestead/Buildings/House_Basic.asset`
    - Set buildingId: "house_basic"
    - Set displayName: "Basic House"
    - Set description: "A simple dwelling for rest and storage"
    - Set maxLevel: 3
    - Create placeholder prefabs for levels 1-3 (simple cubes with different sizes)
    - Create InventoryRequirement assets for each level (wood, stone)
    - Create CurrencyRequirement assets for each level (100, 250, 500 gold)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_
  
  - [x] 10.2 Create example building: Workshop
    - Create BuildingData asset: `Assets/Internal Assets/Homestead/Buildings/Workshop.asset`
    - Set buildingId: "workshop"
    - Set displayName: "Workshop"
    - Set description: "Craft items and process resources"
    - Set maxLevel: 2
    - Create placeholder prefabs for levels 1-2
    - Create requirements including PrerequisiteBuildingRequirement (requires Basic House)
    - _Requirements: 3.1, 7.1, 7.2, 7.3_
  
  - [x] 10.3 Create example building: Storage Shed
    - Create BuildingData asset: `Assets/Internal Assets/Homestead/Buildings/Storage.asset`
    - Set buildingId: "storage"
    - Set displayName: "Storage Shed"
    - Set description: "Store extra items and resources"
    - Set maxLevel: 1
    - Set maxInstances: 3 (test building limit feature)
    - Create placeholder prefab
    - Create simple requirements (wood, gold)
    - _Requirements: 3.1, 21.1, 21.2, 21.3, 21.4_

- [x] 11. Create homestead scene and integrate components
  - [x] 11.1 Create homestead scene
    - Create new scene: `Assets/Internal Assets/Homestead/Scenes/Homestead.unity`
    - Add terrain or ground plane
    - Add lighting and skybox
    - _Requirements: 1.1, 1.2_
  
  - [x] 11.2 Setup EstateManager in scene
    - Create empty GameObject named "EstateManager"
    - Add EstateManager component
    - Set homesteadIdentifier: "main_homestead"
    - Create empty GameObject named "Buildings" as parent for instantiated buildings
    - Assign buildingsParent reference
    - _Requirements: 1.2, 20.1_
  
  - [x] 11.3 Create building slots in scene
    - Create 5 empty GameObjects named "BuildingSlot_1" through "BuildingSlot_5"
    - Position them in a logical layout on the terrain
    - Add BuildingSlot component to each
    - Assign unique slotIds (or leave empty for auto-generation)
    - Assign allowedBuildings arrays (mix of house, workshop, storage)
    - Set interactionRadius: 3.0
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  
  - [x] 11.4 Create ConstructionMenuUI prefab
    - Create Canvas with ConstructionMenuUI component
    - Create menu panel with building list (ScrollView)
    - Create building details panel with name, description, icon, level text
    - Create requirements container (ScrollView)
    - Create action buttons: Build, Upgrade, Demolish, Close
    - Create confirmation dialog panel with Yes/No buttons
    - Wire all UI references in ConstructionMenuUI component
    - Save as prefab: `Assets/Internal Assets/Homestead/Prefabs/UI/ConstructionMenu.prefab`
    - Add prefab instance to Homestead scene
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6_
  
  - [x] 11.5 Create UI element prefabs
    - Create BuildingButton prefab with Button, TextMeshProUGUI, Image components
    - Save as: `Assets/Internal Assets/Homestead/Prefabs/UI/BuildingButton.prefab`
    - Create RequirementItem prefab with TextMeshProUGUI component
    - Save as: `Assets/Internal Assets/Homestead/Prefabs/UI/RequirementItem.prefab`
    - Assign prefab references in ConstructionMenuUI
    - _Requirements: 15.3_

- [x] 12. Create HomesteadTeleportItem asset and test integration
  - [x] 12.1 Create teleport item asset
    - Create HomesteadTeleportItem asset: `Assets/Internal Assets/Homestead/HomesteadTeleport.asset`
    - Set homesteadSceneName: "Homestead"
    - Set cooldownSeconds: 5.0
    - Set display name, description, icon
    - _Requirements: 16.1, 16.2, 16.3_
  
  - [x] 12.2 Test teleport functionality
    - Add HomesteadTeleport item to player inventory in a test scene
    - Use item and verify scene loads correctly
    - Verify return location is saved
    - Test cooldown timer
    - _Requirements: 16.4, 16.5, 16.6, 16.7_

- [x] 13. Integration testing and bug fixes
  - [x] 13.1 Test construction flow
    - Test building construction on empty slot
    - Verify resources are deducted correctly
    - Verify building prefab instantiates at correct position
    - Verify building is added to unlocked list
    - Verify autosave triggers
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8_
  
  - [x] 13.2 Test upgrade flow
    - Test building upgrade with sufficient resources
    - Verify old prefab is destroyed and new prefab instantiates
    - Verify level increments correctly
    - Test upgrade at max level (should be disabled)
    - Verify autosave triggers
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8_
  
  - [x] 13.3 Test demolition flow
    - Test building demolition
    - Verify building remains in unlocked list
    - Verify no resource refund
    - Verify autosave triggers
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7_
  
  - [x] 13.4 Test free reconstruction
    - Demolish a building
    - Reconstruct the same building
    - Verify resources are not deducted
    - Verify [FREE] indicator appears in UI
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_
  
  - [x] 13.5 Test requirement validation
    - Test each requirement type (inventory, currency, quest, level, prerequisite)
    - Test logical requirements (AND, OR)
    - Verify UI displays requirement status correctly
    - Test with insufficient resources
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_
  
  - [x] 13.6 Test save/load system
    - Construct multiple buildings
    - Save game
    - Reload scene
    - Verify all buildings restore correctly
    - Verify unlocked buildings list persists
    - Test with corrupted save data (should initialize empty state)
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 14.8, 14.9, 25.3_
  
  - [x] 13.7 Test building limits
    - Test maxInstances limit (Storage Shed limited to 3)
    - Verify construction is blocked when limit reached
    - Verify UI displays count/max
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.5_
  
  - [x] 13.8 Test error handling
    - Test with null BuildingData references
    - Test with missing prefabs
    - Test with missing components (Inventory, Purse, etc.)
    - Verify appropriate error messages are logged
    - Verify system doesn't crash
    - _Requirements: 25.1, 25.2, 25.3, 25.4, 25.5_

- [x] 14. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 15. Documentation and polish
  - [x] 15.1 Add XML documentation comments
    - Add XML comments to all public methods in EstateManager
    - Add XML comments to all public methods in BuildingSlot
    - Add XML comments to all BuildingRequirement subclasses
    - Add XML comments to ConstructionMenuUI public methods
    - _Requirements: 25.5_
  
  - [x] 15.2 Create README for designers
    - Document how to create new BuildingData assets
    - Document how to create requirement assets
    - Document how to setup building slots in scenes
    - Document building prefab requirements
    - Include example configurations
    - Save as: `Assets/Internal Assets/Homestead/README.md`
    - _Requirements: 3.1, 17.4_

## Notes

- All scripts use the `RPG.Homestead` namespace
- The system integrates with existing RPG systems without modifying them
- BuildingData and BuildingRequirement are ScriptableObjects for designer-friendly configuration
- EstateManager implements ISaveable for automatic save/load integration
- The system supports extensibility through abstract BuildingRequirement base class
- UI uses TextMeshPro for text rendering
- Building prefabs can contain any components (CraftingStation, ProcessingStation, ChestInventory, NPCs, etc.)
- Free reconstruction allows players to experiment with layouts without penalty
- Autosave triggers after all state-changing operations (construct, upgrade, demolish)
- Error handling ensures graceful degradation when components or data are missing
