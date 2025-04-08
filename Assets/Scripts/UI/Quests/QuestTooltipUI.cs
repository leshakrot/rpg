using RPG.Quests;
using System;
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
        [SerializeField] private TextMeshProUGUI _rewardText;
        public void Setup(QuestStatus status)
        {
            Quest quest = status.GetQuest();
            _title.text = quest.GetTitle();
            foreach (Transform item in _objectiveContainer)
            {
                Destroy(item.gameObject);
            }

            foreach (var objective in quest.GetObjectives())
            {
                GameObject prefab = status.IsObjectiveComplete(objective.reference) ? _objectivePrefab : _objectiveIncompletePrefab;
                GameObject objectiveInstance = Instantiate(prefab, _objectiveContainer);
                TextMeshProUGUI objectiveText = objectiveInstance.GetComponentInChildren<TextMeshProUGUI>();

                string progressText = "";
                if (objective.hasProgress && !status.IsObjectiveComplete(objective.reference))
                {
                    int current = status.GetCurrentProgress(objective.reference);
                    progressText = $" ({current}/{objective.requiredCount})";
                }

                objectiveText.text = objective.description + progressText;
            }

            _rewardText.text = GetRewardText(quest);
        }

        private string GetRewardText(Quest quest)
        {
            string rewardText = "";
            foreach(var reward in quest.GetRewards())
            {
                if(rewardText != "")
                {
                    rewardText += ", ";
                }
                if(reward.number > 1)
                {
                    rewardText += reward.number + " ";
                }
                rewardText += reward.item.GetDisplayName();
            }
            if(rewardText == "")
            {
                rewardText = "Нет награды";
            }
            rewardText += ".";
            return rewardText;
        }
    }
}
