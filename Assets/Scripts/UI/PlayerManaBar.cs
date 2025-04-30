using UnityEngine;
using RPG.Attributes;
using System;

namespace RPG.UI
{
    public class PlayerManaBar : StatusBar
    {
        private Mana playerMana;
        
        protected override void Awake()
        {
            base.Awake();
            barTitle = "Мана";
            barColor = Color.blue;
            
            // Находим компонент Mana игрока
            playerMana = GameObject.FindWithTag("Player").GetComponent<Mana>();
            if (playerMana == null)
            {
                Debug.LogError("PlayerManaBar: Не удалось найти компонент Mana у игрока!");
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