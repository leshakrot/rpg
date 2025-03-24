using UnityEngine;
using TMPro;

public class BookReader : MonoBehaviour
{
	[SerializeField] private BookUI bookUI;
	
	
	private void Start()
	{
		bookUI.gameObject.SetActive(false);
	}
	
	public void OpenBook(string title, string text)
	{
		bookUI.SetTitle(title);
		bookUI.SetText(text);
		bookUI.gameObject.SetActive(true);
	}
	
	public void CloseBook()
	{
		bookUI.gameObject.SetActive(false);
	}
}
