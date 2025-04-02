using GameDevTV.Saving;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace RPG.SceneManagement
{

    public class SavingWrapper : MonoBehaviour
    {
	    private const string currentSaveKey = "currentSaveName";
        
	    [SerializeField] private float _fadeInTime = 0.2f;
	    [SerializeField] private float _fadeOutTime = 0.2f;
	    [SerializeField] private int firstLevelBuildIndex = 1;
	    [SerializeField] private int menuLevelBuildIndex = 0;

	    public void ContinueGame()
	    {
		    if(!PlayerPrefs.HasKey(currentSaveKey)) return;
		    if(!GetComponent<SavingSystem>().SaveFileExists(GetCurrentSave())) return;
            StartCoroutine(LoadLastScene());
        }
        
	    public void NewGame(string saveFile)
	    {
	    	if(String.IsNullOrEmpty(saveFile)) return;
	    	SetCurrentSave(saveFile);
	    	StartCoroutine(LoadFirstScene());
	    }
	    
	    private void SetCurrentSave(string saveFile)
	    {
		    PlayerPrefs.SetString(currentSaveKey, saveFile);
	    }
	    
	    private string GetCurrentSave()
	    {
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
		    yield return fader.FadeOut(_fadeOutTime);
		    yield return GetComponent<SavingSystem>().LoadLastScene(GetCurrentSave());        
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
