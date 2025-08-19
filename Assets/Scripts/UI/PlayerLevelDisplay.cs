using UnityEngine;
using RPG.Stats;
using TMPro;
using System;

namespace RPG.UI
{
    public class PlayerLevelDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelText;
        
	    [SerializeField] private BaseStats playerStats;
        
        private void Awake()
        {
            if (playerStats == null)
            {
                Debug.LogError("PlayerLevelDisplay: Не удалось найти компонент BaseStats у игрока!");
            }
        }
        
        //private void Update()
        //{
        //    if (playerStats != null && levelText != null)
        //    {
        //        // Обновляем текст с уровнем
        //        levelText.text = String.Format("{0}", playerStats.GetLevel());
        //    }
        //}
        
	    private void OnEnable()
	    {
		    if (playerStats != null)
		    {
			    // Подписываемся на события изменения статистики и повышения уровня
			    playerStats.onLevelUp += RefreshLevelDisplayUI;
		    }
	    }
        
	    private void OnDisable()
	    {
		    if (playerStats != null)
		    {
			    // Отписываемся от событий
			    playerStats.onLevelUp -= RefreshLevelDisplayUI;
		    }
	    }
	    
	    private void RefreshLevelDisplayUI()
	    {
		    if (playerStats != null && levelText != null)
		    {
			    // Обновляем текст с уровнем
			    levelText.text = String.Format("{0}", playerStats.GetLevel());
		    }
	    }
    }
} 