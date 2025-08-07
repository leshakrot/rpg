using System;
using UnityEngine;
using UnityEngine.Events;

namespace RPG.Dialogue
{
    public class DialogueTrigger : MonoBehaviour
    {
        [Serializable]
        public class DialogueAction
        {
            public string actionName;
            public UnityEvent onTrigger;
        }

        [Tooltip("List of actions this trigger can perform")]
        [SerializeField] private DialogueAction[] _actions;

        /// <summary>
        /// Triggers the action with the specified name
        /// </summary>
        /// <param name="actionToTrigger">Name of the action to trigger</param>
        public void Trigger(string actionToTrigger)
        {
            if (string.IsNullOrEmpty(actionToTrigger) || _actions == null) return;

            foreach (var action in _actions)
            {
                if (action.actionName == actionToTrigger)
                {
                    action.onTrigger?.Invoke();
                    return;
                }
            }
            
            // Fallback to old behavior for backward compatibility
            if (_actions.Length > 0 && _actions[0].actionName == actionToTrigger)
            {
                _actions[0].onTrigger?.Invoke();
            }
        }

        /// <summary>
        /// Triggers a quest by index from the QuestGiver component
        /// </summary>
        /// <param name="questIndex">Index of the quest to give (0-based)</param>
        public void TriggerQuest(int questIndex)
        {
            var questGiver = GetComponent<Quests.QuestGiver>();
            if (questGiver != null)
            {
                questGiver.GiveQuest(questIndex);
            }
            else
            {
                Debug.LogWarning($"No QuestGiver component found on {gameObject.name}");
            }
        }
    }
}
