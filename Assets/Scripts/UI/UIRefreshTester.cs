using UnityEngine;
using RPG.Stats;

namespace RPG.UI
{
    /// <summary>
    /// Временный скрипт для тестирования обновления UI
    /// </summary>
    public class UIRefreshTester : MonoBehaviour
    {
        [Header("Test Controls")]
        [SerializeField] private KeyCode refreshUIKey = KeyCode.U;
        [SerializeField] private KeyCode refreshStatsKey = KeyCode.R;
        
        private void Update()
        {
            if (Input.GetKeyDown(refreshUIKey))
            {
                Debug.Log("UIRefreshTester: Принудительно обновляем все UI");
                UIManager.RefreshUIFromAnywhere();
            }
            
            if (Input.GetKeyDown(refreshStatsKey))
            {
                Debug.Log("UIRefreshTester: Принудительно обновляем статистики игрока");
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    BaseStats playerStats = player.GetComponent<BaseStats>();
                    if (playerStats != null)
                    {
                        playerStats.RefreshStats();
                    }
                    else
                    {
                        Debug.LogError("BaseStats не найден у игрока!");
                    }
                }
                else
                {
                    Debug.LogError("Игрок не найден!");
                }
            }
        }
        
        private void Start()
        {
            Debug.Log("UIRefreshTester активирован. Нажмите:");
            Debug.Log($"  {refreshUIKey} - для обновления UI");
            Debug.Log($"  {refreshStatsKey} - для обновления статистик");
        }
    }
}