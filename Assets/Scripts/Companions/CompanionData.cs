using UnityEngine;

namespace RPG.Companions
{
    /// <summary>
    /// ScriptableObject с настройками компаньона.
    /// Создаётся через Assets → Create → Companions → Companion Data.
    /// </summary>
    [CreateAssetMenu(fileName = "New Companion", menuName = "Companions/Companion Data", order = 0)]
    public class CompanionData : ScriptableObject
    {
        [Header("Основная информация")]
        [SerializeField] private string companionName = "Наёмник";
        [SerializeField] private string companionID   = "";

        [Header("Стоимость найма")]
        [SerializeField] private float hireCost = 100f;

        [Header("Поведение")]
        [SerializeField] private float followDistance = 3f;
        [SerializeField] private float combatRange    = 10f;

        public string CompanionName   => companionName;
        public string CompanionID     => companionID;
        public float  HireCost        => hireCost;
        public float  FollowDistance  => followDistance;
        public float  CombatRange     => combatRange;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(companionID))
                companionID = System.Guid.NewGuid().ToString();
        }
    }
}
