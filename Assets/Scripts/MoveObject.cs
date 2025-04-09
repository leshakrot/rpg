using UnityEngine;

public class MoveObject : MonoBehaviour
{
    public Transform targetPosition;
    public float speed = 5f;

    void Update()
    {
        if (!targetPosition.gameObject.activeSelf) return;

        transform.position = Vector3.Lerp(transform.position, targetPosition.position, Time.deltaTime * speed);
        transform.LookAt(targetPosition.position);
    }
}