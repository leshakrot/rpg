using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Local storage for guests.
    ///
    /// Desktop: one JSON file per save in Application.persistentDataPath.
    /// WebGL: JSON is stored in PlayerPrefs, which Unity maps to browser
    /// persistent storage. We do NOT use System.IO on WebGL.
    /// </summary>
    public sealed class LocalSaveStorage
    {
        private const string WebPrefix = "GameDevTV.Saving.Local.";
        private const string WebIndexKey = WebPrefix + "__index";

        public bool Exists(string saveFile)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return PlayerPrefs.HasKey(GetWebKey(saveFile));
#else
            return File.Exists(GetFilePath(saveFile));
#endif
        }

        public bool TryLoad(
            string saveFile,
            out Dictionary<string, object> state)
        {
            state = new Dictionary<string, object>();

#if UNITY_WEBGL && !UNITY_EDITOR
            string key = GetWebKey(saveFile);

            if (!PlayerPrefs.HasKey(key))
                return false;

            string json = PlayerPrefs.GetString(key, string.Empty);

            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                state = SaveJson.Deserialize(json);
                return state.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[Saving] Failed to deserialize local WebGL save '{saveFile}': {e}");
                state = new Dictionary<string, object>();
                return false;
            }
#else
            string path = GetFilePath(saveFile);

            if (!File.Exists(path))
                return false;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                state = SaveJson.Deserialize(json);
                return state.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[Saving] Failed to read local save '{saveFile}': {e}");
                state = new Dictionary<string, object>();
                return false;
            }
#endif
        }

        public bool TrySave(
            string saveFile,
            Dictionary<string, object> state)
        {
            if (state == null)
                return false;

            try
            {
                string json = SaveJson.Serialize(state, true);

#if UNITY_WEBGL && !UNITY_EDITOR
                PlayerPrefs.SetString(GetWebKey(saveFile), json);
                AddToWebIndex(saveFile);
                PlayerPrefs.Save();
                return true;
#else
                string path = GetFilePath(saveFile);
                string tempPath = path + ".tmp";

                File.WriteAllText(tempPath, json, Encoding.UTF8);

                if (File.Exists(path))
                    File.Delete(path);

                File.Move(tempPath, path);
                return true;
#endif
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[Saving] Failed to save local '{saveFile}': {e}");
                return false;
            }
        }

        public bool Delete(string saveFile)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                string key = GetWebKey(saveFile);

                if (!PlayerPrefs.HasKey(key))
                    return false;

                PlayerPrefs.DeleteKey(key);
                RemoveFromWebIndex(saveFile);
                PlayerPrefs.Save();
                return true;
#else
                string path = GetFilePath(saveFile);

                if (!File.Exists(path))
                    return false;

                File.Delete(path);
                return true;
#endif
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[Saving] Failed to delete local save '{saveFile}': {e}");
                return false;
            }
        }

        public IEnumerable<string> ListSaves()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            foreach (string save in ReadWebIndex())
                yield return save;
#else
            if (!Directory.Exists(Application.persistentDataPath))
                yield break;

            foreach (string path in Directory.EnumerateFiles(
                Application.persistentDataPath,
                "*.json",
                SearchOption.TopDirectoryOnly))
            {
                yield return Path.GetFileNameWithoutExtension(path);
            }
#endif
        }

        private static string GetFilePath(string saveFile)
        {
            ValidateSaveName(saveFile);

            return Path.Combine(
                Application.persistentDataPath,
                saveFile + ".json");
        }

        private static string GetWebKey(string saveFile)
        {
            ValidateSaveName(saveFile);
            return WebPrefix + saveFile;
        }

        private static void ValidateSaveName(string saveFile)
        {
            if (string.IsNullOrWhiteSpace(saveFile))
                throw new ArgumentException(
                    "Save file name cannot be empty.",
                    nameof(saveFile));

#if !UNITY_WEBGL || UNITY_EDITOR
            // Preserve the original filename semantics but prevent traversal.
            if (saveFile.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0 ||
                saveFile.Contains("/") ||
                saveFile.Contains("\\") ||
                saveFile == "." ||
                saveFile == "..")
            {
                throw new ArgumentException(
                    $"Invalid save file name: '{saveFile}'",
                    nameof(saveFile));
            }
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static List<string> ReadWebIndex()
        {
            var result = new List<string>();

            string raw = PlayerPrefs.GetString(WebIndexKey, string.Empty);

            if (string.IsNullOrEmpty(raw))
                return result;

            string[] entries = raw.Split('\n');

            foreach (string entry in entries)
            {
                if (!string.IsNullOrEmpty(entry) &&
                    !result.Contains(entry) &&
                    PlayerPrefs.HasKey(GetWebKey(entry)))
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        private static void AddToWebIndex(string saveFile)
        {
            List<string> saves = ReadWebIndex();

            if (!saves.Contains(saveFile))
                saves.Add(saveFile);

            PlayerPrefs.SetString(
                WebIndexKey,
                string.Join("\n", saves.ToArray()));
        }

        private static void RemoveFromWebIndex(string saveFile)
        {
            List<string> saves = ReadWebIndex();
            saves.Remove(saveFile);

            PlayerPrefs.SetString(
                WebIndexKey,
                string.Join("\n", saves.ToArray()));
        }
#endif
    }
}
