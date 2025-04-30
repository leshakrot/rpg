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
        private static readonly float PlayerAdvantageMultiplier = 2.5f;
        
        // Множители для разных типов статистик
        private static readonly Dictionary<Stat, float> StatBaselineValues = new Dictionary<Stat, float>() 
        {
            { Stat.Health, 100f },
            { Stat.Mana, 50f },
            { Stat.ManaRegenRate, 2f },
            { Stat.ExperienceReward, 100f },
            { Stat.ExperienceToLevelUp, 1000f },
            { Stat.Damage, 10f },
            { Stat.TotalTraitPoints, 1f },
            { Stat.BuyingDiscountPercentage, 0f },
            { Stat.Defence, 5f }
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
                    { Stat.Health, 0.7f },
                    { Stat.Damage, 0.6f },
                    { Stat.Defence, 0.5f }
                }
            },
            { 
                CharacterClass.Mage, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.6f },
                    { Stat.Mana, 1.3f },
                    { Stat.Damage, 0.8f },
                    { Stat.Defence, 0.4f }
                }
            },
            { 
                CharacterClass.Archer, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.65f },
                    { Stat.Damage, 0.7f },
                    { Stat.Defence, 0.5f }
                }
            },
            { 
                CharacterClass.Orc, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.9f },
                    { Stat.Damage, 0.8f },
                    { Stat.Defence, 0.7f }
                }
            },
            { 
                CharacterClass.Wolf, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.5f },
                    { Stat.Damage, 0.6f },
                    { Stat.Defence, 0.3f }
                }
            },
            { 
                CharacterClass.Boar, new Dictionary<Stat, float>() {
                    { Stat.Health, 0.7f },
                    { Stat.Damage, 0.5f },
                    { Stat.Defence, 0.6f }
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
        /// <returns>Словарь с прогрессией статистик</returns>
        public static Dictionary<Stat, float[]> GenerateFullStatProgression(CharacterClass characterClass, int levels)
        {
            Dictionary<Stat, float[]> statProgression = new Dictionary<Stat, float[]>();
            
            // Здоровье - быстрый рост для игрока, учитывая количество врагов
            if (characterClass == CharacterClass.Player)
            {
                float baseHealth = GetRecommendedBaseValue(Stat.Health, characterClass);
                statProgression[Stat.Health] = CreatePathOfExileHealthProgression(baseHealth, baseHealth * 0.12f, levels);
            }
            else
            {
                float baseHealth = GetRecommendedBaseValue(Stat.Health, characterClass);
                statProgression[Stat.Health] = CreateLinearProgression(baseHealth, baseHealth * 0.08f, levels);
            }
            
            // Мана - экспоненциальный рост для магов, линейный для остальных
            if (characterClass == CharacterClass.Mage || characterClass == CharacterClass.Player)
            {
                float baseMana = GetRecommendedBaseValue(Stat.Mana, characterClass);
                statProgression[Stat.Mana] = CreateExponentialProgression(baseMana, 1.15f, levels);
            }
            else if (ClassStatMultipliers.ContainsKey(characterClass) && 
                     ClassStatMultipliers[characterClass].ContainsKey(Stat.Mana))
            {
                float baseMana = GetRecommendedBaseValue(Stat.Mana, characterClass);
                statProgression[Stat.Mana] = CreateLinearProgression(baseMana, baseMana * 0.05f, levels);
            }
            
            // Урон - разные формулы для разных классов
            if (characterClass == CharacterClass.Player)
            {
                float baseDamage = GetRecommendedBaseValue(Stat.Damage, characterClass);
                statProgression[Stat.Damage] = CreateDiabloProgression(baseDamage, baseDamage * 15f, levels);
            }
            else
            {
                float baseDamage = GetRecommendedBaseValue(Stat.Damage, characterClass);
                statProgression[Stat.Damage] = CreatePolynomialProgression(baseDamage, baseDamage * 0.05f, 1.5f, levels);
            }
            
            // Защита - медленный рост для большинства
            if (ClassStatMultipliers.ContainsKey(characterClass) && 
                ClassStatMultipliers[characterClass].ContainsKey(Stat.Defence))
            {
                float baseDefence = GetRecommendedBaseValue(Stat.Defence, characterClass);
                statProgression[Stat.Defence] = characterClass == CharacterClass.Player 
                    ? CreateDarkSoulsProgression(baseDefence, 0.2f, 5f, levels)
                    : CreateLinearProgression(baseDefence, baseDefence * 0.04f, levels);
            }
            
            // Опыт для повышения уровня - только для игрока
            if (characterClass == CharacterClass.Player)
            {
                float baseXP = 1000f; // Базовое количество опыта для первого уровня
                statProgression[Stat.ExperienceToLevelUp] = CreateFinalFantasyXPProgression(baseXP, 1.2f, levels);
            }
            
            // Опыт за убийство - только для врагов
            if (characterClass != CharacterClass.Player && characterClass != CharacterClass.Chest)
            {
                float baseXPReward = levels > 0 ? statProgression[Stat.Health][0] * 0.5f : 50f;
                statProgression[Stat.ExperienceReward] = new float[levels];
                
                for (int i = 0; i < levels; i++)
                {
                    // XP награда пропорциональна здоровью на данном уровне
                    statProgression[Stat.ExperienceReward][i] = statProgression.ContainsKey(Stat.Health) 
                        ? statProgression[Stat.Health][i] * 0.5f
                        : baseXPReward * (i + 1);
                }
            }
            
            // Скорость регенерации маны - только для классов с маной
            if (statProgression.ContainsKey(Stat.Mana))
            {
                float baseManaRegen = GetRecommendedBaseValue(Stat.ManaRegenRate, characterClass);
                statProgression[Stat.ManaRegenRate] = CreateLinearProgression(baseManaRegen, baseManaRegen * 0.1f, levels);
            }
            
            // Очки талантов - только для игрока
            if (characterClass == CharacterClass.Player)
            {
                float[] traitPoints = new float[levels];
                for (int i = 0; i < levels; i++)
                {
                    // Каждые 3 уровня дают дополнительное очко талантов
                    traitPoints[i] = i > 0 && (i + 1) % 3 == 0 ? 2 : 1;
                }
                statProgression[Stat.TotalTraitPoints] = traitPoints;
            }
            
            // Скидка при покупке - только для игрока
            if (characterClass == CharacterClass.Player)
            {
                float[] discount = new float[levels];
                for (int i = 0; i < levels; i++)
                {
                    // Скидка увеличивается с уровнем, но не больше 30%
                    discount[i] = Mathf.Min(30f, i * 0.5f);
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