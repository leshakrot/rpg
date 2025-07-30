using UnityEngine;
using GameDevTV.Inventories;

namespace RPG.Harvesting
{
    public enum ResourceCategory { Wood, Ore, Herb }

    // Определение HarvestingToolType теперь ТОЛЬКО здесь.
    public enum HarvestingToolType
    {
        None = -1,     // Без инструмента, уникальное значение
        Axe = 0,       // Топор для деревьев
        Pickaxe = 1,   // Кирка для руды
        Sickle = 2,    // Серп для растений/трав
        Skinning = 3   // Нож для снятия шкур
    }

    [CreateAssetMenu(fileName = "New Harvestable Resource", menuName = "RPG/Harvesting/Harvestable Resource")]
    public class HarvestableResource : ScriptableObject
    {
        [Header("Основная информация")]
        [SerializeField] private string resourceName = "Ресурс";
        [SerializeField] private InventoryItem inventoryItem = null;
        [SerializeField] private int baseHarvestTime = 3; // базовое время добычи в секундах
        [SerializeField] private int resourceAmount = 10; // количество ресурса с одного источника

        [Header("Требования уровня")]
        [SerializeField] private int minimumLevel = 1;
        [SerializeField] private int harvestingSkillRequired = 0; // для будущего расширения

        [Header("Требования инструментов")]
        [SerializeField] private HarvestingToolType requiredToolType = HarvestingToolType.None;
        [SerializeField] private int resourceTier = 1; // Уровень ресурса (1-5 как в Albion)

        [Header("Категория ресурса")]
        [SerializeField] private ResourceCategory resourceCategory = ResourceCategory.Wood;
        public ResourceCategory ResourceCategory => resourceCategory;

        [Header("Опыт")]
        [SerializeField] private float experiencePerUnit = 1f; // Опыт за единицу добытого ресурса
        public float ExperiencePerUnit => experiencePerUnit;

        [Header("Визуальные эффекты")]
        [SerializeField] private Color progressBarColor = Color.green;
        [SerializeField] private AudioClip harvestSound = null;
        [SerializeField] private ParticleSystem harvestEffect = null;

        [Header("Анимации")]
        [SerializeField] private AnimationClip woodHarvestAnimation = null;
        [SerializeField] private AnimationClip oreHarvestAnimation = null;
        [SerializeField] private AnimationClip herbHarvestAnimation = null;
        [SerializeField] private AnimationClip noToolHarvestAnimation = null; // Анимация для сбора без инструмента (руками)

        public string ResourceName => resourceName;
        public InventoryItem InventoryItem => inventoryItem;
        public int BaseHarvestTime => baseHarvestTime;
        public int ResourceAmount => resourceAmount;
        public int MinimumLevel => minimumLevel;
        public int HarvestingSkillRequired => harvestingSkillRequired;
        public HarvestingToolType RequiredToolType => requiredToolType;
        public int ResourceTier => resourceTier;
        public Color ProgressBarColor => progressBarColor;
        public AudioClip HarvestSound => harvestSound;
        public ParticleSystem HarvestEffect => harvestEffect;

        public AnimationClip WoodHarvestAnimation => woodHarvestAnimation;
        public AnimationClip OreHarvestAnimation => oreHarvestAnimation;
        public AnimationClip HerbHarvestAnimation => herbHarvestAnimation;
        public AnimationClip NoToolHarvestAnimation => noToolHarvestAnimation; // Свойство для анимации без инструмента

        // Метод для вычисления времени добычи с учётом инструментов
        public float GetHarvestTime(int playerLevel, HarvestingTool tool = null)
        {
            // Базовое время добычи
            float finalTime = baseHarvestTime;

            // Бонус от уровня игрока (до 50% быстрее на высоких уровнях)
            float levelBonus = Mathf.Clamp(playerLevel - minimumLevel, 0, 20) * 0.025f;
            finalTime *= (1f - levelBonus);

            // Множитель от инструмента
            if (tool != null)
            {
                float toolMultiplier = tool.GetHarvestSpeedMultiplier(playerLevel);
                finalTime /= toolMultiplier; // Делим, потому что больший множитель = меньше времени
            }
            else if (requiredToolType != HarvestingToolType.None)
            {
                // Если нужен инструмент, но его нет - добыча невозможна
                return float.MaxValue;
            }
            else
            {
                // Добыча руками (без инструмента) для ресурсов 1 уровня
                finalTime *= 2f; // В 2 раза медленнее
            }

            return Mathf.Max(finalTime, 0.5f); // Минимум 0.5 секунды
        }

        // Перегрузка для обратной совместимости
        public float GetHarvestTime(int playerLevel)
        {
            return GetHarvestTime(playerLevel, null);
        }

        public bool CanHarvest(int playerLevel, GameDevTV.Inventories.Inventory inventory)
        {
            // Проверка уровня игрока
            if (playerLevel < minimumLevel)
                return false;

            // Проверка наличия подходящего инструмента
            return HarvestingToolChecker.CanHarvestResource(inventory, requiredToolType, resourceTier);
        }

        // Перегрузка для обратной совместимости
        public bool CanHarvest(int playerLevel)
        {
            return playerLevel >= minimumLevel;
        }

        // Получить лучший инструмент для этого ресурса из инвентаря
        public HarvestingTool GetBestToolFromInventory(GameDevTV.Inventories.Inventory inventory)
        {
            return HarvestingToolChecker.FindBestTool(inventory, requiredToolType, resourceTier);
        }

        // Получить описание требований для UI
        public string GetRequirementsDescription()
        {
            string desc = $"Уровень: {minimumLevel}";

            if (requiredToolType != HarvestingToolType.None)
            {
                string toolName = requiredToolType switch
                {
                    HarvestingToolType.Axe => "Топор",
                    HarvestingToolType.Pickaxe => "Кирка",
                    HarvestingToolType.Sickle => "Серп",
                    HarvestingToolType.Skinning => "Нож",
                    _ => "Инструмент"
                };
                desc += $"\nТребуется: {toolName} T{resourceTier}+";
            }
            else
            {
                desc += "\nМожно собирать руками";
            }

            return desc;
        }
    }
}