using System.Collections.Generic;
using UnityEngine;

namespace RPG.Stats
{
    /// <summary>
    /// Вспомогательный класс для удобного управления статистиками персонажа
    /// </summary>
    [System.Serializable]
    public class StatCollection
    {
        private Dictionary<Stat, float> _statValues = new Dictionary<Stat, float>();
        
        /// <summary>
        /// Добавить или обновить значение статистики
        /// </summary>
        public void SetStat(Stat stat, float value)
        {
            _statValues[stat] = value;
        }
        
        /// <summary>
        /// Получить значение статистики
        /// </summary>
        public float GetStat(Stat stat)
        {
            if (_statValues.ContainsKey(stat))
            {
                return _statValues[stat];
            }
            return 0f;
        }
        
        /// <summary>
        /// Проверить наличие статистики в коллекции
        /// </summary>
        public bool HasStat(Stat stat)
        {
            return _statValues.ContainsKey(stat);
        }
        
        /// <summary>
        /// Добавить модификатор к существующему значению статистики
        /// </summary>
        public void AddModifier(Stat stat, float modifier)
        {
            if (_statValues.ContainsKey(stat))
            {
                _statValues[stat] += modifier;
            }
            else
            {
                _statValues[stat] = modifier;
            }
        }
        
        /// <summary>
        /// Получить все статистики в коллекции
        /// </summary>
        public IEnumerable<Stat> GetAllStats()
        {
            return _statValues.Keys;
        }
        
        /// <summary>
        /// Загрузить статистики из Progression для указанного класса и уровня
        /// </summary>
        public void LoadFromProgression(Progression progression, CharacterClass characterClass, int level)
        {
            _statValues.Clear();
            
            foreach (Stat stat in System.Enum.GetValues(typeof(Stat)))
            {
                if (progression.HasStat(stat, characterClass))
                {
                    float value = progression.GetStat(stat, characterClass, level);
                    _statValues[stat] = value;
                }
            }
        }
        
        /// <summary>
        /// Объединить несколько коллекций статистик
        /// </summary>
        public static StatCollection Combine(params StatCollection[] collections)
        {
            StatCollection result = new StatCollection();
            
            foreach (StatCollection collection in collections)
            {
                foreach (Stat stat in collection.GetAllStats())
                {
                    result.AddModifier(stat, collection.GetStat(stat));
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Создать копию коллекции
        /// </summary>
        public StatCollection Clone()
        {
            StatCollection clone = new StatCollection();
            
            foreach (var pair in _statValues)
            {
                clone._statValues[pair.Key] = pair.Value;
            }
            
            return clone;
        }
    }
} 