using UnityEngine;
using RPG.Control;
using GameDevTV.Saving;

public class TriggeredQuestObject : MonoBehaviour, ISaveable
{
    private bool _isTriggered;
    
    // Триггер игнорирует физические пересечения до конца первого кадра.
    // Это защищает от ложных срабатываний в WebGL, где физика может
    // зафиксировать коллизию в момент расстановки объектов при старте сцены.
    private bool _isReady;

    private void Start()
    {
        if (_isTriggered)
        {
            SelfDestroy();
            return;
        }
        
        // Откладываем активацию триггера на один физический кадр
        Invoke(nameof(SetReady), Time.fixedDeltaTime);
    }

    private void SetReady()
    {
        _isReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isReady) return;
        
        if (other.TryGetComponent(out PlayerController player) && !_isTriggered)
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
        // Start() мог уже отработать к этому моменту (особенно в WebGL).
        // Поэтому проверяем и здесь.
        if (_isTriggered)
        {
            SelfDestroy();
        }
    }
}
