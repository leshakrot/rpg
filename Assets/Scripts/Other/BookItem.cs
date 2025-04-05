using UnityEngine;
using GameDevTV.Inventories;

[CreateAssetMenu(fileName = "My Book", menuName = "RPG/Books", order = 0)]
public class BookItem : ActionItem
{
	[SerializeField][TextArea] private string text;
	
	public override bool Use(GameObject user)
	{
		if(user.TryGetComponent(out BookReader bookReader))
		{
			bookReader.OpenBook(this.GetDisplayName(), text);
			return true;
		}
		return false;
	}
}
