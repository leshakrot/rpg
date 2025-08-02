using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace RPG.Harvesting
{
    public class HarvestBar : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private RectTransform foreground = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private Text titleText = null;
        [SerializeField] private Text progressText = null;
        [SerializeField] private Image foregroundImage = null;

        [Header("Settings")]
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private Color processingColor = new Color(0.2f, 0.6f, 1f); // Голубой цвет для крафта
        [SerializeField] private Color harvestingColor = new Color(0.2f, 0.8f, 0.2f); // Зеленый для добычи

        private Coroutine activeProcessCoroutine = null;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (foreground != null && foregroundImage == null) foregroundImage = foreground.GetComponent<Image>();
            canvasGroup.alpha = 0; // Скрыт по умолчанию
        }

        /// <summary>
        /// Запускает отображение для процесса добычи (убывающий).
        /// </summary>
        public void StartHarvesting(IHarvestable harvestable)
        {
            // Останавливаем любой предыдущий процесс
            if (activeProcessCoroutine != null) StopCoroutine(activeProcessCoroutine);

            // Запускаем новую корутину для отслеживания добычи
            activeProcessCoroutine = StartCoroutine(TrackHarvestable(harvestable));
        }

        /// <summary>
        /// Запускает отображение для процесса переработки (возрастающий).
        /// </summary>
        public void StartProcessing(string title, float duration)
        {
            if (activeProcessCoroutine != null) StopCoroutine(activeProcessCoroutine);
            activeProcessCoroutine = StartCoroutine(TrackTimedProcess(title, duration));
        }

        /// <summary>
        /// Немедленно останавливает отображение и скрывает бар.
        /// </summary>
        public void Stop()
        {
            if (activeProcessCoroutine != null)
            {
                StopCoroutine(activeProcessCoroutine);
                activeProcessCoroutine = null;
            }
            StartCoroutine(FadeOut());
        }

        // Корутина для отслеживания добычи (убывающий прогресс)
        private IEnumerator TrackHarvestable(IHarvestable harvestable)
        {
            // Настройка и отображение
            SetupUI(harvestable.GetResource().ResourceName, harvestingColor);
            yield return StartCoroutine(FadeIn());

            float currentProgress = 1f; // Начинаем с полного

            // Лямбда-функция для обновления прогресса по событию
            System.Action<float> progressUpdater = (progress) => { currentProgress = progress; };
            harvestable.OnHarvestProgress += progressUpdater;

            // Цикл обновления, пока корутина активна
            while (true)
            {
                UpdateBar(currentProgress);
                yield return null;
            }

            // Отписка (хотя до сюда код не дойдет, т.к. корутину остановят извне)
            // harvestable.OnHarvestProgress -= progressUpdater;
        }

        // Корутина для отслеживания переработки (возрастающий прогресс)
        private IEnumerator TrackTimedProcess(string title, float duration)
        {
            SetupUI(title, processingColor);
            yield return StartCoroutine(FadeIn());

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                UpdateBar(timer / duration);
                yield return null;
            }

            // Процесс завершен, скрываем бар
            yield return StartCoroutine(FadeOut());
            activeProcessCoroutine = null;
        }

        private void SetupUI(string title, Color barColor)
        {
            if (titleText != null) titleText.text = title;
            if (foregroundImage != null) foregroundImage.color = barColor;
        }

        private void UpdateBar(float targetFraction)
        {
            float newX = Mathf.Lerp(foreground.anchorMax.x, targetFraction, Time.deltaTime * smoothSpeed);
            foreground.anchorMax = new Vector2(newX, 1f);
            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(newX * 100)}%";
            }
        }

        private IEnumerator FadeIn()
        {
            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += Time.deltaTime * 5f;
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime * 5f;
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
    }
}