using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevTV.Saving;

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
    
    [Header("Auto Save/Load Settings")]
    [SerializeField] private bool autoSaveOnStateChange = true;
    [SerializeField] private bool autoLoadOnStart = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private SavingSystem savingSystem;
    
    private void Awake()
    {
        savingSystem = FindObjectOfType<SavingSystem>();
        if (savingSystem == null && debugMode)
        {
            Debug.LogWarning("SavingSystem not found in scene. ObjectStateManager will not save/load states.");
        }
    }
    
    private void Start()
    {
        if (autoLoadOnStart && savingSystem != null)
        {
            LoadStates();
        }
    }
    
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
        if (state is ObjectStatesData data)
        {
            int restoredCount = 0;
            
            foreach (ObjectVisibilityData objectData in data.objectStates)
            {
                GameObject targetObject = FindManagedObjectByName(objectData.objectName);
                if (targetObject != null)
                {
                    targetObject.SetActive(objectData.isActive);
                    restoredCount++;
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
    }
    
    [ContextMenu("Save Current States")]
    public void SaveStates()
    {
        if (savingSystem != null)
        {
            savingSystem.Save("objectStates");
            if (debugMode)
            {
                Debug.Log("ObjectStateManager: States saved to file");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("ObjectStateManager: Cannot save - SavingSystem not found");
        }
    }
    
    [ContextMenu("Load Saved States")]
    public void LoadStates()
    {
        if (savingSystem != null && savingSystem.SaveFileExists("objectStates"))
        {
            savingSystem.Load("objectStates");
            if (debugMode)
            {
                Debug.Log("ObjectStateManager: States loaded from file");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("ObjectStateManager: Cannot load - save file does not exist or SavingSystem not found");
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
        
        if (autoSaveOnStateChange)
        {
            SaveStates();
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
        
        if (autoSaveOnStateChange)
        {
            SaveStates();
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
            
            if (autoSaveOnStateChange)
            {
                SaveStates();
            }
            
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