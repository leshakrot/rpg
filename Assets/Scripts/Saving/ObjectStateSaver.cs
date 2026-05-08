using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevTV.Saving;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[System.Serializable]
public class ObjectVisibilityData
{
    public string objectName;
    public bool isActive;
    
    public ObjectVisibilityData(string name, bool active)
    {
        objectName = name;
        isActive = active;
    }
}

[System.Serializable]
public class ObjectStatesData
{
    public List<ObjectVisibilityData> objectStates = new List<ObjectVisibilityData>();
}

public class ObjectStateSaver : MonoBehaviour, ISaveable
{
    [Header("Objects to Manage")]
    [SerializeField] private List<GameObject> managedObjects = new List<GameObject>();
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    public object CaptureState()
    {
        ObjectStatesData data = new ObjectStatesData();
        
        foreach (GameObject obj in managedObjects)
        {
            if (obj != null)
            {
                data.objectStates.Add(new ObjectVisibilityData(obj.name, obj.activeInHierarchy));
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"ObjectStateManager: Captured state for {data.objectStates.Count} objects");
        }
        
        return data;
    }
    
    public void RestoreState(object state)
    {
        // Обработка различных типов входных данных
        ObjectStatesData data = state switch
        {
            ObjectStatesData osd => osd,
            JObject jo => jo.ToObject<ObjectStatesData>(),
            _ => null
        };

        if (data == null || data.objectStates == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"ObjectStateManager: Could not deserialize state data (type: {state?.GetType()})");
            }
            return;
        }

        int restoredCount = 0;
        
        foreach (ObjectVisibilityData objectData in data.objectStates)
        {
            GameObject targetObject = FindManagedObjectByName(objectData.objectName);
            if (targetObject != null)
            {
                targetObject.SetActive(objectData.isActive);
                restoredCount++;
                
                if (debugMode)
                {
                    Debug.Log($"ObjectStateManager: Restored '{objectData.objectName}' to active={objectData.isActive}");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning($"ObjectStateManager: Could not find object '{objectData.objectName}' to restore state");
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"ObjectStateManager: Restored state for {restoredCount} objects");
        }
    }
    
    [ContextMenu("Show All Objects")]
    public void ShowAllObjects()
    {
        foreach (GameObject obj in managedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        
        if (debugMode)
        {
            Debug.Log("ObjectStateManager: All managed objects are now visible");
        }
    }
    
    [ContextMenu("Hide All Objects")]
    public void HideAllObjects()
    {
        foreach (GameObject obj in managedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        
        if (debugMode)
        {
            Debug.Log("ObjectStateManager: All managed objects are now hidden");
        }
    }
    
    public void SetObjectVisibility(GameObject obj, bool visible)
    {
        if (obj != null && managedObjects.Contains(obj))
        {
            obj.SetActive(visible);
            
            if (debugMode)
            {
                Debug.Log($"ObjectStateManager: Set {obj.name} visibility to {visible}");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning($"ObjectStateManager: Object {obj?.name} is not in managed objects list");
        }
    }
    
    public void AddManagedObject(GameObject obj)
    {
        if (obj != null && !managedObjects.Contains(obj))
        {
            managedObjects.Add(obj);
            
            if (debugMode)
            {
                Debug.Log($"ObjectStateManager: Added {obj.name} to managed objects");
            }
        }
    }
    
    public void RemoveManagedObject(GameObject obj)
    {
        if (managedObjects.Contains(obj))
        {
            managedObjects.Remove(obj);
            
            if (debugMode)
            {
                Debug.Log($"ObjectStateManager: Removed {obj.name} from managed objects");
            }
        }
    }
    
    public List<GameObject> GetManagedObjects()
    {
        return new List<GameObject>(managedObjects);
    }
    
    public int GetManagedObjectsCount()
    {
        return managedObjects.Count;
    }
    
    public bool IsObjectManaged(GameObject obj)
    {
        return managedObjects.Contains(obj);
    }
    
    private GameObject FindManagedObjectByName(string objectName)
    {
        foreach (GameObject obj in managedObjects)
        {
            if (obj != null && obj.name == objectName)
            {
                return obj;
            }
        }
        return null;
    }
}