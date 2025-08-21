using UnityEngine;
using RPG.Saving;

public class VisibilityTester : MonoBehaviour
{
    [SerializeField] private GameObject testObject;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("H pressed - Hiding object");
            if (testObject != null)
            {
                ObjectVisibilitySaver saver = testObject.GetComponent<ObjectVisibilitySaver>();
                if (saver != null)
                {
                    saver.SetVisible(false);
                }
                else
                {
                    Debug.LogError("ObjectVisibilitySaver not found on test object!");
                }
            }
        }
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("J pressed - Showing object");
            if (testObject != null)
            {
                ObjectVisibilitySaver saver = testObject.GetComponent<ObjectVisibilitySaver>();
                if (saver != null)
                {
                    saver.SetVisible(true);
                }
                else
                {
                    Debug.LogError("ObjectVisibilitySaver not found on test object!");
                }
            }
        }
        
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("K pressed - Checking object state");
            if (testObject != null)
            {
                Debug.Log($"Test object {testObject.name} is active: {testObject.activeInHierarchy}");
                ObjectVisibilitySaver saver = testObject.GetComponent<ObjectVisibilitySaver>();
                if (saver != null)
                {
                    Debug.Log("ObjectVisibilitySaver component found");
                }
                else
                {
                    Debug.LogError("ObjectVisibilitySaver not found on test object!");
                }
            }
        }
    }
}