using UnityEngine;
using UnityEngine.UI;

namespace RPG.UI
{
    public class StatusBar : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] protected RectTransform foreground = null;
        [SerializeField] protected Text valueText = null;
        [SerializeField] protected Text titleText = null;
        
        [Header("Settings")]
        [SerializeField] protected string barTitle = "Status";
        [SerializeField] protected Color barColor = Color.white;
        [SerializeField] protected float smoothSpeed = 5f;
        [SerializeField] protected bool hideWhenFull = false;
        [SerializeField] protected bool hideWhenEmpty = false;
        
        // Параметры для плавного изменения
        protected float currentFraction = 1f;
        protected float targetFraction = 1f;
        
        protected virtual void Awake()
        {
            // Настройка цвета полосы
            if (foreground != null)
            {
                Image foregroundImage = foreground.GetComponent<Image>();
                if (foregroundImage != null)
                {
                    foregroundImage.color = barColor;
                }
            }
            
            // Настройка заголовка
            if (titleText != null)
            {
                titleText.text = barTitle;
            }
        }
        
        protected virtual void Update()
        {
            // Плавное изменение текущей фракции до целевой
            currentFraction = Mathf.Lerp(currentFraction, targetFraction, Time.deltaTime * smoothSpeed);
            
            // Если разница очень мала, просто устанавливаем равенство
            if (Mathf.Abs(currentFraction - targetFraction) < 0.001f)
            {
                currentFraction = targetFraction;
            }
            
            // Скрываем полосу при необходимости
            if ((hideWhenFull && Mathf.Approximately(currentFraction, 1)) || 
                (hideWhenEmpty && Mathf.Approximately(currentFraction, 0)))
            {
                gameObject.SetActive(false);
                return;
            }
            
            gameObject.SetActive(true);
            
            // Обновляем масштаб заполнения полосы как в оригинальном HealthBar
            if (foreground != null)
            {
                foreground.localScale = new Vector3(currentFraction, 1, 1);
            }
        }
        
        // Метод для установки значения заполнения полосы (0-1)
        public virtual void SetFraction(float fraction)
        {
            targetFraction = Mathf.Clamp01(fraction);
        }
        
        // Метод для установки текста значения
        public virtual void SetValueText(string text)
        {
            if (valueText != null)
            {
                valueText.text = text;
            }
        }
    }
} 