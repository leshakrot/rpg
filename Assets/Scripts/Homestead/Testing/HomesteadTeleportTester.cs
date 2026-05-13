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
        
        /// <summary>
        /// Adds the teleport item to the player's inventory.
        /// Can be called from Inspector context menu or code.
        /// </summary>
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
        
        /// <summary>
        /// Tests the return to previous location functionality.
        /// Can be called from Inspector context menu or code.
        /// </summary>
        [ContextMenu("Test Return to Previous Location")]
        public void TestReturnToPreviousLocation()
        {
            HomesteadTeleportItem.ReturnToPreviousLocation();
        }
        
        /// <summary>
        /// Checks and logs the currently saved return location.
        /// Can be called from Inspector context menu or code.
        /// </summary>
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
        
        /// <summary>
        /// Clears the saved return location from PlayerPrefs.
        /// Can be called from Inspector context menu or code.
        /// </summary>
        [ContextMenu("Clear Saved Return Location")]
        public void ClearSavedReturnLocation()
        {
            PlayerPrefs.DeleteKey("HomesteadReturnScene");
            PlayerPrefs.Save();
            Debug.Log("Cleared saved return location");
        }
        
        /// <summary>
        /// Simulates using the teleport item directly (bypasses inventory).
        /// Useful for testing without setting up inventory.
        /// </summary>
        [ContextMenu("Use Teleport Item Directly")]
        public void UseTeleportItemDirectly()
        {
            if (teleportItem == null)
            {
                Debug.LogError("HomesteadTeleportTester: Teleport item not assigned!");
                return;
            }
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("HomesteadTeleportTester: Player not found!");
                return;
            }
            
            bool success = teleportItem.Use(player);
            if (success)
            {
                Debug.Log("<color=green>Teleport initiated successfully</color>");
            }
            else
            {
                Debug.LogWarning("Teleport failed (probably on cooldown)");
            }
        }
    }
}
