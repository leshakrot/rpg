using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RPG.Quests
{
    [System.Serializable]
    public class QuestStatus
    {
        private Quest _quest;
        private List<string> _completedObjectives = new List<string>();
        private List<string> _revealedObjectives = new List<string>();

        // Добавляем прогресс по количеству
        private Dictionary<string, int> _objectiveProgress = new Dictionary<string, int>();
        
        // Отслеживание сработавших ObjectiveReactor-ов
        private HashSet<string> _firedReactors = new HashSet<string>();

        [System.Serializable]
        class QuestStatusRecord
        {
            public string questName;
            public List<string> completedObjectives;
            public List<string> revealedObjectives;
            public Dictionary<string, int> objectiveProgress;
            public List<string> firedReactors;
        }

        public QuestStatus(Quest quest)
        {
            this._quest = quest;
        }

        public QuestStatus(object objectState)
        {
            QuestStatusRecord state = objectState as QuestStatusRecord;
            _quest = Quest.GetByName(state.questName);
            if (_quest == null)
            {
                Debug.LogWarning($"Failed to load quest: {state.questName}. Make sure the quest asset exists in Resources folder.");
            }
            _completedObjectives = state.completedObjectives;
            _revealedObjectives = state.revealedObjectives;
            _objectiveProgress = state.objectiveProgress ?? new Dictionary<string, int>();
            
            // Загружаем список сработавших реакторов
            if (state.firedReactors != null)
            {
                _firedReactors = new HashSet<string>(state.firedReactors);
            }
            else
            {
                _firedReactors = new HashSet<string>();
            }
        }

        public void RevealObjective(string objectiveRef)
        {
            if (!_revealedObjectives.Contains(objectiveRef))
            {
                _revealedObjectives.Add(objectiveRef);
            }
        }

        public bool IsObjectiveRevealed(string reference)
        {
            if (_quest == null) return false;
            var objective = _quest.GetObjectives().FirstOrDefault(o => o.reference == reference);
            if (objective == null) return false;
            return !objective.hiddenInitially || _revealedObjectives.Contains(reference);
        }

        public Quest GetQuest()
        {
            return _quest;
        }

        public int GetCompletedCount()
        {
            return _completedObjectives.Count;
        }

        public bool IsObjectiveComplete(string objective)
        {
            return _completedObjectives.Contains(objective);
        }

        public void CompleteObjective(string objective)
        {
            if (_quest == null) return;
            if (_quest.HasObjective(objective) && !_completedObjectives.Contains(objective))
            {
                _completedObjectives.Add(objective);

                if (QuestEvents.Instance != null)
                {
                    QuestEvents.Instance.ObjectiveCompleted(_quest, objective);
                }
            }
        }

        // 🔼 Добавляем прогресс
        public void IncrementProgress(string objective, int amount = 1)
        {
            if (!_objectiveProgress.ContainsKey(objective))
            {
                _objectiveProgress[objective] = 0;
            }

            _objectiveProgress[objective] += amount;

            var obj = _quest.GetObjective(objective);
            if (obj != null && obj.hasProgress && _objectiveProgress[objective] >= obj.requiredCount)
            {
                CompleteObjective(objective);
            }
        }

        public int GetCurrentProgress(string objective)
        {
            return _objectiveProgress.ContainsKey(objective) ? _objectiveProgress[objective] : 0;
        }

        public void SetProgress(string objectiveRef, int count)
        {
            if (_quest == null) return;
            
            // Инициализируем прогресс, если его нет
            if (!_objectiveProgress.ContainsKey(objectiveRef))
            {
                _objectiveProgress[objectiveRef] = 0;
            }

            // Получаем цель для проверки требований
            var objective = GetQuest().GetObjective(objectiveRef);
            if (objective == null) return;

            // Ограничиваем прогресс требуемым количеством
            int required = objective.hasProgress ? objective.requiredCount : 1;
            int newProgress = Mathf.Min(count, required);
            
            // Устанавливаем новый прогресс
            _objectiveProgress[objectiveRef] = newProgress;
        }

        public bool IsReactorFired(string reactorKey)
        {
            return _firedReactors.Contains(reactorKey);
        }

        public void MarkReactorFired(string reactorKey)
        {
            _firedReactors.Add(reactorKey);
        }

        public object CaptureState()
        {
            if (_quest == null)
            {
                Debug.LogWarning("QuestStatus.CaptureState: _quest is null, skipping save");
                return null;
            }

            QuestStatusRecord state = new QuestStatusRecord();
            state.questName = _quest.name;
            state.completedObjectives = _completedObjectives;
            state.revealedObjectives = _revealedObjectives;
            state.objectiveProgress = _objectiveProgress;
            state.firedReactors = new List<string>(_firedReactors);
            return state;
        }

        public bool IsComplete()
        {
            if (_quest == null) return false;

            int objectivesToComplete = _quest.GetObjectivesToComplete();
            int completedCount = GetCompletedCount();

            if (objectivesToComplete == 0)
            {
                return completedCount >= _quest.GetObjectiveCount();
            }
            
            return completedCount >= objectivesToComplete;
        }
    }
}