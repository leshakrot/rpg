using RPG.Stats;
using TMPro;
using UnityEngine;

namespace RPG.Attributes
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Health _healthComponent = null;
        [SerializeField] private RectTransform _foreground = null;
        [SerializeField] private Canvas _rootCanvas = null;
        [SerializeField] private TextMeshProUGUI _level;

        // Дополнительные параметры для плавного изменения
        private float _currentHealthFraction = 1f; // Текущая длина полоски здоровья
        private float _targetHealthFraction = 1f; // Целевая длина полоски здоровья
        private float _smoothSpeed = 5f; // Скорость плавного изменения

        private void Start()
        {
            _level.text = _healthComponent.gameObject.GetComponent<BaseStats>().GetLevel().ToString();
        }

        private void Update()
        {
            // Обновляем целевое значение здоровья
            _targetHealthFraction = _healthComponent.GetFraction();

            // Плавно изменяем текущее значение к целевому
            _currentHealthFraction = Mathf.Lerp(_currentHealthFraction, _targetHealthFraction, Time.deltaTime * _smoothSpeed);

            // Если разница между текущим и целевым значением мала, фиксируем целевое значение
            if (Mathf.Abs(_currentHealthFraction - _targetHealthFraction) < 0.001f)
            {
                _currentHealthFraction = _targetHealthFraction;
            }

            // Управление видимостью холста
            if (Mathf.Approximately(_currentHealthFraction, 0) || Mathf.Approximately(_currentHealthFraction, 1))
            {
                _rootCanvas.enabled = false;
                return;
            }

            _rootCanvas.enabled = true;

            // Обновляем масштаб переднего плана
            _foreground.localScale = new Vector3(_currentHealthFraction, 1, 1);

            // Применяем прозрачность к части, которая "обрезается"
            ApplyForegroundTransparency();
        }

        private void ApplyForegroundTransparency()
        {
            // Получаем компонент Image для переднего плана
            var foregroundImage = _foreground.GetComponent<UnityEngine.UI.Image>();
            if (foregroundImage == null) return;

            // Вычисляем разницу между целевым и текущим здоровьем
            float delta = _currentHealthFraction - _targetHealthFraction;

            // Устанавливаем прозрачность для "обрезанной" части
            Color color = foregroundImage.color;
            color.a = Mathf.Clamp01(1 - delta); // Прозрачность зависит от разницы
            foregroundImage.color = color;
        }
    }
}