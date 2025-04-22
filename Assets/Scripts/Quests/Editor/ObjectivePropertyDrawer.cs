// Поместите этот скрипт в папку "Editor"
using UnityEngine;
using UnityEditor;
using RPG.Quests; // Ваше пространство имен
using GameDevTV.Utils; // Пространство имен для Condition

namespace RPG.Quests.Editor
{
	// Указываем, для какого типа данных этот PropertyDrawer
	[CustomPropertyDrawer(typeof(Quest.Objective))]
	public class ObjectivePropertyDrawer : PropertyDrawer
	{
		// Переопределяем метод отрисовки свойства
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Начинаем отрисовку свойства. Обязательно для корректной работы с префабами и Undo.
			EditorGUI.BeginProperty(position, label, property);

			// Используем стандартную отрисовку для вложенных полей,
			// но с заголовком, взятым из поля 'reference' или 'description', если reference пустое.
			string displayLabel = property.displayName; // Стандартный заголовок (обычно "Element 0", "Element 1"...)

			SerializedProperty referenceProp = property.FindPropertyRelative("reference");
			SerializedProperty descriptionProp = property.FindPropertyRelative("description");

			// Пытаемся сделать заголовок более информативным
			if (referenceProp != null && !string.IsNullOrWhiteSpace(referenceProp.stringValue))
			{
				displayLabel = $"Objective: {referenceProp.stringValue}";
			}
			else if (descriptionProp != null && !string.IsNullOrWhiteSpace(descriptionProp.stringValue))
			{
				// Обрезаем длинное описание
				string shortDesc = descriptionProp.stringValue.Length > 30
					? descriptionProp.stringValue.Substring(0, 27) + "..."
					: descriptionProp.stringValue;
				displayLabel = $"Objective: {shortDesc}";
			}

			// Рисуем стандартное поле со всеми вложенными элементами, но с нашим заголовком
			// foldout = true делает его раскрывающимся списком
			property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, displayLabel, true);


			// Если список раскрыт, рисуем его содержимое стандартным образом
			if (property.isExpanded)
			{
				EditorGUI.indentLevel++; // Увеличиваем отступ для вложенных полей
				// Начинаем рисовать со следующей строки
				Rect currentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

				// Рисуем все дочерние свойства по очереди
				DrawProperty(ref currentRect, property.FindPropertyRelative("reference"));
				DrawProperty(ref currentRect, property.FindPropertyRelative("description"));
				DrawProperty(ref currentRect, property.FindPropertyRelative("usesCondition"));
				// Показываем Condition только если галочка usesCondition включена
				if (property.FindPropertyRelative("usesCondition")?.boolValue ?? false)
				{
					DrawProperty(ref currentRect, property.FindPropertyRelative("completionCondition"));
				}
				DrawProperty(ref currentRect, property.FindPropertyRelative("hasProgress"));
				// Показываем requiredCount только если галочка hasProgress включена
				if (property.FindPropertyRelative("hasProgress")?.boolValue ?? false)
				{
					DrawProperty(ref currentRect, property.FindPropertyRelative("requiredCount"));
				}
				DrawProperty(ref currentRect, property.FindPropertyRelative("hiddenInitially"));
				// Показываем revealCondition только если галочка hiddenInitially включена
				if (property.FindPropertyRelative("hiddenInitially")?.boolValue ?? false)
				{
					DrawProperty(ref currentRect, property.FindPropertyRelative("revealCondition"));
				}

				EditorGUI.indentLevel--; // Уменьшаем отступ обратно
			}

			EditorGUI.EndProperty();
		}

		// Вспомогательный метод для отрисовки свойства и смещения Rect вниз
		private void DrawProperty(ref Rect currentRect, SerializedProperty prop)
		{
			if (prop == null) return; // Проверка на случай, если поле не найдено
			float height = EditorGUI.GetPropertyHeight(prop, true); // Получаем высоту свойства
			EditorGUI.PropertyField(currentRect, prop, true); // Рисуем свойство
			currentRect.y += height + EditorGUIUtility.standardVerticalSpacing; // Смещаем Y для следующего свойства
		}


		// Переопределяем метод для расчета высоты. Это ВАЖНО!
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float totalHeight = EditorGUIUtility.singleLineHeight; // Высота для Foldout заголовка

			// Если список раскрыт, добавляем высоту всех видимых дочерних свойств
			if (property.isExpanded)
			{
				totalHeight += GetChildPropertyHeight(property.FindPropertyRelative("reference"));
				totalHeight += GetChildPropertyHeight(property.FindPropertyRelative("description"));
				totalHeight += GetChildPropertyHeight(property.FindPropertyRelative("usesCondition"));
				if (property.FindPropertyRelative("usesCondition")?.boolValue ?? false)
				{
					totalHeight += GetChildPropertyHeight(property.FindPropertyRelative("completionCondition"));
				}
				totalHeight += GetChildPropertyHeight(property.FindPropertyRelative("hasProgress"));
				if (property.FindPropertyRelative("hasProgress")?.boolValue ?? false)
				{
					totalHeight += GetChildPropertyHeight(property.FindPropertyRelative("requiredCount"));
				}
				totalHeight += GetChildPropertyHeight(property.FindPropertyRelative("hiddenInitially"));
				if (property.FindPropertyRelative("hiddenInitially")?.boolValue ?? false)
				{
					totalHeight += GetChildPropertyHeight(property.FindPropertyRelative("revealCondition"));
				}
			}

			return totalHeight;
		}

		// Вспомогательный метод для получения высоты дочернего свойства с отступом
		private float GetChildPropertyHeight(SerializedProperty prop)
		{
			if (prop == null) return 0;
			return EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
		}
	}
}