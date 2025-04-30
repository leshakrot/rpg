using UnityEngine;
using UnityEditor;
using RPG.Inventories;
using UnityEditorInternal;

[CustomEditor(typeof(DropLibrary))]
public class DropLibraryEditor : Editor
{
    private SerializedProperty dropConfigsProperty;
    private SerializedProperty globalConfigProperty;
    private ReorderableList dropItemsList;
    
    private bool showGlobalSettings = true;
    private bool showDropChanceLevels = false;
    private bool showMinDropsLevels = false;
    private bool showMaxDropsLevels = false;
    
    private GUIStyle headerStyle;
    private GUIStyle boldLabelStyle;
    private GUIStyle boxStyle;
    
    private void OnEnable()
    {
        // Получаем свойства
        dropConfigsProperty = serializedObject.FindProperty("dropConfigs");
        globalConfigProperty = serializedObject.FindProperty("globalConfig");
        
        // Создаем ReorderableList для удобного перемещения и управления предметами
        CreateDropItemsList();
    }

    private void CreateDropItemsList()
    {
        dropItemsList = new ReorderableList(
            serializedObject, 
            dropConfigsProperty, 
            true, // draggable
            true, // displayHeader
            true, // add button
            true  // remove button
        );
        
        // Настройка заголовка списка
        dropItemsList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Предметы для выпадения", EditorStyles.boldLabel);
        };
        
        // Настройка отображения элементов списка
        dropItemsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = dropItemsList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty itemProperty = element.FindPropertyRelative("item");
            SerializedProperty relativeChanceProperty = element.FindPropertyRelative("relativeChance");
            
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            
            // Вычисляем ширины для каждого элемента
            float itemWidth = rect.width * 0.7f;
            float chanceWidth = rect.width * 0.25f;
            
            // Предмет
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, itemWidth, rect.height),
                itemProperty,
                GUIContent.none
            );
            
            // Шанс выпадения
            EditorGUI.PropertyField(
                new Rect(rect.x + itemWidth + 5, rect.y, chanceWidth, rect.height),
                relativeChanceProperty,
                GUIContent.none
            );
        };
        
        // Настройка высоты элемента списка
        dropItemsList.elementHeightCallback = (int index) =>
        {
            float baseHeight = EditorGUIUtility.singleLineHeight + 4;
            
            // Если элемент раскрыт, увеличиваем высоту
            if (index == dropItemsList.index)
            {
                SerializedProperty element = dropItemsList.serializedProperty.GetArrayElementAtIndex(index);
                SerializedProperty usePerLevelChance = element.FindPropertyRelative("usePerLevelChance");
                SerializedProperty usePerLevelMinNumber = element.FindPropertyRelative("usePerLevelMinNumber");
                SerializedProperty usePerLevelMaxNumber = element.FindPropertyRelative("usePerLevelMaxNumber");
                
                float additionalHeight = EditorGUIUtility.singleLineHeight * 3 + 8; // Базовые 3 строки
                
                // Если используется настройка по уровням, добавляем высоту
                if (usePerLevelChance.boolValue)
                {
                    SerializedProperty rangesProperty = element.FindPropertyRelative("relativeChancePerLevel");
                    additionalHeight += EditorGUIUtility.singleLineHeight + 4; // Для заголовка
                    additionalHeight += rangesProperty.arraySize * (EditorGUIUtility.singleLineHeight * 3 + 8);
                    additionalHeight += EditorGUIUtility.singleLineHeight + 4; // Для кнопки добавления
                }
                
                if (usePerLevelMinNumber.boolValue)
                {
                    SerializedProperty rangesProperty = element.FindPropertyRelative("minNumberPerLevel");
                    additionalHeight += EditorGUIUtility.singleLineHeight + 4; // Для заголовка
                    additionalHeight += rangesProperty.arraySize * (EditorGUIUtility.singleLineHeight * 3 + 8);
                    additionalHeight += EditorGUIUtility.singleLineHeight + 4; // Для кнопки добавления
                }
                
                if (usePerLevelMaxNumber.boolValue)
                {
                    SerializedProperty rangesProperty = element.FindPropertyRelative("maxNumberPerLevel");
                    additionalHeight += EditorGUIUtility.singleLineHeight + 4; // Для заголовка
                    additionalHeight += rangesProperty.arraySize * (EditorGUIUtility.singleLineHeight * 3 + 8);
                    additionalHeight += EditorGUIUtility.singleLineHeight + 4; // Для кнопки добавления
                }
                
                return baseHeight + additionalHeight;
            }
            
            return baseHeight;
        };
        
        // Настройка логики выбора элемента
        dropItemsList.onSelectCallback = (ReorderableList list) =>
        {
            // При выборе элемента отмечаем, что GUI изменился для перерисовки
            GUI.changed = true;
        };
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        InitializeStyles();
        
        // Глобальные настройки
        DrawGlobalSettings();
        
        EditorGUILayout.Space(10);
        
        // Список предметов для выпадения
        dropItemsList.DoLayoutList();
        
        // Если выбран элемент, отображаем его детали
        if (dropItemsList.index >= 0 && dropItemsList.index < dropConfigsProperty.arraySize)
        {
            DrawSelectedItemDetails(dropItemsList.index);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
        }
        
        if (boldLabelStyle == null)
        {
            boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
        }
        
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(EditorStyles.helpBox);
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
            boxStyle.margin = new RectOffset(0, 0, 10, 10);
        }
    }
    
    private void DrawGlobalSettings()
    {
        showGlobalSettings = EditorGUILayout.Foldout(showGlobalSettings, "Глобальные настройки дропа", true, EditorStyles.foldoutHeader);
        
        if (showGlobalSettings)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            // Отображаем базовые настройки
            EditorGUILayout.PropertyField(
                globalConfigProperty.FindPropertyRelative("dropChancePercentage"), 
                new GUIContent("Шанс дропа (%)", "Шанс того, что что-то выпадет (в процентах)")
            );
            
            EditorGUILayout.PropertyField(
                globalConfigProperty.FindPropertyRelative("minDrops"), 
                new GUIContent("Мин. предметов", "Минимальное количество предметов, которое может выпасть")
            );
            
            EditorGUILayout.PropertyField(
                globalConfigProperty.FindPropertyRelative("maxDrops"), 
                new GUIContent("Макс. предметов", "Максимальное количество предметов, которое может выпасть")
            );
            
            EditorGUILayout.Space(5);
            
            // Настройки по уровням
            SerializedProperty usePerLevelChanceProp = globalConfigProperty.FindPropertyRelative("usePerLevelDropChance");
            
            EditorGUILayout.PropertyField(
                usePerLevelChanceProp, 
                new GUIContent("Настройки по уровням", "Использовать индивидуальные настройки для разных уровней")
            );
            
            if (usePerLevelChanceProp.boolValue)
            {
                EditorGUILayout.Space(5);
                
                // Шанс дропа по уровням
                showDropChanceLevels = EditorGUILayout.Foldout(showDropChanceLevels, "Шанс дропа по уровням", true);
                if (showDropChanceLevels)
                {
                    DrawLevelRanges(globalConfigProperty.FindPropertyRelative("dropChancePerLevel"), "Шанс дропа");
                }
                
                // Мин. предметов по уровням
                showMinDropsLevels = EditorGUILayout.Foldout(showMinDropsLevels, "Мин. предметов по уровням", true);
                if (showMinDropsLevels)
                {
                    DrawLevelRanges(globalConfigProperty.FindPropertyRelative("minDropsPerLevel"), "Мин. предметов");
                }
                
                // Макс. предметов по уровням
                showMaxDropsLevels = EditorGUILayout.Foldout(showMaxDropsLevels, "Макс. предметов по уровням", true);
                if (showMaxDropsLevels)
                {
                    DrawLevelRanges(globalConfigProperty.FindPropertyRelative("maxDropsPerLevel"), "Макс. предметов");
                }
            }
            
            EditorGUILayout.EndVertical();
        }
    }
    
    private void DrawSelectedItemDetails(int index)
    {
        SerializedProperty itemProperty = dropConfigsProperty.GetArrayElementAtIndex(index);
        SerializedProperty itemName = itemProperty.FindPropertyRelative("item");
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginVertical(boxStyle);
        
        // Заголовок с названием предмета
        string itemDisplayName = itemName.objectReferenceValue != null 
            ? itemName.objectReferenceValue.name 
            : "Предмет не выбран";
            
        EditorGUILayout.LabelField($"Настройки: {itemDisplayName}", headerStyle);
        
        EditorGUILayout.Space(5);
        
        // Основные параметры
        EditorGUILayout.PropertyField(
            itemProperty.FindPropertyRelative("item"), 
            new GUIContent("Предмет")
        );
        
        EditorGUILayout.PropertyField(
            itemProperty.FindPropertyRelative("relativeChance"), 
            new GUIContent("Шанс выпадения", "Относительный шанс выпадения этого предмета")
        );
        
        // Количество для стакуемых предметов
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(
            itemProperty.FindPropertyRelative("minNumber"), 
            new GUIContent("Мин. количество")
        );
        
        EditorGUILayout.PropertyField(
            itemProperty.FindPropertyRelative("maxNumber"), 
            new GUIContent("Макс. количество")
        );
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Настройки по уровням
        EditorGUILayout.LabelField("Настройки по уровням", boldLabelStyle);
        
        // Шанс по уровням
        SerializedProperty usePerLevelChanceProp = itemProperty.FindPropertyRelative("usePerLevelChance");
        EditorGUILayout.PropertyField(
            usePerLevelChanceProp, 
            new GUIContent("Шанс зависит от уровня")
        );
        
        if (usePerLevelChanceProp.boolValue)
        {
            DrawLevelRanges(itemProperty.FindPropertyRelative("relativeChancePerLevel"), "Шанс");
        }
        
        // Мин количество по уровням
        SerializedProperty usePerLevelMinProp = itemProperty.FindPropertyRelative("usePerLevelMinNumber");
        EditorGUILayout.PropertyField(
            usePerLevelMinProp, 
            new GUIContent("Мин. количество зависит от уровня")
        );
        
        if (usePerLevelMinProp.boolValue)
        {
            DrawLevelRanges(itemProperty.FindPropertyRelative("minNumberPerLevel"), "Мин. количество");
        }
        
        // Макс количество по уровням
        SerializedProperty usePerLevelMaxProp = itemProperty.FindPropertyRelative("usePerLevelMaxNumber");
        EditorGUILayout.PropertyField(
            usePerLevelMaxProp, 
            new GUIContent("Макс. количество зависит от уровня")
        );
        
        if (usePerLevelMaxProp.boolValue)
        {
            DrawLevelRanges(itemProperty.FindPropertyRelative("maxNumberPerLevel"), "Макс. количество");
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawLevelRanges(SerializedProperty rangesProperty, string labelPrefix)
    {
        EditorGUI.indentLevel++;
        
        for (int i = 0; i < rangesProperty.arraySize; i++)
        {
            SerializedProperty rangeProperty = rangesProperty.GetArrayElementAtIndex(i);
            SerializedProperty minLevelProp = rangeProperty.FindPropertyRelative("minLevel");
            SerializedProperty maxLevelProp = rangeProperty.FindPropertyRelative("maxLevel");
            SerializedProperty valueProp = rangeProperty.FindPropertyRelative("value");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Заголовок диапазона
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Диапазон #{i+1}");
            
            if (GUILayout.Button("Удалить", GUILayout.Width(70)))
            {
                rangesProperty.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();
            
            // Настройки диапазона
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(
                minLevelProp, 
                new GUIContent("От уровня")
            );
            
            EditorGUILayout.PropertyField(
                maxLevelProp, 
                new GUIContent("До уровня")
            );
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.PropertyField(
                valueProp, 
                new GUIContent($"{labelPrefix}")
            );
            
            EditorGUILayout.EndVertical();
        }
        
        // Кнопка добавления нового диапазона
        if (GUILayout.Button("Добавить диапазон"))
        {
            int newIndex = rangesProperty.arraySize;
            rangesProperty.arraySize++;
            
            SerializedProperty newRange = rangesProperty.GetArrayElementAtIndex(newIndex);
            
            // Установка значений по умолчанию для нового диапазона
            int startLevel = 1;
            if (newIndex > 0)
            {
                // Если уже есть диапазоны, начинаем с уровня, следующего за последним
                SerializedProperty lastRange = rangesProperty.GetArrayElementAtIndex(newIndex - 1);
                startLevel = lastRange.FindPropertyRelative("maxLevel").intValue + 1;
            }
            
            newRange.FindPropertyRelative("minLevel").intValue = startLevel;
            newRange.FindPropertyRelative("maxLevel").intValue = startLevel + 4; // +4 для диапазона в 5 уровней
        }
        
        EditorGUI.indentLevel--;
    }
} 