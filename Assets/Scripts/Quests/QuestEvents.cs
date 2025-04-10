using UnityEngine;
using System;
using RPG.Quests;

public class QuestEvents : MonoBehaviour
{
	public static QuestEvents Instance;

	public event Action<Quest, string> onObjectiveCompleted;

	void Awake()
	{
		if (Instance == null) Instance = this;
	}

	public void ObjectiveCompleted(Quest quest, string objectiveRef)
	{
		onObjectiveCompleted?.Invoke(quest, objectiveRef);
	}
}
