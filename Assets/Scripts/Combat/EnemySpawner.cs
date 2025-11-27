using UnityEngine;
using System.Collections.Generic;
using GameDevTV.Saving;
using System;
using UnityEngine.SceneManagement;
using RPG.SceneManagement;
using System.Collections;
using RPG.Stats;
using RPG.Core;
using Newtonsoft.Json;
using GameDevTV.Utils;
using System.Linq;
using UnityEngine.Events;

namespace RPG.Combat
{
	using RPG.Attributes;
    public class EnemySpawner : MonoBehaviour, ISaveable
    {
        [SerializeField] private int minGuaranteedSpawns = 3;
        [SerializeField] private int maxEnemiesPerScene = 10;
        [Tooltip("Если true, то враги будут респавниться при возвращении на сцену")]
        [SerializeField] private bool respawnOnRevisit = true;
        [SerializeField] private float respawnDelay = 1f;
        
        [Header("Респавн убитых врагов")]
        [Tooltip("Если true, убитые враги будут респавниться через некоторое время")]
        [SerializeField] private bool respawnDeadEnemies = false;
        [Tooltip("Время в секундах до респавна убитого врага")]
        [SerializeField] private float deadEnemyRespawnTime = 60f;
        [Tooltip("Максимальное количество одновременных респавнов мертвых врагов")]
        [SerializeField] private int maxSimultaneousRespawns = 3;
        
        [Header("Настройки зон спавна")]
        [SerializeField] private bool useSpawnZones = true;
        [Tooltip("Максимальное количество врагов на зону")]
        [SerializeField] private int maxEnemiesPerZone = 5;
        
        [Header("Автолевелинг врагов")]
        [Tooltip("Включить автоматическую установку уровня врагов равным уровню игрока")]
        [SerializeField] private bool useAutoLeveling = true;
        [Tooltip("Минимальное отклонение от уровня игрока (может быть отрицательным)")]
        [SerializeField] private int minLevelOffset = -1;
        [Tooltip("Максимальное отклонение от уровня игрока")]
        [SerializeField] private int maxLevelOffset = 2;
        
        [Header("Настройки спавна")]
        [Tooltip("Минимальная дистанция между врагами при спавне")]
        [SerializeField] private float minDistanceBetweenEnemies = 2.5f;
        [Tooltip("Максимальное количество попыток найти подходящую позицию")]
        [SerializeField] private int maxSpawnAttempts = 15;
        [Tooltip("Использовать проверку физических коллайдеров при спавне")]
        [SerializeField] private bool usePhysicsCheck = true;
        [Tooltip("Радиус проверки коллайдеров (должен соответствовать размеру врага)")]
        [SerializeField] private float physicsCheckRadius = 0.5f;
        
        [Header("Настройки удаления трупов")]
        [Tooltip("Автоматически удалять трупы врагов через некоторое время")]
        [SerializeField] private bool autoRemoveCorpses = true;
        [Tooltip("Время в секундах до удаления трупа врага")]
        [SerializeField] private float corpseRemovalTime = 10f;
        
        [Header("Отладка и визуализация")]
        [Tooltip("Выводить отладочную информацию о спавне врагов в консоль")]
        [SerializeField] private bool showDebugInfo = false;
        [Tooltip("Отрисовывать линии от спавнера к созданным врагам")]
        [SerializeField] private bool drawLinesInGame = false;
        [SerializeField] private Color debugLineColor = Color.yellow;
        
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private List<GameObject> conditionalEnemies = new List<GameObject>();
        private List<Vector3> pendingSpawnPositions = new List<Vector3>();
        private Dictionary<SpawnPoint, List<GameObject>> enemiesBySpawnPoint = new Dictionary<SpawnPoint, List<GameObject>>();
        private Dictionary<SpawnPoint, Coroutine> respawnCoroutines = new Dictionary<SpawnPoint, Coroutine>();
        private Dictionary<GameObject, Coroutine> corpseRemovalCoroutines = new Dictionary<GameObject, Coroutine>();
        private string currentSceneName;
        private bool isInitialized = false;
        private int playerLevel = 1;
        
        private void Awake()
        {
            // Подписываемся на событие загрузки сцены
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDestroy()
        {
            // Отписываемся от события при удалении объекта
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnDrawGizmos()
        {
            if (drawLinesInGame && Application.isPlaying)
            {
                Gizmos.color = debugLineColor;
                foreach (GameObject enemy in spawnedEnemies)
                {
                    if (enemy != null)
                    {
                        Gizmos.DrawLine(transform.position, enemy.transform.position);
                        
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(enemy.transform.position, minDistanceBetweenEnemies);
                        Gizmos.color = debugLineColor;
                    }
                }
                
                Gizmos.color = Color.yellow;
                foreach (Vector3 pendingPos in pendingSpawnPositions)
                {
                    Gizmos.DrawWireSphere(pendingPos, 0.5f);
                }
            }
        }
        
        private void Start()
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            
            if (!isInitialized)
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Start() - начинаем инициализацию для сцены {currentSceneName}");
                isInitialized = true;
                SpawnEnemies();
            }
            else
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Start() - пропускаем, уже инициализирован");
            }
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (showDebugInfo) Debug.Log($"[EnemySpawner] OnSceneLoaded вызван для сцены: {scene.name}, текущая: {currentSceneName}");
            
            if (scene.name != currentSceneName)
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Загружена новая сцена: {scene.name}");
                ClearEnemies();
                currentSceneName = scene.name;
                isInitialized = false;
            }
            else if (respawnOnRevisit && isInitialized)
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Повторное посещение сцены {scene.name}, переспавн врагов");
                ClearEnemies();
                isInitialized = false;
                StartCoroutine(SpawnEnemiesWithDelay());
            }
        }
        
        private IEnumerator SpawnEnemiesWithDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            SpawnEnemies();
        }
        
        public void SpawnEnemies()
        {
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Начинаем спавн врагов на сцене {currentSceneName}");
            
            // Обновляем уровень игрока перед спавном врагов
            if (useAutoLeveling)
            {
                UpdatePlayerLevel();
            }
            
            if (useSpawnZones)
            {
                SpawnEnemiesInZones();
            }
            else
            {
                SpawnEnemiesAtIndividualPoints();
            }
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Спавн завершен, создано {spawnedEnemies.Count} врагов");
        }
        
        // Спавн врагов с использованием зон
        private void SpawnEnemiesInZones()
        {
            SpawnZone[] zones = FindObjectsOfType<SpawnZone>();
            
            if (zones.Length == 0)
            {
                // Если зон нет, используем обычный спавн
                if (showDebugInfo) Debug.Log("[EnemySpawner] Зоны спавна не найдены, используем обычный спавн");
                SpawnEnemiesAtIndividualPoints();
                return;
            }
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Найдено {zones.Length} зон спавна");
            
            // Очищаем статус всех точек спавна
            SpawnPoint[] allPoints = FindObjectsOfType<SpawnPoint>();
            foreach (SpawnPoint point in allPoints)
            {
                point.SetOccupied(false);
            }
            
            // Обрабатываем каждую зону
            foreach (SpawnZone zone in zones)
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Обрабатываем зону '{zone.GetZoneName()}'");
                
                // Получаем активные точки в зависимости от режима спавна зоны
                List<SpawnPoint> activePoints = zone.GetActiveSpawnPoints();
                int zoneCap = Mathf.Min(maxEnemiesPerZone, activePoints.Count);
                
                if (showDebugInfo) 
                {
                    Debug.Log($"[EnemySpawner] Зона '{zone.GetZoneName()}': {activePoints.Count} активных точек" +
                               $", режим: {zone.GetSpawnMode()}, лимит: {zoneCap}");
                }
                
                // Ограничиваем общее количество врагов
                if (spawnedEnemies.Count >= maxEnemiesPerScene)
                {
                    if (showDebugInfo) Debug.Log("[EnemySpawner] Достигнут лимит врагов на сцене");
                    break;
                }
                
                // Спавним врагов в активных точках зоны
                int zoneEnemyCount = 0;
                foreach (SpawnPoint point in activePoints)
                {
                    if (zoneEnemyCount >= zoneCap) break;
                    if (spawnedEnemies.Count >= maxEnemiesPerScene) break;
                    
                    if (SpawnEnemyAtPoint(point))
                    {
                        zoneEnemyCount++;
                    }
                }
                
                if (showDebugInfo) Debug.Log($"[EnemySpawner] В зоне '{zone.GetZoneName()}' создано {zoneEnemyCount} врагов");
            }
            
            // Если не удалось заспавнить минимальное количество врагов через зоны,
            // добавляем врагов через отдельные точки
            if (spawnedEnemies.Count < minGuaranteedSpawns)
            {
                if (showDebugInfo) 
                {
                    Debug.Log($"[EnemySpawner] Создано {spawnedEnemies.Count} врагов, " +
                               $"минимум: {minGuaranteedSpawns}, добавляем еще {minGuaranteedSpawns - spawnedEnemies.Count}");
                }
                SpawnAdditionalGuaranteedEnemies(minGuaranteedSpawns - spawnedEnemies.Count);
            }
        }
        
        // Стандартный спавн через отдельные точки
        private void SpawnEnemiesAtIndividualPoints()
        {
            SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();
            
            if (spawnPoints.Length == 0)
            {
                Debug.LogWarning("[EnemySpawner] На сцене нет точек спавна врагов!");
                return;
            }
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Найдено {spawnPoints.Length} точек спавна");
            
            // Сначала сбрасываем статус занятости для всех точек
            foreach (SpawnPoint point in spawnPoints)
            {
                point.SetOccupied(false);
            }
            
            // Сначала спавним гарантированное количество врагов
            int guaranteedSpawns = Mathf.Min(minGuaranteedSpawns, spawnPoints.Length);
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Гарантированное количество врагов: {guaranteedSpawns}");
            
            // Создаем список индексов и перемешиваем его для случайного выбора точек
            List<int> indices = new List<int>();
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                indices.Add(i);
            }
            ShuffleList(indices);
            
            // Спавним гарантированное количество врагов
            int spawned = 0;
            for (int i = 0; i < guaranteedSpawns && i < indices.Count; i++)
            {
                SpawnPoint point = spawnPoints[indices[i]];
                if (SpawnEnemyAtPoint(point))
                {
                    spawned++;
                }
            }
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Создано {spawned} гарантированных врагов");
            
            // Затем спавним случайное количество врагов до максимума
            for (int i = guaranteedSpawns; i < spawnPoints.Length && spawnedEnemies.Count < maxEnemiesPerScene; i++)
            {
                SpawnPoint point = spawnPoints[indices[i]];
                
                // Проверяем шанс спавна для этой точки
                if (UnityEngine.Random.value <= point.GetSpawnChance())
                {
                    SpawnEnemyAtPoint(point);
                }
            }
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Всего создано {spawnedEnemies.Count} врагов");
        }
        
        // Дополнительный спавн для гарантированного минимума
        private void SpawnAdditionalGuaranteedEnemies(int count)
        {
            SpawnPoint[] availablePoints = FindObjectsOfType<SpawnPoint>();
            List<SpawnPoint> unoccupiedPoints = new List<SpawnPoint>();
            
            foreach (SpawnPoint point in availablePoints)
            {
                if (!point.IsOccupied())
                {
                    unoccupiedPoints.Add(point);
                }
            }
            
            if (unoccupiedPoints.Count == 0)
            {
                if (showDebugInfo) Debug.Log("[EnemySpawner] Нет свободных точек для дополнительного спавна");
                return;
            }
            
            // Перемешиваем список свободных точек
            ShuffleList(unoccupiedPoints);
            
            int additionalSpawned = 0;
            // Спавним дополнительных врагов
            for (int i = 0; i < count && i < unoccupiedPoints.Count; i++)
            {
                if (SpawnEnemyAtPoint(unoccupiedPoints[i]))
                {
                    additionalSpawned++;
                }
            }
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Дополнительно создано {additionalSpawned} врагов");
        }
        
        private bool SpawnEnemyAtPoint(SpawnPoint point)
        {
            if (!CheckSpawnConditions(point))
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Условия спавна не выполнены для точки {point.name}");
                return false;
            }
            
            GameObject enemyPrefab = point.GetRandomEnemyPrefab();
            if (enemyPrefab == null)
            {
                if (showDebugInfo) Debug.LogWarning($"[EnemySpawner] Точка {point.name} не имеет указанных врагов!");
                return false;
            }
            
            Vector3 spawnPosition = GetValidSpawnPosition(point);
            if (spawnPosition == Vector3.zero)
            {
                if (showDebugInfo) Debug.LogWarning($"[EnemySpawner] Не удалось найти подходящую позицию для спавна в точке {point.name}");
                return false;
            }
            
            pendingSpawnPositions.Add(spawnPosition);
            
            Quaternion randomRotation = point.GetRandomRotation();
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, randomRotation);
            
            pendingSpawnPositions.Remove(spawnPosition);
            
            spawnedEnemies.Add(enemy);
            
            if (!enemiesBySpawnPoint.ContainsKey(point))
            {
                enemiesBySpawnPoint[point] = new List<GameObject>();
            }
            enemiesBySpawnPoint[point].Add(enemy);
            
            if (useAutoLeveling)
            {
                SetEnemyLevel(enemy);
            }
            
            AddDynamicComponents(enemy, point);
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Создан враг {enemy.name} в позиции {spawnPosition} от точки {point.name}");
            
            var aiController = enemy.GetComponent<RPG.Control.AIController>();
            if (aiController != null)
            {
                aiController.SetSpawnPoint(point);
                aiController.Reset();
            }
            
            if (respawnDeadEnemies)
            {
                var health = enemy.GetComponent<Health>();
                if (health != null)
                {
                    health.onDie.RemoveAllListeners();
                    health.onDie.AddListener(() => OnEnemyDied(enemy, point));
                }
            }
            
            if (enemy.GetComponent<GameDevTV.Saving.SaveableEntity>() == null)
            {
                enemy.AddComponent<GameDevTV.Saving.SaveableEntity>();
            }
            
            point.SetOccupied(true);
            
            bool isConditionalSpawn = point.GetComponentsToAdd().Count > 0;
            if (isConditionalSpawn)
            {
                conditionalEnemies.Add(enemy);
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Враг {enemy.name} помечен как условный спавн");
            }
            
            return true;
        }
        
        private bool CheckSpawnConditions(SpawnPoint point)
        {
            IPredicateEvaluator[] evaluators = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IPredicateEvaluator>()
                .ToArray();
            
            return point.CheckSpawnConditions(evaluators);
        }
        
        private void AddDynamicComponents(GameObject enemy, SpawnPoint point)
        {
            List<ComponentToAdd> componentsToAdd = point.GetComponentsToAdd();
            if (componentsToAdd == null || componentsToAdd.Count == 0)
            {
                return;
            }
            
            foreach (ComponentToAdd componentData in componentsToAdd)
            {
                if (componentData.componentTemplate == null)
                {
                    if (showDebugInfo) Debug.LogWarning($"[EnemySpawner] Компонент-шаблон не указан на точке {point.name}");
                    continue;
                }
                
                Type componentType = componentData.componentTemplate.GetType();
                Component addedComponent = enemy.GetComponent(componentType);
                
                if (addedComponent == null)
                {
                    addedComponent = enemy.AddComponent(componentType);
                }
                
                CopyComponentValues(componentData.componentTemplate, addedComponent);
                
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Добавлен компонент {componentType.Name} на {enemy.name}");
                
                if (componentData.eventBinding != null && componentData.eventBinding.eventType != ComponentEventBinding.EventType.None)
                {
                    BindComponentToEvent(enemy, addedComponent, componentData.eventBinding);
                }
            }
        }
        
        private void CopyComponentValues(Component source, Component destination)
        {
            if (source == null || destination == null) return;
            
            Type componentType = source.GetType();
            if (componentType != destination.GetType())
            {
                Debug.LogError($"[EnemySpawner] Типы компонентов не совпадают: {componentType} != {destination.GetType()}");
                return;
            }
            
            string json = JsonUtility.ToJson(source);
            JsonUtility.FromJsonOverwrite(json, destination);
        }
        
        private void BindComponentToEvent(GameObject enemy, Component component, ComponentEventBinding binding)
        {
            if (binding.eventType == ComponentEventBinding.EventType.OnDie)
            {
                var health = enemy.GetComponent<Health>();
                if (health != null && !string.IsNullOrEmpty(binding.methodName))
                {
                    var method = component.GetType().GetMethod(binding.methodName, 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (method != null)
                    {
                        UnityAction action = () => method.Invoke(component, null);
                        health.onDie.AddListener(action);
                        
                        if (showDebugInfo) 
                        {
                            Debug.Log($"[EnemySpawner] Метод {binding.methodName} привязан к событию OnDie для {enemy.name}");
                        }
                    }
                    else if (showDebugInfo)
                    {
                        Debug.LogWarning($"[EnemySpawner] Метод {binding.methodName} не найден в компоненте {component.GetType().Name}");
                    }
                }
            }
        }
        
        private Vector3 GetValidSpawnPosition(SpawnPoint point)
        {
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector3 candidatePosition = point.GetSpawnPosition();
                
                if (IsValidSpawnPosition(candidatePosition))
                {
                    return candidatePosition;
                }
                
                if (showDebugInfo && attempt > maxSpawnAttempts / 2)
                {
                    Debug.Log($"[EnemySpawner] Попытка {attempt + 1}/{maxSpawnAttempts} для точки {point.name}");
                }
            }
            
            if (showDebugInfo)
            {
                Debug.LogWarning($"[EnemySpawner] Не удалось найти валидную позицию за {maxSpawnAttempts} попыток");
            }
            
            return Vector3.zero;
        }
        
        private bool IsValidSpawnPosition(Vector3 position)
        {
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Health enemyHealth = enemy.GetComponent<Health>();
                    if (enemyHealth != null && !enemyHealth.IsDead())
                    {
                        float distance = Vector3.Distance(position, enemy.transform.position);
                        if (distance < minDistanceBetweenEnemies)
                        {
                            if (showDebugInfo)
                            {
                                Debug.Log($"[EnemySpawner] Позиция отклонена - слишком близко к {enemy.name} (дистанция: {distance:F2})");
                            }
                            return false;
                        }
                    }
                }
            }
            
            foreach (Vector3 pendingPosition in pendingSpawnPositions)
            {
                float distance = Vector3.Distance(position, pendingPosition);
                if (distance < minDistanceBetweenEnemies)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"[EnemySpawner] Позиция отклонена - слишком близко к ожидающей позиции (дистанция: {distance:F2})");
                    }
                    return false;
                }
            }
            
            if (usePhysicsCheck)
            {
                Collider[] colliders = Physics.OverlapSphere(position, physicsCheckRadius);
                foreach (Collider collider in colliders)
                {
                    Health health = collider.GetComponent<Health>();
                    if (health != null && !health.IsDead())
                    {
                        if (showDebugInfo)
                        {
                            Debug.Log($"[EnemySpawner] Позиция отклонена - найден коллайдер {collider.gameObject.name}");
                        }
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        private void OnEnemyDied(GameObject deadEnemy, SpawnPoint spawnPoint)
        {
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Враг {deadEnemy.name} умер в точке {spawnPoint.name}");
            
            spawnedEnemies.Remove(deadEnemy);
            conditionalEnemies.Remove(deadEnemy);
            
            if (enemiesBySpawnPoint.ContainsKey(spawnPoint))
            {
                enemiesBySpawnPoint[spawnPoint].Remove(deadEnemy);
            }
            
            if (autoRemoveCorpses && deadEnemy != null)
            {
                if (corpseRemovalCoroutines.ContainsKey(deadEnemy) && corpseRemovalCoroutines[deadEnemy] != null)
                {
                    StopCoroutine(corpseRemovalCoroutines[deadEnemy]);
                }
                
                Coroutine removalCoroutine = StartCoroutine(RemoveCorpseAfterDelay(deadEnemy));
                corpseRemovalCoroutines[deadEnemy] = removalCoroutine;
            }
            
            if (respawnDeadEnemies)
            {
                if (!respawnCoroutines.ContainsKey(spawnPoint) || respawnCoroutines[spawnPoint] == null)
                {
                    respawnCoroutines[spawnPoint] = StartCoroutine(RespawnEnemyAfterDelay(spawnPoint));
                }
            }
        }
        
        private IEnumerator RemoveCorpseAfterDelay(GameObject corpse)
        {
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Труп {corpse.name} будет удален через {corpseRemovalTime}с");
            
            yield return new WaitForSeconds(corpseRemovalTime);
            
            if (corpse != null)
            {
                SaveableEntity saveableEntity = corpse.GetComponent<SaveableEntity>();
                if (saveableEntity != null)
                {
                    var savingWrapper = FindAnyObjectByType<RPG.SceneManagement.SavingWrapper>();
                    if (savingWrapper != null)
                    {
                        var savingSystem = savingWrapper.GetComponent<GameDevTV.Saving.SavingSystem>();
                        if (savingSystem != null)
                        {
                            var entityRegistry = typeof(GameDevTV.Saving.SavingSystem)
                                .GetField("entityRegistry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?.GetValue(savingSystem);
                            
                            if (entityRegistry != null)
                            {
                                var removeMethod = entityRegistry.GetType().GetMethod("Remove", 
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                
                                if (removeMethod != null)
                                {
                                    removeMethod.Invoke(entityRegistry, new object[] { saveableEntity.GetUniqueIdentifier() });
                                }
                            }
                        }
                    }
                }
                
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Удаляем труп {corpse.name}");
                corpseRemovalCoroutines.Remove(corpse);
                Destroy(corpse);
            }
        }
        
        // Корутина для респавна врага после задержки
        private IEnumerator RespawnEnemyAfterDelay(SpawnPoint spawnPoint)
        {
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Запущен таймер респавна для точки {spawnPoint.name} ({deadEnemyRespawnTime}с)");
            
            yield return new WaitForSeconds(deadEnemyRespawnTime);
            
            // Проверяем, не превышен ли лимит врагов на сцене
            if (spawnedEnemies.Count >= maxEnemiesPerScene)
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Респавн отменен - достигнут лимит врагов на сцене");
                respawnCoroutines.Remove(spawnPoint);
                yield break;
            }
            
            // Проверяем лимит одновременных респавнов
            int currentRespawns = 0;
            foreach (var coroutine in respawnCoroutines.Values)
            {
                if (coroutine != null) currentRespawns++;
            }
            
            if (currentRespawns > maxSimultaneousRespawns)
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Респавн отложен - слишком много одновременных респавнов");
                yield return new WaitForSeconds(5f); // Ждем еще немного
            }
            
            // Проверяем, что точка спавна все еще существует
            if (spawnPoint == null)
            {
                respawnCoroutines.Remove(spawnPoint);
                yield break;
            }
            
            // Спавним нового врага
            if (SpawnEnemyAtPoint(spawnPoint))
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Враг респавнился в точке {spawnPoint.name}");
            }
            
            // Удаляем корутину из словаря
            respawnCoroutines.Remove(spawnPoint);
        }
        
        // Метод для установки уровня врага на основе уровня игрока
        private void SetEnemyLevel(GameObject enemy)
        {
            BaseStats enemyStats = enemy.GetComponent<BaseStats>();
            if (enemyStats == null) return;
            
            // Обновляем уровень игрока перед установкой уровня врага
            UpdatePlayerLevel();
            
            // Рассчитываем случайное отклонение от уровня игрока
            int levelOffset = UnityEngine.Random.Range(minLevelOffset, maxLevelOffset + 1);
            int enemyLevel = Mathf.Max(1, playerLevel + levelOffset); // Уровень не может быть меньше 1
            
            // Устанавливаем уровень врага через рефлексию, так как startingLevel - приватное поле
            var field = typeof(BaseStats).GetField("_startingLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(enemyStats, enemyLevel);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[EnemySpawner] Установлен уровень {enemyLevel} для врага {enemy.name} " +
                              $"(уровень игрока: {playerLevel}, смещение: {levelOffset})");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("[EnemySpawner] Не удалось установить уровень врага - не найдено поле _startingLevel");
            }
        }
        
        // Получить текущий уровень игрока
        private void UpdatePlayerLevel()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                BaseStats playerStats = player.GetComponent<BaseStats>();
                if (playerStats != null)
                {
                    playerLevel = playerStats.GetLevel();
                    if (showDebugInfo) Debug.Log($"[EnemySpawner] Уровень игрока: {playerLevel}");
                }
                else if (showDebugInfo)
                {
                    Debug.LogWarning("[EnemySpawner] У игрока отсутствует компонент BaseStats");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("[EnemySpawner] Игрок не найден на сцене");
            }
        }
        
        private void ClearEnemies()
        {
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Очищаем список врагов, количество: {spawnedEnemies.Count}");
            
            foreach (var coroutine in respawnCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            respawnCoroutines.Clear();
            
            foreach (var kvp in corpseRemovalCoroutines)
            {
                if (kvp.Value != null)
                {
                    StopCoroutine(kvp.Value);
                }
            }
            corpseRemovalCoroutines.Clear();
            
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            
            spawnedEnemies.Clear();
            conditionalEnemies.Clear();
            pendingSpawnPositions.Clear();
            enemiesBySpawnPoint.Clear();
            
            SpawnPoint[] allPoints = FindObjectsOfType<SpawnPoint>();
            foreach (SpawnPoint point in allPoints)
            {
                point.SetOccupied(false);
            }
        }
        
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                int r = i + UnityEngine.Random.Range(0, n - i);
                T temp = list[i];
                list[i] = list[r];
                list[r] = temp;
            }
        }
        
        public GameObject SpawnDynamicEnemy(Vector3 position, GameObject enemyPrefab, Action<GameObject> onSpawnCallback = null)
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("[EnemySpawner] Попытка создать врага с null префабом");
                return null;
            }
            
            GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
            
            conditionalEnemies.Add(enemy);
            
            if (useAutoLeveling)
            {
                SetEnemyLevel(enemy);
            }
            
            if (enemy.GetComponent<GameDevTV.Saving.SaveableEntity>() == null)
            {
                enemy.AddComponent<GameDevTV.Saving.SaveableEntity>();
            }
            
            var health = enemy.GetComponent<Health>();
            if (health != null)
            {
                health.onDie.AddListener(() => OnDynamicEnemyDied(enemy));
            }
            
            onSpawnCallback?.Invoke(enemy);
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Динамически создан враг {enemy.name} в позиции {position}");
            
            return enemy;
        }
        
        public GameObject SpawnDynamicEnemyWithComponents(
            Vector3 position, 
            GameObject enemyPrefab, 
            List<ComponentToAdd> components,
            Action<GameObject> onSpawnCallback = null)
        {
            GameObject enemy = SpawnDynamicEnemy(position, enemyPrefab, onSpawnCallback);
            
            if (enemy != null && components != null && components.Count > 0)
            {
                foreach (ComponentToAdd componentData in components)
                {
                    if (componentData.componentTemplate == null)
                    {
                        if (showDebugInfo) Debug.LogWarning($"[EnemySpawner] Компонент-шаблон не указан");
                        continue;
                    }
                    
                    Type componentType = componentData.componentTemplate.GetType();
                    Component addedComponent = enemy.GetComponent(componentType);
                    
                    if (addedComponent == null)
                    {
                        addedComponent = enemy.AddComponent(componentType);
                    }
                    
                    CopyComponentValues(componentData.componentTemplate, addedComponent);
                    
                    if (componentData.eventBinding != null && componentData.eventBinding.eventType != ComponentEventBinding.EventType.None)
                    {
                        BindComponentToEvent(enemy, addedComponent, componentData.eventBinding);
                    }
                }
            }
            
            return enemy;
        }
        
        private void OnDynamicEnemyDied(GameObject enemy)
        {
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Динамический враг {enemy.name} умер");
            
            conditionalEnemies.Remove(enemy);
            
            if (autoRemoveCorpses && enemy != null)
            {
                if (corpseRemovalCoroutines.ContainsKey(enemy) && corpseRemovalCoroutines[enemy] != null)
                {
                    StopCoroutine(corpseRemovalCoroutines[enemy]);
                }
                
                Coroutine removalCoroutine = StartCoroutine(RemoveCorpseAfterDelay(enemy));
                corpseRemovalCoroutines[enemy] = removalCoroutine;
            }
        }
        
        public object CaptureState()
        {
            List<string> enemyData = new List<string>();
            
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Health health = enemy.GetComponent<Health>();
                    if (health != null && !health.IsDead())
                    {
                        SaveableEntity saveable = enemy.GetComponent<SaveableEntity>();
                        if (saveable != null)
                        {
                            enemyData.Add(saveable.GetUniqueIdentifier());
                        }
                    }
                }
            }
            
            return new SpawnerSaveData
            {
                enemyIds = enemyData.ToArray(),
                sceneName = currentSceneName,
                isInitialized = isInitialized
            };
        }
        
        public void RestoreState(object state)
        {
            if (showDebugInfo) Debug.Log($"[EnemySpawner] RestoreState вызван");
            
            SpawnerSaveData saveData = (SpawnerSaveData)state;
            
            currentSceneName = saveData.sceneName;
            
            if (saveData.enemyIds == null || saveData.enemyIds.Length == 0)
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Нет сохраненных врагов, пропускаем восстановление");
                isInitialized = false;
                return;
            }
            
            isInitialized = true;
            
            spawnedEnemies.Clear();
            enemiesBySpawnPoint.Clear();
            
            foreach (var kvp in corpseRemovalCoroutines)
            {
                if (kvp.Value != null)
                {
                    StopCoroutine(kvp.Value);
                }
            }
            corpseRemovalCoroutines.Clear();
            
            SaveableEntity[] entities = FindObjectsOfType<SaveableEntity>();
            
            int restoredCount = 0;
            foreach (SaveableEntity entity in entities)
            {
                string id = entity.GetUniqueIdentifier();
                
                if (Array.Exists(saveData.enemyIds, savedId => savedId == id))
                {
                    GameObject enemy = entity.gameObject;
                    Health health = enemy.GetComponent<Health>();
                    
                    if (health != null && !health.IsDead())
                    {
                        spawnedEnemies.Add(enemy);
                        restoredCount++;
                        
                        if (respawnDeadEnemies)
                        {
                            var aiController = enemy.GetComponent<RPG.Control.AIController>();
                            if (aiController != null)
                            {
                                SpawnPoint spawnPoint = aiController.GetSpawnPoint();
                                if (spawnPoint != null)
                                {
                                    if (!enemiesBySpawnPoint.ContainsKey(spawnPoint))
                                    {
                                        enemiesBySpawnPoint[spawnPoint] = new List<GameObject>();
                                    }
                                    enemiesBySpawnPoint[spawnPoint].Add(enemy);
                                    
                                    health.onDie.RemoveAllListeners();
                                    health.onDie.AddListener(() => OnEnemyDied(enemy, spawnPoint));
                                    
                                    spawnPoint.SetOccupied(true);
                                }
                            }
                        }
                        
                        if (showDebugInfo) Debug.Log($"[EnemySpawner] Восстановлен живой враг с ID {id}");
                    }
                    else if (health != null && health.IsDead() && autoRemoveCorpses)
                    {
                        if (showDebugInfo) Debug.Log($"[EnemySpawner] Найден мертвый враг с ID {id}, запускаем удаление трупа");
                        Coroutine removalCoroutine = StartCoroutine(RemoveCorpseAfterDelay(enemy));
                        corpseRemovalCoroutines[enemy] = removalCoroutine;
                    }
                }
            }
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Восстановлено состояние: {restoredCount} живых врагов из {saveData.enemyIds.Length} сохраненных");
        }
        
        // Класс для хранения данных при сохранении
        [Serializable]
        private struct SpawnerSaveData
        {
            [JsonProperty] public string[] enemyIds;
            [JsonProperty] public string sceneName;
            [JsonProperty] public bool isInitialized;
        }
    }
}