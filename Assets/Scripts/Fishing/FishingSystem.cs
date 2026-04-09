using System;
using UnityEngine;
using System.Collections.Generic;
using GameDevTV.Inventories;

public class FishingSystem : MonoBehaviour
{
	[SerializeField] private FishingUI fishingUI;
	[SerializeField] private FishingMiniGame fishingMiniGame;
	[SerializeField] private Inventory inventory;

	private FishingAreaTrigger currentFishingArea;
	private FishData selectedFishData;
	private FishItemData selectedFishItem;

	private void OnEnable()
	{
		fishingMiniGame.OnFishingComplete += HandleFishCaught;
	}

	private void OnDisable()
	{
		fishingMiniGame.OnFishingComplete -= HandleFishCaught;
	}

	public void SetCurrentFishingArea(FishingAreaTrigger fishingArea)
	{
		currentFishingArea = fishingArea;
	}

	public void StartFishing()
	{
		if (currentFishingArea == null)
		{
			Debug.LogWarning("Нет активной зоны рыбалки!");
			return;
		}

		// Выбираем случайную рыбу из доступных в этой зоне
		selectedFishData = SelectRandomFish(currentFishingArea.GetAvailableFish());

		if (selectedFishData == null)
		{
			Debug.LogWarning("В этой зоне нет доступных рыб!");
			return;
		}

		// Выбираем случайный предмет из этой рыбы (с учетом весов и индивидуальной сложности)
		selectedFishItem = selectedFishData.GetRandomFishItem();

		if (selectedFishItem == null || selectedFishItem.item == null)
		{
			Debug.LogWarning("В FishData нет доступных предметов!");
			return;
		}

		Debug.Log($"Заброс удочки... Попытка поймать: {selectedFishItem.item.GetDisplayName()} (сложность: {selectedFishItem.catchDifficulty:F2})");

		// Запускаем мини-игру с параметрами выбранного предмета
		fishingMiniGame.StartMiniGame(selectedFishData, selectedFishItem.catchDifficulty);
	}

	private FishData SelectRandomFish(List<FishData> availableFish)
	{
		if (availableFish == null || availableFish.Count == 0)
			return null;

		// Рассчитываем общий вес всех FishData
		int totalWeight = 0;
		foreach (var fish in availableFish)
		{
			// Суммируем веса всех предметов внутри каждого FishData
			foreach (var fishItem in fish.possibleItems)
			{
				totalWeight += fishItem.dropWeight;
			}
		}

		if (totalWeight == 0)
			return availableFish[0];

		// Выбираем случайное значение
		int randomValue = UnityEngine.Random.Range(0, totalWeight);

		// Находим рыбу по весу
		int currentWeight = 0;
		foreach (var fish in availableFish)
		{
			foreach (var fishItem in fish.possibleItems)
			{
				currentWeight += fishItem.dropWeight;
				if (randomValue < currentWeight)
				{
					return fish;
				}
			}
		}

		// На всякий случай возвращаем первую рыбу
		return availableFish[0];
	}

	private void HandleFishCaught()
	{
		if (selectedFishItem != null && selectedFishItem.item != null)
		{
			AddCaughtFishToInventory(selectedFishItem.item, 1);
			Debug.Log($"Поймана рыба: {selectedFishItem.item.GetDisplayName()}");
		}
		else
		{
			Debug.LogWarning("Нет выбранного предмета для добавления!");
		}

		fishingUI.ToggleSuccessNotificationVisibility(true);
		fishingUI.ShowFishingButton(true);
	}

	private void AddCaughtFishToInventory(InventoryItem fishItem, int number)
	{
		inventory.AddToFirstEmptySlot(fishItem, number);
	}
}