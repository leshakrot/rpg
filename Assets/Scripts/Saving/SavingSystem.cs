using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace GameDevTV.Saving
{
    public class SavingSystem : MonoBehaviour
    {
        [Header("Веб-интеграция")]
        [SerializeField] private bool waitForYandexSDK = true;
        [SerializeField] private float maxWaitTime = 15f;

        public static event Action OnRestoreStateComplete;

        private bool isWebPlatform
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

        public IEnumerator LoadLastScene(string saveFile)
        {
            if (isWebPlatform && waitForYandexSDK)
            {
                yield return WaitForSaveSystem();
            }

            Dictionary<string, object> state = LoadFile(saveFile);

            int buildIndex = SceneManager.GetActiveScene().buildIndex;

            if (state.ContainsKey("lastSceneBuildIndex"))
            {
                buildIndex = JsonSaveHelper.ToInt(state["lastSceneBuildIndex"]);
            }

            yield return SceneManager.LoadSceneAsync(buildIndex);

            RestoreState(state);
        }

        public IEnumerator WaitForSaveSystem()
        {
            if (isWebPlatform && waitForYandexSDK)
            {
                yield return WebSavingAdapter.WaitForData(maxWaitTime);
            }
        }

        public void Save(string saveFile)
        {
            if (isWebPlatform && waitForYandexSDK && !WebSavingAdapter.IsDataLoaded())
            {
                Debug.LogWarning(
                    $"SavingSystem: Save '{saveFile}' пропущен — данные Yandex ещё не загружены."
                );
                return;
            }

            Dictionary<string, object> state = LoadFile(saveFile);
            CaptureState(state);
            SaveFile(saveFile, state);
        }

        public void Delete(string saveFile)
        {
            if (isWebPlatform)
            {
                WebSavingAdapter.DeleteSave(saveFile);
            }
            else
            {
                File.Delete(GetPathFromSaveFile(saveFile));
            }
        }

        public void Load(string saveFile)
        {
            if (isWebPlatform && waitForYandexSDK && !WebSavingAdapter.IsDataLoaded())
            {
                Debug.LogWarning(
                    $"SavingSystem: Load '{saveFile}' пропущен — данные Yandex ещё не загружены."
                );
                return;
            }

            RestoreState(LoadFile(saveFile));
        }

        public bool SaveFileExists(string saveFile)
        {
            if (isWebPlatform)
            {
                return WebSavingAdapter.SaveExists(saveFile);
            }

            return File.Exists(GetPathFromSaveFile(saveFile));
        }

        public IEnumerable<string> ListSaves()
        {
            if (isWebPlatform)
            {
                foreach (string save in WebSavingAdapter.GetAvailableSaves())
                    yield return save;

                yield break;
            }

            foreach (string path in Directory.EnumerateFiles(Application.persistentDataPath))
            {
                if (Path.GetExtension(path) == ".json")
                    yield return Path.GetFileNameWithoutExtension(path);
            }
        }

        private Dictionary<string, object> LoadFile(string saveFile)
        {
            if (isWebPlatform)
            {
                var webData = WebSavingAdapter.LoadGameData(saveFile);

                if (webData != null && webData.Count > 0)
                    return webData;

                Debug.Log($"SavingSystem: Нет данных в веб-хранилище для '{saveFile}'.");
                return new Dictionary<string, object>();
            }

            string path = GetPathFromSaveFile(saveFile);

            if (!File.Exists(path))
                return new Dictionary<string, object>();

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);

                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Converters = new JsonConverter[]
                    {
                        new Vector3JsonConverter(),
                        new QuaternionJsonConverter(),
                        new ColorJsonConverter()
                    }
                };

                return JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    json,
                    settings
                ) ?? new Dictionary<string, object>();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"SavingSystem: Failed to load save file '{saveFile}': {e}"
                );
                return new Dictionary<string, object>();
            }
        }

        private void SaveFile(string saveFile, object state)
        {
            if (isWebPlatform)
            {
                var stateDict = state as Dictionary<string, object>;

                if (stateDict == null)
                {
                    Debug.LogError(
                        $"SavingSystem: состояние '{saveFile}' имеет неверный тип."
                    );
                    return;
                }

                int sceneIndex = 0;

                if (stateDict.ContainsKey("lastSceneBuildIndex"))
                    sceneIndex = JsonSaveHelper.ToInt(stateDict["lastSceneBuildIndex"]);

                WebSavingAdapter.SaveGameData(saveFile, stateDict, sceneIndex);
                return;
            }

            string path = GetPathFromSaveFile(saveFile);

            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Formatting = Formatting.Indented,
                    Converters = new JsonConverter[]
                    {
                        new Vector3JsonConverter(),
                        new QuaternionJsonConverter(),
                        new ColorJsonConverter()
                    }
                };

                string json = JsonConvert.SerializeObject(state, settings);
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"SavingSystem: Failed to save file '{saveFile}': {e}"
                );
            }
        }

        private void CaptureState(Dictionary<string, object> state)
        {
            foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>())
            {
                state[saveable.GetUniqueIdentifier()] = saveable.CaptureState();
            }

            state["lastSceneBuildIndex"] = SceneManager.GetActiveScene().buildIndex;
        }

        private void RestoreState(Dictionary<string, object> state)
        {
            foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>())
            {
                string id = saveable.GetUniqueIdentifier();

                if (state.ContainsKey(id))
                {
                    saveable.RestoreState(state[id]);
                }
            }

            OnRestoreStateComplete?.Invoke();
        }

        private string GetPathFromSaveFile(string saveFile)
        {
            return Path.Combine(
                Application.persistentDataPath,
                saveFile + ".json"
            );
        }
    }
}
