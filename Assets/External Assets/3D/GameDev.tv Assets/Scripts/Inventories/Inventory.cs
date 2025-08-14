using System;
using UnityEngine;
using GameDevTV.Saving;
using System.Collections.Generic;
using GameDevTV.Utils;

namespace GameDevTV.Inventories
{
    public class Inventory : MonoBehaviour, ISaveable, IPredicateEvaluator
    {
        // CONFIG DATA
        [Tooltip("Allowed size")]
        [SerializeField] int inventorySize = 16;

        // STATE
        InventorySlot[] slots;

        public struct InventorySlot
        {
            public InventoryItem item;
            public int number;
        }

        public event Action inventoryUpdated;

        public static Inventory GetPlayerInventory()
        {
            var player = GameObject.FindWithTag("Player");
            return player.GetComponent<Inventory>();
        }

        public bool HasSpaceFor(InventoryItem item)
        {
            return FindSlot(item) >= 0;
        }

        public bool HasSpaceFor(IEnumerable<InventoryItem> items)
        {
            int freeSlots = FreeSlots();
            List<InventoryItem> stackedItems = new List<InventoryItem>();
            foreach (var item in items)
            {
                if (item.IsStackable())
                {
                    if (HasItem(item)) continue;
                    if (stackedItems.Contains(item)) continue;
                    stackedItems.Add(item);
                }
                if (freeSlots <= 0) return false;
                freeSlots--;
            }
            return true;
        }

        public int FreeSlots()
        {
            int count = 0;
            foreach (InventorySlot slot in slots)
            {
                if (slot.number == 0)
                {
                    count++;
                }
            }
            return count;
        }

        public int GetSize()
        {
            return slots.Length;
        }

        public bool AddToFirstEmptySlot(InventoryItem item, int number)
        {
            foreach (var store in GetComponents<IItemStore>())
            {
                number -= store.AddItems(item, number);
            }
            if (number <= 0) return true;

            int i = FindSlot(item);

            if (i < 0)
            {
                return false;
            }

            slots[i].item = item;
            slots[i].number += number;
            if (inventoryUpdated != null)
            {
                inventoryUpdated();
            }
            return true;
        }

        public bool HasItem(InventoryItem item)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (object.ReferenceEquals(slots[i].item, item))
                {
                    return true;
                }
            }
            return false;
        }

        public InventoryItem GetItemInSlot(int slot)
        {
            return slots[slot].item;
        }

        public int GetItemSlot(InventoryItem item, int number)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (object.ReferenceEquals(slots[i].item, item) && GetNumberInSlot(i) >= number)
                {
                    // If true, return the slot number
                    return i;
                }
            }
            return -1;
        }

        public int GetNumberInSlot(int slot)
        {
            return slots[slot].number;
        }

        public void RemoveFromSlot(int slot, int number)
        {
            slots[slot].number -= number;
            if (slots[slot].number <= 0)
            {
                slots[slot].number = 0;
                slots[slot].item = null;
            }
            if (inventoryUpdated != null)
            {
                inventoryUpdated();
            }
        }

        public bool AddItemToSlot(int slot, InventoryItem item, int number)
        {
            if (slots[slot].item != null)
            {
                return AddToFirstEmptySlot(item, number); ;
            }

            var i = FindStack(item);
            if (i >= 0)
            {
                slot = i;
            }

            slots[slot].item = item;
            slots[slot].number += number;
            if (inventoryUpdated != null)
            {
                inventoryUpdated();
            }
            return true;
        }

        public int GetItemCount(InventoryItem item)
        {
            if (item == null) return 0;
            int total = 0;
            foreach (var slot in slots)
            {
                if (object.ReferenceEquals(slot.item, item))
                {
                    total += slot.number;
                }
            }
            return total;
        }

        /// <summary>
        /// Передает (удаляет) предмет определенного количества из инвентаря.
        /// Возвращает true если операция прошла успешно.
        /// </summary>
        public bool TransferItem(InventoryItem item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning("Invalid item or quantity for transfer");
                return false;
            }

            int currentItemCount = GetItemCount(item);
            
            if (currentItemCount < quantity)
            {
                Debug.LogWarning($"Not enough items! Have {currentItemCount}, need {quantity} of {item.GetDisplayName()}");
                return false;
            }

            int remainingToRemove = quantity;
            
            // Удаляем предметы из слотов
            for (int slotIndex = 0; slotIndex < slots.Length && remainingToRemove > 0; slotIndex++)
            {
                if (slots[slotIndex].item != null && object.ReferenceEquals(slots[slotIndex].item, item))
                {
                    int numberInSlot = slots[slotIndex].number;
                    int toRemoveFromSlot = Mathf.Min(remainingToRemove, numberInSlot);
                    
                    RemoveFromSlot(slotIndex, toRemoveFromSlot);
                    remainingToRemove -= toRemoveFromSlot;
                }
            }

            Debug.Log($"Successfully transferred {quantity} of {item.GetDisplayName()}");
            return true;
        }

        /// <summary>
        /// Передает предмет по ID.
        /// </summary>
        public bool TransferItemByID(string itemID, int quantity)
        {
            var item = InventoryItem.GetFromID(itemID);
            if (item == null)
            {
                Debug.LogError($"Item with ID '{itemID}' not found!");
                return false;
            }

            return TransferItem(item, quantity);
        }

        /// <summary>
        /// Проверяет, достаточно ли предметов для передачи.
        /// </summary>
        public bool CanTransferItem(InventoryItem item, int quantity)
        {
            if (item == null || quantity <= 0) return false;
            return GetItemCount(item) >= quantity;
        }

        /// <summary>
        /// Проверяет, достаточно ли предметов для передачи по ID.
        /// </summary>
        public bool CanTransferItemByID(string itemID, int quantity)
        {
            var item = InventoryItem.GetFromID(itemID);
            if (item == null) return false;
            return CanTransferItem(item, quantity);
        }

        private void Awake()
        {
            slots = new InventorySlot[inventorySize];
        }

        private int FindSlot(InventoryItem item)
        {
            int i = FindStack(item);
            if (i < 0)
            {
                i = FindEmptySlot();
            }
            return i;
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item == null)
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindStack(InventoryItem item)
        {
            if (!item.IsStackable())
            {
                return -1;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (object.ReferenceEquals(slots[i].item, item))
                {
                    return i;
                }
            }
            return -1;
        }

        [System.Serializable]
        private struct InventorySlotRecord
        {
            public string itemID;
            public int number;
        }

        object ISaveable.CaptureState()
        {
            var slotStrings = new InventorySlotRecord[inventorySize];
            for (int i = 0; i < inventorySize; i++)
            {
                if (slots[i].item != null)
                {
                    slotStrings[i].itemID = slots[i].item.GetItemID();
                    slotStrings[i].number = slots[i].number;
                }
            }
            return slotStrings;
        }

        void ISaveable.RestoreState(object state)
        {
            var slotStrings = (InventorySlotRecord[])state;
            for (int i = 0; i < inventorySize; i++)
            {
                slots[i].item = InventoryItem.GetFromID(slotStrings[i].itemID);
                slots[i].number = slotStrings[i].number;
            }
            if (inventoryUpdated != null)
            {
                inventoryUpdated();
            }
        }

        public bool? Evaluate(string predicate, string[] parameters)
        {
            switch (predicate)
            {
                case "HasInventoryItem":
                    {
                        if (parameters.Length < 1) return false;
                        InventoryItem item = InventoryItem.GetFromID(parameters[0]);
                        if (item == null) return false;

                        if (parameters.Length > 1 && int.TryParse(parameters[1], out int requiredAmount))
                        {
                            return GetItemCount(item) >= requiredAmount;
                        }
                        else
                        {
                            return HasItem(item);
                        }
                    }
            }
            return null;
        }
    }
}