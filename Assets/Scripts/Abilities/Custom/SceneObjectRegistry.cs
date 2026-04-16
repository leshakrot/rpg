using System.Collections.Generic;
using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Lightweight scene-lifetime registry that maps string keys to GameObjects.
    ///
    /// Place this component on any persistent GameObject in the scene.
    /// VFX objects that need to be toggled via item/ability effects should
    /// register themselves here (or be registered manually via the Inspector list).
    ///
    /// This is intentionally NOT a DontDestroyOnLoad singleton — it lives
    /// exactly as long as the scene it belongs to. Each scene that needs
    /// toggleable objects has its own registry instance.
    /// </summary>
    public class SceneObjectRegistry : MonoBehaviour
    {
        // ── Singleton (scene-scoped, not cross-scene) ──────────────────────
        public static SceneObjectRegistry Instance { get; private set; }

        // ── Inspector-registered entries ──────────────────────────────────
        [System.Serializable]
        public struct Entry
        {
            public string key;
            public GameObject target;
        }

        [Tooltip("Objects registered at scene load. Add your VFX/other objects here.")]
        [SerializeField] private List<Entry> initialEntries = new();

        // ── Runtime dictionary ─────────────────────────────────────────────
        private readonly Dictionary<string, GameObject> _registry = new();

        // ──────────────────────────────────────────────────────────────────
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
                    Debug.LogWarning("[SceneObjectRegistry] Invalid entry (empty key or null target) skipped.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>Register a GameObject under a key. Overwrites any existing entry.</summary>
        public void Register(string key, GameObject obj)
        {
            if (string.IsNullOrEmpty(key) || obj == null) return;
            _registry[key] = obj;
        }

        /// <summary>Unregister a key.</summary>
        public void Unregister(string key)
        {
            _registry.Remove(key);
        }

        /// <summary>
        /// Retrieve a registered object. Returns null and logs a warning if not found.
        /// </summary>
        public GameObject Get(string key)
        {
            if (_registry.TryGetValue(key, out var obj)) return obj;
            return null;
        }

        /// <summary>Returns true if the key is registered and the object is not null.</summary>
        public bool Contains(string key) =>
            _registry.TryGetValue(key, out var obj) && obj != null;
    }
}
