using UnityEngine;
using TMPro;
using UnityEngine.UI;
using RPG.Stats;

namespace RPG.UI
{
	public class TraitUI : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI unassignedPointsText;
		[SerializeField] Button commitButton;
		
		TraitStore playerTraitStore = null;

		private void Start() 
		{
			playerTraitStore = GameObject.FindGameObjectWithTag("Player").GetComponent<TraitStore>();
			commitButton.onClick.AddListener(playerTraitStore.Commit);
		}
		
		private void Update()
		{
			unassignedPointsText.text = playerTraitStore.GetUnassignedPoints().ToString();
		}
	}
}
