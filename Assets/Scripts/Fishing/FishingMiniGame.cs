using System;
using UnityEngine;
using UnityEngine.UI;

public class FishingMiniGame : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField] private RectTransform fishingBar;
	[SerializeField] private RectTransform targetZone;
	[SerializeField] private RectTransform fishingIcon;
	[SerializeField] private Image progressFill;

	[Header("Settings")]
	[SerializeField] private float requiredHoldTime = 2f;
	[SerializeField] private float gravity = 100f;
	[SerializeField] private float smoothSpeed = 30f;
	[SerializeField] private bool canFail = false;
	[SerializeField] private float failThreshold = 0f;

	public event Action OnFishingComplete;
	public event Action OnFishingFailed;

	// Параметры сложности текущей рыбы
	private float minIconSpeed = 30f;
	private float maxIconSpeed = 80f;
	private float minDirectionChangeInterval = 0.5f;
	private float maxDirectionChangeInterval = 2f;

	private float holdTimer = 0f;
	private bool isPlaying = false;
	private bool isHolding = false;

	private float minY, maxY;
	private bool boundsInitialized = false;

	private float currentIconSpeed;
	private float iconVelocityY = 1f;
	private float directionChangeTimer = 0f;

	private void Awake()
	{
		// Сбрасываем прогресс при старте
		if (progressFill != null)
			progressFill.fillAmount = 0f;
	}

	private void OnEnable()
	{
		// Инициализируем границы при включении — layout к этому моменту уже готов
		InitializeBounds();
	}

	private void InitializeBounds()
	{
		if (fishingBar == null) return;

		// Используем Canvas.ForceUpdateCanvases чтобы получить актуальные размеры
		Canvas.ForceUpdateCanvases();

		minY = fishingBar.rect.yMin;
		maxY = fishingBar.rect.yMax;
		boundsInitialized = true;
	}

	private void Update()
	{
		if (!isPlaying) return;

		UpdateTargetZonePosition();
		UpdateFishingIconPosition();

		// Клавиатурный ввод (ПК)
		if (Input.GetKeyDown(KeyCode.Space)) isHolding = true;
		if (Input.GetKeyUp(KeyCode.Space)) isHolding = false;

		CheckTargetZone();
	}

	/// <summary>
	/// Зажать — вызывается из UI (PointerDown)
	/// </summary>
	public void OnPullAction()
	{
		isHolding = true;
	}

	/// <summary>
	/// Отпустить — вызывается из UI (PointerUp)
	/// </summary>
	public void OnPullRelease()
	{
		isHolding = false;
	}

	public void StartMiniGame(FishData fishData, float difficulty)
	{
		if (isPlaying)
		{
			Debug.LogWarning("Мини-игра уже запущена!");
			return;
		}

		if (!boundsInitialized)
			InitializeBounds();

		if (fishData != null)
		{
			fishData.CalculateParameters(difficulty, out minIconSpeed, out maxIconSpeed,
				out minDirectionChangeInterval, out maxDirectionChangeInterval);
		}

		ResetGame();
		isPlaying = true;
	}

	/// <summary>
	/// Останавливает мини-игру без результата (отмена)
	/// </summary>
	public void StopMiniGame()
	{
		if (!isPlaying) return;

		isPlaying = false;
		isHolding = false;
		ResetGame();
	}

	private void UpdateTargetZonePosition()
	{
		float currentY = targetZone.anchoredPosition.y;

		currentY += isHolding
			? smoothSpeed * Time.deltaTime
			: -gravity * Time.deltaTime;

		float half = targetZone.rect.height / 2f;
		currentY = Mathf.Clamp(currentY, minY + half, maxY - half);

		targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, currentY);
	}

	private void UpdateFishingIconPosition()
	{
		if (maxIconSpeed <= 0f) return;

		directionChangeTimer -= Time.deltaTime;

		if (directionChangeTimer <= 0f)
		{
			iconVelocityY = UnityEngine.Random.value > 0.5f ? 1f : -1f;
			currentIconSpeed = UnityEngine.Random.Range(minIconSpeed, maxIconSpeed);
			directionChangeTimer = UnityEngine.Random.Range(minDirectionChangeInterval, maxDirectionChangeInterval);
		}

		float currentY = fishingIcon.anchoredPosition.y;
		currentY += iconVelocityY * currentIconSpeed * Time.deltaTime;

		float half = fishingIcon.rect.height / 2f;

		if (currentY - half < minY)
		{
			currentY = minY + half;
			iconVelocityY = 1f;
		}
		else if (currentY + half > maxY)
		{
			currentY = maxY - half;
			iconVelocityY = -1f;
		}

		fishingIcon.anchoredPosition = new Vector2(fishingIcon.anchoredPosition.x, currentY);
	}

	private void CheckTargetZone()
	{
		float iconPos = fishingIcon.anchoredPosition.y;
		float half = targetZone.rect.height / 2f;
		float targetMin = targetZone.anchoredPosition.y - half;
		float targetMax = targetZone.anchoredPosition.y + half;

		if (iconPos >= targetMin && iconPos <= targetMax)
			holdTimer += Time.deltaTime;
		else
			holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 2f);

		progressFill.fillAmount = holdTimer / requiredHoldTime;

		if (holdTimer >= requiredHoldTime)
		{
			CompleteFishing();
		}
		else if (canFail && holdTimer <= failThreshold && progressFill.fillAmount > 0f)
		{
			// Проваливаем только если игрок уже набирал прогресс и потерял весь
			FailFishing();
		}
	}

	private void CompleteFishing()
	{
		isPlaying = false;
		isHolding = false;
		ResetGame();
		OnFishingComplete?.Invoke();
	}

	private void FailFishing()
	{
		isPlaying = false;
		isHolding = false;
		ResetGame();
		OnFishingFailed?.Invoke();
	}

	private void ResetGame()
	{
		holdTimer = 0f;
		isHolding = false;

		if (progressFill != null)
			progressFill.fillAmount = 0f;

		if (targetZone != null && boundsInitialized)
		{
			float half = targetZone.rect.height / 2f;
			targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, minY + half);
		}

		if (fishingIcon != null)
		{
			fishingIcon.anchoredPosition = new Vector2(fishingIcon.anchoredPosition.x, 0f);
		}

		// Сбрасываем таймер смены направления
		directionChangeTimer = 0f;
		iconVelocityY = 1f;
	}
}
