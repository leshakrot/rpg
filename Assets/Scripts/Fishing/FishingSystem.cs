using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using GameDevTV.Inventories;
using RPG.Core;
using RPG.UI.RequirementText;
using RPG.Stats;
using RPG.Combat;

public enum FishingState
{
	Idle,
	WaitingForBite,
	Biting,
	Catching
}

public class FishingSystem : MonoBehaviour, IAction
{
	[Header("References")]
	[SerializeField] private FishingUI fishingUI;
	[SerializeField] private FishingMiniGame fishingMiniGame;
	[SerializeField] private Inventory inventory;
	[SerializeField] private Experience experience;
	[SerializeField] private Animator playerAnimator;
	[SerializeField] private GameObject fishingRodPrefab;
	[SerializeField] private Transform rightHandTransform;
	[SerializeField] private ActionScheduler actionScheduler;

	[Header("Player Movement")]
	[Tooltip("NavMeshAgent игрока. FishingSystem не на игроке — тащи сюда вручную.")]
	[SerializeField] private NavMeshAgent playerNavMeshAgent;

	[Header("Player Weapon")]
	[Tooltip("PlayerFighter для скрытия оружия во время рыбалки")]
	[SerializeField] private PlayerFighter playerFighter;

	[Header("Bite Settings")]
	[SerializeField] private float minBiteTime = 3f;
	[SerializeField] private float maxBiteTime = 8f;
	[SerializeField] private float biteWindowDuration = 3f;
	[SerializeField] private AudioClip biteSound;
	[SerializeField] private AudioSource audioSource;

	[Header("Reel Sound Settings")]
	[SerializeField] private AudioClip reelSound;
	[SerializeField] private AudioSource reelAudioSource;

	[Header("Result Sounds")]
	[SerializeField] private AudioClip fishCaughtSound;
	[SerializeField] private AudioClip fishEscapedSound;

	private FishingAreaTrigger currentFishingArea;
	private FishData selectedFishData;
	private FishItemData selectedFishItem;
	private FishingState currentState = FishingState.Idle;
	private GameObject currentFishingRod;
	private Coroutine biteCoroutine;

	public FishingState CurrentState => currentState;

	private void Awake()
	{
		if (actionScheduler == null)
			actionScheduler = GetComponent<ActionScheduler>();
	}

	private void OnEnable()
	{
		fishingMiniGame.OnFishingComplete += HandleFishCaught;
		fishingMiniGame.OnFishingFailed += HandleFishingFailed;
	}

	private void OnDisable()
	{
		fishingMiniGame.OnFishingComplete -= HandleFishCaught;
		fishingMiniGame.OnFishingFailed -= HandleFishingFailed;
	}

	public void SetCurrentFishingArea(FishingAreaTrigger fishingArea)
	{
		currentFishingArea = fishingArea;
	}

	// ──────────────────────────────────────────────
	// Публичные методы
	// ──────────────────────────────────────────────

	public void StartFishing()
	{
		if (currentFishingArea == null)
		{
			Debug.LogWarning("Нет активной зоны рыбалки!");
			return;
		}

		if (currentState != FishingState.Idle)
		{
			Debug.LogWarning("Рыбалка уже начата!");
			return;
		}

		selectedFishData = SelectRandomFish(currentFishingArea.GetAvailableFish());
		if (selectedFishData == null)
		{
			Debug.LogWarning("В этой зоне нет доступных рыб!");
			return;
		}

		selectedFishItem = selectedFishData.GetRandomFishItem();
		if (selectedFishItem == null || selectedFishItem.item == null)
		{
			Debug.LogWarning("В FishData нет доступных предметов!");
			return;
		}

		actionScheduler.StartAction(this);
		StopPlayerMovement(true);

		HidePlayerWeapon();
		ShowFishingRod();
		SetFishingAnimation(true, false);

		currentState = FishingState.WaitingForBite;
		fishingUI.ShowCancelButton();
		biteCoroutine = StartCoroutine(WaitForBiteCoroutine());
	}

	public void HookFish()
	{
		if (currentState != FishingState.Biting)
		{
			Debug.LogWarning("Подсечка невозможна в текущем состоянии!");
			return;
		}

		StopBiteCoroutine();
		currentState = FishingState.Catching;
		SetFishingAnimation(true, true);
		fishingUI.ShowCatchingUI();
		fishingMiniGame.StartMiniGame(selectedFishData, selectedFishItem.catchDifficulty, reelAudioSource);
		StartReelSound();
	}

	public void PullFish()
	{
		if (currentState == FishingState.Catching)
			fishingMiniGame.OnPullAction();
	}

	public void ReleasePull()
	{
		if (currentState == FishingState.Catching)
			fishingMiniGame.OnPullRelease();
	}

	/// <summary>
	/// Завершить рыбалку вручную — вызывается и кнопкой в мини-игре UI,
	/// и кнопкой InteractButton "Отменить". Единая точка выхода.
	/// </summary>
	public void StopFishing()
	{
		if (currentState == FishingState.Idle) return;
		actionScheduler.CancelCurrentAction();
	}

	/// <summary>
	/// Реализация IAction. Вызывается ActionScheduler'ом когда стартует другое действие,
	/// а также напрямую через StopFishing().
	/// </summary>
	public void Cancel()
	{
		if (currentState == FishingState.Idle) return;

		InternalCancel();
		StopPlayerMovement(false);

		fishingUI.HideFishingMiniGame();

		if (currentFishingArea != null)
			fishingUI.ShowFishingButton(true);
	}

	// ──────────────────────────────────────────────
	// Обработчики событий мини-игры
	// ──────────────────────────────────────────────

	private void HandleFishCaught()
	{
		StopReelSound();
		PlayFishCaughtSound();
		
		if (selectedFishItem?.item != null)
		{
			inventory.AddToFirstEmptySlot(selectedFishItem.item, 1);
			Debug.Log($"Поймана рыба: {selectedFishItem.item.GetDisplayName()}");
			
			// Добавляем опыт за пойманную рыбу через 1 секунду
			if (experience != null)
			{
				StartCoroutine(AddExperienceDelayed(selectedFishItem.experienceReward));
			}
			
			// Показываем всплывающее уведомление с именем рыбы
			string notificationText = $"Вы поймали {selectedFishItem.item.GetDisplayName()}";
			RequirementTextManager.Show(notificationText);
		}
		EndFishing(success: true);
	}

	private void HandleFishingFailed()
	{
		StopReelSound();
		PlayFishEscapedSound();
		EndFishing(success: false);
	}

	// ──────────────────────────────────────────────
	// Корутины
	// ──────────────────────────────────────────────

	private IEnumerator WaitForBiteCoroutine()
	{
		float waitTime = UnityEngine.Random.Range(minBiteTime, maxBiteTime);
		yield return new WaitForSeconds(waitTime);
		OnBiteOccurred();
	}

	private IEnumerator BiteWindowCoroutine()
	{
		yield return new WaitForSeconds(biteWindowDuration);
		if (currentState == FishingState.Biting)
		{
			Debug.Log("Не успели подсечь!");
			EndFishing(success: false);
		}
	}

	private IEnumerator AddExperienceDelayed(float experienceAmount)
	{
		yield return new WaitForSeconds(1f);
		experience.GainExperience(experienceAmount);
	}

	// ──────────────────────────────────────────────
	// Внутренняя логика
	// ──────────────────────────────────────────────

	private void OnBiteOccurred()
	{
		currentState = FishingState.Biting;

		if (audioSource != null && biteSound != null)
			audioSource.PlayOneShot(biteSound);

		fishingUI.ShowHookButton();
		biteCoroutine = StartCoroutine(BiteWindowCoroutine());
	}

	private void EndFishing(bool success)
	{
		InternalCancel();
		StopPlayerMovement(false);
		actionScheduler.CancelCurrentAction();

		if (success)
			fishingUI.ShowSuccessResult();
		else
			fishingUI.ShowFailureResult();
	}

	/// <summary>
	/// Внутренняя очистка: корутины, мини-игра, удочка, анимация, состояние.
	/// Не трогает UI, NavMeshAgent и ActionScheduler.
	/// </summary>
	private void InternalCancel()
	{
		StopBiteCoroutine();

		if (currentState == FishingState.Catching)
		{
			fishingMiniGame.StopMiniGame();
			StopReelSound();
		}

		HideFishingRod();
		ShowPlayerWeapon();
		SetFishingAnimation(false, false);
		if (playerAnimator != null)
			playerAnimator.SetFloat("forwardSpeed", 0f);

		currentState = FishingState.Idle;
		selectedFishData = null;
		selectedFishItem = null;
	}

	private void StopPlayerMovement(bool stop)
	{
		if (playerNavMeshAgent == null)
		{
			Debug.LogWarning("FishingSystem: playerNavMeshAgent не назначен! Перетащи NavMeshAgent игрока в инспектор.");
			return;
		}

		if (!playerNavMeshAgent.enabled || !playerNavMeshAgent.isOnNavMesh) return;

		playerNavMeshAgent.isStopped = stop;
	}

	private void StopBiteCoroutine()
	{
		if (biteCoroutine != null)
		{
			StopCoroutine(biteCoroutine);
			biteCoroutine = null;
		}
	}

	private void SetFishingAnimation(bool isFishing, bool isCatching)
	{
		if (playerAnimator == null) return;
		playerAnimator.SetBool("IsFishing", isFishing);
		playerAnimator.SetBool("IsCatching", isCatching);
	}

	private FishData SelectRandomFish(List<FishData> availableFish)
	{
		if (availableFish == null || availableFish.Count == 0)
			return null;

		int totalWeight = 0;
		foreach (var fish in availableFish)
			foreach (var item in fish.possibleItems)
				totalWeight += item.dropWeight;

		if (totalWeight == 0)
			return availableFish[0];

		int randomValue = UnityEngine.Random.Range(0, totalWeight);
		int currentWeight = 0;

		foreach (var fish in availableFish)
		{
			foreach (var item in fish.possibleItems)
			{
				currentWeight += item.dropWeight;
				if (randomValue < currentWeight)
					return fish;
			}
		}

		return availableFish[0];
	}

	private void ShowFishingRod()
	{
		if (fishingRodPrefab == null || rightHandTransform == null) return;
		HideFishingRod();
		currentFishingRod = Instantiate(fishingRodPrefab, rightHandTransform);
		currentFishingRod.transform.localPosition = Vector3.zero;
		currentFishingRod.transform.localRotation = Quaternion.identity;
	}

	private void HideFishingRod()
	{
		if (currentFishingRod != null)
		{
			Destroy(currentFishingRod);
			currentFishingRod = null;
		}
	}

	// ──────────────────────────────────────────────
	// Управление звуком сматывания лески
	// ──────────────────────────────────────────────

	private void StartReelSound()
	{
		if (reelAudioSource != null && reelSound != null)
		{
			reelAudioSource.clip = reelSound;
			reelAudioSource.loop = true;
			reelAudioSource.pitch = 1f;
			reelAudioSource.Play();
		}
	}

	private void StopReelSound()
	{
		if (reelAudioSource != null && reelAudioSource.isPlaying)
		{
			reelAudioSource.Stop();
		}
	}

	// ──────────────────────────────────────────────
	// Управление оружием игрока
	// ──────────────────────────────────────────────

	private void HidePlayerWeapon()
	{
		if (playerFighter != null)
		{
			playerFighter.HideWeapon();
		}
	}

	private void ShowPlayerWeapon()
	{
		if (playerFighter != null)
		{
			playerFighter.ShowWeapon();
		}
	}

	// ──────────────────────────────────────────────
	// Звуки результата рыбалки
	// ──────────────────────────────────────────────

	private void PlayFishCaughtSound()
	{
		if (audioSource != null && fishCaughtSound != null)
		{
			audioSource.PlayOneShot(fishCaughtSound);
		}
	}

	private void PlayFishEscapedSound()
	{
		if (audioSource != null && fishEscapedSound != null)
		{
			audioSource.PlayOneShot(fishEscapedSound);
		}
	}
}
