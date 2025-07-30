using UnityEngine;

namespace RPG.Harvesting
{
	using RPG.Control;
    /// <summary>
    /// Обрабатывает события анимации добычи (Hit, Start, End)
    /// </summary>
    public class HarvestingAnimationEvents : MonoBehaviour
    {
        [Header("Компоненты")]
        [SerializeField] private HarvestingAudioManager audioManager;
        [SerializeField] private HarvestingVFXManager vfxManager;
        
        [Header("Настройки")]
        [SerializeField] private bool enableHitSounds = true;
        [SerializeField] private bool enableStartSounds = true;
        [SerializeField] private bool enableEndSounds = true;
        
        private HarvestingToolType currentToolType = HarvestingToolType.None;
        private HarvestableResource currentResource = null;
        private Vector3 harvestPosition = Vector3.zero;
        
        private void Awake()
        {
            // Автоматически находим компоненты если не назначены
            if (audioManager == null)
                audioManager = GetComponent<HarvestingAudioManager>();
                
            if (vfxManager == null)
                vfxManager = FindObjectOfType<HarvestingVFXManager>();
        }
        
        private void Start()
        {
            // Подписываемся на события добычи для получения информации
            if (HarvestingManager.Instance != null)
            {
                HarvestingManager.Instance.OnHarvestingStarted += OnHarvestingStarted;
                HarvestingManager.Instance.OnHarvestingStopped += OnHarvestingStopped;
            }
        }
        
        private void OnDestroy()
        {
            // Отписываемся от событий
            if (HarvestingManager.Instance != null)
            {
                HarvestingManager.Instance.OnHarvestingStarted -= OnHarvestingStarted;
                HarvestingManager.Instance.OnHarvestingStopped -= OnHarvestingStopped;
            }
        }
        
        /// <summary>
        /// Вызывается при начале добычи - сохраняем информацию
        /// </summary>
        private void OnHarvestingStarted(IHarvestable harvestable, PlayerController player)
        {
            currentResource = harvestable.GetResource();
            
            // Определяем тип инструмента
            var inventory = player.GetComponent<GameDevTV.Inventories.Inventory>();
            if (inventory != null && currentResource != null)
            {
                var bestTool = HarvestingToolChecker.FindBestTool(inventory, currentResource.RequiredToolType, currentResource.ResourceTier);
                currentToolType = bestTool != null ? bestTool.ToolType : HarvestingToolType.None;
            }
            else
            {
                currentToolType = HarvestingToolType.None;
            }
            
            // Сохраняем позицию добычи
            harvestPosition = (harvestable as MonoBehaviour)?.transform.position ?? Vector3.zero;
            
            Debug.Log($"HarvestingAnimationEvents: Начата добыча - инструмент: {currentToolType}, ресурс: {currentResource?.ResourceName}");
        }
        
        /// <summary>
        /// Вызывается при остановке добычи - очищаем информацию
        /// </summary>
        private void OnHarvestingStopped()
        {
            currentResource = null;
            currentToolType = HarvestingToolType.None;
            harvestPosition = Vector3.zero;
            
            Debug.Log("HarvestingAnimationEvents: Добыча остановлена");
        }
        
        /// <summary>
        /// Событие анимации - начало добычи
        /// Вызывается из анимации через AnimationEvent
        /// </summary>
        public void OnHarvestStart()
        {
            // Если нужен отдельный звук старта — добавьте его в AudioManager и вызовите здесь.
            // Сейчас ничего не делаем.
        }
        
        /// <summary>
        /// Событие анимации - удар/добыча ресурса
        /// Вызывается из анимации через AnimationEvent
        /// </summary>
        public void OnHarvestHit()
        {
            if (!enableHitSounds || audioManager == null) return;
            
            // Воспроизводим звук добычи ресурса (только через AudioManager, только один раз)
            audioManager.PlayResourceHitSound(currentResource);
            
            // Создаём VFX эффект добычи
            if (vfxManager != null)
            {
                vfxManager.CreateResourceHarvestEffect(harvestPosition, currentResource);
            }
            
            Debug.Log("HarvestingAnimationEvents: OnHarvestHit - воспроизведён звук добычи ресурса");
        }
        
        /// <summary>
        /// Событие анимации - завершение добычи
        /// Вызывается из анимации через AnimationEvent
        /// </summary>
        public void OnHarvestEnd()
        {
            if (!enableEndSounds || audioManager == null) return;
            audioManager.PlayResourceCompleteSound();
            Debug.Log("HarvestingAnimationEvents: OnHarvestEnd - воспроизведён звук завершения добычи");
        }
        
        /// <summary>
        /// Событие анимации - истощение ресурса
        /// Вызывается из анимации через AnimationEvent
        /// </summary>
        public void OnResourceDepleted()
        {
            if (audioManager == null) return;
            
            audioManager.PlayResourceDepletedSound();
            
            if (vfxManager != null)
            {
                vfxManager.CreateResourceDepletedEffect(harvestPosition);
            }
            
            Debug.Log("HarvestingAnimationEvents: OnResourceDepleted - воспроизведён звук истощения ресурса");
        }
        
        /// <summary>
        /// Получает текущий тип инструмента (для отладки)
        /// </summary>
        public HarvestingToolType GetCurrentToolType() => currentToolType;
        
        /// <summary>
        /// Получает текущий ресурс (для отладки)
        /// </summary>
        public HarvestableResource GetCurrentResource() => currentResource;
        
        /// <summary>
        /// Получает позицию добычи (для отладки)
        /// </summary>
        public Vector3 GetHarvestPosition() => harvestPosition;
    }
} 