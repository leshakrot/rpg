using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;
using Newtonsoft.Json;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Адаптер системы сохранений для Yandex Games.
    ///
    /// В Yandex хранится один JSON-объект пользователя. Поэтому несколько
    /// игровых слотов хранятся внутри этого JSON в виде словаря:
    /// "__saveSlots" -> имя слота -> состояние игры.
    ///
    /// Старый формат, где gameDataJson содержал состояние только одного
    /// сохранения, автоматически мигрируется в слот с именем
    /// savesData.currentSaveFile.
    /// </summary>
    public class WebSavingAdapter : MonoBehaviour
    {
        private const string SlotsKey = "__saveSlots";

        public static Action OnSaveLoaded;
        public static Action OnSaveSaved;

        private static bool dataLoaded;
        private static bool loadRequested;

        public static bool IsDataLoaded => dataLoaded;

        private void OnEnable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGame.GetDataEvent += OnYandexDataLoaded;

            // Если SDK уже готов к моменту появления адаптера, сами
            // запрашиваем данные. Это также позволяет повторно получить
            // событие, если первоначальная подписка была слишком поздней.
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
            dataLoaded = true;
            loadRequested = false;

#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log(
                $"WebSavingAdapter: данные Yandex загружены. " +
                $"Авторизация: {YandexGame.auth}, " +
                $"currentSaveFile: '{YandexGame.savesData.currentSaveFile}', " +
                $"размер gameDataJson: {YandexGame.savesData.gameDataJson?.Length ?? 0}");
#endif

            OnSaveLoaded?.Invoke();
        }

        /// <summary>
        /// Ждёт именно загрузки данных игрока, а не только готовности SDK.
        /// </summary>
        public static IEnumerator WaitForData(float maxWaitTime = 10f)
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
                Debug.LogWarning("WebSavingAdapter: YandexSDK не готов.");
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
                Debug.LogWarning("WebSavingAdapter: данные Yandex не загрузились за отведённое время.");
            }
#else
            yield return null;
#endif
        }

        private static void RequestLoadFromYandex()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGame.SDKEnabled || loadRequested || dataLoaded)
                return;

            loadRequested = true;
            YandexGame.LoadProgress();
            Debug.Log("WebSavingAdapter: запрошена загрузка данных из Yandex.");
#endif
        }

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
        /// Сохраняет состояние указанного слота, не уничтожая остальные слоты.
        /// </summary>
        public static void SaveGameData(
            string saveFileName,
            Dictionary<string, object> gameData,
            int sceneIndex)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (!dataLoaded)
                {
                    Debug.LogWarning(
                        $"WebSavingAdapter: попытка сохранить '{saveFileName}' до загрузки " +
                        "облачных данных. Сохранение отменено, чтобы не затереть существующие слоты.");
                    return;
                }

                if (string.IsNullOrEmpty(saveFileName))
                {
                    Debug.LogError("WebSavingAdapter: имя сохранения пустое.");
                    return;
                }

                string currentJson = YandexGame.savesData.gameDataJson;
                Dictionary<string, Dictionary<string, object>> slots =
                    ReadSlots(currentJson);

                gameData["lastSceneBuildIndex"] = sceneIndex;
                slots[saveFileName] = gameData;

                string jsonData = JsonConvert.SerializeObject(
                    new Dictionary<string, object>
                    {
                        [SlotsKey] = slots
                    },
                    GetJsonSettings());

                YandexGame.savesData.currentSaveFile = saveFileName;
                YandexGame.savesData.gameDataJson = jsonData;
                YandexGame.savesData.lastSceneBuildIndex = sceneIndex;

                YandexGame.SaveProgress();

                OnSaveSaved?.Invoke();

                Debug.Log(
                    $"WebSavingAdapter: сохранение '{saveFileName}' записано в Yandex. " +
                    $"Всего слотов: {slots.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"WebSavingAdapter: ошибка сохранения '{saveFileName}': {e}");
            }
#endif
        }

        /// <summary>
        /// Загружает состояние конкретного слота.
        /// </summary>
        public static Dictionary<string, object> LoadGameData(string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (!dataLoaded)
                {
                    Debug.LogWarning(
                        $"WebSavingAdapter: данные ещё не загружены. " +
                        $"Нельзя загрузить '{saveFileName}'.");
                    return new Dictionary<string, object>();
                }

                if (string.IsNullOrEmpty(YandexGame.savesData.gameDataJson))
                {
                    Debug.Log(
                        $"WebSavingAdapter: облачных сохранений нет. Запрошен '{saveFileName}'.");
                    return new Dictionary<string, object>();
                }

                Dictionary<string, Dictionary<string, object>> slots =
                    ReadSlots(YandexGame.savesData.gameDataJson);

                if (!slots.TryGetValue(saveFileName, out Dictionary<string, object> gameData))
                {
                    Debug.Log(
                        $"WebSavingAdapter: слот '{saveFileName}' не найден. " +
                        $"Текущий слот Yandex: '{YandexGame.savesData.currentSaveFile}'.");
                    return new Dictionary<string, object>();
                }

                return gameData;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"WebSavingAdapter: ошибка загрузки '{saveFileName}': {e}");
                return new Dictionary<string, object>();
            }
#else
            return new Dictionary<string, object>();
#endif
        }

        public static bool SaveExists(string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!dataLoaded || string.IsNullOrEmpty(saveFileName))
                return false;

            Dictionary<string, Dictionary<string, object>> slots =
                ReadSlots(YandexGame.savesData.gameDataJson);

            return slots.ContainsKey(saveFileName);
#else
            return false;
#endif
        }

        public static void DeleteSave(string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                if (!dataLoaded)
                {
                    Debug.LogWarning(
                        "WebSavingAdapter: нельзя удалить сохранение до загрузки данных.");
                    return;
                }

                Dictionary<string, Dictionary<string, object>> slots =
                    ReadSlots(YandexGame.savesData.gameDataJson);

                if (!slots.Remove(saveFileName))
                {
                    Debug.LogWarning(
                        $"WebSavingAdapter: слот '{saveFileName}' не найден.");
                    return;
                }

                if (slots.Count == 0)
                {
                    YandexGame.savesData.currentSaveFile = "";
                    YandexGame.savesData.gameDataJson = "";
                    YandexGame.savesData.lastSceneBuildIndex = 0;
                }
                else
                {
                    if (YandexGame.savesData.currentSaveFile == saveFileName)
                    {
                        string nextSave = null;
                        foreach (string name in slots.Keys)
                        {
                            nextSave = name;
                            break;
                        }

                        YandexGame.savesData.currentSaveFile = nextSave;
                    }

                    string jsonData = JsonConvert.SerializeObject(
                        new Dictionary<string, object>
                        {
                            [SlotsKey] = slots
                        },
                        GetJsonSettings());

                    YandexGame.savesData.gameDataJson = jsonData;

                    Dictionary<string, object> selected =
                        slots[YandexGame.savesData.currentSaveFile];

                    if (selected.ContainsKey("lastSceneBuildIndex"))
                    {
                        YandexGame.savesData.lastSceneBuildIndex =
                            JsonSaveHelper.ToInt(selected["lastSceneBuildIndex"]);
                    }
                }

                YandexGame.SaveProgress();

                Debug.Log(
                    $"WebSavingAdapter: слот '{saveFileName}' удалён. " +
                    $"Осталось слотов: {slots.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"WebSavingAdapter: ошибка удаления '{saveFileName}': {e}");
            }
#endif
        }

        public static List<string> GetAvailableSaves()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!dataLoaded)
                return new List<string>();

            Dictionary<string, Dictionary<string, object>> slots =
                ReadSlots(YandexGame.savesData.gameDataJson);

            return new List<string>(slots.Keys);
#else
            return new List<string>();
#endif
        }

        public static string GetCurrentSaveFileName()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return YandexGame.savesData.currentSaveFile ?? "";
#else
            return "";
#endif
        }

        public static void ForceLoadFromYandex()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (YandexGame.SDKEnabled)
            {
                dataLoaded = false;
                loadRequested = false;
                RequestLoadFromYandex();
            }
            else
            {
                Debug.LogWarning("WebSavingAdapter: YandexSDK не готов.");
            }
#endif
        }

        public static void ForceSaveToYandex()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (YandexGame.SDKEnabled)
            {
                YandexGame.SaveProgress();
                Debug.Log("WebSavingAdapter: принудительно вызвано сохранение Yandex.");
            }
            else
            {
                Debug.LogWarning("WebSavingAdapter: YandexSDK не готов.");
            }
#endif
        }

        private static Dictionary<string, Dictionary<string, object>> ReadSlots(string json)
        {
            var result = new Dictionary<string, Dictionary<string, object>>();

            if (string.IsNullOrEmpty(json))
                return result;

            try
            {
                Dictionary<string, object> root =
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        json,
                        GetJsonSettings());

                if (root == null)
                    return result;

                // Новый формат: { "__saveSlots": { "save1": {...}, ... } }
                if (root.TryGetValue(SlotsKey, out object slotsObject))
                {
                    if (slotsObject is Dictionary<string, object> slotsDictionary)
                    {
                        foreach (KeyValuePair<string, object> pair in slotsDictionary)
                        {
                            if (pair.Value is Dictionary<string, object> state)
                            {
                                result[pair.Key] = state;
                            }
                        }
                    }

                    return result;
                }

                // Старый формат: gameDataJson был непосредственно состоянием
                // одного сохранения. Мигрируем его в слот currentSaveFile.
                string legacySaveName = YandexGame.savesData.currentSaveFile;

                if (!string.IsNullOrEmpty(legacySaveName))
                {
                    result[legacySaveName] = root;

                    Debug.Log(
                        $"WebSavingAdapter: обнаружен старый формат сохранения. " +
                        $"Он мигрирован в слот '{legacySaveName}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"WebSavingAdapter: не удалось разобрать gameDataJson: {e}");
            }

            return result;
        }
    }
}
