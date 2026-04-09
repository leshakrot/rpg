using UnityEngine;

public class PlayerFishingSystemInteraction : MonoBehaviour
{
	[SerializeField] private FishingUI fishingUI; // Ссылка на FishingUI
	[SerializeField] private FishingSystem fishingSystem; // Ссылка на FishingSystem

	private FishingAreaTrigger currentFishingArea;

	public void EnterFishingArea(FishingAreaTrigger fishingArea)
	{
		// Игрок вошел в зону рыбалки
		currentFishingArea = fishingArea;
		fishingSystem.SetCurrentFishingArea(fishingArea);
		fishingUI.ShowFishingButton(true);
	}

	public void ExitFishingArea()
	{
		// Игрок покинул зону рыбалки
		currentFishingArea = null;
		fishingSystem.SetCurrentFishingArea(null);
		fishingUI.ShowFishingButton(false);
	}
}