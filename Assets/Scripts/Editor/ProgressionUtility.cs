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
        private static readonly float AverageEnemiesPerFight = 2.5f;
        
        // Общий множитель для преимущества игрока над группой врагов
        private static readonly float PlayerAdvantageMultiplier = 2.5f;
        
        // Настройки глобальной сложности
        private static Dictionary<string, float> _difficultySettings = new Dictionary<string, float>() 
        {
            { "Здоровье врагов", 1.0f },
            { "Урон врагов", 1.0f },
            { "Защита врагов", 1.0f },
            { "Опыт за врагов", 1.0f },
            { "Опыт для повышения уровня", 1.0f }
        };
        
        // Базовые значения для статистик (пересчитаны под новый баланс)
        private static readonly Dictionary<Stat, float> StatBaselineValues = new Dictionary<Stat, float>() 
        {
            { Stat.Health, 280f },           // Базовое HP игрока
            { Stat.Mana, 100f },             // Базовая мана
            { Stat.ManaRegenRate, 2.5f },    // Базовая регенерация
            { Stat.ExperienceReward, 25f },  // Базовая награда опытом
            { Stat.ExperienceToLevelUp, 100f }, // Базовый опыт для левела
            { Stat.Damage, 35f },            // Базовый урон игрока
            { Stat.TotalTraitPoints, 1f },
            { Stat.BuyingDiscountPercentage, 0f },
            { Stat.Defence, 15f },           // Базовая защита игрока
            { Stat.MovementSpeed, 5.5f }     // Базовая скорость игрока
        };
        
        // Множители важности статистик для различных классов (переработаны)
        private static readonly Dictionary<CharacterClass, Dictionary<Stat, float>> ClassStatMultipliers = 
            new Dictionary<CharacterClass, Dictionary<Stat, float>>()
        {
            { 
                CharacterClass.Player, new Dictionary<Stat, float>() {
                    { Stat.Health, 1.0f },
                    { Stat.Mana, 1.0f },
                    { Stat.Damage, 1.0f },
                    { Stat.Defence, 1.0f },
                    { Stat.MovementSpeed, 1.0f }
                }
            },
            { 
                CharacterClass.Grunt, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.43f },      // 120 vs 280
                    { Stat.Damage, 0.51f },      // 18 vs 35
                    { Stat.Defence, 0.53f },     // 8 vs 15
                    { Stat.MovementSpeed, 0.82f } // 4.5 vs 5.5
                }
            },
            { 
                CharacterClass.Mage, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.29f },      // 80 vs 280 (самый слабый)
                    { Stat.Mana, 1.5f },         // 150 vs 100
                    { Stat.Damage, 0.8f },       // 28 vs 35 (высокий для врага)
                    { Stat.Defence, 0.33f },     // 5 vs 15 (самая слабая)
                    { Stat.MovementSpeed, 0.87f } // 4.8 vs 5.5
                }
            },
            { 
                CharacterClass.Archer, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.36f },      // 100 vs 280
                    { Stat.Damage, 0.63f },      // 22 vs 35
                    { Stat.Defence, 0.47f },     // 7 vs 15
                    { Stat.MovementSpeed, 0.95f } // 5.2 vs 5.5 (быстрый)
                }
            },
            { 
                CharacterClass.Orc, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.71f },      // 200 vs 280 (танк)
                    { Stat.Damage, 0.91f },      // 32 vs 35 (сильный)
                    { Stat.Defence, 1.2f },      // 18 vs 15 (самая высокая)
                    { Stat.MovementSpeed, 0.73f } // 4.0 vs 5.5 (медленный)
                }
            },
            { 
                CharacterClass.Wolf, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.21f },      // 60 vs 280 (слабый)
                    { Stat.Damage, 0.34f },      // 12 vs 35
                    { Stat.Defence, 0.27f },     // 4 vs 15
                    { Stat.MovementSpeed, 1.09f } // 6.0 vs 5.5 (быстрый)
                }
            },
            { 
                CharacterClass.Boar, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.32f },      // 90 vs 280
                    { Stat.Damage, 0.43f },      // 15 vs 35
                    { Stat.Defence, 0.4f },      // 6 vs 15
                    { Stat.MovementSpeed, 0.78f } // 4.3 vs 5.5
                }
            },
            { 
                CharacterClass.Spider, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.20f },      // 55 vs 280
                    { Stat.Damage, 0.4f },       // 14 vs 35
                    { Stat.Defence, 0.27f },     // ~4 vs 15
                    { Stat.MovementSpeed, 1.0f } // 5.5 vs 5.5
                }
            }
        };
        
        // Относительные силы классов врагов (сколько таких врагов нужно для сравнения с игроком)
        private static readonly Dictionary<CharacterClass, float> RelativeEnemyStrength = new Dictionary<CharacterClass, float>()
        {
            { CharacterClass.Grunt, 2.5f },   // Игрок = 2.5 гранта
            { CharacterClass.Mage, 2.0f },    // Игрок = 2 мага (опасен уроном)
            { CharacterClass.Archer, 2.3f },  // Игрок = 2.3 лучника
            { CharacterClass.Orc, 1.5f },     // Игрок = 1.5 орка (сильный враг)
            { CharacterClass.Wolf, 3.5f },    // Игрок = 3.5 волка
            { CharacterClass.Boar, 3.0f },    // Игрок = 3 кабана
            { CharacterClass.Spider, 3.5f },  // Игрок = 3.5 паука
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
            
            float dodgeMultiplier = GetEnemyDodgeChanceMultiplier();
            float critMultiplier = GetEnemyCritChanceMultiplier();
            float parryMultiplier = GetEnemyParryChanceMultiplier();
            
            float baseDodgeChance = 0.05f;
            float baseCritChance = 0.08f;
            float baseParryChance = 0.04f;
            
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
                case CharacterClass.Spider:
                    baseDodgeChance = 0.08f;
                    baseCritChance = 0.06f;
                    baseParryChance = 0.02f;
                    break;
            }
            
            bonuses["DodgeChance"] = baseDodgeChance * dodgeMultiplier;
            bonuses["CritChance"] = baseCritChance * critMultiplier;
            bonuses["ParryChance"] = baseParryChance * parryMultiplier;
            
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
            
            if (ClassStatMultipliers.ContainsKey(characterClass) && 
                ClassStatMultipliers[characterClass].ContainsKey(stat))
            {
                baseValue *= ClassStatMultipliers[characterClass][stat];
            }
            
            // Применяем глобальные настройки сложности
            if (characterClass != CharacterClass.Player)
            {
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
        public static float[] CreateLogarithmicProgression(float baseValue, float factor, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                values[i] = baseValue + factor * Mathf.Log(i + 2);
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает шаблон прогрессии в стиле Diablo (быстрый рост с замедлением)
        /// </summary>
        public static float[] CreateDiabloProgression(float startValue, float endValue, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                float t = (float)i / (levels - 1);
                float curve = 1 - Mathf.Pow(1 - t, 2.5f);
                values[i] = Mathf.Lerp(startValue, endValue, curve);
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает шаблон прогрессии в стиле Dark Souls
        /// </summary>
        public static float[] CreateDarkSoulsProgression(float startValue, float factor, float levelOffset, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                float level = i + 1;
                values[i] = startValue * Mathf.Pow(level + levelOffset, 1.8f) * factor;
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает сбалансированную прогрессию для здоровья игрока (стиль Path of Exile)
        /// </summary>
        public static float[] CreatePathOfExileHealthProgression(float baseHealth, float healthPerLevel, int levels)
        {
            float[] values = new float[levels];
            values[0] = baseHealth;
            
            for (int i = 1; i < levels; i++)
            {
                values[i] = values[i-1] + healthPerLevel + (values[i-1] * 0.08f);
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает сбалансированную прогрессию для опыта (стиль Final Fantasy)
        /// </summary>
        public static float[] CreateFinalFantasyXPProgression(float baseXP, float growthFactor, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                float level = i + 1;
                values[i] = baseXP * (level * level - level + 2) * growthFactor;
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает волнообразную прогрессию сложности (как в God of War)
        /// </summary>
        public static float[] CreateWavyProgression(float baseValue, float amplitude, float frequency, float growthFactor, int levels)
        {
            float[] values = new float[levels];
            
            for (int i = 0; i < levels; i++)
            {
                float level = i + 1;
                float linearValue = baseValue * (1 + level * growthFactor);
                float wave = amplitude * Mathf.Sin(level * frequency * Mathf.PI / levels);
                values[i] = linearValue + wave;
            }
            
            return values;
        }
        
        /// <summary>
        /// Создает полный набор статистик для указанного класса и количества уровней
        /// </summary>
        public static Dictionary<Stat, float[]> GenerateFullStatProgression(CharacterClass characterClass, int levels, float difficultyMultiplier = 1.0f)
        {
            Dictionary<Stat, float[]> statProgression = new Dictionary<Stat, float[]>();
            
            float enemyMultiplier = characterClass != CharacterClass.Player ? difficultyMultiplier : 1.0f;
            
            // ========== HEALTH ==========
            float baseHealth = GetRecommendedBaseValue(Stat.Health, characterClass);
            
            if (characterClass == CharacterClass.Player)
            {
                // Игрок: 280 -> 1640 (линейный рост ~75 HP/уровень)
                statProgression[Stat.Health] = CreateLinearProgression(280f, 75f, levels);
            }
            else if (characterClass == CharacterClass.Grunt)
            {
                // Grunt: 120 -> 953 (средний рост)
                statProgression[Stat.Health] = CreatePolynomialProgression(120f * enemyMultiplier, 3.5f * enemyMultiplier, 1.4f, levels);
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Mage: 80 -> 607 (низкое HP)
                statProgression[Stat.Health] = CreatePolynomialProgression(80f * enemyMultiplier, 2.0f * enemyMultiplier, 1.5f, levels);
            }
            else if (characterClass == CharacterClass.Archer)
            {
                // Archer: 100 -> 848
                statProgression[Stat.Health] = CreatePolynomialProgression(100f * enemyMultiplier, 2.8f * enemyMultiplier, 1.45f, levels);
            }
            else if (characterClass == CharacterClass.Orc)
            {
                // Orc: 200 -> 1781 (высокое HP)
                statProgression[Stat.Health] = CreatePolynomialProgression(200f * enemyMultiplier, 5.5f * enemyMultiplier, 1.38f, levels);
            }
            else if (characterClass == CharacterClass.Wolf)
            {
                // Wolf: 60 -> 400 (слабый)
                statProgression[Stat.Health] = CreateLinearProgression(60f * enemyMultiplier, 20f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Boar)
            {
                // Boar: 90 -> 668
                statProgression[Stat.Health] = CreatePolynomialProgression(90f * enemyMultiplier, 2.2f * enemyMultiplier, 1.42f, levels);
            }
            else if (characterClass == CharacterClass.Spider)
            {
                // Spider: 55 -> 378
                statProgression[Stat.Health] = CreateLinearProgression(55f * enemyMultiplier, 19f * enemyMultiplier, levels);
            }
            
            // ========== MANA ==========
            if (characterClass == CharacterClass.Player)
            {
                // Player: 100 -> 1080
                statProgression[Stat.Mana] = CreateExponentialProgression(100f, 1.15f, levels);
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Mage: 150 -> 3330 (много маны)
                statProgression[Stat.Mana] = CreateExponentialProgression(150f * enemyMultiplier, 1.20f, levels);
            }
            
            // ========== MANA REGEN ==========
            if (characterClass == CharacterClass.Player || characterClass == CharacterClass.Mage)
            {
                float baseManaRegen = characterClass == CharacterClass.Player ? 2.5f : 4.0f * enemyMultiplier;
                statProgression[Stat.ManaRegenRate] = CreateExponentialProgression(baseManaRegen, 1.11f, levels);
            }
            
            // ========== DAMAGE ==========
            if (characterClass == CharacterClass.Player)
            {
                // Player: 35 -> 290 (линейный рост ~15/уровень)
                statProgression[Stat.Damage] = CreateLinearProgression(35f, 15f, levels);
            }
            else if (characterClass == CharacterClass.Grunt)
            {
                // Grunt: 18 -> 222
                statProgression[Stat.Damage] = CreateLinearProgression(18f * enemyMultiplier, 12f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Mage: 28 -> 436 (высокий урон)
                statProgression[Stat.Damage] = CreatePolynomialProgression(28f * enemyMultiplier, 1.5f * enemyMultiplier, 1.52f, levels);
            }
            else if (characterClass == CharacterClass.Archer)
            {
                // Archer: 22 -> 260
                statProgression[Stat.Damage] = CreateLinearProgression(22f * enemyMultiplier, 14f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Orc)
            {
                // Orc: 32 -> 457
                statProgression[Stat.Damage] = CreatePolynomialProgression(32f * enemyMultiplier, 1.5f * enemyMultiplier, 1.48f, levels);
            }
            else if (characterClass == CharacterClass.Wolf)
            {
                // Wolf: 12 -> 199
                statProgression[Stat.Damage] = CreateLinearProgression(12f * enemyMultiplier, 11f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Boar)
            {
                // Boar: 15 -> 219
                statProgression[Stat.Damage] = CreateLinearProgression(15f * enemyMultiplier, 12f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Spider)
            {
                // Spider: 14 -> 218
                statProgression[Stat.Damage] = CreateLinearProgression(14f * enemyMultiplier, 12f * enemyMultiplier, levels);
            }
            
            // ========== DEFENCE ==========
            if (characterClass == CharacterClass.Player)
            {
                // Player: 15 -> 187
                statProgression[Stat.Defence] = CreateLinearProgression(15f, 10f, levels);
            }
            else if (characterClass == CharacterClass.Grunt)
            {
                // Grunt: 8 -> 151
                statProgression[Stat.Defence] = CreateLinearProgression(8f * enemyMultiplier, 8.4f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Mage: 5 -> 145 (слабая защита)
                statProgression[Stat.Defence] = CreateLinearProgression(5f * enemyMultiplier, 8.2f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Archer)
            {
                // Archer: 7 -> 177
                statProgression[Stat.Defence] = CreateLinearProgression(7f * enemyMultiplier, 10f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Orc)
            {
                // Orc: 18 -> 239 (высокая защита)
                statProgression[Stat.Defence] = CreateLinearProgression(18f * enemyMultiplier, 13f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Wolf)
            {
                // Wolf: 4 -> 142
                statProgression[Stat.Defence] = CreateLinearProgression(4f * enemyMultiplier, 8.1f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Boar)
            {
                // Boar: 6 -> 176
                statProgression[Stat.Defence] = CreateLinearProgression(6f * enemyMultiplier, 10f * enemyMultiplier, levels);
            }
            else if (characterClass == CharacterClass.Spider)
            {
                // Spider: ~4 -> ~142
                statProgression[Stat.Defence] = CreateLinearProgression(4f * enemyMultiplier, 8.1f * enemyMultiplier, levels);
            }
            
            // ========== EXPERIENCE TO LEVEL UP (только для игрока) ==========
            if (characterClass == CharacterClass.Player)
            {
                // Player: 100 -> 9450 (квадратичный рост)
                float[] xpValues = new float[levels];
                for (int i = 0; i < levels; i++)
                {
                    int level = i + 1;
                    // Формула: 100 + 150*level + 25*level^2
                    xpValues[i] = 100f + (150f * level) + (25f * level * level);
                }
                statProgression[Stat.ExperienceToLevelUp] = xpValues;
            }
            
            // ========== EXPERIENCE REWARD (для врагов) ==========
            if (characterClass != CharacterClass.Player)
            {
                float baseXP = 25f; // Базовая награда за Grunt
                
                if (characterClass == CharacterClass.Orc)
                {
                    baseXP = 50f; // Двойная награда за сильного врага
                }
                else if (characterClass == CharacterClass.Mage)
                {
                    baseXP = 35f; // Больше за мага
                }
                else if (characterClass == CharacterClass.Archer)
                {
                    baseXP = 28f;
                }
                else if (characterClass == CharacterClass.Wolf)
                {
                    baseXP = 15f; // Меньше за слабого врага
                }
                else if (characterClass == CharacterClass.Boar)
                {
                    baseXP = 20f;
                }
                else if (characterClass == CharacterClass.Spider)
                {
                    baseXP = 18f;
                }
                
                // Линейный рост награды опытом
                statProgression[Stat.ExperienceReward] = CreateLinearProgression(baseXP * enemyMultiplier, baseXP * 0.6f * enemyMultiplier, levels);
            }
            
            // ========== TRAIT POINTS (только для игрока) ==========
            if (characterClass == CharacterClass.Player)
            {
                float[] traitPoints = new float[levels];
                for (int i = 0; i < levels; i++)
                {
                    traitPoints[i] = 1;
                    
                    int level = i + 1;
                    if (level % 3 == 0) traitPoints[i] += 1;
                    if (level % 10 == 0) traitPoints[i] += 1;
                    if (level % 20 == 0) traitPoints[i] += 1;
                }
                statProgression[Stat.TotalTraitPoints] = traitPoints;
            }
            
            // ========== BUYING DISCOUNT (только для игрока) ==========
            if (characterClass == CharacterClass.Player)
            {
                float[] discountValues = new float[levels];
                for (int i = 0; i < levels; i++)
                {
                    int level = i + 1;
                    discountValues[i] = Mathf.Min(18, level * 1.0f);
                }
                statProgression[Stat.BuyingDiscountPercentage] = discountValues;
            }
            
            // ========== MOVEMENT SPEED ==========
            float baseSpeed = GetRecommendedBaseValue(Stat.MovementSpeed, characterClass);
            float speedIncrement = 0.1f;
            
            if (characterClass == CharacterClass.Player)
            {
                // Player: 5.5 -> 7.3
                speedIncrement = 0.1f;
            }
            else if (characterClass == CharacterClass.Grunt)
            {
                // Grunt: 4.5 -> 6.2
                speedIncrement = 0.1f;
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Mage: 4.8 -> 6.5
                speedIncrement = 0.1f;
            }
            else if (characterClass == CharacterClass.Archer)
            {
                // Archer: 5.2 -> 6.9
                speedIncrement = 0.1f;
            }
            else if (characterClass == CharacterClass.Orc)
            {
                // Orc: 4.0 -> 5.7 (медленный)
                speedIncrement = 0.1f;
            }
            else if (characterClass == CharacterClass.Wolf)
            {
                // Wolf: 6.0 -> 7.7 (быстрый)
                speedIncrement = 0.1f;
            }
            else if (characterClass == CharacterClass.Boar)
            {
                // Boar: 4.3 -> 6.0
                speedIncrement = 0.1f;
            }
            else if (characterClass == CharacterClass.Spider)
            {
                // Spider: 5.5 -> 7.2
                speedIncrement = 0.1f;
            }
            
            statProgression[Stat.MovementSpeed] = CreateLinearProgression(baseSpeed, speedIncrement, levels);
            
            // ========== CHEST (специальный случай) ==========
            if (characterClass == CharacterClass.Chest)
            {
                // HP = 1 (неуязвимый)
                statProgression[Stat.Health] = new float[levels];
                for (int i = 0; i < levels; i++) statProgression[Stat.Health][i] = 1f;
                
                // Damage = 0
                statProgression[Stat.Damage] = new float[levels];
                for (int i = 0; i < levels; i++) statProgression[Stat.Damage][i] = 0f;
                
                // Experience reward: 30 -> 472
                statProgression[Stat.ExperienceReward] = CreatePolynomialProgression(30f, 1.5f, 1.5f, levels);
                
                // Speed = 0
                statProgression[Stat.MovementSpeed] = new float[levels];
                for (int i = 0; i < levels; i++) statProgression[Stat.MovementSpeed][i] = 0f;
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