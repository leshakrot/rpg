using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameDevTV.Saving;
using Newtonsoft.Json.Linq;

namespace RPG.Companions
{
    /// <summary>
    /// Глобальный менеджер компаньонов. Присутствует на каждой сцене через мегапрефаб.
    /// Ответственности:
    ///   1. Хранит состояние найма (сохраняется через ISaveable)
    ///   2. Спавнит активных компаньонов при загрузке каждой сцены
    ///   3. Регистрирует живые контроллеры
    /// </summary>
    public class CompanionManager : MonoBehaviour, ISaveable
    {
        [Header("Реестр компаньонов")]
        [SerializeField] private CompanionRegistry registry;

        // ── Singleton ─────────────────────────────────────────────────────

        private static CompanionManager _instance;
        public static CompanionManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindObjectOfType<CompanionManager>();
                return _instance;
            }
        }

        // ── данные ───────────────────────────────────────────────────────

        // companionID → состояние найма (сохраняется)
        private Dictionary<string, HiredCompanionInfo> _hired = new();

        // companionID → живой контроллер в текущей сцене (не сохраняется)
        private Dictionary<string, CompanionController> _active = new();

        // ── lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Мегапрефаб появляется на каждой сцене — уничтожаем дубликат,
                // оставляем тот что уже живёт
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Start() вызывается после RestoreState() — состояние найма уже актуально.
        /// Здесь спавним активных компаньонов на текущей сцене.
        /// </summary>
        private void Start()
        {
            SpawnActiveCompanions();
        }

        // ── спавн ─────────────────────────────────────────────────────────

        private void SpawnActiveCompanions()
        {
            if (registry == null)
            {
                Debug.LogWarning("[CompanionManager] registry не назначен — компаньоны не будут заспавнены.");
                return;
            }

            foreach (string id in GetActiveCompanionIDs())
            {
                // CompanionSpawner на родной сцене уже заспавнил его в своём Start()
                // (спавнер тоже вызывается после RestoreState, порядок не гарантирован)
                // Поэтому проверяем регистрацию — если уже есть, пропускаем
                if (IsCompanionRegistered(id)) continue;

                var entry = registry.GetEntry(id);
                if (entry == null || entry.prefab == null)
                {
                    Debug.LogWarning($"[CompanionManager] Нет записи в реестре для {id}");
                    continue;
                }

                GameObject player = GameObject.FindWithTag("Player");
                if (player == null) return;

                Vector3 spawnPos = player.transform.position
                                 + player.transform.right * 1.5f
                                 - player.transform.forward;

                GameObject instance = Instantiate(entry.prefab, spawnPos, Quaternion.identity);
                instance.name = entry.data.CompanionName;

                instance.GetComponent<CompanionController>()?.Activate(entry.data);
            }
        }

        // ── публичный API ─────────────────────────────────────────────────

        public bool HireCompanion(string id, string sceneName, Vector3 recruiterPos)
        {
            if (_hired.ContainsKey(id))
            {
                Debug.LogWarning($"[CompanionManager] {id} уже нанят.");
                return false;
            }
            _hired[id] = new HiredCompanionInfo
            {
                companionID        = id,
                isActive           = true,
                recruiterSceneName = sceneName,
                recruiterPosition  = recruiterPos
            };
            return true;
        }

        public void DismissCompanion(string id)
        {
            if (!_hired.TryGetValue(id, out var info)) return;
            info.isActive = false;
            DestroyActiveController(id);
        }

        public void ActivateCompanion(string id)
        {
            if (_hired.TryGetValue(id, out var info))
                info.isActive = true;
        }

        public void OnCompanionDied(string id) => DismissCompanion(id);

        // ── регистрация контроллеров ──────────────────────────────────────

        public void RegisterActiveCompanion(string id, CompanionController ctrl)
            => _active[id] = ctrl;

        public void UnregisterActiveCompanion(string id)
            => _active.Remove(id);

        // ── запросы состояния ─────────────────────────────────────────────

        public bool IsCompanionHired(string id)      => _hired.ContainsKey(id);
        public bool IsCompanionActive(string id)     => _hired.TryGetValue(id, out var i) && i.isActive;
        public bool IsCompanionRegistered(string id) => _active.ContainsKey(id);

        public HiredCompanionInfo GetCompanionInfo(string id) =>
            _hired.TryGetValue(id, out var info) ? info : null;

        public List<string> GetActiveCompanionIDs() =>
            _hired.Where(kv => kv.Value.isActive).Select(kv => kv.Key).ToList();

        // ── внутреннее ───────────────────────────────────────────────────

        private void DestroyActiveController(string id)
        {
            if (!_active.TryGetValue(id, out var ctrl)) return;
            if (ctrl != null) ctrl.Deactivate();
            _active.Remove(id);
        }

        // ── ISaveable ────────────────────────────────────────────────────

        [System.Serializable]
        public struct SaveData { public List<HiredCompanionInfo> hiredCompanions; }

        public object CaptureState() =>
            new SaveData { hiredCompanions = _hired.Values.ToList() };

        public void RestoreState(object state)
        {
            SaveData data = state switch
            {
                SaveData sd => sd,
                JObject  jo => jo.ToObject<SaveData>(),
                _           => default
            };

            if (data.hiredCompanions == null)
            {
                Debug.LogWarning($"[CompanionManager] RestoreState: не удалось разобрать данные ({state?.GetType()})");
                return;
            }

            _hired.Clear();
            foreach (var info in data.hiredCompanions)
                _hired[info.companionID] = info;

            // Живые контроллеры прошлой сцены уничтожены вместе с ней
            _active.Clear();
        }
    }

    [System.Serializable]
    public class HiredCompanionInfo
    {
        public string  companionID;
        public bool    isActive;
        public string  recruiterSceneName;
        public Vector3 recruiterPosition;
    }
}
