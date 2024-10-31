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
        public void Setup(Quest quest)
        {
            _title.text = quest.GetTitle();
            _objectiveContainer.DetachChildren();
            foreach(string objective in quest.GetObjectives())
            {
                GameObject objectiveInstance = Instantiate(_objectivePrefab, _objectiveContainer);
                TextMeshProUGUI objectiveText = objectiveInstance.GetComponentInChildren<TextMeshProUGUI>();
                objectiveText.text = objective;
            }
        }
    }
}
