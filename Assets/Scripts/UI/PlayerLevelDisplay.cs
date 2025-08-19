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
            // Автоматически находим компоненты игрока
            FindPlayerComponents();
        }
        
        private void FindPlayerComponents()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                if (playerStats == null)
                    playerStats = player.GetComponent<BaseStats>();
            }
            else
            {
                Debug.LogWarning("PlayerLevelDisplay: Игрок не найден! Убедитесь, что игрок имеет тег 'Player'.");
            }
        }
        
        private void Start()
        {
            // Принудительно обновляем отображение уровня при старте
            RefreshLevelDisplayUI();
        }
        
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
	    
	    // Публичный метод для принудительного обновления UI
	    public void ForceRefresh()
	    {
	        // Повторно находим компоненты игрока на случай, если они изменились
	        FindPlayerComponents();
	        RefreshLevelDisplayUI();
	    }
    }
} 