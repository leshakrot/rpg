using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using RPG.Quests;

public class ObjectiveReactor : MonoBehaviour
{
	[Header("Что за цель должна быть выполнена")]
	public string questName;
	public string objectiveReference;

	[Header("Что произойдет при выполнении")]
	public UnityEvent onObjectiveCompleted;

	[Header("Идентификатор реактора")]
	[SerializeField] private string reactorId;

	private bool hasFired = false;
	private QuestList questList;

	void Start()
	{
		// Генерируем уникальный ID если его нет
		if (string.IsNullOrEmpty(reactorId))
		{
			reactorId = GenerateReactorId();
		}

		questList = FindObjectOfType<QuestList>();
		if (questList == null)
		{
			Debug.LogError($"ObjectiveReactor на {gameObject.name}: QuestList не найден в сцене!");
			return;
		}

		Quest quest = Quest.GetByName(questName);
		if (quest == null)
		{
			Debug.LogError($"ObjectiveReactor на {gameObject.name}: Квест '{questName}' не найден!");
			return;
		}

		QuestStatus status = questList.GetQuestStatus(quest);
		
		// Если квест еще не взят, ждем его добавления
		if (status == null)
		{
			SubscribeToUpdates();
			return;
		}

		// Проверяем, уже ли этот реактор срабатывал
		if (status.IsReactorFired(reactorId))
		{
			hasFired = true;
			Debug.Log($"ObjectiveReactor на {gameObject.name}: Реактор {reactorId} уже срабатывал ранее, пропускаем.");
			return;
		}

		// Проверяем, уже ли выполнена цель
		if (status.IsObjectiveComplete(objectiveReference))
		{
			Trigger();
			return;
		}

		// Подписываемся на события
		SubscribeToUpdates();
	}

	private string GenerateReactorId()
	{
		// Создаем уникальный ID на основе имени объекта, квеста и цели
		string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		return $"{sceneName}_{gameObject.name}_{questName}_{objectiveReference}";
	}

	private void SubscribeToUpdates()
	{
		// Подписка на общие обновления квестов
		if (questList != null)
		{
			questList.onUpdate += CheckObjectiveStatus;
		}

		// Подписка на конкретные события завершения целей
		if (QuestEvents.Instance != null)
		{
			QuestEvents.Instance.onObjectiveCompleted += OnObjectiveCompleted;
		}
	}

	private void CheckObjectiveStatus()
	{
		if (hasFired) return;

		Quest quest = Quest.GetByName(questName);
		if (quest == null) return;

		QuestStatus status = questList.GetQuestStatus(quest);
		if (status == null) return;

		// Проверяем, не срабатывал ли уже этот реактор
		if (status.IsReactorFired(reactorId))
		{
			hasFired = true;
			UnsubscribeFromUpdates();
			return;
		}

		if (status.IsObjectiveComplete(objectiveReference))
		{
			Trigger();
		}
	}

	private void OnObjectiveCompleted(Quest quest, string objRef)
	{
		if (!hasFired && quest.name == questName && objRef == objectiveReference)
		{
			QuestStatus status = questList.GetQuestStatus(quest);
			if (status != null && !status.IsReactorFired(reactorId))
			{
				Trigger();
			}
		}
	}

	private void Trigger()
	{
		if (hasFired) return;
		
		hasFired = true;
		
		// Отмечаем реактор как сработавший в QuestStatus
		Quest quest = Quest.GetByName(questName);
		if (quest != null)
		{
			QuestStatus status = questList.GetQuestStatus(quest);
			if (status != null)
			{
				status.MarkReactorFired(reactorId);
			}
		}
		
		onObjectiveCompleted.Invoke();
		
		Debug.Log($"ObjectiveReactor на {gameObject.name}: Цель '{objectiveReference}' квеста '{questName}' выполнена! ID реактора: {reactorId}");
		
		// Отписываемся от событий
		UnsubscribeFromUpdates();
	}

	private void UnsubscribeFromUpdates()
	{
		if (questList != null)
		{
			questList.onUpdate -= CheckObjectiveStatus;
		}

		if (QuestEvents.Instance != null)
		{
			QuestEvents.Instance.onObjectiveCompleted -= OnObjectiveCompleted;
		}
	}

	void OnDestroy()
	{
		UnsubscribeFromUpdates();
	}

#if UNITY_EDITOR
	[ContextMenu("Сгенерировать новый ID реактора")]
	private void RegenerateReactorId()
	{
		reactorId = GenerateReactorId();
		Debug.Log($"Новый ID реактора: {reactorId}");
	}
#endif
}
