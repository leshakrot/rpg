using RPG.Quests;
using TMPro;
using UnityEngine;

namespace RPG.UI.Quests
{
    public class QuestItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _progress;

        private Quest _quest;

        public void Setup(Quest quest)
        {
            this._quest = quest;
            _title.text = quest.GetTitle();
            _progress.text = "0/" + quest.GetObjectiveCount();
        }

        public Quest GetQuest()
        {
            return _quest;
        }
    }
}
