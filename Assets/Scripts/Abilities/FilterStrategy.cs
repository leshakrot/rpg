using UnityEngine;
using System.Collections.Generic;

public abstract class FilterStrategy : ScriptableObject
{
	public abstract IEnumerable<GameObject>Filter(IEnumerable<GameObject> objectsToFilter);
}
