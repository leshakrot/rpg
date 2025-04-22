using GameDevTV.Inventories;
using GameDevTV.Saving;
using GameDevTV.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Quests
{
	public class QuestList : MonoBehaviour, ISaveable, IPredicateEvaluator
	{
		private List<QuestStatus> _statuses = new List<QuestStatus>();

		public event Action onUpdate;

		private void Update()
		{
			CompleteObjectivesByPredicates();
			RevealObjectivesByConditions();
		}

		public void AddQuest(Quest quest)
		{
			if (HasQuest(quest)) return;
			QuestStatus newStatus = new QuestStatus(quest);
			_statuses.Add(newStatus);
			if(onUpdate != null)
			{
				onUpdate();
			}              
		}

		public bool HasQuest(Quest quest)
		{
			return GetQuestStatus(quest) != null;
		}

		public IEnumerable<QuestStatus> GetStatuses()
		{
			return _statuses;
		}

		public void CompleteObjective(Quest quest, string objective)
		{
			QuestStatus status = GetQuestStatus(quest);
			status.CompleteObjective(objective);
			if (status.IsComplete())
			{
				GiveReward(quest);
			}
			if (onUpdate != null)
			{
				onUpdate();
			}
		}

		public void AddProgress(Quest quest, string objectiveRef, int amount = 1)
		{
			QuestStatus status = GetQuestStatus(quest);
			if (status == null) return;

			status.IncrementProgress(objectiveRef, amount);

			if (status.IsComplete())
			{
				GiveReward(quest);
			}

			onUpdate?.Invoke();
		}


		public QuestStatus GetQuestStatus(Quest quest)
		{
			foreach (QuestStatus status in _statuses)
			{
				if (status.GetQuest() == quest)
				{
					return status;
				}
			}
			return null;
		}

		private void GiveReward(Quest quest)
		{
			foreach(var reward in quest.GetRewards())
			{
				bool success = GetComponent<Inventory>().AddToFirstEmptySlot(reward.item, reward.number);
				if (!success)
				{
					GetComponent<ItemDropper>().DropItem(reward.item, reward.number);
				}
			}
		}

		private void CompleteObjectivesByPredicates()
		{
			foreach (QuestStatus status in _statuses)
			{
				if (status.IsComplete()) continue;
				Quest quest = status.GetQuest();
				foreach (var objective in quest.GetObjectives())
				{
					if (status.IsObjectiveComplete(objective.reference)) continue;
					if (!objective.usesCondition) continue;
					if (objective.completionCondition.Check(GetComponents<IPredicateEvaluator>()))
					{
						CompleteObjective(quest, objective.reference);
					}
				}
			}
		}
        
		private void RevealObjectivesByConditions()
		{
			foreach (QuestStatus status in _statuses)
			{
				if (status.IsComplete()) continue;

				Quest quest = status.GetQuest();

				foreach (var objective in quest.GetObjectives())
				{
					if (!objective.hiddenInitially) continue;
					if (status.IsObjectiveRevealed(objective.reference)) continue;
					if (objective.revealCondition != null && 
						objective.revealCondition.Check(GetComponents<IPredicateEvaluator>()))
					{
						status.RevealObjective(objective.reference);
						onUpdate?.Invoke(); // чтобы обновить UI
					}
				}
			}
		}

		public object CaptureState()
		{
			List<object> state = new List<object>();
			foreach(QuestStatus status in _statuses)
			{
				state.Add(status.CaptureState());
			}
			return state;
		}

		public void RestoreState(object state)
		{
			List<object> stateList = state as List<object>;
			if (stateList == null) return;

			_statuses.Clear();

			foreach (object objectState in stateList)
			{
				_statuses.Add(new QuestStatus(objectState));               
			}
		}

		public bool? Evaluate(string predicate, string[] parameters)
		{
			switch (predicate)
			{
			case "HasQuest":
				Quest questHas = Quest.GetByName(parameters[0]);
				return questHas != null && HasQuest(questHas);

			case "CompletedQuest":
				Quest questCompleted = Quest.GetByName(parameters[0]);
				if (questCompleted == null) return false;

				QuestStatus status = GetQuestStatus(questCompleted);
				return status != null && status.IsComplete();
			case "CompletedObjective":
				if (parameters.Length < 1) return false;
				string objectiveRef = parameters[0];
				foreach (var objectiveStatus in GetComponent<QuestList>().GetStatuses())
				{
					if (objectiveStatus.IsObjectiveComplete(objectiveRef))
						return true;
				}
				return false;
			}
			return null;
		}
	}
}