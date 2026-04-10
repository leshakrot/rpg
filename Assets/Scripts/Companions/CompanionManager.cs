using System.Collections.Generic;
using UnityEngine;
using GameDevTV.Saving;
using System.Linq;

namespace RPG.Companions
{
    /// <summary>
    /// Глобальный менеджер компаньонов - управляет активными компаньонами игрока
    /// Singleton, сохраняется между сценами
    /// </summary>
    public class CompanionManager : MonoBehaviour, ISaveable
    {
        private static CompanionManager _instance;
        public static CompanionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CompanionManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CompanionManager");
                        _instance = go.AddComponent<CompanionManager>();
                    }
                }
                return _instance;
            }
        }

        // Список активных компаньонов (ID компаньона -> данные о найме)
        private Dictionary<string, HiredCompanionInfo> _hiredCompanions = new Dictionary<string, HiredCompanionInfo>();
        
        // Ссылки на активные экземпляры компаньонов в текущей сцене
        private Dictionary<string, CompanionController> _activeCompanions = new Dictionary<string, CompanionController>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Добавляем SaveableEntity если его нет
            if (GetComponent<GameDevTV.Saving.SaveableEntity>() == null)
            {
                gameObject.AddComponent<GameDevTV.Saving.SaveableEntity>();
            }
        }

        /// <summary>
        /// Нанять компаньона
        /// </summary>
        public bool HireCompanion(string companionID, string recruiterSceneName, Vector3 recruiterPosition)
        {
            if (_hiredCompanions.ContainsKey(companionID))
            {
                Debug.LogWarning($"Компаньон {companionID} уже нанят");
                return false;
            }

            HiredCompanionInfo info = new HiredCompanionInfo
            {
                companionID = companionID,
                isActive = true,
                recruiterSceneName = recruiterSceneName,
                recruiterPosition = recruiterPosition
            };

            _hiredCompanions[companionID] = info;
            return true;
        }

        /// <summary>
        /// Отправить компаньона на базу (к рекрутеру)
        /// </summary>
        public void DismissCompanion(string companionID)
        {
            if (_hiredCompanions.ContainsKey(companionID))
            {
                _hiredCompanions[companionID].isActive = false;
                
                // Удаляем из активных если есть
                if (_activeCompanions.ContainsKey(companionID))
                {
                    CompanionController companion = _activeCompanions[companionID];
                    if (companion != null)
                    {
                        Destroy(companion.gameObject);
                    }
                    _activeCompanions.Remove(companionID);
                }
            }
        }

        /// <summary>
        /// Активировать компаньона (вызвать обратно)
        /// </summary>
        public void ActivateCompanion(string companionID)
        {
            if (_hiredCompanions.ContainsKey(companionID))
            {
                _hiredCompanions[companionID].isActive = true;
            }
        }

        /// <summary>
        /// Проверить нанят ли компаньон
        /// </summary>
        public bool IsCompanionHired(string companionID)
        {
            return _hiredCompanions.ContainsKey(companionID);
        }

        /// <summary>
        /// Проверить активен ли компаньон (следует за игроком)
        /// </summary>
        public bool IsCompanionActive(string companionID)
        {
            return _hiredCompanions.ContainsKey(companionID) && _hiredCompanions[companionID].isActive;
        }

        /// <summary>
        /// Получить информацию о компаньоне
        /// </summary>
        public HiredCompanionInfo GetCompanionInfo(string companionID)
        {
            return _hiredCompanions.ContainsKey(companionID) ? _hiredCompanions[companionID] : null;
        }

        /// <summary>
        /// Зарегистрировать активного компаньона в сцене
        /// </summary>
        public void RegisterActiveCompanion(string companionID, CompanionController companion)
        {
            _activeCompanions[companionID] = companion;
        }

        /// <summary>
        /// Удалить регистрацию компаньона
        /// </summary>
        public void UnregisterActiveCompanion(string companionID)
        {
            _activeCompanions.Remove(companionID);
        }

        /// <summary>
        /// Получить список всех активных компаньонов
        /// </summary>
        public List<string> GetActiveCompanionIDs()
        {
            return _hiredCompanions
                .Where(kvp => kvp.Value.isActive)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        // Сохранение/загрузка
        [System.Serializable]
        public struct CompanionManagerSaveData
        {
            public List<HiredCompanionInfo> hiredCompanions;
        }

        public object CaptureState()
        {
            return new CompanionManagerSaveData
            {
                hiredCompanions = _hiredCompanions.Values.ToList()
            };
        }

        public void RestoreState(object state)
        {
            if (state is CompanionManagerSaveData data)
            {
                _hiredCompanions.Clear();
                foreach (var info in data.hiredCompanions)
                {
                    _hiredCompanions[info.companionID] = info;
                }
            }
        }
    }

    /// <summary>
    /// Информация о нанятом компаньоне
    /// </summary>
    [System.Serializable]
    public class HiredCompanionInfo
    {
        public string companionID;
        public bool isActive; // Следует за игроком или на базе
        public string recruiterSceneName; // Сцена где был нанят
        public Vector3 recruiterPosition; // Позиция рекрутера
    }
}
