using UnityEngine;
using UnityEngine.UI;
using RPG.Crafting;
using RPG.Inventories;
using GameDevTV.Inventories;

namespace RPG.UI.Crafting
{
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] GameObject recipePrefab = null;
        [SerializeField] CraftingSlotUI itemSlot = null;
        [SerializeField] GameObject recipeArrow = null;
        [SerializeField] Button craftButton = null;

        CraftingRecipe craftingRecipe;
        Inventory inventory;

        private void Awake()
        {
            //Get the player inventory component using the GetPlayerInventory method in the Inventory class.
            inventory = Inventory.GetPlayerInventory();
        }

        // НОВЫЙ МЕТОД: Подписка на события инвентаря для обновления UI
        private void OnEnable()
        {
            if (inventory != null)
            {
                // Подписываемся на события изменения инвентаря
                inventory.inventoryUpdated += UpdateCraftButtonStates;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                // Отписываемся от событий
                inventory.inventoryUpdated -= UpdateCraftButtonStates;
            }
        }

        public void SetupRecipes(CraftingRecipe recipe)
        {
            // Assign the parameter value (recipe) to the cached reference recipe (craftingRecipe).
            craftingRecipe = recipe;

            // Call the Redraw method which instantiates and sets up the UI elements.
            Redraw();
        }

        private void Redraw()
        {
            // Destroy all of the child elements in the current gameobject's transform.
            DestroyChild(transform);

            // Iterate through all of the crafting recipes.
            for (int i = 0; i < craftingRecipe.GetCraftingRecipes().Length; i++)
            {
                // For each crafting recipe:

                // Create a recipe holder gameobject in under the current transform.
                var recipeHolder = Instantiate(recipePrefab, transform);
                // Destroy all of the child elements in the recipe holder gameobject.
                DestroyChild(recipeHolder.transform);

                // Assign the current recipe iteration to a new variable.
                var recipe = craftingRecipe.GetCraftingRecipes()[i];

                // Create and setup recipe ingredient gameobject elements under the recipe holder gameobject transform.
                CreateRecipeIngredients(recipe, recipeHolder.transform);

                // Create and setup recipe objects. Arrow image, recipe result item and craft button.
                CreateRecipeObjects(craftingRecipe.GetCraftingRecipes()[i].item, recipeHolder.transform, recipe);
            }
        }

        private void CreateRecipeIngredients(CraftingRecipe.Recipes recipe, Transform recipeHolder)
        {
            // Store recipe ingredients length in a variable.
            int ingredientsSize = recipe.ingredients.Length;
            // Loop through all of the ingredients in the recipe.
            for (int ingredient = 0; ingredient < ingredientsSize; ingredient++)
            {
                // Create the itemSlot prefab and make it a child under the recipeHolder transform.
                var ingredientItem = Instantiate(itemSlot, recipeHolder);

                // Set up the item's (ingredientItem) icon and number amount.
                ingredientItem.Setup(recipe.ingredients[ingredient].item, recipe.ingredients[ingredient].number);
            }
        }

        private void CreateRecipeObjects(InventoryItem inventoryItem, Transform recipeHolder, CraftingRecipe.Recipes recipe)
        {
            // Create the arrow image UI element and make it a child under the recipeHolder transform.
            var arrow = Instantiate(recipeArrow, recipeHolder);

            // Instantiate the itemSlot prefab and make it a child under the recipeHolder transform.
            var item = Instantiate(itemSlot, recipeHolder);
            // Set up the item's (item) icon and number amount.
            item.Setup(inventoryItem, 1);

            // Create the crafting button UI element and make it a child under the recipeHolder transform.
            var button = Instantiate(craftButton, recipeHolder);
            // Get the Craft script component from the button gameobject and store it in a variable.
            var craft = button.GetComponent<Craft>();

            // ОБНОВЛЕНО: Проверяем доступность крафта и настраиваем кнопку
            bool canCraft = craft.CanCraft(inventory, recipe);
            button.interactable = canCraft;

            // Изменяем текст кнопки в зависимости от доступности
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = canCraft ? "Craft" : "Insufficient Materials";
            }

            // Add a listener to the button onClick event using a lambda expression.
            button.onClick.AddListener(() => {
                craft.CraftItem(inventory, inventoryItem, recipe);
                // НОВОЕ: Обновляем состояние всех кнопок после крафта
                UpdateCraftButtonStates();
            });
        }

        // НОВЫЙ МЕТОД: Обновление состояния всех кнопок крафта
        private void UpdateCraftButtonStates()
        {
            if (craftingRecipe == null) return;

            // Получаем все кнопки крафта в дочерних объектах
            Button[] craftButtons = GetComponentsInChildren<Button>();

            int recipeIndex = 0;
            foreach (Button button in craftButtons)
            {
                // Проверяем, что это действительно кнопка крафта (имеет компонент Craft)
                Craft craft = button.GetComponent<Craft>();
                if (craft != null && recipeIndex < craftingRecipe.GetCraftingRecipes().Length)
                {
                    var recipe = craftingRecipe.GetCraftingRecipes()[recipeIndex];
                    bool canCraft = craft.CanCraft(inventory, recipe);

                    // Обновляем состояние кнопки
                    button.interactable = canCraft;

                    // Обновляем текст кнопки
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
            // Iterate through all child transforms of the parameter specified transform.
            foreach (Transform child in transform)
            {
                // Remove each iterated transform.
                Destroy(child.gameObject);
            }
        }
    }
}