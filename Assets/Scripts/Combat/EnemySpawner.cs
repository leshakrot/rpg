using UnityEngine;
using System.Collections.Generic;
using GameDevTV.Saving;
using System;
using UnityEngine.SceneManagement;
using RPG.SceneManagement;
using System.Collections;
using RPG.Stats;

namespace RPG.Combat
{
    public class EnemySpawner : MonoBehaviour, ISaveable
    {
        [SerializeField] private int minGuaranteedSpawns = 3;
        [SerializeField] private int maxEnemiesPerScene = 10;
        [Tooltip("Если true, то враги будут респавниться при возвращении на сцену")]
        [SerializeField] private bool respawnOnRevisit = true;
        [SerializeField] private float respawnDelay = 1f;
        
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
        
        [Header("Отладка и визуализация")]
        [Tooltip("Выводить отладочную информацию о спавне врагов в консоль")]
        [SerializeField] private bool showDebugInfo = false;
        [Tooltip("Отрисовывать линии от спавнера к созданным врагам")]
        [SerializeField] private bool drawLinesInGame = false;
        [SerializeField] private Color debugLineColor = Color.yellow;
        
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private string currentSceneName;
        private bool isInitialized = false;
        private int playerLevel = 1; // Значение по умолчанию
        
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
                    }
                }
            }
        }
        
        private void Start()
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            if (!isInitialized)
            {
                SpawnEnemies();
                isInitialized = true;
            }
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Проверяем, изменилась ли сцена
            if (scene.name != currentSceneName)
            {
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Загружена новая сцена: {scene.name}");
                ClearEnemies();
                currentSceneName = scene.name;
                
                // Небольшая задержка перед спавном, чтобы дать сцене время загрузиться полностью
                StartCoroutine(SpawnEnemiesWithDelay());
            }
            else if (respawnOnRevisit)
            {
                // Если вернулись на ту же сцену и нужно респавнить врагов
                if (showDebugInfo) Debug.Log($"[EnemySpawner] Повторное посещение сцены, переспавн врагов");
                ClearEnemies();
                StartCoroutine(SpawnEnemiesWithDelay());
            }
        }
        
        private IEnumerator SpawnEnemiesWithDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            SpawnEnemies();
        }
        
        // Основной метод спавна врагов
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
        
        // Метод для спавна врага в конкретной точке
        private bool SpawnEnemyAtPoint(SpawnPoint point)
        {
            if (point.IsOccupied()) return false;
            
            GameObject enemyPrefab = point.GetRandomEnemyPrefab();
            if (enemyPrefab == null)
            {
                if (showDebugInfo) Debug.LogWarning($"[EnemySpawner] Точка {point.name} не имеет указанных врагов!");
                return false;
            }
            
            Vector3 spawnPosition = point.GetSpawnPosition();
            // Получаем случайный поворот из точки спавна
            Quaternion randomRotation = point.GetRandomRotation();
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, randomRotation);
            
            // Добавляем врага в список и помечаем точку как занятую
            spawnedEnemies.Add(enemy);
            point.SetOccupied(true);
            
            // Устанавливаем уровень врага, если включен автолевелинг
            if (useAutoLeveling)
            {
                SetEnemyLevel(enemy);
            }
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Создан враг {enemy.name} в точке {point.name}");
            
            // Инициализируем врага
            var aiController = enemy.GetComponent<RPG.Control.AIController>();
            if (aiController != null)
            {
                aiController.Reset();
            }
            
            // Проверяем, есть ли у врага компонент SaveableEntity, если нет - добавляем
            if (enemy.GetComponent<GameDevTV.Saving.SaveableEntity>() == null)
            {
                enemy.AddComponent<GameDevTV.Saving.SaveableEntity>();
            }
            
            return true;
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
        
        // Очистка списка врагов (используется при смене сцены)
        private void ClearEnemies()
        {
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Очищаем список врагов, количество: {spawnedEnemies.Count}");
            
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            
            spawnedEnemies.Clear();
            
            // Сбрасываем статус занятости для всех точек
            SpawnPoint[] allPoints = FindObjectsOfType<SpawnPoint>();
            foreach (SpawnPoint point in allPoints)
            {
                point.SetOccupied(false);
            }
        }
        
        // Перемешивание списка (алгоритм Фишера-Йейтса)
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
        
        // Реализация интерфейса ISaveable
        public object CaptureState()
        {
            // Сохраняем только текущие позиции врагов и их идентификаторы
            List<string> enemyData = new List<string>();
            
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    SaveableEntity saveable = enemy.GetComponent<SaveableEntity>();
                    if (saveable != null)
                    {
                        // Сохраняем ID врага, который уже существует на сцене
                        enemyData.Add(saveable.GetUniqueIdentifier());
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
            SpawnerSaveData saveData = (SpawnerSaveData)state;
            
            // Восстанавливаем состояние системы
            currentSceneName = saveData.sceneName;
            isInitialized = saveData.isInitialized;
            
            // Очищаем существующий список врагов при загрузке
            spawnedEnemies.Clear();
            
            // Находим всех врагов на сцене с SaveableEntity
            SaveableEntity[] entities = FindObjectsOfType<SaveableEntity>();
            
            // Добавляем в список только тех врагов, чьи ID есть в сохраненных данных
            foreach (SaveableEntity entity in entities)
            {
                string id = entity.GetUniqueIdentifier();
                
                if (Array.Exists(saveData.enemyIds, savedId => savedId == id))
                {
                    // Этот враг был на сцене при сохранении, добавляем его в список
                    spawnedEnemies.Add(entity.gameObject);
                    
                    if (showDebugInfo) Debug.Log($"[EnemySpawner] Восстановлен враг с ID {id}");
                }
            }
            
            if (showDebugInfo) Debug.Log($"[EnemySpawner] Восстановлено состояние: {spawnedEnemies.Count} врагов");
        }
        
        // Класс для хранения данных при сохранении
        [Serializable]
        private struct SpawnerSaveData
        {
            public string[] enemyIds;
            public string sceneName;
            public bool isInitialized;
        }
    }
} 