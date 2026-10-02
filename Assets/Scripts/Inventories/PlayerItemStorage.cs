using GameDevTV.Inventories;

namespace RPG.Inventories
{
    /// <summary>
    /// Общие операции над предметами игрока, учитывающие и инвентарь, и action bar (ActionStore).
    /// Используется системами крафта и переработки.
    /// </summary>
    public static class PlayerItemStorage
    {
        /// <summary>
        /// Общее количество предмета в инвентаре и на action bar.
        /// </summary>
        public static int GetTotalCount(Inventory inventory, InventoryItem item)
        {
            if (inventory == null || item == null) return 0;

            int total = inventory.GetItemCount(item);

            ActionStore actionStore = inventory.GetComponent<ActionStore>();
            if (actionStore != null)
            {
                total += actionStore.GetItemCount(item);
            }
            return total;
        }

        /// <summary>
        /// Достаточно ли предмета суммарно в инвентаре и на action bar.
        /// </summary>
        public static bool HasItems(Inventory inventory, InventoryItem item, int number)
        {
            return GetTotalCount(inventory, item) >= number;
        }

        /// <summary>
        /// Списывает предмет: сначала из инвентаря, остаток — с action bar.
        /// Если предметов не хватает, ничего не списывается и возвращается false.
        /// </summary>
        public static bool RemoveItems(Inventory inventory, InventoryItem item, int number)
        {
            if (inventory == null || item == null) return false;
            if (number <= 0) return true;
            if (!HasItems(inventory, item, number)) return false;

            int remaining = number;

            for (int i = 0; i < inventory.GetSize() && remaining > 0; i++)
            {
                if (!object.ReferenceEquals(inventory.GetItemInSlot(i), item)) continue;

                int toRemove = System.Math.Min(inventory.GetNumberInSlot(i), remaining);
                inventory.RemoveFromSlot(i, toRemove);
                remaining -= toRemove;
            }

            if (remaining > 0)
            {
                ActionStore actionStore = inventory.GetComponent<ActionStore>();
                if (actionStore != null)
                {
                    remaining -= actionStore.RemoveItemsByType(item, remaining);
                }
            }

            return remaining <= 0;
        }
    }
}
