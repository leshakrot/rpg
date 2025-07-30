using UnityEngine;
using RPG.Combat;
using RPG.Stats;

namespace RPG.Harvesting
{
	using RPG.Control;
    /// <summary>
    /// Управляет анимациями добычи для игрока
    /// </summary>
    public class HarvestingAnimationController : MonoBehaviour
    {
        [Header("Аниматор")]
        [SerializeField] private Animator animator;
        
        [Header("Параметры аниматора")]
        [SerializeField] private string harvestingParameter = "harvesting";
        [SerializeField] private string harvestingSpeedParameter = "harvestingSpeed";
        [SerializeField] private string toolTypeParameter = "toolType";
        
        [Header("Настройки анимации")]
        [SerializeField] private float minHarvestingSpeed = 0.5f;
        [SerializeField] private float maxHarvestingSpeed = 2.0f;
        
        private bool isHarvesting = false;
        private HarvestingToolType currentToolType = HarvestingToolType.None;
        
        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        private void Start()
        {
            // Подписываемся на события добычи только для управления аниматором
            if (HarvestingManager.Instance != null)
            {
                HarvestingManager.Instance.OnHarvestingStarted += StartHarvestingAnimation;
                HarvestingManager.Instance.OnHarvestingStopped += StopHarvestingAnimation;
            }
        }
        
        private void OnDestroy()
        {
            // Отписываемся от событий
            if (HarvestingManager.Instance != null)
            {
                HarvestingManager.Instance.OnHarvestingStarted -= StartHarvestingAnimation;
                HarvestingManager.Instance.OnHarvestingStopped -= StopHarvestingAnimation;
            }
        }
        
        /// <summary>
        /// Начинает анимацию добычи
        /// </summary>
        /// <param name="harvestable">Объект добычи</param>
        /// <param name="player">Игрок</param>
        public void StartHarvestingAnimation(IHarvestable harvestable, PlayerController player)
        {
            if (animator == null) return;
            
            isHarvesting = true;
            
            // Определяем тип инструмента
            var inventory = player.GetComponent<GameDevTV.Inventories.Inventory>();
            var resource = harvestable.GetResource();
            
            if (inventory != null && resource != null)
            {
                var bestTool = HarvestingToolChecker.FindBestTool(inventory, resource.RequiredToolType, resource.ResourceTier);
                currentToolType = bestTool != null ? bestTool.ToolType : HarvestingToolType.None;
            }
            else
            {
                currentToolType = HarvestingToolType.None;
            }
            
            // Устанавливаем параметры аниматора
            animator.SetBool(harvestingParameter, true);
            animator.SetInteger(toolTypeParameter, (int)currentToolType);
            
            // Рассчитываем скорость анимации на основе времени добычи
            float harvestTime = resource.GetHarvestTime(player.GetComponent<BaseStats>().GetLevel(), null);
            float animationSpeed = Mathf.Lerp(maxHarvestingSpeed, minHarvestingSpeed, harvestTime / 5f); // 5 сек - базовая скорость
            animator.SetFloat(harvestingSpeedParameter, animationSpeed);
            
            Debug.Log($"Начата анимация добычи: инструмент={currentToolType}, скорость={animationSpeed:F2}");
        }
        
        /// <summary>
        /// Останавливает анимацию добычи
        /// </summary>
        public void StopHarvestingAnimation()
        {
            if (animator == null) return;
            
            isHarvesting = false;
            
            // Сбрасываем параметры аниматора
            animator.SetBool(harvestingParameter, false);
            animator.SetInteger(toolTypeParameter, (int)HarvestingToolType.None);
            animator.SetFloat(harvestingSpeedParameter, 1f);
            
            Debug.Log("Остановлена анимация добычи");
        }
        
        /// <summary>
        /// Обновляет скорость анимации во время добычи
        /// </summary>
        /// <param name="harvestTime">Время добычи одной единицы ресурса</param>
        public void UpdateHarvestingSpeed(float harvestTime)
        {
            if (animator == null || !isHarvesting) return;
            
            float animationSpeed = Mathf.Lerp(maxHarvestingSpeed, minHarvestingSpeed, harvestTime / 5f);
            animator.SetFloat(harvestingSpeedParameter, animationSpeed);
        }
        
        /// <summary>
        /// Проверяет, активна ли анимация добычи
        /// </summary>
        public bool IsHarvesting => isHarvesting;
        
        /// <summary>
        /// Получает текущий тип инструмента
        /// </summary>
        public HarvestingToolType CurrentToolType => currentToolType;
    }
} 