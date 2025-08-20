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

		private Inventory _inventory;

		private void Awake()
		{
			_inventory = GetComponent<Inventory>();
		}

		private void OnEnable()
		{
			if (_inventory != null)
			{
				// Подписываемся на событие "инвентарь обновился"
				_inventory.inventoryUpdated += CheckItemCollectionObjectives;
			}
		}

		private void OnDisable()
		{
			if (_inventory != null)
			{
				// Всегда отписываемся, чтобы избежать утечек памяти
				_inventory.inventoryUpdated -= CheckItemCollectionObjectives;
			}
		}

		// Этот метод будет автоматически вызываться при любом изменении инвентаря.
		private void CheckItemCollectionObjectives()
		{
			bool hasChanges = false;
			
			foreach (var status in _statuses)
			{
				if (status == null || status.GetQuest() == null) continue;
				if (status.IsComplete()) continue;

				foreach (var objective in status.GetQuest().GetObjectives())
				{
					// Нас интересуют только активные цели по сбору предметов
					if (!objective.isCollectionObjective || objective.itemToCollect == null) continue;
					if (status.IsObjectiveComplete(objective.reference)) continue;

					// Получаем актуальное количество предметов из инвентаря
					int currentItemCount = _inventory.GetItemCount(objective.itemToCollect);
					int previousProgress = status.GetCurrentProgress(objective.reference);

					// Устанавливаем прогресс квеста равным количеству предметов
					status.SetProgress(objective.reference, currentItemCount);
					
					// Проверяем, изменился ли прогресс
					if (currentItemCount != previousProgress)
					{
						hasChanges = true;
					}

					// Проверяем завершение цели
					if (objective.hasProgress && currentItemCount >= objective.requiredCount)
					{
						if (!status.IsObjectiveComplete(objective.reference))
						{
							status.CompleteObjective(objective.reference);
							hasChanges = true;
						}
					}
				}
			}
			
			// Вызываем общее событие обновления только если что-то изменилось
			if (hasChanges && onUpdate != null)
			{
				onUpdate();
			}
		}

		private void Update()
		{
			CompleteObjectivesByPredicates();
			RevealObjectivesByConditions();
			
			// Также проверяем прогресс сбора предметов в Update для обновления UI
			UpdateCollectionProgress();
		}
		
		// Новый метод для обновления прогресса без дублирования логики
		private void UpdateCollectionProgress()
		{
			foreach (var status in _statuses)
			{
				if (status == null || status.GetQuest() == null) continue;
				if (status.IsComplete()) continue;

				foreach (var objective in status.GetQuest().GetObjectives())
				{
					if (!objective.isCollectionObjective || objective.itemToCollect == null) continue;
					if (status.IsObjectiveComplete(objective.reference)) continue;

					int currentItemCount = _inventory.GetItemCount(objective.itemToCollect);
					int currentProgress = status.GetCurrentProgress(objective.reference);
					
					// Обновляем прогресс если он отличается от количества предметов в инвентаре
					if (currentItemCount != currentProgress)
					{
						status.SetProgress(objective.reference, currentItemCount);
					}
				}
			}
		}

		public void AddQuest(Quest quest)
		{
			if (HasQuest(quest)) return;
			QuestStatus newStatus = new QuestStatus(quest);
			_statuses.Add(newStatus);
			if (onUpdate != null)
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
			foreach (var reward in quest.GetRewards())
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
				if (status == null || status.GetQuest() == null) continue;
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
				if (status == null || status.GetQuest() == null) continue;
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
			foreach (QuestStatus status in _statuses)
			{
				if (status != null && status.GetQuest() != null)
				{
					object statusState = status.CaptureState();
					if (statusState != null)
					{
						state.Add(statusState);
					}
				}
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
				QuestStatus questStatus = new QuestStatus(objectState);
				// Добавляем только валидные квесты
				if (questStatus != null && questStatus.GetQuest() != null)
				{
					_statuses.Add(questStatus);
				}
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