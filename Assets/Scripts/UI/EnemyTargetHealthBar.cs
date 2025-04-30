using UnityEngine;
using RPG.Attributes;
using RPG.Combat;
using System;

namespace RPG.UI
{
    public class EnemyTargetHealthBar : StatusBar
    {
        private PlayerFighter playerFighter;
        
        protected override void Awake()
        {
            base.Awake();
            barTitle = "Цель";
            barColor = new Color(0.8f, 0.2f, 0.2f); // Темно-красный
            
            // Находим компонент PlayerFighter игрока
            playerFighter = GameObject.FindWithTag("Player").GetComponent<PlayerFighter>();
            if (playerFighter == null)
            {
                Debug.LogError("EnemyTargetHealthBar: Не удалось найти компонент PlayerFighter у игрока!");
            }
        }
        
        protected override void Update()
        {
            if (playerFighter == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            // Получаем текущую цель
            Health targetHealth = playerFighter.GetTarget();
            
            if (targetHealth == null)
            {
                // Если цель не выбрана, скрываем полосу
                gameObject.SetActive(false);
                return;
            }
            
            // Обновляем заполнение полосы
            SetFraction(targetHealth.GetFraction());
            
            // Обновляем текст со значением
            SetValueText(String.Format("{0:0}/{1:0}", 
                targetHealth.GetHealthPoints(), 
                targetHealth.GetMaxHealthPoints()));
            
            base.Update();
        }
    }
} 