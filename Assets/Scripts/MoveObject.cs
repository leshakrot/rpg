using UnityEngine;

public class MoveObject : MonoBehaviour
{
    public Transform targetPosition;
	public float speed = 5f;
	public bool isLookAt = true;

    void Update()
    {
        if (!targetPosition.gameObject.activeSelf) return;

        transform.position = Vector3.Lerp(transform.position, targetPosition.position, Time.deltaTime * speed);
	    if(isLookAt) transform.LookAt(targetPosition.position);
    }
}