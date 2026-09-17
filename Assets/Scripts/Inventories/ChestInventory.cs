using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevTV.Inventories;
using GameDevTV.Saving;
using RPG.Control;
using RPG.Core;

[RequireComponent(typeof(SaveableEntity))]
public class ChestInventory : InteractableObject, ISaveable
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

	/// <summary>
	/// Добавляет предмет в сундук в количестве 1 штука, автоматически находя
	/// подходящий слот (существующий стек этого же предмета или первый пустой).
	/// Удобно вызывать из UnityEvent, например из ObjectiveReactor.onObjectiveCompleted.
	/// </summary>
	public void AddItem(InventoryItem item)
	{
		AddItem(item, 1);
	}

	/// <summary>
	/// Добавляет предмет в сундук в указанном количестве, автоматически находя
	/// подходящий слот (существующий стек этого же предмета или первый пустой).
	/// Возвращает true, если предмет удалось добавить.
	/// </summary>
	public bool AddItem(InventoryItem item, int number)
	{
		if (item == null || number <= 0)
		{
			Debug.LogWarning($"ChestInventory на {gameObject.name}: попытка добавить null-предмет или неверное количество ({number}).", this);
			return false;
		}

		int slot = FindStackSlot(item);
		if (slot < 0)
		{
			slot = FindEmptySlot();
		}

		if (slot < 0)
		{
			Debug.LogWarning($"ChestInventory на {gameObject.name}: нет свободного места для предмета {item.GetDisplayName()}.", this);
			return false;
		}

		items[slot] = item;
		numbers[slot] += number;
		chestUpdated?.Invoke();
		return true;
	}

	/// <summary>
	/// Добавляет предмет в сундук по его ID. Принимает формат "itemID,quantity"
	/// или просто "itemID" (количество по умолчанию = 1). Удобно для UnityEvent,
	/// когда нет прямой ссылки на ScriptableObject предмета.
	/// </summary>
	public void AddItemByID(string itemIDWithQuantity)
	{
		if (string.IsNullOrEmpty(itemIDWithQuantity))
		{
			Debug.LogError("ChestInventory: ID предмета не может быть пустым.", this);
			return;
		}

		string itemID = itemIDWithQuantity;
		int quantity = 1;

		if (itemIDWithQuantity.Contains(","))
		{
			string[] parts = itemIDWithQuantity.Split(',');
			if (parts.Length != 2 || !int.TryParse(parts[1].Trim(), out quantity))
			{
				Debug.LogError($"ChestInventory: неверный формат '{itemIDWithQuantity}'. Ожидается 'itemID,quantity'.", this);
				return;
			}
			itemID = parts[0].Trim();
		}

		InventoryItem item = InventoryItem.GetFromID(itemID);
		if (item == null)
		{
			Debug.LogError($"ChestInventory: предмет с ID '{itemID}' не найден!", this);
			return;
		}

		AddItem(item, quantity);
	}

	private int FindEmptySlot()
	{
		for (int i = 0; i < size; i++)
		{
			if (items[i] == null) return i;
		}
		return -1;
	}

	private int FindStackSlot(InventoryItem item)
	{
		if (item == null || !item.IsStackable()) return -1;

		for (int i = 0; i < size; i++)
		{
			if (ReferenceEquals(items[i], item)) return i;
		}
		return -1;
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

	public override CursorType GetCursorType()
	{
		return CursorType.Pickup;
	}

	protected override void OnInteract(PlayerController callingController)
	{
		if (chestUI != null)
		{
			chestUI.OpenChest(this);
		}
		else
		{
			Debug.LogWarning("ChestUI не назначен для сундука!");
		}
	}
}
