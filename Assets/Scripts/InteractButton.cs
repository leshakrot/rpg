using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractButton : MonoBehaviour
{
	[SerializeField] private Image interactionImage;
	[SerializeField] private TextMeshProUGUI interactionText;
	private Button button;
	
	
	private void Start()
	{
		GetComponent<Button>();
	}
	
	public void SetIcon(Sprite sprite)
	{
		interactionImage.sprite	= sprite;	
	}
	
	public void SetInteractionText(string text)
	{
		interactionText.text = text;
	}
}
