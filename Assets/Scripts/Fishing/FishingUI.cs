using UnityEngine;
using UnityEngine.UI;

public class FishingUI : MonoBehaviour
{
	[SerializeField] private Button fishingButton; // Кнопка "Ловить рыбу"
	[SerializeField] private GameObject fishingMiniGamePanel; // Панель мини-игры
	[SerializeField] private FishingMiniGame fishingMiniGame; // Мини-игра рыбалки
    [SerializeField] private GameObject successNotification;
    [SerializeField] private GameObject failureNotification;

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
		ToggleFishingButtonVisibility();
		ToggleSuccessNotificationVisibility(false);
		ToggleFailureNotificationVisibility(false);
    }

	public void HideFishingMiniGame()
	{
		if (fishingMiniGamePanel != null)
			fishingMiniGamePanel.SetActive(false);

        ToggleSuccessNotificationVisibility(false);
        ToggleFailureNotificationVisibility(false);
    }

	public void ToggleFishingButtonVisibility()
	{
        fishingButton.gameObject.SetActive(!fishingButton.gameObject.activeSelf);
    }

	public void ToggleSuccessNotificationVisibility(bool b)
	{
		successNotification.SetActive(b);
	}

    public void ToggleFailureNotificationVisibility(bool b)
    {
        failureNotification.SetActive(b);
    }
}