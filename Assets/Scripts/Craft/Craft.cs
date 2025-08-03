using UnityEngine;
using GameDevTV.Inventories;
using RPG.Control;

namespace RPG.Crafting
{
    /// <summary>
    /// Утилитарный класс для крафта. 
    /// Теперь используется в основном для обратной совместимости.
    /// Основная логика перенесена в CraftingManager.
    /// </summary>
    public class Craft : MonoBehaviour
    {
        /// <summary>
        /// Выполняет крафт предмета. Использует CraftingManager для централизованной логики.
        /// </summary>
        public void CraftItem(Inventory inventory, InventoryItem inventoryItem, CraftingRecipe.Recipes recipe)
        {
            var player = inventory.GetComponent<PlayerController>();
            if (player == null)
            {
                // Пытаемся найти игрока по тегу, если компонент не найден на том же объекте
                player = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
            }

            if (player != null && CraftingManager.Instance != null)
            {
                CraftingManager.Instance.CraftItem(player, inventoryItem, recipe);
            }
            else
            {
                Debug.LogError("Не удалось найти PlayerController или CraftingManager для выполнения крафта!");
            }
        }

        /// <summary>
        /// Проверяет, может ли игрок скрафтить предмет.
        /// </summary>
        public bool CanCraft(Inventory inventory, CraftingRecipe.Recipes recipe)
        {
            var player = inventory.GetComponent<PlayerController>();
            if (player == null)
            {
                player = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
            }

            if (player != null && CraftingManager.Instance != null)
            {
                return CraftingManager.Instance.CanCraft(player, recipe);
            }

            Debug.LogError("Не удалось найти PlayerController или CraftingManager для проверки крафта!");
            return false;
        }

        // Оставляем старые методы для обратной совместимости, но помечаем как устаревшие
        [System.Obsolete("Используйте CraftingManager.Instance.CraftItem() вместо этого метода")]
        private void RemoveItems(Inventory inventory, CraftingRecipe.Recipes recipe)
        {
            // Логика перенесена в CraftingManager
        }

        [System.Obsolete("Используйте CraftingManager.Instance.CanCraft() вместо этого метода")]
        private bool HasIngredients(Inventory inventory, CraftingRecipe.Recipes recipe)
        {
            // Логика перенесена в CraftingManager
            return CanCraft(inventory, recipe);
        }
    }
}