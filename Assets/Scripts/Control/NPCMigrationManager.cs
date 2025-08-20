using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameDevTV.Saving;
using Newtonsoft.Json;

namespace RPG.Control
{
    [System.Serializable]
    public class NPCMigrationData
    {
        [JsonProperty] public int npcId;
        [JsonProperty] public string npcName;
        [JsonProperty] public int targetSceneIndex;
        [JsonProperty] public SerializableVector3 targetPosition;
        [JsonProperty] public SerializableQuaternion targetRotation;
        [JsonProperty] public bool isActive;
    }

    [System.Serializable]
    public class MigrationManagerSaveData
    {
        [JsonProperty] public List<NPCMigrationData> migratedNPCs = new List<NPCMigrationData>();
    }

    public class NPCMigrationManager : MonoBehaviour, ISaveable
    {
        [Header("Migration Settings")]
        [SerializeField] private bool _enableCrossSceneMigration = true;
        [SerializeField] private bool _debugMigration = true;

        private static NPCMigrationManager _instance;
        public static NPCMigrationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<NPCMigrationManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("NPCMigrationManager");
                        _instance = go.AddComponent<NPCMigrationManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private List<NPCMigrationData> _migratedNPCs = new List<NPCMigrationData>();
        private Dictionary<int, PeacefulNPC> _currentSceneNPCs = new Dictionary<int, PeacefulNPC>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RefreshCurrentSceneNPCs();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshCurrentSceneNPCs();
            ProcessMigratedNPCs();
        }

        private void RefreshCurrentSceneNPCs()
        {
            _currentSceneNPCs.Clear();
            PeacefulNPC[] npcs = FindObjectsOfType<PeacefulNPC>();
            
            foreach (var npc in npcs)
            {
                if (!_currentSceneNPCs.ContainsKey(npc.NPCId))
                {
                    _currentSceneNPCs.Add(npc.NPCId, npc);
                }
            }

            if (_debugMigration)
            {
                Debug.Log($"Найдено {_currentSceneNPCs.Count} NPC в сцене {SceneManager.GetActiveScene().name}");
            }
        }

        public void RegisterNPCMigration(PeacefulNPC npc, int targetSceneIndex, Vector3 targetPosition)
        {
            if (!_enableCrossSceneMigration)
            {
                Debug.LogWarning("Миграция между сценами отключена");
                return;
            }

            var migrationData = new NPCMigrationData
            {
                npcId = npc.NPCId,
                npcName = npc.NPCName,
                targetSceneIndex = targetSceneIndex,
                targetPosition = new SerializableVector3(targetPosition),
                targetRotation = new SerializableQuaternion(npc.transform.rotation),
                isActive = npc.gameObject.activeSelf
            };

            // Удаляем предыдущие записи для этого NPC
            _migratedNPCs.RemoveAll(data => data.npcId == npc.NPCId);
            _migratedNPCs.Add(migrationData);

            if (_debugMigration)
            {
                Debug.Log($"Зарегистрирована миграция {npc.NPCName} (ID: {npc.NPCId}) в сцену {targetSceneIndex}");
            }

            // Деактивируем NPC в текущей сцене
            npc.gameObject.SetActive(false);
        }

        private void ProcessMigratedNPCs()
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            var npcsForThisScene = _migratedNPCs.Where(data => data.targetSceneIndex == currentSceneIndex).ToList();

            foreach (var migrationData in npcsForThisScene)
            {
                // Проверяем, есть ли уже NPC с таким ID в сцене
                if (_currentSceneNPCs.TryGetValue(migrationData.npcId, out PeacefulNPC existingNPC))
                {
                    // Перемещаем существующего NPC на сохраненную позицию
                    existingNPC.transform.position = migrationData.targetPosition.ToVector();
                    existingNPC.transform.rotation = migrationData.targetRotation.ToQuaternion();
                    existingNPC.gameObject.SetActive(migrationData.isActive);

                    if (_debugMigration)
                    {
                        Debug.Log($"Восстановлен мигрированный NPC {migrationData.npcName} в сцене {SceneManager.GetActiveScene().name}");
                    }
                }
                else
                {
                    // NPC не найден в сцене - возможно нужно создать или загрузить из префаба
                    if (_debugMigration)
                    {
                        Debug.LogWarning($"NPC {migrationData.npcName} (ID: {migrationData.npcId}) должен быть в этой сцене, но не найден");
                    }
                }
            }

            // Удаляем обработанные записи миграции
            _migratedNPCs.RemoveAll(data => data.targetSceneIndex == currentSceneIndex);
        }

        public void MigrateNPC(int npcId, int targetSceneIndex, Vector3 targetPosition)
        {
            if (_currentSceneNPCs.TryGetValue(npcId, out PeacefulNPC npc))
            {
                RegisterNPCMigration(npc, targetSceneIndex, targetPosition);
            }
            else
            {
                Debug.LogError($"NPC с ID {npcId} не найден в текущей сцене");
            }
        }

        public void MigrateNPCByName(string npcName, int targetSceneIndex, Vector3 targetPosition)
        {
            var npc = _currentSceneNPCs.Values.FirstOrDefault(n => n.NPCName == npcName);
            if (npc != null)
            {
                RegisterNPCMigration(npc, targetSceneIndex, targetPosition);
            }
            else
            {
                Debug.LogError($"NPC {npcName} не найден в текущей сцене");
            }
        }

        public bool IsNPCInScene(int npcId, int sceneIndex)
        {
            if (sceneIndex == SceneManager.GetActiveScene().buildIndex)
            {
                return _currentSceneNPCs.ContainsKey(npcId);
            }

            return _migratedNPCs.Any(data => data.npcId == npcId && data.targetSceneIndex == sceneIndex);
        }

        public List<string> GetNPCsInScene(int sceneIndex)
        {
            var result = new List<string>();

            if (sceneIndex == SceneManager.GetActiveScene().buildIndex)
            {
                result.AddRange(_currentSceneNPCs.Values.Select(npc => npc.NPCName));
            }

            result.AddRange(_migratedNPCs
                .Where(data => data.targetSceneIndex == sceneIndex)
                .Select(data => data.npcName));

            return result;
        }

        // Методы для ручного управления миграцией
        public void ForceReturnAllNPCsToOriginalScenes()
        {
            _migratedNPCs.Clear();
            
            foreach (var npc in _currentSceneNPCs.Values)
            {
                npc.gameObject.SetActive(true);
            }

            if (_debugMigration)
            {
                Debug.Log("Все NPC возвращены в исходные сцены");
            }
        }

        public void ClearMigrationData()
        {
            _migratedNPCs.Clear();
            
            if (_debugMigration)
            {
                Debug.Log("Данные миграции NPC очищены");
            }
        }

        // ISaveable Implementation
        public object CaptureState()
        {
            return new MigrationManagerSaveData
            {
                migratedNPCs = new List<NPCMigrationData>(_migratedNPCs)
            };
        }

        public void RestoreState(object state)
        {
            if (state is MigrationManagerSaveData saveData)
            {
                _migratedNPCs = saveData.migratedNPCs ?? new List<NPCMigrationData>();
                
                if (_debugMigration)
                {
                    Debug.Log($"Восстановлены данные миграции для {_migratedNPCs.Count} NPC");
                }
            }
        }

        // Debug методы
        [ContextMenu("Debug: Show Migration Data")]
        public void DebugShowMigrationData()
        {
            Debug.Log($"=== Migration Data ===");
            Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name} (Index: {SceneManager.GetActiveScene().buildIndex})");
            Debug.Log($"NPCs in current scene: {_currentSceneNPCs.Count}");
            Debug.Log($"Migrated NPCs: {_migratedNPCs.Count}");

            foreach (var data in _migratedNPCs)
            {
                Debug.Log($"- {data.npcName} (ID: {data.npcId}) -> Scene {data.targetSceneIndex}");
            }
        }
    }
}