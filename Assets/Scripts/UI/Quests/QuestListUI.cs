using RPG.Quests;
using RPG.UI.Quests;
using UnityEngine;

namespace RPG.UI.Quests
{
    public class QuestListUI : MonoBehaviour
    {
        [SerializeField] private Quest[] _tempQuests;
        [SerializeField] private QuestItemUI _questPrefab;

        private void Start()
        {
            transform.DetachChildren();
            foreach(Quest quest in _tempQuests)
            {
                QuestItemUI uiInstance = Instantiate<QuestItemUI>(_questPrefab, transform);
                uiInstance.Setup(quest);
            }
        }
    }
}
