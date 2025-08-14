using UnityEngine;
using GameDevTV.Inventories;
using RPG.Inventories;

namespace RPG.Dialogue
{
    public class SimpleItemTransfer : MonoBehaviour
    {
        [Header("Предмет для передачи")]
        [Tooltip("Предмет для передачи (перетащите ScriptableObject)")]
        [SerializeField] private InventoryItem itemToTransfer;
        
        [Tooltip("Альтернативно - ID предмета")]
        [SerializeField] private string itemID;
        
        [Tooltip("Количество предметов для передачи")]
        [SerializeField] [Min(1)] private int quantity = 1;

        [Header("Настройки")]
        [Tooltip("Показывать сообщения в консоли")]
        [SerializeField] private bool showMessages = true;

        private ItemTransferSystem transferSystem;

        private void Awake()
        {
            transferSystem = GetComponent<ItemTransferSystem>();
            if (transferSystem == null)
            {
                transferSystem = gameObject.AddComponent<ItemTransferSystem>();
            }
        }

        public void TransferItem()
        {
            InventoryItem item = GetItem();
            
            if (item == null)
            {
                if (showMessages)
                    Debug.LogError("No item specified for transfer!");
                return;
            }

            bool success = transferSystem.TransferItemFromPlayer(item, quantity);
            
            if (showMessages)
            {
                string message = success ? 
                    $"Передано {quantity} {item.GetDisplayName()}" : 
                    $"Не удалось передать {quantity} {item.GetDisplayName()}";
                Debug.Log(message);
            }
        }

        public void TransferCustomAmount(int customQuantity)
        {
            InventoryItem item = GetItem();
            
            if (item == null)
            {
                if (showMessages)
                    Debug.LogError("No item specified for transfer!");
                return;
            }

            bool success = transferSystem.TransferItemFromPlayer(item, customQuantity);
            
            if (showMessages)
            {
                string message = success ? 
                    $"Передано {customQuantity} {item.GetDisplayName()}" : 
                    $"Не удалось передать {customQuantity} {item.GetDisplayName()}";
                Debug.Log(message);
            }
        }

        public bool CanTransferItem()
        {
            InventoryItem item = GetItem();
            if (item == null) return false;

            return transferSystem.HasEnoughItems(item, quantity);
        }

        public bool CanTransferCustomAmount(int customQuantity)
        {
            InventoryItem item = GetItem();
            if (item == null) return false;

            return transferSystem.HasEnoughItems(item, customQuantity);
        }

        private InventoryItem GetItem()
        {
            if (itemToTransfer != null)
            {
                return itemToTransfer;
            }
            
            if (!string.IsNullOrEmpty(itemID))
            {
                return InventoryItem.GetFromID(itemID);
            }

            return null;
        }

        #region Unity Editor Helpers
        [ContextMenu("Test Transfer")]
        private void TestTransfer()
        {
            if (Application.isPlaying)
            {
                TransferItem();
            }
            else
            {
                Debug.Log("This function only works in play mode");
            }
        }

        [ContextMenu("Check Item Availability")]
        private void CheckItemAvailability()
        {
            if (Application.isPlaying)
            {
                var item = GetItem();
                if (item != null)
                {
                    var playerInventory = Inventory.GetPlayerInventory();
                    int currentCount = playerInventory != null ? playerInventory.GetItemCount(item) : 0;
                    bool canTransfer = CanTransferItem();
                    
                    Debug.Log($"Item: {item.GetDisplayName()} - Have: {currentCount}, Need: {quantity}, Can transfer: {canTransfer}");
                }
                else
                {
                    Debug.Log("No item specified");
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