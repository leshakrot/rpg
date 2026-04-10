using UnityEngine;

namespace RPG.Companions
{
    /// <summary>
    /// ScriptableObject для настройки компаньона
    /// Хранит параметры найма и поведения
    /// </summary>
    [CreateAssetMenu(fileName = "New Companion", menuName = "Companions/Companion Data", order = 0)]
    public class CompanionData : ScriptableObject
    {
        [Header("Основная информация")]
        [SerializeField] private string companionName = "Наемник";
        [SerializeField] private string companionID = ""; // Уникальный ID для сохранений
        
        [Header("Стоимость найма")]
        [SerializeField] private float hireCost = 100f;
        
        [Header("Боевые характеристики")]
        [SerializeField] private float followDistance = 3f;
        [SerializeField] private float combatRange = 10f;

        public string CompanionName => companionName;
        public string CompanionID => companionID;
        public float HireCost => hireCost;
        public float FollowDistance => followDistance;
        public float CombatRange => combatRange;

        private void OnValidate()
        {
            // Автоматически генерируем ID если он пустой
            if (string.IsNullOrEmpty(companionID))
            {
                companionID = System.Guid.NewGuid().ToString();
            }
        }
    }
}
