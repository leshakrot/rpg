using UnityEngine;
using RPG.Control;
using RPG.Combat;
using RPG.Stats;

namespace RPG.Harvesting
{
    public class HarvestingManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private HarvestBar harvestBar = null;
        
        [Header("Настройки")]
        [SerializeField] private bool autoFindHarvestBar = true;
        [SerializeField] private float interruptionCheckDelay = 1.0f; // Задержка перед проверкой прерывания
        
        private static HarvestingManager instance;
        private IHarvestable currentHarvestTarget;
        private PlayerController player;
        private float harvestStartTime = 0f; // Время начала добычи
        
        // События для анимаций, звуков и VFX
        public System.Action<IHarvestable, PlayerController> OnHarvestingStarted;
        public System.Action OnHarvestingStopped;
        public System.Action<HarvestableResource, int> OnResourceHarvestedEvent;
        public System.Action OnResourceDepletedEvent;
        
        public static HarvestingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<HarvestingManager>();
                    
                    if (instance == null)
                    {
                        GameObject go = new GameObject("HarvestingManager");
                        instance = go.AddComponent<HarvestingManager>();
                    }
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            // Singleton pattern
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // Автоматический поиск UI если не назначен
            if (autoFindHarvestBar && harvestBar == null)
            {
                harvestBar = FindObjectOfType<HarvestBar>();
            }
        }
        
        private void Start()
        {
            // Находим игрока
            player = FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("HarvestingManager: PlayerController не найден!");
            }
        }
        
        private void Update()
        {
            // Проверяем прерывание добычи (например, движение или бой)
            CheckForHarvestInterruption();
        }
        
        public void StartHarvesting(IHarvestable target)
        {
            if (currentHarvestTarget != null)
            {
                StopHarvesting();
            }
            
            currentHarvestTarget = target;
            harvestStartTime = Time.time; // Записываем время начала добычи
            
            // Подписываемся на события
            target.OnHarvestComplete += OnResourceHarvested;
            target.OnResourceDepleted += OnResourceDepleted;
            
            // ВАЖНО: Запускаем добычу в источнике ресурса
            if (player != null)
            {
                target.StartHarvesting(player);
                
                // Вызываем событие начала добычи
                OnHarvestingStarted?.Invoke(target, player);
            }
            else
            {
                Debug.LogError("Player не найден!");
                return;
            }

            // Запускаем UI (предпочитаем новый HarvestBar)
            if (harvestBar != null)
            {
                // ИЗМЕНЕНИЕ: Вызываем новый метод
                harvestBar.StartHarvesting(target);
            }
            else
            {
                Debug.LogError("HarvestBar не найден! Добавьте HarvestBar в Canvas.");
            }
        }
        
        public void StopHarvesting()
        {
            if (currentHarvestTarget != null)
            {
                // Отписываемся от событий
                currentHarvestTarget.OnHarvestComplete -= OnResourceHarvested;
                currentHarvestTarget.OnResourceDepleted -= OnResourceDepleted;
                
                // Останавливаем добычу
                currentHarvestTarget.StopHarvesting();
                currentHarvestTarget = null;
                
                // Вызываем событие остановки добычи
                OnHarvestingStopped?.Invoke();
            }

            // Останавливаем UI
            if (harvestBar != null)
            {
                // ИЗМЕНЕНИЕ: Вызываем новый метод
                harvestBar.Stop();
            }
        }
        
        private void OnResourceHarvested(HarvestableResource resource, int amount)
        {
            // Здесь можно добавить дополнительную логику:
            // - Обновление статистик игрока
            // - Получение опыта
            // - Уведомления в UI
            // - Достижения
            
            // Пример получения опыта (если есть система опыта добычи)
            GiveHarvestingExperience(resource, amount);
            
            // Вызываем событие добычи ресурса
            OnResourceHarvestedEvent?.Invoke(resource, amount);
        }
        
        private void OnResourceDepleted()
        {
            // Вызываем событие истощения ресурса
            OnResourceDepletedEvent?.Invoke();
            
            StopHarvesting();
        }
        
        private void CheckForHarvestInterruption()
        {
            if (currentHarvestTarget == null || player == null) return;
            
            // Не проверяем прерывание сразу после начала добычи
            if (Time.time - harvestStartTime < interruptionCheckDelay)
            {
                return;
            }
            
            // Прерываем добычу если игрок начал движение
            var mover = player.GetComponent<RPG.Movement.Mover>();
            if (mover != null)
            {
                var navAgent = mover.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null && navAgent.velocity.magnitude > 2.0f) // Увеличил порог для стабильности
                {
                    Debug.Log($"Прерывание добычи: игрок движется со скоростью {navAgent.velocity.magnitude:F2}");
                    StopHarvesting();
                    return;
                }
            }
            
            // Прерываем добычу если игрок в бою (имеет цель для атаки)
            var fighter = player.GetComponent<RPG.Combat.PlayerFighter>();
            if (fighter != null && fighter.GetTarget() != null)
            {
                Debug.Log("Прерывание добычи: игрок в бою");
                StopHarvesting();
                return;
            }
        }
        
        private void GiveHarvestingExperience(HarvestableResource resource, int amount)
        {
            // Система опыта - даём опыт за добычу в зависимости от ресурса
            if (player != null && resource != null)
            {
                var experience = player.GetComponent<RPG.Stats.Experience>();
                if (experience != null)
                {
                    // Получаем количество опыта за единицу из настроек ресурса
                    float expPerUnit = resource.ExperiencePerUnit;
                    float totalExp = expPerUnit * amount;
                    
                    experience.GainExperience(totalExp);
                    
                    Debug.Log($"Получен опыт за добычу {resource.ResourceName}: {totalExp} (за {amount} единиц)");
                }
            }
        }
        
        // Публичные методы для внешнего управления
        public bool IsCurrentlyHarvesting => currentHarvestTarget != null && currentHarvestTarget.IsHarvesting;
        
        public void SetHarvestBar(HarvestBar newHarvestBar)
        {
            harvestBar = newHarvestBar;
        }
        
        public HarvestBar GetHarvestBar()
        {
            return harvestBar;
        }
        
        // Метод для интеграции с системой добычи
        public static void RegisterHarvestStart(IHarvestable target)
        {
            if (target == null)
            {
                Debug.LogError("RegisterHarvestStart: target is null!");
                return;
            }
            
            if (Instance == null)
            {
                Debug.LogError("RegisterHarvestStart: HarvestingManager.Instance is null!");
                return;
            }
            
            Debug.Log($"Начинаем добычу: {target.GetResource()?.ResourceName}");
            Instance.StartHarvesting(target);
        }
        
        public static void RegisterHarvestStop()
        {
            if (Instance == null)
            {
                return;
            }
            
            Instance.StopHarvesting();
        }
    }
} 