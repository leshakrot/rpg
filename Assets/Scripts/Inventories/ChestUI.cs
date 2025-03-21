using UnityEngine;
using UnityEngine.UI;
using GameDevTV.Inventories;

public class ChestUI : MonoBehaviour
{
	[SerializeField] InteractButton interactButton;
	[SerializeField] Sprite interactSprite;
	[SerializeField] GameObject chestSlotPrefab;
	[SerializeField] Transform slotsParent;

	ChestInventory currentChest;

	private void Start()
	{
		if (interactButton != null)
			interactButton.gameObject.SetActive(false);
			
		gameObject.SetActive(false);
	}
	
	public void ShowChestOpenButton(bool show, ChestInventory chest)
	{
		if (interactButton != null)
		{
			interactButton.SetIcon(interactSprite);
			interactButton.SetInteractionText("Открыть сундук");

			Button button = interactButton.gameObject.GetComponent<Button>();
			button.onClick.RemoveAllListeners(); // Очищаем предыдущие слушатели перед добавлением нового

			if (show) 
			{
				button.onClick.AddListener(() => OpenChest(chest)); // Добавляем новый слушатель только если show == true
			}

			interactButton.gameObject.SetActive(show);
		}
	}

	// Вызывается из скрипта взаимодействия с сундуком (например, когда игрок нажимает клавишу "E")
	public void OpenChest(ChestInventory chest)
	{
		currentChest = chest;
		gameObject.SetActive(true);
		interactButton.gameObject.SetActive(false);

		// Центрируем UI по экрану
		//RectTransform rect = GetComponent<RectTransform>();
		//rect.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);

		Redraw();
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
