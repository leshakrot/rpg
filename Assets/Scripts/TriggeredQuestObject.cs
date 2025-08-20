using UnityEngine;
using RPG.Control;
using GameDevTV.Saving;

public class TriggeredQuestObject : MonoBehaviour, ISaveable
{
	private bool _isTriggered;

	private void Start()
	{
		if(_isTriggered) SelfDestroy();
	}
	
	private void OnTriggerEnter(Collider other)
	{
		if(other.TryGetComponent(out PlayerController player) && !_isTriggered)
		{
			_isTriggered = true; 
		}           
	}

	public void SetTriggered()
    {
		_isTriggered = true;
	}
	
	public void SelfDestroy()
	{
		Destroy(gameObject);
	}

	public object CaptureState()
	{
		return _isTriggered;
	}

	public void RestoreState(object state)
	{
		_isTriggered = (bool)state;
	}
}
