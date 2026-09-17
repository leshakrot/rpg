using UnityEngine;

namespace RPG.UI.KnockoutCountdownText
{
    /// <summary>
    /// Спавнит всплывающие подсказки с обратным отсчётом до возрождения компаньона.
    /// Аналогичен DamageTextSpawner, но показывает произвольный текст вместо числа урона.
    /// </summary>
    public class KnockoutCountdownSpawner : MonoBehaviour
    {
        [SerializeField] private KnockoutCountdownText _knockoutCountdownTextPrefab = null;

        /// <summary>Создаёт всплывающую подсказку с указанным текстом над компаньоном.</summary>
        public void Spawn(string message)
        {
            KnockoutCountdownText instance = Instantiate<KnockoutCountdownText>(_knockoutCountdownTextPrefab, transform);
            instance.SetText(message);
        }
    }
}
