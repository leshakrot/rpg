using UnityEngine;
using UnityEngine.UI;

public class FishingUI : MonoBehaviour
{
	[SerializeField] private Button fishingButton; // Кнопка "Ловить рыбу"
	[SerializeField] private GameObject fishingMiniGamePanel; // Панель мини-игры
	[SerializeField] private FishingMiniGame fishingMiniGame; // Мини-игра рыбалки

	private void Start()
	{
		// Инициализация
		if (fishingButton != null)
			fishingButton.gameObject.SetActive(false);

		if (fishingMiniGamePanel != null)
			fishingMiniGamePanel.SetActive(false);
	}

	public void ShowFishingButton(bool show)
	{
		if (fishingButton != null)
			fishingButton.gameObject.SetActive(show);
	}

	public void StartFishingMiniGame()
	{
		if (fishingMiniGamePanel != null)
		{
			fishingMiniGamePanel.SetActive(true);

			// Запускаем мини-игру через FishingMiniGame
			fishingMiniGame.StartMiniGame();
		}
		fishingButton.gameObject.SetActive(false);
	}

	public void HideFishingMiniGame()
	{
		if (fishingMiniGamePanel != null)
			fishingMiniGamePanel.SetActive(false);
	}
}