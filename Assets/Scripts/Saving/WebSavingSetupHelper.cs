using UnityEngine;
using RPG.SceneManagement;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Вспомогательный компонент для быстрой настройки веб-сохранений
    /// Автоматически добавляет необходимые компоненты при первом запуске
    /// </summary>
    public class WebSavingSetupHelper : MonoBehaviour
    {
        [Header("Автонастройка")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool createSavingSystemIfMissing = true;
        [SerializeField] private bool createSavingWrapperIfMissing = true;
        [SerializeField] private bool createWebInitializerIfMissing = true;

        [Header("Отладка")]
        [SerializeField] private bool showDebugLogs = true;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupWebSaving();
            }
        }

        /// <summary>
        /// Автоматическая настройка веб-сохранений
        /// </summary>
        [ContextMenu("Setup Web Saving")]
        public void SetupWebSaving()
        {
            if (showDebugLogs)
                Debug.Log("WebSavingSetupHelper: Начинаем автонастройку веб-сохранений");

            // Настраиваем SavingSystem
            if (createSavingSystemIfMissing)
            {
                SetupSavingSystem();
            }

            // Настраиваем SavingWrapper
            if (createSavingWrapperIfMissing)
            {
                SetupSavingWrapper();
            }

            // Настраиваем WebSavingInitializer
            if (createWebInitializerIfMissing)
            {
                SetupWebInitializer();
            }

            if (showDebugLogs)
                Debug.Log("WebSavingSetupHelper: Автонастройка завершена");
        }

        private void SetupSavingSystem()
        {
            SavingSystem savingSystem = FindObjectOfType<SavingSystem>();
            
            if (savingSystem == null)
            {
                // Создаем новый GameObject для SavingSystem
                GameObject savingGO = new GameObject("SavingSystem");
                savingSystem = savingGO.AddComponent<SavingSystem>();
                
                // Делаем его persistent между сценами
                DontDestroyOnLoad(savingGO);
                
                if (showDebugLogs)
                    Debug.Log("WebSavingSetupHelper: Создан SavingSystem");
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("WebSavingSetupHelper: SavingSystem уже существует");
            }
        }

        private void SetupSavingWrapper()
        {
            SavingWrapper savingWrapper = FindObjectOfType<SavingWrapper>();
            
            if (savingWrapper == null)
            {
                // Пытаемся найти SavingSystem и добавить SavingWrapper к нему
                SavingSystem savingSystem = FindObjectOfType<SavingSystem>();
                
                if (savingSystem != null)
                {
                    savingWrapper = savingSystem.gameObject.AddComponent<SavingWrapper>();
                    if (showDebugLogs)
                        Debug.Log("WebSavingSetupHelper: Добавлен SavingWrapper к SavingSystem");
                }
                else
                {
                    // Создаем отдельный GameObject для SavingWrapper
                    GameObject wrapperGO = new GameObject("SavingWrapper");
                    savingWrapper = wrapperGO.AddComponent<SavingWrapper>();
                    DontDestroyOnLoad(wrapperGO);
                    
                    if (showDebugLogs)
                        Debug.Log("WebSavingSetupHelper: Создан отдельный SavingWrapper");
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("WebSavingSetupHelper: SavingWrapper уже существует");
            }
        }

        private void SetupWebInitializer()
        {
            WebSavingInitializer webInitializer = FindObjectOfType<WebSavingInitializer>();
            
            if (webInitializer == null)
            {
                // Пытаемся найти YandexGame и добавить WebSavingInitializer к нему
#if UNITY_WEBGL
                var yandexGame = FindObjectOfType<YG.YandexGame>();
                
                if (yandexGame != null)
                {
                    webInitializer = yandexGame.gameObject.AddComponent<WebSavingInitializer>();
                    if (showDebugLogs)
                        Debug.Log("WebSavingSetupHelper: Добавлен WebSavingInitializer к YandexGame");
                }
                else
#endif
                {
                    // Создаем отдельный GameObject для WebSavingInitializer
                    GameObject initGO = new GameObject("WebSavingInitializer");
                    webInitializer = initGO.AddComponent<WebSavingInitializer>();
                    DontDestroyOnLoad(initGO);
                    
                    if (showDebugLogs)
                        Debug.Log("WebSavingSetupHelper: Создан отдельный WebSavingInitializer");
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("WebSavingSetupHelper: WebSavingInitializer уже существует");
            }
        }

        /// <summary>
        /// Проверка готовности всей системы веб-сохранений
        /// </summary>
        [ContextMenu("Check Web Saving Status")]
        public void CheckWebSavingStatus()
        {
            Debug.Log("=== Статус системы веб-сохранений ===");
            
            SavingSystem savingSystem = FindObjectOfType<SavingSystem>();
            Debug.Log($"SavingSystem: {(savingSystem != null ? "✓ Найден" : "✗ Отсутствует")}");
            
            SavingWrapper savingWrapper = FindObjectOfType<SavingWrapper>();
            Debug.Log($"SavingWrapper: {(savingWrapper != null ? "✓ Найден" : "✗ Отсутствует")}");
            
            WebSavingInitializer webInitializer = FindObjectOfType<WebSavingInitializer>();
            Debug.Log($"WebSavingInitializer: {(webInitializer != null ? "✓ Найден" : "✗ Отсутствует")}");

#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log($"YandexSDK Status: {(YG.YandexGame.SDKEnabled ? "✓ Готов" : "✗ Не готов")}");
            
            if (webInitializer != null)
            {
                Debug.Log($"Web Saving Ready: {(webInitializer.IsReady() ? "✓ Готов" : "✗ Не готов")}");
            }
#else
            Debug.Log("Платформа: Не WebGL (веб-инициализация не требуется)");
#endif

            Debug.Log("=====================================");
        }

        /// <summary>
        /// Тестирование базовых функций сохранения
        /// </summary>
        [ContextMenu("Test Save System")]
        public void TestSaveSystem()
        {
            SavingWrapper savingWrapper = FindObjectOfType<SavingWrapper>();
            
            if (savingWrapper == null)
            {
                Debug.LogError("SavingWrapper не найден! Выполните автонастройку.");
                return;
            }

            try
            {
                // Тестируем сохранение
                savingWrapper.Save();
                Debug.Log("✓ Тест сохранения прошел успешно");
                
                // Тестируем проверку существования файла
                SavingSystem savingSystem = savingWrapper.GetComponent<SavingSystem>();
                if (savingSystem != null)
                {
                    bool exists = savingSystem.SaveFileExists("slot1");
                    Debug.Log($"✓ Тест проверки файла: slot1 {(exists ? "существует" : "не существует")}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Ошибка тестирования: {e.Message}");
            }
        }
    }
}