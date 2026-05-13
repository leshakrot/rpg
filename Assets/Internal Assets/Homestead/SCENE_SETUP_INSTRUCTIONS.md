# Homestead Scene Setup Instructions

## Task 11: Create Homestead Scene and Integrate Components

This document provides instructions for setting up the Homestead scene programmatically.

## Automated Setup (Recommended)

An editor script has been created to automatically set up the entire Homestead scene with all required components.

### Steps:

1. **Open Unity Editor**

2. **Run the Setup Script**
   - In the Unity menu bar, go to: **Homestead → Setup Homestead Scene**
   - A confirmation dialog will appear
   - Click **Yes** to proceed

3. **What Gets Created:**
   - ✅ New scene: `Assets/Internal Assets/Homestead/Scenes/Homestead.unity`
   - ✅ Ground plane (100x100 units, green material)
   - ✅ Directional light configured
   - ✅ EstateManager GameObject with:
     - EstateManager component
     - homesteadIdentifier: "main_homestead"
     - Buildings parent GameObject
   - ✅ 5 BuildingSlot GameObjects positioned in a logical layout:
     - BuildingSlot_1 at (-20, 0, 20) - Top-left
     - BuildingSlot_2 at (0, 0, 20) - Top-center
     - BuildingSlot_3 at (20, 0, 20) - Top-right
     - BuildingSlot_4 at (-20, 0, 0) - Middle-left
     - BuildingSlot_5 at (20, 0, 0) - Middle-right
   - ✅ Each slot configured with:
     - BuildingSlot component
     - interactionRadius: 3.0
     - allowedBuildings array (slots 1-3: all buildings, slots 4-5: storage/workshop only)
   - ✅ UI Prefabs created:
     - BuildingButton.prefab
     - RequirementItem.prefab
     - ConstructionMenu.prefab (fully wired with all references)
   - ✅ ConstructionMenuUI instance added to scene

4. **Verify the Setup:**
   - Open the Homestead scene
   - Check that all GameObjects are present in the hierarchy
   - Select EstateManager and verify the buildingsParent reference is set
   - Select each BuildingSlot and verify:
     - interactionRadius is 3.0
     - allowedBuildings array is populated
   - Check that ConstructionMenuUI is in the scene with all references wired

## Manual Setup (If Automated Fails)

If the automated setup encounters issues, follow these manual steps:

### 11.1 Create Homestead Scene

1. Create new scene: File → New Scene
2. Add a Plane GameObject (GameObject → 3D Object → Plane)
   - Name: "Ground"
   - Scale: (10, 1, 10)
   - Material: Green-ish color
3. Configure Directional Light:
   - Rotation: (50, -30, 0)
   - Intensity: 1.0
4. Save scene as: `Assets/Internal Assets/Homestead/Scenes/Homestead.unity`

### 11.2 Setup EstateManager

1. Create empty GameObject: "EstateManager"
2. Add EstateManager component
3. Set homesteadIdentifier: "main_homestead"
4. Create child GameObject: "Buildings"
5. Assign Buildings transform to buildingsParent field

### 11.3 Create Building Slots

1. Create 5 empty GameObjects:
   - BuildingSlot_1 at position (-20, 0, 20)
   - BuildingSlot_2 at position (0, 0, 20)
   - BuildingSlot_3 at position (20, 0, 20)
   - BuildingSlot_4 at position (-20, 0, 0)
   - BuildingSlot_5 at position (20, 0, 0)

2. For each slot:
   - Add BuildingSlot component
   - Set interactionRadius: 3.0
   - Assign allowedBuildings:
     - Slots 1-3: All BuildingData assets from `Assets/Internal Assets/Homestead/Buildings/`
     - Slots 4-5: Only Storage and Workshop BuildingData assets

### 11.4 Create UI Element Prefabs

1. **BuildingButton Prefab:**
   - Create GameObject with Image (background)
   - Add Button component
   - Add child "Icon" with Image component (40x40)
   - Add child "Text" with TextMeshProUGUI component
   - Save as: `Assets/Internal Assets/Homestead/Prefabs/UI/BuildingButton.prefab`

2. **RequirementItem Prefab:**
   - Create GameObject with TextMeshProUGUI component
   - Set size: (300, 30)
   - Save as: `Assets/Internal Assets/Homestead/Prefabs/UI/RequirementItem.prefab`

### 11.5 Create ConstructionMenu Prefab

1. Create Canvas (Screen Space Overlay)
2. Add ConstructionMenuUI component
3. Create MenuPanel (covers 60% of screen)
4. Create Building List section (left 35%):
   - ScrollView with Vertical Layout Group
5. Create Details section (right 65%):
   - Building name, icon, level, description
   - Requirements ScrollView
   - Build, Upgrade, Demolish, Close buttons
6. Create Confirmation Dialog (hidden by default):
   - Background overlay
   - Panel with text and Yes/No buttons
7. Wire all references in ConstructionMenuUI component
8. Save as: `Assets/Internal Assets/Homestead/Prefabs/UI/ConstructionMenu.prefab`
9. Add prefab instance to Homestead scene

## Verification Checklist

After setup (automated or manual), verify:

- [ ] Homestead.unity scene exists in `Assets/Internal Assets/Homestead/Scenes/`
- [ ] Ground plane is visible
- [ ] Directional light is configured
- [ ] EstateManager exists with buildingsParent reference set
- [ ] 5 BuildingSlot GameObjects exist at correct positions
- [ ] Each BuildingSlot has allowedBuildings assigned
- [ ] BuildingButton.prefab exists
- [ ] RequirementItem.prefab exists
- [ ] ConstructionMenu.prefab exists with all references wired
- [ ] ConstructionMenuUI instance exists in scene
- [ ] No console errors

## Troubleshooting

**Issue: Menu item not appearing**
- Solution: Reimport the HomesteadSceneSetup.cs script
- Solution: Restart Unity Editor

**Issue: BuildingData assets not found**
- Solution: Ensure BuildingData assets exist in `Assets/Internal Assets/Homestead/Buildings/`
- Solution: Check that assets have correct type (BuildingData ScriptableObject)

**Issue: UI references not wired**
- Solution: Manually assign references in ConstructionMenuUI component
- Solution: Use the Inspector to drag and drop UI elements

**Issue: Scene not saving**
- Solution: Ensure `Assets/Internal Assets/Homestead/Scenes/` directory exists
- Solution: Check file permissions

## Next Steps

After completing this task:
1. Test the scene by entering Play mode
2. Verify that building slots are interactable
3. Test the Construction Menu UI
4. Proceed to Task 12: Create HomesteadTeleportItem asset

## Related Files

- Editor Script: `Assets/Scripts/Homestead/Editor/HomesteadSceneSetup.cs`
- EstateManager: `Assets/Scripts/Homestead/Core/EstateManager.cs`
- BuildingSlot: `Assets/Scripts/Homestead/Core/BuildingSlot.cs`
- ConstructionMenuUI: `Assets/Scripts/Homestead/UI/ConstructionMenuUI.cs`
- BuildingData assets: `Assets/Internal Assets/Homestead/Buildings/`

## Requirements Satisfied

This task satisfies the following requirements from the spec:

- **Requirement 1.1**: Homestead scene created
- **Requirement 1.2**: EstateManager singleton in scene
- **Requirement 2.1-2.7**: Building slots configured
- **Requirement 15.1-15.6**: Construction Menu UI created
- **Requirement 20.1**: EstateManager with homestead identifier
