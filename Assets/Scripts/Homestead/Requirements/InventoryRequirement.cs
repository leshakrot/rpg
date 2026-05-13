using UnityEngine;
using GameDevTV.Inventories;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires specific items in player inventory.
    /// </summary>
    [CreateAssetMenu(fileName = "New Inventory Requirement", menuName = "Homestead/Requirements/Inventory", order = 0)]
    public class InventoryRequirement : BuildingRequirement
    {
        [SerializeField] private InventoryItem requiredItem;
        [SerializeField] private int requiredQuantity = 1;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (skipResourceChecks) return true;
            
            if (requiredItem == null)
            {
                Debug.LogError("InventoryRequirement: Required item is null");
                return false;
            }
            
            var inventory = Inventory.GetPlayerInventory();
            if (inventory == null)
            {
                Debug.LogError("InventoryRequirement: Player inventory not found");
                return false;
            }
            
            int currentCount = inventory.GetItemCount(requiredItem);
            return currentCount >= requiredQuantity;
        }
        
        public override string GetDescription()
        {
            if (requiredItem == null) return "Invalid item requirement";
            return $"{requiredItem.GetDisplayName()} x{requiredQuantity}";
        }
        
        public override string GetMissingInfo()
        {
            if (requiredItem == null) return "Invalid item";
            
            var inventory = Inventory.GetPlayerInventory();
            if (inventory == null) return "Inventory not found";
            
            int currentCount = inventory.GetItemCount(requiredItem);
            int missing = Mathf.Max(0, requiredQuantity - currentCount);
            
            return $"Need {missing} more {requiredItem.GetDisplayName()} (have {currentCount}/{requiredQuantity})";
        }
        
        public override bool DeductResources()
        {
            var inventory = Inventory.GetPlayerInventory();
            if (inventory == null) return false;
            
            // Find and remove items
            int remainingToRemove = requiredQuantity;
            for (int i = 0; i < inventory.GetSize() && remainingToRemove > 0; i++)
            {
                if (object.ReferenceEquals(inventory.GetItemInSlot(i), requiredItem))
                {
                    int inSlot = inventory.GetNumberInSlot(i);
                    int toRemove = Mathf.Min(inSlot, remainingToRemove);
                    inventory.RemoveFromSlot(i, toRemove);
                    remainingToRemove -= toRemove;
                }
            }
            
            return remainingToRemove == 0;
        }
    }
}
