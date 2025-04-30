using UnityEngine;
using RPG.Attributes;
using RPG.Stats;
using RPG.Combat;

namespace RPG.UI
{
    public class HUDTester : MonoBehaviour
    {
        [Header("Test Controls")]
        [SerializeField] private bool enableTestMode = true;
        [SerializeField] private KeyCode healthDecreaseKey = KeyCode.Q;
        [SerializeField] private KeyCode healthIncreaseKey = KeyCode.W;
        [SerializeField] private KeyCode manaDecreaseKey = KeyCode.A;
        [SerializeField] private KeyCode manaIncreaseKey = KeyCode.S;
        [SerializeField] private KeyCode experienceAddKey = KeyCode.E;
        
        [Header("Enemy Selection")]
        [SerializeField] private KeyCode selectEnemyKey = KeyCode.F;
        [SerializeField] private GameObject testEnemy;
        
        [Header("Test Values")]
        [SerializeField] private float healthChangeAmount = 10f;
        [SerializeField] private float manaChangeAmount = 10f;
        [SerializeField] private float experienceAddAmount = 25f;
        
        private Health playerHealth;
        private Mana playerMana;
        private Experience playerExperience;
        private PlayerFighter playerFighter;
        
        private void Start()
        {
            if (!enableTestMode) return;
            
            // Находим необходимые компоненты
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
                playerMana = player.GetComponent<Mana>();
                playerExperience = player.GetComponent<Experience>();
                playerFighter = player.GetComponent<PlayerFighter>();
                
                Debug.Log("HUD тестер активирован. Используйте клавиши для тестирования HUD:");
                Debug.Log($"{healthDecreaseKey} - уменьшить здоровье, {healthIncreaseKey} - увеличить здоровье");
                Debug.Log($"{manaDecreaseKey} - уменьшить ману, {manaIncreaseKey} - увеличить ману");
                Debug.Log($"{experienceAddKey} - добавить опыт");
                Debug.Log($"{selectEnemyKey} - выбрать врага как цель");
            }
            else
            {
                Debug.LogError("HUDTester: Игрок не найден!");
            }
        }
        
        private void Update()
        {
            if (!enableTestMode) return;
            
            // Управление здоровьем
            if (playerHealth != null)
            {
                if (Input.GetKeyDown(healthDecreaseKey))
                {
                    playerHealth.TakeDamage(gameObject, healthChangeAmount);
                    Debug.Log($"Здоровье уменьшено на {healthChangeAmount}");
                }
                
                if (Input.GetKeyDown(healthIncreaseKey))
                {
                    // Прямой доступ к методу восстановления (если есть) или модификация здоровья
                    if (playerHealth.GetHealthPoints() < playerHealth.GetMaxHealthPoints())
                    {
                        // Примечание: Используйте правильный метод для восстановления здоровья в вашем проекте
                        playerHealth.Heal(healthChangeAmount);
                        Debug.Log($"Здоровье увеличено на {healthChangeAmount}");
                    }
                }
            }
            
            // Управление маной
            if (playerMana != null)
            {
                //if (Input.GetKeyDown(manaDecreaseKey))
                //{
                //    // Примечание: Используйте правильный метод для уменьшения маны в вашем проекте
                //    playerMana.UseMana(manaChangeAmount);
                //    Debug.Log($"Мана уменьшена на {manaChangeAmount}");
                //}
                
                //if (Input.GetKeyDown(manaIncreaseKey))
                //{
                //    // Примечание: Используйте правильный метод для восстановления маны в вашем проекте
                //    playerMana. RestoreMana(manaChangeAmount);
                //    Debug.Log($"Мана увеличена на {manaChangeAmount}");
                //}
            }
            
            // Управление опытом
            if (playerExperience != null && Input.GetKeyDown(experienceAddKey))
            {
                playerExperience.GainExperience(experienceAddAmount);
                Debug.Log($"Добавлено {experienceAddAmount} опыта");
            }
            
            //// Выбор врага как цели
            //if (Input.GetKeyDown(selectEnemyKey) && testEnemy != null && playerFighter != null)
            //{
            //    Health enemyHealth = testEnemy.GetComponent<Health>();
            //    if (enemyHealth != null)
            //    {
            //        playerFighter.SetTarget(enemyHealth);
            //        Debug.Log("Выбран враг как цель");
            //    }
            //}
        }
    }
} 