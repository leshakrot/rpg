using UnityEngine;
using GameDevTV.Inventories;

namespace RPG.Inventories
{
    public class ItemTransferSystem : MonoBehaviour
    {
        [System.Serializable]
        public class ItemTransferData
        {
            [Tooltip("Предмет для передачи")]
            public InventoryItem item;
            
            [Tooltip("Количество предметов для передачи")]
            public int quantity = 1;
            
            [Tooltip("ID предмета (альтернатива ссылке на ScriptableObject)")]
            public string itemID;
        }

        public bool TransferItemFromPlayer(InventoryItem item, int quantity)
        {
            var playerInventory = Inventory.GetPlayerInventory();
            if (playerInventory == null)
            {
                Debug.LogError("Player inventory not found!");
                return false;
            }

            return TransferItemFromInventory(playerInventory, item, quantity);
        }

        public bool TransferItemFromPlayerByID(string itemID, int quantity)
        {
            var item = InventoryItem.GetFromID(itemID);
            if (item == null)
            {
                Debug.LogError($"Item with ID '{itemID}' not found!");
                return false;
            }

            return TransferItemFromPlayer(item, quantity);
        }

        public bool TransferItemFromInventory(Inventory inventory, InventoryItem item, int quantity)
        {
            if (inventory == null || item == null || quantity <= 0)
            {
                Debug.LogError("Invalid parameters for item transfer");
                return false;
            }

            int currentItemCount = inventory.GetItemCount(item);
            
            if (currentItemCount < quantity)
            {
                Debug.LogWarning($"Not enough items! Player has {currentItemCount}, but {quantity} required.");
                return false;
            }

            int remainingToRemove = quantity;
            
            for (int slotIndex = 0; slotIndex < inventory.GetSize() && remainingToRemove > 0; slotIndex++)
            {
                var slotItem = inventory.GetItemInSlot(slotIndex);
                if (slotItem != null && object.ReferenceEquals(slotItem, item))
                {
                    int numberInSlot = inventory.GetNumberInSlot(slotIndex);
                    int toRemoveFromSlot = Mathf.Min(remainingToRemove, numberInSlot);
                    
                    inventory.RemoveFromSlot(slotIndex, toRemoveFromSlot);
                    remainingToRemove -= toRemoveFromSlot;
                }
            }

            Debug.Log($"Successfully transferred {quantity} of {item.GetDisplayName()} from player inventory");
            return true;
        }

        public void TransferItemFromPlayerUsingData(ItemTransferData transferData)
        {
            if (transferData == null)
            {
                Debug.LogError("Transfer data is null!");
                return;
            }

            InventoryItem itemToTransfer = transferData.item;
            
            if (itemToTransfer == null && !string.IsNullOrEmpty(transferData.itemID))
            {
                itemToTransfer = InventoryItem.GetFromID(transferData.itemID);
            }

            if (itemToTransfer == null)
            {
                Debug.LogError("No valid item found for transfer!");
                return;
            }

            bool success = TransferItemFromPlayer(itemToTransfer, transferData.quantity);
            
            if (!success)
            {
                Debug.LogWarning($"Failed to transfer {transferData.quantity} of {itemToTransfer.GetDisplayName()}");
            }
        }

        public bool HasEnoughItems(InventoryItem item, int quantity)
        {
            var playerInventory = Inventory.GetPlayerInventory();
            if (playerInventory == null) return false;

            return playerInventory.GetItemCount(item) >= quantity;
        }

        public bool HasEnoughItemsByID(string itemID, int quantity)
        {
            var item = InventoryItem.GetFromID(itemID);
            if (item == null) return false;

            return HasEnoughItems(item, quantity);
        }
    }
}