using System;
using System.Collections;
using UnityEngine;
using YG;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Optional helper for scenes that want an explicit "Yandex is ready"
    /// lifecycle. SavingSystem does not require this component.
    /// </summary>
    public class WebSavingInitializer : MonoBehaviour
    {
        [Header("Настройки инициализации")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool waitForSDK = true;
        [SerializeField] private float maxWaitTime = 10f;

        [Header("Отладка")]
        [SerializeField] private bool enableDebugLogs = true;

        private bool isInitialized;

        public static event Action OnReady;

        private void Start()
        {
            if (initializeOnStart)
                InitializeWebSaving();
        }

        private void OnEnable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGame.GetDataEvent += OnYandexDataReceived;
#endif
        }

        private void OnDisable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGame.GetDataEvent -= OnYandexDataReceived;
#endif
        }

        public void InitializeWebSaving()
        {
            if (isInitialized)
                return;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (waitForSDK)
                StartCoroutine(WaitForSDKAndInitialize());
            else
                CompleteInitialization();
#else
            CompleteInitialization();
#endif
        }

        private IEnumerator WaitForSDKAndInitialize()
        {
            float timer = 0f;

            while (!YandexGame.SDKEnabled &&
                   timer < maxWaitTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!YandexGame.SDKEnabled)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning(
                        "WebSavingInitializer: YandexSDK did not become ready " +
                        "within the timeout. Local guest saving remains available.");
                }
            }

            CompleteInitialization();
        }

        private void CompleteInitialization()
        {
            isInitialized = true;

            if (enableDebugLogs)
                Debug.Log("WebSavingInitializer: Initialization complete.");

            OnReady?.Invoke();
        }

        private void OnYandexDataReceived()
        {
            if (enableDebugLogs)
                Debug.Log("WebSavingInitializer: Yandex data received.");
        }

        public bool IsReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return isInitialized && YandexGame.SDKEnabled;
#else
            return true;
#endif
        }

        public bool IsAuthorized()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsReady() && YandexGame.auth;
#else
            return false;
#endif
        }

        [ContextMenu("Show System Status")]
        public void ShowSystemStatus()
        {
            Debug.Log("=== WebSavingInitializer Status ===");
            Debug.Log($"Initialized: {isInitialized}");

#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log($"YandexSDK ready: {YandexGame.SDKEnabled}");
            Debug.Log($"Authorized: {YandexGame.auth}");
            Debug.Log($"Current save: '{YandexGame.savesData.currentSaveFile}'");
            Debug.Log(
                $"Has cloud data: {!string.IsNullOrEmpty(YandexGame.savesData.gameDataJson)}");
            Debug.Log(
                $"Last scene: {YandexGame.savesData.lastSceneBuildIndex}");
#else
            Debug.Log("Platform: non-WebGL/editor");
#endif

            Debug.Log("==================================");
        }
    }
}
