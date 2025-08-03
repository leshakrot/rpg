using UnityEngine;
using RPG.Control;
using GameDevTV.Inventories;
using RPG.UI.RequirementText;

namespace RPG.Crafting
{
    public class CraftingManager : MonoBehaviour
    {
        private static CraftingManager _instance;
        public static CraftingManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Выполняет крафт предмета по указанному рецепту.
        /// </summary>
        public bool CraftItem(PlayerController player, InventoryItem resultItem, CraftingRecipe.Recipes recipe)
        {
            var inventory = player.GetComponent<Inventory>();
            if (inventory == null)
            {
                Debug.LogError("У игрока нет компонента Inventory!");
                return false;
            }

            // Проверяем наличие ингредиентов
            if (!HasIngredients(inventory, recipe))
            {
                ShowMissingIngredientsMessage(recipe);
                return false;
            }

            // Убираем ингредиенты из инвентаря
            RemoveItems(inventory, recipe);

            // Добавляем результат
            bool addedSuccessfully = inventory.AddToFirstEmptySlot(resultItem, 1);
            if (!addedSuccessfully)
            {
                Debug.LogWarning($"Не удалось добавить {resultItem.GetDisplayName()} в инвентарь. Нет места!");
                // Возвращаем ингредиенты обратно
                ReturnIngredients(inventory, recipe);
                RequirementTextManager.Show("Недостаточно места в инвентаре!");
                return false;
            }

            Debug.Log($"Успешно создан предмет: {resultItem.GetDisplayName()}");
            return true;
        }

        /// <summary>
        /// Проверяет, может ли игрок скрафтить предмет.
        /// </summary>
        public bool CanCraft(PlayerController player, CraftingRecipe.Recipes recipe)
        {
            var inventory = player.GetComponent<Inventory>();
            if (inventory == null) return false;

            return HasIngredients(inventory, recipe);
        }

        /// <summary>
        /// Показывает сообщение о недостающих ингредиентах.
        /// </summary>
        private void ShowMissingIngredientsMessage(CraftingRecipe.Recipes recipe)
        {
            string missingItems = "";
            var inventory = Inventory.GetPlayerInventory();

            foreach (var ingredient in recipe.ingredients)
            {
                int available = GetTotalItemCount(inventory, ingredient.item);
                if (available < ingredient.number)
                {
                    if (missingItems != "") missingItems += ", ";
                    missingItems += $"{ingredient.item.GetDisplayName()} ({available}/{ingredient.number})";
                }
            }

            RequirementTextManager.Show($"Недостаточно материалов: {missingItems}");
        }

        /// <summary>
        /// Возвращает ингредиенты в инвентарь (в случае ошибки).
        /// </summary>
        private void ReturnIngredients(Inventory inventory, CraftingRecipe.Recipes recipe)
        {
            foreach (var ingredient in recipe.ingredients)
            {
                inventory.AddToFirstEmptySlot(ingredient.item, ingredient.number);
            }
        }

        /// <summary>
        /// Убирает ингредиенты из инвентаря.
        /// </summary>
        private void RemoveItems(Inventory inventory, CraftingRecipe.Recipes recipe)
        {
            foreach (CraftingRecipe.Ingredients ingredient in recipe.ingredients)
            {
                if (ingredient.item.IsStackable())
                {
                    int itemSlot = inventory.GetItemSlot(ingredient.item, ingredient.number);
                    inventory.RemoveFromSlot(itemSlot, ingredient.number);
                }
                else
                {
                    for (int i = 0; i < ingredient.number; i++)
                    {
                        int itemSlot = inventory.GetItemSlot(ingredient.item, 1);
                        inventory.RemoveFromSlot(itemSlot, 1);
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет наличие всех ингредиентов в инвентаре.
        /// </summary>
        private bool HasIngredients(Inventory inventory, CraftingRecipe.Recipes recipe)
        {
            foreach (CraftingRecipe.Ingredients ingredient in recipe.ingredients)
            {
                bool hasItem = false;

                if (ingredient.item.IsStackable())
                {
                    hasItem = inventory.GetItemSlot(ingredient.item, ingredient.number) >= 0;
                }
                else
                {
                    int itemCount = GetTotalItemCount(inventory, ingredient.item);
                    hasItem = itemCount >= ingredient.number;
                }

                if (!hasItem) return false;
            }
            return true;
        }

        /// <summary>
        /// Подсчитывает общее количество предмета в инвентаре.
        /// </summary>
        private int GetTotalItemCount(Inventory inventory, InventoryItem item)
        {
            int total = 0;
            for (int i = 0; i < inventory.GetSize(); i++)
            {
                if (object.ReferenceEquals(inventory.GetItemInSlot(i), item))
                {
                    total += inventory.GetNumberInSlot(i);
                }
            }
            return total;
        }
    }
}