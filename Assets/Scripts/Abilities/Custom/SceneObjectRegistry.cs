using System.Collections.Generic;
using GameDevTV.Saving;
using Newtonsoft.Json;
using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Scene-scoped registry: maps string keys → GameObjects.
    ///
    /// Теперь реализует ISaveable — сохраняет активное состояние каждого
    /// зарегистрированного объекта. При переходе между сценами SavingSystem
    /// вызовет RestoreState уже на новой сцене, и объекты (напр. VFX-след)
    /// активируются/деактивируются автоматически.
    ///
    /// Требования к сцене:
    ///   • На этом же GameObject должен стоять SaveableEntity (GameDevTV) с
    ///     фиксированным GUID — иначе Save-система не найдёт компонент.
    ///   • В каждой сцене, где нужно восстанавливать состояние, должен быть
    ///     свой SceneObjectRegistry с тем же GUID у SaveableEntity.
    /// </summary>
    public class SceneObjectRegistry : MonoBehaviour, ISaveable
    {
        public static SceneObjectRegistry Instance { get; private set; }

        [System.Serializable]
        public struct Entry
        {
            public string key;
            public GameObject target;
        }

        [Tooltip("Objects registered at scene load.")]
        [SerializeField] private List<Entry> initialEntries = new();

        private readonly Dictionary<string, GameObject> _registry = new();

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SceneObjectRegistry] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            foreach (var entry in initialEntries)
            {
                if (!string.IsNullOrEmpty(entry.key) && entry.target != null)
                    Register(entry.key, entry.target);
                else
                    Debug.LogWarning("[SceneObjectRegistry] Invalid entry skipped.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────

        public void Register(string key, GameObject obj)
        {
            if (string.IsNullOrEmpty(key) || obj == null) return;
            _registry[key] = obj;
        }

        public void Unregister(string key) => _registry.Remove(key);

        public GameObject Get(string key)
        {
            _registry.TryGetValue(key, out var obj);
            return obj;
        }

        public bool Contains(string key) =>
            _registry.TryGetValue(key, out var obj) && obj != null;

        // ── ISaveable ──────────────────────────────────────────────────────

        /// <summary>
        /// Сохраняем словарь key → activeSelf для всех зарегистрированных объектов.
        /// Объекты, которые были null во время сохранения, пропускаем.
        /// </summary>
        public object CaptureState()
        {
            var state = new Dictionary<string, bool>();
            foreach (var kvp in _registry)
            {
                if (kvp.Value != null)
                    state[kvp.Key] = kvp.Value.activeSelf;
            }
            return state;
        }

        /// <summary>
        /// Восстанавливаем состояния. Вызывается SavingSystem сразу после
        /// загрузки сцены — к этому моменту все Awake() уже выполнились и
        /// initialEntries уже зарегистрированы, так что объекты доступны.
        /// </summary>
        public void RestoreState(object state)
        {
            if (state is not Dictionary<string, bool> saved)
            {
                Debug.LogError("[SceneObjectRegistry] RestoreState: unexpected data type.");
                return;
            }

            foreach (var kvp in saved)
            {
                if (_registry.TryGetValue(kvp.Key, out var obj) && obj != null)
                {
                    obj.SetActive(kvp.Value);
                }
                // Ключ есть в сохранении, но не зарегистрирован в этой сцене —
                // это нормально: не все сцены содержат все объекты.
            }
        }
    }
}
