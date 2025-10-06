using UnityEngine;
using YG;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Компонент для инициализации веб-адаптера сохранений
    /// Обеспечивает корректную работу с YandexSDK
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
            // Подписываемся на события YandexSDK
            YandexGame.GetDataEvent += OnYandexDataReceived;
        }

        private void OnDisable()
        {
            // Отписываемся от событий YandexSDK
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
                    Debug.Log("WebSavingInitializer уже инициализирован");
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
                Debug.Log("WebSavingInitializer: платформа не WebGL, инициализация не требуется");
            
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
                    Debug.Log("YandexSDK успешно загружен, инициализация веб-сохранений завершена");
                    
                CompleteInitialization();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("YandexSDK не загрузился в отведенное время. Инициализация может работать некорректно.");
                    
                CompleteInitialization();
            }
        }

        private void CompleteInitialization()
        {
            isInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log("WebSavingInitializer инициализирован успешно");
                
            // Уведомляем другие компоненты о готовности
            WebSavingAdapter.OnSaveLoaded?.Invoke();
        }

        private void OnYandexDataReceived()
        {
            if (enableDebugLogs)
                Debug.Log("Получены данные от YandexSDK");
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
        /// Принудительная загрузка данных из YandexSDK
        /// </summary>
        public void ForceLoadData()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (YandexGame.SDKEnabled)
            {
                YandexGame.LoadProgress();
                if (enableDebugLogs)
                    Debug.Log("Принудительная загрузка данных из YandexSDK");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("YandexSDK не готов для загрузки данных");
            }
#endif
        }

        /// <summary>
        /// Принудительное сохранение данных в YandexSDK
        /// </summary>
        public void ForceSaveData()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (YandexGame.SDKEnabled)
            {
                YandexGame.SaveProgress();
                if (enableDebugLogs)
                    Debug.Log("Принудительное сохранение данных в YandexSDK");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("YandexSDK не готов для сохранения данных");
            }
#endif
        }
    }
}