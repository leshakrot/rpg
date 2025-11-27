using UnityEngine;
using RPG.Combat;

namespace RPG.Combat
{
    public class RandomEncounterTrigger : MonoBehaviour
    {
        [Header("Настройки спавна")]
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private GameObject[] enemyPrefabs;
        
        [Header("Параметры встречи")]
        [Tooltip("Вероятность случайной встречи при входе в триггер")]
        [Range(0f, 1f)]
        [SerializeField] private float encounterChance = 0.3f;
        
        [Tooltip("Минимальное время между встречами в секундах")]
        [SerializeField] private float cooldownTime = 30f;
        
        [Tooltip("Радиус спавна вокруг триггера")]
        [SerializeField] private float spawnRadius = 5f;
        
        [Header("Опциональные компоненты")]
        [Tooltip("Настроенный компонент QuestProgress как шаблон")]
        [SerializeField] private QuestProgress questProgressTemplate;
        [SerializeField] private bool addQuestProgress = false;
        
        private float lastEncounterTime = -999f;
        private bool playerInTrigger = false;
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            
            playerInTrigger = true;
            
            if (CanTriggerEncounter())
            {
                TriggerEncounter();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInTrigger = false;
            }
        }
        
        private bool CanTriggerEncounter()
        {
            if (spawner == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("[RandomEncounterTrigger] EnemySpawner или враги не настроены!");
                return false;
            }
            
            if (Time.time - lastEncounterTime < cooldownTime)
            {
                return false;
            }
            
            return Random.value <= encounterChance;
        }
        
        private void TriggerEncounter()
        {
            lastEncounterTime = Time.time;
            
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            if (addQuestProgress && questProgressTemplate != null)
            {
                SpawnWithQuestProgress(spawnPosition, enemyPrefab);
            }
            else
            {
                SpawnSimpleEnemy(spawnPosition, enemyPrefab);
            }
        }
        
        private void SpawnSimpleEnemy(Vector3 position, GameObject prefab)
        {
            GameObject enemy = spawner.SpawnDynamicEnemy(
                position,
                prefab,
                OnEnemySpawned
            );
            
            Debug.Log($"[RandomEncounterTrigger] Создана случайная встреча: {enemy.name}");
        }
        
        private void SpawnWithQuestProgress(Vector3 position, GameObject prefab)
        {
            var components = new System.Collections.Generic.List<ComponentToAdd>
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
            
            GameObject enemy = spawner.SpawnDynamicEnemyWithComponents(
                position,
                prefab,
                components,
                OnEnemySpawned
            );
            
            Debug.Log($"[RandomEncounterTrigger] Создана квестовая встреча: {enemy.name}");
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 spawnPosition = transform.position + offset;
            
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return transform.position;
        }
        
        private void OnEnemySpawned(GameObject enemy)
        {
            Debug.Log($"[RandomEncounterTrigger] Враг заспавнен: {enemy.name}");
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
    }
}
