using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Main saving facade.
    ///
    /// The original save model is deliberately preserved:
    /// Dictionary<string, object> at the root, SaveableEntity IDs as keys,
    /// and "lastSceneBuildIndex" as the scene marker.
    ///
    /// Storage selection:
    /// - desktop/editor: local JSON file;
    /// - WebGL guest: local JSON string in PlayerPrefs/browser storage;
    /// - WebGL authorized Yandex user: Yandex cloud.
    /// </summary>
    public class SavingSystem : MonoBehaviour
    {
        [Header("Web")]
        [SerializeField] private bool waitForYandexSDK = true;
        [SerializeField] private float maxWaitTime = 10f;

        [Header("Guest -> Cloud")]
        [Tooltip(
            "When an authorized Yandex user has no cloud save, an existing " +
            "guest save with the same name may be copied to the cloud.")]
        [SerializeField] private bool migrateGuestSaveToCloud = true;

        public static event Action OnRestoreStateComplete;

        private readonly LocalSaveStorage localStorage =
            new LocalSaveStorage();

        private readonly YandexSaveStorage yandexStorage =
            new YandexSaveStorage();

        private bool IsWebPlatform
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

        /// <summary>
        /// Loads the last saved scene and restores its state.
        /// </summary>
        public IEnumerator LoadLastScene(string saveFile)
        {
            if (IsWebPlatform && waitForYandexSDK)
                yield return WaitForYandexSDK();

            Dictionary<string, object> state =
                LoadFile(saveFile);

            int buildIndex =
                SceneManager.GetActiveScene().buildIndex;

            if (state.TryGetValue(
                "lastSceneBuildIndex",
                out object sceneValue))
            {
                buildIndex = JsonSaveHelper.ToInt(sceneValue);
            }

            if (buildIndex < 0 ||
                buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning(
                    $"[Saving] Save '{saveFile}' contains invalid scene index " +
                    $"{buildIndex}. Current scene will be kept.");
                RestoreState(state);
                yield break;
            }

            yield return SceneManager.LoadSceneAsync(buildIndex);
            yield return null;

            RestoreState(state);
        }

        /// <summary>
        /// Saves the current scene while preserving all previously stored
        /// state that belongs to other scenes/entities.
        /// </summary>
        public void Save(string saveFile)
        {
            Dictionary<string, object> state =
                LoadFile(saveFile);

            CaptureState(state);
            SaveFile(saveFile, state);
        }

        public void Delete(string saveFile)
        {
            if (UseYandexCloud())
            {
                yandexStorage.Delete(saveFile);
            }
            else
            {
                localStorage.Delete(saveFile);
            }
        }

        public void Load(string saveFile)
        {
            RestoreState(LoadFile(saveFile));
        }

        public bool SaveFileExists(string saveFile)
        {
            if (UseYandexCloud())
                return yandexStorage.Exists(saveFile);

            return localStorage.Exists(saveFile);
        }

        public IEnumerable<string> ListSaves()
        {
            if (UseYandexCloud())
            {
                foreach (string save in yandexStorage.ListSaves())
                    yield return save;

                yield break;
            }

            foreach (string save in localStorage.ListSaves())
                yield return save;
        }

        private Dictionary<string, object> LoadFile(string saveFile)
        {
            if (UseYandexCloud())
            {
                if (yandexStorage.TryLoad(saveFile, out Dictionary<string, object> cloudState))
                {
                    Debug.Log(
                        $"SavingSystem: Loaded '{saveFile}' from Yandex cloud.");
                    return cloudState;
                }

                // Important: only fall back to guest data when the cloud
                // save does not exist. We never use a stale local guest save
                // to override an existing cloud save.
                if (migrateGuestSaveToCloud &&
                    localStorage.TryLoad(
                        saveFile,
                        out Dictionary<string, object> localState))
                {
                    Debug.Log(
                        $"SavingSystem: No cloud save '{saveFile}'. " +
                        "Using local guest save and migrating it to cloud.");

                    yandexStorage.TrySave(saveFile, localState);
                    return localState;
                }

                Debug.Log(
                    $"SavingSystem: No cloud save '{saveFile}'.");
                return new Dictionary<string, object>();
            }

            if (localStorage.TryLoad(
                saveFile,
                out Dictionary<string, object> state))
            {
                Debug.Log(
                    $"SavingSystem: Loaded local save '{saveFile}'.");
                return state;
            }

            Debug.Log(
                $"SavingSystem: No local save '{saveFile}'.");
            return new Dictionary<string, object>();
        }

        private void SaveFile(
            string saveFile,
            Dictionary<string, object> state)
        {
            if (UseYandexCloud())
            {
                if (!yandexStorage.TrySave(saveFile, state))
                {
                    Debug.LogWarning(
                        $"SavingSystem: Yandex cloud save failed for '{saveFile}'. " +
                        "The local guest save is kept as a backup.");

                    // Do not lose the user's current state if cloud saving
                    // fails transiently.
                    localStorage.TrySave(saveFile, state);
                }
                else
                {
                    // Keep the local copy only as a guest backup. It is not
                    // selected while the user is authorized.
                    localStorage.TrySave(saveFile, state);
                }

                return;
            }

            if (!localStorage.TrySave(saveFile, state))
            {
                Debug.LogError(
                    $"SavingSystem: Failed to save local file '{saveFile}'.");
            }
        }

        private void CaptureState(
            Dictionary<string, object> state)
        {
            foreach (SaveableEntity saveable
                     in FindObjectsOfType<SaveableEntity>())
            {
                string id = saveable.GetUniqueIdentifier();

                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning(
                        $"SavingSystem: SaveableEntity '{saveable.name}' has no ID.",
                        saveable);
                    continue;
                }

                state[id] = saveable.CaptureState();
            }

            state["lastSceneBuildIndex"] =
                SceneManager.GetActiveScene().buildIndex;
        }

        private void RestoreState(
            Dictionary<string, object> state)
        {
            if (state == null)
                state = new Dictionary<string, object>();

            foreach (SaveableEntity saveable
                     in FindObjectsOfType<SaveableEntity>())
            {
                string id = saveable.GetUniqueIdentifier();

                if (string.IsNullOrEmpty(id))
                    continue;

                if (state.TryGetValue(id, out object entityState))
                {
                    try
                    {
                        saveable.RestoreState(entityState);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(
                            $"SavingSystem: Failed to restore entity '{id}' " +
                            $"({saveable.name}): {e}");
                    }
                }
            }

            OnRestoreStateComplete?.Invoke();
        }

        private bool UseYandexCloud()
        {
            if (!IsWebPlatform)
                return false;

            return yandexStorage.IsReadyAndAuthorized;
        }

        private IEnumerator WaitForYandexSDK()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!waitForYandexSDK)
                yield break;

            float timer = 0f;

            while (!YG.YandexGame.SDKEnabled &&
                   timer < maxWaitTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!YG.YandexGame.SDKEnabled)
            {
                Debug.LogWarning(
                    "SavingSystem: Yandex SDK did not become ready in time. " +
                    "Guest/local storage will be used.");
            }
#else
            yield return null;
#endif
        }
    }
}
