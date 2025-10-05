using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RPG.Stats
{
    /// <summary>
    /// Вспомогательный класс для работы с прогрессиями
    /// </summary>
    public static class ProgressionUtility
    {
        // Стандартное количество врагов в группе для расчета баланса
        private static readonly float AverageEnemiesPerFight = 3f;
        
        // Общий множитель для преимущества игрока над группой врагов (уменьшен для повышения сложности)
        private static readonly float PlayerAdvantageMultiplier = 1.5f;
        
        // Настройки глобальной сложности
        private static Dictionary<string, float> _difficultySettings = new Dictionary<string, float>() 
        {
            { "Здоровье врагов", 1.0f },
            { "Урон врагов", 1.0f },
            { "Защита врагов", 1.0f },
            { "Опыт за врагов", 1.0f },
            { "Опыт для повышения уровня", 1.0f }
        };
        
        // Множители для разных типов статистик (увеличены для врагов)
        private static readonly Dictionary<Stat, float> StatBaselineValues = new Dictionary<Stat, float>() 
        {
            { Stat.Health, 200f },     // Было 180f
            { Stat.Mana, 70f },        // Было 60f
            { Stat.ManaRegenRate, 2.2f }, // Было 2f
            { Stat.ExperienceReward, 90f }, // Было 100f - снижено для замедления прогресса
            { Stat.ExperienceToLevelUp, 1200f }, // Было 1000f - повышено для замедления прогресса
            { Stat.Damage, 30f },      // Было 25f
            { Stat.TotalTraitPoints, 1f },
            { Stat.BuyingDiscountPercentage, 0f },
            { Stat.Defence, 16f }      // Было 14f
        };
        
        // Множители важности статистик для различных классов
        private static readonly Dictionary<CharacterClass, Dictionary<Stat, float>> ClassStatMultipliers = 
            new Dictionary<CharacterClass, Dictionary<Stat, float>>()
        {
            { 
                CharacterClass.Player, new Dictionary<Stat, float>() {
                    { Stat.Health, 1.15f },  // Было 1.2f - снижена живучесть игрока
                    { Stat.Mana, 1.0f },
                    { Stat.Damage, 1.05f },  // Было 1.1f - снижен урон игрока
                    { Stat.Defence, 0.85f }  // Было 0.9f - еще снижена защита игрока
                }
            },
            { 
                CharacterClass.Grunt, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.95f },   // Было 0.85f
                    { Stat.Damage, 0.9f },    // Было 0.8f
                    { Stat.Defence, 0.8f }    // Было 0.7f
                }
            },
            { 
                CharacterClass.Mage, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.75f },    // Было 0.7f
                    { Stat.Mana, 1.5f },      // Было 1.4f
                    { Stat.Damage, 1.2f },   // Было 1.05f
                    { Stat.Defence, 0.55f }   // Было 0.5f
                }
            },
            { 
                CharacterClass.Archer, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.8f },   // Было 0.75f
                    { Stat.Damage, 1.0f },    // Было 0.9f
                    { Stat.Defence, 0.7f }   // Было 0.65f
                }
            },
            { 
                CharacterClass.Orc, new Dictionary<Stat, float>() {
                    { Stat.Health, 1.5f },    // Было 1.4f
                    { Stat.Damage, 1.3f },    // Было 1.2f
                    { Stat.Defence, 1.1f }    // Было 1.0f
                }
            },
            { 
                CharacterClass.Wolf, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.8f },    // Было 0.7f
                    { Stat.Damage, 0.9f },    // Было 0.8f
                    { Stat.Defence, 0.6f }    // Было 0.5f
                }
            },
            { 
                CharacterClass.Boar, new Dictionary<Stat, float>() {
                    { Stat.Health, 1.0f },    // Было 0.9f
                    { Stat.Damage, 0.8f },    // Было 0.7f
                    { Stat.Defence, 0.8f }    // Было 0.7f
                }
            }
        };
        
        // Относительные силы классов врагов (сколько таких врагов нужно для сравнения с игроком)
        // Уменьшены значения для повышения сложности (теперь игрок слабее)
        private static readonly Dictionary<CharacterClass, float> RelativeEnemyStrength = new Dictionary<CharacterClass, float>()
        {
            { CharacterClass.Grunt, 2.0f },   // Было 2.2f
            { CharacterClass.Mage, 1.7f },    // Было 1.9f
            { CharacterClass.Archer, 1.9f },  // Было 2.1f
            { CharacterClass.Orc, 1.3f },     // Было 1.5f
            { CharacterClass.Wolf, 2.7f },    // Было 3.0f
            { CharacterClass.Boar, 2.3f },    // Было 2.6f
            { CharacterClass.Chest, 0f }      // Сундуки не имеют боевых характеристик
        };
        
        /// <summary>
        /// Устанавливает глобальные настройки сложности игры
        /// </summary>
        public static void SetGlobalDifficultySettings(Dictionary<string, float> settings)
        {
            if (settings != null)
            {
                _difficultySettings = new Dictionary<string, float>(settings);
            }
        }
        
        /// <summary>
        /// Получает текущие глобальные настройки сложности
        /// </summary>
        public static Dictionary<string, float> GetGlobalDifficultySettings()
        {
            return new Dictionary<string, float>(_difficultySettings);
        }
        
        /// <summary>
        /// Получает множитель для шанса уклонения в зависимости от сложности
        /// </summary>
        public static float GetEnemyDodgeChanceMultiplier()
        {
            if (_difficultySettings.ContainsKey("Шанс уклонения врагов"))
            {
                return _difficultySettings["Шанс уклонения врагов"];
            }
            return 1.0f;
        }

        /// <summary>
        /// Получает множитель для шанса критического удара в зависимости от сложности
        /// </summary>
        public static float GetEnemyCritChanceMultiplier()
        {
            if (_difficultySettings.ContainsKey("Шанс критического удара врагов"))
            {
                return _difficultySettings["Шанс критического удара врагов"];
            }
            return 1.0f;
        }

        /// <summary>
        /// Получает множитель для шанса парирования в зависимости от сложности
        /// </summary>
        public static float GetEnemyParryChanceMultiplier()
        {
            if (_difficultySettings.ContainsKey("Шанс парирования врагов"))
            {
                return _difficultySettings["Шанс парирования врагов"];
            }
            return 1.0f;
        }

        /// <summary>
        /// Применяет бонусы высокой сложности к характеристикам врага
        /// </summary>
        public static void ApplyHighDifficultyBonuses(CharacterClass characterClass, ref Dictionary<string, float> bonuses)
        {
            if (characterClass == CharacterClass.Player) return;
            
            // Получаем множители из настроек сложности
            float dodgeMultiplier = GetEnemyDodgeChanceMultiplier();
            float critMultiplier = GetEnemyCritChanceMultiplier();
            float parryMultiplier = GetEnemyParryChanceMultiplier();
            
            // Базовые шансы для разных врагов
            float baseDodgeChance = 0.05f;
            float baseCritChance = 0.08f;
            float baseParryChance = 0.04f;
            
            // Настраиваем базовые шансы в зависимости от класса
            switch (characterClass)
            {
                case CharacterClass.Grunt:
                    baseDodgeChance = 0.04f;
                    baseCritChance = 0.07f;
                    baseParryChance = 0.05f;
                    break;
                case CharacterClass.Mage:
                    baseDodgeChance = 0.06f;
                    baseCritChance = 0.10f;
                    baseParryChance = 0.02f;
                    break;
                case CharacterClass.Archer:
                    baseDodgeChance = 0.07f;
                    baseCritChance = 0.08f;
                    baseParryChance = 0.03f;
                    break;
                case CharacterClass.Orc:
                    baseDodgeChance = 0.03f;
                    baseCritChance = 0.06f;
                    baseParryChance = 0.07f;
                    break;
                case CharacterClass.Wolf:
                    baseDodgeChance = 0.09f;
                    baseCritChance = 0.05f;
                    baseParryChance = 0.01f;
                    break;
                case CharacterClass.Boar:
                    baseDodgeChance = 0.05f;
                    baseCritChance = 0.07f;
                    baseParryChance = 0.03f;
                    break;
            }
            
            // Применяем множители и добавляем в бонусы
            bonuses["DodgeChance"] = baseDodgeChance * dodgeMultiplier;
            bonuses["CritChance"] = baseCritChance * critMultiplier;
            bonuses["ParryChance"] = baseParryChance * parryMultiplier;
            
            // Ограничиваем максимальные значения
            bonuses["DodgeChance"] = Mathf.Min(bonuses["DodgeChance"], 0.25f);
            bonuses["CritChance"] = Mathf.Min(bonuses["CritChance"], 0.30f);
            bonuses["ParryChance"] = Mathf.Min(bonuses["ParryChance"], 0.20f);
        }
        
        /// <summary>
        /// Получает рекомендуемое базовое значение для статистики с учетом класса персонажа
        /// </summary>
        public static float GetRecommendedBaseValue(Stat stat, CharacterClass characterClass)
        {
            float baseValue = StatBaselineValues.ContainsKey(stat) ? StatBaselineValues[stat] : 10f;
            
            // Применяем множитель класса, если он определен
            if (ClassStatMultipliers.ContainsKey(characterClass) && 
                ClassStatMultipliers[characterClass].ContainsKey(stat))
            {
                baseValue *= ClassStatMultipliers[characterClass][stat];
            }
            
            // Для игрока применяем дополнительное преимущество, учитывая, что он сражается с группами врагов
            if (characterClass == CharacterClass.Player)
            {
                // Для атакующих статистик (урон) умножаем на AverageEnemiesPerFight
                if (stat == Stat.Damage)
                {
                    baseValue *= PlayerAdvantageMultiplier;
                }
                // Для защитных статистик (здоровье, защита) тоже даем преимущество
                else if (stat == Stat.Health || stat == Stat.Defence)
                {
                    baseValue *= PlayerAdvantageMultiplier;
                }
            }

            // Применяем глобальные настройки сложности
            if (characterClass != CharacterClass.Player)
            {
                // Применяем множители в зависимости от типа статистики
                if (stat == Stat.Health && _difficultySettings.ContainsKey("Здоровье врагов"))
                {
                    baseValue *= _difficultySettings["Здоровье врагов"];
                }
                else if (stat == Stat.Damage && _difficultySettings.ContainsKey("Урон врагов"))
                {
                    baseValue *= _difficultySettings["Урон врагов"];
                }
                else if (stat == Stat.Defence && _difficultySettings.ContainsKey("Защита врагов"))
                {
                    baseValue *= _difficultySettings["Защита врагов"];
                }
                else if (stat == Stat.ExperienceReward && _difficultySettings.ContainsKey("Опыт за врагов"))
                {
                    baseValue *= _difficultySettings["Опыт за врагов"];
                }
            }
            else if (characterClass == CharacterClass.Player)
            {
                // Для игрока применяем настройку только к опыту для повышения уровня
                if (stat == Stat.ExperienceToLevelUp && _difficultySettings.ContainsKey("Опыт для повышения уровня"))
                {
                    baseValue *= _difficultySettings["Опыт для повышения уровня"];
                }
            }
            
            return baseValue;
        }
        
        /// <summary>
        /// Создает шаблон прогрессии с линейным ростом значений
        /// </summary>
        /// <param name="startValue">Начальное значение</param>
        /// <param name="increment">Прирост на каждый уровень</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Массив значений</returns>
        public static float[] CreateLinearProgression(float startValue, float increment, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                values[i] = startValue + increment * i;
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает шаблон прогрессии с экспоненциальным ростом значений
        /// </summary>
        /// <param name="startValue">Начальное значение</param>
        /// <param name="growthFactor">Коэффициент роста</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Массив значений</returns>
        public static float[] CreateExponentialProgression(float startValue, float growthFactor, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                values[i] = startValue * Mathf.Pow(growthFactor, i);
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает шаблон прогрессии с полиномиальным ростом значений
        /// </summary>
        /// <param name="baseValue">Базовое значение</param>
        /// <param name="factor">Коэффициент</param>
        /// <param name="power">Степень</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Массив значений</returns>
        public static float[] CreatePolynomialProgression(float baseValue, float factor, float power, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                values[i] = baseValue + factor * Mathf.Pow(i + 1, power);
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает шаблон прогрессии с логарифмическим ростом значений
        /// </summary>
        /// <param name="baseValue">Базовое значение</param>
        /// <param name="factor">Коэффициент</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Массив значений</returns>
        public static float[] CreateLogarithmicProgression(float baseValue, float factor, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                values[i] = baseValue + factor * Mathf.Log(i + 2); // +2 чтобы избежать log(0) и log(1)=0
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает шаблон прогрессии в стиле Diablo (быстрый рост с замедлением)
        /// </summary>
        /// <param name="startValue">Начальное значение</param>
        /// <param name="endValue">Конечное значение</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Массив значений</returns>
        public static float[] CreateDiabloProgression(float startValue, float endValue, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                float t = (float)i / (levels - 1);
                // Кривая, которая быстро растет вначале и замедляется в конце
                float curve = 1 - Mathf.Pow(1 - t, 2.5f);
                values[i] = Mathf.Lerp(startValue, endValue, curve);
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает шаблон прогрессии в стиле Dark Souls (каждый уровень требует все больше усилий)
        /// </summary>
        /// <param name="startValue">Начальное значение</param>
        /// <param name="factor">Множитель роста</param>
        /// <param name="levelOffset">Начальное смещение (влияет на кривизну)</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Массив значений</returns>
        public static float[] CreateDarkSoulsProgression(float startValue, float factor, float levelOffset, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                // Формула из Dark Souls для опыта: Value = BaseValue * (Level + Offset)^1.8
                float level = i + 1; // Уровень начинается с 1
                values[i] = startValue * Mathf.Pow(level + levelOffset, 1.8f) * factor;
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает более сбалансированную прогрессию для здоровья игрока (стиль Path of Exile)
        /// </summary>
        /// <param name="baseHealth">Базовое здоровье на 1 уровне</param>
        /// <param name="healthPerLevel">Прирост здоровья за уровень</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Массив значений</returns>
        public static float[] CreatePathOfExileHealthProgression(float baseHealth, float healthPerLevel, int levels)
        {
            float[] values = new float[levels];
            
            // Начальное значение здоровья
            values[0] = baseHealth;
            
            for (int i = 1; i < levels; i++)
            {
                // Здоровье растет линейно + небольшой % от предыдущего уровня
                values[i] = values[i-1] + healthPerLevel + (values[i-1] * 0.08f);
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает сбалансированную прогрессию для опыта (стиль Final Fantasy)
        /// </summary>
        /// <param name="baseXP">Базовый опыт для 1 уровня</param>
        /// <param name="growthFactor">Множитель роста</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Массив значений</returns>
        public static float[] CreateFinalFantasyXPProgression(float baseXP, float growthFactor, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                // Используем формулу растущей кривой, где каждый следующий уровень требует больше опыта
                // XP = BaseXP * (Level^2 - Level + 2) * GrowthFactor
                float level = i + 1; // Уровень начинается с 1
                values[i] = baseXP * (level * level - level + 2) * growthFactor;
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает волнообразную прогрессию сложности (как в God of War)
        /// </summary>
        /// <param name="baseValue">Базовое значение</param>
        /// <param name="amplitude">Амплитуда волны</param>
        /// <param name="frequency">Частота волны</param>
        /// <param name="growthFactor">Множитель общего роста</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Массив значений</returns>
        public static float[] CreateWavyProgression(float baseValue, float amplitude, float frequency, float growthFactor, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                float level = i + 1;
                // Базовая линейная прогрессия
                float linearValue = baseValue * (1 + level * growthFactor);
                // Добавляем волнообразную компоненту
                float wave = amplitude * Mathf.Sin(level * frequency * Mathf.PI / levels);
                values[i] = linearValue + wave;
            }
            
            return values;
        }
        
        private static float GetScalingMultiplierForLevel(CharacterClass characterClass, int levels)
        {
            float scalingMultiplier = 1.0f;
            
            // Применяем более агрессивное масштабирование для высоких уровней
            if (characterClass != CharacterClass.Player && levels > 10)
            {
                // Для высоких уровней враги должны становиться сильнее
                scalingMultiplier = 1.0f + (levels - 10) * 0.1f; // Было 0.08f - еще более агрессивное масштабирование
            }
            
            return scalingMultiplier;
        }
        
        /// <summary>
        /// Создает полный набор статистик для указанного класса и количества уровней
        /// </summary>
        /// <param name="characterClass">Класс персонажа</param>
        /// <param name="levels">Количество уровней</param>
        /// <param name="difficultyMultiplier">Множитель сложности (1.0 = стандартный, меньше - легче, больше - сложнее)</param>
        /// <returns>Словарь с прогрессией статистик</returns>
        public static Dictionary<Stat, float[]> GenerateFullStatProgression(CharacterClass characterClass, int levels, float difficultyMultiplier = 1.0f)
        {
            Dictionary<Stat, float[]> statProgression = new Dictionary<Stat, float[]>();
            
            // Игрок не изменяется в зависимости от сложности класса, но меняется от глобальных настроек
            float enemyMultiplier = characterClass != CharacterClass.Player ? difficultyMultiplier : 1.0f;
            
            // Множитель силы врага относительно игрока
            float enemyStrengthMultiplier = 1.0f;
            if (characterClass != CharacterClass.Player && RelativeEnemyStrength.ContainsKey(characterClass))
            {
                enemyStrengthMultiplier = 1.0f / RelativeEnemyStrength[characterClass];
            }
            
            // Множитель масштабирования для высоких уровней
            // Обеспечивает более плавный рост и уменьшает разрыв между игроком и врагами
            float scalingMultiplier = GetScalingMultiplierForLevel(characterClass, levels);
            
            // Здоровье - разные типы прогрессии для разных классов
            float baseHealth = GetRecommendedBaseValue(Stat.Health, characterClass);
            
            // Применяем множитель относительной силы для врагов
            if (characterClass != CharacterClass.Player)
            {
                baseHealth *= enemyMultiplier * scalingMultiplier * enemyStrengthMultiplier;
            }
            
            if (characterClass == CharacterClass.Player)
            {
                // Для игрока используем более мощную прогрессию, но не слишком сильную
                statProgression[Stat.Health] = CreatePathOfExileHealthProgression(baseHealth, baseHealth * 0.12f, levels); // Было 0.13f
                
                // Дополнительный множитель для высоких уровней
                for (int i = 0; i < levels; i++)
                {
                    if (i > 5) // После 5-го уровня дополнительный бонус
                    {
                        statProgression[Stat.Health][i] *= 1.0f + ((i - 5) * 0.016f); // Было 0.018f
                    }
                }
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Используем экспоненциальный рост для магов, чтобы они были конкурентоспособны на высоких уровнях
                statProgression[Stat.Health] = CreateExponentialProgression(baseHealth, 1.17f + enemyMultiplier * 0.04f, levels); // Было 1.16f
            }
            else if (characterClass == CharacterClass.Grunt || characterClass == CharacterClass.Archer)
            {
                // Более быстрый рост для стандартных противников
                statProgression[Stat.Health] = CreatePolynomialProgression(baseHealth, baseHealth * 0.11f * enemyMultiplier, 1.55f, levels); // Было 0.1f, 1.5f
            }
            else if (characterClass == CharacterClass.Orc)
            {
                // Орки должны быть самыми мощными стандартными противниками
                statProgression[Stat.Health] = CreatePolynomialProgression(baseHealth, baseHealth * 0.13f * enemyMultiplier, 1.65f, levels); // Было 0.12f, 1.6f
            }
            else if (characterClass == CharacterClass.Wolf || characterClass == CharacterClass.Boar)
            {
                // Животные должны быть несколько слабее, но все еще масштабироваться с уровнем
                statProgression[Stat.Health] = CreatePolynomialProgression(baseHealth, baseHealth * 0.1f * enemyMultiplier, 1.45f, levels); // Было 0.09f, 1.4f
            }
            else
            {
                // Стандартная прогрессия для других классов
                statProgression[Stat.Health] = CreatePolynomialProgression(baseHealth, baseHealth * 0.09f * enemyMultiplier, 1.55f, levels); // Было 0.08f, 1.5f
            }
            
            // Мана - разные типы прогрессии для разных классов
            if (characterClass == CharacterClass.Mage)
            {
                float baseMana = GetRecommendedBaseValue(Stat.Mana, characterClass) * enemyMultiplier * scalingMultiplier * enemyStrengthMultiplier;
                statProgression[Stat.Mana] = CreateExponentialProgression(baseMana, 1.2f, levels); // Было 1.18f
            }
            else if (characterClass == CharacterClass.Player)
            {
                float baseMana = GetRecommendedBaseValue(Stat.Mana, characterClass);
                
                // Более умеренный рост маны для игрока
                statProgression[Stat.Mana] = CreateExponentialProgression(baseMana, 1.15f, levels); // Было 1.16f
                
                // Добавляем пики на ключевых уровнях
                for (int i = 0; i < levels; i++)
                {
                    int level = i + 1;
                    if (level % 5 == 0) // На уровнях 5, 10, 15, 20...
                    {
                        statProgression[Stat.Mana][i] *= 1.12f; // Было 1.15f
                    }
                }
            }
            else if (ClassStatMultipliers.ContainsKey(characterClass) && 
                     ClassStatMultipliers[characterClass].ContainsKey(Stat.Mana))
            {
                float baseMana = GetRecommendedBaseValue(Stat.Mana, characterClass) * enemyMultiplier * scalingMultiplier * enemyStrengthMultiplier;
                statProgression[Stat.Mana] = CreateExponentialProgression(baseMana, 1.14f, levels); // Было 1.12f
            }
            
            // Регенерация маны
            if (characterClass == CharacterClass.Player || characterClass == CharacterClass.Mage)
            {
                float baseManaRegen = StatBaselineValues[Stat.ManaRegenRate];
                if (characterClass == CharacterClass.Mage)
                {
                    baseManaRegen *= 1.25f * enemyMultiplier * enemyStrengthMultiplier; // Было 1.2f
                }
                
                statProgression[Stat.ManaRegenRate] = CreateExponentialProgression(baseManaRegen, 1.12f, levels); // Было 1.1f
            }
            
            // Урон - разные формулы для разных классов
            float baseDamage = GetRecommendedBaseValue(Stat.Damage, characterClass);
            if (characterClass != CharacterClass.Player)
            {
                // Применяем множитель сложности и масштабирования для врагов, а также их относительную силу
                baseDamage *= enemyMultiplier * scalingMultiplier * enemyStrengthMultiplier;
            }
            
            if (characterClass == CharacterClass.Player)
            {
                // Более умеренный рост урона для игрока, чтобы не было слишком большого разрыва
                statProgression[Stat.Damage] = CreateDiabloProgression(baseDamage, baseDamage * 11f, levels); // Было 12f
                
                // Добавляем пики мощности на ключевых уровнях
                for (int i = 0; i < levels; i++)
                {
                    int level = i + 1;
                    if (level % 5 == 0) // На уровнях 5, 10, 15, 20...
                    {
                        statProgression[Stat.Damage][i] *= 1.1f; // Было 1.12f
                    }
                }
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Маги наносят высокий урон, компенсируя низкую защиту
                statProgression[Stat.Damage] = CreateExponentialProgression(baseDamage, 1.19f + (enemyMultiplier * 0.04f), levels); // Было 1.17f, 0.03f
            }
            else if (characterClass == CharacterClass.Archer)
            {
                // Лучники сбалансированы по урону и здоровью
                statProgression[Stat.Damage] = CreatePolynomialProgression(baseDamage, baseDamage * 0.1f * enemyMultiplier, 1.6f, levels); // Было 0.09f, 1.55f
            }
            else if (characterClass == CharacterClass.Orc)
            {
                // Орки - сильные противники с высоким уроном
                statProgression[Stat.Damage] = CreatePolynomialProgression(baseDamage, baseDamage * 0.11f * enemyMultiplier, 1.65f, levels); // Было 0.1f, 1.6f
            }
            else
            {
                // Стандартная прогрессия для других классов
                statProgression[Stat.Damage] = CreatePolynomialProgression(baseDamage, baseDamage * 0.09f * enemyMultiplier, 1.6f, levels); // Было 0.08f, 1.55f
            }
            
            // Защита - разные формулы для разных классов
            if (ClassStatMultipliers.ContainsKey(characterClass) && 
                ClassStatMultipliers[characterClass].ContainsKey(Stat.Defence))
            {
                float baseDefence = GetRecommendedBaseValue(Stat.Defence, characterClass);
                if (characterClass != CharacterClass.Player)
                {
                    // Применяем множитель сложности и масштабирования для врагов
                    baseDefence *= enemyMultiplier * scalingMultiplier * enemyStrengthMultiplier;
                }
                
                if (characterClass == CharacterClass.Player)
                {
                    // Более умеренный рост защиты для игрока
                    statProgression[Stat.Defence] = CreateDarkSoulsProgression(baseDefence, 0.18f, 5f, levels); // Было 0.2f
                    
                    // Дополнительная защита игрока, имитирующая улучшение экипировки
                    for (int i = 0; i < levels; i++)
                    {
                        int level = i + 1;
                        // Бонус защиты, растущий с уровнем, как от улучшения экипировки
                        float equipmentBonus = 1.0f + (level * 0.022f); // Было 0.025f
                        statProgression[Stat.Defence][i] *= equipmentBonus;
                        
                        // Дополнительные бонусы на ключевых уровнях
                        if (level % 10 == 0) // На уровнях 10, 20, 30...
                        {
                            statProgression[Stat.Defence][i] *= 1.06f; // Было 1.08f
                        }
                    }
                }
                else if (characterClass == CharacterClass.Orc)
                {
                    // Орки имеют высокую защиту
                    statProgression[Stat.Defence] = CreatePolynomialProgression(baseDefence, baseDefence * 0.09f * enemyMultiplier, 1.55f, levels); // Было 0.085f, 1.5f
                }
                else if (characterClass == CharacterClass.Mage)
                {
                    // Маги имеют низкую защиту
                    statProgression[Stat.Defence] = CreatePolynomialProgression(baseDefence, baseDefence * 0.055f * enemyMultiplier, 1.35f, levels); // Было 0.05f, 1.3f
                }
                else if (characterClass == CharacterClass.Wolf || characterClass == CharacterClass.Boar)
                {
                    // Животные имеют низкую защиту
                    statProgression[Stat.Defence] = CreatePolynomialProgression(baseDefence, baseDefence * 0.065f * enemyMultiplier, 1.4f, levels); // Было 0.06f, 1.35f
                }
                else
                {
                    // Стандартная прогрессия защиты
                    statProgression[Stat.Defence] = CreatePolynomialProgression(baseDefence, baseDefence * 0.075f * enemyMultiplier, 1.45f, levels); // Было 0.07f, 1.4f
                }
            }
            
            // Опыт для повышения уровня (только для игрока)
            if (characterClass == CharacterClass.Player)
            {
                float baseXP = GetRecommendedBaseValue(Stat.ExperienceToLevelUp, characterClass);
                statProgression[Stat.ExperienceToLevelUp] = CreateFinalFantasyXPProgression(baseXP, 1.6f, levels); // Было 1.5f - требуется больше опыта
            }
            
            // Награда опытом (для всех кроме игрока)
            if (characterClass != CharacterClass.Player)
            {
                float xpValue = GetRecommendedBaseValue(Stat.ExperienceReward, characterClass);
                
                // Орки и маги дают больше опыта
                if (characterClass == CharacterClass.Orc)
                {
                    xpValue *= 1.6f; // Было 1.5f
                } 
                else if (characterClass == CharacterClass.Mage)
                {
                    xpValue *= 1.35f; // Было 1.3f
                }
                // Животные дают меньше опыта
                else if (characterClass == CharacterClass.Wolf || characterClass == CharacterClass.Boar)
                {
                    xpValue *= 0.75f; // Было 0.8f
                }
                
                // Награда растет с уровнем
                statProgression[Stat.ExperienceReward] = CreatePolynomialProgression(xpValue, xpValue * 0.14f, 1.75f, levels); // Было 0.15f, 1.8f
            }
            
            // Очки характеристик (только для игрока)
            if (characterClass == CharacterClass.Player)
            {
                float[] traitPoints = new float[levels];
                for (int i = 0; i < levels; i++)
                {
                    // Начиная с 1 очка на первом уровне
                    // Далее даем +1 очко каждые 3 уровня, +2 очка каждые 10 уровней
                    traitPoints[i] = 1;
                    
                    int level = i + 1;
                    if (level % 3 == 0) traitPoints[i] += 1;
                    if (level % 10 == 0) traitPoints[i] += 1;
                    if (level % 20 == 0) traitPoints[i] += 1;
                }
                statProgression[Stat.TotalTraitPoints] = traitPoints;
            }
            
            // Скидка при покупке (только для игрока)
            if (characterClass == CharacterClass.Player)
            {
                float[] discountValues = new float[levels];
                for (int i = 0; i < levels; i++)
                {
                    // Максимальная скидка 25% на 30 уровне (было 30%)
                    int level = i + 1;
                    discountValues[i] = Mathf.Min(25, level * 0.8f); // Добавлен множитель 0.8f для замедления роста скидки
                }
                statProgression[Stat.BuyingDiscountPercentage] = discountValues;
            }
            
            // Скорость движения - разные значения для разных классов
            float baseMovementSpeed = GetRecommendedBaseValue(Stat.MovementSpeed, characterClass);
            
            if (characterClass != CharacterClass.Player)
            {
                // Применяем множитель сложности для врагов, но меньший чем для других статистик
                baseMovementSpeed *= enemyMultiplier * 0.5f + 0.5f; // Более мягкое влияние сложности на скорость
            }
            
            if (characterClass == CharacterClass.Player)
            {
                // Игрок получает умеренное увеличение скорости с уровнем
                statProgression[Stat.MovementSpeed] = CreateLinearProgression(baseMovementSpeed, baseMovementSpeed * 0.02f, levels);
            }
            else if (characterClass == CharacterClass.Wolf)
            {
                // Волки быстрые и становятся еще быстрее
                statProgression[Stat.MovementSpeed] = CreateLinearProgression(baseMovementSpeed, baseMovementSpeed * 0.03f, levels);
            }
            else if (characterClass == CharacterClass.Archer)
            {
                // Лучники быстрые и мобильные
                statProgression[Stat.MovementSpeed] = CreateLinearProgression(baseMovementSpeed, baseMovementSpeed * 0.025f, levels);
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Маги средней скорости
                statProgression[Stat.MovementSpeed] = CreateLinearProgression(baseMovementSpeed, baseMovementSpeed * 0.015f, levels);
            }
            else if (characterClass == CharacterClass.Orc)
            {
                // Орки медленные, но немного ускоряются с уровнем
                statProgression[Stat.MovementSpeed] = CreateLinearProgression(baseMovementSpeed, baseMovementSpeed * 0.01f, levels);
            }
            else if (characterClass == CharacterClass.Boar)
            {
                // Кабаны могут быть быстрыми при атаке
                statProgression[Stat.MovementSpeed] = CreateLinearProgression(baseMovementSpeed, baseMovementSpeed * 0.02f, levels);
            }
            else
            {
                // Стандартная прогрессия скорости для остальных классов
                statProgression[Stat.MovementSpeed] = CreateLinearProgression(baseMovementSpeed, baseMovementSpeed * 0.015f, levels);
            }
            
            return statProgression;
        }
    }
    
    /// <summary>
    /// Редактор меню для Progression
    /// </summary>
    public static class ProgressionMenu
    {
        [MenuItem("RPG/Stats/Create Progression Template")]
        public static void CreateProgressionTemplate()
        {
            Progression template = ScriptableObject.CreateInstance<Progression>();
            AssetDatabase.CreateAsset(template, "Assets/Resources/Progression_Template.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = template;
        }
        
        [MenuItem("RPG/Stats/Open Progression Editor")]
        public static void OpenProgressionEditor()
        {
            // Находим все файлы Progression в проекте
            string[] guids = AssetDatabase.FindAssets("t:Progression");
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Progression progression = AssetDatabase.LoadAssetAtPath<Progression>(path);
                Selection.activeObject = progression;
                EditorUtility.FocusProjectWindow();
            }
            else
            {
                Debug.LogWarning("Не найдено файлов Progression. Сначала создайте шаблон.");
                CreateProgressionTemplate();
            }
        }
    }
} 