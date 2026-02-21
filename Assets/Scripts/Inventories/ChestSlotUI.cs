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

		chestInventory.chestUpdated += Redraw;
	}

	public int MaxAcceptable(InventoryItem item)
	{
		// Если слот занят тем же стекируемым предметом — принимаем
		var existingItem = chestInventory.GetItemInSlot(index);
		if (existingItem != null)
		{
			if (object.ReferenceEquals(existingItem, item) && item.IsStackable())
				return int.MaxValue;
			return 0;
		}
		return int.MaxValue;
	}

	public void AddItems(InventoryItem item, int number)
	{
		chestInventory.AddItemToSlot(index, item, number);
		Redraw();
	}

	public InventoryItem GetItem() => chestInventory.GetItemInSlot(index);
	public int GetNumber() => chestInventory.GetNumberInSlot(index);

	public void RemoveItems(int number)
	{
		// Просто удаляем из слота — фреймворк DragItem сам переместит предмет в нужный контейнер
		chestInventory.RemoveFromSlot(index, number);
		Redraw();
	}

	void Redraw()
	{
		if (icon == null) return;
		icon.SetItem(GetItem(), GetNumber());
	}
}
