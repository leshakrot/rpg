using UnityEngine;
using TMPro;

public class BookUI : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _title;
	[SerializeField] private TextMeshProUGUI _text;
	
	public void SetTitle(string title)
	{
		_title.text = title;
	}
	
	public void SetText(string text)
	{
		_text.text = text;
	}
}
