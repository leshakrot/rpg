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

        // ─────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (alwaysShowGizmo)
            {
                DrawGizmo();
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!alwaysShowGizmo)
            {
                DrawGizmo();
            }
            else
            {
                Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, spawnRadius + 0.2f);
            }
            
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
            if (patrolPathObject != null) infoText += "\n[ПАТРУЛЬ]";
            if (isOccupied)              infoText += "\n[ЗАНЯТО]";

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, infoText, style);
#endif

            if (usePointDirection && useRandomRotation)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, transform.forward * spawnRadius);
                
                if (maxYRotation - minYRotation < 360f)
                {
                    float baseAngle = transform.eulerAngles.y;
                    
                    Quaternion minRotation = Quaternion.Euler(0, baseAngle + minYRotation, 0);
                    Gizmos.color = new Color(0, 0.5f, 1f, 0.5f);
                    Gizmos.DrawRay(transform.position, minRotation * Vector3.forward * spawnRadius);
                    
                    Quaternion maxRotation = Quaternion.Euler(0, baseAngle + maxYRotation, 0);
                    Gizmos.DrawRay(transform.position, maxRotation * Vector3.forward * spawnRadius);
                }
            }
        }
        
        private void DrawGizmo()
        {
            Color mainColor = isOccupied ? occupiedGizmoColor : gizmoColor;
            
            Gizmos.color = mainColor;
            Gizmos.DrawSphere(transform.position, 0.3f);
            
            Gizmos.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.2f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
            
            Gizmos.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.5f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            
            Gizmos.DrawRay(transform.position, Vector3.up * 1.0f);
            
            if (usePointDirection && !Application.isPlaying)
            {
                Gizmos.color = new Color(0, 0, 1f, 0.5f);
                Gizmos.DrawRay(transform.position, transform.forward * 1.0f);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Публичный API
        // ─────────────────────────────────────────────────────────────────────

        public GameObject GetRandomEnemyPrefab()
        {
            if (enemyPrefabs.Count == 0) return null;
            return enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Count)];
        }

        public Vector3 GetSpawnPosition()
        {
            Vector3 randomPos = UnityEngine.Random.insideUnitSphere * spawnRadius;
            randomPos.y = 0;
            
            Vector3 spawnPosition = transform.position + randomPos;
            
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(
                    spawnPosition, out hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return transform.position;
        }

        public Quaternion GetRandomRotation()
        {
            if (!useRandomRotation) return Quaternion.identity;
            
            float baseAngle  = usePointDirection ? transform.eulerAngles.y : 0f;
            float randomAngle = UnityEngine.Random.Range(minYRotation, maxYRotation);
            return Quaternion.Euler(0f, baseAngle + randomAngle, 0f);
        }

        public float GetSpawnChance()       => spawnChance;
        public bool  IsOccupied()           => isOccupied;
        public void  SetOccupied(bool v)    => isOccupied = v;
        public List<GameObject> GetEnemyPrefabs()          => enemyPrefabs;
        public List<ComponentToAdd> GetComponentsToAdd()   => componentsToAdd;
        public GameObject GetPatrolPathObject()             => patrolPathObject;
        public float GetSpawnRadius()                       => spawnRadius;
        
        public bool CheckSpawnConditions(IEnumerable<IPredicateEvaluator> evaluators)
        {
            if (spawnConditions == null) return true;
            return spawnConditions.Check(evaluators);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Применение динамических компонентов к заспавненному врагу
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Добавляет все компоненты из <see cref="componentsToAdd"/> на
        /// <paramref name="spawnedEnemy"/>, копирует их сериализованные поля
        /// с шаблонов и подписывает на события.
        ///
        /// Вызывай этот метод из своего SpawnManager сразу после Instantiate.
        /// </summary>
        public void ApplyComponentsTo(GameObject spawnedEnemy)
        {
            foreach (var entry in componentsToAdd)
            {
                if (entry.componentTemplate == null)
                {
                    Debug.LogWarning(
                        $"[SpawnPoint] '{gameObject.name}': шаблон компонента не задан, пропускаем.");
                    continue;
                }

                // Добавляем компонент и копируем все [SerializeField]-поля.
                MonoBehaviour added = ComponentCopier.AddAndCopy(
                    entry.componentTemplate, spawnedEnemy);

                if (added == null) continue;

                // Привязка к событиям (расширяй switch по мере надобности).
                TryBindEvent(added, entry.eventBinding, spawnedEnemy);
            }
        }

        private void TryBindEvent(
            MonoBehaviour added,
            ComponentEventBinding binding,
            GameObject spawnedEnemy)
        {
            if (binding == null ||
                binding.eventType == ComponentEventBinding.EventType.None ||
                string.IsNullOrEmpty(binding.methodName))
            {
                return;
            }

            switch (binding.eventType)
            {
                case ComponentEventBinding.EventType.OnDie:
                    // Ищем компонент с событием OnDie на заспавненном враге.
                    // Замени Health на свой тип, в котором объявлено событие.
                    var health = spawnedEnemy.GetComponent<RPG.Attributes.Health>();
                    if (health == null)
                    {
                        Debug.LogWarning(
                            $"[SpawnPoint] Не нашли компонент Health на '{spawnedEnemy.name}' " +
                            $"для привязки события OnDie → {binding.methodName}.");
                        break;
                    }

                    var method = added.GetType().GetMethod(
                        binding.methodName,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);

                    if (method == null)
                    {
                        Debug.LogWarning(
                            $"[SpawnPoint] Метод '{binding.methodName}' не найден " +
                            $"в {added.GetType().Name}.");
                        break;
                    }

                    // Создаём делегат UnityAction и подписываем на onDie.
                    var action = (UnityAction)System.Delegate.CreateDelegate(
                        typeof(UnityAction), added, method);
                    health.onDie.AddListener(action);
                    break;

                // Добавляй другие типы событий здесь:
                // case ComponentEventBinding.EventType.OnLevelUp:
                //     ...
                //     break;

                default:
                    Debug.LogWarning(
                        $"[SpawnPoint] Неизвестный тип события: {binding.eventType}");
                    break;
            }
        }
    }
}
