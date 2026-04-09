using UnityEngine;
using GameDevTV.Inventories;
using System.Collections.Generic;

[System.Serializable]
public class FishItemData
{
	[Tooltip("Предмет, который можно получить")]
	public InventoryItem item;

	[Tooltip("Сложность поимки от 0 (легко) до 1 (очень сложно)")]
	[Range(0f, 1f)]
	public float catchDifficulty = 0.5f;

	[Tooltip("Вес выпадения (чем больше, тем чаще попадается)")]
	[Range(1, 100)]
	public int dropWeight = 50;
}

[CreateAssetMenu(fileName = "New Fish", menuName = "Fishing/Fish Data", order = 0)]
public class FishData : ScriptableObject
{
	[Header("Доступные предметы")]
	[Tooltip("Список предметов с индивидуальными настройками сложности")]
	public List<FishItemData> possibleItems = new List<FishItemData>();

	public void CalculateParameters(float difficulty, out float minSpeed, out float maxSpeed, out float minInterval, out float maxInterval)
	{
		// Если сложность 0 - крючок не двигается
		if (difficulty <= 0f)
		{
			minSpeed = 0f;
			maxSpeed = 0f;
			minInterval = 999f; // Очень большое значение, чтобы направление не менялось
			maxInterval = 999f;
			return;
		}

		// Квадратичная прогрессия для более ощутимой разницы на высоких уровнях
		float difficultySquared = difficulty * difficulty;
		
		// Скорость: от 0 (неподвижно) до 200 (очень быстро)
		minSpeed = Mathf.Lerp(0f, 100f, difficultySquared);
		maxSpeed = Mathf.Lerp(0f, 200f, difficultySquared);

		// Интервалы смены направления: от очень редко до очень часто
		// При низкой сложности - редкие смены (предсказуемо)
		// При высокой сложности - частые смены (хаотично)
		minInterval = Mathf.Lerp(3f, 0.1f, difficultySquared);
		maxInterval = Mathf.Lerp(5f, 0.3f, difficultySquared);
	}

	public FishItemData GetRandomFishItem()
	{
		if (possibleItems == null || possibleItems.Count == 0)
			return null;

		// Рассчитываем общий вес
		int totalWeight = 0;
		foreach (var fishItem in possibleItems)
		{
			totalWeight += fishItem.dropWeight;
		}

		// Выбираем случайное значение
		int randomValue = Random.Range(0, totalWeight);

		// Находим предмет по весу
		int currentWeight = 0;
		foreach (var fishItem in possibleItems)
		{
			currentWeight += fishItem.dropWeight;
			if (randomValue < currentWeight)
			{
				return fishItem;
			}
		}

		// На всякий случай возвращаем первый предмет
		return possibleItems[0];
	}
}
