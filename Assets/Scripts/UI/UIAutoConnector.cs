using UnityEngine;
using System.Collections;

namespace RPG.UI
{
    /// <summary>
    /// Автоматически подключает все UI компоненты к игроку при запуске сцены
    /// </summary>
    public class UIAutoConnector : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        private void Start()
        {
            StartCoroutine(ConnectUIComponentsDelayed());
        }
        
        private IEnumerator ConnectUIComponentsDelayed()
        {
            // Ждем один кадр, чтобы все объекты инициализировались
            yield return null;
            
            if (enableDebugLogs)
                Debug.Log("UIAutoConnector: Начинаем автоматическое подключение UI компонентов");
            
            ConnectUIComponents();
            
            // Еще раз обновляем через небольшую задержку
            yield return new WaitForSeconds(0.5f);
            UIManager.RefreshUIFromAnywhere();
        }
        
        private void ConnectUIComponents()
        {
            // Находим все UI компоненты в сцене
            PlayerHealthBar[] healthBars = FindObjectsOfType<PlayerHealthBar>();
            PlayerManaBar[] manaBars = FindObjectsOfType<PlayerManaBar>();
            PlayerExperienceBar[] experienceBars = FindObjectsOfType<PlayerExperienceBar>();
            PlayerLevelDisplay[] levelDisplays = FindObjectsOfType<PlayerLevelDisplay>();
            
            if (enableDebugLogs)
            {
                Debug.Log($"UIAutoConnector: Найдено компонентов:");
                Debug.Log($"  PlayerHealthBar: {healthBars.Length}");
                Debug.Log($"  PlayerManaBar: {manaBars.Length}");
                Debug.Log($"  PlayerExperienceBar: {experienceBars.Length}");
                Debug.Log($"  PlayerLevelDisplay: {levelDisplays.Length}");
            }
            
            // Принудительно обновляем все найденные компоненты
            foreach (var healthBar in healthBars)
            {
                if (healthBar != null)
                    healthBar.ForceRefresh();
            }
            
            foreach (var manaBar in manaBars)
            {
                if (manaBar != null)
                    manaBar.ForceRefresh();
            }
            
            foreach (var experienceBar in experienceBars)
            {
                if (experienceBar != null)
                    experienceBar.ForceRefresh();
            }
            
            foreach (var levelDisplay in levelDisplays)
            {
                if (levelDisplay != null)
                    levelDisplay.ForceRefresh();
            }
            
            if (enableDebugLogs)
                Debug.Log("UIAutoConnector: Автоматическое подключение UI завершено");
        }
    }
}