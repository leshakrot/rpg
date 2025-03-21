using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevTV.Inventories;
using GameDevTV.Saving;
using RPG.Control;

[RequireComponent(typeof(SaveableEntity))]
public class ChestInventory : MonoBehaviour, ISaveable
{
	[Header("Настройка сундука")]
	[SerializeField] int size = 16;
	[SerializeField] InventoryItem[] initialItems = null;
	[SerializeField] int[] initialNumbers = null;
	
	[SerializeField] ChestUI chestUI;

	InventoryItem[] items;
	int[] numbers;

	public event Action chestUpdated; // Событие для обновления UI

	private void Awake()
	{
		items = new InventoryItem[size];
		numbers = new int[size];

		// Инициализация начальными значениями (если заданы)
		if (initialItems != null && initialNumbers != null)
		{
			int len = Mathf.Min(initialItems.Length, initialNumbers.Length, size);
			for (int i = 0; i < len; i++)
			{
				items[i] = initialItems[i];
				numbers[i] = initialNumbers[i];
			}
		}
	}

	public int GetSize() => size;
	public InventoryItem GetItemInSlot(int slot) => items[slot];
	public int GetNumberInSlot(int slot) => numbers[slot];

	public void AddItemToSlot(int slot, InventoryItem item, int number)
	{
		items[slot] = item;
		numbers[slot] = number;
		chestUpdated?.Invoke();
	}

	public void RemoveFromSlot(int slot, int number)
	{
		if (numbers[slot] <= 0) return;

		numbers[slot] -= number;
		if (numbers[slot] <= 0)
		{
			items[slot] = null;
			numbers[slot] = 0;
		}

		chestUpdated?.Invoke();
	}


	public bool HasSpaceFor(InventoryItem item)
	{
		// Если найдется пустой слот – считаем, что место есть.
		for (int i = 0; i < size; i++)
		{
			if (items[i] == null) return true;
		}
		return false;
	}

	// Реализация ISaveable для интеграции через SaveableEntity
	public object CaptureState()
	{
		string[] itemIDs = new string[size];
		int[] itemNumbers = new int[size];
		for (int i = 0; i < size; i++)
		{
			itemIDs[i] = items[i] ? items[i].GetItemID() : "";
			itemNumbers[i] = numbers[i];
		}
		Dictionary<string, object> chestState = new Dictionary<string, object>();
		chestState["itemIDs"] = itemIDs;
		chestState["itemNumbers"] = itemNumbers;
		return chestState;
	}

	public void RestoreState(object state)
	{
		var chestState = (Dictionary<string, object>)state;
		string[] itemIDs = chestState["itemIDs"] as string[];

		// Возможны нюансы с типами: если сохраненные числа приходят как long[], приводим их к int[]
		object rawNumbers = chestState["itemNumbers"];
		int[] itemNumbers;
		if (rawNumbers is int[])
		{
			itemNumbers = rawNumbers as int[];
		}
		else if (rawNumbers is long[])
		{
			long[] longNums = rawNumbers as long[];
			itemNumbers = new int[longNums.Length];
			for (int i = 0; i < longNums.Length; i++)
			{
				itemNumbers[i] = Convert.ToInt32(longNums[i]);
			}
		}
		else
		{
			// Если формат другой – попробуем привести стандартно
			itemNumbers = chestState["itemNumbers"] as int[];
		}

		for (int i = 0; i < size; i++)
		{
			if (!string.IsNullOrEmpty(itemIDs[i]))
			{
				// Важно: метод GetFromID должен корректно возвращать объект InventoryItem по его ID
				items[i] = InventoryItem.GetFromID(itemIDs[i]);
			}
			else
			{
				items[i] = null;
			}
			numbers[i] = itemNumbers[i];
		}
		chestUpdated?.Invoke();
	}

	private void OnTriggerEnter(Collider other)
	{
		if(other.gameObject.TryGetComponent(out PlayerController player)){
			chestUI.ShowChestOpenButton(true, this);
		}
	}
	
	private void OnTriggerExit(Collider other)
	{
		if(other.gameObject.TryGetComponent(out PlayerController player)){
			chestUI.ShowChestOpenButton(false, this);
		}
	}
	
}
