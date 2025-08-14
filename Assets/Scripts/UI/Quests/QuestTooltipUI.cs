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
            	if (!status.IsObjectiveRevealed(objective.reference)) continue;
            	
                GameObject prefab = status.IsObjectiveComplete(objective.reference) ? _objectivePrefab : _objectiveIncompletePrefab;
                GameObject objectiveInstance = Instantiate(prefab, _objectiveContainer);
                TextMeshProUGUI objectiveText = objectiveInstance.GetComponentInChildren<TextMeshProUGUI>();

                string progressText = "";
                
                // Показываем прогресс если:
                // 1. Цель имеет обычный прогресс (hasProgress = true)
                // 2. Цель на сбор предметов (isCollectionObjective = true)
                bool shouldShowProgress = (objective.hasProgress || objective.isCollectionObjective) && 
                                        !status.IsObjectiveComplete(objective.reference);
                
                if (shouldShowProgress)
                {
                    int current = status.GetCurrentProgress(objective.reference);
                    int required = objective.requiredCount;
                    
                    // Для целей сбора предметов, если requiredCount не установлен, используем 1
                    if (objective.isCollectionObjective && required <= 0)
                    {
                        required = 1;
                    }
                    
                    progressText = $" ({current}/{required})";
                }

                objectiveText.text = objective.description + progressText;
            }

            _rewardText.text = GetRewardText(quest);
        }

        private string GetRewardText(Quest quest)
        {
            string rewardText = "";
            
            // Показываем только видимые награды (не тайные)
            foreach(var reward in quest.GetVisibleRewards())
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
            
            // Добавляем подсказку о тайных наградах, если они есть
            if(QuestRewardHelper.HasSecretRewards(quest))
            {
                if(rewardText != "")
                {
                    rewardText += ", ";
                }
                rewardText += "???";
            }
            
            if(rewardText == "")
            {
                rewardText = "Нет наград";
            }
            rewardText += ".";
            return rewardText;
        }
    }
}