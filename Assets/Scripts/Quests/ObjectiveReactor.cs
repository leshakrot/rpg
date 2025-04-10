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

	void Start()
	{
		QuestList questList = FindObjectOfType<QuestList>();
		if (questList == null) return;

		Quest quest = Quest.GetByName(questName);
		if (quest == null) return;

		QuestStatus status = questList.GetQuestStatus(quest);
		if (status == null) return;

		// Уже выполнено?
		if (status.IsObjectiveComplete(objectiveReference))
		{
			Trigger();
			return;
		}

		// Подписка (если реализована система событий — см. ниже)
		QuestEvents questEvents = FindObjectOfType<QuestEvents>();
		if (questEvents != null)
		{
			questEvents.onObjectiveCompleted += (q, objRef) =>
			{
				if (!hasFired && q.name == questName && objRef == objectiveReference)
				{
					Trigger();
				}
			};
		}
	}

	private void Trigger()
	{
		hasFired = true;
		onObjectiveCompleted.Invoke();
	}
}
