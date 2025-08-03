using UnityEngine;
using System.Collections.Generic;

namespace RPG.Combat
{
    public class SpawnZone : MonoBehaviour
    {
        public enum SpawnMode
        {
            Random,         // Случайное количество врагов на основе activationRate
            Exact,          // Точное количество врагов
            RandomRange     // Случайное количество врагов в заданном диапазоне
        }
        
        [SerializeField] private string zoneName = "Новая зона";
        [SerializeField] private Color zoneColor = new Color(0.1f, 0.8f, 0.2f, 0.3f);
        [SerializeField] private Vector3 zoneSize = new Vector3(10f, 2f, 10f);
        
        [Header("Режим спавна")]
        [SerializeField] private SpawnMode spawnMode = SpawnMode.Random;
        
        [Tooltip("Процент точек, которые будут активны в этой зоне (от 0 до 1)")]
        [Range(0, 1)]
        [SerializeField] private float activationRate = 0.7f;
        
        [Tooltip("Точное количество врагов для спавна (для режима Exact)")]
        [SerializeField] private int exactEnemyCount = 3;
        
        [Tooltip("Минимальное количество врагов (для режима RandomRange)")]
        [SerializeField] private int minEnemyCount = 2;
        
        [Tooltip("Максимальное количество врагов (для режима RandomRange)")]
        [SerializeField] private int maxEnemyCount = 5;
        
        [Header("Дополнительные настройки")]
        [Tooltip("Если включено, то SpawnPoints будут автоматически добавлены к зоне при создании внутри неё")]
        [SerializeField] private bool autoAssignPoints = true;
        
        [Tooltip("Отображать визуальное представление зоны спавна всегда")]
        [SerializeField] private bool alwaysShowGizmo = true;
        
        [Tooltip("Отображать точки спавна внутри зоны")]
        [SerializeField] private bool showContainedSpawnPoints = true;
        
        [Tooltip("Отображать дополнительную информацию о зоне")]
        [SerializeField] private bool showZoneInfo = true;

        private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

        private void OnDrawGizmos()
        {
            if (alwaysShowGizmo)
            {
                DrawZoneGizmo();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!alwaysShowGizmo)
            {
                DrawZoneGizmo();
            }
            
            // Дополнительная подсветка при выделении
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            Gizmos.DrawWireCube(transform.position, zoneSize + new Vector3(0.2f, 0.2f, 0.2f));
            
            // Отображаем информацию о зоне, если включено
            if (showZoneInfo)
            {
                RefreshSpawnPoints();
                
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontStyle = FontStyle.Bold;
                
                string infoText = zoneName;
                infoText += $"\nТочек спавна: {spawnPoints.Count}";
                
                switch (spawnMode)
                {
                    case SpawnMode.Random:
                        int activeCount = Mathf.Max(1, Mathf.RoundToInt(spawnPoints.Count * activationRate));
                        infoText += $"\nРежим: Случайный ({activeCount} из {spawnPoints.Count})";
                        break;
                    case SpawnMode.Exact:
                        infoText += $"\nРежим: Точный ({exactEnemyCount})";
                        break;
                    case SpawnMode.RandomRange:
                        infoText += $"\nРежим: Диапазон ({minEnemyCount}-{maxEnemyCount})";
                        break;
                }

#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * (zoneSize.y/2 + 1f), infoText, style);
#endif
            }
            
            // Отображаем все точки спавна в зоне, если включено
            if (showContainedSpawnPoints)
            {
                RefreshSpawnPoints();
                
                foreach (SpawnPoint point in spawnPoints)
                {
                    // Соединяем центр зоны с точкой спавна линией
                    Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.8f);
                    Gizmos.DrawLine(transform.position, point.transform.position);
                }
            }
        }
        
        private void DrawZoneGizmo()
        {
            // Рисуем основной куб зоны
            Gizmos.color = zoneColor;
            Gizmos.DrawCube(transform.position, zoneSize);
            
            // Рисуем каркас куба
            Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.8f);
            Gizmos.DrawWireCube(transform.position, zoneSize);
            
            // Рисуем стрелку вверх
            Gizmos.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 1f);
            Gizmos.DrawRay(transform.position + new Vector3(0, zoneSize.y/2, 0), Vector3.up * 1.5f);
            
            // Добавляем имя зоны, если showZoneInfo выключен, но alwaysShowGizmo включен
            if (alwaysShowGizmo && !showZoneInfo)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * (zoneSize.y/2 + 0.5f), zoneName, style);
#endif
            }
        }

        public string GetZoneName()
        {
            return zoneName;
        }

        public bool IsInZone(Vector3 position)
        {
            // Проверяем, находится ли позиция внутри зоны
            Vector3 localPos = transform.InverseTransformPoint(position);
            return Mathf.Abs(localPos.x) <= zoneSize.x / 2 &&
                   Mathf.Abs(localPos.y) <= zoneSize.y / 2 &&
                   Mathf.Abs(localPos.z) <= zoneSize.z / 2;
        }

        // Получить список точек спавна в этой зоне
        public List<SpawnPoint> GetSpawnPoints()
        {
            // Обновляем список точек в зоне
            RefreshSpawnPoints();
            return spawnPoints;
        }

        // Обновить список точек спавна в зоне
        public void RefreshSpawnPoints()
        {
            spawnPoints.Clear();
            SpawnPoint[] allPoints = FindObjectsOfType<SpawnPoint>();
            
            foreach (SpawnPoint point in allPoints)
            {
                if (IsInZone(point.transform.position))
                {
                    spawnPoints.Add(point);
                }
            }
        }

        // Получить активные точки спавна в зависимости от выбранного режима
        public List<SpawnPoint> GetActiveSpawnPoints()
        {
            RefreshSpawnPoints();
            
            if (spawnPoints.Count == 0) return new List<SpawnPoint>();
            
            // Создаем копию списка и перемешиваем его
            List<SpawnPoint> shuffledPoints = new List<SpawnPoint>(spawnPoints);
            ShuffleList(shuffledPoints);
            
            switch (spawnMode)
            {
                case SpawnMode.Random:
                    // Вычисляем, сколько точек будет активно на основе коэффициента
                    int activeCount = Mathf.Max(1, Mathf.RoundToInt(shuffledPoints.Count * activationRate));
                    return shuffledPoints.GetRange(0, Mathf.Min(activeCount, shuffledPoints.Count));
                    
                case SpawnMode.Exact:
                    // Возвращаем точное количество точек
                    return shuffledPoints.GetRange(0, Mathf.Min(exactEnemyCount, shuffledPoints.Count));
                    
                case SpawnMode.RandomRange:
                    // Случайное количество в заданном диапазоне
                    int randomCount = Random.Range(minEnemyCount, maxEnemyCount + 1);
                    return shuffledPoints.GetRange(0, Mathf.Min(randomCount, shuffledPoints.Count));
                    
                default:
                    return new List<SpawnPoint>();
            }
        }

        // Перемешивание списка (алгоритм Фишера-Йейтса)
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                int r = i + Random.Range(0, n - i);
                T temp = list[i];
                list[i] = list[r];
                list[r] = temp;
            }
        }

        // Активировать все точки в зоне
        public void ActivateAllPoints()
        {
            RefreshSpawnPoints();
            foreach (SpawnPoint point in spawnPoints)
            {
                point.SetOccupied(false);
            }
        }

        // Удалить все точки спавна внутри зоны
        public void ClearZone()
        {
            RefreshSpawnPoints();
            
#if UNITY_EDITOR
            if (spawnPoints.Count > 0 && 
                UnityEditor.EditorUtility.DisplayDialog("Подтверждение", 
                    $"Вы уверены, что хотите удалить все {spawnPoints.Count} точки спавна в зоне '{zoneName}'?", 
                    "Да", "Отмена"))
            {
                foreach (SpawnPoint point in new List<SpawnPoint>(spawnPoints))
                {
                    UnityEditor.Undo.DestroyObjectImmediate(point.gameObject);
                }
                spawnPoints.Clear();
            }
#endif
        }

        // Автоматическое создание точек спавна внутри зоны
        public void CreateSpawnPoints(int count)
        {
#if UNITY_EDITOR
            for (int i = 0; i < count; i++)
            {
                // Создаем случайную позицию внутри зоны
                Vector3 randomPos = new Vector3(
                    Random.Range(-zoneSize.x / 2, zoneSize.x / 2),
                    0, // Высота всегда 0 относительно зоны
                    Random.Range(-zoneSize.z / 2, zoneSize.z / 2)
                );
                
                // Преобразуем локальную позицию в мировую
                Vector3 worldPos = transform.TransformPoint(randomPos);
                
                // Создаем точку спавна
                GameObject spawnPoint = new GameObject($"Enemy Spawn Point ({zoneName})");
                spawnPoint.transform.position = worldPos;
                spawnPoint.AddComponent<SpawnPoint>();
                
                // Регистрируем для Undo
                UnityEditor.Undo.RegisterCreatedObjectUndo(spawnPoint, "Create Spawn Points");
            }
            
            // Обновляем список точек
            RefreshSpawnPoints();
#endif
        }
        
        // Получить режим спавна
        public SpawnMode GetSpawnMode()
        {
            return spawnMode;
        }
    }
} 