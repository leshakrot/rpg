using System.Collections.Generic;
using UnityEngine;

namespace RPG.Stats
{
    /// <summary>
    /// Класс для управления прогрессией и статистикой персонажей разных классов на разных уровнях.
    /// </summary>
    [CreateAssetMenu(fileName = "Progression", menuName = "RPG/ Stats/ New Progression", order = 0)]
    public class Progression : ScriptableObject
    {
        [Tooltip("Настройки прогрессии для различных классов персонажей")]
        [SerializeField] ProgressionCharacterClass[] _characterClasses = null;

        // Кэш данных для быстрого доступа
        private Dictionary<CharacterClass, Dictionary<Stat, float[]>> _lookupTable = null;

        /// <summary>
        /// Получает значение статистики для указанного класса персонажа и уровня.
        /// </summary>
        /// <param name="stat">Тип статистики</param>
        /// <param name="characterClass">Класс персонажа</param>
        /// <param name="level">Уровень персонажа (начиная с 1)</param>
        /// <returns>Значение статистики или 0, если данные отсутствуют</returns>
        public float GetStat(Stat stat, CharacterClass characterClass, int level)
        {
            // Гарантируем, что таблица поиска инициализирована
            BuildLookup();
            
            // Проверяем, существует ли запрашиваемый класс персонажа в таблице
            if (!_lookupTable.ContainsKey(characterClass))
            {
                Debug.LogWarning($"Класс персонажа {characterClass} не найден в таблице прогрессии");
                return 0;
            }
            
            // Проверяем, существует ли запрашиваемая статистика для данного класса
            if (!_lookupTable[characterClass].ContainsKey(stat))
            {
                return 0;
            }

            float[] levels = _lookupTable[characterClass][stat];
            
            // Проверяем, есть ли какие-либо данные для этой статистики
            if (levels.Length == 0)
            {
                return 0;
            }
            
            // Если запрашиваемый уровень выше максимального, возвращаем значение для максимального уровня
            if (levels.Length < level)
            {
                return levels[levels.Length - 1];
            }

            // Возвращаем значение для указанного уровня (с учетом того, что массив начинается с 0)
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
            BuildLookup();
            
            // Проверяем, существует ли запрашиваемый класс персонажа в таблице
            if (!_lookupTable.ContainsKey(characterClass))
            {
                Debug.LogWarning($"Класс персонажа {characterClass} не найден в таблице прогрессии");
                return 0;
            }
            
            // Проверяем, существует ли запрашиваемая статистика для данного класса
            if (!_lookupTable[characterClass].ContainsKey(stat))
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
            BuildLookup();
            
            if (!_lookupTable.ContainsKey(characterClass))
            {
                return false;
            }
            
            return _lookupTable[characterClass].ContainsKey(stat);
        }

        /// <summary>
        /// Получает все доступные статистики для указанного класса персонажа.
        /// </summary>
        /// <param name="characterClass">Класс персонажа</param>
        /// <returns>Список статистик или пустой список, если класс не найден</returns>
        public IEnumerable<Stat> GetAvailableStats(CharacterClass characterClass)
        {
            BuildLookup();
            
            if (!_lookupTable.ContainsKey(characterClass))
            {
                Debug.LogWarning($"Класс персонажа {characterClass} не найден в таблице прогрессии");
                return new List<Stat>();
            }
            
            return _lookupTable[characterClass].Keys;
        }

        /// <summary>
        /// Инициализирует таблицу поиска для быстрого доступа к данным.
        /// </summary>
        private void BuildLookup()
        {
            // Пропускаем инициализацию, если таблица уже построена
            if (_lookupTable != null) return;

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
                    System.Array.Copy(progressionStat.levels, levelsCopy, progressionStat.levels.Length);
                    
                    statLookupTable[progressionStat.stat] = levelsCopy;
                }

                _lookupTable[progressionClass.characterClass] = statLookupTable;
            }
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

        [System.Serializable]
        private class ProgressionCharacterClass
        {
            [Tooltip("Класс персонажа")]
            public CharacterClass characterClass;
            
            [Tooltip("Настройки статистики для данного класса")]
            public ProgressionStat[] stats;            
        }

        [System.Serializable]
        private class ProgressionStat
        {
            [Tooltip("Тип статистики")]
            public Stat stat;
            
            [Tooltip("Значения для каждого уровня (начиная с 1)")]
            public float[] levels;
        }
    }
}