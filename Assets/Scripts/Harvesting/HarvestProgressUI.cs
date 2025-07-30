using UnityEngine;
using UnityEngine.UI;
using RPG.UI;

namespace RPG.Harvesting
{
    /// <summary>
    /// Компонент для отображения прогресса добычи ресурсов
    /// Используется как альтернатива HarvestBar для более простых случаев
    /// </summary>
    public class HarvestProgressUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Slider progressSlider = null;
        [SerializeField] private Text resourceNameText = null;
        [SerializeField] private Text progressText = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        
        [Header("Settings")]
        [SerializeField] private float showDuration = 0.5f;
        [SerializeField] private float hideDuration = 0.3f;
        
        private IHarvestable currentTarget = null;
        private bool isVisible = false;
        
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
                
            // Скрываем по умолчанию
            SetVisible(false, false);
        }
        
        public void StartTracking(IHarvestable harvestable)
        {
            if (currentTarget != null)
            {
                StopTracking();
            }
            
            currentTarget = harvestable;
            
            if (harvestable != null)
            {
                // Подписываемся на события
                harvestable.OnHarvestProgress += OnProgressChanged;
                harvestable.OnResourceDepleted += OnResourceDepleted;
                
                // Настраиваем UI
                SetupForResource(harvestable.GetResource());
                
                // Показываем UI
                SetVisible(true, true);
                
                Debug.Log($"HarvestProgressUI: Начато отслеживание {harvestable.GetResource().ResourceName}");
            }
        }
        
        public void StopTracking()
        {
            if (currentTarget != null)
            {
                // Отписываемся от событий
                currentTarget.OnHarvestProgress -= OnProgressChanged;
                currentTarget.OnResourceDepleted -= OnResourceDepleted;
            }
            
            currentTarget = null;
            
            // Скрываем UI
            SetVisible(false, true);
            
            Debug.Log("HarvestProgressUI: Остановлено отслеживание");
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
            if (progressSlider != null)
            {
                var fillImage = progressSlider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = resource.ProgressBarColor;
                }
            }
        }
        
        private void OnProgressChanged(float progress)
        {
            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }
            
            if (progressText != null)
            {
                int percentage = Mathf.RoundToInt(progress * 100f);
                progressText.text = $"{percentage}%";
            }
            
            Debug.Log($"HarvestProgressUI: Прогресс изменён на {progress:F2}");
        }
        
        private void OnResourceDepleted()
        {
            if (progressSlider != null)
            {
                progressSlider.value = 0f;
            }
            
            if (progressText != null)
            {
                progressText.text = "0%";
            }
            
            Debug.Log("HarvestProgressUI: Ресурс истощён");
        }
        
        private void SetVisible(bool visible, bool animate)
        {
            if (canvasGroup == null) return;
            
            isVisible = visible;
            
            if (animate)
            {
                // Анимация появления/исчезновения
                float targetAlpha = visible ? 1f : 0f;
                float duration = visible ? showDuration : hideDuration;
                
                // Простая анимация через корутину
                StartCoroutine(AnimateAlpha(targetAlpha, duration));
            }
            else
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }
        
        private System.Collections.IEnumerator AnimateAlpha(float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                
                yield return null;
            }
            
            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }
        
        public bool IsTracking => currentTarget != null;
        
        // Методы для отладки
        [ContextMenu("Debug UI Components")]
        private void DebugUIComponents()
        {
            Debug.Log($"HarvestProgressUI Debug:");
            Debug.Log($"  - ProgressSlider: {progressSlider != null}");
            Debug.Log($"  - ResourceNameText: {resourceNameText != null}");
            Debug.Log($"  - ProgressText: {progressText != null}");
            Debug.Log($"  - CanvasGroup: {canvasGroup != null}");
            Debug.Log($"  - IsTracking: {IsTracking}");
            Debug.Log($"  - CurrentTarget: {currentTarget != null}");
        }
    }
} 