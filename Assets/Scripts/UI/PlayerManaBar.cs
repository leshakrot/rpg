using UnityEngine;
using RPG.Attributes;
using RPG.Stats;
using System;

namespace RPG.UI
{
    public class PlayerManaBar : StatusBar
    {
        [SerializeField] private Mana playerMana;
        [SerializeField] private BaseStats playerStats;
        
        protected override void Awake()
        {
            base.Awake();
            barTitle = "Мана";
            barColor = Color.blue;
        }
        
        private void OnEnable()
        {
            if (playerStats != null)
            {
                // Подписываемся на событие изменения статистики
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
            if (stat == Stat.Mana || stat == Stat.ManaRegenRate)
            {
                RefreshBar();
            }
        }
        
        // Обновление данных бара
        private void RefreshBar()
        {
            if (playerMana != null)
            {
                // При изменении значения, принудительно обновляем UI
                // Это особенно важно при повышении уровня, когда максимальная мана изменяется
                float currentMana = playerMana.GetMana();
                float maxMana = playerMana.GetMaxMana();
                
                // Обновляем заполнение полосы
                SetFraction(currentMana / maxMana);
                
                // Обновляем текст со значением
                SetValueText(String.Format("{0:0}/{1:0}", currentMana, maxMana));
            }
        }
        
        protected override void Update()
        {
            if (playerMana != null)
            {
                // Обновляем заполнение полосы
                SetFraction(playerMana.GetMana() / playerMana.GetMaxMana());
                
                // Обновляем текст со значением
                SetValueText(String.Format("{0:0}/{1:0}", 
                    playerMana.GetMana(), 
                    playerMana.GetMaxMana()));
            }
            
            base.Update();
        }
    }
} 