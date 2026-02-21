using UnityEngine;
using System.Collections.Generic;
using GameDevTV.Utils;
using UnityEngine.Events;
using System;

namespace RPG.Combat
{
    [Serializable]
    public class ComponentToAdd
    {
        [Tooltip("Компонент-шаблон для копирования на врага")]
        [SerializeReference] public MonoBehaviour componentTemplate;
        
        [Tooltip("Событие для подключения (например, OnDie)")]
        public ComponentEventBinding eventBinding;
    }
    
    [Serializable]
    public class ComponentEventBinding
    {
        public enum EventType
        {
            None,
            OnDie
        }
        
        [Tooltip("Тип события для подписки")]
        public EventType eventType = EventType.None;
        
        [Tooltip("Метод для вызова при событии")]
        public string methodName;
    }
    
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
        [SerializeField] private float spawnRadius = 2f;
        [Tooltip("Вероятность того, что в этой точке появится враг, от 0 до 1")]
        [Range(0, 1)]
        [SerializeField] private float spawnChance = 0.7f;
        [SerializeField] private bool isOccupied = false;
        
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
        
        [Header("Условный спавн")]
        [Tooltip("Условия для спавна врага (для квестов, событий и т.д.)")]
        [SerializeField] private Condition spawnConditions;
        
        [Header("Патруль")]
        [Tooltip("Путь патрулирования для заспавненного врага (опционально). Перетащи сюда GameObject с компонентом PatrolPath.")]
        [SerializeField] private GameObject patrolPathObject;
        
        [Header("Динамические компоненты")]
        [Tooltip("Компоненты для добавления на заспавненного врага")]
        [SerializeField] private List<ComponentToAdd> componentsToAdd = new List<ComponentToAdd>();

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
            if (patrolPathObject != null)
            {
                infoText += "\n[ПАТРУЛЬ]";
            }
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
        
        public List<GameObject> GetEnemyPrefabs()
        {
            return enemyPrefabs;
        }
        
        public bool CheckSpawnConditions(IEnumerable<IPredicateEvaluator> evaluators)
        {
            if (spawnConditions == null)
            {
                return true;
            }
            return spawnConditions.Check(evaluators);
        }
        
        public List<ComponentToAdd> GetComponentsToAdd()
        {
            return componentsToAdd;
        }
        
        public GameObject GetPatrolPathObject()
        {
            return patrolPathObject;
        }

        public float GetSpawnRadius()
        {
            return spawnRadius;
        }
    }
} 
