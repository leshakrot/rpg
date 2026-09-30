using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using RPG.Stats;

namespace RPG.EditorTools
{
    [CustomEditor(typeof(Progression))]
    public class ProgressionEditor : UnityEditor.Editor
    {
        private const string PrefDefenceModel = "RPG.Progression.DefenceModel";

        private static readonly string[] ModeLabels = { "Таблица", "Линейная", "Степенная", "Геометрическая", "Кривая" };
        private static readonly int[] ReportOffsets = { -3, 0, 3 };

        private SerializedProperty maxLevelProp;
        private SerializedProperty dataProp;

        private readonly HashSet<int> openClasses = new HashSet<int>();
        private readonly HashSet<int> openEntries = new HashSet<int>();

        private bool showPreset;
        private bool showReport;
        private DefenceModel defenceModel;
        private CharacterClass reportEnemy = CharacterClass.Grunt;

        private GUIStyle classFoldout;

        private Action pending; // отложенное изменение массива (нельзя менять его посреди отрисовки)

        private void OnEnable()
        {
            maxLevelProp = serializedObject.FindProperty("maxLevel");
            dataProp = serializedObject.FindProperty("progressionData");
            defenceModel = (DefenceModel)EditorPrefs.GetInt(PrefDefenceModel, 0);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSettings();
            DrawWarnings();
            DrawPresetPanel();
            DrawReportPanel();

            EditorGUILayout.Space(6);
            DrawClasses();

            serializedObject.ApplyModifiedProperties();

            if (pending != null)
            {
                Action action = pending;
                pending = null;
                action();
                serializedObject.ApplyModifiedProperties();
                Repaint();
            }
        }

        #region Верх: настройки, предупреждения

        private void DrawSettings()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(maxLevelProp, new GUIContent("Максимальный уровень",
                "Кривые Линейная/Степенная/Геометрическая/Кривая растягиваются на этот диапазон. Таблицы не меняются."));
        }

        private void DrawWarnings()
        {
            int maxLevel = maxLevelProp.intValue;
            var seen = new HashSet<int>();
            var problems = new StringBuilder();

            for (int i = 0; i < dataProp.arraySize; i++)
            {
                SerializedProperty e = dataProp.GetArrayElementAtIndex(i);
                int cls = e.FindPropertyRelative("characterClass").intValue;
                int stat = e.FindPropertyRelative("stat").intValue;

                if (!seen.Add(EntryKey(cls, stat)))
                    problems.AppendLine($"• Дубликат: {(CharacterClass)cls} / {(Stat)stat} (работает только последняя запись)");

                if (e.FindPropertyRelative("mode").intValue == (int)CurveMode.Table)
                {
                    int length = e.FindPropertyRelative("values").arraySize;
                    int expected = ExpectedTableLength(stat, maxLevel);
                    if (length != expected)
                        problems.AppendLine($"• Таблица {(CharacterClass)cls} / {(Stat)stat}: {length} значений, ожидается {expected}");
                }
            }

            if (problems.Length > 0)
                EditorGUILayout.HelpBox(problems.ToString().TrimEnd(), MessageType.Warning);
        }

        #endregion

        #region Пресет

        private void DrawPresetPanel()
        {
            showPreset = EditorGUILayout.Foldout(showPreset, "Дизайн-пресет", true);
            if (!showPreset) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();
            defenceModel = (DefenceModel)EditorGUILayout.Popup("Формула защиты", (int)defenceModel, ProgressionBalance.DefenceModelLabels);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetInt(PrefDefenceModel, (int)defenceModel);

            EditorGUILayout.HelpBox(
                "Пересоздаёт ВСЕ записи: игрок и 7 типов врагов + сундук. Враги считаются от игрока того же уровня " +
                "(ударов на убийство / ударов до смерти), поэтому баланс не разъезжается. Ctrl+Z отменяет.",
                MessageType.None);

            if (GUILayout.Button("Применить пресет (заменит все записи)"))
            {
                if (EditorUtility.DisplayDialog("Дизайн-пресет",
                        "Все текущие записи прогрессии будут заменены. Продолжить?", "Заменить", "Отмена"))
                {
                    List<ProgressionEntry> preset = ProgressionBalance.BuildPreset(maxLevelProp.intValue, defenceModel);
                    pending = () => ReplaceAll(preset);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void ReplaceAll(List<ProgressionEntry> entries)
        {
            dataProp.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
                WriteEntry(dataProp.GetArrayElementAtIndex(i), entries[i]);
            openEntries.Clear();
        }

        #endregion

        #region Отчёт по бою

        private void DrawReportPanel()
        {
            showReport = EditorGUILayout.Foldout(showReport, "Отчёт по бою (проверка ощущений)", true);
            if (!showReport) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var progression = (Progression)target;

            if (!progression.HasStat(Stat.Health, CharacterClass.Player) || !progression.HasStat(Stat.Damage, CharacterClass.Player))
            {
                EditorGUILayout.HelpBox("Нужны Health и Damage у Player.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            reportEnemy = (CharacterClass)EditorGUILayout.EnumPopup("Враг", reportEnemy);
            if (!progression.HasStat(Stat.Health, reportEnemy) || !progression.HasStat(Stat.Damage, reportEnemy))
            {
                EditorGUILayout.HelpBox("У выбранного врага нет Health/Damage.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField(
                "Ударов игрока до смерти врага / ударов врага до смерти игрока, при уровне врага −3 / равном / +3.",
                EditorStyles.wordWrappedMiniLabel);

            const float levelW = 34f, cellW = 74f;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Ур.", EditorStyles.miniBoldLabel, GUILayout.Width(levelW));
            foreach (int offset in ReportOffsets)
                GUILayout.Label(offset == 0 ? "равный" : (offset > 0 ? "+" : "") + offset, EditorStyles.miniBoldLabel, GUILayout.Width(cellW));
            GUILayout.Label("убийств/ур.", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();

            int max = maxLevelProp.intValue;
            var levels = new SortedSet<int> { 1, Mathf.Max(1, max / 6), max / 3, max / 2, max * 2 / 3, max * 5 / 6, max };

            foreach (int playerLevel in levels)
            {
                if (playerLevel < 1) continue;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(playerLevel.ToString(), GUILayout.Width(levelW));

                float killsToLevel = 0f;
                foreach (int offset in ReportOffsets)
                {
                    int enemyLevel = Mathf.Clamp(playerLevel + offset, 1, max);
                    ProgressionBalance.FightReport r = ProgressionBalance.Analyze(
                        progression, reportEnemy, playerLevel, enemyLevel, defenceModel);

                    if (offset == 0) killsToLevel = r.killsToLevel;

                    Color old = GUI.contentColor;
                    if (r.hitsToDie < 5f) GUI.contentColor = new Color(1f, 0.55f, 0.5f);
                    else if (r.hitsToKill < 1.5f) GUI.contentColor = new Color(0.6f, 0.85f, 1f);
                    GUILayout.Label($"{r.hitsToKill:0.0} / {r.hitsToDie:0.0}", GUILayout.Width(cellW));
                    GUI.contentColor = old;
                }

                GUILayout.Label(killsToLevel > 0f ? killsToLevel.ToString("0.0") : "—");
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField("Красный: враг убивает игрока быстрее чем за 5 ударов. Голубой: враг умирает с одного удара.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Классы и записи

        private void DrawClasses()
        {
            var groups = new SortedDictionary<int, List<int>>();
            for (int i = 0; i < dataProp.arraySize; i++)
            {
                int cls = dataProp.GetArrayElementAtIndex(i).FindPropertyRelative("characterClass").intValue;
                if (!groups.TryGetValue(cls, out List<int> list))
                    groups[cls] = list = new List<int>();
                list.Add(i);
            }

            foreach (var group in groups)
                DrawClass(group.Key, group.Value);

            EditorGUILayout.Space(2);
            if (GUILayout.Button("+ Добавить класс"))
            {
                var menu = new GenericMenu();
                foreach (CharacterClass c in Enum.GetValues(typeof(CharacterClass)))
                {
                    CharacterClass captured = c;
                    if (groups.ContainsKey((int)c))
                        menu.AddDisabledItem(new GUIContent(c.ToString()));
                    else
                        menu.AddItem(new GUIContent(c.ToString()), false, () => pending = () => AddEntry(captured, Stat.Health));
                }
                menu.ShowAsContext();
            }
        }

        private void DrawClass(int cls, List<int> indices)
        {
            bool open = openClasses.Contains(cls);
            if (classFoldout == null)
                classFoldout = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            open = EditorGUILayout.Foldout(open, $"{(CharacterClass)cls}  ({indices.Count})", true, classFoldout);
            if (open) openClasses.Add(cls); else openClasses.Remove(cls);

            if (GUILayout.Button("+ Стат", EditorStyles.miniButton, GUILayout.Width(56)))
                ShowAddStatMenu(cls, indices);
            if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(26)))
                ShowClassMenu(cls, indices);

            EditorGUILayout.EndHorizontal();

            if (open)
            {
                foreach (int index in indices)
                    DrawEntry(index);
            }

            EditorGUILayout.EndVertical();
        }

        private void ShowAddStatMenu(int cls, List<int> indices)
        {
            var existing = new HashSet<int>();
            foreach (int i in indices)
                existing.Add(dataProp.GetArrayElementAtIndex(i).FindPropertyRelative("stat").intValue);

            var menu = new GenericMenu();
            foreach (Stat s in Enum.GetValues(typeof(Stat)))
            {
                Stat captured = s;
                CharacterClass capturedClass = (CharacterClass)cls;
                if (existing.Contains((int)s))
                    menu.AddDisabledItem(new GUIContent(s.ToString()));
                else
                    menu.AddItem(new GUIContent(s.ToString()), false, () => pending = () => AddEntry(capturedClass, captured));
            }
            menu.ShowAsContext();
        }

        private void ShowClassMenu(int cls, List<int> indices)
        {
            var menu = new GenericMenu();
            CharacterClass destination = (CharacterClass)cls;

            foreach (CharacterClass source in Enum.GetValues(typeof(CharacterClass)))
            {
                if (source == destination) continue;
                CharacterClass captured = source;
                menu.AddItem(new GUIContent("Скопировать записи из/" + source), false,
                    () => pending = () => CopyClass(captured, destination));
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Удалить класс"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Удалить класс", $"Удалить все записи {destination}?", "Удалить", "Отмена"))
                    pending = () => RemoveIndices(indices);
            });
            menu.ShowAsContext();
        }

        private void DrawEntry(int index)
        {
            SerializedProperty e = dataProp.GetArrayElementAtIndex(index);
            SerializedProperty modeProp = e.FindPropertyRelative("mode");
            int cls = e.FindPropertyRelative("characterClass").intValue;
            int stat = e.FindPropertyRelative("stat").intValue;
            int key = EntryKey(cls, stat);

            ProgressionEntry entry = ReadEntry(e);
            float[] baked = entry.Bake(maxLevelProp.intValue);

            Rect row = EditorGUILayout.GetControlRect(false, 24f);
            bool open = openEntries.Contains(key);

            Rect foldRect = new Rect(row.x + 10f, row.y + 4f, 120f, 16f);
            open = EditorGUI.Foldout(foldRect, open, ((Stat)stat).ToString(), true);
            if (open) openEntries.Add(key); else openEntries.Remove(key);

            Rect modeRect = new Rect(foldRect.xMax + 4f, row.y + 3f, 92f, 18f);
            int newMode = EditorGUI.Popup(modeRect, modeProp.intValue, ModeLabels, EditorStyles.miniPullDown);
            if (newMode != modeProp.intValue)
                ChangeMode(e, entry, newMode);

            Rect sparkRect = new Rect(modeRect.xMax + 6f, row.y + 1f, 64f, 22f);
            DrawSparkline(sparkRect, baked);

            Rect deleteRect = new Rect(row.xMax - 22f, row.y + 3f, 22f, 18f);
            Rect textRect = new Rect(sparkRect.xMax + 6f, row.y + 4f, Mathf.Max(0f, deleteRect.x - sparkRect.xMax - 10f), 16f);
            if (baked.Length > 0)
                GUI.Label(textRect, $"{Format(baked[0])} → {Format(baked[baked.Length - 1])}", EditorStyles.miniLabel);

            if (GUI.Button(deleteRect, "×", EditorStyles.miniButton))
                pending = () => RemoveIndices(new List<int> { index });

            if (!open) return;

            EditorGUI.indentLevel++;
            DrawEntryBody(e, entry, baked);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawEntryBody(SerializedProperty e, ProgressionEntry entry, float[] baked)
        {
            CurveMode mode = entry.mode;

            if (mode == CurveMode.Table)
            {
                DrawTable(e.FindPropertyRelative("values"), (int)entry.stat);
            }
            else
            {
                EditorGUILayout.PropertyField(e.FindPropertyRelative("start"), new GUIContent("Уровень 1"));
                EditorGUILayout.PropertyField(e.FindPropertyRelative("end"), new GUIContent("Макс. уровень"));

                if (mode == CurveMode.Power)
                {
                    EditorGUILayout.Slider(e.FindPropertyRelative("shape"), 0.3f, 3f, new GUIContent("Изгиб",
                        "1 = линейно. Меньше 1 — быстрый рост в начале. Больше 1 — рост ускоряется к концу."));
                }
                else if (mode == CurveMode.Geometric && (entry.start <= 0f || entry.end <= 0f))
                {
                    EditorGUILayout.HelpBox("Геометрическая кривая требует start и end > 0, иначе работает как линейная.", MessageType.Warning);
                }
                else if (mode == CurveMode.Curve)
                {
                    EditorGUILayout.PropertyField(e.FindPropertyRelative("curve"), new GUIContent("Форма (0..1)"));
                }

                EditorGUILayout.PropertyField(e.FindPropertyRelative("rounding"), new GUIContent("Округление"));
            }

            // Большой график + значения по уровням
            Rect graph = EditorGUILayout.GetControlRect(false, 64f);
            graph.xMin += EditorGUI.indentLevel * 15f;
            DrawSparkline(graph, baked);

            if (baked.Length > 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < baked.Length; i++)
                {
                    if (i > 0) sb.Append("   ");
                    sb.Append(i + 1).Append(": ").Append(Format(baked[i]));
                }
                EditorGUILayout.LabelField(sb.ToString(), EditorStyles.wordWrappedMiniLabel);
            }

            if (mode != CurveMode.Table && GUILayout.Button("Преобразовать в таблицу (для ручной правки)", EditorStyles.miniButton))
            {
                float[] copy = baked;
                SerializedProperty modeProp = e.FindPropertyRelative("mode");
                SerializedProperty valuesProp = e.FindPropertyRelative("values");
                modeProp.intValue = (int)CurveMode.Table;
                valuesProp.arraySize = copy.Length;
                for (int i = 0; i < copy.Length; i++)
                    valuesProp.GetArrayElementAtIndex(i).floatValue = copy[i];
            }
        }

        private void DrawTable(SerializedProperty values, int stat)
        {
            int maxLevel = ExpectedTableLength(stat, maxLevelProp.intValue);
            if (values.arraySize != maxLevel &&
                GUILayout.Button($"Подогнать длину таблицы до {maxLevel} (сейчас {values.arraySize})", EditorStyles.miniButton))
            {
                int old = values.arraySize;
                float last = old > 0 ? values.GetArrayElementAtIndex(old - 1).floatValue : 0f;
                values.arraySize = maxLevel;
                for (int i = old; i < maxLevel; i++)
                    values.GetArrayElementAtIndex(i).floatValue = last;
            }

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 28f;

            const int perRow = 4;
            for (int i = 0; i < values.arraySize; i += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + perRow, values.arraySize); j++)
                {
                    EditorGUILayout.PropertyField(values.GetArrayElementAtIndex(j), new GUIContent("L" + (j + 1)));
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        #endregion

        #region Операции над массивом

        private void ChangeMode(SerializedProperty e, ProgressionEntry entry, int newMode)
        {
            int maxLevel = maxLevelProp.intValue;

            // Пытаемся сохранить форму: start/end берём из текущих значений.
            float[] current = entry.Bake(maxLevel);
            if (current.Length > 0)
            {
                e.FindPropertyRelative("start").floatValue = current[0];
                e.FindPropertyRelative("end").floatValue = current[current.Length - 1];
            }

            e.FindPropertyRelative("mode").intValue = newMode;

            if (newMode == (int)CurveMode.Table)
            {
                SerializedProperty valuesProp = e.FindPropertyRelative("values");
                valuesProp.arraySize = current.Length;
                for (int i = 0; i < current.Length; i++)
                    valuesProp.GetArrayElementAtIndex(i).floatValue = current[i];
            }
        }

        private void AddEntry(CharacterClass cls, Stat stat)
        {
            int index = dataProp.arraySize;
            dataProp.arraySize = index + 1;
            var entry = new ProgressionEntry
            {
                characterClass = cls,
                stat = stat,
                mode = CurveMode.Linear,
                start = 10f,
                end = 100f,
                rounding = ValueRounding.Integer
            };
            WriteEntry(dataProp.GetArrayElementAtIndex(index), entry);
            openClasses.Add((int)cls);
            openEntries.Add(EntryKey((int)cls, (int)stat));
        }

        private void CopyClass(CharacterClass source, CharacterClass destination)
        {
            var copies = new List<ProgressionEntry>();
            var existing = new HashSet<int>();

            for (int i = 0; i < dataProp.arraySize; i++)
            {
                SerializedProperty e = dataProp.GetArrayElementAtIndex(i);
                int cls = e.FindPropertyRelative("characterClass").intValue;
                int stat = e.FindPropertyRelative("stat").intValue;

                if (cls == (int)destination) existing.Add(stat);
                else if (cls == (int)source) copies.Add(ReadEntry(e));
            }

            foreach (ProgressionEntry copy in copies)
            {
                if (existing.Contains((int)copy.stat)) continue; // уже есть — не перезаписываем
                copy.characterClass = destination;

                int index = dataProp.arraySize;
                dataProp.arraySize = index + 1;
                WriteEntry(dataProp.GetArrayElementAtIndex(index), copy);
            }

            openClasses.Add((int)destination);
        }

        private void RemoveIndices(List<int> indices)
        {
            var sorted = new List<int>(indices);
            sorted.Sort();
            for (int i = sorted.Count - 1; i >= 0; i--)
                dataProp.DeleteArrayElementAtIndex(sorted[i]);
        }

        #endregion

        #region Чтение / запись / рисование

        /// <summary>У ExperienceToLevelUp на одну запись меньше: после последней игрок получает максимальный уровень.</summary>
        private static int ExpectedTableLength(int stat, int maxLevel)
        {
            return stat == (int)Stat.ExperienceToLevelUp ? Mathf.Max(1, maxLevel - 1) : maxLevel;
        }

        private static int EntryKey(int cls, int stat) => (cls << 16) | (stat & 0xFFFF);

        private static ProgressionEntry ReadEntry(SerializedProperty p)
        {
            var entry = new ProgressionEntry
            {
                characterClass = (CharacterClass)p.FindPropertyRelative("characterClass").intValue,
                stat = (Stat)p.FindPropertyRelative("stat").intValue,
                mode = (CurveMode)p.FindPropertyRelative("mode").intValue,
                start = p.FindPropertyRelative("start").floatValue,
                end = p.FindPropertyRelative("end").floatValue,
                shape = p.FindPropertyRelative("shape").floatValue,
                curve = p.FindPropertyRelative("curve").animationCurveValue,
                rounding = (ValueRounding)p.FindPropertyRelative("rounding").intValue
            };

            SerializedProperty values = p.FindPropertyRelative("values");
            entry.values = new float[values.arraySize];
            for (int i = 0; i < entry.values.Length; i++)
                entry.values[i] = values.GetArrayElementAtIndex(i).floatValue;

            return entry;
        }

        private static void WriteEntry(SerializedProperty p, ProgressionEntry e)
        {
            p.FindPropertyRelative("characterClass").intValue = (int)e.characterClass;
            p.FindPropertyRelative("stat").intValue = (int)e.stat;
            p.FindPropertyRelative("mode").intValue = (int)e.mode;
            p.FindPropertyRelative("start").floatValue = e.start;
            p.FindPropertyRelative("end").floatValue = e.end;
            p.FindPropertyRelative("shape").floatValue = e.shape;
            p.FindPropertyRelative("curve").animationCurveValue = e.curve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
            p.FindPropertyRelative("rounding").intValue = (int)e.rounding;

            SerializedProperty values = p.FindPropertyRelative("values");
            float[] source = e.values ?? new float[0];
            values.arraySize = source.Length;
            for (int i = 0; i < source.Length; i++)
                values.GetArrayElementAtIndex(i).floatValue = source[i];
        }

        private static string Format(float v)
        {
            return Mathf.Abs(v) >= 100f ? v.ToString("0") : v.ToString("0.##");
        }

        private static void DrawSparkline(Rect rect, float[] v)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.25f));
            if (Event.current.type != EventType.Repaint || v == null || v.Length < 2) return;

            float min = float.MaxValue, max = float.MinValue;
            foreach (float f in v)
            {
                if (f < min) min = f;
                if (f > max) max = f;
            }

            var points = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                float x = rect.x + 3f + (rect.width - 6f) * i / (v.Length - 1);
                float t = max - min > 1e-4f ? Mathf.InverseLerp(min, max, v[i]) : 0.5f;
                float y = rect.yMax - 3f - (rect.height - 6f) * t;
                points[i] = new Vector3(x, y, 0f);
            }

            Color previous = Handles.color;
            Handles.color = new Color(0.45f, 0.8f, 1f);
            Handles.DrawAAPolyLine(2f, points);
            Handles.color = previous;
        }

        #endregion
    }

    public static class ProgressionMenu
    {
        [MenuItem("RPG/Stats/Select Progression Asset")]
        private static void SelectProgression()
        {
            string[] guids = AssetDatabase.FindAssets("t:Progression");
            if (guids.Length == 0)
            {
                Debug.LogWarning("Progression не найден. Создайте: Create → RPG → Stats → Progression.");
                return;
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Progression>(AssetDatabase.GUIDToAssetPath(guids[0]));
            EditorUtility.FocusProjectWindow();
        }
    }
}
