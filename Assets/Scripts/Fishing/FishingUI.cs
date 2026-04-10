using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

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
	private Coroutine _deferredHideCoroutine;

	private void Start()
	{
		if (interactButton != null)
		{
			interactButton.gameObject.SetActive(false);
			interactButtonComponent = interactButton.GetComponent<Button>();
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

	public void ShowFishingButton(bool show)
	{
		if (interactButton == null) return;

		if (show)
		{
			CancelDeferredHide();
			ConfigureButton("Ловить рыбу", OnStartFishingClicked, raycastEnabled: true, useHold: false);
			interactButton.gameObject.SetActive(true);
		}
		else
		{
			HideInteractButtonDeferred();
		}
	}

	public void ShowHookButton()
	{
		if (interactButton == null) return;

		CancelDeferredHide();
		ConfigureButton("Подсечь", OnHookClicked, raycastEnabled: true, useHold: false);
		interactButton.gameObject.SetActive(true);
	}

	/// <summary>
	/// Кнопка "Отменить" во время ожидания поклёвки.
	/// Поведение идентично кнопке "Завершить рыбалку" в мини-игре UI —
	/// оба вызывают fishingSystem.StopFishing().
	/// </summary>
	public void ShowCancelButton()
	{
		if (interactButton == null) return;

		CancelDeferredHide();
		ConfigureButton("Отменить", OnStopFishingClicked, raycastEnabled: true, useHold: false);
		interactButton.gameObject.SetActive(true);
	}

	public void ShowCatchingUI()
	{
		SetPanelActive(fishingMiniGamePanel, true);

		if (interactButton == null) return;

		CancelDeferredHide();
		SetButtonLabel("Тянуть");
		SetButtonRaycastTarget(true);
		interactButtonComponent.onClick.RemoveAllListeners();
		SetupHoldEvents();
		interactButton.gameObject.SetActive(true);
	}

	/// <summary>
	/// Скрыть все элементы рыбалки (при отмене или выходе из зоны).
	/// InteractButton скрывается отложенно — через кадр после того как
	/// MouseButtonUp успеет сбросить PlayerController._isDraggingUI.
	/// Иначе кнопка исчезает в том же кадре что и клик, isDraggingUI
	/// застревает в true и игрок не может двигаться.
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
			HideInteractButtonDeferred();
		}
	}

	public void ShowSuccessResult()
	{
		HideFishingMiniGame();
		SetPanelActive(successNotification, true);
		ShowFishingButton(true);
	}

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
		HideInteractButtonDeferred();
		fishingSystem.StartFishing();
	}

	private void OnHookClicked()
	{
		fishingSystem.HookFish();
	}

	private void OnStopFishingClicked()
	{
		fishingSystem.StopFishing();
	}

	// ──────────────────────────────────────────────
	// Отложенное скрытие кнопки
	// ──────────────────────────────────────────────

	/// <summary>
	/// Скрываем кнопку через два кадра — чтобы в текущем кадре успели
	/// отработать MouseButtonUp и сброс _isDraggingUI в PlayerController.
	/// Один кадр иногда не хватает если Update PlayerController идёт раньше.
	/// </summary>
	private void HideInteractButtonDeferred()
	{
		CancelDeferredHide();
		_deferredHideCoroutine = StartCoroutine(HideAfterFrames(2));
	}

	private IEnumerator HideAfterFrames(int frames)
	{
		for (int i = 0; i < frames; i++)
			yield return null;

		if (interactButton != null)
			interactButton.gameObject.SetActive(false);

		_deferredHideCoroutine = null;
	}

	private void CancelDeferredHide()
	{
		if (_deferredHideCoroutine != null)
		{
			StopCoroutine(_deferredHideCoroutine);
			_deferredHideCoroutine = null;
		}
	}

	// ──────────────────────────────────────────────
	// Вспомогательные методы
	// ──────────────────────────────────────────────

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
