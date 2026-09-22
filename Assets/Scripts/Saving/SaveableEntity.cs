using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDevTV.Saving
{
    /// <summary>
    /// Original GameDevTV-style entity container.
    ///
    /// IMPORTANT: the component key remains GetType().ToString() so existing
    /// save files remain compatible. Do not change this to an assembly-qualified
    /// name without a dedicated migration.
    /// </summary>
    [ExecuteAlways]
    public class SaveableEntity : MonoBehaviour
    {
        [Tooltip(
            "The unique ID is automatically generated in a scene file if empty. " +
            "Do not set it in a prefab unless all instances should share one ID.")]
        [SerializeField] private string uniqueIdentifier = "";

        private static readonly Dictionary<string, SaveableEntity> globalLookup =
            new Dictionary<string, SaveableEntity>();

        public string GetUniqueIdentifier()
        {
            return uniqueIdentifier;
        }

        public object CaptureState()
        {
            Dictionary<string, object> state =
                new Dictionary<string, object>();

            foreach (ISaveable saveable in GetComponents<ISaveable>())
            {
                if (saveable == null)
                    continue;

                string typeString =
                    saveable.GetType().ToString();

                if (state.ContainsKey(typeString))
                {
                    Debug.LogError(
                        $"SaveableEntity '{name}' has multiple ISaveable " +
                        $"components of type '{typeString}'. The original save " +
                        "format cannot distinguish them.",
                        this);
                    continue;
                }

                state[typeString] =
                    saveable.CaptureState();
            }

            return state;
        }

        public void RestoreState(object state)
        {
            Dictionary<string, object> stateDict =
                ConvertToDictionary(state);

            if (stateDict == null)
            {
                Debug.LogWarning(
                    $"SaveableEntity '{name}': unsupported state type " +
                    $"'{state?.GetType().FullName}'.",
                    this);
                return;
            }

            foreach (ISaveable saveable in GetComponents<ISaveable>())
            {
                if (saveable == null)
                    continue;

                string typeString =
                    saveable.GetType().ToString();

                if (stateDict.TryGetValue(
                    typeString,
                    out object componentState))
                {
                    saveable.RestoreState(componentState);
                }
            }
        }

        private static Dictionary<string, object> ConvertToDictionary(
            object state)
        {
            if (state is Dictionary<string, object> dictionary)
                return dictionary;

            if (state is Newtonsoft.Json.Linq.JObject jsonObject)
            {
                return jsonObject.ToObject<Dictionary<string, object>>();
            }

            return null;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Application.IsPlaying(gameObject))
                return;

            if (string.IsNullOrEmpty(gameObject.scene.path))
                return;

            UnityEditor.SerializedObject serializedObject =
                new UnityEditor.SerializedObject(this);

            UnityEditor.SerializedProperty property =
                serializedObject.FindProperty(nameof(uniqueIdentifier));

            if (property == null)
                return;

            if (string.IsNullOrEmpty(property.stringValue) ||
                !IsUnique(property.stringValue))
            {
                property.stringValue =
                    Guid.NewGuid().ToString();

                serializedObject.ApplyModifiedProperties();
            }

            globalLookup[property.stringValue] = this;
        }

        private bool IsUnique(string candidate)
        {
            if (!globalLookup.ContainsKey(candidate))
                return true;

            if (globalLookup[candidate] == this)
                return true;

            if (globalLookup[candidate] == null)
            {
                globalLookup.Remove(candidate);
                return true;
            }

            if (globalLookup[candidate].GetUniqueIdentifier() != candidate)
            {
                globalLookup.Remove(candidate);
                return true;
            }

            return false;
        }
#endif
    }
}
