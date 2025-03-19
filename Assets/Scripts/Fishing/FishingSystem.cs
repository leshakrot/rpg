using System;
using UnityEngine;
using System.Collections.Generic;
using GameDevTV.Inventories;

public class FishingSystem : MonoBehaviour
{
	[SerializeField] private FishingUI fishingUI;
	[SerializeField] private FishingMiniGame fishingMiniGame;
	[SerializeField] private List<InventoryItem> possibleCatches = new List<InventoryItem>();
	[SerializeField] private Inventory inventory;
	

    private void OnEnable()
    {
        fishingMiniGame.OnFishingComplete += HandleFishCaught;
    }

    private void OnDisable()
    {
        fishingMiniGame.OnFishingComplete -= HandleFishCaught;
    }

    public void StartFishing()
	{
		Debug.Log("Заброс удочки...");

		fishingMiniGame.StartMiniGame();
	}

	private void HandleFishCaught()
	{
		AddCaughtFishToInventory(possibleCatches[UnityEngine.Random.Range(0, possibleCatches.Count - 1)], 1);
		fishingUI.ToggleSuccessNotificationVisibility(true);
		fishingUI.ToggleFishingButtonVisibility();
	}

	private void AddCaughtFishToInventory(InventoryItem fishItem, int number)
	{
		inventory.AddToFirstEmptySlot(fishItem, number);
	}
}