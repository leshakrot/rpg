using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Events;

namespace RPG.Combat
{
    [Serializable]
    public class DynamicSpawnData
    {
        [Tooltip("Условие для спавна (необязательно)")]
        public UnityEvent<SpawnConditionResult> spawnCondition;
        
        [Tooltip("Компоненты, которые будут добавлены на врага после спавна")]
        public List<ComponentToAdd> componentsToAdd = new List<ComponentToAdd>();
    }
    
    [Serializable]
    public class ComponentToAdd
    {
        [Tooltip("Тип компонента (полное имя класса, например: QuestProgress)")]
        public string componentTypeName;
        
        [Tooltip("Настройка компонента после добавления")]
        public UnityEvent<Component> onComponentAdded;
    }
    
    [Serializable]
    public class SpawnConditionResult
    {
        public bool canSpawn = true;
    }

    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
        [SerializeField] private float spawnRadius = 2f;
        [Tooltip("Вероятность того, что в этой точке появится враг, от 0 до 1")]
        [Range(0, 1)]
        [SerializeField] private float spawnChance = 0.7f;
        [SerializeField] private bool isOccupied = false;
        
        [Header("Динамический спавн")]
        [Tooltip("Использовать условный спавн для этой точки")]
        [SerializeField] private bool useDynamicSpawn = false;
        
        [Tooltip("Данные динамического спавна")]
        [SerializeField] private DynamicSpawnData dynamicSpawnData = new DynamicSpawnData();
        
        [Tooltip("Отображать визуальное представление точки спавна всегда, а не только при выборе")]
        [SerializeField] private bool alwaysShowGizmo = true;
        
        [Tooltip("Цвет точки спавна в неактивном состоянии")]
        [SerializeField] private Color gizmoColor = Color.green;
        
        [Tooltip("Цвет точки спавна в занятом состоянии")]
        [SerializeField] private Color occupiedGizmoColor = Color.red;

        [Header("Настройки поворота")]
        [Tooltip("Использовать случайный поворот врага")]
        [SerializeField] private bool useRandomRotation = true;

        [Tooltip("Минимальный угол поворота по Y (0-360)")]
        [Range(0, 360)]
        [SerializeField] private float minYRotation = 0f;

        [Tooltip("Максимальный угол поворота по Y (0-360)")]
        [Range(0, 360)]
        [SerializeField] private float maxYRotation = 360f;

        [Tooltip("Использовать направление точки спавна как базовое направление")]
        [SerializeField] private bool usePointDirection = false;

        // Метод для отображения визуализации всегда
        private void OnDrawGizmos()
        {
            if (alwaysShowGizmo)
            {
                DrawGizmo();
            }
        }
        
        // Метод для визуализации радиуса спавна в редакторе при выделении
        private void OnDrawGizmosSelected()
        {
            if (!alwaysShowGizmo)
            {
                DrawGizmo();
            }
            else
            {
                // Если точка выбрана, рисуем дополнительную подсветку
                Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, spawnRadius + 0.2f);
            }
            
            // Показываем информацию о точке спавна
            GUIStyle style = new GUIStyle();
            style.normal.textColor = isOccupied ? occupiedGizmoColor : gizmoColor;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            
            string infoText = gameObject.name;
            if (enemyPrefabs.Count > 0)
            {
                infoText += $"\nВрагов: {enemyPrefabs.Count}";
            }
            else
            {
                infoText += "\nНет врагов!";
            }
            
            infoText += $"\nШанс: {spawnChance * 100}%";
            if (isOccupied)
            {
                infoText += "\n[ЗАНЯТО]";
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, infoText, style);
#endif

            // Отображаем направление, если используется направление точки
            if (usePointDirection && useRandomRotation)
            {
                Gizmos.color = Color.blue;
                Vector3 direction = transform.forward * spawnRadius;
                Gizmos.DrawRay(transform.position, direction);
                
                // Отображаем диапазон углов
                if (maxYRotation - minYRotation < 360f)
                {
                    float baseAngle = transform.eulerAngles.y;
                    
                    // Рисуем минимальный угол
                    Quaternion minRotation = Quaternion.Euler(0, baseAngle + minYRotation, 0);
                    Gizmos.color = new Color(0, 0.5f, 1f, 0.5f);
                    Gizmos.DrawRay(transform.position, minRotation * Vector3.forward * spawnRadius);
                    
                    // Рисуем максимальный угол
                    Quaternion maxRotation = Quaternion.Euler(0, baseAngle + maxYRotation, 0);
                    Gizmos.DrawRay(transform.position, maxRotation * Vector3.forward * spawnRadius);
                }
            }
        }
        
        // Общий метод отрисовки Gizmo
        private void DrawGizmo()
        {
            Color mainColor = isOccupied ? occupiedGizmoColor : gizmoColor;
            
            // Рисуем центральную точку
            Gizmos.color = mainColor;
            Gizmos.DrawSphere(transform.position, 0.3f);
            
            // Рисуем область спавна
            Gizmos.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.2f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
            
            // Рисуем контур области спавна
            Gizmos.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.5f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            
            // Рисуем стрелку вверх
            Gizmos.DrawRay(transform.position, Vector3.up * 1.0f);
            
            // Рисуем направление, если это настроено
            if (usePointDirection && !Application.isPlaying)
            {
                Gizmos.color = new Color(0, 0, 1f, 0.5f);
                Gizmos.DrawRay(transform.position, transform.forward * 1.0f);
            }
        }

        // Получить случайного врага из списка
        public GameObject GetRandomEnemyPrefab()
        {
            if (enemyPrefabs.Count == 0) return null;
            int randomIndex = UnityEngine.Random.Range(0, enemyPrefabs.Count);
            return enemyPrefabs[randomIndex];
        }

        // Получить позицию для спавна (случайная в радиусе точки)
        public Vector3 GetSpawnPosition()
        {
            Vector3 randomPos = UnityEngine.Random.insideUnitSphere * spawnRadius;
            randomPos.y = 0; // Обеспечиваем, что враг появится на том же уровне Y
            
            Vector3 spawnPosition = transform.position + randomPos;
            
            // Проверяем, что позиция находится на NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return transform.position; // Если не нашли подходящей позиции, возвращаем исходную точку
        }

        // Получить случайный поворот для врага
        public Quaternion GetRandomRotation()
        {
            if (!useRandomRotation)
            {
                // Если случайный поворот отключен, возвращаем стандартный поворот
                return Quaternion.identity;
            }
            
            float baseAngle = usePointDirection ? transform.eulerAngles.y : 0f;
            float randomAngle = UnityEngine.Random.Range(minYRotation, maxYRotation);
            
            return Quaternion.Euler(0f, baseAngle + randomAngle, 0f);
        }

        // Получить шанс спавна
        public float GetSpawnChance()
        {
            return spawnChance;
        }

        // Проверка, занята ли точка
        public bool IsOccupied()
        {
            return isOccupied;
        }

        // Установить статус занятости
        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;
        }
        
        // Получить список врагов
        public List<GameObject> GetEnemyPrefabs()
        {
            return enemyPrefabs;
        }
        
        public bool UseDynamicSpawn()
        {
            return useDynamicSpawn;
        }
        
        public bool CanSpawn()
        {
            if (!useDynamicSpawn)
            {
                return true;
            }
            
            if (dynamicSpawnData.spawnCondition == null || dynamicSpawnData.spawnCondition.GetPersistentEventCount() == 0)
            {
                return true;
            }
            
            SpawnConditionResult result = new SpawnConditionResult();
            dynamicSpawnData.spawnCondition.Invoke(result);
            return result.canSpawn;
        }
        
        public void ApplyDynamicComponents(GameObject spawnedEnemy)
        {
            if (!useDynamicSpawn || spawnedEnemy == null)
            {
                return;
            }
            
            foreach (ComponentToAdd componentData in dynamicSpawnData.componentsToAdd)
            {
                if (string.IsNullOrEmpty(componentData.componentTypeName))
                {
                    continue;
                }
                
                Type componentType = FindComponentType(componentData.componentTypeName);
                if (componentType == null)
                {
                    Debug.LogWarning($"[SpawnPoint] Не удалось найти тип компонента: {componentData.componentTypeName}");
                    continue;
                }
                
                if (!typeof(Component).IsAssignableFrom(componentType))
                {
                    Debug.LogWarning($"[SpawnPoint] Тип {componentData.componentTypeName} не является компонентом");
                    continue;
                }
                
                Component existingComponent = spawnedEnemy.GetComponent(componentType);
                if (existingComponent == null)
                {
                    existingComponent = spawnedEnemy.AddComponent(componentType);
                }
                
                if (componentData.onComponentAdded != null && componentData.onComponentAdded.GetPersistentEventCount() > 0)
                {
                    componentData.onComponentAdded.Invoke(existingComponent);
                }
            }
        }
        
        private Type FindComponentType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }
            
            type = Type.GetType(typeName + ", Assembly-CSharp");
            if (type != null)
            {
                return type;
            }
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            
            return null;
        }
    }
} 