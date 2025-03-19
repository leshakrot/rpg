using UnityEngine;

public class FishingAreaTrigger : MonoBehaviour
{
	[SerializeField] private PlayerFishingSystemInteraction playerFishingSystemInteraction;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			playerFishingSystemInteraction.EnterFishingArea();
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