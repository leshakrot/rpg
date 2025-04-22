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
		// Указываем высоту - нам нужна только одна строка
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Делим доступное пространство на две части: для предмета и для количества
			Rect itemRect = new Rect(position.x, position.y, position.width * 0.7f - 5, position.height); // 70% ширины под предмет
			Rect numberRect = new Rect(position.x + position.width * 0.7f, position.y, position.width * 0.3f, position.height); // 30% под количество

			// Находим свойства 'item' и 'number'
			SerializedProperty itemProp = property.FindPropertyRelative("item");
			SerializedProperty numberProp = property.FindPropertyRelative("number");

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


			EditorGUI.EndProperty();
		}
	}
}