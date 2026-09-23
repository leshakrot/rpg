using GameDevTV.Saving;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Stats;
using RPG.UI;

namespace RPG.SceneManagement
{
    public class SavingWrapper : MonoBehaviour
    {
        private const string currentSaveKey = "currentSaveName";

        [SerializeField] private float _fadeInTime = 0.2f;
        [SerializeField] private float _fadeOutTime = 0.2f;
        [SerializeField] private int firstLevelBuildIndex = 1;
        [SerializeField] private int menuLevelBuildIndex = 0;

        [Header("Веб-интеграция")]
        [SerializeField] private bool useWebAdapter = true;

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

        private void OnEnable()
        {
            if (isWebPlatform && useWebAdapter)
            {
                WebSavingAdapter.OnSaveLoaded += OnWebSaveLoaded;
                WebSavingAdapter.OnSaveSaved += OnWebSaveSaved;
            }
        }

        private void OnDisable()
        {
            if (isWebPlatform && useWebAdapter)
            {
                WebSavingAdapter.OnSaveLoaded -= OnWebSaveLoaded;
                WebSavingAdapter.OnSaveSaved -= OnWebSaveSaved;
            }
        }

        private void OnWebSaveLoaded()
        {
            Debug.Log("SavingWrapper: веб-сохранения загружены.");
        }

        private void OnWebSaveSaved()
        {
            Debug.Log("SavingWrapper: веб-сохранения записаны.");
        }

        public void ContinueGame()
        {
            if (isWebPlatform && useWebAdapter)
            {
                StartCoroutine(ContinueWebGame());
                return;
            }

            if (!PlayerPrefs.HasKey(currentSaveKey))
                return;

            if (!GetComponent<SavingSystem>().SaveFileExists(GetCurrentSave()))
                return;

            StartCoroutine(LoadLastScene());
        }

        private IEnumerator ContinueWebGame()
        {
            yield return GetComponent<SavingSystem>().WaitForSaveSystem();

            string save = WebSavingAdapter.GetCurrentSaveFileName();

            if (string.IsNullOrEmpty(save))
            {
                Debug.Log("SavingWrapper: В Yandex нет текущего сохранения.");
                yield break;
            }

            yield return LoadLastScene();
        }

        public void NewGame(string saveFile)
        {
            if (string.IsNullOrEmpty(saveFile))
                saveFile = "AutoSave";

            SetCurrentSave(saveFile);
            StartCoroutine(LoadFirstScene());
        }

        private void SetCurrentSave(string saveFile)
        {
            PlayerPrefs.SetString(currentSaveKey, saveFile);
            PlayerPrefs.Save();
        }

        private string GetCurrentSave()
        {
            if (isWebPlatform && useWebAdapter)
            {
                string webSave = WebSavingAdapter.GetCurrentSaveFileName();

                if (!string.IsNullOrEmpty(webSave))
                    return webSave;

                return PlayerPrefs.GetString(currentSaveKey, "AutoSave");
            }

            return PlayerPrefs.GetString(currentSaveKey);
        }

        public void LoadGame(string saveFile)
        {
            SetCurrentSave(saveFile);
            ContinueGame();
        }

        public void LoadMenu()
        {
            StartCoroutine(LoadMenuScene());
        }

        private IEnumerator LoadLastScene()
        {
            Fader fader = FindObjectOfType<Fader>();

            if (fader == null)
            {
                Debug.LogError("SavingWrapper: Fader не найден.");
                yield break;
            }

            yield return fader.FadeOut(_fadeOutTime);

            yield return GetComponent<SavingSystem>().LoadLastScene(GetCurrentSave());

            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                BaseStats playerStats = player.GetComponent<BaseStats>();

                if (playerStats != null)
                    playerStats.RefreshStats();
            }

            yield return new WaitForSeconds(0.1f);
            UIManager.RefreshUIFromAnywhere();

            yield return fader.FadeIn(_fadeInTime);
        }

        private IEnumerator LoadFirstScene()
        {
            Fader fader = FindObjectOfType<Fader>();

            yield return fader.FadeOut(_fadeOutTime);
            yield return SceneManager.LoadSceneAsync(firstLevelBuildIndex);
            yield return fader.FadeIn(_fadeInTime);
        }

        private IEnumerator LoadMenuScene()
        {
            Fader fader = FindObjectOfType<Fader>();

            yield return fader.FadeOut(_fadeOutTime);
            yield return SceneManager.LoadSceneAsync(menuLevelBuildIndex);
            yield return fader.FadeIn(_fadeInTime);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
                Load();

            if (Input.GetKeyDown(KeyCode.S))
                Save();

            if (Input.GetKeyDown(KeyCode.Delete))
                Delete();
        }

        public void Load()
        {
            GetComponent<SavingSystem>().Load(GetCurrentSave());

            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                BaseStats playerStats = player.GetComponent<BaseStats>();

                if (playerStats != null)
                    playerStats.RefreshStats();
            }

            UIManager.RefreshUIFromAnywhere();
        }

        public void Save()
        {
            GetComponent<SavingSystem>().Save(GetCurrentSave());
        }

        public void Delete()
        {
            GetComponent<SavingSystem>().Delete(GetCurrentSave());
        }

        public IEnumerable<string> ListSaves()
        {
            return GetComponent<SavingSystem>().ListSaves();
        }

        public IEnumerator WaitForSaveSystem()
        {
            if (isWebPlatform && useWebAdapter)
                yield return GetComponent<SavingSystem>().WaitForSaveSystem();
        }
    }
}
