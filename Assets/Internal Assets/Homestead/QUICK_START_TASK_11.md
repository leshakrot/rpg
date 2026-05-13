# Quick Start: Task 11 - Homestead Scene Setup

## TL;DR

Run this in Unity Editor:
```
Menu: Homestead → Setup Homestead Scene
```

Click "Yes" when prompted. Done! ✅

## What It Does

Creates the complete Homestead scene with:
- Ground, lighting, and environment
- EstateManager with proper configuration
- 5 building slots positioned in a grid
- Complete Construction Menu UI with all prefabs
- Everything wired and ready to use

## Expected Result

After running:
- New scene: `Assets/Internal Assets/Homestead/Scenes/Homestead.unity`
- 3 new UI prefabs in `Assets/Internal Assets/Homestead/Prefabs/UI/`
- Scene contains: EstateManager, 5 BuildingSlots, ConstructionMenuUI
- Console message: "Homestead scene created successfully"

## If Menu Item Doesn't Appear

1. Check Unity console for compilation errors
2. Restart Unity Editor
3. Reimport the script: Right-click `HomesteadSceneSetup.cs` → Reimport

## Known Issue

⚠️ If no BuildingData assets exist (Task 10 incomplete):
- Building slots will have empty `allowedBuildings` arrays
- You can manually assign BuildingData assets later in Inspector
- Or run Task 10 first, then re-run this setup

## Verification (30 seconds)

1. Open `Homestead.unity` scene
2. Check Hierarchy:
   - ✅ EstateManager (with Buildings child)
   - ✅ BuildingSlot_1 through BuildingSlot_5
   - ✅ ConstructionMenuCanvas
3. Select EstateManager:
   - ✅ buildingsParent is assigned
4. Select any BuildingSlot:
   - ✅ interactionRadius = 3.0

## Next Steps

1. ✅ Run the setup
2. ✅ Verify scene created
3. ⏭️ Move to Task 12 (Create HomesteadTeleportItem)

## Full Documentation

See `TASK_11_COMPLETION_SUMMARY.md` for complete details.
