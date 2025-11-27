using UnityEngine;
using UnityEditor;
using GameDevTV.Utils; // Укажите правильное пространство имен для Condition
using System.Text;
using System.Collections.Generic; // Для списка предикатов

namespace RPG.Quests.Editor // Используйте ваше пространство имен для редактора или GameDevTV.Utils.Editor
{
	[CustomPropertyDrawer(typeof(Condition))]
	public class ConditionDrawer : PropertyDrawer
	{
		private float _lineHeight => EditorGUIUtility.singleLineHeight;
		private float _spacing => EditorGUIUtility.standardVerticalSpacing;
		private float _indentWidth = 15f;

		// --- Кеширование для производительности ---
		private SerializedProperty _andProperty;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Получаем основное свойство - массив 'and' (Disjunction[])
			_andProperty = property.FindPropertyRelative("_and");
			if (_andProperty == null || !_andProperty.isArray)
			{
				EditorGUI.HelpBox(position, "'and' array not found in Condition!", MessageType.Error);
				EditorGUI.EndProperty();
				return;
			}

			float currentY = position.y;

			// --- Заголовок поля Condition ---
			// Рисуем стандартный заголовок поля (например, "Completion Condition")
			Rect labelRect = new Rect(position.x, currentY, EditorGUIUtility.labelWidth, _lineHeight);
			EditorGUI.LabelField(labelRect, label);
			currentY += _lineHeight + _spacing;

			// Кнопка добавления первой/следующей AND-группы (справа от заголовка)
			Rect addAndGroupButtonRect = new Rect(position.xMax - 55f, position.y, 50f, _lineHeight);
			if (GUI.Button(addAndGroupButtonRect, "+ AND"))
			{
				_andProperty.arraySize++;
			}

			// Начинаем отрисовку с небольшим отступом
			Rect contentRect = EditorGUI.IndentedRect(new Rect(position.x, currentY, position.width, position.height - _lineHeight - _spacing));
			contentRect.x -= _indentWidth; // Компенсируем стандартный отступ IndentedRect
			currentY = contentRect.y;


			if (_andProperty.arraySize == 0)
			{
				Rect infoRect = new Rect(contentRect.x, currentY, contentRect.width, _lineHeight);
				EditorGUI.LabelField(infoRect, "No conditions set. Click '+ AND' to add a group.", EditorStyles.centeredGreyMiniLabel);
				currentY += _lineHeight + _spacing;
			}

			// --- Итерация по AND-группам (Disjunction) ---
			for (int i = 0; i < _andProperty.arraySize; i++)
			{
				// Горизонтальный разделитель и метка "AND" (для групп > 0)
				if (i > 0)
				{
					currentY += _spacing; // Доп. отступ перед разделителем
					Rect separatorRect = new Rect(contentRect.x, currentY, contentRect.width, 1);
					EditorGUI.DrawRect(separatorRect, Color.gray * 0.6f);
					currentY += _spacing;
					Rect andLabelRect = new Rect(contentRect.x, currentY, contentRect.width, _lineHeight);
					EditorGUI.LabelField(andLabelRect, "AND", EditorStyles.boldLabel);
					currentY += _lineHeight + _spacing;
				}

				// Кнопка удаления текущей AND-группы (справа)
				Rect removeAndGroupRect = new Rect(contentRect.xMax - 25f, currentY, 20f, _lineHeight);
				// Сдвигаем кнопку чуть выше, чтобы она была на уровне первого предиката группы (или кнопки +OR)
				removeAndGroupRect.y = currentY; // Позиционируем относительно начала группы

				if (GUI.Button(removeAndGroupRect, new GUIContent("X", "Remove this AND group")))
				{
					// Подтверждение перед удалением группы
					if (EditorUtility.DisplayDialog("Remove AND Group?", "Are you sure you want to remove this entire AND group and all its OR conditions?", "Yes", "Cancel"))
					{
						_andProperty.DeleteArrayElementAtIndex(i);
						property.serializedObject.ApplyModifiedProperties();
						GUIUtility.ExitGUI(); // Важно для предотвращения ошибок GUI
						break; // Выходим из цикла, т.к. массив изменился
					}
				}


				SerializedProperty disjunctionProp = _andProperty.GetArrayElementAtIndex(i);
				SerializedProperty orProperty = disjunctionProp.FindPropertyRelative("_or"); // Массив предикатов

				if (orProperty == null || !orProperty.isArray)
				{
					EditorGUI.HelpBox(new Rect(contentRect.x, currentY, contentRect.width, _lineHeight * 2), "'or' array not found!", MessageType.Error);
					currentY += _lineHeight * 2 + _spacing;
					continue; // Переходим к следующей AND-группе
				}

				// Сдвигаем вправо для предикатов внутри группы
				float predicateStartX = contentRect.x + _indentWidth;
				float predicateWidth = contentRect.width - _indentWidth; // Уменьшаем доступную ширину


				// --- Итерация по OR-предикатам (Predicate) ---
				for (int j = 0; j < orProperty.arraySize; j++)
				{
					// Метка "OR" (для предикатов > 0)
					if (j > 0)
					{
						Rect orLabelRect = new Rect(predicateStartX, currentY, predicateWidth, _lineHeight);
						EditorGUI.LabelField(orLabelRect, "OR", EditorStyles.miniBoldLabel);
						currentY += _lineHeight + _spacing;
					}

					SerializedProperty predicateProp = orProperty.GetArrayElementAtIndex(j);
					float predicateHeight = GetPredicateHeight(predicateProp); // Получаем высоту предиката
					Rect predicateRect = new Rect(predicateStartX, currentY, predicateWidth, predicateHeight);

					// Отрисовка самого предиката
					DrawPredicate(predicateRect, predicateProp);
					currentY += predicateHeight + _spacing;
				} // Конец цикла OR

				// Кнопка добавления нового OR-предиката в текущую AND-группу
				Rect addOrRect = new Rect(predicateStartX, currentY, predicateWidth, _lineHeight);
				if (GUI.Button(addOrRect, "+ Add OR Condition"))
				{
					orProperty.arraySize++;
					// Можно сбросить значения нового предиката, если нужно
					SerializedProperty newPredicate = orProperty.GetArrayElementAtIndex(orProperty.arraySize - 1);
					newPredicate.FindPropertyRelative("_negate").boolValue = false;
					newPredicate.FindPropertyRelative("_predicate").stringValue = ""; // Очищаем имя
					newPredicate.FindPropertyRelative("_parameters").ClearArray(); // Очищаем параметры
				}
				currentY += _lineHeight + _spacing * 2; // Доп. отступ после группы

			} // Конец цикла AND

			EditorGUI.EndProperty();
		}

		// --- Отрисовка одного предиката ---
		void DrawPredicate(Rect position, SerializedProperty predicateProperty)
		{
			SerializedProperty negateProp = predicateProperty.FindPropertyRelative("_negate");
			SerializedProperty predicateNameProp = predicateProperty.FindPropertyRelative("_predicate");
			SerializedProperty parametersProp = predicateProperty.FindPropertyRelative("_parameters");

			float buttonWidth = 20f; // Ширина кнопки удаления
			float negateWidth = 40f; // Ширина для "NOT"
			float fieldWidth = position.width - negateWidth - buttonWidth - _spacing; // Ширина для имени предиката
			float currentY = position.y;

			// 1. Кнопка удаления предиката (справа)
			Rect removeButtonRect = new Rect(position.xMax - buttonWidth, currentY, buttonWidth, _lineHeight);
			if (GUI.Button(removeButtonRect, new GUIContent("-", "Remove this condition")))
			{
				// Получаем индекс этого предиката в массиве 'or'
				// Это немного хак, но должно сработать для PropertyDrawer
				string path = predicateProperty.propertyPath; // e.g., and.Array.data[0].or.Array.data[1]
				int lastDot = path.LastIndexOf('.');
				string orArrayPath = path.Substring(0, lastDot);
				int indexToRemove = int.Parse(path.Substring(path.LastIndexOf('[') + 1).TrimEnd(']'));

				SerializedProperty orArrayProp = predicateProperty.serializedObject.FindProperty(orArrayPath);
				if (orArrayProp != null && orArrayProp.isArray)
				{
					orArrayProp.DeleteArrayElementAtIndex(indexToRemove);
					predicateProperty.serializedObject.ApplyModifiedProperties();
				}
				GUIUtility.ExitGUI(); // Важно
				return;
			}

			// 2. Галочка "NOT" (слева)
			Rect negateRect = new Rect(position.x, currentY, negateWidth, _lineHeight);
			Rect negateToggleRect = new Rect(position.x, currentY, 15f, _lineHeight);
			negateProp.boolValue = EditorGUI.Toggle(negateToggleRect, GUIContent.none, negateProp.boolValue);
			Rect negateLabelRect = new Rect(negateToggleRect.xMax, currentY, negateWidth - 15f, _lineHeight);
			EditorGUI.LabelField(negateLabelRect, new GUIContent("NOT", "Negate condition?"));


			// 3. Поле для имени предиката
			Rect nameRect = new Rect(negateRect.xMax + _spacing, currentY, fieldWidth, _lineHeight);
			// TODO: Заменить на выпадающий список доступных предикатов!
			int indent = EditorGUI.indentLevel; // Сохраняем и сбрасываем индентацию для поля
			EditorGUI.indentLevel = 0;
			EditorGUI.PropertyField(nameRect, predicateNameProp, GUIContent.none);
			EditorGUI.indentLevel = indent; // Восстанавливаем

			currentY += _lineHeight + _spacing;

			// 4. Параметры (под именем)
			Rect paramsRect = new Rect(position.x, currentY, position.width, 0); // Высота вычисляется ниже
			if (parametersProp != null && parametersProp.isArray)
			{
				paramsRect.height = EditorGUI.GetPropertyHeight(parametersProp, true);
				EditorGUI.PropertyField(paramsRect, parametersProp, new GUIContent("Parameters"), true);
			}
			else
			{
				// Обработка случая, если параметры не массив (маловероятно)
				paramsRect.height = _lineHeight;
				EditorGUI.LabelField(paramsRect, "Parameters property is not an array!");
			}
		}

		// --- Расчет высоты всего PropertyDrawer ---
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Начинаем с высоты заголовка
			float totalHeight = _lineHeight + _spacing;

			SerializedProperty andProp = property.FindPropertyRelative("_and");
			if (andProp == null || !andProp.isArray) return totalHeight; // Базовая высота, если нет данных

			if (andProp.arraySize == 0) {
				// Высота для текста "No conditions set..."
				totalHeight += _lineHeight + _spacing;
			}

			for (int i = 0; i < andProp.arraySize; i++)
			{
				if (i > 0)
				{
					// Высота для разделителя и метки "AND"
					totalHeight += _lineHeight + _spacing * 3; // Доп. отступ
				}

				// Считаем высоту для всех OR-предикатов внутри этой AND-группы
				SerializedProperty disjunctionProp = andProp.GetArrayElementAtIndex(i);
				SerializedProperty orProp = disjunctionProp.FindPropertyRelative("_or");
				if (orProp != null && orProp.isArray)
				{
					for (int j = 0; j < orProp.arraySize; j++)
					{
						if (j > 0)
						{
							// Высота для метки "OR"
							totalHeight += _lineHeight + _spacing;
						}
						// Высота для самого предиката
						SerializedProperty predicateProp = orProp.GetArrayElementAtIndex(j);
						totalHeight += GetPredicateHeight(predicateProp) + _spacing;
					}
					// Высота для кнопки "+ Add OR Condition"
					totalHeight += _lineHeight + _spacing * 2; // Доп. отступ в конце группы
				} else {
					// Высота для сообщения об ошибке (если 'or' не найден)
					totalHeight += _lineHeight * 2 + _spacing;
				}
			}
			// Небольшой финальный отступ
			totalHeight += _spacing;

			return totalHeight;
		}

		// --- Расчет высоты одного предиката ---
		float GetPredicateHeight(SerializedProperty predicateProperty)
		{
			// Высота для строки с NOT, именем предиката и кнопкой "-"
			float height = _lineHeight;

			// Добавляем высоту для параметров
			SerializedProperty parametersProp = predicateProperty.FindPropertyRelative("_parameters");
			if (parametersProp != null && parametersProp.isArray)
			{
				height += _spacing + EditorGUI.GetPropertyHeight(parametersProp, true);
			} else {
				height += _spacing + _lineHeight; // Высота для метки ошибки параметров
			}
			return height;
		}
	}
}