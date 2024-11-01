using RPG.Quests;
using TMPro;
using UnityEngine;

namespace RPG.UI.Quests
{
    public class QuestTooltipUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private Transform _objectiveContainer;
        [SerializeField] private GameObject _objectivePrefab;
        [SerializeField] private GameObject _objectiveIncompletePrefab;
        public void Setup(QuestStatus status)
        {
            Quest quest = status.GetQuest();
            _title.text = quest.GetTitle();
            _objectiveContainer.DetachChildren();
            foreach(string objective in quest.GetObjectives())
            {
                GameObject prefab = _objectiveIncompletePrefab;
                if (status.IsObjectiveComplete(objective))
                {
                    prefab = _objectivePrefab;
                }
                GameObject objectiveInstance = Instantiate(prefab, _objectiveContainer);
                TextMeshProUGUI objectiveText = objectiveInstance.GetComponentInChildren<TextMeshProUGUI>();
                objectiveText.text = objective;
            }
        }
    }
}
