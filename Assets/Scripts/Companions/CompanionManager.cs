using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameDevTV.Saving;
using RPG.Combat;
using Newtonsoft.Json.Linq;

namespace RPG.Companions
{
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

        private Dictionary<string, HiredCompanionInfo> _hired = new();
        private Dictionary<string, CompanionController> _active = new();

        // ── lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void Start()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            SpawnActiveCompanions(currentScene);
            SpawnWaitingCompanions(currentScene);
            SpawnBasedCompanions(currentScene);
        }

        // ── спавн ─────────────────────────────────────────────────────────

        /// <summary>Активные компаньоны — следуют за игроком.</summary>
        private void SpawnActiveCompanions(string currentScene)
        {
            if (registry == null) return;

            foreach (string id in GetActiveCompanionIDs())
            {
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

        /// <summary>
        /// Ждущие компаньоны — нанят, не активен, не на базе.
        /// Появляются на той сцене и позиции где их отпустили.
        /// </summary>
        private void SpawnWaitingCompanions(string currentScene)
        {
            if (registry == null) return;

            foreach (var kvp in _hired)
            {
                var info = kvp.Value;
                if (info.isActive) continue;
                if (info.sentToBase) continue;
                if (info.waitSceneName != currentScene) continue;
                if (IsCompanionRegistered(info.companionID)) continue;

                var entry = registry.GetEntry(info.companionID);
                if (entry == null || entry.prefab == null) continue;

                // Спавним на позиции где отпустили — просто NPC, не активируем контроллер
                GameObject instance = Instantiate(entry.prefab, info.waitPosition, Quaternion.identity);
                instance.name = entry.data.CompanionName;
                // CompanionController в Inactive — стоит на месте, можно поговорить
            }
        }

        /// <summary>
        /// Компаньоны на базе — нанят, sentToBase = true.
        /// За их спавн отвечает CompanionSpawner на родной сцене.
        /// Этот метод только сигнализирует спавнеру.
        /// </summary>
        private void SpawnBasedCompanions(string currentScene)
        {
            // CompanionSpawner.Start() уже отработал к этому моменту и проверил sentToBase сам.
            // Дополнительно вызываем на случай если порядок Start() был другим.
            foreach (var spawner in FindObjectsOfType<CompanionSpawner>())
                spawner.SpawnIfNeeded();
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
                sentToBase         = false,
                recruiterSceneName = sceneName,
                recruiterPosition  = recruiterPos,
                waitSceneName      = "",
                waitPosition       = Vector3.zero
            };
            return true;
        }

        /// <summary>
        /// Компаньон ждёт здесь — остаётся на текущей позиции на текущей сцене.
        /// </summary>
        public void WaitHere(string id)
        {
            if (!_hired.TryGetValue(id, out var info)) return;

            // Запоминаем позицию живого контроллера
            Vector3 waitPos = Vector3.zero;
            if (_active.TryGetValue(id, out var ctrl) && ctrl != null)
                waitPos = ctrl.transform.position;

            info.isActive     = false;
            info.sentToBase   = false;
            info.waitSceneName = SceneManager.GetActiveScene().name;
            info.waitPosition  = waitPos;

            // Деактивируем контроллер — компаньон становится обычным NPC на месте
            if (_active.TryGetValue(id, out var controller) && controller != null)
                controller.Deactivate();

            _active.Remove(id);
        }

        /// <summary>
        /// Отправить компаньона на базу — исчезает с текущей сцены,
        /// появляется на родной сцене у CompanionSpawner.
        /// </summary>
        public void SendToBase(string id)
        {
            if (!_hired.TryGetValue(id, out var info)) return;

            info.isActive   = false;
            info.sentToBase = true;

            DestroyActiveInstance(id);

            // Если мы уже на родной сцене компаньона — спавним сразу,
            // иначе CompanionSpawner отработает при следующей загрузке сцены
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == info.recruiterSceneName)
            {
                foreach (var spawner in FindObjectsOfType<CompanionSpawner>())
                {
                    spawner.ResetAndSpawn();
                }
            }
        }

        /// <summary>Вызвать компаньона — становится активным на текущей сцене.</summary>
        public void ActivateCompanion(string id)
        {
            if (!_hired.TryGetValue(id, out var info)) return;
            info.isActive   = true;
            info.sentToBase = false;
        }

        public void OnCompanionDied(string id) => WaitHere(id);

        // ── регистрация ───────────────────────────────────────────────────

        public void RegisterActiveCompanion(string id, CompanionController ctrl)
            => _active[id] = ctrl;

        public void UnregisterActiveCompanion(string id)
            => _active.Remove(id);

        // ── запросы состояния ─────────────────────────────────────────────

        public bool IsCompanionHired(string id)      => _hired.ContainsKey(id);
        public bool IsCompanionActive(string id)     => _hired.TryGetValue(id, out var i) && i.isActive;
        public bool IsCompanionRegistered(string id) => _active.ContainsKey(id);
        public bool IsCompanionOnBase(string id)     => _hired.TryGetValue(id, out var i) && !i.isActive && i.sentToBase;
        public bool IsCompanionWaiting(string id)    => _hired.TryGetValue(id, out var i) && !i.isActive && !i.sentToBase;

        public HiredCompanionInfo GetCompanionInfo(string id) =>
            _hired.TryGetValue(id, out var info) ? info : null;

        public List<string> GetActiveCompanionIDs() =>
            _hired.Where(kv => kv.Value.isActive).Select(kv => kv.Key).ToList();

        // ── внутреннее ───────────────────────────────────────────────────

        private void DestroyActiveInstance(string id)
        {
            if (!_active.TryGetValue(id, out var ctrl)) return;
            if (ctrl != null) Destroy(ctrl.gameObject);
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

            _active.Clear();
        }
    }

    [System.Serializable]
    public class HiredCompanionInfo
    {
        public string  companionID;
        public bool    isActive;

        // Ждёт на месте (не активен, не на базе)
        public string  waitSceneName;
        public Vector3 waitPosition;

        // Отправлен на базу — появится у CompanionSpawner на родной сцене
        public bool    sentToBase;

        // Данные рекрутера
        public string  recruiterSceneName;
        public Vector3 recruiterPosition;
    }
}
