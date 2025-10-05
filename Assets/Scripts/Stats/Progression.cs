using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Stats
{
    /// <summary>
    /// Система прогрессии персонажей - унифицированная, эффективная и расширяемая.
    /// Обеспечивает быстрый доступ к статистикам персонажей на разных уровнях.
    /// </summary>
    [CreateAssetMenu(fileName = "Progression", menuName = "RPG/ Stats/ New Progression", order = 0)]
    public class Progression : ScriptableObject
    {
        private const int MAX_LEVEL = 100;
        private const int DEFAULT_LEVEL = 1;
        
        [SerializeField] private StatProgressionEntry[] progressionData = new StatProgressionEntry[0];
        
        private Dictionary<ProgressionKey, float[]> statCache;
        private Dictionary<CharacterClass, Stat[]> classStatsCache;
        private Dictionary<Stat, CharacterClass[]> statClassesCache;
        private bool isInitialized = false;

        public event Action<CharacterClass, Stat> OnStatProgressionChanged;

        #region Публичные методы

        /// <summary>
        /// Получает значение статистики для указанного класса персонажа и уровня.
        /// </summary>
        public float GetStat(Stat stat, CharacterClass characterClass, int level)
        {
            Initialize();
            
            var key = new ProgressionKey(characterClass, stat);
            
            if (!statCache.TryGetValue(key, out float[] values))
                return 0f;
                
            int clampedLevel = Mathf.Clamp(level, 1, values.Length);
            return values[clampedLevel - 1];
        }

        /// <summary>
        /// Получает максимальное количество уровней для указанной статистики и класса персонажа.
        /// </summary>
        public int GetLevels(Stat stat, CharacterClass characterClass)
        {
            Initialize();
            
            var key = new ProgressionKey(characterClass, stat);
            return statCache.TryGetValue(key, out float[] values) ? values.Length : 0;
        }

        /// <summary>
        /// Проверяет, существует ли статистика для указанного класса персонажа.
        /// </summary>
        public bool HasStat(Stat stat, CharacterClass characterClass)
        {
            Initialize();
            return statCache.ContainsKey(new ProgressionKey(characterClass, stat));
        }

        /// <summary>
        /// Получает все доступные статистики для указанного класса персонажа.
        /// </summary>
        public IEnumerable<Stat> GetAvailableStats(CharacterClass characterClass)
        {
            Initialize();
            
            if (classStatsCache.TryGetValue(characterClass, out Stat[] stats))
                return stats;
                
            return new Stat[0];
        }

        /// <summary>
        /// Получает все доступные классы персонажей.
        /// </summary>
        public IEnumerable<CharacterClass> GetAvailableClasses()
        {
            Initialize();
            return classStatsCache.Keys;
        }

        /// <summary>
        /// Сбрасывает кэш данных, заставляя его перестроиться при следующем обращении.
        /// </summary>
        public void ResetCache()
        {
            isInitialized = false;
            statCache = null;
            classStatsCache = null;
            statClassesCache = null;
        }

        /// <summary>
        /// Принудительно обновляет кэш данных.
        /// </summary>
        public void ForceUpdateCache()
        {
            ResetCache();
            Initialize();
        }

        /// <summary>
        /// Устанавливает новое значение для статистики указанного класса персонажа и уровня.
        /// </summary>
        public bool SetStat(Stat stat, CharacterClass characterClass, int level, float value)
        {
            Initialize();
            
            var key = new ProgressionKey(characterClass, stat);
            
            if (!statCache.TryGetValue(key, out float[] values))
                return false;
                
            if (level < 1 || level > values.Length)
                return false;
                
            values[level - 1] = value;
            OnStatProgressionChanged?.Invoke(characterClass, stat);
            return true;
        }

        /// <summary>
        /// Получает значения статистики для всех классов персонажей на указанном уровне.
        /// </summary>
        public Dictionary<CharacterClass, float> GetStatForAllClasses(Stat stat, int level)
        {
            Initialize();
            
            var result = new Dictionary<CharacterClass, float>();
            
            if (!statClassesCache.TryGetValue(stat, out CharacterClass[] classes))
                return result;
                
            foreach (var characterClass in classes)
            {
                result[characterClass] = GetStat(stat, characterClass, level);
            }
            
            return result;
        }

        #endregion

        #region Приватные методы

        private void Initialize()
        {
            if (isInitialized && statCache != null)
                return;
                
            BuildCaches();
            isInitialized = true;
        }

        private void BuildCaches()
        {
            statCache = new Dictionary<ProgressionKey, float[]>();
            var tempClassStats = new Dictionary<CharacterClass, List<Stat>>();
            var tempStatClasses = new Dictionary<Stat, List<CharacterClass>>();
            
            foreach (var entry in progressionData)
            {
                if (entry?.values == null || entry.values.Length == 0)
                    continue;
                    
                var key = new ProgressionKey(entry.characterClass, entry.stat);
                var values = new float[entry.values.Length];
                Array.Copy(entry.values, values, entry.values.Length);
                
                statCache[key] = values;
                
                if (!tempClassStats.ContainsKey(entry.characterClass))
                    tempClassStats[entry.characterClass] = new List<Stat>();
                tempClassStats[entry.characterClass].Add(entry.stat);
                
                if (!tempStatClasses.ContainsKey(entry.stat))
                    tempStatClasses[entry.stat] = new List<CharacterClass>();
                tempStatClasses[entry.stat].Add(entry.characterClass);
            }
            
            classStatsCache = new Dictionary<CharacterClass, Stat[]>();
            foreach (var kvp in tempClassStats)
            {
                classStatsCache[kvp.Key] = kvp.Value.ToArray();
            }
            
            statClassesCache = new Dictionary<Stat, CharacterClass[]>();
            foreach (var kvp in tempStatClasses)
            {
                statClassesCache[kvp.Key] = kvp.Value.ToArray();
            }
        }

        #endregion

        #region Структуры данных

        [Serializable]
        private struct ProgressionKey : IEquatable<ProgressionKey>
        {
            public CharacterClass characterClass;
            public Stat stat;
            
            public ProgressionKey(CharacterClass characterClass, Stat stat)
            {
                this.characterClass = characterClass;
                this.stat = stat;
            }
            
            public bool Equals(ProgressionKey other)
            {
                return characterClass == other.characterClass && stat == other.stat;
            }
            
            public override bool Equals(object obj)
            {
                return obj is ProgressionKey other && Equals(other);
            }
            
            public override int GetHashCode()
            {
                return ((int)characterClass << 16) | (int)stat;
            }
        }

        [Serializable]
        private class StatProgressionEntry
        {
            [Tooltip("Класс персонажа")]
            public CharacterClass characterClass;
            
            [Tooltip("Тип статистики")]
            public Stat stat;
            
            [Tooltip("Значения для каждого уровня")]
            public float[] values = new float[0];
        }

        #endregion
    }
}