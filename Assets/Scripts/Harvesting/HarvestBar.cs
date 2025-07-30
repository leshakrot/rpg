using RPG.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.Harvesting
{
    public class HarvestBar : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private RectTransform foreground = null;
        [SerializeField] private Canvas rootCanvas = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private Text resourceNameText = null;
        [SerializeField] private Text progressText = null;
        [SerializeField] private Image backgroundImage = null;
        
        [Header("Settings")]
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private float hideDelay = 0.5f;
        
        // Состояние прогресса
        private float currentProgressFraction = 1f;
        private float targetProgressFraction = 1f;
        private IHarvestable targetHarvestable = null;
        private bool isTracking = false;
        private float hideTimer = 0f;
        
        private void Awake()
        {
            // Получаем CanvasGroup для безопасного управления видимостью
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
                
            // Если нет CanvasGroup - создаём
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Для HUD элементов Canvas может быть родительским, не трогаем его настройки
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();
                
            // Скрываем UI по умолчанию
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        private void Update()
        {
            if (!isTracking || targetHarvestable == null)
            {
                // Плавное скрытие с задержкой
                hideTimer += Time.deltaTime;
                if (hideTimer >= hideDelay && canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
                return;
            }
            
            hideTimer = 0f;
            
            // Убеждаемся что UI виден
            if (canvasGroup != null && canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            // Плавное изменение прогресса
            currentProgressFraction = Mathf.Lerp(currentProgressFraction, targetProgressFraction, Time.deltaTime * smoothSpeed);
            
            // Если разница очень мала, просто устанавливаем равенство
            if (Mathf.Abs(currentProgressFraction - targetProgressFraction) < 0.001f)
            {
                currentProgressFraction = targetProgressFraction;
            }
            
            // Обновляем визуалы
            UpdateBarVisuals();
            
            // Обновляем текст прогресса
            UpdateProgressText();
        }
        
        public void StartTracking(IHarvestable harvestable)
        {
            if (targetHarvestable != null)
            {
                StopTracking();
            }
            
            targetHarvestable = harvestable;
            isTracking = true;
            hideTimer = 0f;
            
            if (harvestable != null)
            {
                // Подписываемся на события
                harvestable.OnHarvestProgress += OnProgressChanged;
                harvestable.OnResourceDepleted += OnResourceDepleted;
                
                // Настраиваем UI для ресурса
                SetupForResource(harvestable.GetResource());
                
                // Получаем текущий прогресс
                GetCurrentProgressFromSource();
            }
        }
        
        public void StopTracking()
        {
            if (targetHarvestable != null)
            {
                // Отписываемся от событий
                targetHarvestable.OnHarvestProgress -= OnProgressChanged;
                targetHarvestable.OnResourceDepleted -= OnResourceDepleted;
            }
            
            targetHarvestable = null;
            isTracking = false;
        }
        
        private void SetupForResource(HarvestableResource resource)
        {
            if (resource == null) return;
            
            // Устанавливаем название ресурса
            if (resourceNameText != null)
            {
                resourceNameText.text = resource.ResourceName;
            }
            
            // Устанавливаем цвет прогресс-бара
            if (backgroundImage != null)
            {
                backgroundImage.color = resource.ProgressBarColor;
            }
            
            // Если есть foreground, тоже устанавливаем цвет
            if (foreground != null)
            {
                var foregroundImage = foreground.GetComponent<Image>();
                if (foregroundImage != null)
                {
                    foregroundImage.color = resource.ProgressBarColor;
                }
            }
        }
        
        private void OnProgressChanged(float progress)
        {
            targetProgressFraction = progress;
        }
        
        private void OnResourceDepleted()
        {
            targetProgressFraction = 0f;
        }
        
        private void SetProgress(float progress)
        {
            targetProgressFraction = Mathf.Clamp01(progress);
            currentProgressFraction = targetProgressFraction;
        }
        
        private void UpdateBarVisuals()
        {
            if (foreground != null)
            {
                // Устанавливаем размер foreground в зависимости от прогресса
                foreground.anchorMax = new Vector2(currentProgressFraction, 1f);
            }
        }
        
        private void UpdateProgressText()
        {
            if (progressText != null)
            {
                int percentage = Mathf.RoundToInt(currentProgressFraction * 100f);
                progressText.text = $"{percentage}%";
            }
        }
        
        public bool IsTracking => isTracking && targetHarvestable != null;
        
        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }
        
        public void SetWorldOffset(Vector3 offset)
        {
            // Метод для позиционирования в world space (если понадобится)
        }
        
        private void GetCurrentProgressFromSource()
        {
            if (targetHarvestable == null) return;
            
            // Получаем текущий прогресс из источника
            var source = targetHarvestable as HarvestableSource;
            if (source != null)
            {
                // Вычисляем прогресс на основе оставшихся ресурсов
                int remaining = source.GetRemainingResources();
                var resource = source.GetResource();
                
                if (resource != null && resource.ResourceAmount > 0)
                {
                    float progress = (float)remaining / resource.ResourceAmount;
                    SetProgress(progress);
                }
            }
        }
    }
} 