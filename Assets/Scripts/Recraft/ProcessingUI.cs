using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Processing;
using RPG.Control;
using GameDevTV.Inventories;
using RPG.UI.Crafting; // Для использования CraftingSlotUI

namespace RPG.UI
{
    public class ProcessingUI : MonoBehaviour
    {
        [Header("Основные компоненты")]
        [SerializeField] private GameObject uiContainer;
        [SerializeField] private Button closeButton;

        [Header("Список рецептов")]
        [SerializeField] private Transform recipeListRoot;
        [SerializeField] private RecipeUIEntry recipeButtonPrefab;

        [Header("Детали выбранного рецепта")]
        [SerializeField] private TextMeshProUGUI stationNameText;
        [SerializeField] private Transform recipeDetailsContainer; // Контейнер для отображения рецепта
        [SerializeField] private CraftingSlotUI itemSlotPrefab; // Префаб слота для предметов
        [SerializeField] private GameObject recipeArrowPrefab; // Префаб стрелки
        [SerializeField] private Button craftButton;

        [Header("Проверка ресурсов")]
        [SerializeField] private TextMeshProUGUI resourceStatusText; // Опциональный текст статуса

        private PlayerController playerController;
        private ProcessingStation currentStation;
        private ProcessingRecipe selectedRecipe;
        private Inventory playerInventory;

        public static ProcessingUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            craftButton.onClick.AddListener(Craft);
            closeButton.onClick.AddListener(Hide);
        }

        private void Start()
        {
            playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            playerInventory = playerController.GetComponent<Inventory>();

            // Подписываемся на изменения инвентаря для обновления UI
            if (playerInventory != null)
            {
                playerInventory.inventoryUpdated += UpdateRecipeDetails;
            }

            Hide();
        }

        private void OnDestroy()
        {
            // Отписываемся от событий при уничтожении
            if (playerInventory != null)
            {
                playerInventory.inventoryUpdated -= UpdateRecipeDetails;
            }
        }

        /// <summary>
        /// Показывает окно переработки для указанной станции.
        /// </summary>
        public void Show(ProcessingStation station)
        {
            this.currentStation = station;
            uiContainer.SetActive(true);

            // Очищаем старый список рецептов
            ClearContainer(recipeListRoot);

            // Заполняем список новыми рецептами
            foreach (var recipe in station.AvailableRecipes)
            {
                var entry = Instantiate(recipeButtonPrefab, recipeListRoot);
                entry.Setup(recipe, this);
            }

            // Обновляем заголовок
            if (stationNameText != null)
            {
                stationNameText.text = station.StationName;
            }

            // Выбираем первый рецепт по умолчанию
            if (station.AvailableRecipes.Count > 0)
            {
                SelectRecipe(station.AvailableRecipes[0]);
            }
        }

        /// <summary>
        /// Вызывается при выборе рецепта из списка.
        /// </summary>
        public void SelectRecipe(ProcessingRecipe recipe)
        {
            this.selectedRecipe = recipe;
            UpdateRecipeDetails();
        }

        /// <summary>
        /// Обновляет визуальное отображение выбранного рецепта.
        /// </summary>
        private void UpdateRecipeDetails()
        {
            if (selectedRecipe == null) return;

            // Очищаем контейнер деталей рецепта
            ClearContainer(recipeDetailsContainer);

            // Создаем слот для входного ресурса
            var inputSlot = Instantiate(itemSlotPrefab, recipeDetailsContainer);

            // Проверяем количество ресурсов в инвентаре
            int availableAmount = GetTotalItemCount(playerInventory, selectedRecipe.Input.item);
            bool hasEnoughResources = availableAmount >= selectedRecipe.Input.quantity;

            // Настраиваем слот с цветовой индикацией
            inputSlot.Setup(selectedRecipe.Input.item, selectedRecipe.Input.quantity);
            SetSlotAppearance(inputSlot, hasEnoughResources, availableAmount, selectedRecipe.Input.quantity);

            // Создаем стрелку
            if (recipeArrowPrefab != null)
            {
                Instantiate(recipeArrowPrefab, recipeDetailsContainer);
            }

            // Создаем слот для результата
            var outputSlot = Instantiate(itemSlotPrefab, recipeDetailsContainer);
            outputSlot.Setup(selectedRecipe.Output.item, selectedRecipe.Output.quantity);
            SetSlotAppearance(outputSlot, true, selectedRecipe.Output.quantity, selectedRecipe.Output.quantity); // Результат всегда "доступен"

            // Обновляем состояние кнопки крафта
            craftButton.interactable = hasEnoughResources;

            // Обновляем текст кнопки
            Text buttonText = craftButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = hasEnoughResources ? "Создать" : "Недостаточно ресурсов";
            }

            // Обновляем статусный текст (если есть)
            if (resourceStatusText != null)
            {
                if (hasEnoughResources)
                {
                    resourceStatusText.text = $"Готово к переработке! Время: {selectedRecipe.TimeToProcess:F1}с";
                    resourceStatusText.color = Color.green;
                }
                else
                {
                    int needed = selectedRecipe.Input.quantity - availableAmount;
                    resourceStatusText.text = $"Нужно еще: {needed}x {selectedRecipe.Input.item.GetDisplayName()}";
                    resourceStatusText.color = Color.red;
                }
            }
        }

        /// <summary>
        /// Настраивает внешний вид слота в зависимости от доступности ресурсов.
        /// </summary>
        private void SetSlotAppearance(CraftingSlotUI slot, bool hasEnough, int available, int required)
        {
            // Получаем компоненты слота для изменения внешнего вида
            Image background = slot.GetComponent<Image>();
            if (background != null)
            {
                if (hasEnough)
                {
                    background.color = new Color(0.2f, 0.8f, 0.2f, 0.3f); // Зеленоватый
                }
                else
                {
                    background.color = new Color(0.8f, 0.2f, 0.2f, 0.3f); // Красноватый
                }
            }

            // Можно добавить дополнительный текст с количеством в инвентаре
            Text[] texts = slot.GetComponentsInChildren<Text>();
            foreach (Text text in texts)
            {
                if (text.name.Contains("Count") || text.name.Contains("Amount"))
                {
                    text.text = $"{available}/{required}";
                    text.color = hasEnough ? Color.white : Color.red;
                    break;
                }
            }
        }

        /// <summary>
        /// Скрывает окно переработки.
        /// </summary>
        public void Hide()
        {
            uiContainer.SetActive(false);
        }

        /// <summary>
        /// Вызывается при нажатии на кнопку "Создать".
        /// </summary>
        private void Craft()
        {
            if (selectedRecipe == null) return;

            ProcessingManager.Instance.StartProcessing(selectedRecipe, playerController);

            // Обновляем UI после начала обработки
            // Небольшая задержка, чтобы инвентарь успел обновиться
            Invoke(nameof(UpdateRecipeDetails), 0.1f);
        }

        /// <summary>
        /// Очищает контейнер от дочерних объектов.
        /// </summary>
        private void ClearContainer(Transform container)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Подсчитывает общее количество предмета в инвентаре.
        /// </summary>
        private int GetTotalItemCount(Inventory inventory, InventoryItem item)
        {
            int total = 0;
            for (int i = 0; i < inventory.GetSize(); i++)
            {
                if (inventory.GetItemInSlot(i) == item)
                {
                    total += inventory.GetNumberInSlot(i);
                }
            }
            return total;
        }
    }
}