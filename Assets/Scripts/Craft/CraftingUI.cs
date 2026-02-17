using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using RPG.Crafting;
using RPG.Inventories;
using RPG.Harvesting;
using GameDevTV.Inventories;

namespace RPG.UI.Crafting
{
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] GameObject recipePrefab = null;
        [SerializeField] CraftingSlotUI itemSlot = null;
        [SerializeField] GameObject recipeArrow = null;
        [SerializeField] Button craftButton = null;

        [Header("Прогресс-бар крафта (опционально)")]
        [Tooltip("Ссылка на компонент HarvestBar. Если не назначен — крафт мгновенный.")]
        [SerializeField] HarvestBar harvestBar = null;
        // craftingDuration удалён — время теперь берётся из каждого рецепта (recipe.craftingTime)

        CraftingRecipe craftingRecipe;
        Inventory inventory;

        // Флаг: идёт ли сейчас крафт — блокирует повторные нажатия
        private bool isCrafting = false;

        private void Awake()
        {
            inventory = Inventory.GetPlayerInventory();
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.inventoryUpdated += UpdateCraftButtonStates;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.inventoryUpdated -= UpdateCraftButtonStates;
            }

            // Если закрыли UI во время крафта — сбрасываем флаг и останавливаем бар
            if (isCrafting)
            {
                isCrafting = false;
                StopAllCoroutines();
                if (harvestBar != null) harvestBar.Stop();
            }
        }

        public void SetupRecipes(CraftingRecipe recipe)
        {
            craftingRecipe = recipe;
            isCrafting = false;
            Redraw();
        }

        private void Redraw()
        {
            DestroyChild(transform);

            for (int i = 0; i < craftingRecipe.GetCraftingRecipes().Length; i++)
            {
                var recipeHolder = Instantiate(recipePrefab, transform);
                DestroyChild(recipeHolder.transform);

                var recipe = craftingRecipe.GetCraftingRecipes()[i];

                CreateRecipeIngredients(recipe, recipeHolder.transform);
                CreateRecipeObjects(craftingRecipe.GetCraftingRecipes()[i].item, recipeHolder.transform, recipe);
            }
        }

        private void CreateRecipeIngredients(CraftingRecipe.Recipes recipe, Transform recipeHolder)
        {
            int ingredientsSize = recipe.ingredients.Length;
            for (int ingredient = 0; ingredient < ingredientsSize; ingredient++)
            {
                var ingredientItem = Instantiate(itemSlot, recipeHolder);
                ingredientItem.Setup(recipe.ingredients[ingredient].item, recipe.ingredients[ingredient].number);
            }
        }

        private void CreateRecipeObjects(InventoryItem inventoryItem, Transform recipeHolder, CraftingRecipe.Recipes recipe)
        {
            Instantiate(recipeArrow, recipeHolder);

            var item = Instantiate(itemSlot, recipeHolder);
            item.Setup(inventoryItem, 1);

            var button = Instantiate(craftButton, recipeHolder);
            var craft = button.GetComponent<Craft>();

            bool canCraft = craft.CanCraft(inventory, recipe);
            button.interactable = canCraft && !isCrafting;

            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = canCraft ? "Craft" : "Insufficient Materials";
            }

            button.onClick.AddListener(() =>
            {
                if (isCrafting) return;
                StartCoroutine(DoCraftWithBar(craft, inventory, inventoryItem, recipe));
            });
        }

        /// <summary>
        /// Корутина крафта: блокирует все кнопки, показывает прогресс-бар с именем предмета
        /// и временем из рецепта, по завершении выполняет крафт и разблокирует UI.
        /// </summary>
        private IEnumerator DoCraftWithBar(Craft craft, Inventory inv, InventoryItem inventoryItem, CraftingRecipe.Recipes recipe)
        {
            isCrafting = true;
            SetAllCraftButtonsInteractable(false);

            float duration = recipe.craftingTime;

            if (harvestBar != null && duration > 0f)
            {
                string itemName = (inventoryItem != null) ? inventoryItem.GetDisplayName() : "Крафт";
                harvestBar.StartProcessing(itemName, duration);
                yield return new WaitForSeconds(duration);
            }
            else
            {
                // Бар не назначен или время = 0 — мгновенный крафт
                yield return null;
            }

            craft.CraftItem(inv, inventoryItem, recipe);

            isCrafting = false;
            UpdateCraftButtonStates();
        }

        /// <summary>
        /// Включает или выключает все кнопки с компонентом Craft в этом UI.
        /// </summary>
        private void SetAllCraftButtonsInteractable(bool interactable)
        {
            foreach (Button btn in GetComponentsInChildren<Button>())
            {
                if (btn.GetComponent<Craft>() != null)
                {
                    btn.interactable = interactable;
                }
            }
        }

        /// <summary>
        /// Обновляет состояние всех кнопок крафта по текущему инвентарю.
        /// Во время крафта (isCrafting = true) ничего не делает.
        /// </summary>
        private void UpdateCraftButtonStates()
        {
            if (craftingRecipe == null) return;
            if (isCrafting) return;

            Button[] craftButtons = GetComponentsInChildren<Button>();

            int recipeIndex = 0;
            foreach (Button button in craftButtons)
            {
                Craft craft = button.GetComponent<Craft>();
                if (craft != null && recipeIndex < craftingRecipe.GetCraftingRecipes().Length)
                {
                    var recipe = craftingRecipe.GetCraftingRecipes()[recipeIndex];
                    bool canCraft = craft.CanCraft(inventory, recipe);

                    button.interactable = canCraft;

                    Text buttonText = button.GetComponentInChildren<Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = canCraft ? "Craft" : "Insufficient Materials";
                    }

                    recipeIndex++;
                }
            }
        }

        private void DestroyChild(Transform transform)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
