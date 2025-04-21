using UnityEngine;
using RPG.Quests;

namespace RPG.Dialogue
{
    public class DialogueAction : MonoBehaviour
    {
        [SerializeField] private Quest _questToStart;

        public void StartQuest()
        {
            QuestList questList = GameObject.FindGameObjectWithTag("Player").GetComponent<QuestList>();
            if (questList != null)
            {
                questList.AddQuest(_questToStart);
            }
        }
    }
} 