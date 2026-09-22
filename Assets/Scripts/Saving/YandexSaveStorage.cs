using System;
using System.Collections.Generic;
using UnityEngine;
using YG;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Cloud storage using the exact YG API already present in the project.
    ///
    /// This class does not invent a second save format. Yandex receives the
    /// same JSON dictionary used by the old WebSavingAdapter.
    /// </summary>
    public sealed class YandexSaveStorage
    {
        public bool IsSupported
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public bool IsReadyAndAuthorized
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return YandexGame.SDKEnabled && YandexGame.auth;
#else
                return false;
#endif
            }
        }

        public bool HasAnyCloudData
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return !string.IsNullOrEmpty(YandexGame.savesData.gameDataJson);
#else
                return false;
#endif
            }
        }

        public bool Exists(string saveFile)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsReadyAndAuthorized || !HasAnyCloudData)
                return false;

            return string.IsNullOrEmpty(saveFile) ||
                   string.IsNullOrEmpty(YandexGame.savesData.currentSaveFile) ||
                   string.Equals(
                       YandexGame.savesData.currentSaveFile,
                       saveFile,
                       StringComparison.Ordinal);
#else
            return false;
#endif
        }

        public bool TryLoad(
            string saveFile,
            out Dictionary<string, object> state)
        {
            state = new Dictionary<string, object>();

#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsReadyAndAuthorized)
                return false;

            string json = YandexGame.savesData.gameDataJson;

            if (string.IsNullOrEmpty(json))
                return false;

            string currentFile = YandexGame.savesData.currentSaveFile;

            if (!string.IsNullOrEmpty(saveFile) &&
                !string.IsNullOrEmpty(currentFile) &&
                !string.Equals(currentFile, saveFile, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                state = SaveJson.Deserialize(json);

                // Old WebSavingAdapter stored the scene index outside the JSON.
                // Keep accepting that format.
                state["lastSceneBuildIndex"] =
                    YandexGame.savesData.lastSceneBuildIndex;

                return state.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[Saving] Failed to deserialize Yandex save '{saveFile}': {e}");
                state = new Dictionary<string, object>();
                return false;
            }
#else
            return false;
#endif
        }

        public bool TrySave(
            string saveFile,
            Dictionary<string, object> state)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsReadyAndAuthorized || state == null)
                return false;

            try
            {
                string json = SaveJson.Serialize(state, false);

                int sceneIndex = 0;

                if (state.TryGetValue(
                    "lastSceneBuildIndex",
                    out object sceneValue))
                {
                    sceneIndex = JsonSaveHelper.ToInt(sceneValue);
                }

                YandexGame.savesData.currentSaveFile = saveFile;
                YandexGame.savesData.gameDataJson = json;
                YandexGame.savesData.lastSceneBuildIndex = sceneIndex;

                YandexGame.SaveProgress();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[Saving] Failed to save Yandex data '{saveFile}': {e}");
                return false;
            }
#else
            return false;
#endif
        }

        public bool Delete(string saveFile)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!IsReadyAndAuthorized)
                return false;

            string currentFile = YandexGame.savesData.currentSaveFile;

            if (!string.IsNullOrEmpty(saveFile) &&
                !string.IsNullOrEmpty(currentFile) &&
                !string.Equals(currentFile, saveFile, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                YandexGame.savesData.currentSaveFile = string.Empty;
                YandexGame.savesData.gameDataJson = string.Empty;
                YandexGame.savesData.lastSceneBuildIndex = 0;

                YandexGame.SaveProgress();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[Saving] Failed to delete Yandex save '{saveFile}': {e}");
                return false;
            }
#else
            return false;
#endif
        }

        public IEnumerable<string> ListSaves()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsReadyAndAuthorized && HasAnyCloudData)
            {
                yield return string.IsNullOrEmpty(
                    YandexGame.savesData.currentSaveFile)
                    ? "AutSave"
                    : YandexGame.savesData.currentSaveFile;
            }
#else
            yield break;
#endif
        }

        public void ForceLoad()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (YandexGame.SDKEnabled)
                YandexGame.LoadProgress();
#endif
        }

        public void ForceSave()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (YandexGame.SDKEnabled && YandexGame.auth)
                YandexGame.SaveProgress();
#endif
        }
    }
}
