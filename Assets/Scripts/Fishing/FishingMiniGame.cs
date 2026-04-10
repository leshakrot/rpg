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

	[Header("Tension Settings")]
	[Tooltip("Время до срыва рыбы когда леска на максимуме (в секундах)")]
	[SerializeField] private float tensionMaxTime = 2f;
	[Tooltip("Множитель скорости заполнения tension относительно скорости откатки прогресса")]
	[SerializeField] private float tensionFillMultiplier = 5f;
	[Tooltip("Цвет progressFill в нормальном состоянии")]
	[SerializeField] private Color normalColor = Color.green;
	[Tooltip("Цвет progressFill в состоянии натяжения (леска вот-вот лопнет)")]
	[SerializeField] private Color tensionColor = Color.red;

	[Header("Reel Sound Settings")]
	[Tooltip("Базовый pitch звука сматывания лески")]
	[SerializeField] private float basePitch = 1f;
	[Tooltip("Целевой pitch при нажатии кнопки")]
	[SerializeField] private float targetPitch = 1.3f;
	[Tooltip("Скорость изменения pitch")]
	[SerializeField] private float pitchChangeSpeed = 2f;

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

	// ── Tension state ──
	// tensionTimer накапливается пока holdTimer == 0 и иконка вне зоны.
	// При попадании иконки в зону — сбрасывается.
	private float tensionTimer = 0f;
	private bool isTense = false;   // true когда holdTimer упал до 0 и иконка по-прежнему вне зоны

	// ── Reel sound pitch ──
	private float currentPitch = 1f;
	private AudioSource reelAudioSource;

	private void Awake()
	{
		if (progressFill != null)
		{
			progressFill.fillAmount = 0f;
			progressFill.color = normalColor;
		}
	}

	private void OnEnable()
	{
		InitializeBounds();
	}

	private void InitializeBounds()
	{
		if (fishingBar == null) return;

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
		UpdateReelPitch();

		// Клавиатурный ввод (ПК)
		if (Input.GetKeyDown(KeyCode.Space)) isHolding = true;
		if (Input.GetKeyUp(KeyCode.Space)) isHolding = false;

		CheckTargetZone();
	}

	/// <summary>Зажать — вызывается из UI (PointerDown)</summary>
	public void OnPullAction()
	{
		isHolding = true;
	}

	/// <summary>Отпустить — вызывается из UI (PointerUp)</summary>
	public void OnPullRelease()
	{
		isHolding = false;
	}

	public void StartMiniGame(FishData fishData, float difficulty, AudioSource audioSource = null)
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

		// Устанавливаем AudioSource если передан
		if (audioSource != null)
			reelAudioSource = audioSource;

		ResetGame();
		isPlaying = true;
		currentPitch = basePitch;
	}

	/// <summary>Останавливает мини-игру без результата (отмена)</summary>
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

		bool iconInZone = iconPos >= targetMin && iconPos <= targetMax;

		if (iconInZone)
		{
			// Иконка поймана — нормальный прогресс
			holdTimer += Time.deltaTime;

			// Если до этого была натяжка — откатываем её с той же скоростью
			if (isTense)
			{
				tensionTimer = Mathf.Max(0f, tensionTimer - Time.deltaTime * tensionFillMultiplier);
				
				// Обновляем прогресс-бар tension
				progressFill.fillAmount = tensionTimer / tensionMaxTime;
				
				// Если tension полностью откатился — выходим из tension-режима
				if (tensionTimer <= 0f)
				{
					isTense = false;
					SetProgressColor(normalColor);
				}
			}
		}
		else
		{
			// Иконка вне зоны — откатываем прогресс
			holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 2f);

			if (holdTimer <= 0f)
			{
				// Прогресс на нуле — переходим / остаёмся в tension-режиме
				if (!isTense)
				{
					isTense = true;
					tensionTimer = 0f;
					SetProgressColor(tensionColor);
				}

				// Tension заполняется в tensionFillMultiplier раз быстрее чем откатка
				tensionTimer += Time.deltaTime * tensionFillMultiplier;

				progressFill.fillAmount = tensionTimer / tensionMaxTime;

				if (tensionTimer >= tensionMaxTime)
				{
					FailFishing();
					return;
				}
			}
		}

		// В нормальном режиме (не tension) обновляем основной прогресс
		if (!isTense)
			progressFill.fillAmount = holdTimer / requiredHoldTime;

		if (holdTimer >= requiredHoldTime)
		{
			CompleteFishing();
			return;
		}

		// Старая логика canFail (оставляем для обратной совместимости)
		if (canFail && !isTense && holdTimer <= failThreshold && progressFill.fillAmount > 0f)
		{
			FailFishing();
		}
	}

	private void SetProgressColor(Color color)
	{
		if (progressFill != null)
			progressFill.color = color;
	}

	private void UpdateReelPitch()
	{
		if (reelAudioSource == null) return;

		float target = isHolding ? targetPitch : basePitch;
		currentPitch = Mathf.Lerp(currentPitch, target, pitchChangeSpeed * Time.deltaTime);
		reelAudioSource.pitch = currentPitch;
	}

	public void SetReelPitchTarget(float newTargetPitch)
	{
		targetPitch = newTargetPitch;
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
		tensionTimer = 0f;
		isTense = false;
		isHolding = false;

		if (progressFill != null)
		{
			progressFill.fillAmount = 0f;
			progressFill.color = normalColor;
		}

		if (targetZone != null && boundsInitialized)
		{
			float half = targetZone.rect.height / 2f;
			targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, minY + half);
		}

		if (fishingIcon != null)
		{
			fishingIcon.anchoredPosition = new Vector2(fishingIcon.anchoredPosition.x, 0f);
		}

		directionChangeTimer = 0f;
		iconVelocityY = 1f;
	}
}
