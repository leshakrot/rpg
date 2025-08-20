using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using GameDevTV.Saving;
using Newtonsoft.Json;
using RPG.Core;
using RPG.Movement;
using RPG.Attributes;
using RPG.Combat;

namespace RPG.Control
{
    [System.Serializable]
    public class DailyActivity
    {
        [Header("Время активности")]
        public int hour = 8;
        public int minute = 0;
        
        [Header("Действие")]
        public string activityName = "Работать";
        public ActivityType activityType = ActivityType.Work;
        public UnityEvent onActivityStart = new UnityEvent();
        
        [Header("Местоположение (опционально)")]
        public Transform targetLocation;
        public bool moveToLocation = false;
        
        [Header("Длительность")]
        public float durationMinutes = 60f;
        public bool hasExecuted = false;

        [Header("Кастомные действия")]
        public UnityEvent onWorkAction = new UnityEvent();
        public UnityEvent onEatAction = new UnityEvent();
        public UnityEvent onSleepAction = new UnityEvent();
        public UnityEvent onSocializeAction = new UnityEvent();
        public UnityEvent onPatrolAction = new UnityEvent();
    }

    [System.Serializable]
    public class NPCSaveData
    {
        [JsonProperty] public SerializableVector3 position;
        [JsonProperty] public SerializableQuaternion rotation;
        [JsonProperty] public int currentSceneIndex;
        [JsonProperty] public string lastActivityName;
        [JsonProperty] public bool[] activitiesExecuted;
        [JsonProperty] public float lastActivityStartTime;
    }

    public class PeacefulNPC : MonoBehaviour, ISaveable
    {
        [Header("NPC Identity")]
        [SerializeField] private string _npcName = "Житель";
        [SerializeField] private int _npcId = 0;

        [Header("Profession")]
        [SerializeField] private NPCProfession _profession;
        [SerializeField] private bool _useDefaultProfessionSchedule = true;

        [Header("Movement")]
        [Range(0,1)]
        [SerializeField] private float _walkSpeedFraction = 0.3f;

        [Header("Custom Actions")]
        [SerializeField] private UnityEvent _onWorkAction = new UnityEvent();
        [SerializeField] private UnityEvent _onEatAction = new UnityEvent();
        [SerializeField] private UnityEvent _onSleepAction = new UnityEvent();
        [SerializeField] private UnityEvent _onSocializeAction = new UnityEvent();
        [SerializeField] private UnityEvent _onPatrolAction = new UnityEvent();

        [Header("Daily Schedule")]
        [SerializeField] private List<DailyActivity> _dailyActivities = new List<DailyActivity>();
        [SerializeField] private bool _useSchedule = true;

        [Header("Scene Migration")]
        [SerializeField] private bool _canMigrateBetweenScenes = true;

        private Mover _mover;
        private AIController _aiController;
        private ActionScheduler _actionScheduler;
        private NavMeshAgent _navMeshAgent;
        private Animator _animator;

        // Optional components
        private Health _health;
        private Fighter _fighter;

        // Runtime data
        private DailyActivity _currentActivity;
        private string _lastActivityName;
        private float _lastActivityStartTime;
        private bool _isPerformingActivity = false;

        // Day/Night system reference
        private DayNightSystem _dayNightSystem;

        private void Awake()
        {
            // Required components
            _mover = GetComponent<Mover>();
            _aiController = GetComponent<AIController>();
            _actionScheduler = GetComponent<ActionScheduler>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();

            // Optional components
            _health = GetComponent<Health>();
            _fighter = GetComponent<Fighter>();

            // Find DayNightSystem
            _dayNightSystem = FindObjectOfType<DayNightSystem>();
        }

        private void Start()
        {
            // Если используется профессия и нужно применить её расписание
            if (_profession != null && _useDefaultProfessionSchedule && _dailyActivities.Count == 0)
            {
                GenerateScheduleFromProfession();
            }

            RegisterTimeEvents();
        }

        private void GenerateScheduleFromProfession()
        {
            if (_profession == null || _profession.defaultSchedule == null) return;

            _dailyActivities.Clear();

            foreach (var profActivity in _profession.defaultSchedule)
            {
                var dailyActivity = new DailyActivity
                {
                    hour = profActivity.startHour,
                    minute = profActivity.startMinute,
                    activityName = profActivity.activityName,
                    activityType = profActivity.activityType,
                    durationMinutes = profActivity.durationMinutes,
                    moveToLocation = false,
                    hasExecuted = false
                };

                // Настраиваем действия в зависимости от типа
                SetupActivityActions(dailyActivity);

                _dailyActivities.Add(dailyActivity);
            }

            Debug.Log($"Создано расписание из профессии {_profession.professionName} для {_npcName}: {_dailyActivities.Count} активностей");
        }

        private void SetupActivityActions(DailyActivity activity)
        {
            // Основное событие активности
            activity.onActivityStart.AddListener(() => {
                Debug.Log($"{_npcName} ({GetProfessionName()}) начинает: {activity.activityName}");
                ExecuteActivityByType(activity.activityType);
            });

            // Настройка специфичных действий для типов активностей
            switch (activity.activityType)
            {
                case ActivityType.Work:
                    activity.onWorkAction.AddListener(() => PerformWorkAction());
                    break;
                case ActivityType.Eat:
                    activity.onEatAction.AddListener(() => PerformEatAction());
                    break;
                case ActivityType.Sleep:
                    activity.onSleepAction.AddListener(() => PerformSleepAction());
                    break;
                case ActivityType.Socialize:
                    activity.onSocializeAction.AddListener(() => PerformSocializeAction());
                    break;
                case ActivityType.Patrol:
                    activity.onPatrolAction.AddListener(() => PerformPatrolAction());
                    break;
            }
        }

        private void RegisterTimeEvents()
        {
            if (!_useSchedule || _dayNightSystem == null) return;

            foreach (var activity in _dailyActivities)
            {
                UnityEvent activityEvent = new UnityEvent();
                activityEvent.AddListener(() => StartActivity(activity));
                _dayNightSystem.AddTimeEvent(activity.hour, activity.minute, activityEvent);
            }
        }

        public void StartActivity(DailyActivity activity)
        {
            if (!_useSchedule || activity.hasExecuted) return;

            _currentActivity = activity;
            _lastActivityName = activity.activityName;
            _lastActivityStartTime = Time.time;
            _isPerformingActivity = true;
            activity.hasExecuted = true;

            Debug.Log($"{_npcName} начинает активность: {activity.activityName} в {activity.hour:00}:{activity.minute:00}");

            // Переместиться к локации если указана
            if (activity.moveToLocation && activity.targetLocation != null)
            {
                MoveTo(activity.targetLocation.position);
            }

            // Вызвать основное событие активности
            activity.onActivityStart?.Invoke();

            // Выполнить специфичное действие в зависимости от типа
            ExecuteActivityByType(activity.activityType);

            // Запланировать окончание активности
            if (activity.durationMinutes > 0)
            {
                Invoke(nameof(EndCurrentActivity), activity.durationMinutes * 60f);
            }
        }

        private void ExecuteActivityByType(ActivityType activityType)
        {
            switch (activityType)
            {
                case ActivityType.Work:
                    PerformWorkAction();
                    break;
                case ActivityType.Eat:
                    PerformEatAction();
                    break;
                case ActivityType.Sleep:
                    PerformSleepAction();
                    break;
                case ActivityType.Socialize:
                    PerformSocializeAction();
                    break;
                case ActivityType.Patrol:
                    PerformPatrolAction();
                    break;
            }
        }

        private void PerformWorkAction()
        {
            // Вызываем кастомное действие работы
            _onWorkAction?.Invoke();
            
            // Если есть кастомное действие в текущей активности
            if (_currentActivity != null)
            {
                _currentActivity.onWorkAction?.Invoke();
            }

            // Воспроизводим анимацию работы если есть профессия
            if (_profession != null && _animator != null && !string.IsNullOrEmpty(_profession.workAnimationTrigger))
            {
                _animator.SetTrigger(_profession.workAnimationTrigger);
            }

            Debug.Log($"{_npcName} выполняет работу: {GetWorkDescription()}");
        }

        private void PerformEatAction()
        {
            _onEatAction?.Invoke();
            if (_currentActivity != null)
            {
                _currentActivity.onEatAction?.Invoke();
            }
            Debug.Log($"{_npcName} ест");
        }

        private void PerformSleepAction()
        {
            _onSleepAction?.Invoke();
            if (_currentActivity != null)
            {
                _currentActivity.onSleepAction?.Invoke();
            }
            Debug.Log($"{_npcName} спит");
        }

        private void PerformSocializeAction()
        {
            _onSocializeAction?.Invoke();
            if (_currentActivity != null)
            {
                _currentActivity.onSocializeAction?.Invoke();
            }
            Debug.Log($"{_npcName} общается с другими");
        }

        private void PerformPatrolAction()
        {
            _onPatrolAction?.Invoke();
            if (_currentActivity != null)
            {
                _currentActivity.onPatrolAction?.Invoke();
            }
            Debug.Log($"{_npcName} патрулирует");
        }

        private void EndCurrentActivity()
        {
            if (_currentActivity != null)
            {
                Debug.Log($"{_npcName} заканчивает активность: {_currentActivity.activityName}");
                _currentActivity = null;
                _isPerformingActivity = false;
            }
        }

        public void MoveTo(Vector3 destination)
        {
            if (_mover != null && _mover.CanMoveTo(destination))
            {
                _mover.StartMoveAction(destination, _walkSpeedFraction);
            }
        }

        public void StopMovement()
        {
            if (_mover != null)
            {
                _mover.Cancel();
            }
        }

        // Методы для работы - УСТАРЕВШИЕ, используйте UnityEvents
        [System.Obsolete("Используйте кастомные UnityEvents вместо этих методов")]
        public void StartWorking()
        {
            PerformWorkAction();
        }

        [System.Obsolete("Используйте кастомные UnityEvents вместо этих методов")]
        public void StartEating()
        {
            PerformEatAction();
        }

        [System.Obsolete("Используйте кастомные UnityEvents вместо этих методов")]
        public void StartSleeping()
        {
            PerformSleepAction();
        }

        [System.Obsolete("Используйте кастомные UnityEvents вместо этих методов")]
        public void StartSocializing()
        {
            PerformSocializeAction();
        }

        [System.Obsolete("Используйте кастомные UnityEvents вместо этих методов")]
        public void StartPatrolling()
        {
            PerformPatrolAction();
        }

        // Методы для миграции между сценами
        public void MigrateToScene(int sceneIndex)
        {
            if (!_canMigrateBetweenScenes)
            {
                Debug.LogWarning($"{_npcName} не может мигрировать между сценами");
                return;
            }

            // Сохраняем текущее состояние перед миграцией
            var saveData = (NPCSaveData)CaptureState();
            saveData.currentSceneIndex = sceneIndex;

            Debug.Log($"{_npcName} мигрирует в сцену с индексом: {sceneIndex}");

            // Здесь можно добавить логику для фактической миграции
            // Например, деактивация в текущей сцене и активация в целевой
            gameObject.SetActive(false);
        }

        public void MigrateToScene(string sceneName)
        {
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
            if (sceneIndex >= 0)
            {
                MigrateToScene(sceneIndex);
            }
            else
            {
                Debug.LogError($"Сцена {sceneName} не найдена в Build Settings");
            }
        }

        // Методы для ручного управления активностями
        public void ForceStartActivity(string activityName)
        {
            var activity = _dailyActivities.Find(a => a.activityName == activityName);
            if (activity != null)
            {
                activity.hasExecuted = false; // Сбрасываем флаг выполнения
                StartActivity(activity);
            }
            else
            {
                Debug.LogWarning($"Активность {activityName} не найдена у {_npcName}");
            }
        }

        public void ResetDailySchedule()
        {
            foreach (var activity in _dailyActivities)
            {
                activity.hasExecuted = false;
            }
            Debug.Log($"Расписание дня сброшено для {_npcName}");
        }

        // Геттеры для информации о NPC
        public string NPCName => _npcName;
        public int NPCId => _npcId;
        public bool IsPerformingActivity => _isPerformingActivity;
        public DailyActivity CurrentActivity => _currentActivity;
        public string LastActivityName => _lastActivityName;
        public NPCProfession Profession => _profession;
        public string GetProfessionName() => _profession != null ? _profession.professionName : "Без профессии";
        public string GetWorkDescription() => _profession != null ? _profession.workDescription : "Выполняет общую работу";

        // Проверки компонентов
        public bool HasHealth => _health != null;
        public bool HasFighter => _fighter != null;
        public bool CanFight => _fighter != null && _health != null && !_health.IsDead();

        // ISaveable Implementation
        public object CaptureState()
        {
            var saveData = new NPCSaveData
            {
                position = new SerializableVector3(transform.position),
                rotation = new SerializableQuaternion(transform.rotation),
                currentSceneIndex = SceneManager.GetActiveScene().buildIndex,
                lastActivityName = _lastActivityName,
                lastActivityStartTime = _lastActivityStartTime,
                activitiesExecuted = new bool[_dailyActivities.Count]
            };

            for (int i = 0; i < _dailyActivities.Count; i++)
            {
                saveData.activitiesExecuted[i] = _dailyActivities[i].hasExecuted;
            }

            return saveData;
        }

        public void RestoreState(object state)
        {
            if (!(state is NPCSaveData saveData)) return;

            // Восстанавливаем позицию
            if (_navMeshAgent != null)
            {
                _navMeshAgent.enabled = false;
            }
            
            transform.position = saveData.position.ToVector();
            transform.rotation = saveData.rotation.ToQuaternion();
            
            if (_navMeshAgent != null)
            {
                _navMeshAgent.enabled = true;
            }

            // Восстанавливаем состояние активностей
            _lastActivityName = saveData.lastActivityName;
            _lastActivityStartTime = saveData.lastActivityStartTime;

            if (saveData.activitiesExecuted != null && 
                saveData.activitiesExecuted.Length == _dailyActivities.Count)
            {
                for (int i = 0; i < _dailyActivities.Count; i++)
                {
                    _dailyActivities[i].hasExecuted = saveData.activitiesExecuted[i];
                }
            }

            // Если NPC был в другой сцене, помечаем для миграции
            if (saveData.currentSceneIndex != SceneManager.GetActiveScene().buildIndex)
            {
                Debug.Log($"{_npcName} должен быть в сцене {saveData.currentSceneIndex}, " +
                         $"но находится в {SceneManager.GetActiveScene().buildIndex}");
                
                if (_canMigrateBetweenScenes)
                {
                    // Можно добавить логику автоматической миграции при загрузке
                }
            }

            Debug.Log($"Состояние {_npcName} восстановлено. Последняя активность: {_lastActivityName}");
        }

        private void OnValidate()
        {
            // Убеждаемся что у каждой активности уникальное имя
            for (int i = 0; i < _dailyActivities.Count; i++)
            {
                if (string.IsNullOrEmpty(_dailyActivities[i].activityName))
                {
                    _dailyActivities[i].activityName = $"Активность {i + 1}";
                }
            }
        }

        // Debug info
        private void OnDrawGizmosSelected()
        {
            if (_dailyActivities == null) return;

            Gizmos.color = Color.green;
            foreach (var activity in _dailyActivities)
            {
                if (activity.targetLocation != null)
                {
                    Gizmos.DrawWireSphere(activity.targetLocation.position, 1f);
                    Gizmos.DrawLine(transform.position, activity.targetLocation.position);
                }
            }
        }
    }
}