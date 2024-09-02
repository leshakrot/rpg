using UnityEngine;

public class Destroyer : MonoBehaviour
{
    [SerializeField] private GameObject _targetToDestroy = null;

    public void DestroyTarget()
    {
        Destroy(_targetToDestroy);
    }
}
