using UnityEngine;
using GameDevTV.Inventories;

namespace RPG.Harvesting
{
    // Определение HarvestingToolType удалено из этого файла.
    // Оно теперь должно быть определено ТОЛЬКО в HarvestableResource.cs

    [CreateAssetMenu(fileName = "New Harvesting Tool", menuName = "RPG/Harvesting/Harvesting Tool")]
    public class HarvestingTool : InventoryItem
    {
        [Header("Инструмент добычи")]
        [SerializeField] private HarvestingToolType toolType = HarvestingToolType.Axe;
        [SerializeField] private int toolTier = 1; // Уровень инструмента (1, 2, 3, 4...)
        [SerializeField] private float speedMultiplier = 1.0f; // Множитель скорости добычи
        [SerializeField] private int maxResourceTier = 1; // Максимальный уровень ресурсов, которые можно добывать

        [Header("Характеристики")]
        [SerializeField] private float durability = 100f; // Прочность (для будущего расширения)
        [SerializeField] private float efficiency = 1.0f; // Эффективность (шанс получить больше ресурсов)

        [Header("Визуальные эффекты")]
        [SerializeField] private AudioClip useSound = null;
        [SerializeField] private ParticleSystem useEffect = null;

        // Свойства для доступа
        public HarvestingToolType ToolType => toolType;
        public int ToolTier => toolTier;
        public float SpeedMultiplier => speedMultiplier;
        public int MaxResourceTier => maxResourceTier;
        public float Durability => durability;
        public float Efficiency => efficiency;
        public AudioClip UseSound => useSound;
        public ParticleSystem UseEffect => useEffect;

        // Метод для вычисления финальной скорости добычи
        public float GetHarvestSpeedMultiplier(int playerLevel = 1)
        {
            // Базовый множитель от инструмента
            float finalMultiplier = speedMultiplier;

            // Бонус от уровня инструмента (каждый уровень даёт +10% скорости)
            finalMultiplier += (toolTier - 1) * 0.1f;

            // Небольшой бонус от уровня игрока (каждые 10 уровней +5% скорости)
            finalMultiplier += (playerLevel / 10) * 0.05f;

            return finalMultiplier;
        }

        // Проверка, может ли инструмент добывать ресурс определённого уровня
        public bool CanHarvestResourceTier(int resourceTier)
        {
            return resourceTier <= maxResourceTier;
        }

        // Проверка совместимости с типом ресурса
        public bool IsCompatibleWith(HarvestingToolType requiredToolType)
        {
            // Если ресурс не требует инструмента, любой инструмент подходит
            if (requiredToolType == HarvestingToolType.None)
                return true;

            return toolType == requiredToolType;
        }

        // Получение описания для UI
        public string GetToolDescription()
        {
            string typeName = GetToolTypeName();
            return $"{typeName} T{toolTier}\n" +
                   $"Скорость: +{(speedMultiplier - 1f) * 100:F0}%\n" +
                   $"Макс. уровень ресурсов: {maxResourceTier}";
        }

        private string GetToolTypeName()
        {
            return toolType switch
            {
                HarvestingToolType.Axe => "Топор",
                HarvestingToolType.Pickaxe => "Кирка",
                HarvestingToolType.Sickle => "Серп",
                HarvestingToolType.Skinning => "Нож для снятия шкур",
                _ => "Инструмент"
            };
        }
    }
}