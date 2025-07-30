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
        [SerializeField] private string resourceCategoryParameter = "resourceCategory";
        // Новый параметр для Animator, если вы хотите использовать его для "без инструмента"
        [SerializeField] private string noToolHarvestParameter = "noToolHarvest"; // <-- НОВОЕ ПОЛЕ

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
            if (HarvestingManager.Instance != null)
            {
                HarvestingManager.Instance.OnHarvestingStarted += StartHarvestingAnimation;
                HarvestingManager.Instance.OnHarvestingStopped += StopHarvestingAnimation;
            }
        }

        private void OnDestroy()
        {
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

            // Устанавливаем общие параметры аниматора
            animator.SetBool(harvestingParameter, true);
            animator.SetInteger(toolTypeParameter, (int)currentToolType); // Устанавливаем toolType (-1 для None)

            // Сбрасываем параметр noToolHarvest по умолчанию, на случай если он был активен
            if (HasAnimatorParameter(animator, noToolHarvestParameter))
            {
                animator.SetBool(noToolHarvestParameter, false);
            }

            // Логика для выбора анимации: сначала проверяем "без инструмента", затем "по категории"
            if (currentToolType == HarvestingToolType.None && resource.NoToolHarvestAnimation != null)
            {
                Debug.Log($"Воспроизводится анимация без инструмента для {resource.ResourceName}");
                // Если есть специальная анимация для сбора без инструмента, используем её
                // ВНИМАНИЕ: Для прямого проигрывания клипа (если он не связан со состоянием Animator'а)
                // вам может понадобиться использовать Animator.Play("ИмяСостояния")
                // или установить булевый параметр, который ведет к этому состоянию.
                // Ниже предлагается установка булевого параметра.
                if (HasAnimatorParameter(animator, noToolHarvestParameter))
                {
                    animator.SetBool(noToolHarvestParameter, true); // Активируем параметр "без инструмента"
                }
            }
            else // Иначе используем логику по категории ресурса (как было ранее)
            {
                if (resource != null)
                {
                    animator.SetInteger(resourceCategoryParameter, (int)resource.ResourceCategory);
                }
                Debug.Log($"Воспроизводится анимация по категории для {resource.ResourceName}: {resource.ResourceCategory}");
            }

            // Рассчитываем скорость анимации на основе времени добычи
            float harvestTime = resource.GetHarvestTime(player.GetComponent<BaseStats>().GetLevel(), null);
            float animationSpeed = Mathf.Lerp(maxHarvestingSpeed, minHarvestingSpeed, harvestTime / 5f);
            animator.SetFloat(harvestingSpeedParameter, animationSpeed);

            Debug.Log($"Начата анимация добычи: инструмент={currentToolType} ({(int)currentToolType}), ресурс={resource.ResourceName}, категория={resource.ResourceCategory} ({(int)resource.ResourceCategory}), скорость={animationSpeed:F2}");
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
            animator.SetInteger(resourceCategoryParameter, -1); // Сброс категории
            if (HasAnimatorParameter(animator, noToolHarvestParameter))
            {
                animator.SetBool(noToolHarvestParameter, false); // Сбрасываем параметр "без инструмента"
            }
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

        // Вспомогательный метод для проверки наличия параметра в Animator
        private bool HasAnimatorParameter(Animator animator, string paramName)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}