using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameDevTV.Utils;

namespace RPG.Dialogue.Editor
{
    /// <summary>
    /// Editor-окно для просмотра всех существующих предикатов диалоговой системы.
    /// Автоматически сканирует все классы, реализующие IPredicateEvaluator,
    /// и извлекает из них список доступных предикатов.
    /// </summary>
    public class DialoguePredicateViewer : EditorWindow
    {
        private Vector2 _scrollPosition;
        private Dictionary<string, List<PredicateInfo>> _predicatesByCategory;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double AUTO_REFRESH_INTERVAL = 2.0; // секунды
        private string _searchQuery = "";
        private Dictionary<string, bool> _categoryFoldouts = new Dictionary<string, bool>();
        private bool _showHelp = false;

        private class PredicateInfo
        {
            public string Name;
            public string Description;
            public string[] Parameters;
            public Type SourceType;

            public PredicateInfo(string name, string description, string[] parameters, Type sourceType)
            {
                Name = name;
                Description = description;
                Parameters = parameters;
                SourceType = sourceType;
            }
        }

        [MenuItem("RPG/Tools/Dialogue Predicate Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialoguePredicateViewer>("Predicate Viewer");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshPredicates();
            _lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private void Update()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > AUTO_REFRESH_INTERVAL)
            {
                RefreshPredicates();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            
            if (_showHelp)
            {
                DrawHelpPanel();
            }
            else
            {
                DrawPredicatesList();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Dialogue Predicates", titleStyle);
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Поиск
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
            string newSearchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);
            if (newSearchQuery != _searchQuery)
            {
                _searchQuery = newSearchQuery;
                Repaint();
            }
            
            if (GUILayout.Button("✖", GUILayout.Width(25)))
            {
                _searchQuery = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Кнопки управления
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔄 Refresh", GUILayout.Width(100)))
            {
                RefreshPredicates();
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", GUILayout.Width(100));

            if (GUILayout.Button("📋 Export to Text", GUILayout.Width(120)))
            {
                ExportToText();
            }

            if (GUILayout.Button("Expand All", GUILayout.Width(80)))
            {
                SetAllFoldouts(true);
            }

            if (GUILayout.Button("Collapse All", GUILayout.Width(80)))
            {
                SetAllFoldouts(false);
            }

            if (GUILayout.Button(_showHelp ? "📋 Predicates" : "❓ Help", GUILayout.Width(100)))
            {
                _showHelp = !_showHelp;
            }

            GUILayout.FlexibleSpace();

            int totalPredicates = _predicatesByCategory?.Sum(kvp => kvp.Value.Count) ?? 0;
            int visiblePredicates = GetVisiblePredicatesCount();
            
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                EditorGUILayout.LabelField($"Showing: {visiblePredicates} / {totalPredicates}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"Total: {totalPredicates}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            DrawSeparator();
        }

        private void DrawPredicatesList()
        {
            if (_predicatesByCategory == null || _predicatesByCategory.Count == 0)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox("No predicates found. Make sure your IPredicateEvaluator classes are compiled.", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var category in _predicatesByCategory.OrderBy(kvp => kvp.Key))
            {
                DrawCategory(category.Key, category.Value);
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCategory(string categoryName, List<PredicateInfo> predicates)
        {
            // Фильтрация предикатов по поиску
            var filteredPredicates = string.IsNullOrEmpty(_searchQuery)
                ? predicates
                : predicates.Where(p => 
                    p.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (!string.IsNullOrEmpty(p.Description) && p.Description.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();

            if (filteredPredicates.Count == 0)
                return;

            // Инициализация состояния foldout
            if (!_categoryFoldouts.ContainsKey(categoryName))
            {
                _categoryFoldouts[categoryName] = true;
            }

            // Заголовок категории
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle categoryStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.3f, 0.6f, 1f) }
            };

            _categoryFoldouts[categoryName] = EditorGUILayout.Foldout(
                _categoryFoldouts[categoryName], 
                $"📁 {categoryName} ({filteredPredicates.Count})",
                true,
                categoryStyle
            );

            if (_categoryFoldouts[categoryName])
            {
                EditorGUILayout.Space(5);

                // Список предикатов в категории
                foreach (var predicate in filteredPredicates.OrderBy(p => p.Name))
                {
                    DrawPredicate(predicate);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPredicate(PredicateInfo predicate)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            
            // Имя предиката
            GUIStyle predicateNameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(1f, 0.8f, 0.2f) }
            };
            EditorGUILayout.LabelField($"▸ {predicate.Name}", predicateNameStyle);

            // Кнопка копирования
            if (GUILayout.Button("📋 Copy", GUILayout.Width(60)))
            {
                EditorGUIUtility.systemCopyBuffer = predicate.Name;
                Debug.Log($"Copied to clipboard: {predicate.Name}");
            }

            EditorGUILayout.EndHorizontal();

            // Описание
            if (!string.IsNullOrEmpty(predicate.Description))
            {
                GUIStyle descStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 11,
                    normal = { textColor = Color.gray }
                };
                EditorGUILayout.LabelField(predicate.Description, descStyle);
            }

            // Параметры
            if (predicate.Parameters != null && predicate.Parameters.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Parameters:", EditorStyles.miniLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField(string.Join(", ", predicate.Parameters), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            // Источник
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source:", EditorStyles.miniLabel, GUILayout.Width(80));
            
            if (GUILayout.Button(predicate.SourceType.Name, EditorStyles.linkLabel))
            {
                // Попытка открыть файл скрипта
                OpenScriptFile(predicate.SourceType);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private void RefreshPredicates()
        {
            _predicatesByCategory = new Dictionary<string, List<PredicateInfo>>();

            // Находим все типы, реализующие IPredicateEvaluator
            var evaluatorTypes = GetAllPredicateEvaluators();

            foreach (var type in evaluatorTypes)
            {
                var predicates = ExtractPredicatesFromType(type);
                
                if (predicates.Count > 0)
                {
                    string category = GetCategoryName(type);
                    
                    if (!_predicatesByCategory.ContainsKey(category))
                    {
                        _predicatesByCategory[category] = new List<PredicateInfo>();
                    }

                    _predicatesByCategory[category].AddRange(predicates);
                }
            }
        }

        private List<Type> GetAllPredicateEvaluators()
        {
            var evaluators = new List<Type>();

            // Получаем все загруженные сборки
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    // Пропускаем системные сборки для ускорения
                    if (assembly.FullName.StartsWith("Unity") || 
                        assembly.FullName.StartsWith("System") ||
                        assembly.FullName.StartsWith("Mono") ||
                        assembly.FullName.StartsWith("mscorlib"))
                        continue;

                    var types = assembly.GetTypes()
                        .Where(t => typeof(IPredicateEvaluator).IsAssignableFrom(t) 
                                 && !t.IsInterface 
                                 && !t.IsAbstract);

                    evaluators.AddRange(types);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Игнорируем ошибки загрузки типов
                    continue;
                }
            }

            return evaluators;
        }

        private List<PredicateInfo> ExtractPredicatesFromType(Type type)
        {
            var predicates = new List<PredicateInfo>();

            // Ищем метод Evaluate
            var evaluateMethod = type.GetMethod("Evaluate", 
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string[]) },
                null);

            if (evaluateMethod == null)
                return predicates;

            // Пытаемся извлечь информацию из XML-комментариев и анализа кода
            var xmlDoc = ExtractXmlDocumentation(type);
            
            // Анализируем исходный код метода через декомпиляцию
            var methodPredicates = AnalyzeEvaluateMethod(type, evaluateMethod);
            
            foreach (var predName in methodPredicates)
            {
                string description = "";
                
                // Пытаемся найти описание в XML-документации
                if (xmlDoc.ContainsKey(predName))
                {
                    description = xmlDoc[predName];
                }

                predicates.Add(new PredicateInfo(
                    predName,
                    description,
                    new string[] { }, // Параметры можно расширить
                    type
                ));
            }

            return predicates;
        }

        private HashSet<string> AnalyzeEvaluateMethod(Type type, MethodInfo method)
        {
            var predicateNames = new HashSet<string>();

            try
            {
                // Получаем IL-код метода
                var methodBody = method.GetMethodBody();
                if (methodBody == null)
                    return predicateNames;

                // Пытаемся найти строковые константы в методе
                // Это упрощенный подход - в реальности нужен более сложный анализ
                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                
                // Проверяем исходный код через Asset Database (если доступен)
                string assetPath = FindScriptAssetPath(type);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var sourceCode = System.IO.File.ReadAllText(assetPath);
                    predicateNames = ExtractPredicateNamesFromSource(sourceCode);
                }
            }
            catch
            {
                // Если анализ не удался, возвращаем пустой список
            }

            return predicateNames;
        }

        private string FindScriptAssetPath(Type type)
        {
            // Ищем файл скрипта в проекте
            var guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                
                if (script != null && script.GetClass() == type)
                {
                    return path;
                }
            }

            return null;
        }

        private HashSet<string> ExtractPredicateNamesFromSource(string sourceCode)
        {
            var predicates = new HashSet<string>();

            // Ищем case-блоки в switch-выражениях
            var casePattern = new System.Text.RegularExpressions.Regex(
                @"case\s+""([^""]+)""\s*:",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            var matches = casePattern.Matches(sourceCode);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    predicates.Add(match.Groups[1].Value);
                }
            }

            // Ищем switch expressions (C# 8.0+)
            var switchExpressionPattern = new System.Text.RegularExpressions.Regex(
                @"""([^""]+)""\s*=>",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            matches = switchExpressionPattern.Matches(sourceCode);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var predName = match.Groups[1].Value;
                    // Фильтруем слишком общие строки
                    if (predName.Length > 3 && !predName.Contains(" ") && char.IsUpper(predName[0]))
                    {
                        predicates.Add(predName);
                    }
                }
            }

            return predicates;
        }

        private Dictionary<string, string> ExtractXmlDocumentation(Type type)
        {
            var docs = new Dictionary<string, string>();

            try
            {
                // Пытаемся прочитать XML-комментарии из исходного кода
                string assetPath = FindScriptAssetPath(type);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var sourceCode = System.IO.File.ReadAllText(assetPath);
                    
                    // Ищем блок с описанием предикатов в XML-комментариях
                    var summaryPattern = new System.Text.RegularExpressions.Regex(
                        @"///\s*(.+?)\s*—\s*(.+?)$",
                        System.Text.RegularExpressions.RegexOptions.Multiline
                    );

                    var matches = summaryPattern.Matches(sourceCode);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Groups.Count > 2)
                        {
                            string predicateName = match.Groups[1].Value.Trim();
                            string description = match.Groups[2].Value.Trim();
                            docs[predicateName] = description;
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки чтения документации
            }

            return docs;
        }

        private string GetCategoryName(Type type)
        {
            // Определяем категорию на основе namespace или имени класса
            if (type.Namespace != null)
            {
                if (type.Namespace.Contains("Quest"))
                    return "Quests";
                if (type.Namespace.Contains("Inventor") || type.Namespace.Contains("Item"))
                    return "Inventory & Items";
                if (type.Namespace.Contains("Companion"))
                    return "Companions";
                if (type.Namespace.Contains("Stat") || type.Namespace.Contains("Trait"))
                    return "Stats & Traits";
                if (type.Namespace.Contains("Dialogue"))
                    return "Dialogue";
            }

            // По умолчанию используем namespace или "Other"
            return type.Namespace?.Split('.').LastOrDefault() ?? "Other";
        }

        private void SetAllFoldouts(bool state)
        {
            if (_predicatesByCategory == null) return;

            foreach (var category in _predicatesByCategory.Keys)
            {
                _categoryFoldouts[category] = state;
            }
            Repaint();
        }

        private int GetVisiblePredicatesCount()
        {
            if (_predicatesByCategory == null || string.IsNullOrEmpty(_searchQuery))
                return _predicatesByCategory?.Sum(kvp => kvp.Value.Count) ?? 0;

            int count = 0;
            foreach (var category in _predicatesByCategory)
            {
                count += category.Value.Count(p =>
                    p.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (!string.IsNullOrEmpty(p.Description) && p.Description.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                );
            }
            return count;
        }

        private void OpenScriptFile(Type type)
        {
            string assetPath = FindScriptAssetPath(type);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                if (script != null)
                {
                    AssetDatabase.OpenAsset(script);
                }
            }
        }

        private void ExportToText()
        {
            if (_predicatesByCategory == null || _predicatesByCategory.Count == 0)
            {
                EditorUtility.DisplayDialog("Export", "No predicates to export!", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Predicates", "", "DialoguePredicates.txt", "txt");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                using (var writer = new System.IO.StreamWriter(path))
                {
                    writer.WriteLine("===========================================");
                    writer.WriteLine("     DIALOGUE PREDICATES REFERENCE");
                    writer.WriteLine("===========================================");
                    writer.WriteLine();
                    writer.WriteLine($"Generated: {System.DateTime.Now}");
                    writer.WriteLine($"Total Predicates: {_predicatesByCategory.Sum(kvp => kvp.Value.Count)}");
                    writer.WriteLine();

                    foreach (var category in _predicatesByCategory.OrderBy(kvp => kvp.Key))
                    {
                        writer.WriteLine();
                        writer.WriteLine($"┌─────────────────────────────────────────");
                        writer.WriteLine($"│ {category.Key.ToUpper()} ({category.Value.Count})");
                        writer.WriteLine($"└─────────────────────────────────────────");
                        writer.WriteLine();

                        foreach (var predicate in category.Value.OrderBy(p => p.Name))
                        {
                            writer.WriteLine($"  ▸ {predicate.Name}");
                            
                            if (!string.IsNullOrEmpty(predicate.Description))
                            {
                                writer.WriteLine($"    {predicate.Description}");
                            }

                            if (predicate.Parameters != null && predicate.Parameters.Length > 0)
                            {
                                writer.WriteLine($"    Parameters: {string.Join(", ", predicate.Parameters)}");
                            }

                            writer.WriteLine($"    Source: {predicate.SourceType.FullName}");
                            writer.WriteLine();
                        }
                    }

                    writer.WriteLine();
                    writer.WriteLine("===========================================");
                    writer.WriteLine("           END OF REFERENCE");
                    writer.WriteLine("===========================================");
                }

                EditorUtility.DisplayDialog("Export Complete", $"Predicates exported to:\n{path}", "OK");
                System.Diagnostics.Process.Start(path);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed", $"Error: {ex.Message}", "OK");
            }
        }

        private void DrawHelpPanel()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
            };

            GUIStyle subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.5f, 0.7f, 1f) }
            };

            EditorGUILayout.Space(10);

            // Что это такое
            EditorGUILayout.LabelField("🎯 Что это?", headerStyle);
            EditorGUILayout.HelpBox(
                "Этот инструмент показывает все предикаты (условия) диалоговой системы вашего проекта. " +
                "Предикаты используются в диалогах для проверки условий показа реплик.",
                MessageType.Info
            );
            EditorGUILayout.Space(10);

            // Как использовать
            EditorGUILayout.LabelField("📖 Как использовать", headerStyle);
            EditorGUILayout.Space(5);

            DrawHelpItem("🔍 Поиск", "Введите текст для поиска предикатов по имени или описанию");
            DrawHelpItem("📋 Copy", "Скопировать имя предиката в буфер обмена");
            DrawHelpItem("🔗 Source", "Кликните на имя класса чтобы открыть исходный код");
            DrawHelpItem("📄 Export", "Экспортировать весь список в текстовый файл");
            DrawHelpItem("🔄 Refresh", "Обновить список предикатов вручную");
            DrawHelpItem("⚙️ Auto Refresh", "Автоматическое обновление каждые 2 секунды");

            EditorGUILayout.Space(10);

            // Категории
            EditorGUILayout.LabelField("📁 Категории", headerStyle);
            EditorGUILayout.Space(5);

            DrawHelpItem("Quests", "Условия связанные с квестами и задачами");
            DrawHelpItem("Inventory & Items", "Проверки предметов в инвентаре");
            DrawHelpItem("Companions", "Состояния компаньонов");
            DrawHelpItem("Stats & Traits", "Характеристики персонажа");

            EditorGUILayout.Space(10);

            // Как добавить свой предикат
            EditorGUILayout.LabelField("➕ Как добавить свой предикат", headerStyle);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "1. Создайте класс, реализующий IPredicateEvaluator\n" +
                "2. Добавьте метод Evaluate с switch/case блоком\n" +
                "3. Добавьте XML-комментарий с описанием\n" +
                "4. Предикат появится автоматически!",
                MessageType.None
            );

            EditorGUILayout.Space(5);

            // Пример кода
            EditorGUILayout.LabelField("💻 Пример кода:", subHeaderStyle);
            DrawCodeExample();

            EditorGUILayout.Space(20);

            EditorGUILayout.EndScrollView();
        }

        private void DrawHelpItem(string title, string description)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(1f, 0.9f, 0.4f) }
            };
            EditorGUILayout.LabelField(title, titleStyle, GUILayout.Width(120));
            
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(3);
        }

        private void DrawCodeExample()
        {
            string exampleCode = @"using GameDevTV.Utils;

public class MyPredicates : MonoBehaviour, IPredicateEvaluator
{
    /// <summary>
    /// Доступные предикаты:
    ///   MyCondition — описание условия
    /// </summary>
    public bool? Evaluate(string predicate, string[] parameters)
    {
        return predicate switch
        {
            ""MyCondition"" => CheckSomething(),
            _ => null
        };
    }
}";

            GUIStyle codeStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = 11,
                wordWrap = false,
                richText = false
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.TextArea(exampleCode, codeStyle, GUILayout.Height(200));
            EditorGUILayout.EndVertical();
        }
    }
}
