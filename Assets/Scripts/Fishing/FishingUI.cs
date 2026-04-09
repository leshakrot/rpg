using UnityEngine;
using UnityEngine.UI;

public class FishingUI : MonoBehaviour
{
	[SerializeField] private InteractButton interactButton;
	[SerializeField] private Sprite interactSprite;
	[SerializeField] private GameObject fishingMiniGamePanel; // Панель мини-игры
	[SerializeField] private FishingSystem fishingSystem; // Ссылка на систему рыбалки
    [SerializeField] private GameObject successNotification;
    [SerializeField] private GameObject failureNotification;

    private void Start()
	{
		// Инициализация
		if (interactButton != null)
			interactButton.gameObject.SetActive(false);

		if (fishingMiniGamePanel != null)
			fishingMiniGamePanel.SetActive(false);
	}

	public void ShowFishingButton(bool show)
	{
		if (interactButton != null)
		{
			interactButton.SetIcon(interactSprite);
			interactButton.SetInteractionText("Ловить рыбу");
			interactButton.gameObject.GetComponent<Button>().onClick.AddListener(StartFishingMiniGame);
			interactButton.gameObject.SetActive(show);
		}
	}

	public void StartFishingMiniGame()
	{
		if (fishingMiniGamePanel != null)
		{
			fishingMiniGamePanel.SetActive(true);

			// Запускаем рыбалку через FishingSystem (он сам запустит мини-игру с нужными параметрами)
			fishingSystem.StartFishing();
		}
		ShowFishingButton(false);
		ToggleSuccessNotificationVisibility(false);
		ToggleFailureNotificationVisibility(false);
    }

	public void HideFishingMiniGame()
	{
		if (fishingMiniGamePanel != null)
		{
			interactButton.SetIcon(null);
			interactButton.SetInteractionText(null);
			interactButton.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
			fishingMiniGamePanel.SetActive(false);	
		}
		ShowFishingButton(true);
        ToggleSuccessNotificationVisibility(false);
        ToggleFailureNotificationVisibility(false);
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