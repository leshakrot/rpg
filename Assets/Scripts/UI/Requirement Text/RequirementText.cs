using UnityEngine;
using TMPro;

namespace RPG.UI.RequirementText
{
	public class RequirementText : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _requirementText = null;

		// Этот метод будет вызываться анимацией в конце, чтобы уничтожить объект
		public void DestroyText()
		{
			Destroy(gameObject);
		}

		// Устанавливаем текстовое сообщение
		public void SetValue(string message)
		{
			_requirementText.text = message;
		}
	}
}