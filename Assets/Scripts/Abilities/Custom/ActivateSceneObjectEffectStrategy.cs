using RPG.Core;
using UnityEngine;

namespace RPG.Abilities.Effects
{
    /// <summary>
    /// Effect strategy that activates (SetActive=true) a pre-existing scene object
    /// registered under a specific key in the SceneObjectRegistry.
    ///
    /// Usage:
    ///   1. Add SceneObjectRegistry to any persistent scene GameObject.
    ///   2. Register your VFX object with a unique key (e.g. "SmokeTrail").
    ///   3. Set that same key in this SO's objectKey field.
    ///
    /// The object stays active until deactivated by something else —
    /// use ActivateSceneObjectEffectStrategy with activate=false for that,
    /// or handle it in your own game logic.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ActivateSceneObjectEffect",
        menuName = "RPG/Abilities/Effects/Activate Scene Object")]
    public class ActivateSceneObjectEffectStrategy : EffectStrategy
    {
        [Tooltip("Key the target object is registered under in SceneObjectRegistry.")]
        [SerializeField] private string objectKey = "";

        [Tooltip("True = SetActive(true), False = SetActive(false).")]
        [SerializeField] private bool activate = true;

        public override void StartEffect(AbilityData data, System.Action onFinish)
        {
            if (string.IsNullOrEmpty(objectKey))
            {
                Debug.LogWarning($"[ActivateSceneObjectEffectStrategy] objectKey is empty on '{name}'.");
                onFinish?.Invoke();
                return;
            }

            GameObject target = SceneObjectRegistry.Instance.Get(objectKey);

            if (target == null)
            {
                Debug.LogWarning($"[ActivateSceneObjectEffectStrategy] No object registered under key '{objectKey}'.");
                onFinish?.Invoke();
                return;
            }

            target.SetActive(activate);
            onFinish?.Invoke();
        }
    }
}
