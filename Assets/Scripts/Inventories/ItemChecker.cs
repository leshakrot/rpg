using UnityEngine;
using GameDevTV.Inventories;
using GameDevTV.Utils;

namespace RPG.Inventories
{
    public class ItemChecker : MonoBehaviour, IPredicateEvaluator
    {
        [System.Serializable]
        public class ItemRequirement
        {
            [Tooltip("Предмет для проверки")]
            public InventoryItem item;
            
            [Tooltip("ID предмета (альтернатива)")]
            public string itemID;
            
            [Tooltip("Необходимое количество")]
            [Min(1)]
            public int requiredQuantity = 1;
        }

        [Header("Предметы для проверки")]
        [SerializeField] private ItemRequirement[] itemRequirements;

        public bool HasAllRequiredItems()
        {
            var playerInventory = Inventory.GetPlayerInventory();
            if (playerInventory == null) return false;

            foreach (var requirement in itemRequirements)
            {
                InventoryItem item = GetItemFromRequirement(requirement);
                if (item == null) return false;

                int currentCount = playerInventory.GetItemCount(item);
                if (currentCount < requirement.requiredQuantity)
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasSpecificItem(InventoryItem item, int quantity = 1)
        {
            var playerInventory = Inventory.GetPlayerInventory();
            if (playerInventory == null || item == null) return false;

            return playerInventory.GetItemCount(item) >= quantity;
        }

        public bool HasItemByID(string itemID, int quantity = 1)
        {
            var item = InventoryItem.GetFromID(itemID);
            return HasSpecificItem(item, quantity);
        }

        public int GetItemCount(InventoryItem item)
        {
            var playerInventory = Inventory.GetPlayerInventory();
            if (playerInventory == null || item == null) return 0;

            return playerInventory.GetItemCount(item);
        }

        public int GetItemCountByID(string itemID)
        {
            var item = InventoryItem.GetFromID(itemID);
            return GetItemCount(item);
        }

        private InventoryItem GetItemFromRequirement(ItemRequirement requirement)
        {
            if (requirement.item != null)
            {
                return requirement.item;
            }
            
            if (!string.IsNullOrEmpty(requirement.itemID))
            {
                return InventoryItem.GetFromID(requirement.itemID);
            }

            return null;
        }

        public bool? Evaluate(string predicate, string[] parameters)
        {
            switch (predicate)
            {
                case "HasRequiredItems":
                    return HasAllRequiredItems();
                    
                case "HasItemForTransfer":
                    if (parameters.Length < 1) return false;
                    
                    string itemID = parameters[0];
                    int quantity = 1;
                    
                    if (parameters.Length > 1 && int.TryParse(parameters[1], out int parsedQuantity))
                    {
                        quantity = parsedQuantity;
                    }
                    
                    return HasItemByID(itemID, quantity);
                    
                case "CanTransferAllItems":
                    return HasAllRequiredItems();
            }

            return null;
        }

        #region Unity Editor Helpers
        [ContextMenu("Check All Requirements")]
        private void CheckAllRequirements()
        {
            if (Application.isPlaying)
            {
                bool hasAll = HasAllRequiredItems();
                Debug.Log($"Has all required items: {hasAll}");
                
                var playerInventory = Inventory.GetPlayerInventory();
                if (playerInventory != null)
                {
                    foreach (var requirement in itemRequirements)
                    {
                        var item = GetItemFromRequirement(requirement);
                        if (item != null)
                        {
                            int currentCount = playerInventory.GetItemCount(item);
                            bool hasEnough = currentCount >= requirement.requiredQuantity;
                            Debug.Log($"Item: {item.GetDisplayName()} - Have: {currentCount}, Need: {requirement.requiredQuantity}, Enough: {hasEnough}");
                        }
                    }
                }
            }
            else
            {
                Debug.Log("This function only works in play mode");
            }
        }
        #endregion
    }
}