using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Processing;
using RPG.Control;
using GameDevTV.Inventories; // Для доступа к инвентарю

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

        [Header("Информация о рецепте")]
        [SerializeField] private TextMeshProUGUI stationNameText;
        [SerializeField] private TextMeshProUGUI selectedRecipeInputText;
        [SerializeField] private TextMeshProUGUI selectedRecipeOutputText;
        [SerializeField] private Button craftButton;

        private PlayerController playerController;
        private ProcessingStation currentStation;
        private ProcessingRecipe selectedRecipe;

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
            Hide();
        }

        /// <summary>
        /// Показывает окно переработки для указанной станции.
        /// </summary>
        public void Show(ProcessingStation station)
        {
            this.currentStation = station;
            uiContainer.SetActive(true);

            // Очищаем старый список рецептов
            foreach (Transform child in recipeListRoot)
            {
                Destroy(child.gameObject);
            }

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

            // Обновляем текстовые поля
            selectedRecipeInputText.text = $"Нужно: {recipe.Input.quantity}x {recipe.Input.item.GetDisplayName()}";
            selectedRecipeOutputText.text = $"Создать: {recipe.Output.quantity}x {recipe.Output.item.GetDisplayName()}";

            // Проверяем, может ли игрок создать предмет, и делаем кнопку активной/неактивной
            var inventory = playerController.GetComponent<Inventory>();
            craftButton.interactable = inventory.HasItem(recipe.Input.item);
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

            // Можно добавить небольшую задержку и обновить состояние кнопки
            // чтобы проверить, остались ли еще ресурсы для крафта.
        }
    }
}