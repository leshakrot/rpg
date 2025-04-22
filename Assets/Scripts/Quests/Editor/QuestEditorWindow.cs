// Поместите этот скрипт в папку "Editor"
using UnityEngine;
using UnityEditor;
using RPG.Quests; // Ваше пространство имен
using GameDevTV.Inventories;
using GameDevTV.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal; // Для ReorderableList

namespace RPG.Quests.Editor
{
	public class QuestEditorWindow : EditorWindow
	{
		// --- Переменные окна ---
		private List<Quest> allQuests = new List<Quest>();
		private Quest selectedQuest = null;
		private SerializedObject selectedQuestSerializedObject;

		// --- Reorderable Lists ---
		private ReorderableList objectiveListDrawer;
		private ReorderableList rewardListDrawer;

		// --- Отслеживание редактируемого элемента ---
		private int currentlyEditedObjectiveIndex = -1;
		// --- Кэширование свойств ---
		private SerializedProperty objectivesProp;
		private SerializedProperty rewardsProp;

		private Vector2 questListScrollPos;
		private Vector2 questDetailScrollPos;
		private const string defaultQuestPath = "Assets/Internal Assets/Quests"; // <--- ИЗМЕНИТЕ НА ВАШ ПУТЬ!

		[MenuItem("Window/RPG Tools/Quest Editor")]
		public static void ShowWindow()
		{
			QuestEditorWindow window = GetWindow<QuestEditorWindow>("Quest Editor");
			window.minSize = new Vector2(600, 400);
		}

		void OnEnable()
		{
			RefreshQuestList();
			// Восстанавливаем выбор, если окно было закрыто/открыто или после компиляции
			string selectedQuestPath = SessionState.GetString("QuestEditor_SelectedQuestPath", "");
			if (!string.IsNullOrEmpty(selectedQuestPath))
			{
				selectedQuest = AssetDatabase.LoadAssetAtPath<Quest>(selectedQuestPath);
			}

			if (Selection.activeObject is Quest selected && selected != selectedQuest)
			{
				SelectQuest(selected);
			}
			else if (selectedQuest != null) // Восстанавливаем состояние после компиляции, если selection сбросился
			{
				SelectQuest(selectedQuest); // Переинициализируем SerializedObject и списки
			}
		}

		void OnDisable()
		{
			// Сохраняем путь выбранного квеста при закрытии окна
			SessionState.SetString("QuestEditor_SelectedQuestPath", selectedQuest != null ? AssetDatabase.GetAssetPath(selectedQuest) : "");
		}


		void OnSelectionChange()
		{
			// Вызывается при изменении выбора в окне Project
			if (Selection.activeObject is Quest selected && selected != selectedQuest)
			{
				GUI.FocusControl(null); // Сбросить фокус перед сменой
				SelectQuest(selected);
				Repaint(); // Перерисовать окно, чтобы подсветить выбранный квест
			}
		}

		void RefreshQuestList()
		{
			allQuests.Clear();
			string[] guids = AssetDatabase.FindAssets("t:Quest");
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				Quest quest = AssetDatabase.LoadAssetAtPath<Quest>(path);
				if (quest != null) allQuests.Add(quest);
			}
			allQuests = allQuests.OrderBy(q => q.name).ToList();
		}

		void SelectQuest(Quest quest)
		{
			if (quest == null)
			{
				DeselectQuest();
				return;
			}

			selectedQuest = quest;
			selectedQuestSerializedObject = new SerializedObject(selectedQuest);
			objectivesProp = selectedQuestSerializedObject.FindProperty("_objectives");
			rewardsProp = selectedQuestSerializedObject.FindProperty("_rewards");
			currentlyEditedObjectiveIndex = -1;
			InitializeReorderableLists();

			// Сохраняем путь для восстановления после перезагрузки
			SessionState.SetString("QuestEditor_SelectedQuestPath", AssetDatabase.GetAssetPath(selectedQuest));

			// Не обязательно форсировать фокус окна Project
			// EditorUtility.FocusProjectWindow();
			// Selection.activeObject = quest;
		}

		void DeselectQuest()
		{
			selectedQuest = null;
			selectedQuestSerializedObject = null;
			objectivesProp = null;
			rewardsProp = null;
			objectiveListDrawer = null;
			rewardListDrawer = null;
			currentlyEditedObjectiveIndex = -1;
			SessionState.SetString("QuestEditor_SelectedQuestPath", ""); // Сбрасываем сохраненный путь
		}

		void InitializeReorderableLists()
		{
			if (selectedQuestSerializedObject == null) return;

			// --- Objectives ---
			if (objectivesProp != null)
			{
				objectiveListDrawer = new ReorderableList(selectedQuestSerializedObject, objectivesProp, true, true, true, true);
				objectiveListDrawer.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Objectives");
				objectiveListDrawer.drawElementCallback = DrawObjectiveElement; // Custom method
				objectiveListDrawer.elementHeightCallback = GetObjectiveElementHeight; // Custom method
				objectiveListDrawer.onAddCallback = (ReorderableList list) => {
					list.serializedProperty.arraySize++;
					list.index = list.serializedProperty.arraySize - 1;
					currentlyEditedObjectiveIndex = list.index;
					// Clear default values if needed
					SerializedProperty newElement = list.serializedProperty.GetArrayElementAtIndex(list.index);
					newElement.FindPropertyRelative("reference").stringValue = "";
					newElement.FindPropertyRelative("description").stringValue = "";
					newElement.FindPropertyRelative("usesCondition").boolValue = false;
					newElement.FindPropertyRelative("hasProgress").boolValue = false;
					newElement.FindPropertyRelative("requiredCount").intValue = 1;
					newElement.FindPropertyRelative("hiddenInitially").boolValue = false;
					// TODO: Reset Condition fields if possible/necessary
				};
				objectiveListDrawer.onReorderCallback = (ReorderableList list) => { currentlyEditedObjectiveIndex = -1; };
				objectiveListDrawer.onRemoveCallback = (ReorderableList list) => {
					// If the removed element was being edited, reset the index
					if(list.index == currentlyEditedObjectiveIndex) {
						currentlyEditedObjectiveIndex = -1;
					} else if (list.index < currentlyEditedObjectiveIndex) {
						// Adjust index if an element before the edited one was removed
						currentlyEditedObjectiveIndex--;
					}
					// Default remove behavior
					ReorderableList.defaultBehaviours.DoRemoveButton(list);
				};

			} else { objectiveListDrawer = null; }

			// --- Rewards ---
			if (rewardsProp != null)
			{
				rewardListDrawer = new ReorderableList(selectedQuestSerializedObject, rewardsProp, true, true, true, true);
				rewardListDrawer.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Rewards");
				rewardListDrawer.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					if (rewardListDrawer == null || rewardListDrawer.serializedProperty == null || index >= rewardListDrawer.serializedProperty.arraySize) return;
					SerializedProperty element = rewardListDrawer.serializedProperty.GetArrayElementAtIndex(index);
					rect.y += 2;
					rect.height = EditorGUI.GetPropertyHeight(element);
					EditorGUI.PropertyField(rect, element, GUIContent.none, true);
				};
				rewardListDrawer.elementHeightCallback = (int index) => {
					if (rewardListDrawer == null || rewardListDrawer.serializedProperty == null || index >= rewardListDrawer.serializedProperty.arraySize) return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					SerializedProperty element = rewardListDrawer.serializedProperty.GetArrayElementAtIndex(index);
					return EditorGUI.GetPropertyHeight(element) + EditorGUIUtility.standardVerticalSpacing;
				};
			} else { rewardListDrawer = null; }
		}

		void DrawObjectiveElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			// Защита от выхода за пределы массива (особенно важно после удаления элемента)
			if (objectivesProp == null || index >= objectivesProp.arraySize || index < 0) return;

			SerializedProperty element = objectivesProp.GetArrayElementAtIndex(index);
			bool isEditing = (index == currentlyEditedObjectiveIndex);

			rect.y += EditorGUIUtility.standardVerticalSpacing / 2;
			rect.height = EditorGUIUtility.singleLineHeight;

			if (isEditing)
			{
				// --- РИСУЕМ РАЗВЕРНУТЫЙ РЕЖИМ РЕДАКТИРОВАНИЯ ---
				float startY = rect.y;
				float currentY = startY;
				float startX = rect.x;
				float availableWidth = rect.width;

				// Обертка для отрисовки свойства, чтобы избежать повторений
				System.Func<string, bool, float> DrawPropertyField = (propName, useFullWidth) => {
					SerializedProperty prop = element.FindPropertyRelative(propName);
					if (prop == null) return 0; // Если свойство не найдено

					float propHeight = EditorGUI.GetPropertyHeight(prop, true);
					float fieldWidth = availableWidth; // По умолчанию полная ширина
					// Уменьшаем ширину для простых полей, чтобы выровнять их
					if (!useFullWidth && prop.propertyType != SerializedPropertyType.Generic) {
						fieldWidth = availableWidth * 0.9f; // Оставляем небольшой отступ справа
					}

					Rect propRect = new Rect(startX, currentY, fieldWidth, propHeight);

					// Небольшой отступ для вложенных блоков
					if (propName.Contains("Condition") || propName == "requiredCount")
					{
						propRect.x += 15f;
						propRect.width -= 15f;
					}


					EditorGUI.PropertyField(propRect, prop, true);
					return propHeight + EditorGUIUtility.standardVerticalSpacing;
				};


				// Кнопка Collapse справа вверху
				Rect collapseButtonRect = new Rect(rect.xMax - 70f, currentY, 65f, EditorGUIUtility.singleLineHeight);
				if (GUI.Button(collapseButtonRect, "Collapse"))
				{
					currentlyEditedObjectiveIndex = -1;
					GUIUtility.keyboardControl = 0;
					GUIUtility.ExitGUI();
				}
				// Сдвигаемся вниз под кнопку
				currentY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				// Рисуем поля
				currentY += DrawPropertyField("reference", true);
				currentY += DrawPropertyField("description", true);

				currentY += DrawPropertyField("usesCondition", false);
				if (element.FindPropertyRelative("usesCondition").boolValue)
				{
					currentY += DrawPropertyField("completionCondition", true);
				}

				currentY += DrawPropertyField("hasProgress", false);
				if (element.FindPropertyRelative("hasProgress").boolValue)
				{
					currentY += DrawPropertyField("requiredCount", false);
				}

				currentY += DrawPropertyField("hiddenInitially", false);
				if (element.FindPropertyRelative("hiddenInitially").boolValue)
				{
					currentY += DrawPropertyField("revealCondition", true);
				}

				// Можно добавить разделитель в конце редактируемого блока
				Rect separatorRect = new Rect(startX, currentY, availableWidth, 1);
				EditorGUI.DrawRect(separatorRect, Color.grey * 0.5f);


			}
			else
			{
				// --- РИСУЕМ КОМПАКТНУЮ СВОДКУ ---
				SerializedProperty referenceProp = element.FindPropertyRelative("reference");
				SerializedProperty descriptionProp = element.FindPropertyRelative("description");

				string summaryText = $"[{index}] {referenceProp.stringValue}";
				if (string.IsNullOrWhiteSpace(referenceProp.stringValue))
				{
					string desc = descriptionProp.stringValue;
					summaryText = $"[{index}] {(desc.Length > 40 ? desc.Substring(0, 37) + "..." : desc)}";
				}
				if (string.IsNullOrWhiteSpace(summaryText) || summaryText == $"[{index}] ") summaryText = $"[{index}] (Empty Objective)";


				Rect labelRect = new Rect(rect.x, rect.y, rect.width - 70f, EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(labelRect, summaryText, isActive ? EditorStyles.boldLabel : EditorStyles.label);

				Rect editButtonRect = new Rect(rect.x + rect.width - 65f, rect.y, 60f, EditorGUIUtility.singleLineHeight);
				if (GUI.Button(editButtonRect, "Edit"))
				{
					currentlyEditedObjectiveIndex = index;
					GUIUtility.keyboardControl = 0;
					GUIUtility.ExitGUI();
				}
			}
		}

		float GetObjectiveElementHeight(int index)
		{
			// Защита от выхода за пределы массива
			if (objectivesProp == null || index >= objectivesProp.arraySize || index < 0) return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			if (index == currentlyEditedObjectiveIndex)
			{
				// --- ВЫЧИСЛЯЕМ ВЫСОТУ ДЛЯ РЕЖИМА РЕДАКТИРОВАНИЯ ---
				float totalHeight = EditorGUIUtility.standardVerticalSpacing; // Отступ сверху
				totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Кнопка Collapse

				SerializedProperty element = objectivesProp.GetArrayElementAtIndex(index);

				System.Func<string, float> GetPropHeight = (propName) => {
					SerializedProperty prop = element.FindPropertyRelative(propName);
					// Возвращаем 0, если свойство не найдено, чтобы избежать ошибок
					return prop != null ? EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing : 0f;
				};


				totalHeight += GetPropHeight("reference");
				totalHeight += GetPropHeight("description");
				totalHeight += GetPropHeight("usesCondition");
				if (element.FindPropertyRelative("usesCondition")?.boolValue ?? false) // Добавим проверку на null
				{
					totalHeight += GetPropHeight("completionCondition");
				}
				totalHeight += GetPropHeight("hasProgress");
				if (element.FindPropertyRelative("hasProgress")?.boolValue ?? false) // Добавим проверку на null
				{
					totalHeight += GetPropHeight("requiredCount");
				}
				totalHeight += GetPropHeight("hiddenInitially");
				if (element.FindPropertyRelative("hiddenInitially")?.boolValue ?? false) // Добавим проверку на null
				{
					totalHeight += GetPropHeight("revealCondition");
				}
				totalHeight += EditorGUIUtility.standardVerticalSpacing; // Дополнительный отступ в конце + разделитель

				return totalHeight;
			}
			else
			{
				// --- ВОЗВРАЩАЕМ ФИКСИРОВАННУЮ ВЫСОТУ ДЛЯ СВОДКИ ---
				return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}
		}

		void CreateNewQuest()
		{
			GUI.FocusControl(null); // Сброс фокуса

			if (!AssetDatabase.IsValidFolder(defaultQuestPath))
			{
				string parentFolder = System.IO.Path.GetDirectoryName(defaultQuestPath);
				string newFolderName = System.IO.Path.GetFileName(defaultQuestPath);
				if (!AssetDatabase.IsValidFolder(parentFolder)) {
					Debug.LogError($"Quest Editor: Parent folder '{parentFolder}' for quests not found. Please create it or change 'defaultQuestPath' in QuestEditorWindow.cs");
					return;
				}
				AssetDatabase.CreateFolder(parentFolder, newFolderName);
				AssetDatabase.Refresh();
				Debug.Log($"Created quest folder at: {defaultQuestPath}");
			}

			// *** ИСПРАВЛЕНО: Добавлена строка создания экземпляра ***
			Quest newQuest = ScriptableObject.CreateInstance<Quest>();

			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(defaultQuestPath + "/New Quest.asset");
			AssetDatabase.CreateAsset(newQuest, assetPathAndName);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			RefreshQuestList();
			// *** ИСПРАВЛЕНО: Используем переменную newQuest ***
			SelectQuest(newQuest);
		}

		void DeleteSelectedQuest()
		{
			if (selectedQuest == null) return;
			GUI.FocusControl(null);

			// *** ИСПРАВЛЕНО: Добавлены аргументы в DisplayDialog ***
			if (EditorUtility.DisplayDialog("Delete Quest?",
				$"Are you sure you want to delete quest '{selectedQuest.name}'? This cannot be undone.",
				"Yes, Delete", "Cancel"))
			{
				string pathToDelete = AssetDatabase.GetAssetPath(selectedQuest);
				DeselectQuest();
				AssetDatabase.DeleteAsset(pathToDelete);
				AssetDatabase.Refresh();
				RefreshQuestList();
				Repaint();
				GUIUtility.ExitGUI();
			}
		}

		void OnGUI()
		{
			// Применяем изменения, если фокус ушел с элемента управления или при перерисовке Layout
			if (selectedQuestSerializedObject != null && Event.current.type == EventType.Layout) {
				selectedQuestSerializedObject.ApplyModifiedProperties();
			}

			EditorGUILayout.BeginHorizontal();
			DrawQuestListPanel();
			DrawQuestDetailsPanel();
			EditorGUILayout.EndHorizontal();

			// Применяем изменения в конце, чтобы учесть правки через ReorderableList и PropertyFields
			if (selectedQuestSerializedObject != null)
			{
				// Используем ApplyModifiedPropertiesWithoutUndo() чтобы избежать конфликтов с Undo/Redo ReorderableList
				selectedQuestSerializedObject.ApplyModifiedPropertiesWithoutUndo();
			}
		}

		void DrawQuestListPanel()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(200), GUILayout.ExpandHeight(true));
			GUILayout.Label("Quests", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			if (GUILayout.Button("Refresh List"))
			{
				GUI.FocusControl(null);
				RefreshQuestList();
			}
			if (GUILayout.Button("Create New Quest"))
			{
				GUI.FocusControl(null);
				CreateNewQuest();
			}

			EditorGUILayout.Space();
			GUILayout.Label("Existing Quests:");

			questListScrollPos = EditorGUILayout.BeginScrollView(questListScrollPos, GUILayout.ExpandWidth(true));

			if (allQuests.Count == 0) {
				GUILayout.Label("No quests found.", EditorStyles.centeredGreyMiniLabel);
			} else {
				// Используем копию на случай изменения списка во время итерации (хотя здесь не должно)
				var questsToDraw = new List<Quest>(allQuests);
				foreach (Quest loopQuest in questsToDraw) // *** ИСПРАВЛЕНО: Используем другую переменную цикла ***
				{
					// Пропускаем, если квест null (например, ассет удален некорректно)
					if (loopQuest == null) continue;

					GUI.backgroundColor = (loopQuest == selectedQuest) ? Color.cyan : Color.white;

					// *** ИСПРАВЛЕНО: Используем переменную цикла loopQuest ***
					if (GUILayout.Button(loopQuest.name))
					{
						GUI.FocusControl(null);
						// *** ИСПРАВЛЕНО: Передаем loopQuest в SelectQuest ***
						SelectQuest(loopQuest);
						GUIUtility.ExitGUI(); // Выходим, чтобы избежать ошибок Layout
					}
					GUI.backgroundColor = Color.white;
				}
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		void DrawQuestDetailsPanel()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			if (selectedQuest != null && selectedQuestSerializedObject != null)
			{
				selectedQuestSerializedObject.UpdateIfRequiredOrScript();

				questDetailScrollPos = EditorGUILayout.BeginScrollView(questDetailScrollPos);

				EditorGUILayout.LabelField("Editing Quest:", EditorStyles.boldLabel);
				EditorGUI.BeginChangeCheck();
				string currentName = selectedQuestSerializedObject.targetObject.name;
				string newName = EditorGUILayout.TextField("Quest Name (Asset Name)", currentName);
				if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newName) && newName != currentName)
				{
					string currentPath = AssetDatabase.GetAssetPath(selectedQuest);
					selectedQuestSerializedObject.ApplyModifiedPropertiesWithoutUndo(); // Сохраняем перед переименованием
					AssetDatabase.RenameAsset(currentPath, newName);
					Repaint(); // Обновляем имя в левой панели
				}
				EditorGUILayout.Space();

				if (objectiveListDrawer != null) { objectiveListDrawer.DoLayoutList(); }
				else { EditorGUILayout.HelpBox("Objectives list could not be initialized.", MessageType.Warning); }
				EditorGUILayout.Space();

				if (rewardListDrawer != null) { rewardListDrawer.DoLayoutList(); }
				else { EditorGUILayout.HelpBox("Rewards list could not be initialized.", MessageType.Warning); }
				EditorGUILayout.Space(20);

				GUI.color = Color.red;
				if (GUILayout.Button("Delete This Quest", GUILayout.Height(30))) { DeleteSelectedQuest(); }
				GUI.color = Color.white;

				EditorGUILayout.EndScrollView();
			}
			else { GUILayout.Label("Select a quest from the list to edit\nor create a new one.", EditorStyles.centeredGreyMiniLabel); }

			EditorGUILayout.EndVertical();
		}
	}
}