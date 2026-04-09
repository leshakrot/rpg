using UnityEngine;
using RPG.Control;
using RPG.Core;
using System.Collections.Generic;

public class FishingAreaTrigger : MonoBehaviour
{
	[SerializeField] private PlayerFishingSystemInteraction playerFishingSystemInteraction;

	[Header("Доступные рыбы в этой зоне")]
	[Tooltip("Список рыб, которые можно поймать в этой зоне")]
	[SerializeField] private List<FishData> availableFish = new List<FishData>();

	public List<FishData> GetAvailableFish()
	{
		return availableFish;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			playerFishingSystemInteraction.EnterFishingArea(this);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			playerFishingSystemInteraction.ExitFishingArea();
		}
	}
}