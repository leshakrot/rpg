using UnityEngine;

namespace RPG.UI.RequirementText
{
	public class RequirementTextSpawner : MonoBehaviour
	{
		[SerializeField] private RequirementText _requirementTextPrefab = null;

		// Создает экземпляр текста с нужным сообщением
		public void Spawn(string message)
		{
			RequirementText instance = Instantiate<RequirementText>(_requirementTextPrefab, transform);
			instance.SetValue(message);
		}
        
		// Метод для менеджера, чтобы установить префаб
		public void SetRequirementTextPrefab(RequirementText prefab)
		{
			_requirementTextPrefab = prefab;
		}
	}
}