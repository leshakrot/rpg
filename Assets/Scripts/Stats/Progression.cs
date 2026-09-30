using System.Collections.Generic;
using UnityEngine;

namespace RPG.Stats
{
    /// <summary>
    /// Таблица прогрессии: (класс персонажа, характеристика) → значение на каждом уровне.
    /// Данные хранятся как кривые (см. ProgressionEntry), в рантайме один раз "запекаются"
    /// в массивы, поэтому GetStat — это два обращения к словарю и индекс в массиве, без аллокаций.
    /// </summary>
    [CreateAssetMenu(fileName = "Progression", menuName = "RPG/Stats/Progression", order = 0)]
    public class Progression : ScriptableObject
    {
        [Tooltip("Максимальный уровень. Кривые start → end растягиваются на этот диапазон.")]
        [SerializeField, Min(1)] private int maxLevel = 18;

        // Имя поля не менять: под ним лежат данные в существующем Progression.asset.
        [SerializeField] private ProgressionEntry[] progressionData = new ProgressionEntry[0];

        private static readonly Stat[] NoStats = new Stat[0];

        private Dictionary<int, float[]> curves;
        private Dictionary<CharacterClass, Stat[]> statsByClass;

        public int MaxLevel => maxLevel;

        #region Публичный API

        /// <summary>Значение характеристики на уровне. Уровень зажимается в [1, длина кривой]. Нет кривой — 0.</summary>
        public float GetStat(Stat stat, CharacterClass characterClass, int level)
        {
            TryGetStat(stat, characterClass, level, out float value);
            return value;
        }

        public bool TryGetStat(Stat stat, CharacterClass characterClass, int level, out float value)
        {
            Build();
            if (!curves.TryGetValue(Key(characterClass, stat), out float[] values))
            {
                value = 0f;
                return false;
            }

            value = values[Mathf.Clamp(level, 1, values.Length) - 1];
            return true;
        }

        /// <summary>Сколько уровней описано для этой характеристики (0, если кривой нет).</summary>
        public int GetLevels(Stat stat, CharacterClass characterClass)
        {
            Build();
            return curves.TryGetValue(Key(characterClass, stat), out float[] values) ? values.Length : 0;
        }

        public bool HasStat(Stat stat, CharacterClass characterClass)
        {
            Build();
            return curves.ContainsKey(Key(characterClass, stat));
        }

        public IReadOnlyList<Stat> GetAvailableStats(CharacterClass characterClass)
        {
            Build();
            return statsByClass.TryGetValue(characterClass, out Stat[] stats) ? stats : NoStats;
        }

        public IEnumerable<CharacterClass> GetAvailableClasses()
        {
            Build();
            return statsByClass.Keys;
        }

        public Dictionary<CharacterClass, float> GetStatForAllClasses(Stat stat, int level)
        {
            Build();
            var result = new Dictionary<CharacterClass, float>();
            foreach (CharacterClass characterClass in statsByClass.Keys)
            {
                if (TryGetStat(stat, characterClass, level, out float value))
                    result[characterClass] = value;
            }
            return result;
        }

        /// <summary>Сбросить кэш (пересоберётся при следующем обращении).</summary>
        public void ResetCache()
        {
            curves = null;
            statsByClass = null;
        }

        #endregion

        #region Кэш

        private void OnEnable() => ResetCache();
        private void OnValidate() => ResetCache();

        private static int Key(CharacterClass characterClass, Stat stat)
        {
            return ((int)characterClass << 16) | ((int)stat & 0xFFFF);
        }

        private void Build()
        {
            if (curves != null) return;

            curves = new Dictionary<int, float[]>();
            var classStats = new Dictionary<CharacterClass, List<Stat>>();

            if (progressionData != null)
            {
                foreach (ProgressionEntry entry in progressionData)
                {
                    if (entry == null) continue;

                    float[] baked = entry.Bake(maxLevel);
                    if (baked.Length == 0) continue;

                    int key = Key(entry.characterClass, entry.stat);
                    if (curves.ContainsKey(key))
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.LogWarning($"[Progression] Дубликат {entry.characterClass}/{entry.stat} в '{name}': используется последняя запись.", this);
#endif
                        curves[key] = baked;
                        continue;
                    }

                    curves[key] = baked;

                    if (!classStats.TryGetValue(entry.characterClass, out List<Stat> list))
                        classStats[entry.characterClass] = list = new List<Stat>();
                    list.Add(entry.stat);
                }
            }

            statsByClass = new Dictionary<CharacterClass, Stat[]>(classStats.Count);
            foreach (var pair in classStats)
                statsByClass[pair.Key] = pair.Value.ToArray();
        }

        #endregion
    }
}
