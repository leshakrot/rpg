using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using YG;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Облачное хранилище для АВТОРИЗОВАННЫХ игроков Яндекс.Игр (PluginYG).
    ///
    /// В YandexGame.savesData есть только одно поле gameDataJson, поэтому несколько
    /// сейвов лежат в нём как именованные слоты (формат прежнего WebSavingAdapter):
    ///
    ///   { "__saveSlots": { "Slot1": { ...state... }, "Slot2": { ...state... } } }
    ///
    /// Старый формат "весь gameDataJson = state одного сейва" читается автоматически
    /// (превращается в слот currentSaveFile / "AutoSave" при первой же записи).
    ///
    /// Используются поля SavesYG: currentSaveFile, gameDataJson, lastSceneBuildIndex.
    /// </summary>
    public sealed class YandexSaveStorage : ISaveStorage
    {
        private const string SlotsKey = "__saveSlots";
        private const string LegacySlotName = "AutoSave";
        private const string LastSceneKey = "lastSceneBuildIndex";

        // Лимит Яндекса на данные игрока - 200 КБ. Оставляем запас.
        private const int MaxJsonLength = 190000;

        private readonly bool allowInEditor;

        public YandexSaveStorage(bool allowInEditor)
        {
            this.allowInEditor = allowInEditor;
        }

        public string Name => "Yandex";

        /// <summary>Платформа вообще может работать с Яндексом (WebGL-билд, либо редактор с галочкой).</summary>
        public bool IsSupported =>
            Application.platform == RuntimePlatform.WebGLPlayer ||
            (allowInEditor && Application.isEditor);

        /// <summary>SDK инициализирован и данные игрока уже получены (GetDataEvent прошёл).</summary>
        public bool IsSdkReady => IsSupported && YandexGame.SDKEnabled;

        /// <summary>Игрок авторизован на Яндексе -> его прогресс хранится в облаке.</summary>
        public bool IsAuthorized => IsSdkReady && YandexGame.auth;

        // ---------------------------------------------------------------- API

        public bool Exists(string saveFile)
        {
            if (!IsAuthorized || string.IsNullOrEmpty(saveFile))
                return false;

            try
            {
                return GetSlots(ReadRoot()).Property(saveFile) != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Saving] Yandex Exists('{saveFile}'): {e}");
                return false;
            }
        }

        public bool TryLoad(string saveFile, out Dictionary<string, object> state)
        {
            state = new Dictionary<string, object>();

            if (!IsAuthorized || string.IsNullOrEmpty(saveFile))
                return false;

            try
            {
                JObject slot = GetSlots(ReadRoot())[saveFile] as JObject;

                if (slot == null)
                    return false;

                state = slot.ToObject<Dictionary<string, object>>(SaveJson.CreateSerializer())
                        ?? new Dictionary<string, object>();

                return state.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Saving] Не удалось прочитать облачный сейв '{saveFile}': {e}");
                state = new Dictionary<string, object>();
                return false;
            }
        }

        public bool TrySave(string saveFile, Dictionary<string, object> state)
        {
            if (!IsAuthorized || state == null || string.IsNullOrEmpty(saveFile))
                return false;

            try
            {
                JObject root = ReadRoot();
                JObject slots = GetSlots(root);

                slots[saveFile] = JObject.FromObject(state, SaveJson.CreateSerializer());

                string json = root.ToString(Newtonsoft.Json.Formatting.None);

                if (json.Length > MaxJsonLength)
                {
                    Debug.LogError(
                        $"[Saving] Облачные данные ({json.Length} симв.) превышают безопасный лимит " +
                        $"{MaxJsonLength} (лимит Яндекса - 200 КБ). Сейв '{saveFile}' НЕ записан. " +
                        "Удалите ненужные сохранения.");
                    return false;
                }

                int sceneIndex = state.TryGetValue(LastSceneKey, out object sceneValue)
                    ? SaveJson.ToInt(sceneValue)
                    : 0;

                YandexGame.savesData.currentSaveFile = saveFile;
                YandexGame.savesData.gameDataJson = json;
                YandexGame.savesData.lastSceneBuildIndex = sceneIndex;

                YandexGame.SaveProgress();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Saving] Не удалось записать облачный сейв '{saveFile}': {e}");
                return false;
            }
        }

        public bool Delete(string saveFile)
        {
            if (!IsAuthorized || string.IsNullOrEmpty(saveFile))
                return false;

            try
            {
                JObject root = ReadRoot();
                JObject slots = GetSlots(root);

                if (!slots.Remove(saveFile))
                    return false;

                // Если удалили текущий (или указатель уже висит в пустоту) - переключаемся на первый оставшийся.
                string current = YandexGame.savesData.currentSaveFile;

                if (string.IsNullOrEmpty(current) || slots.Property(current) == null)
                {
                    JProperty first = null;

                    foreach (JProperty property in slots.Properties())
                    {
                        first = property;
                        break;
                    }

                    YandexGame.savesData.currentSaveFile = first != null ? first.Name : string.Empty;

                    JObject firstSlot = first != null ? first.Value as JObject : null;
                    YandexGame.savesData.lastSceneBuildIndex =
                        firstSlot != null ? (int?)firstSlot[LastSceneKey] ?? 0 : 0;
                }

                YandexGame.savesData.gameDataJson = root.ToString(Newtonsoft.Json.Formatting.None);
                YandexGame.SaveProgress();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Saving] Не удалось удалить облачный сейв '{saveFile}': {e}");
                return false;
            }
        }

        public List<string> ListSaves()
        {
            var result = new List<string>();

            if (!IsAuthorized)
                return result;

            try
            {
                foreach (JProperty property in GetSlots(ReadRoot()).Properties())
                    result.Add(property.Name);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Saving] Не удалось получить список облачных сейвов: {e}");
            }

            return result;
        }

        public string GetCurrentSaveName()
        {
            return IsSdkReady ? (YandexGame.savesData.currentSaveFile ?? string.Empty) : string.Empty;
        }

        public void SetCurrentSaveName(string saveFile)
        {
            // Меняем только в памяти; в облако уйдёт вместе со следующим SaveProgress.
            if (IsSdkReady)
                YandexGame.savesData.currentSaveFile = saveFile ?? string.Empty;
        }

        // ------------------------------------------------------------ helpers

        private static JObject NewRoot()
        {
            return new JObject { [SlotsKey] = new JObject() };
        }

        private static JObject GetSlots(JObject root)
        {
            return (JObject)root[SlotsKey];
        }

        /// <summary>
        /// Читает gameDataJson и ВСЕГДА возвращает корень нового формата со словарём слотов.
        /// Ничего не пишет обратно в savesData.
        /// </summary>
        private static JObject ReadRoot()
        {
            string json = YandexGame.savesData.gameDataJson;

            if (string.IsNullOrEmpty(json))
                return NewRoot();

            JObject parsed;

            try
            {
                parsed = JObject.Parse(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Saving] gameDataJson повреждён и не читается, начинаем с чистого листа: {e.Message}");
                return NewRoot();
            }

            if (parsed[SlotsKey] is JObject)
                return parsed;

            // Старый формат: весь JSON - это state одного сейва.
            JObject root = NewRoot();

            if (parsed.HasValues)
            {
                string slotName = YandexGame.savesData.currentSaveFile;

                if (string.IsNullOrEmpty(slotName))
                    slotName = LegacySlotName;

                if (parsed[LastSceneKey] == null)
                    parsed[LastSceneKey] = YandexGame.savesData.lastSceneBuildIndex;

                GetSlots(root)[slotName] = parsed;
            }

            return root;
        }
    }
}
