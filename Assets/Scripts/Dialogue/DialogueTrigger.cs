using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Dialogue
{
	/// <summary>
	/// Тип действия, которое может выполнить триггер
	/// </summary>
	public enum TriggerActionType
	{
		StartQuest,
		CompleteQuest,
		GiveItem,
		TakeItem,
		Custom
	}

	public class DialogueTrigger : MonoBehaviour
	{
		[Tooltip("Список действий, которые может выполнить этот триггер")]
		[SerializeField] private List<TriggerAction> _actions = new List<TriggerAction>();

		// Словарь для кастомных действий (используется для системы компаньонов и других расширений)
		private Dictionary<string, Action> _customActions = new Dictionary<string, Action>();

		/// <summary>
		/// Регистрация кастомного действия
		/// </summary>
		public void RegisterAction(string actionID, Action callback)
		{
			_customActions[actionID] = callback;
		}

		/// <summary>
		/// Удаление кастомного действия
		/// </summary>
		public void UnregisterAction(string actionID)
		{
			_customActions.Remove(actionID);
		}

		/// <summary>
		/// Вызов действия по ID
		/// </summary>
		public void Trigger(string actionID)
		{
			// Сначала проверяем кастомные действия
			if (_customActions.ContainsKey(actionID))
			{
				_customActions[actionID]?.Invoke();
				return;
			}

			// Затем проверяем стандартные действия
			foreach (TriggerAction action in _actions)
			{
				if (action.actionID == actionID)
				{
					ExecuteAction(action);
					return;
				}
			}

			Debug.LogWarning($"DialogueTrigger: Действие '{actionID}' не найдено на {gameObject.name}");
		}

		private void ExecuteAction(TriggerAction action)
		{
			switch (action.actionType)
			{
				case TriggerActionType.StartQuest:
					// Логика запуска квеста
					Debug.Log($"Запуск квеста: {action.actionID}");
					break;

				case TriggerActionType.CompleteQuest:
					// Логика завершения квеста
					Debug.Log($"Завершение квеста: {action.actionID}");
					break;

				case TriggerActionType.GiveItem:
					// Логика выдачи предмета
					Debug.Log($"Выдача предмета: {action.actionID}");
					break;

				case TriggerActionType.TakeItem:
					// Логика забора предмета
					Debug.Log($"Забор предмета: {action.actionID}");
					break;

				case TriggerActionType.Custom:
					// Кастомная логика
					Debug.Log($"Кастомное действие: {action.actionID}");
					break;
			}
		}
	}

	[System.Serializable]
	public class TriggerAction
	{
		public string actionID;
		public TriggerActionType actionType;
	}
}
