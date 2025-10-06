using UnityEngine;
using UnityEngine.UI;
using GameDevTV.Saving;
using RPG.SceneManagement;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Демонстрационный компонент для тестирования веб-сохранений
    /// Показывает как использовать интегрированную систему сохранений с YandexSDK
    /// </summary>
    public class WebSavingDemo : MonoBehaviour
    {
        [Header("UI Элементы")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private InputField saveNameInput;
        [SerializeField] private Text statusText;
        [SerializeField] private Text saveInfoText;

        [Header("Настройки")]
        [SerializeField] private bool autoUpdateUI = true;

        private SavingWrapper savingWrapper;
        private WebSavingInitializer webInitializer;

        private void Start()
        {
            // Получаем компоненты
            savingWrapper = FindObjectOfType<SavingWrapper>();
            webInitializer = FindObjectOfType<WebSavingInitializer>();

            // Настраиваем кнопки
            SetupButtons();
            
            // Обновляем UI
            if (autoUpdateUI)
            {
                UpdateUI();
                InvokeRepeating(nameof(UpdateUI), 1f, 1f);
            }

            // Подписываемся на события веб-адаптера
            WebSavingAdapter.OnSaveLoaded += OnSaveLoaded;
            WebSavingAdapter.OnSaveSaved += OnSaveSaved;
        }

        private void OnDestroy()
        {
            // Отписываемся от событий
            WebSavingAdapter.OnSaveLoaded -= OnSaveLoaded;
            WebSavingAdapter.OnSaveSaved -= OnSaveSaved;
        }

        private void SetupButtons()
        {
            // Кнопки основных операций
            if (saveButton != null)
                saveButton.onClick.AddListener(SaveGame);
            if (loadButton != null)
                loadButton.onClick.AddListener(LoadGame);
            if (deleteButton != null)
                deleteButton.onClick.AddListener(DeleteSave);
            if (newGameButton != null)
                newGameButton.onClick.AddListener(StartNewGame);
            if (continueButton != null)
                continueButton.onClick.AddListener(ContinueGame);
        }

        public void SaveGame()
        {
            if (savingWrapper == null)
            {
                UpdateStatus("SavingWrapper не найден!");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (webInitializer != null && !webInitializer.IsReady())
            {
                UpdateStatus("Веб-инициализатор не готов!");
                return;
            }
#endif

            try
            {
                savingWrapper.Save();
                UpdateStatus("Игра сохранена");
            }
            catch (System.Exception e)
            {
                UpdateStatus($"Ошибка сохранения: {e.Message}");
                Debug.LogError($"Ошибка сохранения: {e}");
            }
        }

        public void LoadGame()
        {
            if (savingWrapper == null)
            {
                UpdateStatus("SavingWrapper не найден!");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (webInitializer != null && !webInitializer.IsReady())
            {
                UpdateStatus("Веб-инициализатор не готов!");
                return;
            }
#endif

            try
            {
                savingWrapper.Load();
                UpdateStatus("Игра загружена");
            }
            catch (System.Exception e)
            {
                UpdateStatus($"Ошибка загрузки: {e.Message}");
                Debug.LogError($"Ошибка загрузки: {e}");
            }
        }

        public void DeleteSave()
        {
            if (savingWrapper == null)
            {
                UpdateStatus("SavingWrapper не найден!");
                return;
            }

            try
            {
                var savingSystem = savingWrapper.GetComponent<SavingSystem>();
                if (savingSystem != null)
                {
                    string currentSave = "";
#if UNITY_WEBGL && !UNITY_EDITOR
                    currentSave = WebSavingAdapter.GetCurrentSaveFileName();
#endif
                    savingSystem.Delete(currentSave);
                    UpdateStatus("Сохранение удалено");
                }
            }
            catch (System.Exception e)
            {
                UpdateStatus($"Ошибка удаления: {e.Message}");
                Debug.LogError($"Ошибка удаления: {e}");
            }
        }

        public void StartNewGame()
        {
            if (savingWrapper == null)
            {
                UpdateStatus("SavingWrapper не найден!");
                return;
            }

            try
            {
                string saveName = "AutoSave";
                if (saveNameInput != null && !string.IsNullOrEmpty(saveNameInput.text))
                {
                    saveName = saveNameInput.text;
                }

                savingWrapper.NewGame(saveName);
                UpdateStatus($"Новая игра начата: {saveName}");
            }
            catch (System.Exception e)
            {
                UpdateStatus($"Ошибка новой игры: {e.Message}");
                Debug.LogError($"Ошибка новой игры: {e}");
            }
        }

        public void ContinueGame()
        {
            if (savingWrapper == null)
            {
                UpdateStatus("SavingWrapper не найден!");
                return;
            }

            try
            {
                savingWrapper.ContinueGame();
                UpdateStatus("Игра продолжена");
            }
            catch (System.Exception e)
            {
                UpdateStatus($"Ошибка продолжения: {e.Message}");
                Debug.LogError($"Ошибка продолжения: {e}");
            }
        }

        private void UpdateUI()
        {
            if (savingWrapper == null) return;

            var savingSystem = savingWrapper.GetComponent<SavingSystem>();
            if (savingSystem == null) return;

            // Обновляем информацию о сохранениях
            UpdateSaveInfo();

            // Обновляем состояние кнопок
            bool hasSave = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            hasSave = !string.IsNullOrEmpty(WebSavingAdapter.GetCurrentSaveFileName());
#else
            // Для других платформ проверяем через обычную систему
            var saves = savingSystem.ListSaves();
            foreach (var save in saves)
            {
                hasSave = true;
                break;
            }
#endif

            if (loadButton != null)
                loadButton.interactable = hasSave;
            if (deleteButton != null)
                deleteButton.interactable = hasSave;
            if (continueButton != null)
                continueButton.interactable = hasSave;
        }

        private void UpdateSaveInfo()
        {
            if (saveInfoText == null) return;

            string info = "=== Информация о сохранениях ===\n";

#if UNITY_WEBGL && !UNITY_EDITOR
            info += $"Платформа: WebGL\n";
            info += $"YandexSDK: {(YG.YandexGame.SDKEnabled ? "Готов" : "Не готов")}\n";
            info += $"Текущий файл: '{WebSavingAdapter.GetCurrentSaveFileName()}'\n";
            
            var saves = WebSavingAdapter.GetAvailableSaves();
            info += $"Доступно сохранений: {saves.Count}\n";
            foreach (var save in saves)
            {
                info += $"- {save}\n";
            }
#else
            info += $"Платформа: Десктоп\n";
            if (savingWrapper != null)
            {
                var savingSystem = savingWrapper.GetComponent<SavingSystem>();
                if (savingSystem != null)
                {
                    var saves = savingSystem.ListSaves();
                    int count = 0;
                    foreach (var save in saves)
                    {
                        if (count == 0) info += "Доступные сохранения:\n";
                        info += $"- {save}\n";
                        count++;
                    }
                    if (count == 0) info += "Нет сохранений\n";
                }
            }
#endif

            saveInfoText.text = info;
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            }
            Debug.Log($"WebSavingDemo: {message}");
        }

        private void OnSaveLoaded()
        {
            UpdateStatus("Данные загружены через YandexSDK");
        }

        private void OnSaveSaved()
        {
            UpdateStatus("Данные сохранены через YandexSDK");
        }

        /// <summary>
        /// Принудительная синхронизация с YandexSDK
        /// </summary>
        public void ForceSyncWithYandex()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebSavingAdapter.ForceLoadFromYandex();
            UpdateStatus("Принудительная синхронизация с YandexSDK");
#else
            UpdateStatus("Синхронизация доступна только в веб-билде");
#endif
        }
    }
}