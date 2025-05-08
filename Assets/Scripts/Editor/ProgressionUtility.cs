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
        
        // Общий множитель для преимущества игрока над группой врагов
        private static readonly float PlayerAdvantageMultiplier = 3.5f;
        
        // Множители для разных типов статистик
        private static readonly Dictionary<Stat, float> StatBaselineValues = new Dictionary<Stat, float>() 
        {
            { Stat.Health, 120f },
            { Stat.Mana, 50f },
            { Stat.ManaRegenRate, 2f },
            { Stat.ExperienceReward, 100f },
            { Stat.ExperienceToLevelUp, 1000f },
            { Stat.Damage, 15f },
            { Stat.TotalTraitPoints, 1f },
            { Stat.BuyingDiscountPercentage, 0f },
            { Stat.Defence, 10f }
        };
        
        // Множители важности статистик для различных классов
        private static readonly Dictionary<CharacterClass, Dictionary<Stat, float>> ClassStatMultipliers = 
            new Dictionary<CharacterClass, Dictionary<Stat, float>>()
        {
            { 
                CharacterClass.Player, new Dictionary<Stat, float>() {
                    { Stat.Health, 1.2f },
                    { Stat.Mana, 1.0f },
                    { Stat.Damage, 1.1f },
                    { Stat.Defence, 1.1f }
                }
            },
            { 
                CharacterClass.Grunt, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.8f },
                    { Stat.Damage, 0.7f },
                    { Stat.Defence, 0.6f }
                }
            },
            { 
                CharacterClass.Mage, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.7f },
                    { Stat.Mana, 1.3f },
                    { Stat.Damage, 0.9f },
                    { Stat.Defence, 0.5f }
                }
            },
            { 
                CharacterClass.Archer, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.75f },
                    { Stat.Damage, 0.8f },
                    { Stat.Defence, 0.6f }
                }
            },
            { 
                CharacterClass.Orc, new Dictionary<Stat, float>() {
                    { Stat.Health, 1.0f },
                    { Stat.Damage, 0.9f },
                    { Stat.Defence, 0.8f }
                }
            },
            { 
                CharacterClass.Wolf, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.6f },
                    { Stat.Damage, 0.7f },
                    { Stat.Defence, 0.4f }
                }
            },
            { 
                CharacterClass.Boar, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.8f },
                    { Stat.Damage, 0.6f },
                    { Stat.Defence, 0.7f }
                }
            }
        };
        
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
            
            // Игрок не изменяется в зависимости от сложности
            float enemyMultiplier = characterClass != CharacterClass.Player ? difficultyMultiplier : 1.0f;
            
            // Множитель масштабирования для высоких уровней
            // Обеспечивает более плавный рост и уменьшает разрыв между игроком и врагами
            float scalingMultiplier = 1.0f;
            if (characterClass != CharacterClass.Player && levels > 10)
            {
                // Для высоких уровней враги должны становиться сильнее
                scalingMultiplier = 1.0f + (levels - 10) * 0.05f;
            }
            
            // Здоровье - разные типы прогрессии для разных классов
            float baseHealth = GetRecommendedBaseValue(Stat.Health, characterClass);
            if (characterClass != CharacterClass.Player)
            {
                // Применяем множитель сложности и масштабирования для врагов
                baseHealth *= enemyMultiplier * scalingMultiplier;
            }
            
            if (characterClass == CharacterClass.Player)
            {
                // Для игрока используем более мощную прогрессию, но не слишком сильную
                statProgression[Stat.Health] = CreatePathOfExileHealthProgression(baseHealth, baseHealth * 0.12f, levels);
                
                // Дополнительный множитель для высоких уровней
                for (int i = 0; i < levels; i++)
                {
                    if (i > 5) // После 5-го уровня дополнительный бонус
                    {
                        statProgression[Stat.Health][i] *= 1.0f + ((i - 5) * 0.015f); // +1.5% за каждый уровень выше 5
                    }
                }
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Используем экспоненциальный рост для магов, чтобы они были конкурентоспособны на высоких уровнях
                statProgression[Stat.Health] = CreateExponentialProgression(baseHealth, 1.15f + enemyMultiplier * 0.05f, levels);
            }
            else if (characterClass == CharacterClass.Grunt || characterClass == CharacterClass.Archer)
            {
                // Более быстрый рост для стандартных противников
                statProgression[Stat.Health] = CreatePolynomialProgression(baseHealth, baseHealth * 0.1f * enemyMultiplier, 1.6f, levels);
            }
            else if (characterClass == CharacterClass.Orc)
            {
                // Орки должны быть самыми мощными стандартными противниками
                statProgression[Stat.Health] = CreatePolynomialProgression(baseHealth, baseHealth * 0.12f * enemyMultiplier, 1.7f, levels);
            }
            else if (characterClass == CharacterClass.Wolf || characterClass == CharacterClass.Boar)
            {
                // Животные должны быть несколько слабее, но все еще масштабироваться с уровнем
                statProgression[Stat.Health] = CreatePolynomialProgression(baseHealth, baseHealth * 0.09f * enemyMultiplier, 1.5f, levels);
            }
            else
            {
                // Стандартная прогрессия для других классов
                statProgression[Stat.Health] = CreatePolynomialProgression(baseHealth, baseHealth * 0.08f * enemyMultiplier, 1.5f, levels);
            }
            
            // Мана - разные типы прогрессии для разных классов
            if (characterClass == CharacterClass.Mage)
            {
                float baseMana = GetRecommendedBaseValue(Stat.Mana, characterClass) * enemyMultiplier * scalingMultiplier;
                statProgression[Stat.Mana] = CreateExponentialProgression(baseMana, 1.18f, levels);
            }
            else if (characterClass == CharacterClass.Player)
            {
                float baseMana = GetRecommendedBaseValue(Stat.Mana, characterClass);
                
                // Более умеренный рост маны для игрока
                statProgression[Stat.Mana] = CreateExponentialProgression(baseMana, 1.16f, levels);
                
                // Добавляем пики на ключевых уровнях
                for (int i = 0; i < levels; i++)
                {
                    int level = i + 1;
                    if (level % 5 == 0) // На уровнях 5, 10, 15, 20...
                    {
                        statProgression[Stat.Mana][i] *= 1.15f; // +15% маны
                    }
                }
            }
            else if (ClassStatMultipliers.ContainsKey(characterClass) && 
                     ClassStatMultipliers[characterClass].ContainsKey(Stat.Mana))
            {
                float baseMana = GetRecommendedBaseValue(Stat.Mana, characterClass) * enemyMultiplier * scalingMultiplier;
                statProgression[Stat.Mana] = CreateExponentialProgression(baseMana, 1.12f, levels);
            }
            
            // Урон - разные формулы для разных классов
            float baseDamage = GetRecommendedBaseValue(Stat.Damage, characterClass);
            if (characterClass != CharacterClass.Player)
            {
                // Применяем множитель сложности и масштабирования для врагов
                baseDamage *= enemyMultiplier * scalingMultiplier;
            }
            
            if (characterClass == CharacterClass.Player)
            {
                // Более умеренный рост урона для игрока, чтобы не было слишком большого разрыва
                statProgression[Stat.Damage] = CreateDiabloProgression(baseDamage, baseDamage * 12f, levels);
                
                // Добавляем пики мощности на ключевых уровнях
                for (int i = 0; i < levels; i++)
                {
                    int level = i + 1;
                    if (level % 5 == 0) // На уровнях 5, 10, 15, 20...
                    {
                        statProgression[Stat.Damage][i] *= 1.12f; // +12% урона
                    }
                }
            }
            else if (characterClass == CharacterClass.Mage)
            {
                // Маги наносят высокий урон, компенсируя низкую защиту
                statProgression[Stat.Damage] = CreateExponentialProgression(baseDamage, 1.18f + (enemyMultiplier * 0.03f), levels);
            }
            else if (characterClass == CharacterClass.Archer)
            {
                // Лучники сбалансированы по урону и здоровью
                statProgression[Stat.Damage] = CreatePolynomialProgression(baseDamage, baseDamage * 0.09f * enemyMultiplier, 1.6f, levels);
            }
            else if (characterClass == CharacterClass.Orc)
            {
                // Орки - сильные противники с высоким уроном
                statProgression[Stat.Damage] = CreatePolynomialProgression(baseDamage, baseDamage * 0.1f * enemyMultiplier, 1.65f, levels);
            }
            else
            {
                // Стандартная прогрессия для других классов
                statProgression[Stat.Damage] = CreatePolynomialProgression(baseDamage, baseDamage * 0.08f * enemyMultiplier, 1.6f, levels);
            }
            
            // Защита - разные формулы для разных классов
            if (ClassStatMultipliers.ContainsKey(characterClass) && 
                ClassStatMultipliers[characterClass].ContainsKey(Stat.Defence))
            {
                float baseDefence = GetRecommendedBaseValue(Stat.Defence, characterClass);
                if (characterClass != CharacterClass.Player)
                {
                    // Применяем множитель сложности и масштабирования для врагов
                    baseDefence *= enemyMultiplier * scalingMultiplier;
                }
                
                if (characterClass == CharacterClass.Player)
                {
                    // Более умеренный рост защиты для игрока
                    statProgression[Stat.Defence] = CreateDarkSoulsProgression(baseDefence, 0.2f, 5f, levels);
                    
                    // Дополнительная защита игрока, имитирующая улучшение экипировки
                    for (int i = 0; i < levels; i++)
                    {
                        int level = i + 1;
                        // Бонус защиты, растущий с уровнем, как от улучшения экипировки
                        float equipmentBonus = 1.0f + (level * 0.025f); // Уменьшено с 0.03f
                        statProgression[Stat.Defence][i] *= equipmentBonus;
                        
                        // Дополнительные бонусы на ключевых уровнях
                        if (level % 10 == 0) // На уровнях 10, 20, 30...
                        {
                            statProgression[Stat.Defence][i] *= 1.08f; // +8% защиты (уменьшено с 10%)
                        }
                    }
                }
                else if (characterClass == CharacterClass.Orc)
                {
                    // Орки имеют высокую защиту
                    statProgression[Stat.Defence] = CreatePolynomialProgression(baseDefence, baseDefence * 0.08f * enemyMultiplier, 1.5f, levels);
                }
                else if (characterClass == CharacterClass.Mage)
                {
                    // Маги имеют низкую защиту
                    statProgression[Stat.Defence] = CreatePolynomialProgression(baseDefence, baseDefence * 0.05f * enemyMultiplier, 1.3f, levels);
                }
                else
                {
                    // Стандартная прогрессия для других классов
                    statProgression[Stat.Defence] = CreatePolynomialProgression(baseDefence, baseDefence * 0.07f * enemyMultiplier, 1.4f, levels);
                }
            }
            
            // Опыт для повышения уровня - только для игрока и с нелинейной прогрессией
            if (characterClass == CharacterClass.Player)
            {
                float baseXP = 1000f; // Базовое количество опыта для первого уровня
                statProgression[Stat.ExperienceToLevelUp] = CreateFinalFantasyXPProgression(baseXP, 1.2f, levels);
                
                // Корректируем кривую опыта, чтобы сделать её более плавной для среднего диапазона уровней
                for (int i = 0; i < levels; i++)
                {
                    int level = i + 1;
                    if (level > 5 && level <= 15)
                    {
                        // Снижаем опыт для среднего диапазона уровней
                        statProgression[Stat.ExperienceToLevelUp][i] *= 0.85f;
                    }
                    else if (level > 15)
                    {
                        // Умеренно повышаем для высоких уровней
                        statProgression[Stat.ExperienceToLevelUp][i] *= 0.95f;
                    }
                }
            }
            
            // Опыт за убийство - только для врагов и пропорционально их здоровью и уровню
            if (characterClass != CharacterClass.Player && characterClass != CharacterClass.Chest)
            {
                float baseXPReward = 50f;
                if (statProgression.ContainsKey(Stat.Health) && statProgression[Stat.Health].Length > 0)
                {
                    baseXPReward = statProgression[Stat.Health][0] * 0.4f; // Уменьшено с 0.5f для лучшего баланса
                }
                
                statProgression[Stat.ExperienceReward] = new float[levels];
                
                for (int i = 0; i < levels; i++)
                {
                    // XP награда зависит от типа врага и его уровня
                    float levelMultiplier = Mathf.Pow(1.1f, i); // Уменьшено с 1.15f
                    
                    if (statProgression.ContainsKey(Stat.Health)) 
                    {
                        int healthIndex = Mathf.Min(i, statProgression[Stat.Health].Length - 1);
                        
                        // Базовая награда пропорциональна здоровью
                        float baseReward = statProgression[Stat.Health][healthIndex] * 0.4f * levelMultiplier;
                        
                        // Дополнительные бонусы за особые типы врагов
                        if (characterClass == CharacterClass.Orc || characterClass == CharacterClass.Mage)
                        {
                            // Элитные противники дают больше опыта
                            baseReward *= 1.3f; // Уменьшено с 1.5f
                        }
                        
                        statProgression[Stat.ExperienceReward][i] = baseReward;
                    }
                    else
                    {
                        statProgression[Stat.ExperienceReward][i] = baseXPReward * (i + 1) * levelMultiplier;
                    }
                }
            }
            
            // Скорость регенерации маны - только для классов с маной
            if (statProgression.ContainsKey(Stat.Mana))
            {
                float baseManaRegen = GetRecommendedBaseValue(Stat.ManaRegenRate, characterClass);
                if (characterClass == CharacterClass.Mage)
                {
                    statProgression[Stat.ManaRegenRate] = CreateLinearProgression(baseManaRegen, baseManaRegen * 0.12f, levels);
                }
                else if (characterClass == CharacterClass.Player)
                {
                    // Для игрока делаем регенерацию маны более эффективной на высоких уровнях
                    statProgression[Stat.ManaRegenRate] = CreatePolynomialProgression(baseManaRegen, baseManaRegen * 0.05f, 1.4f, levels);
                }
                else
                {
                    statProgression[Stat.ManaRegenRate] = CreateLinearProgression(baseManaRegen, baseManaRegen * 0.1f, levels);
                }
            }
            
            // Очки талантов - только для игрока, с нелинейным распределением
            if (characterClass == CharacterClass.Player)
            {
                float[] traitPoints = new float[levels];
                for (int i = 0; i < levels; i++)
                {
                    // Каждые 3 уровня дают дополнительное очко талантов
                    // На вехах (5, 10, 15) даем бонусные очки
                    int level = i + 1;
                    float points = 1;
                    
                    if (level % 3 == 0) points += 1;
                    if (level % 5 == 0) points += 1;
                    if (level % 10 == 0) points += 2;
                    if (level % 20 == 0) points += 3; // Мощный бонус на 20-м уровне
                    
                    traitPoints[i] = points;
                }
                statProgression[Stat.TotalTraitPoints] = traitPoints;
            }
            
            // Скидка при покупке - только для игрока с прогрессивной шкалой
            if (characterClass == CharacterClass.Player)
            {
                float[] discount = new float[levels];
                for (int i = 0; i < levels; i++)
                {
                    // Нелинейная прогрессия скидки, с замедлением роста на высоких уровнях
                    float level = i + 1;
                    float maxDiscount = 40f; // Увеличенный максимум
                    
                    // Более быстрый рост скидки на начальных уровнях
                    if (level <= 10)
                    {
                        discount[i] = 5f + level * 1.0f; // Быстрый рост для ранних уровней
                    }
                    else
                    {
                        // Замедление роста на высоких уровнях
                        discount[i] = Mathf.Min(maxDiscount, 15f + (level - 10) * 0.5f * Mathf.Pow(0.95f, (level - 10) / 5f));
                    }
                    
                    // Бонусные скидки на определенных уровнях (как при получении особых торговых навыков)
                    if (level % 10 == 0)
                    {
                        discount[i] += 2f; // Дополнительные 2% скидки
                    }
                }
                statProgression[Stat.BuyingDiscountPercentage] = discount;
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