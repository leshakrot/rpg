using System.Collections;
using System.Collections.Generic; // <-- Добавлено для List
using UnityEngine;
using RPG.Control;
using RPG.Movement;
using RPG.UI.Crafting;

namespace RPG.Crafting
{
	public class CraftingStation : MonoBehaviour, IRaycastable
	{
		[Header("Настройки Станции")]
		[SerializeField] private string stationName = "Верстак";
		[SerializeField] private float craftingDistance = 2.5f;

		[Header("Рецепты")]
		[Tooltip("Список ассетов с рецептами, доступными на этой станции")]
		// ИЗМЕНЕНО: Заменяем один рецепт на список рецептов
		[SerializeField] private List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>();

		private Coroutine activeInteractionCoroutine = null;

		public string StationName => stationName;

		// УДАЛЕНО: Старое свойство больше не нужно в таком виде.
		// public CraftingRecipe CraftingRecipe => craftingRecipe;

		/// <summary>
		/// НОВЫЙ МЕТОД: Собирает все рецепты из списка в один массив.
		/// UI будет использовать этот метод, чтобы получить плоский список всех доступных рецептов.
		/// </summary>
		public CraftingRecipe.Recipes[] GetAllRecipes()
		{
			var allRecipes = new List<CraftingRecipe.Recipes>();
			foreach (var recipeAsset in craftingRecipes)
			{
				if (recipeAsset != null)
				{
					allRecipes.AddRange(recipeAsset.GetCraftingRecipes());
				}
			}
			return allRecipes.ToArray();
		}


		public CursorType GetCursorType()
		{
			return CursorType.Harvesting;
		}

		public bool HandleRaycast(PlayerController callingController)
		{
			if (activeInteractionCoroutine != null) return true;

			if (Input.GetMouseButtonDown(0))
			{
				activeInteractionCoroutine = StartCoroutine(MoveToStationAndInteract(callingController));
			}
			return true;
		}

		private IEnumerator MoveToStationAndInteract(PlayerController callingController)
		{
			Mover mover = callingController.GetComponent<Mover>();

			while (Vector3.Distance(transform.position, callingController.transform.position) > craftingDistance)
			{
				mover.StartMoveAction(transform.position, 1f);
				yield return null;
			}

			mover.Cancel();

			// ИЗМЕНЕНО: Проверяем, есть ли вообще рецепты в списке.
			if (craftingRecipes.Count > 0)
			{
				CraftingUIManager.Instance.Show(this);
			}
			else
			{
				Debug.LogWarning($"На станции {stationName} не назначены рецепты!");
			}

			activeInteractionCoroutine = null;
		}

		public bool IsPlayerInRange(PlayerController player)
		{
			return Vector3.Distance(transform.position, player.transform.position) <= craftingDistance;
		}
	}
}