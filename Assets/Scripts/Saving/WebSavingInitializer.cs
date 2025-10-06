using UnityEngine;
using YG;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Компонент для инициализации веб-адаптера сохранений
    /// Обеспечивает корректную работу с YandexSDK по стандартному подходу
    /// </summary>
    public class WebSavingInitializer : MonoBehaviour
    {
        [Header("Настройки инициализации")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool waitForSDK = true;
        [SerializeField] private float maxWaitTime = 10f;

        [Header("Отладка")]
        [SerializeField] private bool enableDebugLogs = true;

        private bool isInitialized = false;

        private void Start()
        {
            if (initializeOnStart)
            {
                InitializeWebSaving();
            }
        }

        private void OnEnable()
        {
            // Подписываемся на событие получения данных YandexSDK
            YandexGame.GetDataEvent += OnYandexDataReceived;
        }

        private void OnDisable()
        {
            // Отписываемся от события YandexSDK
            YandexGame.GetDataEvent -= OnYandexDataReceived;
        }

        /// <summary>
        /// Инициализация веб-системы сохранений
        /// </summary>
        public void InitializeWebSaving()
        {
            if (isInitialized)
            {
                if (enableDebugLogs)
                    Debug.Log("WebSavingInitializer: Уже инициализирован");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (waitForSDK)
            {
                StartCoroutine(WaitForSDKAndInitialize());
            }
            else
            {
                CompleteInitialization();
            }
#else
            if (enableDebugLogs)
                Debug.Log("WebSavingInitializer: Платформа не WebGL, инициализация не требуется");
            
            isInitialized = true;
#endif
        }

        private System.Collections.IEnumerator WaitForSDKAndInitialize()
        {
            float timer = 0f;
            
            while (!YandexGame.SDKEnabled && timer < maxWaitTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (YandexGame.SDKEnabled)
            {
                if (enableDebugLogs)
                    Debug.Log("WebSavingInitializer: YandexSDK готов, инициализация завершена");
                    
                CompleteInitialization();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("WebSavingInitializer: YandexSDK не загрузился вовремя");
                    
                CompleteInitialization();
            }
        }

        private void CompleteInitialization()
        {
            isInitialized = true;
            
            if (enableDebugLogs)
            {
                Debug.Log("WebSavingInitializer: Инициализация завершена");
#if UNITY_WEBGL && !UNITY_EDITOR
                Debug.Log($"WebSavingInitializer: SDK статус: {YandexGame.SDKEnabled}");
                Debug.Log($"WebSavingInitializer: Текущее сохранение: '{YandexGame.savesData.currentSaveFile}'");
                Debug.Log($"WebSavingInitializer: Есть данные: {!string.IsNullOrEmpty(YandexGame.savesData.gameDataJson)}");
#endif
            }
        }

        private void OnYandexDataReceived()
        {
            if (enableDebugLogs)
            {
                Debug.Log("WebSavingInitializer: Получены данные от YandexSDK");
#if UNITY_WEBGL && !UNITY_EDITOR
                Debug.Log($"WebSavingInitializer: Имя файла: '{YandexGame.savesData.currentSaveFile}'");
                Debug.Log($"WebSavingInitializer: Размер данных: {YandexGame.savesData.gameDataJson?.Length ?? 0} символов");
                Debug.Log($"WebSavingInitializer: Последняя сцена: {YandexGame.savesData.lastSceneBuildIndex}");
#endif
            }
        }

        /// <summary>
        /// Проверка готовности веб-системы сохранений
        /// </summary>
        public bool IsReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return isInitialized && YandexGame.SDKEnabled;
#else
            return true;
#endif
        }

        /// <summary>
        /// Информация о состоянии системы
        /// </summary>
        [ContextMenu("Show System Status")]
        public void ShowSystemStatus()
        {
            Debug.Log("=== WebSavingInitializer Status ===");
            Debug.Log($"Инициализирован: {isInitialized}");
            
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log($"YandexSDK готов: {YandexGame.SDKEnabled}");
            Debug.Log($"Авторизация: {YandexGame.auth}");
            Debug.Log($"Текущий файл: '{YandexGame.savesData.currentSaveFile}'");
            Debug.Log($"Есть данные: {!string.IsNullOrEmpty(YandexGame.savesData.gameDataJson)}");
            Debug.Log($"Последняя сцена: {YandexGame.savesData.lastSceneBuildIndex}");
#else
            Debug.Log("Платформа: Не WebGL");
#endif
            Debug.Log("==================================");
        }
    }
}