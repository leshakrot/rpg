using UnityEngine;
using GameDevTV.Utils;
using RPG.SceneManagement;
using TMPro;

namespace  RPG.UI
{	
	public class MainMenuUI : MonoBehaviour
	{
		LazyValue<SavingWrapper> savingWrapper;
		
		[SerializeField] TMP_InputField newGameNameField;
		
		private void Awake()
		{
			savingWrapper = new LazyValue<SavingWrapper>(GetSavingWrapper);
		}
		
		private SavingWrapper GetSavingWrapper()
		{
			return FindObjectOfType<SavingWrapper>();
		}
		
		public void ContinueGame()
		{
			savingWrapper.value.ContinueGame();
		}
		
		public void NewGame()
		{			
			savingWrapper.value.NewGame(newGameNameField.text);
		}
		
		public void QuitGame()
		{
			Application.Quit();
		}
	}
}
