using UnityEngine;
using RPG.Stats;
using UnityEngine.UI;
using System;

namespace RPG.UI
{
    public class PlayerLevelDisplay : MonoBehaviour
    {
        [SerializeField] private Text levelText;
        
        private BaseStats playerStats;
        
        private void Awake()
        {
            // Находим компонент BaseStats игрока
            playerStats = GameObject.FindWithTag("Player").GetComponent<BaseStats>();
            if (playerStats == null)
            {
                Debug.LogError("PlayerLevelDisplay: Не удалось найти компонент BaseStats у игрока!");
            }
        }
        
        private void Update()
        {
            if (playerStats != null && levelText != null)
            {
                // Обновляем текст с уровнем
                levelText.text = String.Format("Уровень: {0}", playerStats.GetLevel());
            }
        }
    }
} 