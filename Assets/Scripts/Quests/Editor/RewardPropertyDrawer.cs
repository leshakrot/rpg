// Поместите этот скрипт в папку "Editor"
using UnityEngine;
using UnityEditor;
using RPG.Quests; // Ваше пространство имен
using GameDevTV.Inventories; // Пространство имен для InventoryItem

namespace RPG.Quests.Editor
{
	[CustomPropertyDrawer(typeof(Quest.Reward))]
	public class RewardPropertyDrawer : PropertyDrawer
	{
		// Указываем высоту - нам нужны теперь две строки (для чекбокса тайной награды)
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Первая строка - предмет и количество
			float lineHeight = EditorGUIUtility.singleLineHeight;
			Rect firstLineRect = new Rect(position.x, position.y, position.width, lineHeight);
			
			// Делим доступное пространство на две части: для предмета и для количества
			Rect itemRect = new Rect(firstLineRect.x, firstLineRect.y, firstLineRect.width * 0.7f - 5, firstLineRect.height);
			Rect numberRect = new Rect(firstLineRect.x + firstLineRect.width * 0.7f, firstLineRect.y, firstLineRect.width * 0.3f, firstLineRect.height);

			// Находим свойства 'item', 'number' и 'isSecret'
			SerializedProperty itemProp = property.FindPropertyRelative("item");
			SerializedProperty numberProp = property.FindPropertyRelative("number");
			SerializedProperty isSecretProp = property.FindPropertyRelative("isSecret");

			// Рисуем поле для выбора предмета (ObjectField)
			if (itemProp != null) {
				EditorGUI.PropertyField(itemRect, itemProp, GUIContent.none); // GUIContent.none убирает стандартный лейбл "Item"
			} else {
				EditorGUI.LabelField(itemRect, "Item not found!");
			}

			// Рисуем поле для ввода числа
			if (numberProp != null) {
				EditorGUI.PropertyField(numberRect, numberProp, GUIContent.none); // GUIContent.none убирает стандартный лейбл "Number"
			} else {
				EditorGUI.LabelField(numberRect, "Num?");
			}

			// Вторая строка - чекбокс для тайной награды
			Rect secondLineRect = new Rect(position.x, position.y + lineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, lineHeight);
			
			if (isSecretProp != null) {
				EditorGUI.PropertyField(secondLineRect, isSecretProp, new GUIContent("Тайная награда", "Если отмечено, награда будет скрыта от игрока до завершения квеста"));
			}

			EditorGUI.EndProperty();
		}
	}
}