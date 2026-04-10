using UnityEngine;
using System.Collections.Generic;

public class FishingAreaTrigger : MonoBehaviour
{
	[SerializeField] private PlayerFishingSystemInteraction playerFishingSystemInteraction;

	[Header("Доступные рыбы в этой зоне")]
	[SerializeField] private List<FishData> availableFish = new List<FishData>();

	public List<FishData> GetAvailableFish() => availableFish;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
			playerFishingSystemInteraction.EnterFishingArea(this);
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
			playerFishingSystemInteraction.ExitFishingArea();
	}
}
