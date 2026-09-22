using System;
using System.Collections.Generic;
using UnityEngine;
using YG;
using Newtonsoft.Json;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Адаптер для интеграции системы сохранений с YandexSDK
    /// Использует стандартный подход YandexSDK для сохранения данных
    /// </summary>
    public class WebSavingAdapter : MonoBehaviour
    {
        // События для отслеживания изменений сохранений
        public static Action OnSaveLoaded;
        public static Action OnSaveSaved;

        private static bool dataLoaded;
        private static bool loadRequested;

        public static bool IsDataLoaded
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return dataLoaded;
#else
                return true;
#endif
            }
        }

        private void OnEnable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGame.GetDataEvent += OnYandexDataLoaded;

            // Событие могло произойти до включения этого компонента.
            // Повторно запрашиваем данные, чтобы не зависеть от порядка Start/Awake.
            if (YandexGame.SDKEnabled && !dataLoaded)
            {
                RequestLoadFromYandex();
            }
#endif
        }

        private void OnDisable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGame.GetDataEvent -= OnYandexDataLoaded;
#endif
        }

        private void OnYandexDataLoaded()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            dataLoaded = true;
            loadRequested = false;

            Debug.Log(
                $"WebSavingAdapter: Yandex data loaded. " +
                $"Save='{YandexGame.savesData.currentSaveFile}', " +
                $"HasData={!string.IsNullOrEmpty(YandexGame.savesData.gameDataJson)}, " +
                $"Scene={YandexGame.savesData.lastSceneBuildIndex}"
            );

            OnSaveLoaded?.Invoke();
#endif
        }

        public static void RequestLoadFromYandex()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGame.SDKEnabled)
            {
                Debug.LogWarning("WebSavingAdapter: Yandex SDK is not ready.");
                return;
            }

            if (dataLoaded || loadRequested) return;

            loadRequested = true;
            YandexGame.LoadProgress();
            Debug.Log("WebSavingAdapter: Requested Yandex save data.");
#endif
        }

        public static System.Collections.IEnumerator WaitForData(float maxWaitTime)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            float timer = 0f;

            while (!YandexGame.SDKEnabled && timer < maxWaitTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!YandexGame.SDKEnabled)
            {
                Debug.LogWarning("WebSavingAdapter: Yandex SDK did not become ready in time.");
                yield break;
            }

            if (!dataLoaded)
            {
                RequestLoadFromYandex();
            }

            timer = 0f;

            while (!dataLoaded && timer < maxWaitTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!dataLoaded)
            {
                Debug.LogWarning("WebSavingAdapter: Yandex save data did not arrive in time.");
            }
#else
            yield return null;
#endif
        }

        /// <summary>
        /// Получает настройки JSON сериализации с кастомными конвертерами для Unity типов
        /// </summary>
        private static JsonSerializerSettings GetJsonSettings()
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.None,
                Converters = new JsonConverter[]
                {
                    new Vector3JsonConverter(),
                    new QuaternionJsonConverter(),
                    new ColorJsonConverter()
                }
            };
        }

        /// <summary>
        /// Сохраняет данные игры в YandexSDK
        /// </summary>
        public static void SaveGameData(string saveFileName, Dictionary<string, object> gameData, int sceneIndex)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // Сериализуем данные игры в JSON с кастомными конвертерами
                string jsonData = JsonConvert.SerializeObject(gameData, GetJsonSettings());

                // Сохраняем в YandexSDK
                YandexGame.savesData.currentSaveFile = saveFileName;
                YandexGame.savesData.gameDataJson = jsonData;
                YandexGame.savesData.lastSceneBuildIndex = sceneIndex;

                // Выполняем сохранение через YandexSDK
                YandexGame.SaveProgress();
                
                dataLoaded = true;
                OnSaveSaved?.Invoke();
                Debug.Log($"WebSavingAdapter: Игра сохранена '{saveFileName}' через YandexSDK");
            }
            catch (Exception e)
            {
                Debug.LogError($"WebSavingAdapter: Ошибка сохранения через YandexSDK: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Загружает данные игры из YandexSDK
        /// </summary>
        public static Dictionary<string, object> LoadGameData(string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!dataLoaded)
            {
                Debug.LogWarning(
                    $"WebSavingAdapter: Load requested before Yandex data was loaded. File='{saveFileName}'"
                );
                return new Dictionary<string, object>();
            }

            try
            {
                if (string.IsNullOrEmpty(YandexGame.savesData.gameDataJson))
                {
                    Debug.Log($"WebSavingAdapter: Нет сохраненных данных для '{saveFileName}'");
                    return new Dictionary<string, object>();
                }

                // Если указано конкретное имя файла, проверяем соответствие
                if (!string.IsNullOrEmpty(saveFileName) && 
                    !string.IsNullOrEmpty(YandexGame.savesData.currentSaveFile) &&
                    YandexGame.savesData.currentSaveFile != saveFileName)
                {
                    Debug.Log($"WebSavingAdapter: Файл сохранения '{saveFileName}' не найден. Текущий: '{YandexGame.savesData.currentSaveFile}'");
                    return new Dictionary<string, object>();
                }

                // Десериализуем данные с кастомными конвертерами
                var gameData = JsonConvert.DeserializeObject<Dictionary<string, object>>(YandexGame.savesData.gameDataJson, GetJsonSettings());
                
                // Добавляем информацию о последней сцене
                if (gameData != null)
                {
                    gameData["lastSceneBuildIndex"] = YandexGame.savesData.lastSceneBuildIndex;
                    Debug.Log($"WebSavingAdapter: Данные игры загружены '{YandexGame.savesData.currentSaveFile}' из YandexSDK");
                    return gameData;
                }
                
                return new Dictionary<string, object>();
            }
            catch (Exception e)
            {
                Debug.LogError($"WebSavingAdapter: Ошибка загрузки из YandexSDK: {e.Message}");
                return new Dictionary<string, object>();
            }
#else
            return new Dictionary<string, object>();
#endif
        }

        /// <summary>
        /// Проверяет существование сохранения
        /// </summary>
        public static bool SaveExists(string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!dataLoaded)
                return false;

            if (string.IsNullOrEmpty(YandexGame.savesData.gameDataJson))
                return false;

            // Если не указано имя файла, считаем что сохранение есть
            if (string.IsNullOrEmpty(saveFileName))
                return true;

            // Проверяем соответствие имени файла
            return YandexGame.savesData.currentSaveFile == saveFileName;
#else
            return false;
#endif
        }

        /// <summary>
        /// Удаляет сохранение
        /// </summary>
        public static void DeleteSave(string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // Если указано конкретное имя и оно совпадает с текущим, или если имя не указано
                if (string.IsNullOrEmpty(saveFileName) || YandexGame.savesData.currentSaveFile == saveFileName)
                {
                    YandexGame.savesData.currentSaveFile = "";
                    YandexGame.savesData.gameDataJson = "";
                    YandexGame.savesData.lastSceneBuildIndex = 0;
                    
                    YandexGame.SaveProgress();
                    Debug.Log($"WebSavingAdapter: Сохранение '{saveFileName}' удалено");
                }
                else
                {
                    Debug.LogWarning($"WebSavingAdapter: Попытка удалить несуществующее сохранение '{saveFileName}'");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"WebSavingAdapter: Ошибка удаления сохранения: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Получает список доступных сохранений
        /// </summary>
        public static List<string> GetAvailableSaves()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var saves = new List<string>();
            
            // В текущей реализации поддерживается одно основное сохранение
            if (!string.IsNullOrEmpty(YandexGame.savesData.gameDataJson))
            {
                string saveName = !string.IsNullOrEmpty(YandexGame.savesData.currentSaveFile) 
                    ? YandexGame.savesData.currentSaveFile 
                    : "Автосохранение";
                saves.Add(saveName);
            }
            
            return saves;
#else
            return new List<string>();
#endif
        }

        /// <summary>
        /// Получает текущее имя файла сохранения
        /// </summary>
        public static string GetCurrentSaveFileName()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return YandexGame.savesData.currentSaveFile ?? "";
#else
            return "";
#endif
        }

        /// <summary>
        /// Принудительно загружает данные из YandexSDK
        /// </summary>
        public static void ForceLoadFromYandex()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (YandexGame.SDKEnabled)
            {
                dataLoaded = false;
                loadRequested = false;
                RequestLoadFromYandex();
                Debug.Log("WebSavingAdapter: Принудительная загрузка из YandexSDK");
            }
            else
            {
                Debug.LogWarning("WebSavingAdapter: YandexSDK не готов");
            }
#endif
        }

        /// <summary>
        /// Принудительно сохраняет данные в YandexSDK
        /// </summary>
        public static void ForceSaveToYandex()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (YandexGame.SDKEnabled)
            {
                YandexGame.SaveProgress();
                Debug.Log("WebSavingAdapter: Принудительное сохранение в YandexSDK");
            }
            else
            {
                Debug.LogWarning("WebSavingAdapter: YandexSDK не готов");
            }
#endif
        }
    }
}