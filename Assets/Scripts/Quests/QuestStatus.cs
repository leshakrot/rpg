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

        [System.Serializable]
        class QuestStatusRecord
        {
            public string questName;
	        public List<string> completedObjectives;
	        public List<string> revealedObjectives;
            public Dictionary<string, int> objectiveProgress;
        }

        public QuestStatus(Quest quest)
        {
            this._quest = quest;
        }

        public QuestStatus(object objectState)
        {
            QuestStatusRecord state = objectState as QuestStatusRecord;
            _quest = Quest.GetByName(state.questName);
	        _completedObjectives = state.completedObjectives;
	        _revealedObjectives = state.revealedObjectives;
            _objectiveProgress = state.objectiveProgress ?? new Dictionary<string, int>();
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

        public object CaptureState()
        {
            QuestStatusRecord state = new QuestStatusRecord();
            state.questName = _quest.name;
	        state.completedObjectives = _completedObjectives;
	        state.revealedObjectives = _revealedObjectives;
            state.objectiveProgress = _objectiveProgress;
            return state;
        }

        public bool IsComplete()
        {
            foreach (var objective in _quest.GetObjectives())
            {
                if (!_completedObjectives.Contains(objective.reference))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
