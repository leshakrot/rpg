using System.Collections.Generic;
using UnityEngine;

namespace RPG.Quests
{
    public class QuestGiver : MonoBehaviour
    {
        [Tooltip("List of quests this NPC can give")]
        [SerializeField] private List<Quest> _quests = new List<Quest>();

        /// <summary>
        /// Gives the first available quest to the player
        /// </summary>
        public void GiveQuest()
        {
            if (_quests.Count == 0) return;
            GiveQuest(0);
        }

        /// <summary>
        /// Gives a specific quest by index
        /// </summary>
        /// <param name="questIndex">Index of the quest in the _quests list</param>
        public void GiveQuest(int questIndex)
        {
            if (questIndex < 0 || questIndex >= _quests.Count) return;
            
            QuestList questList = GameObject.FindGameObjectWithTag("Player").GetComponent<QuestList>();
            if (questList != null && _quests[questIndex] != null)
            {
                questList.AddQuest(_quests[questIndex]);
            }
        }

        /// <summary>
        /// Gives a specific quest by Quest reference
        /// </summary>
        /// <param name="quest">The quest to give</param>
        public void GiveQuest(Quest quest)
        {
            if (quest == null || !_quests.Contains(quest)) return;
            
            QuestList questList = GameObject.FindGameObjectWithTag("Player").GetComponent<QuestList>();
            if (questList != null)
            {
                questList.AddQuest(quest);
            }
        }

        /// <summary>
        /// Returns all quests this NPC can give
        /// </summary>
        public IReadOnlyList<Quest> GetQuests()
        {
            return _quests.AsReadOnly();
        }
    }
}
