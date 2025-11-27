using UnityEngine;
using RPG.Combat;

public class TestSpawnPointSerialization : MonoBehaviour
{
    void Start()
    {
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        
        foreach (SpawnPoint point in spawnPoints)
        {
            var components = point.GetComponentsToAdd();
            Debug.Log($"SpawnPoint {point.name}: componentsToAdd = {(components != null ? components.Count.ToString() : "null")}");
        }
    }
}
