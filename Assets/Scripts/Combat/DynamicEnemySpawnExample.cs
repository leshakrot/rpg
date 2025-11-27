using UnityEngine;
using System.Collections.Generic;
using RPG.Combat;

public class DynamicEnemySpawnExample : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnLocation;
    
    [Header("Компоненты для добавления")]
    [Tooltip("Настроенный компонент QuestProgress как шаблон")]
    [SerializeField] private QuestProgress questProgressTemplate;
    
    public void SpawnQuestEnemy()
    {
        if (enemySpawner == null || enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner или enemyPrefab не настроены!");
            return;
        }
        
        Vector3 spawnPos = spawnLocation != null ? spawnLocation.position : transform.position;
        
        List<ComponentToAdd> components = new List<ComponentToAdd>
        {
            new ComponentToAdd
            {
                componentTemplate = questProgressTemplate,
                eventBinding = new ComponentEventBinding
                {
                    eventType = ComponentEventBinding.EventType.OnDie,
                    methodName = "AddProgress"
                }
            }
        };
        
        GameObject questEnemy = enemySpawner.SpawnDynamicEnemyWithComponents(
            spawnPos,
            enemyPrefab,
            components,
            OnEnemySpawned
        );
        
        Debug.Log($"Квестовый враг создан: {questEnemy.name}");
    }
    
    public void SpawnRandomEncounter()
    {
        if (enemySpawner == null || enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner или enemyPrefab не настроены!");
            return;
        }
        
        Vector3 spawnPos = spawnLocation != null ? spawnLocation.position : transform.position;
        
        GameObject randomEnemy = enemySpawner.SpawnDynamicEnemy(
            spawnPos,
            enemyPrefab,
            OnEnemySpawned
        );
        
        Debug.Log($"Случайная встреча создана: {randomEnemy.name}");
    }
    
    private void OnEnemySpawned(GameObject enemy)
    {
        Debug.Log($"Враг заспавнен и готов к использованию: {enemy.name}");
    }
}
