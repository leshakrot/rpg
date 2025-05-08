using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Stats
{
    /// <summary>
    /// Класс для управления прогрессией и статистикой персонажей разных классов на разных уровнях.
    /// Обеспечивает эффективную работу со StatusBar и другими системами статистики.
    /// </summary>
    [CreateAssetMenu(fileName = "Progression", menuName = "RPG/ Stats/ New Progression", order = 0)]
    public class Progression : ScriptableObject
    {
        [Tooltip("Настройки прогрессии для различных классов персонажей")]
        [SerializeField] private ProgressionCharacterClass[] _characterClasses = null;

        // Кэш данных для быстрого доступа
        private Dictionary<CharacterClass, Dictionary<Stat, float[]>> _lookupTable = null;

        // События для отслеживания изменений в прогрессии
        public event Action<CharacterClass, Stat> OnStatProgressionChanged;

        #region Публичные методы

        /// <summary>
        /// Получает значение статистики для указанного класса персонажа и уровня.
        /// </summary>
        /// <param name="stat">Тип статистики</param>
        /// <param name="characterClass">Класс персонажа</param>
        /// <param name="level">Уровень персонажа (начиная с 1)</param>
        /// <returns>Значение статистики или 0, если данные отсутствуют</returns>
        public float GetStat(Stat stat, CharacterClass characterClass, int level)
        {
            BuildLookupIfNeeded();
            
            if (!HasStatForClass(stat, characterClass))
            {
                return 0;
            }
            
            float[] levels = _lookupTable[characterClass][stat];
            
            if (levels.Length == 0)
            {
                return 0;
            }
            
            if (levels.Length < level)
            {
                return levels[levels.Length - 1];
            }

            return levels[level - 1];
        }

        /// <summary>
        /// Получает максимальное количество уровней для указанной статистики и класса персонажа.
        /// </summary>
        /// <param name="stat">Тип статистики</param>
        /// <param name="characterClass">Класс персонажа</param>
        /// <returns>Количество уровней или 0, если данные отсутствуют</returns>
        public int GetLevels(Stat stat, CharacterClass characterClass)
        {
            BuildLookupIfNeeded();
            
            if (!HasStatForClass(stat, characterClass))
            {
                return 0;
            }

            return _lookupTable[characterClass][stat].Length;
        }

        /// <summary>
        /// Проверяет, существует ли статистика для указанного класса персонажа.
        /// </summary>
        /// <param name="stat">Тип статистики</param>
        /// <param name="characterClass">Класс персонажа</param>
        /// <returns>true, если статистика существует</returns>
        public bool HasStat(Stat stat, CharacterClass characterClass)
        {
            BuildLookupIfNeeded();
            return HasStatForClass(stat, characterClass);
        }

        /// <summary>
        /// Получает все доступные статистики для указанного класса персонажа.
        /// </summary>
        /// <param name="characterClass">Класс персонажа</param>
        /// <returns>Список статистик или пустой список, если класс не найден</returns>
        public IEnumerable<Stat> GetAvailableStats(CharacterClass characterClass)
        {
            BuildLookupIfNeeded();
            
            if (!_lookupTable.ContainsKey(characterClass))
            {
                Debug.LogWarning($"Класс персонажа {characterClass} не найден в таблице прогрессии");
                return new List<Stat>();
            }
            
            return _lookupTable[characterClass].Keys;
        }

        /// <summary>
        /// Получает все доступные классы персонажей.
        /// </summary>
        /// <returns>Список классов персонажей</returns>
        public IEnumerable<CharacterClass> GetAvailableClasses()
        {
            BuildLookupIfNeeded();
            return _lookupTable.Keys;
        }

        /// <summary>
        /// Сбрасывает кэш данных, заставляя его перестроиться при следующем обращении.
        /// Полезно при изменении данных во время выполнения.
        /// </summary>
        public void ResetCache()
        {
            _lookupTable = null;
        }

        /// <summary>
        /// Принудительно обновляет кэш данных. Полезно вызывать после внесения изменений в данные.
        /// </summary>
        public void ForceUpdateCache()
        {
            ResetCache();
            BuildLookup();
        }

        /// <summary>
        /// Устанавливает новое значение для статистики указанного класса персонажа и уровня.
        /// </summary>
        /// <param name="stat">Тип статистики</param>
        /// <param name="characterClass">Класс персонажа</param>
        /// <param name="level">Уровень персонажа (начиная с 1)</param>
        /// <param name="value">Новое значение</param>
        /// <returns>true, если значение было успешно установлено</returns>
        public bool SetStat(Stat stat, CharacterClass characterClass, int level, float value)
        {
            BuildLookupIfNeeded();
            
            if (!HasStatForClass(stat, characterClass))
            {
                Debug.LogWarning($"Статистика {stat} для класса {characterClass} не найдена");
                return false;
            }
            
            float[] levels = _lookupTable[characterClass][stat];
            
            if (level < 1 || level > levels.Length)
            {
                Debug.LogWarning($"Уровень {level} выходит за пределы допустимого диапазона для {stat}");
                return false;
            }
            
            levels[level - 1] = value;
            
            // Оповещаем об изменении статистики
            OnStatProgressionChanged?.Invoke(characterClass, stat);
            
            return true;
        }

        /// <summary>
        /// Получает значения статистики для всех классов персонажей на указанном уровне.
        /// </summary>
        /// <param name="stat">Тип статистики</param>
        /// <param name="level">Уровень персонажа (начиная с 1)</param>
        /// <returns>Словарь со значениями для каждого класса</returns>
        public Dictionary<CharacterClass, float> GetStatForAllClasses(Stat stat, int level)
        {
            BuildLookupIfNeeded();
            
            Dictionary<CharacterClass, float> result = new Dictionary<CharacterClass, float>();
            
            foreach (CharacterClass characterClass in GetAvailableClasses())
            {
                if (HasStat(stat, characterClass))
                {
                    result[characterClass] = GetStat(stat, characterClass, level);
                }
            }
            
            return result;
        }

        #endregion

        #region Приватные методы

        /// <summary>
        /// Строит таблицу поиска, если она ещё не построена.
        /// </summary>
        private void BuildLookupIfNeeded()
        {
            if (_lookupTable == null)
            {
                BuildLookup();
            }
        }

        /// <summary>
        /// Инициализирует таблицу поиска для быстрого доступа к данным.
        /// </summary>
        private void BuildLookup()
        {
            _lookupTable = new Dictionary<CharacterClass, Dictionary<Stat, float[]>>();

            foreach (ProgressionCharacterClass progressionClass in _characterClasses)
            {
                if (progressionClass == null) continue; // Защита от null

                var statLookupTable = new Dictionary<Stat, float[]>();

                foreach (ProgressionStat progressionStat in progressionClass.stats)
                {
                    if (progressionStat == null || progressionStat.levels == null) continue; // Защита от null
                    
                    // Создаем копию массива для предотвращения случайных изменений извне
                    float[] levelsCopy = new float[progressionStat.levels.Length];
                    Array.Copy(progressionStat.levels, levelsCopy, progressionStat.levels.Length);
                    
                    statLookupTable[progressionStat.stat] = levelsCopy;
                }

                _lookupTable[progressionClass.characterClass] = statLookupTable;
            }
        }

        /// <summary>
        /// Проверяет наличие статистики для указанного класса персонажа.
        /// </summary>
        private bool HasStatForClass(Stat stat, CharacterClass characterClass)
        {
            if (!_lookupTable.ContainsKey(characterClass))
            {
                return false;
            }
            
            return _lookupTable[characterClass].ContainsKey(stat);
        }

        #endregion

        #region Вложенные классы

        [Serializable]
        private class ProgressionCharacterClass
        {
            [Tooltip("Класс персонажа")]
            public CharacterClass characterClass;
            
            [Tooltip("Настройки статистики для данного класса")]
            public ProgressionStat[] stats;            
        }

        [Serializable]
        private class ProgressionStat
        {
            [Tooltip("Тип статистики")]
            public Stat stat;
            
            [Tooltip("Значения для каждого уровня (начиная с 1)")]
            public float[] levels;
        }

        #endregion
    }
}