# Task 12 Quick Start Guide

## 🚀 Quick Execution Steps

### Step 1: Create the Asset (30 seconds)

1. Open Unity Editor
2. Go to menu: **Homestead → Create Teleport Item Asset**
3. Click **Yes**
4. Wait for success dialog
5. ✅ Done! Asset created at: `Assets/Internal Assets/Homestead/HomesteadTeleport.asset`

### Step 2: Quick Test (2 minutes)

**Option A: Automated Test (Easiest)**
1. Open any scene with a player character (e.g., Sandbox, Village)
2. Create empty GameObject, name it "TeleportTester"
3. Add component: `HomesteadTeleportTester`
4. Drag `HomesteadTeleport.asset` into "Teleport Item" field
5. Check ✓ "Add To Inventory On Start"
6. Press Play
7. Open inventory (press 'I')
8. Use the Homestead Teleport item
9. ✅ You should teleport to Homestead scene!

**Option B: Manual Test**
1. Open test scene with player
2. Press Play
3. Select Player in Hierarchy
4. Find Inventory component in Inspector
5. Drag `HomesteadTeleport.asset` into an empty inventory slot
6. Set slot number to 1
7. Open inventory UI
8. Use the item
9. ✅ You should teleport to Homestead scene!

### Step 3: Test Return (30 seconds)

1. After teleporting to Homestead
2. Select "TeleportTester" GameObject in Hierarchy
3. Right-click `HomesteadTeleportTester` component in Inspector
4. Click: **Test Return to Previous Location**
5. ✅ You should return to the previous scene!

## ✅ Verification

After testing, verify:
- [ ] Asset exists at correct path
- [ ] Teleport to Homestead works
- [ ] Screen fades out and in
- [ ] Return to previous location works
- [ ] Cooldown timer works (try using twice quickly)
- [ ] Item is not consumed

## 📋 Full Documentation

For detailed instructions, see:
- **TASK_12_INSTRUCTIONS.md** - Complete test procedures
- **TASK_12_COMPLETION_SUMMARY.md** - Technical details

## 🐛 Troubleshooting

**Menu item doesn't appear?**
- Restart Unity Editor
- Check for compilation errors in Console

**Scene doesn't load?**
- Ensure Homestead scene exists (run Task 11 first)
- Add scene to Build Settings: File → Build Settings → Add Open Scenes

**No fade transitions?**
- Check that Fader component exists in scene
- Fader is optional - scene will load without it

**Player inventory not found?**
- Ensure player GameObject has Inventory component
- Check player is tagged as "Player"

## 🎯 Requirements Satisfied

✅ **16.1**: Teleport item is ActionItem subclass  
✅ **16.2**: Using item initiates scene transition  
✅ **16.3**: Loads homestead scene  
✅ **16.4**: Preserves player state  
✅ **16.5**: Item not consumed  
✅ **16.6**: Cooldown timer (5 seconds)  
✅ **16.7**: Return functionality  

## ⏭️ Next Steps

After completing Task 12:
1. Verify all tests pass
2. Proceed to Task 13: Integration testing

## 💡 Tips

- Use the HomesteadTeleportTester component for easy testing
- Right-click the component in Inspector to access test methods
- Check Unity Console for helpful log messages
- The item cannot be dropped (it's a quest item)
- Cooldown is 5 seconds between uses
- Return location is saved in PlayerPrefs

## 📞 Need Help?

Check the full documentation:
- TASK_12_INSTRUCTIONS.md (detailed procedures)
- TASK_12_COMPLETION_SUMMARY.md (technical details)
- Unity Console (error messages and logs)
