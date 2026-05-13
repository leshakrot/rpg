# Task 13: Integration Testing Instructions

## Overview

This document provides comprehensive instructions for executing Task 13: Integration testing and bug fixes for the Player Homestead System.

## Test Setup

### Prerequisites

1. **Homestead Scene**: Ensure the Homestead scene exists (created in Task 11)
2. **Building Data Assets**: Ensure example buildings exist (created in Task 10):
   - Basic House (house_basic)
   - Workshop (workshop)
   - Storage Shed (storage) - with maxInstances = 3
3. **Player Character**: Scene must have a player with:
   - Inventory component
   - Purse component
   - BaseStats component (optional)
   - QuestList component (optional)
4. **EstateManager**: Homestead scene must have EstateManager singleton
5. **Building Slots**: At least 5 building slots configured in the scene

### Test Tool Setup

1. **Create Test GameObject**:
   - In Homestead scene, create empty GameObject named "IntegrationTester"
   - Add `HomesteadIntegrationTester` component

2. **Configure Test Tool**:
   - **testBuilding1**: Assign Basic House BuildingData
   - **testBuilding2**: Assign Workshop BuildingData
   - **testBuilding3**: Assign Storage Shed BuildingData
   - **testSlot**: Assign any BuildingSlot from the scene
   - **testItem**: Assign an InventoryItem (e.g., Wood, Stone)
   - **testItemQuantity**: Set to 10
   - **testCurrency**: Set to 1000

3. **Enter Play Mode**

## Test Execution

### Option 1: Run All Tests (Recommended)

1. Select IntegrationTester GameObject in Hierarchy
2. Right-click on HomesteadIntegrationTester component
3. Select **"Run All Integration Tests"**
4. Wait for all tests to complete (approximately 10-15 seconds)
5. Check Console for results

### Option 2: Run Individual Tests

Execute tests one at a time via context menu:

#### Test 13.1: Construction Flow
- Right-click → **"Test 13.1: Construction Flow"**
- **Verifies**:
  - Building construction on empty slot
  - Resources deducted correctly
  - Prefab instantiation at correct position
  - Building added to unlocked list
  - Autosave triggered

#### Test 13.2: Upgrade Flow
- Right-click → **"Test 13.2: Upgrade Flow"**
- **Verifies**:
  - Building upgrade with sufficient resources
  - Old prefab destroyed, new prefab instantiated
  - Level increments correctly
  - Max level handling (upgrade disabled)
  - Autosave triggered

#### Test 13.3: Demolition Flow
- Right-click → **"Test 13.3: Demolition Flow"**
- **Verifies**:
  - Building demolition
  - Building remains in unlocked list
  - No resource refund
  - Slot cleared correctly
  - Autosave triggered

#### Test 13.4: Free Reconstruction
- Right-click → **"Test 13.4: Free Reconstruction"**
- **Verifies**:
  - Demolish and reconstruct building
  - Resources NOT deducted
  - Building constructed at level 1
  - [FREE] indicator in UI (manual verification)

#### Test 13.5: Requirement Validation
- Right-click → **"Test 13.5: Requirement Validation"**
- **Verifies**:
  - Inventory requirement validation
  - Currency requirement validation
  - Level requirement validation (if applicable)
  - Prerequisite requirement validation (if applicable)
  - Logical requirements (AND/OR)
  - UI displays requirement status correctly
  - Construction blocked with insufficient resources

#### Test 13.6: Save/Load System
- Right-click → **"Test 13.6: Save/Load System"**
- **Verifies**:
  - State capture (CaptureState)
  - State restoration (RestoreState)
  - Buildings restore correctly
  - Unlocked buildings list persists
  - Corrupted save data handling (null state)

#### Test 13.7: Building Limits
- Right-click → **"Test 13.7: Building Limits"**
- **Verifies**:
  - maxInstances limit enforced (Storage Shed limited to 3)
  - Construction blocked when limit reached
  - Building count tracked correctly
  - UI displays count/max (manual verification)

#### Test 13.8: Error Handling
- Right-click → **"Test 13.8: Error Handling"**
- **Verifies**:
  - Null BuildingData handling
  - Missing prefab handling
  - Missing component handling (Inventory, Purse, etc.)
  - Appropriate error messages logged
  - System doesn't crash

## Manual Verification Tests

Some aspects require manual verification in addition to automated tests:

### UI Verification

1. **Construction Menu Display**:
   - Open construction menu on a building slot
   - Verify building list displays correctly
   - Verify [FREE] indicator for unlocked buildings
   - Verify building details (name, description, icon, level)

2. **Requirements Display**:
   - Verify requirements list shows all requirements
   - Verify ✓/✗ indicators for met/unmet requirements
   - Verify color coding (green for met, red for unmet, cyan for free)
   - Verify missing info displayed for unmet requirements

3. **Action Buttons**:
   - Verify Build button shows for empty slots
   - Verify Upgrade button shows for occupied slots (not max level)
   - Verify Demolish button shows for occupied slots
   - Verify buttons enabled/disabled based on requirements

4. **Confirmation Dialog**:
   - Verify demolition shows confirmation dialog
   - Verify building replacement shows confirmation dialog
   - Verify Yes/No buttons work correctly

### Gameplay Flow Verification

1. **Complete Construction Flow**:
   - Start with empty slot
   - Open construction menu
   - Select building
   - Verify requirements displayed
   - Click Build button
   - Verify building appears in scene
   - Verify menu closes

2. **Complete Upgrade Flow**:
   - Select occupied slot with building
   - Open construction menu
   - Verify current level displayed
   - Click Upgrade button
   - Verify building visually changes (if prefabs differ)
   - Verify level text updates

3. **Complete Demolition Flow**:
   - Select occupied slot
   - Open construction menu
   - Click Demolish button
   - Verify confirmation dialog appears
   - Click Yes
   - Verify building disappears
   - Verify slot is empty

4. **Complete Free Reconstruction Flow**:
   - Demolish a building
   - Open construction menu on empty slot
   - Select demolished building
   - Verify [FREE] indicator in building list
   - Verify "FREE RECONSTRUCTION" message in requirements
   - Click Build button
   - Verify building appears without resource deduction

## Expected Results

### Test 13.1: Construction Flow
- ✅ Construction succeeds
- ✅ Resources deducted from inventory/purse
- ✅ Building prefab instantiated at slot position
- ✅ Building added to unlocked list
- ✅ Autosave triggered (check console for "Save" message)

### Test 13.2: Upgrade Flow
- ✅ Upgrade succeeds
- ✅ Level increments by 1
- ✅ Old prefab destroyed, new prefab instantiated
- ✅ Max level upgrade blocked
- ✅ Autosave triggered

### Test 13.3: Demolition Flow
- ✅ Demolition succeeds
- ✅ Slot becomes empty
- ✅ Building remains in unlocked list
- ✅ No resource refund
- ✅ Autosave triggered

### Test 13.4: Free Reconstruction
- ✅ Reconstruction succeeds
- ✅ Resources NOT deducted
- ✅ Building constructed at level 1
- ✅ [FREE] indicator visible in UI

### Test 13.5: Requirement Validation
- ✅ All requirement types validated correctly
- ✅ Logical requirements (AND/OR) work correctly
- ✅ UI displays requirement status accurately
- ✅ Construction blocked with insufficient resources

### Test 13.6: Save/Load System
- ✅ State captured successfully
- ✅ Buildings restored after reload
- ✅ Unlocked buildings list persists
- ✅ Null state handled gracefully (no crash)

### Test 13.7: Building Limits
- ✅ maxInstances limit enforced
- ✅ Construction blocked at limit
- ✅ Building count tracked correctly
- ✅ UI displays count/max

### Test 13.8: Error Handling
- ✅ Null BuildingData handled (error logged, no crash)
- ✅ Missing prefab handled (error logged, no crash)
- ✅ Missing components handled (error logged, no crash)
- ✅ Appropriate error messages in console
- ✅ System remains stable

## Bug Reporting

If any test fails, document the following:

1. **Test Name**: Which test failed
2. **Expected Behavior**: What should have happened
3. **Actual Behavior**: What actually happened
4. **Steps to Reproduce**: How to reproduce the issue
5. **Console Errors**: Any error messages in console
6. **Screenshots**: Visual evidence if applicable

## Common Issues and Solutions

### Issue: "Player inventory not found"
**Solution**: Ensure player GameObject is tagged as "Player" and has Inventory component

### Issue: "EstateManager not found"
**Solution**: Ensure Homestead scene has EstateManager GameObject with EstateManager component

### Issue: "BuildingData not assigned"
**Solution**: Assign BuildingData assets to test tool in Inspector

### Issue: "No building slots found"
**Solution**: Ensure Homestead scene has BuildingSlot GameObjects with BuildingSlot components

### Issue: "Resources not deducted"
**Solution**: Verify BuildingData has requirements configured (InventoryRequirement, CurrencyRequirement)

### Issue: "Autosave not triggered"
**Solution**: Ensure scene has SavingSystem component (optional, system works without it)

### Issue: "Building prefab not instantiated"
**Solution**: Verify BuildingData has prefabs assigned for each level

## Performance Testing

After all functional tests pass, perform performance testing:

1. **Construct 10+ buildings**: Verify no performance degradation
2. **Rapid construction/demolition**: Verify no memory leaks
3. **Save/Load with many buildings**: Verify save/load performance
4. **UI responsiveness**: Verify menu opens/closes quickly

## Completion Criteria

Task 13 is complete when:

- ✅ All 8 automated tests pass
- ✅ All manual verification tests pass
- ✅ No console errors during normal operation
- ✅ UI displays correctly and responsively
- ✅ All gameplay flows work as expected
- ✅ Error handling works gracefully
- ✅ Performance is acceptable
- ✅ Any bugs found are documented and fixed

## Next Steps

After Task 13 completion:

1. Document any bugs found and fixed
2. Update requirements/design documents if needed
3. Proceed to Task 14: Final checkpoint
4. Proceed to Task 15: Documentation and polish

## Support

For issues or questions:
1. Check console for error messages
2. Verify all prerequisites are met
3. Review test setup configuration
4. Check that all previous tasks (1-12) are complete
