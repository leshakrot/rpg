using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using RPG.Stats;

namespace RPG.Editor
{
    [CustomEditor(typeof(Progression))]
    public class ProgressionEditor : UnityEditor.Editor
    {
        private SerializedProperty _characterClassesProperty;
        private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _statStyle;
        private GUIStyle _levelStyle;
        private Color _originalBackgroundColor;
        
        // Параметры для генерации значений
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
        private bool _showAutoGenSettings = false;
        
        private void OnEnable()
        {
            _characterClassesProperty = serializedObject.FindProperty("_characterClasses");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            InitializeStyles();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Настройки прогрессии персонажей", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            ShowGenerationTools();
            ShowAutoGenerationTools();
            
            EditorGUILayout.Space(10);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            for (int classIndex = 0; classIndex < _characterClassesProperty.arraySize; classIndex++)
            {
                SerializedProperty classProperty = _characterClassesProperty.GetArrayElementAtIndex(classIndex);
                SerializedProperty characterClassProperty = classProperty.FindPropertyRelative("characterClass");
                SerializedProperty statsProperty = classProperty.FindPropertyRelative("stats");
                
                string className = characterClassProperty.enumDisplayNames[characterClassProperty.enumValueIndex];
                
                // Создаем уникальный ключ для foldout
                string foldoutKey = $"Class_{classIndex}_{className}";
                if (!_foldoutStates.ContainsKey(foldoutKey))
                {
                    _foldoutStates[foldoutKey] = false;
                }
                
                // Рисуем заголовок класса с цветом фона
                GUI.backgroundColor = GetClassColor(characterClassProperty.enumValueIndex);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = _originalBackgroundColor;
                
                // Добавляем кнопки управления справа от заголовка
                EditorGUILayout.BeginHorizontal();
                _foldoutStates[foldoutKey] = EditorGUILayout.Foldout(_foldoutStates[foldoutKey], 
                    $"Класс: {className}", true, _headerStyle);
                
                GUILayout.FlexibleSpace();
                
                // Кнопка автогенерации для конкретного класса
                if (GUILayout.Button("Автогенерация", GUILayout.Width(110)))
                {
                    AutoGenerateForClass((CharacterClass)characterClassProperty.enumValueIndex, statsProperty);
                }
                
                // Кнопка изменения класса
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(characterClassProperty, GUIContent.none, GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                
                // Кнопки управления классами
                if (GUILayout.Button("+", GUILayout.Width(25)))
                {
                    InsertClassAt(classIndex);
                }
                
                if (GUILayout.Button("-", GUILayout.Width(25)))
                {
                    RemoveClassAt(classIndex);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Если раздел развернут, показываем статистики для класса
                if (_foldoutStates[foldoutKey])
                {
                    if (statsProperty.arraySize == 0)
                    {
                        EditorGUILayout.HelpBox("Нет настроек статистики. Добавьте статистику кнопкой ниже или используйте автогенерацию.", MessageType.Info);
                    }
                    
                    // Отображаем статистики для данного класса
                    for (int statIndex = 0; statIndex < statsProperty.arraySize; statIndex++)
                    {
                        SerializedProperty statProperty = statsProperty.GetArrayElementAtIndex(statIndex);
                        SerializedProperty statTypeProperty = statProperty.FindPropertyRelative("stat");
                        SerializedProperty levelsProperty = statProperty.FindPropertyRelative("levels");
                        
                        string statName = statTypeProperty.enumDisplayNames[statTypeProperty.enumValueIndex];
                        string statFoldoutKey = $"{foldoutKey}_Stat_{statIndex}_{statName}";
                        
                        if (!_foldoutStates.ContainsKey(statFoldoutKey))
                        {
                            _foldoutStates[statFoldoutKey] = false;
                        }
                        
                        // Вложенный блок для статистики
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        // Заголовок статистики
                        EditorGUILayout.BeginHorizontal();
                        _foldoutStates[statFoldoutKey] = EditorGUILayout.Foldout(_foldoutStates[statFoldoutKey], 
                            $"Статистика: {statName}", true, _statStyle);
                        
                        GUILayout.FlexibleSpace();
                        
                        // Предлагаем рекомендуемое начальное значение
                        if (GUILayout.Button("Рекомендуемое значение", GUILayout.Width(150)))
                        {
                            if (levelsProperty.arraySize > 0)
                            {
                                SerializedProperty firstLevelProperty = levelsProperty.GetArrayElementAtIndex(0);
                                firstLevelProperty.floatValue = ProgressionUtility.GetRecommendedBaseValue(
                                    (Stat)statTypeProperty.enumValueIndex, 
                                    (CharacterClass)characterClassProperty.enumValueIndex);
                                serializedObject.ApplyModifiedProperties();
                            }
                        }
                        
                        // Выбор типа статистики
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(statTypeProperty, GUIContent.none, GUILayout.Width(100));
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                        }
                        
                        // Кнопки управления статистиками
                        if (GUILayout.Button("+", GUILayout.Width(25)))
                        {
                            InsertStatAt(statsProperty, statIndex);
                        }
                        
                        if (GUILayout.Button("-", GUILayout.Width(25)))
                        {
                            RemoveStatAt(statsProperty, statIndex);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            break;
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // Если статистика развернута, показываем значения по уровням
                        if (_foldoutStates[statFoldoutKey])
                        {
                            // Ряд кнопок для управления массивом уровней
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Значения статистики по уровням:", EditorStyles.miniLabel);
                            GUILayout.FlexibleSpace();
                            
                            // Кнопка для генерации значений для этой статистики
                            if (GUILayout.Button("Сгенерировать...", GUILayout.Width(120)))
                            {
                                ShowGenerateValuesContextMenu(levelsProperty);
                            }
                            
                            EditorGUI.BeginChangeCheck();
                            int newSize = EditorGUILayout.IntField("Размер", levelsProperty.arraySize, GUILayout.Width(100));
                            if (EditorGUI.EndChangeCheck() && newSize >= 0)
                            {
                                levelsProperty.arraySize = newSize;
                                serializedObject.ApplyModifiedProperties();
                            }
                            
                            if (GUILayout.Button("Добавить уровень", GUILayout.Width(120)))
                            {
                                levelsProperty.arraySize++;
                                serializedObject.ApplyModifiedProperties();
                                
                                // Копируем предыдущее значение, если оно есть
                                if (levelsProperty.arraySize > 1)
                                {
                                    SerializedProperty newLevelProp = levelsProperty.GetArrayElementAtIndex(levelsProperty.arraySize - 1);
                                    SerializedProperty prevLevelProp = levelsProperty.GetArrayElementAtIndex(levelsProperty.arraySize - 2);
                                    newLevelProp.floatValue = prevLevelProp.floatValue;
                                    serializedObject.ApplyModifiedProperties();
                                }
                            }
                            
                            EditorGUILayout.EndHorizontal();
                            
                            // Таблица значений по уровням
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            
                            // Заголовки столбцов
                            EditorGUILayout.BeginHorizontal(_levelStyle);
                            EditorGUILayout.LabelField("Уровень", GUILayout.Width(60));
                            EditorGUILayout.LabelField("Значение");
                            EditorGUILayout.EndHorizontal();
                            
                            // Значения по уровням
                            for (int levelIndex = 0; levelIndex < levelsProperty.arraySize; levelIndex++)
                            {
                                SerializedProperty levelProperty = levelsProperty.GetArrayElementAtIndex(levelIndex);
                                
                                EditorGUILayout.BeginHorizontal(levelIndex % 2 == 0 ? _levelStyle : EditorStyles.inspectorDefaultMargins);
                                EditorGUILayout.LabelField($"Ур. {levelIndex + 1}", GUILayout.Width(60));
                                
                                EditorGUI.BeginChangeCheck();
                                float newValue = EditorGUILayout.FloatField(levelProperty.floatValue);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    levelProperty.floatValue = newValue;
                                    serializedObject.ApplyModifiedProperties();
                                }
                                
                                EditorGUILayout.EndHorizontal();
                            }
                            
                            EditorGUILayout.EndVertical();
                        }
                        
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(5);
                    }
                    
                    // Кнопка для добавления новой статистики
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Добавить статистику", GUILayout.Width(150)))
                    {
                        AddStatToClass(statsProperty);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(10);
            }
            
            // Кнопка для добавления нового класса персонажа
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Добавить класс персонажа", GUILayout.Width(200), GUILayout.Height(30)))
            {
                AddCharacterClass();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void ShowGenerationTools()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Инструменты генерации значений", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Тип прогрессии:", GUILayout.Width(120));
            _selectedProgressionType = EditorGUILayout.Popup(_selectedProgressionType, _progressionTypes);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Начальное значение:", GUILayout.Width(120));
            _startValue = EditorGUILayout.FloatField(_startValue);
            EditorGUILayout.EndHorizontal();
            
            // Показываем параметры в зависимости от выбранного типа прогрессии
            switch (_selectedProgressionType)
            {
                case 0: // Линейная
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Прирост:", GUILayout.Width(120));
                    _increment = EditorGUILayout.FloatField(_increment);
                    EditorGUILayout.EndHorizontal();
                    break;
                
                case 1: // Экспоненциальная
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Множитель роста:", GUILayout.Width(120));
                    _growthFactor = EditorGUILayout.FloatField(_growthFactor);
                    EditorGUILayout.EndHorizontal();
                    break;
                
                case 2: // Полиномиальная
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Коэффициент:", GUILayout.Width(120));
                    _increment = EditorGUILayout.FloatField(_increment);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Степень:", GUILayout.Width(120));
                    _power = EditorGUILayout.FloatField(_power);
                    EditorGUILayout.EndHorizontal();
                    break;
                
                case 3: // Логарифмическая
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Коэффициент:", GUILayout.Width(120));
                    _increment = EditorGUILayout.FloatField(_increment);
                    EditorGUILayout.EndHorizontal();
                    break;
                
                case 4: // Diablo-стиль
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Конечное значение:", GUILayout.Width(120));
                    _endValue = EditorGUILayout.FloatField(_endValue);
                    EditorGUILayout.EndHorizontal();
                    break;
                
                case 5: // Dark Souls-стиль
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Множитель:", GUILayout.Width(120));
                    _increment = EditorGUILayout.FloatField(_increment);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Смещение:", GUILayout.Width(120));
                    _levelOffset = EditorGUILayout.FloatField(_levelOffset);
                    EditorGUILayout.EndHorizontal();
                    break;
                
                case 6: // Path of Exile-стиль
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Прирост за уровень:", GUILayout.Width(120));
                    _increment = EditorGUILayout.FloatField(_increment);
                    EditorGUILayout.EndHorizontal();
                    break;
                
                case 7: // Final Fantasy-стиль
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Множитель роста:", GUILayout.Width(120));
                    _growthFactor = EditorGUILayout.FloatField(_growthFactor);
                    EditorGUILayout.EndHorizontal();
                    break;
                
                case 8: // Волнообразная
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Множитель роста:", GUILayout.Width(120));
                    _growthFactor = EditorGUILayout.FloatField(_growthFactor);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Амплитуда:", GUILayout.Width(120));
                    _amplitude = EditorGUILayout.FloatField(_amplitude);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Частота:", GUILayout.Width(120));
                    _frequency = EditorGUILayout.FloatField(_frequency);
                    EditorGUILayout.EndHorizontal();
                    break;
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Количество уровней:", GUILayout.Width(120));
            _generationLevels = EditorGUILayout.IntField(_generationLevels);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void ShowAutoGenerationTools()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Автоматическая генерация всех статистик", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            _showAutoGenSettings = EditorGUILayout.Foldout(_showAutoGenSettings, 
                _showAutoGenSettings ? "Скрыть" : "Показать настройки", true);
            EditorGUILayout.EndHorizontal();
            
            if (_showAutoGenSettings)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Автоматически создает полный набор статистик для выбранного класса персонажа, учитывая баланс игры и типичные формулы из популярных action RPG.", MessageType.Info);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Класс персонажа:", GUILayout.Width(120));
                _selectedAutoGenClass = (CharacterClass)EditorGUILayout.EnumPopup(_selectedAutoGenClass);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Количество уровней:", GUILayout.Width(120));
                _generationLevels = EditorGUILayout.IntField(_generationLevels);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // Опции для существующих классов
                int existingClassIndex = -1;
                for (int i = 0; i < _characterClassesProperty.arraySize; i++)
                {
                    SerializedProperty classProperty = _characterClassesProperty.GetArrayElementAtIndex(i);
                    SerializedProperty characterClassProperty = classProperty.FindPropertyRelative("characterClass");
                    
                    if (characterClassProperty.enumValueIndex == (int)_selectedAutoGenClass)
                    {
                        existingClassIndex = i;
                        break;
                    }
                }
                
                if (existingClassIndex != -1)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Заменить существующие статистики", GUILayout.Height(30)))
                    {
                        SerializedProperty classProperty = _characterClassesProperty.GetArrayElementAtIndex(existingClassIndex);
                        SerializedProperty statsProperty = classProperty.FindPropertyRelative("stats");
                        AutoGenerateForClass(_selectedAutoGenClass, statsProperty);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Добавить новый класс со статистиками", GUILayout.Height(30)))
                    {
                        AddClassWithAutoStats(_selectedAutoGenClass);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
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
        
        private void AddCharacterClass()
        {
            _characterClassesProperty.arraySize++;
            int newIndex = _characterClassesProperty.arraySize - 1;
            SerializedProperty newClassProperty = _characterClassesProperty.GetArrayElementAtIndex(newIndex);
            SerializedProperty newClassTypeProperty = newClassProperty.FindPropertyRelative("characterClass");
            SerializedProperty newStatsProperty = newClassProperty.FindPropertyRelative("stats");
            
            // Устанавливаем тип по умолчанию и очищаем массив статистик
            newClassTypeProperty.enumValueIndex = 0;
            newStatsProperty.arraySize = 0;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void InsertClassAt(int index)
        {
            _characterClassesProperty.InsertArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }
        
        private void RemoveClassAt(int index)
        {
            _characterClassesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }
        
        private void AddStatToClass(SerializedProperty statsProperty)
        {
            statsProperty.arraySize++;
            int newIndex = statsProperty.arraySize - 1;
            SerializedProperty newStatProperty = statsProperty.GetArrayElementAtIndex(newIndex);
            SerializedProperty newStatTypeProperty = newStatProperty.FindPropertyRelative("stat");
            SerializedProperty newLevelsProperty = newStatProperty.FindPropertyRelative("levels");
            
            // Устанавливаем тип по умолчанию и начальный размер массива уровней
            newStatTypeProperty.enumValueIndex = 0;
            newLevelsProperty.arraySize = 1;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void InsertStatAt(SerializedProperty statsProperty, int index)
        {
            statsProperty.InsertArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }
        
        private void RemoveStatAt(SerializedProperty statsProperty, int index)
        {
            statsProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
        }
        
        private void AutoGenerateForClass(CharacterClass characterClass, SerializedProperty statsProperty)
        {
            // Получаем полную прогрессию для класса
            Dictionary<Stat, float[]> statProgression = ProgressionUtility.GenerateFullStatProgression(characterClass, _generationLevels);
            
            // Очищаем существующие статистики
            statsProperty.arraySize = 0;
            serializedObject.ApplyModifiedProperties();
            
            // Добавляем новые статистики
            foreach (var pair in statProgression)
            {
                // Пропускаем пустые массивы
                if (pair.Value == null || pair.Value.Length == 0)
                    continue;
                
                // Добавляем новую статистику
                statsProperty.arraySize++;
                int newIndex = statsProperty.arraySize - 1;
                SerializedProperty newStatProperty = statsProperty.GetArrayElementAtIndex(newIndex);
                SerializedProperty newStatTypeProperty = newStatProperty.FindPropertyRelative("stat");
                SerializedProperty newLevelsProperty = newStatProperty.FindPropertyRelative("levels");
                
                // Устанавливаем тип статистики
                newStatTypeProperty.enumValueIndex = (int)pair.Key;
                
                // Устанавливаем значения по уровням
                newLevelsProperty.arraySize = pair.Value.Length;
                for (int i = 0; i < pair.Value.Length; i++)
                {
                    SerializedProperty levelProperty = newLevelsProperty.GetArrayElementAtIndex(i);
                    levelProperty.floatValue = pair.Value[i];
                }
            }
            
            serializedObject.ApplyModifiedProperties();
            Debug.Log($"Автоматически сгенерированы статистики для класса {characterClass}");
        }
        
        private void AddClassWithAutoStats(CharacterClass characterClass)
        {
            // Добавляем новый класс
            _characterClassesProperty.arraySize++;
            int newIndex = _characterClassesProperty.arraySize - 1;
            SerializedProperty newClassProperty = _characterClassesProperty.GetArrayElementAtIndex(newIndex);
            SerializedProperty newClassTypeProperty = newClassProperty.FindPropertyRelative("characterClass");
            SerializedProperty newStatsProperty = newClassProperty.FindPropertyRelative("stats");
            
            // Устанавливаем тип класса
            newClassTypeProperty.enumValueIndex = (int)characterClass;
            
            // Очищаем статистики
            newStatsProperty.arraySize = 0;
            serializedObject.ApplyModifiedProperties();
            
            // Автоматически генерируем статистики
            AutoGenerateForClass(characterClass, newStatsProperty);
        }
    }
} 