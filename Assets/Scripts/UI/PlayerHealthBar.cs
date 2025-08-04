using UnityEngine;
using RPG.Attributes;
using RPG.Stats;
using System;

namespace RPG.UI
{
    public class PlayerHealthBar : StatusBar
	{
		[SerializeField] private Health playerHealth;
		[SerializeField] private BaseStats playerStats;
        private float lastHealthAmount = -1; // Для отслеживания изменений здоровья
        
        protected override void Awake()
        {
            base.Awake();
            barTitle = "Здоровье";
            barColor = Color.red;           
        }
        
        private void OnEnable()
        {
            if (playerStats != null)
            {
                // Подписываемся на события изменения статистики и повышения уровня
                playerStats.onStatChanged += OnStatChanged;
                playerStats.onLevelUp += RefreshBar;
            }
        }
        
        private void OnDisable()
        {
            if (playerStats != null)
            {
                // Отписываемся от событий
                playerStats.onStatChanged -= OnStatChanged;
                playerStats.onLevelUp -= RefreshBar;
            }
        }
        
        // Реагируем на изменение статистики
        private void OnStatChanged(Stat stat)
        {
            if (stat == Stat.Health)
            {
                RefreshBar();
            }
        }
        
        // Обновление данных бара
        private void RefreshBar()
        {
            if (playerHealth != null)
            {
                try
                {
                    // При изменении значения, принудительно обновляем UI
                    float fraction = playerHealth.GetFraction();
                    lastHealthAmount = playerHealth.GetHealthPoints();
                    
                    // Обновляем заполнение полосы
                    SetFraction(fraction);
                    
                    // Обновляем текст со значением
                    SetValueText(String.Format("{0:0}/{1:0}", 
                        playerHealth.GetHealthPoints(), 
                        playerHealth.GetMaxHealthPoints()));
                }
                catch (NullReferenceException e)
                {
                    Debug.LogError($"PlayerHealthBar: NullReferenceException в RefreshBar: {e.Message}");
                }
            }
        }
        
        protected override void Update()
        {
            // Если lastHealthAmount не инициализирован, и playerHealth не null
            if (lastHealthAmount < 0 && playerHealth != null)
            {
                try
                {
                    lastHealthAmount = playerHealth.GetHealthPoints();
                }
                catch (NullReferenceException)
                {
                    // Компоненты могут быть ещё не готовы, попробуем еще раз в следующем кадре
                    lastHealthAmount = -1;
                }
            }
            
            if (playerHealth != null)
            {
                try
                {
                    // Проверяем, изменилось ли здоровье
                    float currentHealth = playerHealth.GetHealthPoints();
                    if (!Mathf.Approximately(currentHealth, lastHealthAmount) && lastHealthAmount >= 0)
                    {
                        lastHealthAmount = currentHealth;
                        RefreshBar();
                    }
                    else if (lastHealthAmount >= 0)
                    {
                        // Обновляем заполнение полосы
                        SetFraction(playerHealth.GetFraction());
                        
                        // Обновляем текст со значением
                        SetValueText(String.Format("{0:0}/{1:0}", 
                            playerHealth.GetHealthPoints(), 
                            playerHealth.GetMaxHealthPoints()));
                        
                        base.Update();
                    }
                    else
                    {
                        base.Update();
                    }
                }
                catch (NullReferenceException e)
                {
                    Debug.LogError($"PlayerHealthBar: NullReferenceException в Update: {e.Message}");
                    base.Update();
                }
            }
            else
            {
                base.Update();
            }
        }
    }
} 