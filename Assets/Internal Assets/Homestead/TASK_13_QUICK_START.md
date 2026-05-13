# Task 13: Integration Testing - Quick Start Guide

## Quick Setup (5 Minutes)

### Step 1: Create Example Buildings (if not already done)
1. In Unity Editor, go to **Tools → Homestead → Create Example Buildings**
2. Click **Yes** to create:
   - Basic House (3 levels)
   - Workshop (2 levels)
   - Storage Shed (1 level, max 3 instances)
3. Wait for success message

### Step 2: Setup Homestead Scene (if not already done)
1. Go to **Homestead → Setup Homestead Scene**
2. Click **Yes** to create the scene
3. Wait for success message
4. Open the Homestead scene

### Step 3: Setup Integration Tester
1. In Homestead scene, create empty GameObject named "IntegrationTester"
2. Add `HomesteadIntegrationTester` component
3. Configure in Inspector:
   - **testBuilding1**: Drag `House_Basic.asset`
   - **testBuilding2**: Drag `Workshop.asset`
   - **testBuilding3**: Drag `Storage.asset`
   - **testSlot**: Drag any BuildingSlot from Hierarchy
   - **testItem**: Drag `WoodItem T1.asset` (from Assets/Internal Assets/Harvesting/Trees/Resources/)
   - **testItemQuantity**: 10
   - **testCurrency**: 1000

### Step 4: Verify Player Setup
1. Ensure scene has Player GameObject with:
   - Inventory component
   - Purse component
   - BaseStats component (optional)
   - QuestList component (optional)
2. If missing, add a player prefab to the scene

### Step 5: Run Tests
1. Enter Play Mode
2. Select IntegrationTester in Hierarchy
3. Right-click on HomesteadIntegrationTester component
4. Select **"Run All Integration Tests"**
5. Wait 10-15 seconds for completion
6. Check Console for results

## Expected Console Output

```
[Homestead Test] Starting Test 13.1: Construction Flow
[Test 13.1: Construction Flow] PASSED
Construction: True, Resources Deducted: True, Prefab Instantiated: True, Building Unlocked: True

[Homestead Test] Starting Test 13.2: Upgrade Flow
[Test 13.2: Upgrade Flow] PASSED
Upgrade Success: True, Level Incremented: True, Prefab Replaced: True, Max Level Handled: True

[Homestead Test] Starting Test 13.3: Demolition Flow
[Test 13.3: Demolition Flow] PASSED
Demolish Success: True, Slot Empty: True, Still Unlocked: True, No Refund: True

[Homestead Test] Starting Test 13.4: Free Reconstruction
[Test 13.4: Free Reconstruction] PASSED
Reconstruction Success: True, No Resources Deducted: True, Constructed at Level 1: True

[Homestead Test] Starting Test 13.5: Requirement Validation
[Test 13.5: Requirement Validation] PASSED
Inventory: True, Currency: True, Level: True, Prerequisite: True, Logical: True

[Homestead Test] Starting Test 13.6: Save/Load System
[Test 13.6: Save/Load System] PASSED
State Captured: True, Building Restored: True, Handled Null State: True

[Homestead Test] Starting Test 13.7: Building Limits
[Test 13.7: Building Limits] PASSED
Max Instances: 3, Current Count: 3, Count Correct: True

[Homestead Test] Starting Test 13.8: Error Handling
[Test 13.8: Error Handling] PASSED
Null Data: True, Missing Prefab: True, Missing Components: True, No Crashes: True

========== ALL INTEGRATION TESTS COMPLETE ==========
```

## Manual UI Testing (5 Minutes)

After automated tests pass, test the UI manually:

### Test 1: Open Construction Menu
1. In Play Mode, approach a BuildingSlot (within 3 units)
2. Click on the slot
3. **Verify**: Construction menu opens

### Test 2: View Building List
1. **Verify**: Building list shows all allowed buildings
2. **Verify**: [FREE] indicator for unlocked buildings
3. Click on different buildings
4. **Verify**: Details panel updates

### Test 3: View Requirements
1. Select a building
2. **Verify**: Requirements list shows all requirements
3. **Verify**: ✓ for met requirements (green)
4. **Verify**: ✗ for unmet requirements (red)
5. **Verify**: Missing info displayed for unmet requirements

### Test 4: Build a Building
1. Select a building with met requirements
2. Click **Build** button
3. **Verify**: Building appears in scene
4. **Verify**: Menu closes

### Test 5: Upgrade a Building
1. Click on occupied slot
2. **Verify**: Current level displayed
3. Click **Upgrade** button
4. **Verify**: Building visually changes (if prefabs differ)
5. **Verify**: Level text updates

### Test 6: Demolish a Building
1. Click on occupied slot
2. Click **Demolish** button
3. **Verify**: Confirmation dialog appears
4. Click **Yes**
5. **Verify**: Building disappears

### Test 7: Free Reconstruction
1. Click on empty slot where building was demolished
2. Select the demolished building
3. **Verify**: [FREE] indicator in building list
4. **Verify**: "FREE RECONSTRUCTION" message in requirements
5. Click **Build** button
6. **Verify**: Building appears without resource deduction

## Troubleshooting

### "Player inventory not found"
- Ensure Player GameObject is tagged as "Player"
- Add Inventory component to Player

### "EstateManager not found"
- Run **Homestead → Setup Homestead Scene**
- Or manually add EstateManager to scene

### "BuildingData not assigned"
- Run **Tools → Homestead → Create Example Buildings**
- Assign assets in IntegrationTester Inspector

### "No building slots found"
- Run **Homestead → Setup Homestead Scene**
- Or manually add BuildingSlot GameObjects

### Tests fail with "Resources not deducted"
- Verify BuildingData has requirements configured
- Check that Inventory and Purse components work

## Success Criteria

✅ All 8 automated tests pass
✅ All 7 manual UI tests pass
✅ No console errors
✅ UI displays correctly
✅ Gameplay flows work smoothly

## Next Steps

After all tests pass:
1. Document any bugs found (if any)
2. Mark Task 13 as complete
3. Proceed to Task 14: Final checkpoint
4. Proceed to Task 15: Documentation and polish

## Time Estimate

- Setup: 5 minutes
- Automated tests: 2 minutes
- Manual UI tests: 5 minutes
- **Total: ~12 minutes**

## Support Files

- **Test Script**: `Assets/Scripts/Homestead/Testing/HomesteadIntegrationTester.cs`
- **Detailed Instructions**: `Assets/Internal Assets/Homestead/TASK_13_TEST_INSTRUCTIONS.md`
- **Building Creator**: `Assets/Scripts/Homestead/Editor/BuildingDataCreator.cs`
- **Scene Setup**: `Assets/Scripts/Homestead/Editor/HomesteadSceneSetup.cs`
