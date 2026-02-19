using UnityEngine;
using GameDevTV.Inventories;
using UnityEngine.Events;

public class ChestUI : MonoBehaviour
{
    public UnityEvent onOpen;

	[SerializeField] GameObject chestSlotPrefab;
	[SerializeField] Transform slotsParent;

	ChestInventory currentChest;

	private void Start()
	{
		gameObject.SetActive(false);
	}
	
	public void OpenChest(ChestInventory chest)
	{
		currentChest = chest;
		gameObject.SetActive(true);

		Redraw();

		onOpen.Invoke();
	}

	public void CloseChest()
	{
		gameObject.SetActive(false);
		currentChest = null;
	}

	void Redraw()
	{
		if (currentChest == null) return; // Проверяем, что сундук назначен

		// Очищаем старые слоты перед обновлением
		foreach (Transform child in slotsParent)
		{
			Destroy(child.gameObject);
		}

		// Создаем слоты для текущего сундука
		int size = currentChest.GetSize();
		for (int i = 0; i < size; i++)
		{
			GameObject slotGO = Instantiate(chestSlotPrefab, slotsParent);
			ChestSlotUI slotUI = slotGO.GetComponent<ChestSlotUI>();
			slotUI.Setup(currentChest, i);
		}
	}

}
