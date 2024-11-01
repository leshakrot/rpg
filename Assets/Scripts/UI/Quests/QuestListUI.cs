using RPG.Quests;
using RPG.UI.Quests;
using UnityEngine;

namespace RPG.UI.Quests
{
    public class QuestListUI : MonoBehaviour
    {
        [SerializeField] private QuestItemUI _questPrefab;
        private QuestList _questList;

        private void Start()
        {
            _questList = GameObject.FindGameObjectWithTag("Player").GetComponent<QuestList>();
            _questList.onUpdate += Redraw;
            Redraw();
        }

        private void Redraw()
        {
            transform.DetachChildren();
            foreach (QuestStatus status in _questList.GetStatuses())
            {
                QuestItemUI uiInstance = Instantiate<QuestItemUI>(_questPrefab, transform);
                uiInstance.Setup(status);
            }
        }
    }
}
