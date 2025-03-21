using UnityEngine;

public class PlayerFishingSystemInteraction : MonoBehaviour
{
	[SerializeField] private FishingUI fishingUI; // Ссылка на FishingUI

	public void EnterFishingArea()
	{
		// Игрок вошел в зону рыбалки
		fishingUI.ShowFishingButton(true);
	}

	public void ExitFishingArea()
	{
		// Игрок покинул зону рыбалки
		fishingUI.ShowFishingButton(false);
	}
}