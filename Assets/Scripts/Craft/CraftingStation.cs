using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Control;
using RPG.Movement;
using RPG.UI.Crafting;

namespace RPG.Crafting
{
	public class CraftingStation : InteractableObject
	{
		[Header("Настройки Станции")]
		[SerializeField] private string stationName = "Верстак";

		[Header("Рецепты")]
		[Tooltip("Список ассетов с рецептами, доступными на этой станции")]
		[SerializeField] private List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>();

		public string StationName => stationName;

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


		public override CursorType GetCursorType()
		{
			return CursorType.Harvesting;
		}

		protected override void OnInteract(PlayerController callingController)
		{
			if (craftingRecipes.Count > 0)
			{
				CraftingUIManager.Instance.Show(this);
			}
			else
			{
				Debug.LogWarning($"На станции {stationName} не назначены рецепты!");
			}
		}
	}
}