# Execute Task 13: Integration Testing

## Ready to Execute

Task 13 integration testing framework is now ready. Follow these steps to execute all tests.

## Prerequisites Check

Before starting, verify:
- ✅ Unity Editor is open
- ✅ Project compiles without errors
- ✅ Tasks 1-12 are complete

## Execution Steps

### Step 1: Create Example Buildings (2 minutes)

If not already done:

1. In Unity Editor menu: **Tools → Homestead → Create Example Buildings**
2. Click **Yes** in confirmation dialog
3. Wait for success message: "Example buildings created successfully!"
4. Verify assets created in: `Assets/Internal Assets/Homestead/Buildings/`
   - House_Basic.asset
   - Workshop.asset
   - Storage.asset

### Step 2: Setup Homestead Scene (2 minutes)

If not already done:

1. In Unity Editor menu: **Homestead → Setup Homestead Scene**
2. Click **Yes** in confirmation dialog
3. Wait for success message
4. Open the Homestead scene from: `Assets/Internal Assets/Homestead/Scenes/Homestead.unity`

### Step 3: Setup Integration Tester (3 minutes)

1. In Homestead scene Hierarchy, create empty GameObject:
   - Right-click in Hierarchy → Create Empty
   - Name it: "IntegrationTester"

2. Add component:
   - Select IntegrationTester GameObject
   - In Inspector, click **Add Component**
   - Search for: "HomesteadIntegrationTester"
   - Click to add

3. Configure test tool in Inspector:
   - **testBuilding1**: 
     - Click circle icon → Select `House_Basic`
   - **testBuilding2**: 
     - Click circle icon → Select `Workshop`
   - **testBuilding3**: 
     - Click circle icon → Select `Storage`
   - **testSlot**: 
     - Drag any `BuildingSlot_X` from Hierarchy
   - **testItem**: 
     - Navigate to: `Assets/Internal Assets/Harvesting/Trees/Resources/`
     - Select `WoodItem T1`
   - **testItemQuantity**: 
     - Set to: `10`
   - **testCurrency**: 
     - Set to: `1000`

4. Save scene: **Ctrl+S** (Cmd+S on Mac)

### Step 4: Verify Player Setup (1 minute)

1. Check if scene has Player GameObject:
   - Look for "Player" in Hierarchy
   - If missing, you'll need to add a player prefab

2. Verify Player has required components:
   - Select Player in Hierarchy
   - Check Inspector for:
     - ✅ Inventory component
     - ✅ Purse component
     - ✅ BaseStats component (optional)
     - ✅ QuestList component (optional)

3. If components are missing:
   - Add Inventory: **Add Component → Inventory**
   - Add Purse: **Add Component → Purse**
   - Set initial currency in Purse: `startingBalance = 1000`

### Step 5: Run Automated Tests (2 minutes)

1. **Enter Play Mode**: Click Play button or press **Ctrl+P** (Cmd+P on Mac)

2. **Select IntegrationTester**: Click on IntegrationTester in Hierarchy

3. **Open Context Menu**: 
   - Right-click on `HomesteadIntegrationTester` component in Inspector
   - OR click the three dots (⋮) in component header

4. **Run All Tests**: 
   - Select **"Run All Integration Tests"**

5. **Wait for completion**: 
   - Tests will run for 10-15 seconds
   - Watch Console for progress

6. **Check Results**: 
   - Open Console window: **Window → General → Console**
   - Look for test results:
     - Green = PASSED ✅
     - Red = FAILED ❌

### Step 6: Verify Test Results (1 minute)

Expected console output:

```
[Homestead Test] Starting Test 13.1: Construction Flow
[Test 13.1: Construction Flow] PASSED ✅

[Homestead Test] Starting Test 13.2: Upgrade Flow
[Test 13.2: Upgrade Flow] PASSED ✅

[Homestead Test] Starting Test 13.3: Demolition Flow
[Test 13.3: Demolition Flow] PASSED ✅

[Homestead Test] Starting Test 13.4: Free Reconstruction
[Test 13.4: Free Reconstruction] PASSED ✅

[Homestead Test] Starting Test 13.5: Requirement Validation
[Test 13.5: Requirement Validation] PASSED ✅

[Homestead Test] Starting Test 13.6: Save/Load System
[Test 13.6: Save/Load System] PASSED ✅

[Homestead Test] Starting Test 13.7: Building Limits
[Test 13.7: Building Limits] PASSED ✅

[Homestead Test] Starting Test 13.8: Error Handling
[Test 13.8: Error Handling] PASSED ✅

========== ALL INTEGRATION TESTS COMPLETE ==========
```

### Step 7: Manual UI Testing (5 minutes)

While still in Play Mode:

#### Test 1: Open Construction Menu
1. Move player close to a BuildingSlot (within 3 units)
2. Click on the slot
3. ✅ Verify: Construction menu opens

#### Test 2: View Building List
1. ✅ Verify: Building list shows all allowed buildings
2. ✅ Verify: [FREE] indicator for unlocked buildings (if any)
3. Click on different buildings
4. ✅ Verify: Details panel updates

#### Test 3: View Requirements
1. Select a building
2. ✅ Verify: Requirements list shows all requirements
3. ✅ Verify: ✓ for met requirements (green)
4. ✅ Verify: ✗ for unmet requirements (red)

#### Test 4: Build a Building
1. Select a building with met requirements
2. Click **Build** button
3. ✅ Verify: Building appears in scene
4. ✅ Verify: Menu closes

#### Test 5: Upgrade a Building
1. Click on occupied slot
2. ✅ Verify: Current level displayed
3. Click **Upgrade** button (if available)
4. ✅ Verify: Building visually changes
5. ✅ Verify: Level text updates

#### Test 6: Demolish a Building
1. Click on occupied slot
2. Click **Demolish** button
3. ✅ Verify: Confirmation dialog appears
4. Click **Yes**
5. ✅ Verify: Building disappears

#### Test 7: Free Reconstruction
1. Click on empty slot where building was demolished
2. Select the demolished building
3. ✅ Verify: [FREE] indicator in building list
4. ✅ Verify: "FREE RECONSTRUCTION" message
5. Click **Build** button
6. ✅ Verify: Building appears without resource deduction

### Step 8: Exit Play Mode

1. Click Play button again or press **Ctrl+P** (Cmd+P on Mac)
2. Save scene if prompted

## Success Criteria

Task 13 is complete when:

- ✅ All 8 automated tests pass (green in console)
- ✅ All 7 manual UI tests pass
- ✅ No console errors during normal operation
- ✅ UI displays correctly and responsively
- ✅ All gameplay flows work as expected

## If Tests Fail

### Automated Test Failures

If any automated test fails (red in console):

1. **Read the error message**: Console shows which aspect failed
2. **Check test details**: Expand console message for details
3. **Verify setup**: Ensure all prerequisites are met
4. **Run individual test**: Right-click component → Select specific test
5. **Check components**: Verify Player has Inventory and Purse
6. **Check assets**: Verify BuildingData assets exist and are configured

### Common Issues

**"Player inventory not found"**
- Solution: Add Inventory component to Player GameObject
- Ensure Player is tagged as "Player"

**"EstateManager not found"**
- Solution: Run **Homestead → Setup Homestead Scene**
- Or manually add EstateManager to scene

**"BuildingData not assigned"**
- Solution: Assign BuildingData assets in IntegrationTester Inspector
- Run **Tools → Homestead → Create Example Buildings** if assets missing

**"No building slots found"**
- Solution: Run **Homestead → Setup Homestead Scene**
- Or manually add BuildingSlot GameObjects to scene

**"Resources not deducted"**
- Solution: Verify BuildingData has requirements configured
- Check InventoryRequirement and CurrencyRequirement assets exist

### Manual UI Test Failures

If UI doesn't work as expected:

1. **Check ConstructionMenuUI**: Verify it exists in scene
2. **Check UI references**: Verify all UI references are wired in Inspector
3. **Check BuildingSlot**: Verify BuildingSlot has IRaycastable interface
4. **Check Player**: Verify Player can interact with objects
5. **Check distance**: Ensure Player is within interactionRadius (3 units)

## Reporting Results

After completing all tests, report:

### If All Tests Pass ✅
- Task 13 is complete
- System is fully functional
- Ready for Task 14 (Final checkpoint)

### If Any Tests Fail ❌
- Document which tests failed
- Document error messages
- Document steps to reproduce
- Request assistance or investigate further

## Time Estimate

- Step 1 (Create Buildings): 2 minutes
- Step 2 (Setup Scene): 2 minutes
- Step 3 (Setup Tester): 3 minutes
- Step 4 (Verify Player): 1 minute
- Step 5 (Run Tests): 2 minutes
- Step 6 (Verify Results): 1 minute
- Step 7 (Manual UI Tests): 5 minutes
- **Total: ~16 minutes**

## Support Files

- **Test Script**: `Assets/Scripts/Homestead/Testing/HomesteadIntegrationTester.cs`
- **Quick Start**: `Assets/Internal Assets/Homestead/TASK_13_QUICK_START.md`
- **Detailed Instructions**: `Assets/Internal Assets/Homestead/TASK_13_TEST_INSTRUCTIONS.md`
- **Completion Summary**: `Assets/Internal Assets/Homestead/TASK_13_COMPLETION_SUMMARY.md`

## Next Steps

After Task 13 completion:

1. ✅ Mark Task 13 as complete in tasks.md
2. ⏭️ Proceed to Task 14: Final checkpoint
3. ⏭️ Proceed to Task 15: Documentation and polish

## Questions?

If you encounter issues:
1. Check troubleshooting sections in support files
2. Review console error messages
3. Verify all prerequisites are met
4. Check that Tasks 1-12 are complete

---

**Ready to begin? Start with Step 1!**
