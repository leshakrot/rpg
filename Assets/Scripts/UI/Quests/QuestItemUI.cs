using RPG.Quests;
using TMPro;
using UnityEngine;

namespace RPG.UI.Quests
{
    public class QuestItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _progress;

        private QuestStatus _status;

        public void Setup(QuestStatus status)
        {
            this._status = status;
            
            if (status == null)
            {
                Debug.LogWarning("QuestItemUI: QuestStatus is null!");
                if (_title != null) _title.text = "Unknown Quest";
                if (_progress != null) _progress.text = "0/0";
                return;
            }

            Quest quest = status.GetQuest();
            if (quest == null)
            {
                Debug.LogWarning("QuestItemUI: Quest is null!");
                if (_title != null) _title.text = "Invalid Quest";
                if (_progress != null) _progress.text = "0/0";
                return;
            }

            if (_title != null) _title.text = quest.GetTitle();
            if (_progress != null) _progress.text = status.GetCompletedCount() + "/" + quest.GetObjectiveCount();
        }

        public QuestStatus GetQuestStatus()
        {
            return _status;
        }
    }
}
