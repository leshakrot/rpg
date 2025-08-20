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
    /// <summary>
    /// This component provides the interface to the saving system. It provides
    /// methods to save and restore a scene.
    ///
    /// This component should be created once and shared between all subsequent scenes.
    /// </summary>
    public class SavingSystem : MonoBehaviour
    {
        /// <summary>
        /// Will load the last scene that was saved and restore the state. This
        /// must be run as a coroutine.
        /// </summary>
        /// <param name="saveFile">The save file to consult for loading.</param>
        public IEnumerator LoadLastScene(string saveFile)
        {
            Dictionary<string, object> state = LoadFile(saveFile);
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            if (state.ContainsKey("lastSceneBuildIndex"))
            {
                buildIndex = JsonSaveHelper.ToInt(state["lastSceneBuildIndex"]);
            }
            yield return SceneManager.LoadSceneAsync(buildIndex);
            RestoreState(state);
        }

        /// <summary>
        /// Save the current scene to the provided save file.
        /// </summary>
        public void Save(string saveFile)
        {
            Dictionary<string, object> state = LoadFile(saveFile);
            CaptureState(state);
            SaveFile(saveFile, state);
        }

        /// <summary>
        /// Delete the state in the given save file.
        /// </summary>
        public void Delete(string saveFile)
        {
            File.Delete(GetPathFromSaveFile(saveFile));
        }

        public void Load(string saveFile)
        {
            RestoreState(LoadFile(saveFile));
        }
        
	    public bool SaveFileExists(string saveFile)
	    {
	    	string path = GetPathFromSaveFile(saveFile);
		    return File.Exists(path);
	    }
	    
	    public IEnumerable<string> ListSaves()
	    {
	    	foreach (string path in Directory.EnumerateFiles(Application.persistentDataPath))
	    	{
	    		if(Path.GetExtension(path) == ".json")
	    		{
	    			yield return Path.GetFileNameWithoutExtension(path);
	    		}
	    	}
	    }

        // PRIVATE

        private Dictionary<string, object> LoadFile(string saveFile)
        {
            string path = GetPathFromSaveFile(saveFile);
            if (!File.Exists(path))
            {
                return new Dictionary<string, object>();
            }
            
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
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json, settings) ?? new Dictionary<string, object>();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load save file {saveFile}: {e.Message}");
                return new Dictionary<string, object>();
            }
        }

        private void SaveFile(string saveFile, object state)
        {
            string path = GetPathFromSaveFile(saveFile);
            print("Saving to " + path);
            
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
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save file {saveFile}: {e.Message}");
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
        }

        private string GetPathFromSaveFile(string saveFile)
        {
            return Path.Combine(Application.persistentDataPath, saveFile + ".json");
        }
    }
}