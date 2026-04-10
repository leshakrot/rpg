using UnityEngine;
using RPG.Core;

public class PlayerFishingSystemInteraction : MonoBehaviour
{
	[SerializeField] private FishingUI fishingUI;
	[SerializeField] private FishingSystem fishingSystem;
	[SerializeField] private ActionScheduler actionScheduler;

	private void Awake()
	{
		if (actionScheduler == null)
			actionScheduler = GetComponent<ActionScheduler>();
	}

	public void EnterFishingArea(FishingAreaTrigger fishingArea)
	{
		fishingSystem.SetCurrentFishingArea(fishingArea);

		if (fishingSystem.CurrentState == FishingState.Idle)
			fishingUI.ShowFishingButton(true);
	}

	public void ExitFishingArea()
	{
		// Отменяем через ActionScheduler — он разблокирует движение
		// и сам вызовет fishingSystem.Cancel()
		if (fishingSystem.CurrentState != FishingState.Idle)
			actionScheduler.CancelCurrentAction();

		fishingSystem.SetCurrentFishingArea(null);
		fishingUI.ShowFishingButton(false);
	}
}
