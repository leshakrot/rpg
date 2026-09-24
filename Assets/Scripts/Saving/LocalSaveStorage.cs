using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Локальное хранилище (для неавторизованных игроков, редактора и десктопа).
    ///
    /// Десктоп / редактор: один JSON-файл на сейв в Application.persistentDataPath
    ///                     (так же, как в оригинальной системе: "&lt;имя&gt;.json").
    /// WebGL:              JSON лежит в PlayerPrefs (Unity сам кладёт их в IndexedDB
    ///                     браузера). System.IO в WebGL не используется.
    /// </summary>
    public sealed class LocalSaveStorage : ISaveStorage
    {
        // Тот же ключ, что использовал старый SavingWrapper - ничего не теряется.
        private const string CurrentSaveKey = "currentSaveName";

        private const string WebPrefix = "GameDevTV.Saving.Local.";
        private const string WebIndexKey = WebPrefix + "__index";
        private const string ReservedName = "__index";

        private static bool UseBrowserStorage =>
            Application.platform == RuntimePlatform.WebGLPlayer;

        public string Name => "Local";

        // ---------------------------------------------------------------- API

        public bool Exists(string saveFile)
        {
            if (!IsValidName(saveFile))
                return false;

            return UseBrowserStorage
                ? PlayerPrefs.HasKey(GetWebKey(saveFile))
                : File.Exists(GetFilePath(saveFile));
        }

        public bool TryLoad(string saveFile, out Dictionary<string, object> state)
        {
            state = new Dictionary<string, object>();

            if (!IsValidName(saveFile))
                return false;

            try
            {
                string json;

                if (UseBrowserStorage)
                {
                    string key = GetWebKey(saveFile);

                    if (!PlayerPrefs.HasKey(key))
                        return false;

                    json = PlayerPrefs.GetString(key, string.Empty);
                }
                else
                {
                    string path = GetFilePath(saveFile);

                    if (!File.Exists(path))
                        return false;

                    json = File.ReadAllText(path, Encoding.UTF8);
                }

                if (string.IsNullOrEmpty(json))
                    return false;

                state = SaveJson.Deserialize(json);
                return state.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Saving] Не удалось прочитать локальный сейв '{saveFile}': {e}");
                state = new Dictionary<string, object>();
                return false;
            }
        }

        public bool TrySave(string saveFile, Dictionary<string, object> state)
        {
            if (state == null || !IsValidName(saveFile))
                return false;

            try
            {
                if (UseBrowserStorage)
                {
                    PlayerPrefs.SetString(GetWebKey(saveFile), SaveJson.Serialize(state, false));
                    AddToWebIndex(saveFile);
                    PlayerPrefs.Save();
                    return true;
                }

                string json = SaveJson.Serialize(state, true);
                string path = GetFilePath(saveFile);
                string tempPath = path + ".tmp";

                // Пишем во временный файл, потом копируем поверх:
                // если игра упадёт посреди записи, старый сейв останется целым.
                File.WriteAllText(tempPath, json, Encoding.UTF8);
                File.Copy(tempPath, path, true);
                File.Delete(tempPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Saving] Не удалось записать локальный сейв '{saveFile}': {e}");
                return false;
            }
        }

        public bool Delete(string saveFile)
        {
            if (!IsValidName(saveFile))
                return false;

            try
            {
                bool deleted;

                if (UseBrowserStorage)
                {
                    string key = GetWebKey(saveFile);
                    deleted = PlayerPrefs.HasKey(key);

                    if (deleted)
                    {
                        PlayerPrefs.DeleteKey(key);
                        RemoveFromWebIndex(saveFile);
                    }
                }
                else
                {
                    string path = GetFilePath(saveFile);
                    deleted = File.Exists(path);

                    if (deleted)
                        File.Delete(path);
                }

                if (deleted)
                {
                    if (GetCurrentSaveName() == saveFile)
                        PlayerPrefs.DeleteKey(CurrentSaveKey);

                    PlayerPrefs.Save();
                }

                return deleted;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Saving] Не удалось удалить локальный сейв '{saveFile}': {e}");
                return false;
            }
        }

        public List<string> ListSaves()
        {
            if (UseBrowserStorage)
                return ReadWebIndex();

            var result = new List<string>();
            string directory = Application.persistentDataPath;

            if (!Directory.Exists(directory))
                return result;

            foreach (string path in Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                if (Path.GetExtension(path) == ".json")
                    result.Add(Path.GetFileNameWithoutExtension(path));
            }

            return result;
        }

        public string GetCurrentSaveName()
        {
            return PlayerPrefs.GetString(CurrentSaveKey, string.Empty);
        }

        public void SetCurrentSaveName(string saveFile)
        {
            if (string.IsNullOrEmpty(saveFile))
                PlayerPrefs.DeleteKey(CurrentSaveKey);
            else
                PlayerPrefs.SetString(CurrentSaveKey, saveFile);

            PlayerPrefs.Save();
        }

        // ------------------------------------------------------------ helpers

        private static bool IsValidName(string saveFile)
        {
            if (string.IsNullOrWhiteSpace(saveFile))
                return false;

            if (saveFile == ReservedName || saveFile.Contains("\n"))
                return false;

            if (UseBrowserStorage)
                return true;

            // Десктоп: имя превращается в имя файла - защищаемся от путей.
            return saveFile.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                   && !saveFile.Contains("/")
                   && !saveFile.Contains("\\")
                   && saveFile != "."
                   && saveFile != "..";
        }

        private static string GetFilePath(string saveFile)
        {
            return Path.Combine(Application.persistentDataPath, saveFile + ".json");
        }

        private static string GetWebKey(string saveFile)
        {
            return WebPrefix + saveFile;
        }

        private static List<string> ReadWebIndex()
        {
            var result = new List<string>();
            string raw = PlayerPrefs.GetString(WebIndexKey, string.Empty);

            if (string.IsNullOrEmpty(raw))
                return result;

            foreach (string entry in raw.Split('\n'))
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

        private static void WriteWebIndex(List<string> saves)
        {
            PlayerPrefs.SetString(WebIndexKey, string.Join("\n", saves.ToArray()));
        }

        private static void AddToWebIndex(string saveFile)
        {
            List<string> saves = ReadWebIndex();

            if (!saves.Contains(saveFile))
                saves.Add(saveFile);

            WriteWebIndex(saves);
        }

        private static void RemoveFromWebIndex(string saveFile)
        {
            List<string> saves = ReadWebIndex();
            saves.Remove(saveFile);
            WriteWebIndex(saves);
        }
    }
}
