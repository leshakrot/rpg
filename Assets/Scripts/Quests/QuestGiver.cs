using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPG.Quests
{
    public class QuestGiver : MonoBehaviour
    {
        [Tooltip("List of quests this NPC can give")]
        [SerializeField] private List<Quest> _quests = new List<Quest>();

        /// <summary>
        /// Gives the first available quest to the player
        /// </summary>
        public void GiveQuest()
        {
            if (_quests.Count == 0) return;
            GiveQuest(0);
        }

        /// <summary>
        /// Gives a specific quest by index
        /// </summary>
        /// <param name="questIndex">Index of the quest in the _quests list</param>
        public void GiveQuest(int questIndex)
        {
            if (questIndex < 0 || questIndex >= _quests.Count) return;
            
            QuestList questList = GameObject.FindGameObjectWithTag("Player").GetComponent<QuestList>();
            if (questList != null && _quests[questIndex] != null)
            {
                questList.AddQuest(_quests[questIndex]);
            }
        }

        /// <summary>
        /// Gives a specific quest by Quest reference
        /// </summary>
        /// <param name="quest">The quest to give</param>
        public void GiveQuest(Quest quest)
        {
            if (quest == null || !_quests.Contains(quest)) return;
            
            QuestList questList = GameObject.FindGameObjectWithTag("Player").GetComponent<QuestList>();
            if (questList != null)
            {
                questList.AddQuest(quest);
            }
        }

        /// <summary>
        /// Returns all quests this NPC can give
        /// </summary>
        public IReadOnlyList<Quest> GetQuests()
        {
            return _quests.AsReadOnly();
        }

        /// <summary>
        /// Возвращает текст с описанием наград для показа игроку до принятия квеста
        /// </summary>
        /// <param name="questIndex">Индекс квеста в списке</param>
        public string GetQuestPreviewText(int questIndex)
        {
            if (questIndex < 0 || questIndex >= _quests.Count || _quests[questIndex] == null) 
                return "Нет квеста для выдачи.";
            
            Quest quest = _quests[questIndex];
            string description = $"Квест: {quest.GetTitle()}\n\n";
            
            // Показываем только видимые награды
            var visibleRewards = quest.GetVisibleRewards();
            bool hasVisibleRewards = visibleRewards.Any();
            
            if (hasVisibleRewards)
            {
                description += "Награды: ";
                description += string.Join(", ", visibleRewards.Select(r => 
                    r.number > 1 ? $"{r.number} {r.item.GetDisplayName()}" : r.item.GetDisplayName()));
            }
            else
            {
                description += "Нет явных наград";
            }
            
            // Добавляем намек на тайные награды
            if (QuestRewardHelper.HasSecretRewards(quest))
            {
                description += "\n\n✨ Этот квест может содержать дополнительные сюрпризы...";
            }
            
            return description;
        }
        
        /// <summary>
        /// Возвращает текст со всеми наградами при завершении квеста
        /// </summary>
        /// <param name="questIndex">Индекс квеста в списке</param>
        public string GetQuestCompletionText(int questIndex)
        {
            if (questIndex < 0 || questIndex >= _quests.Count || _quests[questIndex] == null) 
                return "Квест не найден.";
            
            Quest quest = _quests[questIndex];
            string completionText = $"Квест '{quest.GetTitle()}' выполнен!\n\n";
            
            var visibleRewards = quest.GetVisibleRewards();
            var secretRewards = quest.GetSecretRewards();
            
            bool hasVisibleRewards = visibleRewards.Any();
            bool hasSecretRewards = secretRewards.Any();
            
            if (hasVisibleRewards)
            {
                completionText += "Награды: ";
                completionText += string.Join(", ", visibleRewards.Select(r => 
                    r.number > 1 ? $"{r.number} {r.item.GetDisplayName()}" : r.item.GetDisplayName()));
                completionText += "\n";
            }
            
            if (hasSecretRewards)
            {
                completionText += "\n🎉 Бонусные награды: ";
                completionText += string.Join(", ", secretRewards.Select(r => 
                    r.number > 1 ? $"{r.number} {r.item.GetDisplayName()}" : r.item.GetDisplayName()));
                completionText += "\n";
            }
            
            if (!hasVisibleRewards && !hasSecretRewards)
            {
                completionText += "Нет материальных наград, но вы получили ценный опыт!";
            }
            
            return completionText;
        }
        
        /// <summary>
        /// Проверяет, есть ли у квеста тайные награды для отображения особых эффектов
        /// </summary>
        /// <param name="questIndex">Индекс квеста в списке</param>
        public bool HasSecretRewards(int questIndex)
        {
            if (questIndex < 0 || questIndex >= _quests.Count || _quests[questIndex] == null) 
                return false;
            
            return QuestRewardHelper.HasSecretRewards(_quests[questIndex]);
        }
        
        /// <summary>
        /// Получает подсказку о тайных наградах
        /// </summary>
        /// <param name="questIndex">Индекс квеста в списке</param>
        public string GetSecretRewardHint(int questIndex)
        {
            if (questIndex < 0 || questIndex >= _quests.Count || _quests[questIndex] == null) 
                return "";
            
            return QuestRewardHelper.GetSecretRewardHint(_quests[questIndex]);
        }
    }
}
