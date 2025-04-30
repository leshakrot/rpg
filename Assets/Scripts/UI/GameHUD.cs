using System;
using UnityEngine;
using RPG.Attributes;
using RPG.Stats;
using RPG.Combat;

namespace RPG.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Player Stats")]
        [SerializeField] private GameObject playerHealthBar;
        [SerializeField] private GameObject playerManaBar;
        [SerializeField] private GameObject playerLevelDisplay;
        [SerializeField] private GameObject playerExperienceBar;
        
        [Header("Enemy Stats")]
        [SerializeField] private GameObject enemyHealthBar;
        
        private void Awake()
        {
            // Убедимся, что все компоненты установлены
            if (playerHealthBar == null || playerManaBar == null || 
                playerLevelDisplay == null || playerExperienceBar == null || 
                enemyHealthBar == null)
            {
                Debug.LogError("GameHUD: Не все UI компоненты установлены!");
            }
        }
    }
} 