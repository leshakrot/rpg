using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Crafting;
using RPG.Control;
using GameDevTV.Inventories;
using System.Collections.Generic;

namespace RPG.UI.Crafting
{
	public class CraftingUIManager : MonoBehaviour
	{
		[Header("Основные компоненты")]
		[SerializeField] private GameObject uiContainer;
		[SerializeField] private Button closeButton;

		[Header("Заголовок")]
		[SerializeField] private TextMeshProUGUI stationNameText;

		[Header("Настройки рецептов")]
		[SerializeField] private GameObject recipePrefab = null;
		[SerializeField] private CraftingSlotUI itemSlot = null;
		[SerializeField] private GameObject recipeArrow = null;
		[SerializeField] private Button craftButton = null;

		[Header("Контейнер для рецептов")]
		[SerializeField] private Transform recipeContainer;

		private PlayerController playerController;
		private CraftingStation currentStation;
		private Inventory playerInventory;

		private List<GameObject> recipeUIList = new List<GameObject>();

		public static CraftingUIManager Instance { get; private set; }

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;

			if (closeButton != null)
			{
				closeButton.onClick.AddListener(Hide);
			}
		}

		private void Start()
		{
			playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
			playerInventory = playerController.GetComponent<Inventory>();

			if (playerInventory != null)
			{
				playerInventory.inventoryUpdated += UpdateAllUI;
			}

			Hide();
		}

		private void OnDestroy()
		{
			if (playerInventory != null)
			{
				playerInventory.inventoryUpdated -= UpdateAllUI;
			}
		}

		private void Update()
		{
			if (currentStation != null && uiContainer.activeSelf)
			{
				if (!currentStation.IsPlayerInRange(playerController))
				{
					Hide();
				}
			}
		}

		public void Show(CraftingStation station)
		{
			this.currentStation = station;
			uiContainer.SetActive(true);

			if (stationNameText != null) stationNameText.text = station.StationName;

			Redraw();
		}

		public void Hide()
		{
			uiContainer.SetActive(false);
			currentStation = null;
		}

		private void UpdateAllUI()
		{
			if (uiContainer.activeSelf)
			{
				Redraw();
			}
		}

		private void Redraw()
		{
			// ИЗМЕНЕНО: Проверку на CraftingRecipe можно убрать, так как GetAllRecipes вернет пустой массив, если рецептов нет.
			if (currentStation == null) return;

			foreach (var item in recipeUIList)
			{
				Destroy(item);
			}
			recipeUIList.Clear();

			// ИЗМЕНЕНО: Вызываем новый метод для получения всех рецептов со станции.
			var recipes = currentStation.GetAllRecipes();

			foreach (var recipe in recipes)
			{
				var recipeHolder = Instantiate(recipePrefab, recipeContainer);
				recipeUIList.Add(recipeHolder);

				ClearContainer(recipeHolder.transform);

				CreateRecipeIngredients(recipe, recipeHolder.transform);
				CreateRecipeResult(recipe, recipeHolder.transform);
			}
		}

		// ... остальной код класса CraftingUIManager остается без изменений ...
        
		private void CreateRecipeIngredients(CraftingRecipe.Recipes recipe, Transform parent)
		{
			foreach (var ingredient in recipe.ingredients)
			{
				var ingredientSlot = Instantiate(itemSlot, parent);
				int available = GetTotalItemCount(playerInventory, ingredient.item);
				bool hasEnough = available >= ingredient.number;
				ingredientSlot.Setup(ingredient.item, ingredient.number, available);
				ingredientSlot.SetResourceState(hasEnough);
			}
		}

		private void CreateRecipeResult(CraftingRecipe.Recipes recipe, Transform parent)
		{
			if (recipeArrow != null) Instantiate(recipeArrow, parent);

			var resultSlot = Instantiate(itemSlot, parent);
			resultSlot.Setup(recipe.item, 1);
			resultSlot.SetResourceState(true);

			var button = Instantiate(craftButton, parent);
			bool canCraft = CraftingManager.Instance.CanCraft(playerController, recipe);
			UpdateCraftButton(button, canCraft);

			button.onClick.AddListener(() => {
				CraftingManager.Instance.CraftItem(playerController, recipe.item, recipe);
			});
		}

		private void UpdateCraftButton(Button button, bool canCraft)
		{
			button.interactable = canCraft;
			Text buttonText = button.GetComponentInChildren<Text>();
			if (buttonText != null)
			{
				buttonText.text = canCraft ? "Создать" : "Недостаточно материалов";
			}
			var buttonImage = button.GetComponent<Image>();
			if (buttonImage != null)
			{
				buttonImage.color = canCraft ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.7f);
			}
		}

		private void ClearContainer(Transform container)
		{
			foreach (Transform child in container) Destroy(child.gameObject);
		}

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