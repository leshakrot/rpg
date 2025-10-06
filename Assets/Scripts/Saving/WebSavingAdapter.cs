using System;
using System.Collections.Generic;
using UnityEngine;
using YG;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Адаптер для интеграции системы сохранений с YandexSDK
    /// Обеспечивает корректную работу сохранений в веб-билдах
    /// </summary>
    public class WebSavingAdapter : MonoBehaviour
    {
        [System.Serializable]
        public class GameSaveData
        {
            public Dictionary<string, object> saveData = new Dictionary<string, object>();
            public int lastSceneBuildIndex = 0;
        }

        private const string GAME_SAVE_SLOT_1 = "slot1";
        private const string GAME_SAVE_SLOT_2 = "slot2";
        private const string GAME_SAVE_SLOT_3 = "slot3";

        // События для отслеживания изменений сохранений
        public static Action OnSaveLoaded;
        public static Action OnSaveSaved;

        private void OnEnable()
        {
            YandexGame.GetDataEvent += OnYandexDataLoaded;
        }

        private void OnDisable()
        {
            YandexGame.GetDataEvent -= OnYandexDataLoaded;
        }

        private void OnYandexDataLoaded()
        {
            OnSaveLoaded?.Invoke();
        }

        /// <summary>
        /// Сохраняет данные игры в YandexSDK
        /// </summary>
        public static void SaveGameData(string saveSlot, Dictionary<string, object> gameData, int sceneIndex)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                var saveData = new GameSaveData
                {
                    saveData = gameData,
                    lastSceneBuildIndex = sceneIndex
                };

                string jsonData = JsonUtility.ToJson(saveData);

                // Сохраняем в соответствующий слот YandexSDK
                switch (saveSlot)
                {
                    case GAME_SAVE_SLOT_1:
                        YandexGame.savesData.gameSaveSlot1 = jsonData;
                        break;
                    case GAME_SAVE_SLOT_2:
                        YandexGame.savesData.gameSaveSlot2 = jsonData;
                        break;
                    case GAME_SAVE_SLOT_3:
                        YandexGame.savesData.gameSaveSlot3 = jsonData;
                        break;
                    default:
                        YandexGame.savesData.gameSaveSlot1 = jsonData;
                        break;
                }

                YandexGame.SaveProgress();
                OnSaveSaved?.Invoke();
                
                Debug.Log($"Игра сохранена в слот {saveSlot} через YandexSDK");
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка сохранения через YandexSDK: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Загружает данные игры из YandexSDK
        /// </summary>
        public static GameSaveData LoadGameData(string saveSlot)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string jsonData = "";

                // Загружаем из соответствующего слота YandexSDK
                switch (saveSlot)
                {
                    case GAME_SAVE_SLOT_1:
                        jsonData = YandexGame.savesData.gameSaveSlot1;
                        break;
                    case GAME_SAVE_SLOT_2:
                        jsonData = YandexGame.savesData.gameSaveSlot2;
                        break;
                    case GAME_SAVE_SLOT_3:
                        jsonData = YandexGame.savesData.gameSaveSlot3;
                        break;
                    default:
                        jsonData = YandexGame.savesData.gameSaveSlot1;
                        break;
                }

                if (string.IsNullOrEmpty(jsonData))
                {
                    Debug.Log($"Слот {saveSlot} пуст");
                    return null;
                }

                var saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                Debug.Log($"Игра загружена из слота {saveSlot} через YandexSDK");
                return saveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка загрузки через YandexSDK: {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Проверяет существование сохранения в слоте
        /// </summary>
        public static bool SaveExists(string saveSlot)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string jsonData = "";

            switch (saveSlot)
            {
                case GAME_SAVE_SLOT_1:
                    jsonData = YandexGame.savesData.gameSaveSlot1;
                    break;
                case GAME_SAVE_SLOT_2:
                    jsonData = YandexGame.savesData.gameSaveSlot2;
                    break;
                case GAME_SAVE_SLOT_3:
                    jsonData = YandexGame.savesData.gameSaveSlot3;
                    break;
                default:
                    jsonData = YandexGame.savesData.gameSaveSlot1;
                    break;
            }

            return !string.IsNullOrEmpty(jsonData);
#else
            return false;
#endif
        }

        /// <summary>
        /// Удаляет сохранение из слота
        /// </summary>
        public static void DeleteSave(string saveSlot)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                switch (saveSlot)
                {
                    case GAME_SAVE_SLOT_1:
                        YandexGame.savesData.gameSaveSlot1 = "";
                        break;
                    case GAME_SAVE_SLOT_2:
                        YandexGame.savesData.gameSaveSlot2 = "";
                        break;
                    case GAME_SAVE_SLOT_3:
                        YandexGame.savesData.gameSaveSlot3 = "";
                        break;
                    default:
                        YandexGame.savesData.gameSaveSlot1 = "";
                        break;
                }

                YandexGame.SaveProgress();
                Debug.Log($"Сохранение из слота {saveSlot} удалено");
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка удаления сохранения: {e.Message}");
            }
#endif
        }

        public static string[] GetAvailableSlots()
        {
            return new string[] { GAME_SAVE_SLOT_1, GAME_SAVE_SLOT_2, GAME_SAVE_SLOT_3 };
        }
    }
}