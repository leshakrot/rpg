using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPG.Quests
{
    /// <summary>
    /// Вспомогательный класс для работы с наградами квестов, включая тайные награды
    /// </summary>
    public static class QuestRewardHelper
    {
        /// <summary>
        /// Получает список наград, которые должны быть показаны игроку до завершения квеста
        /// </summary>
        /// <param name="quest">Квест</param>
        /// <returns>Коллекция видимых наград</returns>
        public static IEnumerable<Quest.Reward> GetRewardsForDisplay(Quest quest)
        {
            return quest.GetVisibleRewards();
        }

        /// <summary>
        /// Получает все награды для выдачи игроку при завершении квеста
        /// </summary>
        /// <param name="quest">Квест</param>
        /// <returns>Коллекция всех наград</returns>
        public static IEnumerable<Quest.Reward> GetRewardsForCompletion(Quest quest)
        {
            return quest.GetRewards();
        }

        /// <summary>
        /// Получает только тайные награды квеста
        /// </summary>
        /// <param name="quest">Квест</param>
        /// <returns>Коллекция тайных наград</returns>
        public static IEnumerable<Quest.Reward> GetSecretRewards(Quest quest)
        {
            return quest.GetSecretRewards();
        }

        /// <summary>
        /// Проверяет, есть ли у квеста тайные награды
        /// </summary>
        /// <param name="quest">Квест</param>
        /// <returns>True, если есть тайные награды</returns>
        public static bool HasSecretRewards(Quest quest)
        {
            return quest.GetSecretRewards().Any();
        }

        /// <summary>
        /// Возвращает количество тайных наград
        /// </summary>
        /// <param name="quest">Квест</param>
        /// <returns>Количество тайных наград</returns>
        public static int GetSecretRewardCount(Quest quest)
        {
            return quest.GetSecretRewards().Count();
        }

        /// <summary>
        /// Возвращает текст-подсказку о наличии тайных наград
        /// </summary>
        /// <param name="quest">Квест</param>
        /// <returns>Строка с подсказкой или пустая строка</returns>
        public static string GetSecretRewardHint(Quest quest)
        {
            int secretCount = GetSecretRewardCount(quest);
            if (secretCount == 0)
                return "";
            
            return secretCount == 1 
                ? "У этого квеста есть тайная награда!" 
                : $"У этого квеста есть {secretCount} тайных наград!";
        }
    }
}