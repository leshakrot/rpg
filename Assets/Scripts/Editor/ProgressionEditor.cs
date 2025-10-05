using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using RPG.Stats;

namespace RPG.Editor
{
    [CustomEditor(typeof(Progression))]
    public class ProgressionEditor : UnityEditor.Editor
    {
        private SerializedProperty progressionDataProperty;
        private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _statStyle;
        private GUIStyle _levelStyle;
        private Color _originalBackgroundColor;
        
        // Параметры для генерации значений (используются только для контекстного меню)
        private float _startValue = 100f;
        private float _increment = 20f;
        private float _growthFactor = 1.2f;
        private float _power = 2f;
        private float _endValue = 800f;
        private float _amplitude = 50f;
        private float _frequency = 2f;
        private float _levelOffset = 5f;
        private int _generationLevels = 18;
        private int _selectedProgressionType = 0;
        private string[] _progressionTypes = { 
            "Линейная", 
            "Экспоненциальная", 
            "Полиномиальная", 
            "Логарифмическая",
            "Diablo-стиль",
            "Dark Souls-стиль",
            "Path of Exile-стиль",
            "Final Fantasy-стиль",
            "Волнообразная"
        };
        
        private CharacterClass _selectedAutoGenClass = CharacterClass.Player;
        private bool _showAutoGenSettings = true; // По умолчанию открыто
        
        // Словарь множителей сложности для каждого класса
        private Dictionary<CharacterClass, float> _difficultyMultipliers = new Dictionary<CharacterClass, float>()
        {
            { CharacterClass.Grunt, 1.0f },
            { CharacterClass.Mage, 1.1f },
            { CharacterClass.Archer, 1.0f },
            { CharacterClass.Orc, 1.2f },
            { CharacterClass.Wolf, 0.8f },
            { CharacterClass.Boar, 0.9f },
            { CharacterClass.Chest, 1.0f },
        };
        
        // Аннотации для ползунков сложности
        private string[] _difficultyLabels = { "Очень легко", "Легко", "Нормально", "Сложно", "Очень сложно" };
        private Color[] _difficultyColors = {
            new Color(0.5f, 1.0f, 0.5f), // Зеленый - легко
            new Color(0.7f, 1.0f, 0.7f), // Светло-зеленый
            new Color(1.0f, 1.0f, 0.6f), // Желтый - средне
            new Color(1.0f, 0.7f, 0.7f), // Светло-красный
            new Color(1.0f, 0.5f, 0.5f)  // Красный - сложно
        };
        
        // Настройки глобальной сложности игры
        private float _globalDifficultyMultiplier = 1.0f;
        private bool _showGlobalDifficultySettings = true;
        private int _selectedDifficultyPreset = 2; // По умолчанию "Нормально"
        private string[] _difficultyPresets = { "Очень легко", "Легко", "Нормально", "Сложно", "Хардкор", "Кошмар", "Невозможно" };
        private float[] _presetValues = { 0.7f, 0.85f, 1.0f, 1.25f, 1.5f, 2.0f, 2.5f };
        private Dictionary<string, float> _difficultySettings = new Dictionary<string, float>()
        {
            { "Здоровье врагов", 1.0f },
            { "Урон врагов", 1.0f },
            { "Защита врагов", 1.0f },
            { "Опыт за врагов", 1.0f },
            { "Опыт для повышения уровня", 1.0f },
            { "Шанс уклонения врагов", 1.0f },
            { "Шанс критического удара врагов", 1.0f },
            { "Шанс парирования врагов", 1.0f }
        };
        
        private void OnEnable()
        {
            progressionDataProperty = serializedObject.FindProperty("progressionData");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            InitializeStyles();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Настройки прогрессии персонажей", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            ShowGlobalDifficultySettings();
            ShowGlobalAutoGenerationButton();
            ShowAutoGenerationTools();
            
            EditorGUILayout.Space(10);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Отображаем все записи прогрессии
            for (int i = 0; i < progressionDataProperty.arraySize; i++)
            {
                SerializedProperty entryProperty = progressionDataProperty.GetArrayElementAtIndex(i);
                SerializedProperty characterClassProperty = entryProperty.FindPropertyRelative("characterClass");
                SerializedProperty statProperty = entryProperty.FindPropertyRelative("stat");
                SerializedProperty valuesProperty = entryProperty.FindPropertyRelative("values");
                
                string className = characterClassProperty.enumDisplayNames[characterClassProperty.enumValueIndex];
                string statName = statProperty.enumDisplayNames[statProperty.enumValueIndex];
                
                // Создаем уникальный ключ для foldout
                string foldoutKey = $"Entry_{i}_{className}_{statName}";
                if (!_foldoutStates.ContainsKey(foldoutKey))
                {
                    _foldoutStates[foldoutKey] = false;
                }
                
                // Рисуем заголовок записи с цветом фона
                GUI.backgroundColor = GetClassColor(characterClassProperty.enumValueIndex);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = _originalBackgroundColor;
                
                EditorGUILayout.BeginHorizontal();
                
                _foldoutStates[foldoutKey] = EditorGUILayout.Foldout(
                    _foldoutStates[foldoutKey], 
                    $"{className} - {statName}", 
                    true, 
                    _headerStyle);
                
                GUILayout.FlexibleSpace();
                
                // Кнопка генерации значений
                if (GUILayout.Button("Сгенерировать", GUILayout.Width(100)))
                {
                    ShowGenerateValuesContextMenu(valuesProperty);
                }
                
                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    InsertEntryAt(i);
                    break;
                }
                
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    RemoveEntryAt(i);
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (_foldoutStates[foldoutKey])
                {
                    // Редактирование класса персонажа и статистики
                    EditorGUILayout.PropertyField(characterClassProperty);
                    EditorGUILayout.PropertyField(statProperty);
                    
                    EditorGUILayout.Space(5);
                    
                    // Отображаем значения для разных уровней
                    EditorGUILayout.LabelField("Значения по уровням:");
                    
                    EditorGUILayout.BeginVertical(_levelStyle);
                    
                    // Кнопка добавления уровня
                    if (GUILayout.Button("Добавить уровень"))
                    {
                        valuesProperty.arraySize++;
                        serializedObject.ApplyModifiedProperties();
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    // Поля для ввода значений
                    for (int levelIndex = 0; levelIndex < valuesProperty.arraySize; levelIndex++)
                    {
                        SerializedProperty levelProperty = valuesProperty.GetArrayElementAtIndex(levelIndex);
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Уровень {levelIndex + 1}:", GUILayout.Width(80));
                        levelProperty.floatValue = EditorGUILayout.FloatField(levelProperty.floatValue);
                        
                        if (GUILayout.Button("Х", GUILayout.Width(25)))
                        {
                            valuesProperty.DeleteArrayElementAtIndex(levelIndex);
                            serializedObject.ApplyModifiedProperties();
                            break;
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // Кнопка добавления новой записи
            if (GUILayout.Button("Добавить запись прогрессии"))
            {
                AddProgressionEntry();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void ShowGlobalAutoGenerationButton()
        {
            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.6f, 0.8f, 1.0f); // Голубой для выделения
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fontSize = 12;
            buttonStyle.padding = new RectOffset(10, 10, 8, 8);
            
            if (GUILayout.Button("СГЕНЕРИРОВАТЬ ВСЕ КЛАССЫ", buttonStyle, GUILayout.Height(40)))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Автогенерация всех классов",
                    "Это действие заменит прогрессию для всех классов персонажей с учетом текущих настроек сложности. Продолжить?",
                    "Да, сгенерировать все", "Отмена");
                
                if (confirm)
                {
                    GenerateAllClasses();
                }
            }
            
            GUI.backgroundColor = _originalBackgroundColor;
            EditorGUILayout.Space(5);
        }
        
        private void GenerateAllClasses()
        {
            // Передаем глобальные настройки сложности в ProgressionUtility
            ProgressionUtility.SetGlobalDifficultySettings(_difficultySettings);
            
            // Сначала сгенерируем игрока
            AddClassWithAutoStats(CharacterClass.Player, 1.0f);
            
            // Затем всех остальных с учетом множителей сложности
            foreach (CharacterClass characterClass in System.Enum.GetValues(typeof(CharacterClass)))
            {
                if (characterClass != CharacterClass.Player)
                {
                    AddClassWithAutoStats(characterClass, _difficultyMultipliers[characterClass]);
                }
            }
            
            // Обновляем кэш после генерации всех классов
            Progression progression = (Progression)target;
            progression.ForceUpdateCache();
            
            Debug.Log("Успешно сгенерированы все классы персонажей с учетом настроек сложности");
        }
        
        private void ShowAutoGenerationTools()
        {
            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.9f, 0.9f, 1.0f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = _originalBackgroundColor;
            
            _showAutoGenSettings = EditorGUILayout.Foldout(_showAutoGenSettings, "Настройки автогенерации и сложности", true, EditorStyles.foldoutHeader);
            
            if (_showAutoGenSettings)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Количество уровней", GUILayout.Width(150));
                _generationLevels = EditorGUILayout.IntSlider(_generationLevels, 1, 100);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Настройка сложности врагов относительно игрока", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Используйте ползунки для настройки сложности каждого класса врагов. Значение 1.0 - стандартный баланс, значения выше делают врагов сильнее, ниже - слабее.", MessageType.Info);
                EditorGUILayout.Space(5);
                
                // Добавляем пояснение о силе врагов
                EditorGUILayout.HelpBox(
                    "Приблизительная сила классов:\n" +
                    "• Игрок = 3 гранта / 2.8 лучника / 2.5 мага / 2 орка / 4 волка / 3.5 кабана\n" +
                    "• Орк сильнее других врагов, но медленнее\n" +
                    "• Маг имеет высокий урон, но низкую защиту\n" +
                    "• Волки и кабаны слабее, но часто встречаются группами",
                    MessageType.Info);
                
                EditorGUILayout.Space(5);
                
                // Ползунки сложности для каждого класса с улучшенной визуализацией
                foreach (CharacterClass enemyClass in System.Enum.GetValues(typeof(CharacterClass)))
                {
                    // Пропускаем игрока, у него нет настройки сложности
                    if (enemyClass == CharacterClass.Player) continue;
                    
                    // Если значения сложности нет, устанавливаем стандартное
                    if (!_difficultyMultipliers.ContainsKey(enemyClass))
                    {
                        _difficultyMultipliers[enemyClass] = 1.0f;
                    }
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    string className = System.Enum.GetName(typeof(CharacterClass), enemyClass);
                    EditorGUILayout.LabelField($"Класс: {className}", GUILayout.Width(150));
                    
                    // Определяем цвет полосы в зависимости от сложности
                    float difficulty = _difficultyMultipliers[enemyClass];
                    Color sliderColor = GetDifficultyColor(difficulty);
                    
                    // Применяем цвет
                    GUI.color = sliderColor;
                    
                    // Ползунок сложности
                    float newDifficulty = EditorGUILayout.Slider(_difficultyMultipliers[enemyClass], 0.5f, 2.0f);
                    
                    // Возвращаем стандартный цвет
                    GUI.color = Color.white;
                    
                    if (newDifficulty != _difficultyMultipliers[enemyClass])
                    {
                        _difficultyMultipliers[enemyClass] = newDifficulty;
                        GUI.changed = true;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Отображаем текстовое описание сложности и примерный баланс
                    string difficultyLabel = GetDifficultyLabel(difficulty);
                    EditorGUILayout.LabelField($"Уровень сложности: {difficultyLabel} ({difficulty:F2}x)", EditorStyles.miniLabel);
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Сбросить сложность"))
                {
                    ResetDifficultyToDefaults();
                }
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Игрок сильный"))
                {
                    SetPlayerStrengthPreset(0.8f); // Слабые враги = сильный игрок
                }
                
                if (GUILayout.Button("Средний баланс"))
                {
                    ResetDifficultyToDefaults();
                }
                
                if (GUILayout.Button("Игрок слабый"))
                {
                    SetPlayerStrengthPreset(1.2f); // Сильные враги = слабый игрок
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField("Класс для автогенерации", GUILayout.Width(150));
                _selectedAutoGenClass = (CharacterClass)EditorGUILayout.EnumPopup(_selectedAutoGenClass);
                
                if (GUILayout.Button("Сгенерировать класс"))
                {
                    float difficultyMultiplier = _selectedAutoGenClass == CharacterClass.Player ? 
                        1.0f : _difficultyMultipliers[_selectedAutoGenClass];
                    
                    AddClassWithAutoStats(_selectedAutoGenClass, difficultyMultiplier);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ResetDifficultyToDefaults()
        {
            _difficultyMultipliers[CharacterClass.Grunt] = 1.2f;
            _difficultyMultipliers[CharacterClass.Mage] = 1.3f;
            _difficultyMultipliers[CharacterClass.Archer] = 1.15f;
            _difficultyMultipliers[CharacterClass.Orc] = 1.4f;
            _difficultyMultipliers[CharacterClass.Wolf] = 1.0f;
            _difficultyMultipliers[CharacterClass.Boar] = 1.1f;
            _difficultyMultipliers[CharacterClass.Chest] = 1.0f;
            GUI.changed = true;
        }
        
        private void SetPlayerStrengthPreset(float multiplier)
        {
            foreach (CharacterClass enemyClass in System.Enum.GetValues(typeof(CharacterClass)))
            {
                if (enemyClass != CharacterClass.Player)
                {
                    // Применяем общий множитель, сохраняя относительные соотношения между врагами
                    float baseMultiplier = GetDefaultDifficultyForClass(enemyClass);
                    _difficultyMultipliers[enemyClass] = baseMultiplier * multiplier;
                }
            }
            GUI.changed = true;
        }
        
        private float GetDefaultDifficultyForClass(CharacterClass characterClass)
        {
            switch (characterClass)
            {
                case CharacterClass.Grunt: return 1.2f;
                case CharacterClass.Mage: return 1.3f;
                case CharacterClass.Archer: return 1.15f;
                case CharacterClass.Orc: return 1.4f;
                case CharacterClass.Wolf: return 1.0f;
                case CharacterClass.Boar: return 1.1f;
                case CharacterClass.Chest: return 1.0f;
                default: return 1.0f;
            }
        }
        
        private void ShowGenerateValuesContextMenu(SerializedProperty levelsProperty)
        {
            GenericMenu menu = new GenericMenu();
            
            for (int i = 0; i < _progressionTypes.Length; i++)
            {
                int type = i; // захватываем индекс
                menu.AddItem(new GUIContent(_progressionTypes[i]), false, () => GenerateValues(levelsProperty, type));
            }
            
            menu.ShowAsContext();
        }
        
        private void GenerateValues(SerializedProperty levelsProperty, int progressionType)
        {
            float[] values = null;
            
            // Сохраняем выбранный тип для будущего использования
            _selectedProgressionType = progressionType;
            
            switch (progressionType)
            {
                case 0: // Линейная
                    values = ProgressionUtility.CreateLinearProgression(_startValue, _increment, _generationLevels);
                    break;
                case 1: // Экспоненциальная
                    values = ProgressionUtility.CreateExponentialProgression(_startValue, _growthFactor, _generationLevels);
                    break;
                case 2: // Полиномиальная
                    values = ProgressionUtility.CreatePolynomialProgression(_startValue, _increment, _power, _generationLevels);
                    break;
                case 3: // Логарифмическая
                    values = ProgressionUtility.CreateLogarithmicProgression(_startValue, _increment, _generationLevels);
                    break;
                case 4: // Diablo-стиль
                    values = ProgressionUtility.CreateDiabloProgression(_startValue, _endValue, _generationLevels);
                    break;
                case 5: // Dark Souls-стиль
                    values = ProgressionUtility.CreateDarkSoulsProgression(_startValue, _increment, _levelOffset, _generationLevels);
                    break;
                case 6: // Path of Exile-стиль
                    values = ProgressionUtility.CreatePathOfExileHealthProgression(_startValue, _increment, _generationLevels);
                    break;
                case 7: // Final Fantasy-стиль
                    values = ProgressionUtility.CreateFinalFantasyXPProgression(_startValue, _growthFactor, _generationLevels);
                    break;
                case 8: // Волнообразная
                    values = ProgressionUtility.CreateWavyProgression(_startValue, _amplitude, _frequency, _growthFactor, _generationLevels);
                    break;
            }
            
            if (values != null)
            {
                levelsProperty.arraySize = values.Length;
                
                for (int i = 0; i < values.Length; i++)
                {
                    SerializedProperty levelProperty = levelsProperty.GetArrayElementAtIndex(i);
                    levelProperty.floatValue = values[i];
                }
                
                serializedObject.ApplyModifiedProperties();
                
                // Обновляем кэш в Progression после генерации
                Progression progression = (Progression)target;
                progression.ForceUpdateCache();
            }
        }
        
        private void InitializeStyles()
        {
            _originalBackgroundColor = GUI.backgroundColor;
            
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.foldout);
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.fontSize = 12;
            }
            
            if (_statStyle == null)
            {
                _statStyle = new GUIStyle(EditorStyles.foldout);
                _statStyle.fontStyle = FontStyle.Normal;
                _statStyle.fontSize = 11;
            }
            
            if (_levelStyle == null)
            {
                _levelStyle = new GUIStyle(EditorStyles.helpBox);
                _levelStyle.padding = new RectOffset(5, 5, 5, 5);
                _levelStyle.margin = new RectOffset(0, 0, 0, 0);
            }
        }
        
        private Color GetClassColor(int classType)
        {
            // Уникальные цвета для разных классов
            switch (classType)
            {
                case 0: return new Color(0.8f, 0.8f, 1.0f); // Player - голубой
                case 1: return new Color(1.0f, 0.8f, 0.8f); // Grunt - красный
                case 2: return new Color(0.8f, 0.8f, 1.0f); // Mage - голубой
                case 3: return new Color(0.8f, 1.0f, 0.8f); // Archer - зеленый
                case 4: return new Color(1.0f, 0.9f, 0.8f); // Orc - оранжевый
                case 5: return new Color(0.9f, 0.9f, 0.9f); // Wolf - серый
                case 6: return new Color(1.0f, 1.0f, 0.8f); // Boar - желтый
                case 7: return new Color(0.8f, 1.0f, 1.0f); // Chest - бирюзовый
                default: return new Color(0.9f, 0.9f, 0.9f); // По умолчанию серый
            }
        }
        
        private void AddProgressionEntry()
        {
            progressionDataProperty.arraySize++;
            int newIndex = progressionDataProperty.arraySize - 1;
            SerializedProperty newEntryProperty = progressionDataProperty.GetArrayElementAtIndex(newIndex);
            SerializedProperty newClassProperty = newEntryProperty.FindPropertyRelative("characterClass");
            SerializedProperty newStatProperty = newEntryProperty.FindPropertyRelative("stat");
            SerializedProperty newValuesProperty = newEntryProperty.FindPropertyRelative("values");
            
            // Устанавливаем значения по умолчанию
            newClassProperty.enumValueIndex = 0;
            newStatProperty.enumValueIndex = 0;
            newValuesProperty.arraySize = 1;
            newValuesProperty.GetArrayElementAtIndex(0).floatValue = 10f;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void InsertEntryAt(int index)
        {
            progressionDataProperty.InsertArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }
        
        private void RemoveEntryAt(int index)
        {
            progressionDataProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }
        
        private void AutoGenerateForClass(CharacterClass characterClass, float difficultyMultiplier = 1.0f)
        {
            // Получаем полную прогрессию для класса с учетом множителя сложности
            Dictionary<Stat, float[]> statProgression = ProgressionUtility.GenerateFullStatProgression(characterClass, _generationLevels, difficultyMultiplier);
            
            // Удаляем существующие записи для этого класса
            for (int i = progressionDataProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty entryProperty = progressionDataProperty.GetArrayElementAtIndex(i);
                SerializedProperty entryClassProperty = entryProperty.FindPropertyRelative("characterClass");
                
                if (entryClassProperty.enumValueIndex == (int)characterClass)
                {
                    progressionDataProperty.DeleteArrayElementAtIndex(i);
                }
            }
            
            // Добавляем новые записи
            foreach (var pair in statProgression)
            {
                if (pair.Value == null || pair.Value.Length == 0)
                    continue;
                
                progressionDataProperty.arraySize++;
                int newIndex = progressionDataProperty.arraySize - 1;
                SerializedProperty newEntryProperty = progressionDataProperty.GetArrayElementAtIndex(newIndex);
                SerializedProperty newClassProperty = newEntryProperty.FindPropertyRelative("characterClass");
                SerializedProperty newStatProperty = newEntryProperty.FindPropertyRelative("stat");
                SerializedProperty newValuesProperty = newEntryProperty.FindPropertyRelative("values");
                
                // Устанавливаем значения
                newClassProperty.enumValueIndex = (int)characterClass;
                newStatProperty.enumValueIndex = (int)pair.Key;
                
                newValuesProperty.arraySize = pair.Value.Length;
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    newValuesProperty.GetArrayElementAtIndex(i).floatValue = pair.Value[i];
                }
            }
            
            serializedObject.ApplyModifiedProperties();
            
            // Обновляем кэш в Progression
            Progression progression = (Progression)target;
            progression.ForceUpdateCache();
            
            Debug.Log($"Автоматически сгенерированы статистики для класса {characterClass} с множителем сложности {difficultyMultiplier:F2}");
        }
        
        private void AddClassWithAutoStats(CharacterClass characterClass, float difficultyMultiplier = 1.0f)
        {
            // Автоматически генерируем статистики с учетом множителя сложности
            AutoGenerateForClass(characterClass, difficultyMultiplier);
        }
        
        // Метод для получения цвета в зависимости от уровня сложности
        private Color GetDifficultyColor(float difficulty)
        {
            if (difficulty <= 0.7f) return _difficultyColors[0]; // Очень легко
            if (difficulty <= 0.9f) return _difficultyColors[1]; // Легко
            if (difficulty <= 1.1f) return _difficultyColors[2]; // Средне
            if (difficulty <= 1.5f) return _difficultyColors[3]; // Сложно
            return _difficultyColors[4]; // Очень сложно
        }
        
        // Метод для получения текстового описания сложности
        private string GetDifficultyLabel(float difficultyValue)
        {
            if (difficultyValue <= 0.75f)
            {
                return "Очень легко";
            }
            else if (difficultyValue <= 0.9f)
            {
                return "Легко";
            }
            else if (difficultyValue <= 1.1f)
            {
                return "Нормально";
            }
            else if (difficultyValue <= 1.4f)
            {
                return "Сложно";
            }
            else if (difficultyValue <= 1.75f)
            {
                return "Хардкор";
            }
            else if (difficultyValue <= 2.2f)
            {
                return "Кошмар";
            }
            else
            {
                return "Невозможно";
            }
        }
        
        private void ShowGlobalDifficultySettings()
        {
            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(1.0f, 0.8f, 0.8f); // Светло-красный для выделения
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = _originalBackgroundColor;
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            
            EditorGUILayout.LabelField("ОБЩАЯ СЛОЖНОСТЬ ИГРЫ", headerStyle);
            EditorGUILayout.Space(5);
            
            _showGlobalDifficultySettings = EditorGUILayout.Foldout(_showGlobalDifficultySettings, "Настройки общей сложности", true, EditorStyles.foldoutHeader);
            
            if (_showGlobalDifficultySettings)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Общая сложность игры влияет на все аспекты баланса. Выберите предустановку или настройте вручную.", MessageType.Info);
                EditorGUILayout.Space(5);
                
                // Секция предустановок сложности
                EditorGUILayout.LabelField("Предустановки сложности", EditorStyles.boldLabel);
                
                // Отрисовка кнопок предустановок
                EditorGUILayout.BeginHorizontal();
                
                for (int i = 0; i < _difficultyPresets.Length - 1; i++)
                {
                    GUIStyle presetButtonStyle = new GUIStyle(GUI.skin.button);
                    
                    // Выделяем выбранную предустановку
                    if (i == _selectedDifficultyPreset)
                    {
                        presetButtonStyle.normal.background = CreateColorTexture(new Color(0.7f, 0.9f, 1.0f));
                        presetButtonStyle.hover.background = CreateColorTexture(new Color(0.8f, 0.95f, 1.0f));
                        presetButtonStyle.fontStyle = FontStyle.Bold;
                    }
                    
                    if (GUILayout.Button(_difficultyPresets[i], presetButtonStyle))
                    {
                        _selectedDifficultyPreset = i;
                        _globalDifficultyMultiplier = _presetValues[i];
                        ApplyGlobalDifficultyPreset(i);
                        GUI.changed = true;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Блок экстремальной сложности с выделением
                EditorGUILayout.Space(10);
                
                // Создаем выделенный блок с красным фоном для экстремальных режимов
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = _originalBackgroundColor;
                
                GUIStyle extremeHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                extremeHeaderStyle.fontSize = 13;
                extremeHeaderStyle.alignment = TextAnchor.MiddleCenter;
                extremeHeaderStyle.normal.textColor = new Color(0.9f, 0.2f, 0.2f);
                
                EditorGUILayout.LabelField("ПОВЫШЕННАЯ СЛОЖНОСТЬ", extremeHeaderStyle);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.HelpBox("Режимы повышенной сложности предназначены для опытных игроков. Враги будут значительно сильнее.", MessageType.Warning);
                
                // Блок кнопок "Кошмар" и "Невозможно"
                EditorGUILayout.BeginHorizontal();
                
                // Стиль кнопки Кошмар
                GUIStyle nightmareButtonStyle = new GUIStyle(GUI.skin.button);
                nightmareButtonStyle.fontStyle = FontStyle.Bold;
                nightmareButtonStyle.normal.textColor = Color.white;
                nightmareButtonStyle.hover.textColor = Color.white;
                nightmareButtonStyle.normal.background = CreateColorTexture(new Color(0.6f, 0.0f, 0.0f));
                nightmareButtonStyle.hover.background = CreateColorTexture(new Color(0.7f, 0.1f, 0.1f));
                
                if (_selectedDifficultyPreset == 5)
                {
                    nightmareButtonStyle.normal.background = CreateColorTexture(new Color(0.7f, 0.2f, 0.2f));
                    nightmareButtonStyle.hover.background = CreateColorTexture(new Color(0.8f, 0.3f, 0.3f));
                }
                
                if (GUILayout.Button("КОШМАР", nightmareButtonStyle, GUILayout.Height(30)))
                {
                    bool confirm = EditorUtility.DisplayDialog(
                        "Внимание! Режим Кошмар",
                        "Вы собираетесь установить режим \"Кошмар\".\n\nЭта сложность предназначена для опытных игроков. Враги будут наносить большой урон и иметь высокое здоровье.\n\nВы уверены?",
                        "Да, я готов к испытанию", "Отмена");
                    
                    if (confirm)
                    {
                        _selectedDifficultyPreset = 5;
                        _globalDifficultyMultiplier = _presetValues[5];
                        ApplyGlobalDifficultyPreset(5);
                        GUI.changed = true;
                    }
                }
                
                // Стиль кнопки Невозможно (еще более яркий и угрожающий)
                GUIStyle impossibleButtonStyle = new GUIStyle(nightmareButtonStyle);
                impossibleButtonStyle.normal.background = CreateColorTexture(new Color(0.5f, 0.0f, 0.0f));
                impossibleButtonStyle.hover.background = CreateColorTexture(new Color(0.6f, 0.1f, 0.1f));
                
                if (_selectedDifficultyPreset == 6)
                {
                    impossibleButtonStyle.normal.background = CreateColorTexture(new Color(0.65f, 0.0f, 0.0f));
                    impossibleButtonStyle.hover.background = CreateColorTexture(new Color(0.75f, 0.1f, 0.1f));
                }
                
                if (GUILayout.Button("НЕВОЗМОЖНО", impossibleButtonStyle, GUILayout.Height(30)))
                {
                    bool confirm = EditorUtility.DisplayDialog(
                        "ВНИМАНИЕ! Экстремальная сложность",
                        "Вы собираетесь установить режим \"Невозможно\".\n\nЭта сложность создаст невероятно сложный вызов даже для самых опытных игроков. Враги будут наносить огромный урон, иметь высокое здоровье и отличные параметры уклонения, критических ударов и парирования.\n\nВы уверены?",
                        "Да, сделать игру безжалостной", "Отмена");
                    
                    if (confirm)
                    {
                        // Дополнительное предупреждение
                        bool secondConfirm = EditorUtility.DisplayDialog(
                            "ПОСЛЕДНЕЕ ПРЕДУПРЕЖДЕНИЕ",
                            "Этот режим сложности действительно очень жесткий.\n\nВы уверены?",
                            "ДА, Я ГОТОВ", "Я передумал");
                        
                        if (secondConfirm)
                        {
                            _selectedDifficultyPreset = 6; // Индекс режима Невозможно
                            _globalDifficultyMultiplier = _presetValues[6];
                            ApplyGlobalDifficultyPreset(6);
                            GUI.changed = true;
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(10);
                
                // Ползунок общей сложности
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Общий множитель сложности:", GUILayout.Width(180));
                
                // Определяем цвет полосы в зависимости от сложности
                Color sliderColor = GetDifficultyColor(_globalDifficultyMultiplier);
                GUI.color = sliderColor;
                
                float newGlobalDifficulty = EditorGUILayout.Slider(_globalDifficultyMultiplier, 0.5f, 2.0f);
                
                GUI.color = Color.white;
                
                if (newGlobalDifficulty != _globalDifficultyMultiplier)
                {
                    _globalDifficultyMultiplier = newGlobalDifficulty;
                    
                    // Автоматически выбираем ближайшую предустановку или "Пользовательскую"
                    _selectedDifficultyPreset = FindClosestPreset(newGlobalDifficulty);
                    
                    // Применяем изменение ко всем параметрам сложности
                    ScaleAllDifficultySettings(newGlobalDifficulty);
                    GUI.changed = true;
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Отображаем текстовое описание сложности
                string difficultyLabel = GetDifficultyLabel(_globalDifficultyMultiplier);
                EditorGUILayout.LabelField($"Уровень сложности: {difficultyLabel} ({_globalDifficultyMultiplier:F2}x)", EditorStyles.miniLabel);
                
                EditorGUILayout.Space(10);
                
                // Детальные настройки сложности
                EditorGUILayout.LabelField("Детальные настройки", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Данные настройки позволяют более тонко регулировать отдельные аспекты игры.", MessageType.Info);
                
                // Создаем временный список для сортировки ключей (чтобы порядок был предсказуемым)
                List<string> sortedKeys = new List<string>(_difficultySettings.Keys);
                
                foreach (string setting in sortedKeys)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(setting + ":", GUILayout.Width(180));
                    
                    // Определяем цвет полосы
                    float value = _difficultySettings[setting];
                    Color paramSliderColor = GetDifficultyColor(value);
                    GUI.color = paramSliderColor;
                    
                    float newValue = EditorGUILayout.Slider(value, 0.5f, 2.0f);
                    
                    GUI.color = Color.white;
                    
                    if (newValue != value)
                    {
                        _difficultySettings[setting] = newValue;
                        GUI.changed = true;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space(10);
                
                // Кнопка применения настроек
                GUI.backgroundColor = new Color(0.8f, 1.0f, 0.8f); // Зеленоватый
                if (GUILayout.Button("Применить настройки сложности", GUILayout.Height(30)))
                {
                    ApplyGlobalDifficultySettings();
                    GUI.changed = true;
                }
                GUI.backgroundColor = _originalBackgroundColor;
                
                EditorGUILayout.Space(5);
                
                // Показываем дополнительные настройки для режимов высокой сложности
                ShowExtraDifficultySettings();
                
                // Пояснения по влиянию настроек
                EditorGUILayout.HelpBox(
                    "Влияние настроек на игровой процесс:\n" +
                    "• Увеличение здоровья врагов делает бои длиннее\n" +
                    "• Увеличение урона врагов требует большей осторожности\n" +
                    "• Увеличение защиты врагов снижает эффективность атак игрока\n" +
                    "• Увеличение опыта за врагов ускоряет прогресс игрока\n" +
                    "• Увеличение требуемого опыта замедляет прогресс игрока\n" +
                    "• Увеличение шанса уклонения повышает вероятность, что враг полностью избежит атаки\n" +
                    "• Увеличение шанса критического удара делает атаки врагов более опасными\n" +
                    "• Увеличение шанса парирования позволяет врагам блокировать атаки и контратаковать",
                    MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private int FindClosestPreset(float value)
        {
            float minDistance = float.MaxValue;
            int closestIndex = 0;
            
            for (int i = 0; i < _presetValues.Length; i++)
            {
                float distance = Mathf.Abs(_presetValues[i] - value);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }
            
            // Если расстояние слишком большое, то это "пользовательская" настройка
            return minDistance < 0.05f ? closestIndex : _selectedDifficultyPreset;
        }
        
        private void ApplyGlobalDifficultyPreset(int presetIndex)
        {
            float multiplier = _presetValues[presetIndex];
            _globalDifficultyMultiplier = multiplier;
            
            // Устанавливаем значения для всех параметров в зависимости от выбранной предустановки
            switch (presetIndex)
            {
                case 0: // Новичок (раньше был Легко)
                    _difficultySettings["Здоровье врагов"] = 0.9f;
                    _difficultySettings["Урон врагов"] = 0.85f;
                    _difficultySettings["Защита врагов"] = 0.9f;
                    _difficultySettings["Опыт за врагов"] = 1.1f;
                    _difficultySettings["Опыт для повышения уровня"] = 0.9f;
                    _difficultySettings["Шанс уклонения врагов"] = 0.7f;
                    _difficultySettings["Шанс критического удара врагов"] = 0.7f;
                    _difficultySettings["Шанс парирования врагов"] = 0.6f;
                    break;
                case 1: // Легко (раньше был Нормально)
                    _difficultySettings["Здоровье врагов"] = 1.1f;
                    _difficultySettings["Урон врагов"] = 1.1f;
                    _difficultySettings["Защита врагов"] = 1.05f;
                    _difficultySettings["Опыт за врагов"] = 1.0f;
                    _difficultySettings["Опыт для повышения уровня"] = 1.0f;
                    _difficultySettings["Шанс уклонения врагов"] = 1.0f;
                    _difficultySettings["Шанс критического удара врагов"] = 1.0f;
                    _difficultySettings["Шанс парирования врагов"] = 1.0f;
                    break;
                case 2: // Нормально (раньше был Сложно)
                    _difficultySettings["Здоровье врагов"] = 1.3f;
                    _difficultySettings["Урон врагов"] = 1.35f;
                    _difficultySettings["Защита врагов"] = 1.25f;
                    _difficultySettings["Опыт за врагов"] = 0.9f;
                    _difficultySettings["Опыт для повышения уровня"] = 1.15f;
                    _difficultySettings["Шанс уклонения врагов"] = 1.3f;
                    _difficultySettings["Шанс критического удара врагов"] = 1.3f;
                    _difficultySettings["Шанс парирования врагов"] = 1.25f;
                    break;
                case 3: // Сложно (раньше был Хардкор)
                    _difficultySettings["Здоровье врагов"] = 1.6f;
                    _difficultySettings["Урон врагов"] = 1.7f;
                    _difficultySettings["Защита врагов"] = 1.5f;
                    _difficultySettings["Опыт за врагов"] = 0.7f;
                    _difficultySettings["Опыт для повышения уровня"] = 1.3f;
                    _difficultySettings["Шанс уклонения врагов"] = 1.6f;
                    _difficultySettings["Шанс критического удара врагов"] = 1.7f;
                    _difficultySettings["Шанс парирования врагов"] = 1.5f;
                    break;
                case 4: // Хардкор (раньше был Экстрим)
                    _difficultySettings["Здоровье врагов"] = 2.0f;
                    _difficultySettings["Урон врагов"] = 2.2f;
                    _difficultySettings["Защита врагов"] = 1.8f;
                    _difficultySettings["Опыт за врагов"] = 0.6f;
                    _difficultySettings["Опыт для повышения уровня"] = 1.5f;
                    _difficultySettings["Шанс уклонения врагов"] = 2.0f;
                    _difficultySettings["Шанс критического удара врагов"] = 2.0f;
                    _difficultySettings["Шанс парирования врагов"] = 1.8f;
                    break;
                case 5: // Экстрим (улучшенный)
                    _difficultySettings["Здоровье врагов"] = 2.3f;
                    _difficultySettings["Урон врагов"] = 2.5f;
                    _difficultySettings["Защита врагов"] = 2.0f;
                    _difficultySettings["Опыт за врагов"] = 0.55f;
                    _difficultySettings["Опыт для повышения уровня"] = 1.6f;
                    _difficultySettings["Шанс уклонения врагов"] = 2.2f;
                    _difficultySettings["Шанс критического удара врагов"] = 2.2f;
                    _difficultySettings["Шанс парирования врагов"] = 2.0f;
                    break;
                case 6: // Кошмар (увеличенная сложность)
                    _difficultySettings["Здоровье врагов"] = 2.7f;
                    _difficultySettings["Урон врагов"] = 3.0f;
                    _difficultySettings["Защита врагов"] = 2.4f;
                    _difficultySettings["Опыт за врагов"] = 0.45f;
                    _difficultySettings["Опыт для повышения уровня"] = 1.8f;
                    _difficultySettings["Шанс уклонения врагов"] = 2.7f;
                    _difficultySettings["Шанс критического удара врагов"] = 2.7f;
                    _difficultySettings["Шанс парирования врагов"] = 2.4f;
                    break;
            }
            
            // Также обновляем множители сложности для классов
            foreach (CharacterClass enemyClass in System.Enum.GetValues(typeof(CharacterClass)))
            {
                if (enemyClass != CharacterClass.Player)
                {
                    // Базовый множитель для класса
                    float baseMultiplier = GetDefaultDifficultyForClass(enemyClass);
                    // Применяем глобальный множитель
                    _difficultyMultipliers[enemyClass] = baseMultiplier * multiplier;
                }
            }
        }
        
        private void ScaleAllDifficultySettings(float globalMultiplier)
        {
            // Масштабирование относительно нормального уровня (1.0)
            float scaleRatio = globalMultiplier / 1.0f;
            
            // Обновляем множители сложности для классов
            foreach (CharacterClass enemyClass in System.Enum.GetValues(typeof(CharacterClass)))
            {
                if (enemyClass != CharacterClass.Player)
                {
                    float baseMultiplier = GetDefaultDifficultyForClass(enemyClass);
                    _difficultyMultipliers[enemyClass] = baseMultiplier * scaleRatio;
                }
            }
        }
        
        private void ApplyGlobalDifficultySettings()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Применение настроек сложности",
                "Это изменит множители сложности для всех классов врагов в соответствии с выбранными настройками. Продолжить?",
                "Применить", "Отмена");
                
            if (confirm)
            {
                // Передаем настройки сложности в ProgressionUtility перед генерацией
                ProgressionUtility.SetGlobalDifficultySettings(_difficultySettings);
                
                // Вызываем генерацию всех классов с обновленными настройками сложности
                GenerateAllClasses();
                
                Debug.Log("Настройки глобальной сложности успешно применены ко всем классам персонажей");
            }
        }
        
        private void ShowExtraDifficultySettings()
        {
            // Если выбран режим Хардкор или выше, показываем дополнительные настройки бонусов для врагов
            if (_selectedDifficultyPreset >= 4) // Хардкор или выше
            {
                EditorGUILayout.Space(10);
                
                GUI.backgroundColor = new Color(0.8f, 0.5f, 0.5f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = _originalBackgroundColor;
                
                GUIStyle bonusHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                bonusHeaderStyle.fontSize = 12;
                bonusHeaderStyle.alignment = TextAnchor.MiddleCenter;
                
                string headerText = _selectedDifficultyPreset == 6 ? "БОНУСЫ ВРАГОВ (РЕЖИМ КОШМАРА)" : "БОНУСЫ ВРАГОВ (ВЫСОКАЯ СЛОЖНОСТЬ)";
                EditorGUILayout.LabelField(headerText, bonusHeaderStyle);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.HelpBox("Эти настройки активны только в режимах высокой сложности и дают дополнительные преимущества врагам.", MessageType.Info);
                EditorGUILayout.Space(5);
                
                // Отображаем ползунки для настройки бонусов
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Шанс уклонения:", GUILayout.Width(180));
                
                float dodgeValue = _difficultySettings["Шанс уклонения врагов"];
                Color dodgeColor = GetDifficultyColor(dodgeValue);
                GUI.color = dodgeColor;
                float newDodgeValue = EditorGUILayout.Slider(dodgeValue, 1.0f, 2.5f);
                GUI.color = Color.white;
                
                if (newDodgeValue != dodgeValue)
                {
                    _difficultySettings["Шанс уклонения врагов"] = newDodgeValue;
                    GUI.changed = true;
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Описание параметра
                EditorGUILayout.LabelField("Влияет на вероятность того, что враг полностью избежит атаки игрока", EditorStyles.miniLabel);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Критический урон:", GUILayout.Width(180));
                
                float critValue = _difficultySettings["Шанс критического удара врагов"];
                Color critColor = GetDifficultyColor(critValue);
                GUI.color = critColor;
                float newCritValue = EditorGUILayout.Slider(critValue, 1.0f, 2.5f);
                GUI.color = Color.white;
                
                if (newCritValue != critValue)
                {
                    _difficultySettings["Шанс критического удара врагов"] = newCritValue;
                    GUI.changed = true;
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Описание параметра
                EditorGUILayout.LabelField("Влияет на вероятность нанесения врагом критического урона (х1.5 от обычного)", EditorStyles.miniLabel);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Шанс парирования:", GUILayout.Width(180));
                
                float parryValue = _difficultySettings["Шанс парирования врагов"];
                Color parryColor = GetDifficultyColor(parryValue);
                GUI.color = parryColor;
                float newParryValue = EditorGUILayout.Slider(parryValue, 1.0f, 2.5f);
                GUI.color = Color.white;
                
                if (newParryValue != parryValue)
                {
                    _difficultySettings["Шанс парирования врагов"] = newParryValue;
                    GUI.changed = true;
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Описание параметра
                EditorGUILayout.LabelField("Влияет на вероятность того, что враг парирует атаку игрока и нанесет ответный удар", EditorStyles.miniLabel);
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        // Вспомогательный метод для создания текстуры цвета для кнопок
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
} 