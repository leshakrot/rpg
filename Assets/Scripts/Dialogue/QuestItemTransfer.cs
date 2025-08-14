using UnityEngine;
using GameDevTV.Inventories;
using RPG.Inventories;

namespace RPG.Dialogue
{
    public class QuestItemTransfer : MonoBehaviour
    {
        [System.Serializable]
        public class RequiredItem
        {
            [Tooltip("Предмет для передачи (перетащите ScriptableObject)")]
            public InventoryItem item;
            
            [Tooltip("Альтернативно - ID предмета (если не используете ссылку выше)")]
            public string itemID;
            
            [Tooltip("Количество предметов")]
            [Min(1)]
            public int quantity = 1;
            
            [Tooltip("Название предмета для отображения в логах")]
            public string displayName;
        }

        [Header("Настройки передачи предметов")]
        [SerializeField] private RequiredItem[] requiredItems;
        
        [Header("Сообщения")]
        [SerializeField] private bool showDebugMessages = true;
        [SerializeField] private string successMessage = "Предметы успешно переданы!";
        [SerializeField] private string failureMessage = "Недостаточно предметов для передачи!";

        private ItemTransferSystem transferSystem;

        private void Awake()
        {
            transferSystem = GetComponent<ItemTransferSystem>();
            if (transferSystem == null)
            {
                transferSystem = gameObject.AddComponent<ItemTransferSystem>();
            }
        }

        public void TransferSingleItem(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= requiredItems.Length)
            {
                Debug.LogError($"Invalid item index: {itemIndex}");
                return;
            }

            TransferItem(requiredItems[itemIndex]);
        }

        public void TransferAllRequiredItems()
        {
            bool allSuccessful = true;
            
            foreach (var requiredItem in requiredItems)
            {
                if (!TransferItem(requiredItem))
                {
                    allSuccessful = false;
                }
            }

            if (showDebugMessages)
            {
                string message = allSuccessful ? successMessage : failureMessage;
                Debug.Log(message);
            }
        }

        public void TransferItemByID(string itemID, int quantity = 1)
        {
            var success = transferSystem.TransferItemFromPlayerByID(itemID, quantity);
            
            if (showDebugMessages)
            {
                var item = InventoryItem.GetFromID(itemID);
                string itemName = item != null ? item.GetDisplayName() : itemID;
                string message = success ? 
                    $"Передано {quantity} {itemName}" : 
                    $"Не удалось передать {quantity} {itemName}";
                Debug.Log(message);
            }
        }

        public void TransferSpecificItem(InventoryItem item, int quantity)
        {
            if (item == null)
            {
                Debug.LogError("Item is null!");
                return;
            }

            var success = transferSystem.TransferItemFromPlayer(item, quantity);
            
            if (showDebugMessages)
            {
                string message = success ? 
                    $"Передано {quantity} {item.GetDisplayName()}" : 
                    $"Не удалось передать {quantity} {item.GetDisplayName()}";
                Debug.Log(message);
            }
        }

        public bool CanTransferAllItems()
        {
            foreach (var requiredItem in requiredItems)
            {
                if (!CanTransferItem(requiredItem))
                {
                    return false;
                }
            }
            return true;
        }

        public bool CanTransferItem(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= requiredItems.Length)
            {
                return false;
            }

            return CanTransferItem(requiredItems[itemIndex]);
        }

        private bool TransferItem(RequiredItem requiredItem)
        {
            InventoryItem item = GetItemFromRequiredItem(requiredItem);
            
            if (item == null)
            {
                if (showDebugMessages)
                {
                    Debug.LogError($"Item not found: {requiredItem.displayName ?? "Unknown"}");
                }
                return false;
            }

            bool success = transferSystem.TransferItemFromPlayer(item, requiredItem.quantity);
            
            if (showDebugMessages)
            {
                string itemName = !string.IsNullOrEmpty(requiredItem.displayName) ? 
                    requiredItem.displayName : item.GetDisplayName();
                    
                string message = success ? 
                    $"Передано {requiredItem.quantity} {itemName}" : 
                    $"Не удалось передать {requiredItem.quantity} {itemName}";
                Debug.Log(message);
            }

            return success;
        }

        private bool CanTransferItem(RequiredItem requiredItem)
        {
            InventoryItem item = GetItemFromRequiredItem(requiredItem);
            
            if (item == null) return false;

            return transferSystem.HasEnoughItems(item, requiredItem.quantity);
        }

        private InventoryItem GetItemFromRequiredItem(RequiredItem requiredItem)
        {
            if (requiredItem.item != null)
            {
                return requiredItem.item;
            }
            
            if (!string.IsNullOrEmpty(requiredItem.itemID))
            {
                return InventoryItem.GetFromID(requiredItem.itemID);
            }

            return null;
        }

        #region Unity Editor Helpers
        [ContextMenu("Check All Items Availability")]
        private void CheckAllItemsAvailability()
        {
            if (Application.isPlaying)
            {
                Debug.Log($"Can transfer all items: {CanTransferAllItems()}");
                
                for (int i = 0; i < requiredItems.Length; i++)
                {
                    var item = GetItemFromRequiredItem(requiredItems[i]);
                    if (item != null)
                    {
                        var playerInventory = Inventory.GetPlayerInventory();
                        int currentCount = playerInventory != null ? playerInventory.GetItemCount(item) : 0;
                        bool canTransfer = CanTransferItem(i);
                        
                        Debug.Log($"Item {i}: {item.GetDisplayName()} - Have: {currentCount}, Need: {requiredItems[i].quantity}, Can transfer: {canTransfer}");
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