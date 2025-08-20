using GameDevTV.Saving;
using UnityEngine;
using System.Collections.Generic;

namespace RPG.Saving
{
    /// <summary>
    /// Универсальный компонент для сохранения состояния активности объектов
    /// Может работать с самим объектом и/или его дочерними объектами
    /// </summary>
    public class ObjectActivator : MonoBehaviour, ISaveable
    {
        [Header("Target Settings")]
        [Tooltip("Сохранять состояние самого объекта")]
        [SerializeField] private bool saveSelf = true;
        
        [Tooltip("Сохранять состояние дочерних объектов")]
        [SerializeField] private bool saveChildren = false;
        
        [Tooltip("Сохранять только прямых потомков (не внуков)")]
        [SerializeField] private bool directChildrenOnly = true;

        [Header("Default Settings")]
        [Tooltip("Состояние по умолчанию для самого объекта")]
        [SerializeField] private bool defaultSelfState = true;
        
        [Header("Debug")]
        [Tooltip("Включить отладочные логи")]
        [SerializeField] private bool enableDebugLogs = false;

        private void Awake()
        {
            if (GetComponent<SaveableEntity>() == null)
            {
                Debug.LogError($"ObjectActivator на {gameObject.name} требует наличия SaveableEntity компонента!", this);
            }
        }

        public object CaptureState()
        {
            var state = new Dictionary<string, object>();
            
            if (saveSelf)
            {
                state["self"] = gameObject.activeSelf;
                if (enableDebugLogs)
                    Debug.Log($"ObjectActivator CaptureState self: {gameObject.activeSelf} on {gameObject.name}");
            }
            
            if (saveChildren)
            {
                List<bool> childStates = new List<bool>();
                
                if (directChildrenOnly)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        childStates.Add(transform.GetChild(i).gameObject.activeSelf);
                    }
                }
                else
                {
                    foreach (Transform child in GetComponentsInChildren<Transform>(true))
                    {
                        if (child != transform)
                        {
                            childStates.Add(child.gameObject.activeSelf);
                        }
                    }
                }
                
                state["children"] = childStates;
                if (enableDebugLogs)
                    Debug.Log($"ObjectActivator CaptureState children: {childStates.Count} on {gameObject.name}");
            }
            
            return state;
        }

        public void RestoreState(object stateObj)
        {
            if (stateObj is not Dictionary<string, object> state)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"ObjectActivator RestoreState: Invalid state format on {gameObject.name}");
                return;
            }

            if (saveSelf && state.ContainsKey("self"))
            {
                bool shouldBeActive = JsonSaveHelper.ToBool(state["self"]);
                gameObject.SetActive(shouldBeActive);
                if (enableDebugLogs)
                    Debug.Log($"ObjectActivator RestoreState self: {shouldBeActive} on {gameObject.name}");
            }

            if (saveChildren && state.ContainsKey("children") && state["children"] is List<object> childStatesList)
            {
                List<Transform> children = new List<Transform>();
                
                if (directChildrenOnly)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        children.Add(transform.GetChild(i));
                    }
                }
                else
                {
                    foreach (Transform child in GetComponentsInChildren<Transform>(true))
                    {
                        if (child != transform)
                        {
                            children.Add(child);
                        }
                    }
                }

                for (int i = 0; i < Mathf.Min(childStatesList.Count, children.Count); i++)
                {
                    bool shouldBeActive = JsonSaveHelper.ToBool(childStatesList[i]);
                    children[i].gameObject.SetActive(shouldBeActive);
                    if (enableDebugLogs)
                        Debug.Log($"ObjectActivator RestoreState child: {children[i].name} -> {shouldBeActive}");
                }
            }
        }

        /// <summary>
        /// Активировать объект
        /// </summary>
        public void Activate()
        {
            if (enableDebugLogs)
                Debug.Log($"ObjectActivator manually activated: {gameObject.name}");
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Деактивировать объект
        /// </summary>
        public void Deactivate()
        {
            if (enableDebugLogs)
                Debug.Log($"ObjectActivator manually deactivated: {gameObject.name}");
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Переключить состояние объекта
        /// </summary>
        public void Toggle()
        {
            bool newState = !gameObject.activeSelf;
            if (enableDebugLogs)
                Debug.Log($"ObjectActivator toggled to: {newState} on {gameObject.name}");
            gameObject.SetActive(newState);
        }

        /// <summary>
        /// Активировать всех детей
        /// </summary>
        [ContextMenu("Activate All Children")]
        public void ActivateAllChildren()
        {
            SetAllChildrenActive(true);
        }

        /// <summary>
        /// Деактивировать всех детей
        /// </summary>
        [ContextMenu("Deactivate All Children")]
        public void DeactivateAllChildren()
        {
            SetAllChildrenActive(false);
        }

        /// <summary>
        /// Переключить состояние всех детей
        /// </summary>
        [ContextMenu("Toggle All Children")]
        public void ToggleAllChildren()
        {
            if (directChildrenOnly)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    child.gameObject.SetActive(!child.gameObject.activeSelf);
                }
            }
            else
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child != transform)
                    {
                        child.gameObject.SetActive(!child.gameObject.activeSelf);
                    }
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"ObjectActivator toggled all children on {gameObject.name}");
        }

        /// <summary>
        /// Сбросить к состоянию по умолчанию
        /// </summary>
        [ContextMenu("Reset To Default State")]
        public void ResetToDefaultState()
        {
            if (enableDebugLogs)
                Debug.Log($"ObjectActivator reset to default: {defaultSelfState} on {gameObject.name}");
            gameObject.SetActive(defaultSelfState);
        }

        private void SetAllChildrenActive(bool active)
        {
            if (directChildrenOnly)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(active);
                }
            }
            else
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child != transform)
                    {
                        child.gameObject.SetActive(active);
                    }
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"ObjectActivator set all children to {active} on {gameObject.name}");
        }
    }
}