using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameDevTV.Saving
{
    /// <summary>
    /// WebGL/Yandex adapter.
    ///
    /// YandexGame.savesData хранит один gameDataJson, поэтому несколько
    /// игровых сейвов хранятся внутри него как именованные слоты:
    ///
    /// {
    ///   "__saveSlots": {
    ///     "save0": { ...state... },
    ///     "save1": { ...state... }
    ///   }
    /// }
    ///
    /// Старый формат, где gameDataJson напрямую содержал state одного сейва,
    /// автоматически мигрируется в слот currentSaveFile (или save0).
    /// </summary>
    public class WebSavingAdapter : MonoBehaviour
    {
        private const string SlotsKey = "__saveSlots";
        private const string DefaultMigratedSlot = "save0";

        public static Action OnSaveLoaded;
        public static Action OnSaveSaved;

        private static bool dataLoaded;

        private void OnEnable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGame.GetDataEvent += OnYandexDataLoaded;

            // Если событие уже прошло до появления этого объекта, не остаёмся
            // навечно в состоянии "не загружено", если SDK уже предоставил данные.
            if (YandexGame.SDKEnabled &&
                (!string.IsNullOrEmpty(YandexGame.savesData.gameDataJson) ||
                 !string.IsNullOrEmpty(YandexGame.savesData.currentSaveFile)))
            {
                dataLoaded = true;
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

            Debug.Log(
                $"WebSavingAdapter: данные Yandex загружены. " +
                $"currentSaveFile='{YandexGame.savesData.currentSaveFile}', " +
                $"jsonLength={YandexGame.savesData.gameDataJson?.Length ?? 0}"
            );

#if UNITY_WEBGL && !UNITY_EDITOR
            MigrateOldSingleSaveIfNeeded();
#endif

            OnSaveLoaded?.Invoke();
        }

        public static bool IsDataLoaded()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return dataLoaded;
#else
            return true;
#endif
        }

        public static IEnumerator WaitForData(float maxWaitTime = 15f)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            float timer = 0f;

            while (!dataLoaded && timer < maxWaitTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!dataLoaded)
            {
                Debug.LogWarning(
                    "WebSavingAdapter: GetDataEvent не был получен за отведённое время. " +
                    "Сохранение/загрузка через Yandex остановлены, чтобы не затереть облачные данные."
                );
            }
#else
            yield return null;
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
        /// Сохраняет state в именованный слот.
        /// До получения GetDataEvent запись НЕ выполняется: это защищает
        /// существующее облачное сохранение от перезаписи пустыми данными.
        /// </summary>
        public static void SaveGameData(
            string saveFileName,
            Dictionary<string, object> gameData,
            int sceneIndex)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!dataLoaded)
            {
                Debug.LogWarning(
                    $"WebSavingAdapter: Save '{saveFileName}' отклонён — " +
                    "данные Yandex ещё не загружены."
                );
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(saveFileName))
                {
                    saveFileName = DefaultMigratedSlot;
                }

                JObject root = ReadRoot();
                JObject slots = GetSlots(root);

                JObject stateObject = JObject.FromObject(gameData, JsonSerializer.Create(GetJsonSettings()));

                // lastSceneBuildIndex должен находиться и внутри конкретного
                // слота, поэтому при загрузке слот полностью самодостаточен.
                stateObject["lastSceneBuildIndex"] = sceneIndex;

                slots[saveFileName] = stateObject;
                root[SlotsKey] = slots;

                string jsonData = root.ToString(Formatting.None);

                YandexGame.savesData.currentSaveFile = saveFileName;
                YandexGame.savesData.gameDataJson = jsonData;
                YandexGame.savesData.lastSceneBuildIndex = sceneIndex;

                YandexGame.SaveProgress();

                OnSaveSaved?.Invoke();

                Debug.Log(
                    $"WebSavingAdapter: сохранён слот '{saveFileName}', " +
                    $"scene={sceneIndex}, jsonLength={jsonData.Length}"
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"WebSavingAdapter: ошибка сохранения '{saveFileName}': {e}"
                );
            }
#endif
        }

        public static Dictionary<string, object> LoadGameData(string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!dataLoaded)
            {
                Debug.LogWarning(
                    $"WebSavingAdapter: Load '{saveFileName}' отклонён — " +
                    "данные Yandex ещё не загружены."
                );
                return new Dictionary<string, object>();
            }

            try
            {
                if (string.IsNullOrEmpty(saveFileName))
                {
                    saveFileName = GetCurrentSaveFileName();
                }

                if (string.IsNullOrEmpty(saveFileName))
                {
                    Debug.Log("WebSavingAdapter: имя сейва не задано.");
                    return new Dictionary<string, object>();
                }

                JObject root = ReadRoot();
                JObject slots = GetSlots(root);

                JToken slotToken = slots[saveFileName];

                // Совместимость со старым single-slot форматом.
                if (slotToken == null && !string.IsNullOrEmpty(YandexGame.savesData.gameDataJson))
                {
                    string currentName = YandexGame.savesData.currentSaveFile;

                    if (!string.IsNullOrEmpty(currentName) && currentName == saveFileName)
                    {
                        var oldState = DeserializeState(YandexGame.savesData.gameDataJson);
                        if (oldState.Count > 0)
                        {
                            oldState["lastSceneBuildIndex"] =
                                YandexGame.savesData.lastSceneBuildIndex;

                            Debug.Log(
                                $"WebSavingAdapter: найден старый single-slot сейв '{saveFileName}'."
                            );

                            return oldState;
                        }
                    }
                }

                if (slotToken == null || slotToken.Type == JTokenType.Null)
                {
                    Debug.Log(
                        $"WebSavingAdapter: слот '{saveFileName}' не найден."
                    );
                    return new Dictionary<string, object>();
                }

                Dictionary<string, object> gameData =
                    slotToken.ToObject<Dictionary<string, object>>(JsonSerializer.Create(GetJsonSettings()))
                    ?? new Dictionary<string, object>();

                Debug.Log(
                    $"WebSavingAdapter: загружен слот '{saveFileName}'."
                );

                return gameData;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"WebSavingAdapter: ошибка загрузки '{saveFileName}': {e}"
                );
                return new Dictionary<string, object>();
            }
#else
            return new Dictionary<string, object>();
#endif
        }

        public static bool SaveExists(string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!dataLoaded || string.IsNullOrEmpty(YandexGame.savesData.gameDataJson))
                return false;

            if (string.IsNullOrEmpty(saveFileName))
                return GetAvailableSaves().Count > 0;

            JObject root = ReadRoot();
            JObject slots = GetSlots(root);

            return slots[saveFileName] != null;
#else
            return false;
#endif
        }

        public static void DeleteSave(string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!dataLoaded)
            {
                Debug.LogWarning(
                    $"WebSavingAdapter: Delete '{saveFileName}' отклонён — " +
                    "данные Yandex ещё не загружены."
                );
                return;
            }

            try
            {
                JObject root = ReadRoot();
                JObject slots = GetSlots(root);

                if (string.IsNullOrEmpty(saveFileName))
                {
                    slots.RemoveAll();
                }
                else
                {
                    slots.Remove(saveFileName);
                }

                root[SlotsKey] = slots;

                List<string> remaining = GetSlotNames(slots);

                if (remaining.Count > 0)
                {
                    string nextCurrent = remaining[0];
                    YandexGame.savesData.currentSaveFile = nextCurrent;

                    Dictionary<string, object> nextState =
                        slots[nextCurrent].ToObject<Dictionary<string, object>>(
                            JsonSerializer.Create(GetJsonSettings())
                        ) ?? new Dictionary<string, object>();

                    YandexGame.savesData.lastSceneBuildIndex =
                        nextState.ContainsKey("lastSceneBuildIndex")
                            ? JsonSaveHelper.ToInt(nextState["lastSceneBuildIndex"])
                            : 0;
                }
                else
                {
                    YandexGame.savesData.currentSaveFile = "";
                    YandexGame.savesData.lastSceneBuildIndex = 0;
                }

                YandexGame.savesData.gameDataJson =
                    root.ToString(Formatting.None);

                YandexGame.SaveProgress();

                Debug.Log(
                    $"WebSavingAdapter: удалён слот '{saveFileName}'."
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"WebSavingAdapter: ошибка удаления '{saveFileName}': {e}"
                );
            }
#endif
        }

        public static List<string> GetAvailableSaves()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var saves = new List<string>();

            if (!dataLoaded || string.IsNullOrEmpty(YandexGame.savesData.gameDataJson))
                return saves;

            try
            {
                JObject root = ReadRoot();
                JObject slots = GetSlots(root);
                saves.AddRange(GetSlotNames(slots));
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"WebSavingAdapter: ошибка получения списка сейвов: {e}"
                );
            }

            return saves;
#else
            return new List<string>();
#endif
        }

        public static string GetCurrentSaveFileName()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!dataLoaded)
                return "";

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
                YandexGame.LoadProgress();
                Debug.Log("WebSavingAdapter: запрошена загрузка данных Yandex.");
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
            if (dataLoaded && YandexGame.SDKEnabled)
            {
                YandexGame.SaveProgress();
                Debug.Log("WebSavingAdapter: принудительное сохранение в Yandex.");
            }
            else
            {
                Debug.LogWarning(
                    "WebSavingAdapter: принудительное сохранение не выполнено — данные не загружены."
                );
            }
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static JObject ReadRoot()
        {
            string json = YandexGame.savesData.gameDataJson;

            if (string.IsNullOrEmpty(json))
            {
                return new JObject();
            }

            try
            {
                return JObject.Parse(json);
            }
            catch
            {
                // Старый формат: gameDataJson непосредственно является state.
                var oldState = DeserializeState(json);

                JObject root = new JObject();
                JObject slots = new JObject();

                string slotName = !string.IsNullOrEmpty(YandexGame.savesData.currentSaveFile)
                    ? YandexGame.savesData.currentSaveFile
                    : DefaultMigratedSlot;

                slots[slotName] =
                    JObject.FromObject(
                        oldState,
                        JsonSerializer.Create(GetJsonSettings())
                    );

                root[SlotsKey] = slots;

                return root;
            }
        }

        private static JObject GetSlots(JObject root)
        {
            JToken token = root[SlotsKey];

            if (token is JObject slots)
            {
                return slots;
            }

            // Если root ещё старого формата — мигрируем его в currentSaveFile/save0.
            JObject newSlots = new JObject();

            string slotName = !string.IsNullOrEmpty(YandexGame.savesData.currentSaveFile)
                ? YandexGame.savesData.currentSaveFile
                : DefaultMigratedSlot;

            newSlots[slotName] = root;

            return newSlots;
        }

        private static List<string> GetSlotNames(JObject slots)
        {
            var names = new List<string>();

            foreach (JProperty property in slots.Properties())
            {
                names.Add(property.Name);
            }

            return names;
        }

        private static Dictionary<string, object> DeserializeState(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    json,
                    GetJsonSettings()
                ) ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        private static void MigrateOldSingleSaveIfNeeded()
        {
            if (string.IsNullOrEmpty(YandexGame.savesData.gameDataJson))
                return;

            try
            {
                JObject root = JObject.Parse(YandexGame.savesData.gameDataJson);

                if (root[SlotsKey] is JObject)
                    return;
            }
            catch
            {
                // Старый формат обрабатывается через ReadRoot().
            }

            try
            {
                JObject migratedRoot = ReadRoot();
                JObject slots = GetSlots(migratedRoot);

                if (slots == null || slots.Count == 0)
                    return;

                migratedRoot[SlotsKey] = slots;
                YandexGame.savesData.gameDataJson =
                    migratedRoot.ToString(Formatting.None);

                string currentSlot = YandexGame.savesData.currentSaveFile;

                if (string.IsNullOrEmpty(currentSlot))
                {
                    foreach (JProperty property in slots.Properties())
                    {
                        currentSlot = property.Name;
                        break;
                    }

                    if (!string.IsNullOrEmpty(currentSlot))
                        YandexGame.savesData.currentSaveFile = currentSlot;
                }

                if (!string.IsNullOrEmpty(currentSlot) &&
                    slots[currentSlot] is JObject currentState &&
                    currentState["lastSceneBuildIndex"] != null)
                {
                    YandexGame.savesData.lastSceneBuildIndex =
                        JsonSaveHelper.ToInt(currentState["lastSceneBuildIndex"]);
                }

                YandexGame.SaveProgress();

                Debug.Log(
                    $"WebSavingAdapter: старый single-slot формат мигрирован " +
                    $"в {slots.Count} слот(а)."
                );
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"WebSavingAdapter: ошибка миграции старого сейва: {e}"
                );
            }
        }

#endif
    }
}
