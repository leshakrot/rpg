using UnityEngine;
using TMPro;

namespace RPG.UI.KnockoutCountdownText
{
    /// <summary>
    /// Отображает всплывающую подсказку с текстом обратного отсчёта до возрождения компаньона.
    /// Аналогичен DamageText, но принимает произвольную строку вместо числового значения урона.
    /// </summary>
    public class KnockoutCountdownText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _countdownText = null;

        /// <summary>Уничтожает всплывающую подсказку. Вызывается по окончании анимации исчезновения.</summary>
        public void DestroyText()
        {
            Destroy(gameObject);
        }

        /// <summary>Задаёт отображаемый текст подсказки (например, "До возрождения 5 сек").</summary>
        public void SetText(string message)
        {
            _countdownText.text = message;
        }
    }
}
