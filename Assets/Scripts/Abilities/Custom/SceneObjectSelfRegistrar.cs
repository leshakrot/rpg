using RPG.Core;
using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Attach this component to any scene GameObject that should register itself
    /// in SceneObjectRegistry on Awake.
    ///
    /// This way the VFX object is self-describing: it knows its own registry key
    /// and handles registration/cleanup automatically.
    ///
    /// Setup:
    ///   1. Add this component to your VFX GameObject (e.g. "SmokeTrail").
    ///   2. Set registryKey to a unique name, e.g. "SmokeTrail".
    ///   3. The object starts inactive (SetActive=false) in the scene.
    ///   4. When the potion is used, ActivateSceneObjectEffectStrategy activates it.
    /// </summary>
    public class SceneObjectSelfRegistrar : MonoBehaviour
    {
        [Tooltip("Key under which this object will be registered in SceneObjectRegistry.")]
        [SerializeField] private string registryKey = "";

        private void Awake()
        {
            if (string.IsNullOrEmpty(registryKey))
            {
                Debug.LogWarning($"[SceneObjectSelfRegistrar] registryKey is empty on '{gameObject.name}'.");
                return;
            }

            // Registration must happen even when the object starts as inactive,
            // so we use Awake (called before first frame, even on inactive objects
            // IF the parent is active at scene load).
            //
            // If the root is also inactive, call Register manually from an always-
            // active manager, or use the SceneObjectRegistry's initialEntries list.
            if (SceneObjectRegistry.Instance != null)
                SceneObjectRegistry.Instance.Register(registryKey, gameObject);
            else
                Debug.LogWarning($"[SceneObjectSelfRegistrar] SceneObjectRegistry not found. Register '{registryKey}' via SceneObjectRegistry.initialEntries instead.");
        }

        private void OnDestroy()
        {
            if (SceneObjectRegistry.Instance != null)
                SceneObjectRegistry.Instance.Unregister(registryKey);
        }
    }
}
