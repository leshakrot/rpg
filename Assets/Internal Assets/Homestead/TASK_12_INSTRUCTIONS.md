# Task 12: Create HomesteadTeleportItem Asset and Test Integration

## Overview
This task creates the HomesteadTeleportItem asset and provides testing instructions to verify the teleport functionality works correctly.

## Subtask 12.1: Create Teleport Item Asset

### Automated Creation (Recommended)

An editor script has been created to automatically generate the HomesteadTeleport.asset file.

**Steps:**
1. Open Unity Editor
2. Go to menu: **Homestead → Create Teleport Item Asset**
3. Click **Yes** in the confirmation dialog
4. Wait for success message
5. The asset will be created at: `Assets/Internal Assets/Homestead/HomesteadTeleport.asset`

**Asset Configuration:**
- **Scene Name**: "Homestead"
- **Cooldown**: 5.0 seconds
- **Display Name**: "Homestead Teleport"
- **Description**: "A magical item that teleports you to your homestead. Use it to quickly travel home and return to your previous location."
- **Stackable**: No
- **Cannot Be Dropped**: Yes (important quest item)
- **Price**: 0 (not for sale)
- **Icon**: None (can be set manually in Inspector if desired)

### Manual Creation (Alternative)

If the menu item doesn't work:

1. In Unity Project window, navigate to: `Assets/Internal Assets/Homestead/`
2. Right-click → Create → Homestead → Teleport Item
3. Name it: `HomesteadTeleport`
4. Select the asset in Inspector
5. Configure the following fields:
   - **Homestead Scene Name**: "Homestead"
   - **Cooldown Seconds**: 5.0
   - **Display Name**: "Homestead Teleport"
   - **Description**: "A magical item that teleports you to your homestead. Use it to quickly travel home and return to your previous location."
   - **Cannot Be Dropped**: ✓ (checked)
   - **Stackable**: ☐ (unchecked)
   - **Price**: 0

## Subtask 12.2: Test Teleport Functionality

### Prerequisites

Before testing, ensure:
1. ✅ The Homestead scene exists at: `Assets/Internal Assets/Homestead/Scenes/Homestead.unity`
   - If not, run: **Homestead → Setup Homestead Scene** (Task 11)
2. ✅ The HomesteadTeleport.asset exists
3. ✅ You have a test scene with a player character (e.g., Sandbox, Village, etc.)
4. ✅ The player has an Inventory component
5. ✅ The game has a SavingWrapper and Fader in the scene

### Test Procedure

#### Test 1: Add Item to Inventory

**Manual Method:**
1. Open a test scene with the player
2. Select the Player GameObject in Hierarchy
3. Find the Inventory component in Inspector
4. Expand the inventory slots
5. Drag the HomesteadTeleport asset into an empty slot
6. Set the slot number to 1

**Script Method (Recommended):**
A test script will be provided below to automatically add the item.

#### Test 2: Verify Scene Loads Correctly

**Steps:**
1. Enter Play mode
2. Open the player inventory (press 'I' or configured key)
3. Use the Homestead Teleport item (click or press action key)
4. **Expected Results:**
   - ✅ Screen fades out (1 second)
   - ✅ Current scene is saved
   - ✅ Homestead scene loads
   - ✅ Screen fades in (2 seconds)
   - ✅ Player appears in Homestead scene
   - ✅ Player control is enabled
   - ✅ Console shows: "Saved return location: [PreviousSceneName]"

**Failure Cases:**
- ❌ If cooldown message appears: Wait 5 seconds and try again
- ❌ If "SavingWrapper not found" warning: Scene is missing SavingWrapper component
- ❌ If scene doesn't load: Check that Homestead scene exists and is named correctly

#### Test 3: Verify Return Location is Saved

**Steps:**
1. After teleporting to Homestead (from Test 2)
2. Open Unity Console (Ctrl+Shift+C)
3. Look for log message: "Saved return location: [SceneName]"
4. **Expected Results:**
   - ✅ Console shows the name of the scene you teleported FROM
   - ✅ PlayerPrefs key "HomesteadReturnScene" is set (can verify in PlayerPrefs editor)

**Verification:**
```csharp
// In Unity Console, you can check:
Debug.Log(PlayerPrefs.GetString("HomesteadReturnScene"));
// Should output the previous scene name
```

#### Test 4: Test Cooldown Timer

**Steps:**
1. In Play mode, use the Homestead Teleport item
2. Immediately try to use it again (within 5 seconds)
3. **Expected Results:**
   - ✅ Console shows: "Teleport on cooldown. Wait [X.X] seconds."
   - ✅ Item is NOT consumed
   - ✅ Scene does NOT reload
4. Wait 5 seconds
5. Try using the item again
6. **Expected Results:**
   - ✅ Teleport works normally
   - ✅ Scene loads successfully

#### Test 5: Test Return Functionality

**Steps:**
1. Teleport to Homestead scene (if not already there)
2. Create a test button or script to call: `HomesteadTeleportItem.ReturnToPreviousLocation()`
3. Click the button or execute the script
4. **Expected Results:**
   - ✅ Screen fades out
   - ✅ Returns to the previous scene (where you teleported from)
   - ✅ Screen fades in
   - ✅ Player appears at the same location as before
   - ✅ Console shows: "Returning to: [SceneName]"

**Note:** The return functionality is typically called from a UI button or portal in the Homestead scene.

#### Test 6: Test Multiple Teleports

**Steps:**
1. Start in Scene A (e.g., Village)
2. Use Homestead Teleport → Should go to Homestead
3. Return to previous location → Should go back to Scene A
4. Move to Scene B (e.g., Forest)
5. Use Homestead Teleport → Should go to Homestead
6. Return to previous location → Should go to Scene B (NOT Scene A)
7. **Expected Results:**
   - ✅ Return location updates each time you teleport
   - ✅ Always returns to the MOST RECENT scene
   - ✅ No errors or crashes

#### Test 7: Test Edge Cases

**Test 7a: No Return Location Saved**
1. Clear PlayerPrefs: `PlayerPrefs.DeleteKey("HomesteadReturnScene")`
2. Call `HomesteadTeleportItem.ReturnToPreviousLocation()`
3. **Expected Results:**
   - ✅ Console shows: "No return location saved. Cannot teleport back."
   - ✅ No scene transition occurs
   - ✅ No errors or crashes

**Test 7b: Missing SavingWrapper**
1. Remove SavingWrapper from the scene
2. Use Homestead Teleport item
3. **Expected Results:**
   - ✅ Console shows: "SavingWrapper not found. Loading scene directly without save."
   - ✅ Scene loads (without fade or save)
   - ✅ No crashes

**Test 7c: Missing Fader**
1. Remove Fader from the scene (keep SavingWrapper)
2. Use Homestead Teleport item
3. **Expected Results:**
   - ✅ Scene loads successfully
   - ✅ No fade transitions (instant transition)
   - ✅ Save/load still works
   - ✅ No errors

**Test 7d: Item Not Consumed**
1. Use Homestead Teleport item
2. Check inventory after teleport
3. **Expected Results:**
   - ✅ Item is still in inventory
   - ✅ Item count remains 1
   - ✅ Can be used again after cooldown

## Test Helper Script

Create this script to help with testing:

**File:** `Assets/Scripts/Homestead/Testing/HomesteadTeleportTester.cs`

```csharp
using UnityEngine;
using RPG.Homestead;
using GameDevTV.Inventories;

namespace RPG.Homestead.Testing
{
    /// <summary>
    /// Helper script for testing HomesteadTeleportItem functionality.
    /// Attach to a GameObject in the scene or use via Unity menu.
    /// </summary>
    public class HomesteadTeleportTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private HomesteadTeleportItem teleportItem;
        [SerializeField] private bool addToInventoryOnStart = false;
        
        private void Start()
        {
            if (addToInventoryOnStart && teleportItem != null)
            {
                AddTeleportItemToInventory();
            }
        }
        
        [ContextMenu("Add Teleport Item to Player Inventory")]
        public void AddTeleportItemToInventory()
        {
            if (teleportItem == null)
            {
                Debug.LogError("HomesteadTeleportTester: Teleport item not assigned!");
                return;
            }
            
            Inventory playerInventory = Inventory.GetPlayerInventory();
            if (playerInventory == null)
            {
                Debug.LogError("HomesteadTeleportTester: Player inventory not found!");
                return;
            }
            
            // Try to add to first empty slot
            bool added = playerInventory.AddToFirstEmptySlot(teleportItem, 1);
            if (added)
            {
                Debug.Log($"<color=green>Added {teleportItem.GetDisplayName()} to player inventory</color>");
            }
            else
            {
                Debug.LogWarning("HomesteadTeleportTester: Could not add item - inventory full?");
            }
        }
        
        [ContextMenu("Test Return to Previous Location")]
        public void TestReturnToPreviousLocation()
        {
            HomesteadTeleportItem.ReturnToPreviousLocation();
        }
        
        [ContextMenu("Check Saved Return Location")]
        public void CheckSavedReturnLocation()
        {
            string returnScene = PlayerPrefs.GetString("HomesteadReturnScene", "");
            if (string.IsNullOrEmpty(returnScene))
            {
                Debug.Log("No return location saved");
            }
            else
            {
                Debug.Log($"Saved return location: {returnScene}");
            }
        }
        
        [ContextMenu("Clear Saved Return Location")]
        public void ClearSavedReturnLocation()
        {
            PlayerPrefs.DeleteKey("HomesteadReturnScene");
            PlayerPrefs.Save();
            Debug.Log("Cleared saved return location");
        }
    }
}
```

### Using the Test Helper

1. Create an empty GameObject in your test scene
2. Name it: "HomesteadTeleportTester"
3. Add the `HomesteadTeleportTester` component
4. Assign the HomesteadTeleport asset to the "Teleport Item" field
5. Check "Add To Inventory On Start" if you want automatic setup
6. Enter Play mode
7. Right-click the component in Inspector to access test methods:
   - **Add Teleport Item to Player Inventory**: Adds item to player
   - **Test Return to Previous Location**: Triggers return teleport
   - **Check Saved Return Location**: Shows saved scene name
   - **Clear Saved Return Location**: Resets PlayerPrefs

## Verification Checklist

After completing all tests, verify:

### Subtask 12.1: Asset Creation
- [ ] HomesteadTeleport.asset exists at correct path
- [ ] homesteadSceneName is set to "Homestead"
- [ ] cooldownSeconds is set to 5.0
- [ ] Display name is "Homestead Teleport"
- [ ] Description is set
- [ ] Cannot be dropped is enabled
- [ ] Stackable is disabled
- [ ] Price is 0

### Subtask 12.2: Functionality Tests
- [ ] Item can be added to player inventory
- [ ] Using item loads Homestead scene correctly
- [ ] Fade transitions work (fade out → fade in)
- [ ] Return location is saved to PlayerPrefs
- [ ] Cooldown timer works (5 seconds)
- [ ] Item is not consumed on use
- [ ] Return functionality works correctly
- [ ] Return location updates with each teleport
- [ ] Edge cases handled gracefully (no crashes)

### Requirements Satisfied
- ✅ **16.1**: Teleport item is InventoryItem subclass (ActionItem)
- ✅ **16.2**: Using item initiates scene transition
- ✅ **16.3**: Loads homestead scene using SceneManager
- ✅ **16.4**: Preserves player state during transition (via SavingWrapper)
- ✅ **16.5**: Item is not consumed when used
- ✅ **16.6**: Cooldown timer prevents spam (5 seconds)
- ✅ **16.7**: Return teleport functionality provided

## Troubleshooting

### Asset Creation Issues

**Problem**: Menu item "Create Teleport Item Asset" doesn't appear
**Solution**:
1. Ensure `CreateHomesteadTeleportAsset.cs` is in `Assets/Scripts/Homestead/Editor/`
2. Check for compilation errors in Console
3. Restart Unity Editor
4. Try manual creation method

**Problem**: Asset created but fields are not set
**Solution**:
1. Select the asset in Project window
2. Manually set fields in Inspector
3. Or delete asset and re-run the creation script

### Testing Issues

**Problem**: "Player inventory not found"
**Solution**:
1. Ensure player GameObject has Inventory component
2. Check that player is tagged as "Player"
3. Verify Inventory component is from GameDevTV.Inventories namespace

**Problem**: Scene doesn't load
**Solution**:
1. Verify Homestead scene exists at: `Assets/Internal Assets/Homestead/Scenes/Homestead.unity`
2. Check that scene name in asset matches exactly: "Homestead"
3. Ensure scene is added to Build Settings (File → Build Settings → Add Open Scenes)

**Problem**: No fade transitions
**Solution**:
1. Check that Fader component exists in scene
2. Verify Fader is from RPG.SceneManagement namespace
3. Fader is optional - scene will load without it

**Problem**: Player state not saved
**Solution**:
1. Check that SavingWrapper component exists in scene
2. Verify SavingWrapper is from GameDevTV.Saving namespace
3. Ensure SavingSystem is properly configured

**Problem**: Cooldown doesn't work
**Solution**:
1. Check that cooldownSeconds is set to 5.0 in asset
2. Verify Time.time is advancing (not paused)
3. Try exiting and re-entering Play mode

**Problem**: Return location not saved
**Solution**:
1. Check Console for "Saved return location" message
2. Verify PlayerPrefs.Save() is being called
3. Check PlayerPrefs using: `Debug.Log(PlayerPrefs.GetString("HomesteadReturnScene"))`

## Next Steps

After completing Task 12:
1. ✅ Verify all tests pass
2. ✅ Document any issues or edge cases
3. ⏭️ Proceed to **Task 13**: Integration testing and bug fixes
4. ⏭️ Test the complete homestead system end-to-end

## Files Created

### New Files:
1. `Assets/Scripts/Homestead/Editor/CreateHomesteadTeleportAsset.cs` - Asset creation script
2. `Assets/Scripts/Homestead/Testing/HomesteadTeleportTester.cs` - Test helper script (optional)
3. `Assets/Internal Assets/Homestead/TASK_12_INSTRUCTIONS.md` - This file

### Files to be Created:
1. `Assets/Internal Assets/Homestead/HomesteadTeleport.asset` - The teleport item asset

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review the HomesteadTeleportItem.cs source code
3. Check Unity console for error messages
4. Verify all prerequisites are met (Task 1-11 completed)
5. Test in a clean scene with minimal setup

## Notes

- The teleport item uses the existing SavingWrapper and Fader systems for proper scene transitions
- The cooldown is implemented using static Time.time tracking
- Return location is stored in PlayerPrefs for persistence across sessions
- The item cannot be dropped or sold (important quest item)
- Icon can be set manually in Inspector if desired
- The item is designed to work with the existing RPG systems without modification
