using UnityEngine;
using UnityEngine.Events;
using RPG.Quests;

public class ObjectiveReactor : MonoBehaviour
{
	[Header("Что за цель должна быть выполнена")]
	public string questName;
	public string objectiveReference;

	[Header("Что произойдет при выполнении")]
	public UnityEvent onObjectiveCompleted;

	private bool hasFired = false;
	private QuestList questList;

	void Start()
	{
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

		// Проверяем, уже ли выполнена цель
		if (status.IsObjectiveComplete(objectiveReference))
		{
			Trigger();
			return;
		}

		// Подписываемся на события
		SubscribeToUpdates();
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

		if (status.IsObjectiveComplete(objectiveReference))
		{
			Trigger();
		}
	}

	private void OnObjectiveCompleted(Quest quest, string objRef)
	{
		if (!hasFired && quest.name == questName && objRef == objectiveReference)
		{
			Trigger();
		}
	}

	private void Trigger()
	{
		if (hasFired) return;
		
		hasFired = true;
		onObjectiveCompleted.Invoke();
		
		Debug.Log($"ObjectiveReactor на {gameObject.name}: Цель '{objectiveReference}' квеста '{questName}' выполнена!");
		
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
}
