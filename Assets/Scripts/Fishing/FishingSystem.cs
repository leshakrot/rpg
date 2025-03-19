using System;
using UnityEngine;
using System.Collections.Generic;

public class FishingSystem : MonoBehaviour
{
	public event Action<string> OnFishCaught; // Событие при поимке рыбы

	[SerializeField] private FishingMiniGame fishingMiniGame; // Мини-игра рыбалки
	[SerializeField] private List<string> possibleCatches; // Возможные находки

	public void StartFishing()
	{
		Debug.Log("Заброс удочки...");

		// Подписываемся на событие завершения мини-игры
		fishingMiniGame.OnFishingComplete += HandleFishCaught;

		// Запускаем мини-игру
		fishingMiniGame.StartMiniGame();
	}

	private void HandleFishCaught(string itemName)
	{
		Debug.Log($"Вы поймали: {itemName}");

		// Отправляем событие о поимке рыбы
		OnFishCaught?.Invoke(itemName);

		// Отписываемся от события, чтобы избежать дублирования
		fishingMiniGame.OnFishingComplete -= HandleFishCaught;
	}
}