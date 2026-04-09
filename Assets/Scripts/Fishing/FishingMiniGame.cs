using System;
using UnityEngine;
using UnityEngine.UI;

public class FishingMiniGame : MonoBehaviour
{
	[SerializeField] private RectTransform fishingBar; // Полоса поклёвки
	[SerializeField] private RectTransform targetZone; // Целевая зона
	[SerializeField] private RectTransform fishingIcon; // Иконка рыбы
	[SerializeField] private Image progressFill; // Заполняемый бар для таймера

	[SerializeField] private float requiredHoldTime = 2f; // Время, которое нужно удерживать
	[SerializeField] private float gravity = 100f; // Скорость падения
	[SerializeField] private float smoothSpeed = 30f; // Скорость подъёма

	private float holdTimer = 0f; // Таймер удержания
	private bool isPlaying = false; // Флаг активности мини-игры
	private bool isHolding = false; // Флаг удержания кнопки

	private float minY, maxY; // Границы движения

	// Параметры текущей рыбы
	private float minIconSpeed = 30f;
	private float maxIconSpeed = 80f;
	private float minDirectionChangeInterval = 0.5f;
	private float maxDirectionChangeInterval = 2f;

	private float currentIconSpeed; // Текущая скорость иконки
	private float iconVelocityY = 1f; // Направление движения иконки (1 = вверх, -1 = вниз)

	private float directionChangeTimer = 0f; // Таймер смены направления

	public event Action OnFishingComplete; // Событие завершения рыбалки

	private void Start()
	{
		// Инициализация границ движения
		if (fishingBar == null)
		{
			Debug.LogError("FishingBar не назначен!");
			return;
		}

		minY = fishingBar.rect.yMin;
		maxY = fishingBar.rect.yMax;

		// Устанавливаем начальную позицию целевой зоны
		targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, minY);
	}

	private void Update()
	{
		if (!isPlaying) return;

		// Логика движения целевой зоны
		UpdateTargetZonePosition();

		// Логика движения иконки рыбы
		UpdateFishingIconPosition();

		// Проверяем нажатие кнопки
		if (Input.GetKeyDown(KeyCode.Space)) isHolding = true;
		if (Input.GetKeyUp(KeyCode.Space)) isHolding = false;

		// Проверяем, находится ли иконка в целевой зоне
		CheckTargetZone();
	}

	public void StartMiniGame(FishData fishData, float difficulty)
	{
		if (isPlaying)
		{
			Debug.LogWarning("Мини-игра уже запущена!");
			return;
		}

		// Применяем параметры сложности из FishData с учетом индивидуальной сложности предмета
		if (fishData != null)
		{
			fishData.CalculateParameters(difficulty, out minIconSpeed, out maxIconSpeed, out minDirectionChangeInterval, out maxDirectionChangeInterval);

			Debug.Log($"Запуск мини-игры с параметрами: Difficulty={difficulty:F2}, Speed [{minIconSpeed:F1}-{maxIconSpeed:F1}], Interval [{minDirectionChangeInterval:F2}-{maxDirectionChangeInterval:F2}]");
		}

		ResetGame();
		Debug.Log("Запуск мини-игры...");
		isPlaying = true;
		holdTimer = 0f;
	}

	private void UpdateTargetZonePosition()
	{
		// Получаем текущую позицию целевой зоны
		float currentY = targetZone.anchoredPosition.y;

		// Определяем направление движения
		if (isHolding)
		{
			// Если пробел зажат, двигаем целевую зону вверх
			currentY += smoothSpeed * Time.deltaTime;
		}
		else
		{
			// Если пробел отпущен, двигаем целевую зону вниз
			currentY -= gravity * Time.deltaTime;
		}

		// Вычисляем границы целевой зоны
		float targetHalfHeight = targetZone.rect.height / 2f; // Половина высоты целевой зоны
		float targetTop = currentY + targetHalfHeight; // Верхняя граница целевой зоны
		float targetBottom = currentY - targetHalfHeight; // Нижняя граница целевой зоны

		// Ограничение по границам
		if (targetBottom < minY) currentY = minY + targetHalfHeight;
		if (targetTop > maxY) currentY = maxY - targetHalfHeight;

		// Устанавливаем новую позицию
		targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, currentY);
	}

	private void UpdateFishingIconPosition()
	{
		// Если скорость 0 - крючок не двигается
		if (maxIconSpeed <= 0f)
		{
			return;
		}

		// Уменьшаем таймер смены направления
		directionChangeTimer -= Time.deltaTime;

		// Если таймер истёк, меняем направление и скорость
		if (directionChangeTimer <= 0f)
		{
			iconVelocityY = UnityEngine.Random.value > 0.5f ? 1f : -1f; // Случайное направление
			currentIconSpeed = UnityEngine.Random.Range(minIconSpeed, maxIconSpeed); // Случайная скорость
			directionChangeTimer = UnityEngine.Random.Range(minDirectionChangeInterval, maxDirectionChangeInterval); // Новый интервал
		}

		// Получаем текущую позицию иконки
		float currentY = fishingIcon.anchoredPosition.y;

		// Двигаем иконку в текущем направлении
		currentY += iconVelocityY * currentIconSpeed * Time.deltaTime;

		// Вычисляем границы иконки
		float iconHalfHeight = fishingIcon.rect.height / 2f; // Половина высоты иконки
		float iconTop = currentY + iconHalfHeight; // Верхняя граница иконки
		float iconBottom = currentY - iconHalfHeight; // Нижняя граница иконки

		// Ограничение по границам
		if (iconBottom < minY)
		{
			currentY = minY + iconHalfHeight; // Корректируем позицию, чтобы нижняя граница совпала с minY
			iconVelocityY = 1f; // Меняем направление на "вверх"
		}

		if (iconTop > maxY)
		{
			currentY = maxY - iconHalfHeight; // Корректируем позицию, чтобы верхняя граница совпала с maxY
			iconVelocityY = -1f; // Меняем направление на "вниз"
		}

		// Устанавливаем новую позицию
		fishingIcon.anchoredPosition = new Vector2(fishingIcon.anchoredPosition.x, currentY);
	}

	private void CheckTargetZone()
	{
		// Проверяем, находится ли иконка в целевой зоне
		float iconPos = fishingIcon.anchoredPosition.y;
		float targetMin = targetZone.anchoredPosition.y - targetZone.rect.height / 2;
		float targetMax = targetZone.anchoredPosition.y + targetZone.rect.height / 2;

		if (iconPos >= targetMin && iconPos <= targetMax)
		{
			// Увеличиваем таймер удержания
			holdTimer += Time.deltaTime;
		}
		else
		{
			// Уменьшаем таймер плавно
			holdTimer = Mathf.Max(0, holdTimer - Time.deltaTime * 2f);
		}

		// Обновляем прогресс
		progressFill.fillAmount = holdTimer / requiredHoldTime;

		if (holdTimer >= requiredHoldTime)
		{
			CompleteFishing();
		}
	}

	private void CompleteFishing()
	{
		isPlaying = false;
		isHolding = false;
		Debug.Log("Рыба поймана!");

		// Отправляем событие о завершении рыбалки
		OnFishingComplete?.Invoke();

		// Сбрасываем игру
		ResetGame();
	}

	private void ResetGame()
	{
		holdTimer = 0f;
		progressFill.fillAmount = 0f;
		isHolding = false;

		// Сбрасываем позицию целевой зоны
		if (targetZone != null)
		{
			targetZone.anchoredPosition = new Vector2(targetZone.anchoredPosition.x, minY);
		}

		// Сбрасываем позицию иконки рыбы
		if (fishingIcon != null)
		{
			fishingIcon.anchoredPosition = new Vector2(fishingIcon.anchoredPosition.x, 0f);
		}
	}
}