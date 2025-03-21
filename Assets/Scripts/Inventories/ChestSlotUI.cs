using UnityEngine;
using GameDevTV.Core.UI.Dragging;
using GameDevTV.Inventories;
using GameDevTV.UI.Inventories;

public class ChestSlotUI : MonoBehaviour, IItemHolder, IDragContainer<InventoryItem>
{
	[SerializeField] InventoryItemIcon icon = null;
	int index;
	ChestInventory chestInventory;

	public void Setup(ChestInventory chest, int slotIndex)
	{
		chestInventory = chest;
		index = slotIndex;
		Redraw();

		// Подписываемся на событие обновления сундука, чтобы перерисовывать слот при изменениях.
		chestInventory.chestUpdated += Redraw;
	}

	public int MaxAcceptable(InventoryItem item)
	{
		// Если слот пустой, то можно добавить любое количество (ограничения можно доработать, если нужны стеки).
		return chestInventory.GetItemInSlot(index) == null ? int.MaxValue : 0;
	}

	public void AddItems(InventoryItem item, int number)
	{
		chestInventory.AddItemToSlot(index, item, number);
		Redraw();
	}

	public InventoryItem GetItem() => chestInventory.GetItemInSlot(index);
	public int GetNumber() => chestInventory.GetNumberInSlot(index);

	private bool isRemoving = false; // Флаг защиты от двойного вызова

	public void RemoveItems(int number)
	{
		if (number <= 0 || isRemoving) return; // Защита от удаления 0 предметов и повторного вызова

		isRemoving = true; // Активируем флаг
		InventoryItem removedItem = GetItem(); // Запоминаем предмет до удаления
		int removedNumber = number; // Используем переданное число

		Debug.Log($"[ChestSlotUI] Удаление предмета: {removedItem?.name}, количество: {removedNumber}");

		chestInventory.RemoveFromSlot(index, number);
		Redraw(); // Обновляем UI

		// Дропаем предмет ТОЛЬКО если он выбрасывается, а не переносится в инвентарь
		if (IsDroppingOutsideInventory())
		{
			Debug.Log($"[ChestSlotUI] Выбрасывание предмета на землю: {removedItem?.name}, количество: {removedNumber}");
			var player = GameObject.FindGameObjectWithTag("Player");
			player.GetComponent<ItemDropper>().DropItem(removedItem, removedNumber);
		}

		isRemoving = false; // Сбрасываем флаг после выполнения
	}


	private bool IsDroppingOutsideInventory()
	{
		bool isOutside = transform.parent == null || transform.parent.GetComponent<ChestUI>() != null;
		Debug.Log($"[ChestSlotUI] Проверка дропа: {isOutside}");
		return isOutside;
	}

	void Redraw()
	{
		if (icon == null) return; // Добавляем проверку на null
		icon.SetItem(GetItem(), GetNumber());
	}
}
