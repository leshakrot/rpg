using GameDevTV.Inventories;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Inventories
{
    [CreateAssetMenu(menuName = ("RPG/Inventory/DropLibrary"))]
    public class DropLibrary : ScriptableObject
    {
        [SerializeField] private DropLibraryConfig[] dropConfigs;
        
        [Tooltip("Настройки дропа для всех уровней")]
        [SerializeField] private GlobalDropConfig globalConfig;
        
        [System.Serializable]
        public class GlobalDropConfig
        {
            [Tooltip("Шанс того, что что-то выпадет (в процентах)")]
            [Range(0, 100)]
            public float dropChancePercentage = 50f;
            
            [Tooltip("Минимальное количество предметов, которое может выпасть")]
            public int minDrops = 1;
            
            [Tooltip("Максимальное количество предметов, которое может выпасть")]
            public int maxDrops = 3;
            
            [Tooltip("Использовать индивидуальные настройки шанса дропа для каждого уровня")]
            public bool usePerLevelDropChance = false;
            
            [Tooltip("Шанс дропа в зависимости от уровня (в процентах)")]
            public LevelRange<float>[] dropChancePerLevel;
            
            [Tooltip("Минимальное количество предметов в зависимости от уровня")]
            public LevelRange<int>[] minDropsPerLevel;
            
            [Tooltip("Максимальное количество предметов в зависимости от уровня")]
            public LevelRange<int>[] maxDropsPerLevel;
        }
        
        [System.Serializable]
        public class LevelRange<T>
        {
            [Tooltip("Минимальный уровень для этого диапазона")]
            public int minLevel = 1;
            
            [Tooltip("Максимальный уровень для этого диапазона")]
            public int maxLevel = 5;
            
            [Tooltip("Значение для этого диапазона уровней")]
            public T value;
        }
        
        [System.Serializable]
        public class DropLibraryConfig
        {
            [Tooltip("Предмет, который может выпасть")]
            public InventoryItem item;
            
            [Tooltip("Относительный шанс выпадения предмета")]
            [Min(0.01f)]
            public float relativeChance = 1f;
            
            [Tooltip("Использовать различные шансы в зависимости от уровня")]
            public bool usePerLevelChance = false;
            
            [Tooltip("Относительный шанс выпадения в зависимости от уровня")]
            public LevelRange<float>[] relativeChancePerLevel;
            
            [Tooltip("Минимальное количество при выпадении (для стакуемых предметов)")]
            [Min(1)]
            public int minNumber = 1;
            
            [Tooltip("Использовать различные минимальные количества в зависимости от уровня")]
            public bool usePerLevelMinNumber = false;
            
            [Tooltip("Минимальное количество предметов в зависимости от уровня")]
            public LevelRange<int>[] minNumberPerLevel;
            
            [Tooltip("Максимальное количество при выпадении (для стакуемых предметов)")]
            [Min(1)]
            public int maxNumber = 1;
            
            [Tooltip("Использовать различные максимальные количества в зависимости от уровня")]
            public bool usePerLevelMaxNumber = false;
            
            [Tooltip("Максимальное количество предметов в зависимости от уровня")]
            public LevelRange<int>[] maxNumberPerLevel;
            
            public int GetRandomNumber(int level)
            {
                if (!item.IsStackable())
                {
                    return 1;
                }
                
                int min = usePerLevelMinNumber 
                    ? GetValueForLevel(minNumberPerLevel, minNumber, level) 
                    : minNumber;
                    
                int max = usePerLevelMaxNumber 
                    ? GetValueForLevel(maxNumberPerLevel, maxNumber, level) 
                    : maxNumber;
                
                return Random.Range(min, max + 1);
            }
            
            public float GetRelativeChance(int level)
            {
                if (!usePerLevelChance) return relativeChance;
                
                return GetValueForLevel(relativeChancePerLevel, relativeChance, level);
            }
        }
        
        public struct Dropped
        {
            public InventoryItem item;
            public int number;
        }
        
        public IEnumerable<Dropped> GetRandomDrops(int level)
        {
            if (dropConfigs == null || dropConfigs.Length == 0)
            {
                Debug.LogWarning($"[DropLibrary] Не настроены предметы для выпадения в {name}");
                yield break;
            }
            
            if (!ShouldRandomDrop(level))
            {
                yield break;
            }
            
            int numberOfDrops = GetRandomNumberOfDrops(level);
            for(int i = 0; i < numberOfDrops; i++)
            {
                DropLibraryConfig drop = SelectRandomItem(level);
                if (drop == null) continue;
                
                Dropped result = new Dropped
                {
                    item = drop.item,
                    number = drop.GetRandomNumber(level)
                };
                
                yield return result;
            }
        }
        
        private bool ShouldRandomDrop(int level)
        {
            float dropChance = globalConfig.usePerLevelDropChance 
                ? GetValueForLevel(globalConfig.dropChancePerLevel, globalConfig.dropChancePercentage, level)
                : globalConfig.dropChancePercentage;
                
            return Random.Range(0, 100) < dropChance;
        }
        
        private int GetRandomNumberOfDrops(int level)
        {
            int min = globalConfig.usePerLevelDropChance 
                ? GetValueForLevel(globalConfig.minDropsPerLevel, globalConfig.minDrops, level)
                : globalConfig.minDrops;
                
            int max = globalConfig.usePerLevelDropChance 
                ? GetValueForLevel(globalConfig.maxDropsPerLevel, globalConfig.maxDrops, level)
                : globalConfig.maxDrops;
                
            return Random.Range(min, max + 1);
        }
        
        private DropLibraryConfig SelectRandomItem(int level)
        {
            float totalChance = GetTotalChance(level);
            float randomRoll = Random.Range(0, totalChance);
            float chanceTotal = 0;
            
            foreach(var drop in dropConfigs)
            {
                chanceTotal += drop.GetRelativeChance(level);
                if(chanceTotal > randomRoll)
                {
                    return drop;
                }
            }
            
            // Если у нас почему-то не выбрался предмет (из-за округления),
            // берем первый в списке (это не должно происходить)
            if (dropConfigs.Length > 0)
            {
                Debug.LogWarning($"[DropLibrary] Не удалось выбрать предмет по шансу, возвращаем первый в списке");
                return dropConfigs[0];
            }
            
            return null;
        }
        
        private float GetTotalChance(int level)
        {
            float total = 0;
            foreach(var drop in dropConfigs)
            {
                total += drop.GetRelativeChance(level);
            }
            return total;
        }
        
        private static T GetValueForLevel<T>(LevelRange<T>[] ranges, T defaultValue, int level)
        {
            if (ranges == null || ranges.Length == 0)
            {
                return defaultValue;
            }
            
            foreach (var range in ranges)
            {
                if (level >= range.minLevel && level <= range.maxLevel)
                {
                    return range.value;
                }
            }
            
            // Если не нашли подходящий диапазон, ищем максимальный
            LevelRange<T> highestRange = null;
            
            foreach (var range in ranges)
            {
                if (highestRange == null || range.maxLevel > highestRange.maxLevel)
                {
                    highestRange = range;
                }
            }
            
            // Если уровень выше максимального в диапазонах, берем значение из
            // диапазона с самым высоким уровнем
            if (highestRange != null && level > highestRange.maxLevel)
            {
                return highestRange.value;
            }
            
            return defaultValue;
        }
        
        #if UNITY_EDITOR
        // Проверка на перекрытие диапазонов уровней (для редактора)
        private void OnValidate()
        {
            ValidateRanges(globalConfig.dropChancePerLevel);
            ValidateRanges(globalConfig.minDropsPerLevel);
            ValidateRanges(globalConfig.maxDropsPerLevel);
            
            if (dropConfigs != null)
            {
                foreach (var config in dropConfigs)
                {
                    ValidateRanges(config.relativeChancePerLevel);
                    ValidateRanges(config.minNumberPerLevel);
                    ValidateRanges(config.maxNumberPerLevel);
                    
                    // Проверка, что максимальное количество не меньше минимального
                    if (config.maxNumber < config.minNumber)
                    {
                        config.maxNumber = config.minNumber;
                    }
                }
            }
            
            // Проверка, что максимальное количество дропов не меньше минимального
            if (globalConfig.maxDrops < globalConfig.minDrops)
            {
                globalConfig.maxDrops = globalConfig.minDrops;
            }
        }
        
        private void ValidateRanges<T>(LevelRange<T>[] ranges)
        {
            if (ranges == null) return;
            
            for (int i = 0; i < ranges.Length; i++)
            {
                // Убедимся, что минимальный уровень не больше максимального
                if (ranges[i].minLevel > ranges[i].maxLevel)
                {
                    ranges[i].minLevel = ranges[i].maxLevel;
                }
                
                // Убедимся, что минимальный уровень не меньше 1
                if (ranges[i].minLevel < 1)
                {
                    ranges[i].minLevel = 1;
                }
            }
        }
        #endif
    }
}
