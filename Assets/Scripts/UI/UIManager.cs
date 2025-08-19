using UnityEngine;
using System.Collections;
using RPG.Stats;

namespace RPG.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private PlayerHealthBar healthBar;
        [SerializeField] private PlayerManaBar manaBar;
        [SerializeField] private PlayerExperienceBar experienceBar;
        [SerializeField] private PlayerLevelDisplay levelDisplay;
        
        private BaseStats playerStats;
        
        private void Awake()
        {
            // Находим все UI компоненты автоматически, если они не назначены
            if (healthBar == null)
                healthBar = FindObjectOfType<PlayerHealthBar>();
            if (manaBar == null)
                manaBar = FindObjectOfType<PlayerManaBar>();
            if (experienceBar == null)
                experienceBar = FindObjectOfType<PlayerExperienceBar>();
            if (levelDisplay == null)
                levelDisplay = FindObjectOfType<PlayerLevelDisplay>();
        }
        
        private void Start()
        {
            // Находим игрока и подписываемся на события
            FindPlayerAndSubscribe();
            
            // Принудительно обновляем все UI при старте
            StartCoroutine(RefreshAllUIDelayed());
        }
        
        private void FindPlayerAndSubscribe()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerStats = player.GetComponent<BaseStats>();
                if (playerStats != null)
                {
                    // Подписываемся на события повышения уровня
                    playerStats.onLevelUp += RefreshAllUI;
                }
            }
        }
        
        private void OnDestroy()
        {
            // Отписываемся от событий
            if (playerStats != null)
            {
                playerStats.onLevelUp -= RefreshAllUI;
            }
        }
        
        // Публичный метод для принудительного обновления всех UI
        public void RefreshAllUI()
        {
            Debug.Log("UIManager: Обновляем все UI компоненты");
            
            if (healthBar != null)
                healthBar.ForceRefresh();
            if (manaBar != null)
                manaBar.ForceRefresh();
            if (experienceBar != null)
                experienceBar.ForceRefresh();
            if (levelDisplay != null)
                levelDisplay.ForceRefresh();
        }
        
        // Корутина для отложенного обновления UI (на случай, если компоненты еще не инициализированы)
        private IEnumerator RefreshAllUIDelayed()
        {
            yield return new WaitForSeconds(0.1f);
            RefreshAllUI();
        }
        
        // Метод для использования из других скриптов (например, после загрузки сохранения)
        public static void RefreshUIFromAnywhere()
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.RefreshAllUI();
            }
            else
            {
                Debug.LogWarning("UIManager не найден в сцене!");
            }
        }
    }
}