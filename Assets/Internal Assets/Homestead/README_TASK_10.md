# Task 10: Create Example BuildingData Assets

## Overview
This task creates example BuildingData assets for testing the Player Homestead System.

## Automated Creation (Recommended)

An editor utility script has been created to automatically generate all example buildings.

### Steps:
1. Open Unity Editor
2. Go to menu: **Tools > Homestead > Create Example Buildings**
3. Click "Yes" in the confirmation dialog
4. Wait for the success message

This will create:
- **Basic House** (3 levels) at `Assets/Internal Assets/Homestead/Buildings/House_Basic.asset`
- **Workshop** (2 levels) at `Assets/Internal Assets/Homestead/Buildings/Workshop.asset`
- **Storage Shed** (1 level, max 3 instances) at `Assets/Internal Assets/Homestead/Buildings/Storage.asset`

All required prefabs and requirement assets will be created automatically.

## What Gets Created

### Task 10.1: Basic House
- **Building ID**: `house_basic`
- **Display Name**: Basic House
- **Description**: A simple dwelling for rest and storage
- **Max Level**: 3
- **Category**: Residential
- **Prefabs**: 
  - Level 1: 2x2x2 cube (brown)
  - Level 2: 3x3x3 cube (darker brown)
  - Level 3: 4x4x4 cube (darkest brown)
- **Requirements**:
  - Level 1: 10x Wood, 100 Gold
  - Level 2: 15x Stone, 250 Gold
  - Level 3: 20x Wood, 500 Gold

### Task 10.2: Workshop
- **Building ID**: `workshop`
- **Display Name**: Workshop
- **Description**: Craft items and process resources
- **Max Level**: 2
- **Category**: Production
- **Prefabs**:
  - Level 1: 3x2.5x3 cube (gray-brown)
  - Level 2: 4x3x4 cube (darker gray-brown)
- **Requirements**:
  - Level 1: 15x Wood, 200 Gold, Requires Basic House (Level 1+)
  - Level 2: 20x Stone, 400 Gold

### Task 10.3: Storage Shed
- **Building ID**: `storage`
- **Display Name**: Storage Shed
- **Description**: Store extra items and resources
- **Max Level**: 1
- **Max Instances**: 3 (tests building limit feature)
- **Category**: Storage
- **Prefabs**:
  - Level 1: 2x2x2 cube (tan)
- **Requirements**:
  - Level 1: 8x Wood, 50 Gold

## Manual Creation (Alternative)

If you prefer to create assets manually or need to customize them:

### 1. Create Building Prefabs
1. In Unity Hierarchy, create a Cube GameObject
2. Scale it to the desired size
3. Create a material and assign a color
4. Save as prefab in `Assets/Internal Assets/Homestead/Prefabs/`
5. Delete from scene

### 2. Create Requirement Assets
1. Right-click in Project window
2. Navigate to **Create > Homestead > Requirements**
3. Choose requirement type (Inventory, Currency, Prerequisite Building)
4. Configure the requirement fields in Inspector
5. Save in `Assets/Internal Assets/Homestead/Requirements/`

### 3. Create BuildingData Asset
1. Right-click in Project window
2. Navigate to **Create > Homestead > Building Data**
3. Configure all fields:
   - Building ID (unique string)
   - Display Name
   - Description
   - Max Level
   - Max Instances (-1 for unlimited)
   - Category
   - Level Prefabs array (assign prefabs for each level)
   - Level Requirements array (assign requirement arrays for each level)
4. Save in `Assets/Internal Assets/Homestead/Buildings/`

## Verification

After creation, verify the assets:

1. Check that all prefabs exist in `Assets/Internal Assets/Homestead/Prefabs/`
2. Check that all requirement assets exist in `Assets/Internal Assets/Homestead/Requirements/`
3. Check that all BuildingData assets exist in `Assets/Internal Assets/Homestead/Buildings/`
4. Open each BuildingData asset and verify:
   - All fields are populated
   - Level Prefabs array has correct number of elements
   - Level Requirements array has correct number of elements
   - Each requirement is properly configured

## Integration with Existing Systems

The example buildings use existing inventory items:
- **Wood**: `Assets/Internal Assets/Harvesting/Trees/Resources/WoodItem T1.asset`
- **Stone**: `Assets/Internal Assets/Harvesting/Stones/Resources/StoneItem.asset`

These items are already in the game and can be collected by the player.

## Next Steps

After completing this task:
1. Proceed to **Task 11**: Create homestead scene and integrate components
2. Test the buildings in the Construction Menu UI
3. Verify requirement validation works correctly
4. Test construction, upgrade, and demolition flows

## Troubleshooting

### "Could not find WoodItem T1 or StoneItem"
- Ensure the inventory item assets exist at the specified paths
- Check that the paths in the script match your project structure

### "Basic House not found" (for Workshop)
- Create Basic House first before creating Workshop
- Workshop has a prerequisite requirement for Basic House

### Prefabs not appearing in scene
- Check that prefabs were saved correctly
- Verify BuildingData references the correct prefabs
- Check console for any error messages

## Files Created

### Prefabs (9 files)
- `House_Basic_Level1.prefab` + material
- `House_Basic_Level2.prefab` + material
- `House_Basic_Level3.prefab` + material
- `Workshop_Level1.prefab` + material
- `Workshop_Level2.prefab` + material
- `Storage_Level1.prefab` + material

### Requirements (9 files)
- `House_Basic_L1_Inventory.asset`
- `House_Basic_L1_Currency.asset`
- `House_Basic_L2_Inventory.asset`
- `House_Basic_L2_Currency.asset`
- `House_Basic_L3_Inventory.asset`
- `House_Basic_L3_Currency.asset`
- `Workshop_L1_Inventory.asset`
- `Workshop_L1_Currency.asset`
- `Workshop_L1_Prerequisite.asset`
- `Workshop_L2_Inventory.asset`
- `Workshop_L2_Currency.asset`
- `Storage_L1_Inventory.asset`
- `Storage_L1_Currency.asset`

### BuildingData (3 files)
- `House_Basic.asset`
- `Workshop.asset`
- `Storage.asset`

**Total**: 21 asset files + 1 editor script
