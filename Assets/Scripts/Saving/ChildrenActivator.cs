using GameDevTV.Saving;
using UnityEngine;
using System.Collections.Generic;

namespace RPG.Saving
{
    /// <summary>
    /// Сохраняет и восстанавливает состояние активности всех дочерних объектов
    /// Полезно для групп объектов, которые нужно показывать/скрывать вместе
    /// </summary>
    public class ChildrenActivator : MonoBehaviour, ISaveable
    {
        [Header("Settings")]
        [Tooltip("Включить отладочные логи")]
        [SerializeField] private bool enableDebugLogs = false;
        
        [Tooltip("Сохранять только прямых потомков (не внуков)")]
        [SerializeField] private bool directChildrenOnly = true;

        private void Awake()
        {
            // Проверяем наличие SaveableEntity
            if (GetComponent<SaveableEntity>() == null)
            {
                Debug.LogError($"ChildrenActivator на {gameObject.name} требует наличия SaveableEntity компонента!", this);
            }
        }

        public object CaptureState()
        {
            List<bool> childStates = new List<bool>();
            
            if (directChildrenOnly)
            {
                // Сохраняем только прямых потомков
                for (int i = 0; i < transform.childCount; i++)
                {
                    childStates.Add(transform.GetChild(i).gameObject.activeSelf);
                }
            }
            else
            {
                // Сохраняем всех потомков рекурсивно
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child != transform) // Исключаем родительский объект
                    {
                        childStates.Add(child.gameObject.activeSelf);
                    }
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"ChildrenActivator CaptureState: {childStates.Count} children on {gameObject.name}");
            }
            
            return childStates;
        }

        public void RestoreState(object state)
        {
            if (state is not List<object> stateList)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"ChildrenActivator RestoreState: Invalid state format on {gameObject.name}");
                }
                return;
            }

            List<Transform> children = new List<Transform>();
            
            if (directChildrenOnly)
            {
                // Получаем только прямых потомков
                for (int i = 0; i < transform.childCount; i++)
                {
                    children.Add(transform.GetChild(i));
                }
            }
            else
            {
                // Получаем всех потомков рекурсивно
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child != transform) // Исключаем родительский объект
                    {
                        children.Add(child);
                    }
                }
            }

            // Восстанавливаем состояния
            for (int i = 0; i < Mathf.Min(stateList.Count, children.Count); i++)
            {
                bool shouldBeActive = JsonSaveHelper.ToBool(stateList[i]);
                children[i].gameObject.SetActive(shouldBeActive);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"ChildrenActivator RestoreState: {children[i].name} -> {shouldBeActive}");
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"ChildrenActivator RestoreState: Restored {Mathf.Min(stateList.Count, children.Count)} children on {gameObject.name}");
            }
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
            {
                Debug.Log($"ChildrenActivator toggled all children on {gameObject.name}");
            }
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
            {
                Debug.Log($"ChildrenActivator set all children to {active} on {gameObject.name}");
            }
        }
    }
}