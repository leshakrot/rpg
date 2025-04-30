using UnityEngine;
using RPG.Attributes;
using System;

namespace RPG.UI
{
    public class PlayerHealthBar : StatusBar
    {
        private Health playerHealth;
        
        protected override void Awake()
        {
            base.Awake();
            barTitle = "Здоровье";
            barColor = Color.red;
            
            // Находим компонент Health игрока
            playerHealth = GameObject.FindWithTag("Player").GetComponent<Health>();
            if (playerHealth == null)
            {
                Debug.LogError("PlayerHealthBar: Не удалось найти компонент Health у игрока!");
            }
        }
        
        protected override void Update()
        {
            if (playerHealth != null)
            {
                // Обновляем заполнение полосы
                SetFraction(playerHealth.GetFraction());
                
                // Обновляем текст со значением
                SetValueText(String.Format("{0:0}/{1:0}", 
                    playerHealth.GetHealthPoints(), 
                    playerHealth.GetMaxHealthPoints()));
            }
            
            base.Update();
        }
    }
} 