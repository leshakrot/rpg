using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace RPG.Dialogue
{
	[Serializable]
	public class TriggerAction
	{
		[Tooltip("Уникальное имя действия — по нему вы будете вызывать этот элемент")]
		public string actionName;

		[Tooltip("Событие, которое будет вызвано при срабатывании")]
		public UnityEvent onTrigger;
	}

	public class DialogueTrigger : MonoBehaviour
	{
		[Tooltip("Список действий, которые может выполнить этот триггер")]
		[SerializeReference]
		[SerializeField]
		private List<TriggerAction> _actions = new List<TriggerAction>();

		private void OnValidate()
		{
			// Если сам список вдруг null — создаём новый
			if (_actions == null)
				_actions = new List<TriggerAction>();

			for (int i = 0; i < _actions.Count; i++)
			{
				// Если в списке на этой позиции null — создаём новый TriggerAction
				if (_actions[i] == null)
					_actions[i] = new TriggerAction();

				// Если внутри TriggerAction ещё нет UnityEvent — инициализируем его
				if (_actions[i].onTrigger == null)
					_actions[i].onTrigger = new UnityEvent();
			}
		}

		/// <summary>
		/// Вызывает событие с указанным именем.
		/// </summary>
		public void Trigger(string actionToTrigger)
		{
			if (string.IsNullOrEmpty(actionToTrigger))
				return;

			foreach (var action in _actions)
			{
				if (action.actionName == actionToTrigger)
				{
					action.onTrigger.Invoke();
					return;
				}
			}

			Debug.LogWarning($"Action \"{actionToTrigger}\" not found on {gameObject.name}");
		}

		/// <summary>
		/// Пример автоматического срабатывания при входе в коллайдер.
		/// </summary>
		private void OnTriggerEnter(Collider other)
		{
			Trigger("OnEnter");
		}

		/// <summary>
		/// Удобный хелпер для выдачи квестов из компонента QuestGiver по индексу.
		/// </summary>
		public void TriggerQuest(int questIndex)
		{
			var questGiver = GetComponent<Quests.QuestGiver>();
			if (questGiver != null)
				questGiver.GiveQuest(questIndex);
			else
				Debug.LogWarning($"No QuestGiver found on {gameObject.name}");
		}
	}
}
