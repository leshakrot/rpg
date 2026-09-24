using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Ядро системы сохранений. Само выбирает, куда писать:
    ///
    ///   WebGL + игрок авторизован на Яндексе  -> облако  (YandexSaveStorage)
    ///   WebGL + гость                          -> локально (PlayerPrefs / IndexedDB)
    ///   Редактор / десктоп                     -> локально (файлы .json)
    ///
    /// Публичный API совместим с прежней версией (Save/Load/Delete/LoadLastScene/
    /// SaveFileExists/ListSaves/WaitForSaveSystem/OnRestoreStateComplete).
    /// </summary>
    public class SavingSystem : MonoBehaviour
    {
        private const string LastSceneKey = "lastSceneBuildIndex";
        private const string DefaultSaveName = "AutoSave";

        [Header("Yandex Games")]
        [Tooltip("Сколько секунд ждать инициализации Yandex SDK, прежде чем считать игрока гостем.")]
        [SerializeField] private float maxWaitTime = 15f;

        [Tooltip("Использовать облачное хранилище в редакторе (для теста; нужна эмуляция авторизации в PluginYG).")]
        [SerializeField] private bool useYandexInEditor = false;

        public static event Action OnRestoreStateComplete;

        // static - переживает пересоздание SavingSystem при смене сцен.
        private static bool sdkWaitTimedOut;

        private LocalSaveStorage localStorage;
        private YandexSaveStorage yandexStorage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sdkWaitTimedOut = false;
            OnRestoreStateComplete = null;
        }

        private LocalSaveStorage Local =>
            localStorage ?? (localStorage = new LocalSaveStorage());

        private YandexSaveStorage Yandex =>
            yandexStorage ?? (yandexStorage = new YandexSaveStorage(useYandexInEditor));

        /// <summary>
        /// Активное хранилище. null = ещё рано (ждём Yandex SDK) - в этом состоянии
        /// ничего не читаем и не пишем, чтобы не затереть облачный прогресс.
        /// </summary>
        private ISaveStorage ActiveStorage
        {
            get
            {
                if (!Yandex.IsSupported)
                    return Local;

                if (Yandex.IsSdkReady)
                    return Yandex.IsAuthorized ? (ISaveStorage)Yandex : Local;

                return sdkWaitTimedOut ? Local : null;
            }
        }

        // -------------------------------------------------------------- state

        /// <summary>Можно ли уже работать с сохранениями.</summary>
        public bool IsReady => ActiveStorage != null;

        /// <summary>Сейчас сохранения идут в облако Яндекса.</summary>
        public bool IsUsingCloud => ActiveStorage is YandexSaveStorage;

        public string ActiveStorageName
        {
            get
            {
                ISaveStorage storage = ActiveStorage;
                return storage != null ? storage.Name : "None";
            }
        }

        /// <summary>
        /// Ждёт готовности хранилища. В WebGL - пока Yandex SDK не отдаст данные игрока.
        /// Если SDK не ответил за maxWaitTime, игрок считается гостем (локальные сейвы).
        /// </summary>
        public IEnumerator WaitForSaveSystem()
        {
            if (!Yandex.IsSupported)
                yield break;

            float timer = 0f;

            while (!Yandex.IsSdkReady && timer < maxWaitTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!Yandex.IsSdkReady && !sdkWaitTimedOut)
            {
                sdkWaitTimedOut = true;
                Debug.LogWarning(
                    "SavingSystem: Yandex SDK не ответил вовремя, переключаемся на локальные сохранения.");
            }
        }

        // ------------------------------------------------------ current save

        public string GetCurrentSaveName()
        {
            ISaveStorage storage = ActiveStorage;
            return storage != null ? storage.GetCurrentSaveName() : string.Empty;
        }

        public void SetCurrentSaveName(string saveFile)
        {
            ISaveStorage storage = ActiveStorage;

            if (storage != null)
                storage.SetCurrentSaveName(saveFile);
        }

        /// <summary>
        /// Какой сейв грузить по кнопке "Продолжить": текущий, если он существует,
        /// иначе первый из имеющихся. null - сохранений нет.
        /// </summary>
        public string ResolveContinueSaveName()
        {
            ISaveStorage storage = ActiveStorage;

            if (storage == null)
                return null;

            string current = storage.GetCurrentSaveName();

            if (!string.IsNullOrEmpty(current) && storage.Exists(current))
                return current;

            List<string> saves = storage.ListSaves();
            return saves.Count > 0 ? saves[0] : null;
        }

        // ------------------------------------------------------------- public

        public bool SaveFileExists(string saveFile)
        {
            ISaveStorage storage = ActiveStorage;
            return storage != null && storage.Exists(saveFile);
        }

        public IEnumerable<string> ListSaves()
        {
            ISaveStorage storage = ActiveStorage;
            return storage != null ? storage.ListSaves() : new List<string>();
        }

        public void Delete(string saveFile)
        {
            ISaveStorage storage = ActiveStorage;

            if (storage == null)
            {
                Debug.LogWarning($"SavingSystem: Delete '{saveFile}' пропущен - хранилище ещё не готово.");
                return;
            }

            storage.Delete(saveFile);
        }

        public void Save(string saveFile)
        {
            ISaveStorage storage = ActiveStorage;

            if (storage == null)
            {
                Debug.LogWarning($"SavingSystem: Save '{saveFile}' пропущен - хранилище ещё не готово.");
                return;
            }

            if (string.IsNullOrEmpty(saveFile))
                saveFile = DefaultSaveName;

            // Сначала читаем существующее состояние: в нём лежат объекты ДРУГИХ сцен,
            // которые мы не должны потерять (классическая схема GameDevTV).
            if (!storage.TryLoad(saveFile, out Dictionary<string, object> state) || state == null)
                state = new Dictionary<string, object>();

            CaptureState(state);

            if (storage.TrySave(saveFile, state))
                storage.SetCurrentSaveName(saveFile);
        }

        public void Load(string saveFile)
        {
            ISaveStorage storage = ActiveStorage;

            if (storage == null)
            {
                Debug.LogWarning($"SavingSystem: Load '{saveFile}' пропущен - хранилище ещё не готово.");
                return;
            }

            if (!storage.TryLoad(saveFile, out Dictionary<string, object> state) || state == null)
                state = new Dictionary<string, object>();

            RestoreState(state);
        }

        public IEnumerator LoadLastScene(string saveFile)
        {
            yield return WaitForSaveSystem();

            ISaveStorage storage = ActiveStorage;
            Dictionary<string, object> state;

            if (storage == null || !storage.TryLoad(saveFile, out state) || state == null)
            {
                Debug.LogWarning($"SavingSystem: сейв '{saveFile}' не найден или не читается - загрузка отменена.");
                yield break;
            }

            int buildIndex = SceneManager.GetActiveScene().buildIndex;

            if (state.TryGetValue(LastSceneKey, out object sceneValue))
                buildIndex = SaveJson.ToInt(sceneValue, buildIndex);

            yield return SceneManager.LoadSceneAsync(buildIndex);

            RestoreState(state);
        }

        // ------------------------------------------------------------ capture

        private void CaptureState(Dictionary<string, object> state)
        {
            foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>())
            {
                string id = saveable.GetUniqueIdentifier();

                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning(
                        $"SavingSystem: у '{saveable.name}' пустой UniqueIdentifier - объект не сохранён.",
                        saveable);
                    continue;
                }

                state[id] = saveable.CaptureState();
            }

            state[LastSceneKey] = SceneManager.GetActiveScene().buildIndex;
        }

        private void RestoreState(Dictionary<string, object> state)
        {
            foreach (SaveableEntity saveable in FindObjectsOfType<SaveableEntity>())
            {
                string id = saveable.GetUniqueIdentifier();

                if (!string.IsNullOrEmpty(id) && state.TryGetValue(id, out object entityState))
                    saveable.RestoreState(entityState);
            }

            OnRestoreStateComplete?.Invoke();
        }
    }
}
