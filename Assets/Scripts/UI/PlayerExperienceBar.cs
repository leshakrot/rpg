using UnityEngine;
using RPG.Stats;
using System;

namespace RPG.UI
{
    public class PlayerExperienceBar : StatusBar
    {
        private Experience playerExperience;
        private BaseStats playerStats;
        
        protected override void Awake()
        {
            base.Awake();
            barTitle = "Опыт";
            barColor = new Color(0.8f, 0.8f, 0.2f); // Золотисто-желтый цвет
            
            // Находим компоненты игрока
            GameObject player = GameObject.FindWithTag("Player");
            playerExperience = player.GetComponent<Experience>();
            playerStats = player.GetComponent<BaseStats>();
            
            if (playerExperience == null || playerStats == null)
            {
                Debug.LogError("PlayerExperienceBar: Не удалось найти необходимые компоненты у игрока!");
            }
        }
        
        protected override void Update()
        {
            if (playerExperience != null && playerStats != null)
            {
                // Получаем текущий уровень и вычисляем опыт для следующего уровня
                int currentLevel = playerStats.GetLevel();
                float currentXP = playerExperience.GetPoints();
                
                // Расчет опыта для текущего уровня и следующего
                float xpForCurrentLevel = GetXPForLevel(currentLevel);
                float xpForNextLevel = GetXPForLevel(currentLevel + 1);
                
                // Расчет прогресса до следующего уровня
                float levelProgress = (currentXP - xpForCurrentLevel) / (xpForNextLevel - xpForCurrentLevel);
                SetFraction(Mathf.Clamp01(levelProgress));
                
                // Обновляем текст со значением
                SetValueText(String.Format("{0:0}/{1:0}", 
                    currentXP, 
                    xpForNextLevel));
            }
            
            base.Update();
        }
        
        // Упрощенная функция для получения необходимого опыта для уровня
        private float GetXPForLevel(int level)
        {
            // Это упрощенный расчет. В реальной игре может использоваться иная формула
            return (level - 1) * 100f;
        }
    }
} 