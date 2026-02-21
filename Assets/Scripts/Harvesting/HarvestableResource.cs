using UnityEngine;
using GameDevTV.Inventories;

namespace RPG.Harvesting
{
    public enum ResourceCategory { Wood, Ore, Herb }

    public enum HarvestingToolType
    {
        None = -1,
        Axe = 0,
        Pickaxe = 1,
        Sickle = 2,
        Skinning = 3
    }

    /// <summary>
    /// Описывает один возможный дополнительный предмет, который может выпасть при добыче.
    /// Предмет — любой InventoryItem (оружие, материал, зелье и т.д.).
    /// </summary>
    [System.Serializable]
    public class BonusDrop
    {
        [Tooltip("Любой InventoryItem: ресурс, оружие, зелье — что угодно")]
        public InventoryItem item = null;

        [Tooltip("Количество предметов при выпадении")]
        [Min(1)] public int amount = 1;

        [Tooltip("Шанс выпадения за каждую единицу добычи (0–1). 0.05 = 5%")]
        [Range(0f, 1f)] public float dropChance = 0.05f;
    }

    [CreateAssetMenu(fileName = "New Harvestable Resource", menuName = "RPG/Harvesting/Harvestable Resource")]
    public class HarvestableResource : ScriptableObject
    {
        [Header("Основная информация")]
        [SerializeField] private string resourceName = "Ресурс";

        [Tooltip("Основной предмет добычи — может быть любым InventoryItem (материал, оружие и т.п.)")]
        [SerializeField] private InventoryItem primaryItem = null;

        [Tooltip("Количество основного предмета за одну «тик» добычи")]
        [Min(1)] [SerializeField] private int primaryItemAmount = 1;

        [SerializeField] private int baseHarvestTime = 3;
        [SerializeField] private int resourceAmount = 10;

        [Header("Требования уровня")]
        [SerializeField] private int minimumLevel = 1;
        [SerializeField] private int harvestingSkillRequired = 0;

        [Header("Требования инструментов")]
        [SerializeField] private HarvestingToolType requiredToolType = HarvestingToolType.None;
        [SerializeField] private int resourceTier = 1;

        [Header("Категория ресурса")]
        [SerializeField] private ResourceCategory resourceCategory = ResourceCategory.Wood;
        public ResourceCategory ResourceCategory => resourceCategory;

        [Header("Опыт")]
        [SerializeField] private float experiencePerUnit = 1f;
        public float ExperiencePerUnit => experiencePerUnit;

        [Header("Дополнительные предметы (бонусный лут)")]
        [Tooltip("Каждый элемент — отдельный предмет с собственным шансом выпадения за тик добычи")]
        [SerializeField] private BonusDrop[] bonusDrops = new BonusDrop[0];
        public BonusDrop[] BonusDrops => bonusDrops;

        [Header("Визуальные эффекты")]
        [SerializeField] private Color progressBarColor = Color.green;
        [SerializeField] private AudioClip harvestSound = null;
        [SerializeField] private ParticleSystem harvestEffect = null;

        [Header("Анимации")]
        [SerializeField] private AnimationClip woodHarvestAnimation = null;
        [SerializeField] private AnimationClip oreHarvestAnimation = null;
        [SerializeField] private AnimationClip herbHarvestAnimation = null;
        [SerializeField] private AnimationClip noToolHarvestAnimation = null;

        // ── Публичные свойства ──────────────────────────────────────────────

        public string ResourceName => resourceName;

        /// <summary>Основной предмет добычи (любой InventoryItem).</summary>
        public InventoryItem PrimaryItem => primaryItem;

        /// <summary>Сколько основного предмета выдаётся за один тик.</summary>
        public int PrimaryItemAmount => primaryItemAmount;

        // Обратная совместимость: старый код мог обращаться к InventoryItem напрямую
        [System.Obsolete("Используйте PrimaryItem вместо InventoryItem")]
        public InventoryItem InventoryItem => primaryItem;

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
        public AnimationClip NoToolHarvestAnimation => noToolHarvestAnimation;

        // ── Логика ─────────────────────────────────────────────────────────

        public float GetHarvestTime(int playerLevel, HarvestingTool tool = null)
        {
            float finalTime = baseHarvestTime;
            float levelBonus = Mathf.Clamp(playerLevel - minimumLevel, 0, 20) * 0.025f;
            finalTime *= (1f - levelBonus);

            if (tool != null)
            {
                finalTime /= tool.GetHarvestSpeedMultiplier(playerLevel);
            }
            else if (requiredToolType != HarvestingToolType.None)
            {
                return float.MaxValue;
            }
            else
            {
                finalTime *= 2f;
            }

            return Mathf.Max(finalTime, 0.5f);
        }

        public float GetHarvestTime(int playerLevel) => GetHarvestTime(playerLevel, null);

        public bool CanHarvest(int playerLevel, GameDevTV.Inventories.Inventory inventory)
        {
            if (playerLevel < minimumLevel) return false;
            return HarvestingToolChecker.CanHarvestResource(inventory, requiredToolType, resourceTier);
        }

        public bool CanHarvest(int playerLevel) => playerLevel >= minimumLevel;

        public HarvestingTool GetBestToolFromInventory(GameDevTV.Inventories.Inventory inventory)
            => HarvestingToolChecker.FindBestTool(inventory, requiredToolType, resourceTier);

        public string GetRequirementsDescription()
        {
            string desc = $"Уровень: {minimumLevel}";

            if (requiredToolType != HarvestingToolType.None)
            {
                string toolName = requiredToolType switch
                {
                    HarvestingToolType.Axe     => "Топор",
                    HarvestingToolType.Pickaxe => "Кирка",
                    HarvestingToolType.Sickle  => "Серп",
                    HarvestingToolType.Skinning => "Нож",
                    _                          => "Инструмент"
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
