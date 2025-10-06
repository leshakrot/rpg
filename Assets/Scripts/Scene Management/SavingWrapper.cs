using GameDevTV.Saving;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
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
			Debug.Log("Веб-сохранения загружены через YandexSDK");
		}

		private void OnWebSaveSaved()
		{
			Debug.Log("Веб-сохранения записаны через YandexSDK");
		}

		public void ContinueGame()
		{
			if (isWebPlatform)
			{
				// В веб-версии проверяем наличие любого сохранения
				if (!string.IsNullOrEmpty(WebSavingAdapter.GetCurrentSaveFileName()))
				{
					StartCoroutine(LoadLastScene());
				}
				else
				{
					Debug.Log("SavingWrapper: Нет сохранений для продолжения игры в веб-версии");
				}
			}
			else
			{
				if(!PlayerPrefs.HasKey(currentSaveKey)) return;
				if(!GetComponent<SavingSystem>().SaveFileExists(GetCurrentSave())) return;
				StartCoroutine(LoadLastScene());
			}
		}
        
		public void NewGame(string saveFile)
		{
			if(String.IsNullOrEmpty(saveFile))
			{
				// Используем имя по умолчанию если не указано
				saveFile = "AutoSave";
			}
			SetCurrentSave(saveFile);
			StartCoroutine(LoadFirstScene());
		}
	    
		private void SetCurrentSave(string saveFile)
		{
			PlayerPrefs.SetString(currentSaveKey, saveFile);
		}
	    
		private string GetCurrentSave()
		{
			if (isWebPlatform)
			{
				// В веб-версии сначала проверяем что сохранено в YandexSDK
				string webSave = WebSavingAdapter.GetCurrentSaveFileName();
				if (!string.IsNullOrEmpty(webSave))
				{
					return webSave;
				}
				
				// Иначе используем PlayerPrefs или имя по умолчанию
				return PlayerPrefs.GetString(currentSaveKey, "AutoSave");
			}
			else
			{
				return PlayerPrefs.GetString(currentSaveKey);
			}
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
			yield return fader.FadeOut(_fadeOutTime);
			yield return GetComponent<SavingSystem>().LoadLastScene(GetCurrentSave());
		    
			// Принудительно обновляем UI после загрузки сцены и восстановления состояния
			BaseStats playerStats = GameObject.FindWithTag("Player").GetComponent<BaseStats>();
			if (playerStats != null)
			{
				playerStats.RefreshStats();
			}
			
			// Дополнительно обновляем UI через UIManager
			yield return new WaitForSeconds(0.1f); // Небольшая задержка для инициализации
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
			{
				Load();
			}

			if (Input.GetKeyDown(KeyCode.S))
			{
				Save();
			}

			if (Input.GetKeyDown(KeyCode.Delete))
			{
				Delete();
			}
		}

		public void Load()
		{
			GetComponent<SavingSystem>().Load(GetCurrentSave());
            
			// Принудительно обновляем UI после быстрой загрузки
			BaseStats playerStats = GameObject.FindWithTag("Player").GetComponent<BaseStats>();
			if (playerStats != null)
			{
				playerStats.RefreshStats();
			}
			
			// Дополнительно обновляем UI через UIManager
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
	}
}