using UnityEngine;

namespace RPG.UI.RequirementText
{
	public class RequirementTextManager : MonoBehaviour
	{
		[SerializeField] private RequirementText _requirementTextPrefab = null;

		private static RequirementTextManager instance;
		private RequirementTextSpawner playerSpawner; // Кэшируем спаунер игрока для быстрого доступа

		public static RequirementTextManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<RequirementTextManager>();
					if (instance == null)
					{
						GameObject go = new GameObject("RequirementTextManager");
						instance = go.AddComponent<RequirementTextManager>();
					}
				}
				return instance;
			}
		}

		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else if (instance != this)
			{
				Destroy(gameObject);
				return;
			}
            
			SetupPlayerRequirementTextSpawner();
		}

		// Статический метод для вызова из любого места в коде
		public static void Show(string message)
		{
			if (Instance == null || Instance.playerSpawner == null)
			{
				Debug.LogWarning("RequirementTextManager или его спаунер недоступны.");
				return;
			}
			Instance.playerSpawner.Spawn(message);
		}

		private void SetupPlayerRequirementTextSpawner()
		{
			GameObject player = GameObject.FindWithTag("Player");
			if (player == null)
			{
				Debug.LogWarning("RequirementTextManager: Игрок не найден!");
				return;
			}

			// Ищем или создаем спаунер
			playerSpawner = player.GetComponentInChildren<RequirementTextSpawner>();
			if (playerSpawner != null)
			{
				return;
			}

			GameObject spawnerGO = new GameObject("Requirement Text Spawner");
			spawnerGO.transform.SetParent(player.transform);
			spawnerGO.transform.localPosition = Vector3.zero;
            
			playerSpawner = spawnerGO.AddComponent<RequirementTextSpawner>();

			if (_requirementTextPrefab != null)
			{
				playerSpawner.SetRequirementTextPrefab(_requirementTextPrefab);
			}
			else
			{
				Debug.LogError("RequirementTextManager: Префаб RequirementText не назначен в инспекторе!");
			}
		}
	}
}