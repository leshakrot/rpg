using System;
using System.Collections.Generic;
using UnityEngine;
using YG;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Compatibility facade for the old WebSavingAdapter API.
    ///
    /// SavingSystem is the preferred entry point. This class remains because
    /// other project scripts may already call these static methods.
    /// </summary>
    public class WebSavingAdapter : MonoBehaviour
    {
        public static Action OnSaveLoaded;
        public static Action OnSaveSaved;

        private void OnEnable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexGame.GetDataEvent += OnYandexDataLoaded;
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
            OnSaveLoaded?.Invoke();
        }

        public static void SaveGameData(
            string saveFileName,
            Dictionary<string, object> gameData,
            int sceneIndex)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!YandexGame.SDKEnabled || !YandexGame.auth)
                return;

            if (gameData == null)
                gameData = new Dictionary<string, object>();

            gameData["lastSceneBuildIndex"] = sceneIndex;

            var storage = new YandexSaveStorage();

            if (storage.TrySave(saveFileName, gameData))
                OnSaveSaved?.Invoke();
#endif
        }

        public static Dictionary<string, object> LoadGameData(
            string saveFileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var storage = new YandexSaveStorage();

            if (storage.TryLoad(
                saveFileName,
                out Dictionary<string, object> state))
            {
                OnSaveLoaded?.Invoke();
                return state;
            }
#endif
            return new Dictionary<string, object>();
        }

        public static bool SaveExists(string saveFileName)
        {
            return new YandexSaveStorage().Exists(saveFileName);
        }

        public static void DeleteSave(string saveFileName)
        {
            new YandexSaveStorage().Delete(saveFileName);
        }

        public static List<string> GetAvailableSaves()
        {
            return new List<string>(
                new YandexSaveStorage().ListSaves());
        }

        public static string GetCurrentSaveFileName()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return YandexGame.savesData.currentSaveFile ?? string.Empty;
#else
            return string.Empty;
#endif
        }

        public static void ForceLoadFromYandex()
        {
            new YandexSaveStorage().ForceLoad();
        }

        public static void ForceSaveToYandex()
        {
            new YandexSaveStorage().ForceSave();
        }
    }
}
