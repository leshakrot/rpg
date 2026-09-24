using System.Collections;
using System.Collections.Generic;
using GameDevTV.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Stats;
using RPG.UI;

namespace RPG.SceneManagement
{
    /// <summary>
    /// Фасад над SavingSystem для остальной игры (Portal, главное меню, SaveLoadUI).
    /// Не знает, куда именно пишутся сейвы (облако / локально) - это решает SavingSystem.
    /// Должен висеть на том же GameObject, что и SavingSystem.
    /// </summary>
    public class SavingWrapper : MonoBehaviour
    {
        private const string DefaultSaveName = "AutoSave";

        [SerializeField] private float _fadeInTime = 0.2f;
        [SerializeField] private float _fadeOutTime = 0.2f;
        [SerializeField] private int firstLevelBuildIndex = 1;
        [SerializeField] private int menuLevelBuildIndex = 0;

        [Header("Debug")]
        [Tooltip("Горячие клавиши: L - загрузить, F5 - сохранить, Delete - удалить текущий сейв. Работает только в редакторе и Development-билдах.")]
        [SerializeField] private bool enableDebugHotkeys = false;

        private SavingSystem savingSystem;

        private SavingSystem SaveSystem
        {
            get
            {
                if (savingSystem == null)
                    savingSystem = GetComponent<SavingSystem>();

                return savingSystem;
            }
        }

        // ------------------------------------------------------------- status

        /// <summary>Сохранения готовы к использованию (в WebGL - Yandex SDK ответил).</summary>
        public bool IsReady => SaveSystem.IsReady;

        /// <summary>Есть ли хотя бы один сейв (для активности кнопки "Продолжить").</summary>
        public bool HasAnySave
        {
            get
            {
                foreach (string _ in SaveSystem.ListSaves())
                    return true;

                return false;
            }
        }

        public IEnumerator WaitForSaveSystem()
        {
            return SaveSystem.WaitForSaveSystem();
        }

        // -------------------------------------------------------- menu actions

        /// <summary>Загрузить последний использованный сейв.</summary>
        public void ContinueGame()
        {
            StartCoroutine(ContinueGameRoutine());
        }

        /// <summary>Загрузить конкретный сейв (например, из SaveLoadUI).</summary>
        public void LoadGame(string saveFile)
        {
            StartCoroutine(LoadGameRoutine(saveFile));
        }

        /// <summary>
        /// Начать новую игру в слоте saveFile. Если слот с таким именем уже есть,
        /// он перезаписывается (иначе старые данные смешались бы с новой игрой).
        /// </summary>
        public void NewGame(string saveFile)
        {
            StartCoroutine(NewGameRoutine(saveFile));
        }

        public void LoadMenu()
        {
            StartCoroutine(LoadMenuScene());
        }

        private IEnumerator ContinueGameRoutine()
        {
            yield return SaveSystem.WaitForSaveSystem();

            string save = SaveSystem.ResolveContinueSaveName();

            if (string.IsNullOrEmpty(save))
            {
                Debug.Log("SavingWrapper: сохранений нет, продолжать нечего.");
                yield break;
            }

            SaveSystem.SetCurrentSaveName(save);
            yield return LoadLastScene();
        }

        private IEnumerator LoadGameRoutine(string saveFile)
        {
            yield return SaveSystem.WaitForSaveSystem();

            if (string.IsNullOrEmpty(saveFile) || !SaveSystem.SaveFileExists(saveFile))
            {
                Debug.LogWarning($"SavingWrapper: сейв '{saveFile}' не найден.");
                yield break;
            }

            SaveSystem.SetCurrentSaveName(saveFile);
            yield return LoadLastScene();
        }

        private IEnumerator NewGameRoutine(string saveFile)
        {
            if (string.IsNullOrWhiteSpace(saveFile))
                saveFile = DefaultSaveName;

            yield return SaveSystem.WaitForSaveSystem();

            if (SaveSystem.SaveFileExists(saveFile))
                SaveSystem.Delete(saveFile);

            SaveSystem.SetCurrentSaveName(saveFile);

            yield return LoadFirstScene();
        }

        // ---------------------------------------------------- scene transitions

        private IEnumerator LoadLastScene()
        {
            Fader fader = FindObjectOfType<Fader>();

            if (fader != null)
                yield return fader.FadeOut(_fadeOutTime);

            yield return SaveSystem.LoadLastScene(GetCurrentSave());

            RefreshPlayerStats();

            yield return new WaitForSeconds(0.1f);
            UIManager.RefreshUIFromAnywhere();

            if (fader != null)
                yield return fader.FadeIn(_fadeInTime);
        }

        private IEnumerator LoadFirstScene()
        {
            Fader fader = FindObjectOfType<Fader>();

            if (fader != null)
                yield return fader.FadeOut(_fadeOutTime);

            yield return SceneManager.LoadSceneAsync(firstLevelBuildIndex);

            if (fader != null)
                yield return fader.FadeIn(_fadeInTime);
        }

        private IEnumerator LoadMenuScene()
        {
            Fader fader = FindObjectOfType<Fader>();

            if (fader != null)
                yield return fader.FadeOut(_fadeOutTime);

            yield return SceneManager.LoadSceneAsync(menuLevelBuildIndex);

            if (fader != null)
                yield return fader.FadeIn(_fadeInTime);
        }

        // ------------------------------------------- in-game save / load (Portal)

        /// <summary>Сохранить в текущий слот.</summary>
        public void Save()
        {
            SaveSystem.Save(GetCurrentSave());
        }

        /// <summary>Сохранить в конкретный слот и сделать его текущим.</summary>
        public void Save(string saveFile)
        {
            SaveSystem.Save(saveFile);
        }

        /// <summary>Восстановить состояние объектов текущей сцены из текущего слота.</summary>
        public void Load()
        {
            SaveSystem.Load(GetCurrentSave());

            RefreshPlayerStats();
            UIManager.RefreshUIFromAnywhere();
        }

        /// <summary>Удалить текущий слот.</summary>
        public void Delete()
        {
            SaveSystem.Delete(GetCurrentSave());
        }

        /// <summary>Удалить конкретный слот (кнопка "удалить" в SaveLoadUI).</summary>
        public void Delete(string saveFile)
        {
            SaveSystem.Delete(saveFile);
        }

        public bool SaveFileExists(string saveFile)
        {
            return SaveSystem.SaveFileExists(saveFile);
        }

        public IEnumerable<string> ListSaves()
        {
            return SaveSystem.ListSaves();
        }

        // ------------------------------------------------------------ helpers

        private string GetCurrentSave()
        {
            string current = SaveSystem.GetCurrentSaveName();
            return string.IsNullOrEmpty(current) ? DefaultSaveName : current;
        }

        private static void RefreshPlayerStats()
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player == null)
                return;

            BaseStats playerStats = player.GetComponent<BaseStats>();

            if (playerStats != null)
                playerStats.RefreshStats();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (!enableDebugHotkeys)
                return;

            if (Input.GetKeyDown(KeyCode.L))
                Load();

            if (Input.GetKeyDown(KeyCode.F5))
                Save();

            if (Input.GetKeyDown(KeyCode.Delete))
                Delete();
        }
#endif
    }
}
