using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FishingUI : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField] private InteractButton interactButton;
	[SerializeField] private Sprite interactSprite;
	[SerializeField] private GameObject fishingMiniGamePanel;
	[SerializeField] private FishingSystem fishingSystem;
	[SerializeField] private FishingMiniGame fishingMiniGame;
	[SerializeField] private GameObject successNotification;
	[SerializeField] private GameObject failureNotification;

	private Button interactButtonComponent;
	private EventTrigger eventTrigger;

	private void Start()
	{
		if (interactButton != null)
		{
			interactButton.gameObject.SetActive(false);
			interactButtonComponent = interactButton.GetComponent<Button>();
			// Создаём EventTrigger заранее, чтобы не добавлять/удалять его каждый раз
			eventTrigger = interactButtonComponent.gameObject.GetComponent<EventTrigger>()
				?? interactButtonComponent.gameObject.AddComponent<EventTrigger>();
		}

		SetPanelActive(fishingMiniGamePanel, false);
		SetPanelActive(successNotification, false);
		SetPanelActive(failureNotification, false);
	}

	// ──────────────────────────────────────────────
	// Публичные методы управления состоянием UI
	// ──────────────────────────────────────────────

	/// <summary>
	/// Показать/скрыть кнопку "Ловить рыбу"
	/// </summary>
	public void ShowFishingButton(bool show)
	{
		if (interactButton == null) return;

		if (show)
		{
			ConfigureButton("Ловить рыбу", OnStartFishingClicked, raycastEnabled: true, useHold: false);
			interactButton.gameObject.SetActive(true);
		}
		else
		{
			interactButton.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// Показать кнопку "Подсечь" при поклёвке
	/// </summary>
	public void ShowHookButton()
	{
		if (interactButton == null) return;

		ConfigureButton("Подсечь", OnHookClicked, raycastEnabled: true, useHold: false);
		interactButton.gameObject.SetActive(true);
	}

	/// <summary>
	/// Показать кнопку "Отменить" во время ожидания поклёвки.
	/// RaycastTarget ВКЛЮЧЁН — кнопка должна быть кликабельна.
	/// Клики по navmesh обрабатывает ActionScheduler через IAction.Cancel().
	/// </summary>
	public void ShowCancelButton()
	{
		if (interactButton == null) return;

		ConfigureButton("Отменить", OnCancelClicked, raycastEnabled: true, useHold: false);
		interactButton.gameObject.SetActive(true);
	}

	/// <summary>
	/// Показать UI процесса ловли (мини-игра + кнопка "Тянуть")
	/// </summary>
	public void ShowCatchingUI()
	{
		SetPanelActive(fishingMiniGamePanel, true);

		if (interactButton == null) return;

		// Кнопка "Тянуть" — работает по удержанию (PointerDown/PointerUp)
		// RaycastTarget включён, иначе нажатие не сработает
		SetButtonLabel("Тянуть");
		SetButtonRaycastTarget(true);

		// Очищаем onClick — будем работать через EventTrigger
		interactButtonComponent.onClick.RemoveAllListeners();

		SetupHoldEvents();

		interactButton.gameObject.SetActive(true);
	}

	/// <summary>
	/// Скрыть все элементы рыбалки (при отмене или выходе из зоны)
	/// </summary>
	public void HideFishingMiniGame()
	{
		SetPanelActive(fishingMiniGamePanel, false);
		HideNotifications();

		if (interactButton != null)
		{
			ClearEventTrigger();
			SetButtonRaycastTarget(true);
			interactButtonComponent?.onClick.RemoveAllListeners();
			interactButton.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// Показать уведомление об успешной ловле
	/// </summary>
	public void ShowSuccessResult()
	{
		HideFishingMiniGame();
		SetPanelActive(successNotification, true);
		ShowFishingButton(true);
	}

	/// <summary>
	/// Показать уведомление о неудачной ловле
	/// </summary>
	public void ShowFailureResult()
	{
		HideFishingMiniGame();
		SetPanelActive(failureNotification, true);
		ShowFishingButton(true);
	}

	// ──────────────────────────────────────────────
	// Обработчики кнопок
	// ──────────────────────────────────────────────

	private void OnStartFishingClicked()
	{
		HideNotifications();
		interactButton.gameObject.SetActive(false);
		fishingSystem.StartFishing();
	}

	private void OnHookClicked()
	{
		fishingSystem.HookFish();
	}

	private void OnCancelClicked()
	{
		// CancelFishing() уведомляет ActionScheduler, который вызовет Cancel()
		fishingSystem.CancelFishing();
	}

	// ──────────────────────────────────────────────
	// Вспомогательные методы
	// ──────────────────────────────────────────────

	/// <summary>
	/// Настроить кнопку с одним onClick-обработчиком
	/// </summary>
	private void ConfigureButton(string label, UnityEngine.Events.UnityAction onClick, bool raycastEnabled, bool useHold)
	{
		SetButtonLabel(label);
		SetButtonRaycastTarget(raycastEnabled);

		interactButtonComponent.onClick.RemoveAllListeners();
		ClearEventTrigger();

		if (!useHold)
			interactButtonComponent.onClick.AddListener(onClick);
	}

	private void SetButtonLabel(string label)
	{
		if (interactButton == null) return;
		interactButton.SetIcon(interactSprite);
		interactButton.SetInteractionText(label);
	}

	/// <summary>
	/// Настраивает EventTrigger для удержания кнопки "Тянуть"
	/// </summary>
	private void SetupHoldEvents()
	{
		ClearEventTrigger();

		var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
		down.callback.AddListener(_ => fishingSystem.PullFish());
		eventTrigger.triggers.Add(down);

		var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
		up.callback.AddListener(_ => fishingSystem.ReleasePull());
		eventTrigger.triggers.Add(up);
	}

	private void ClearEventTrigger()
	{
		if (eventTrigger != null)
			eventTrigger.triggers.Clear();
	}

	private void SetButtonRaycastTarget(bool enabled)
	{
		if (interactButton == null) return;

		var graphics = interactButton.GetComponentsInChildren<Graphic>(true);
		foreach (var g in graphics)
			g.raycastTarget = enabled;
	}

	private void HideNotifications()
	{
		SetPanelActive(successNotification, false);
		SetPanelActive(failureNotification, false);
	}

	private static void SetPanelActive(GameObject panel, bool active)
	{
		if (panel != null) panel.SetActive(active);
	}

	// Обратная совместимость
	public void ToggleSuccessNotificationVisibility(bool b) => SetPanelActive(successNotification, b);
	public void ToggleFailureNotificationVisibility(bool b) => SetPanelActive(failureNotification, b);
}
