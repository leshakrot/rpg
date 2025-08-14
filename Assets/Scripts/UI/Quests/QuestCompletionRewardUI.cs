using RPG.Quests;
using TMPro;
using UnityEngine;
using System.Linq;

namespace RPG.UI.Quests
{
    /// <summary>
    /// UI компонент для отображения всех наград (включая тайные) при завершении квеста
    /// </summary>
    public class QuestCompletionRewardUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private TextMeshProUGUI _secretRewardHeader;
        
        /// <summary>
        /// Отображает все награды квеста при его завершении
        /// </summary>
        /// <param name="quest">Завершенный квест</param>
        public void ShowCompletionRewards(Quest quest)
        {
            string allRewardsText = GetAllRewardsText(quest);
            _rewardText.text = allRewardsText;
            
            // Показываем заголовок о тайных наградах, если они есть
            if (_secretRewardHeader != null)
            {
                bool hasSecrets = QuestRewardHelper.HasSecretRewards(quest);
                _secretRewardHeader.gameObject.SetActive(hasSecrets);
                
                if (hasSecrets)
                {
                    int secretCount = QuestRewardHelper.GetSecretRewardCount(quest);
                    _secretRewardHeader.text = secretCount == 1 
                        ? "Бонусная награда:" 
                        : "Бонусные награды:";
                }
            }
        }
        
        /// <summary>
        /// Отображает награды с разделением на обычные и тайные
        /// </summary>
        /// <param name="quest">Завершенный квест</param>
        public void ShowDetailedCompletionRewards(Quest quest)
        {
            string visibleRewards = GetRewardString(quest.GetVisibleRewards());
            string secretRewards = GetRewardString(quest.GetSecretRewards());
            
            string fullText = "";
            
            if (!string.IsNullOrEmpty(visibleRewards))
            {
                fullText += "Награды: " + visibleRewards;
            }
            
            if (!string.IsNullOrEmpty(secretRewards))
            {
                if (!string.IsNullOrEmpty(fullText))
                    fullText += "\n\n";
                    
                fullText += "Бонусные награды: " + secretRewards;
            }
            
            if (string.IsNullOrEmpty(fullText))
            {
                fullText = "Нет наград.";
            }
            
            _rewardText.text = fullText;
            
            // Скрываем заголовок, так как информация уже в основном тексте
            if (_secretRewardHeader != null)
            {
                _secretRewardHeader.gameObject.SetActive(false);
            }
        }
        
        private string GetAllRewardsText(Quest quest)
        {
            string rewardText = GetRewardString(quest.GetRewards());
            return string.IsNullOrEmpty(rewardText) ? "Нет наград." : rewardText;
        }
        
        private string GetRewardString(System.Collections.Generic.IEnumerable<Quest.Reward> rewards)
        {
            string rewardText = "";
            
            foreach(var reward in rewards)
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
            
            return rewardText;
        }
    }
}