using GameDevTV.Saving;
using UnityEngine;

namespace RPG.Saving
{
    /// <summary>
    /// Сохраняет и восстанавливает состояние активности GameObject (SetActive)
    /// Требует наличия SaveableEntity на том же объекте
    /// </summary>
    public class GameObjectActivator : MonoBehaviour, ISaveable
    {
        [Header("Settings")]
        [Tooltip("Если true, объект будет активным по умолчанию при первом запуске")]
        [SerializeField] private bool defaultActiveState = true;
        
        [Header("Debug")]
        [Tooltip("Включить отладочные логи для отслеживания состояния")]
        [SerializeField] private bool enableDebugLogs = false;

        private void Awake()
        {
            // Проверяем наличие SaveableEntity
            if (GetComponent<SaveableEntity>() == null)
            {
                Debug.LogError($"GameObjectActivator на {gameObject.name} требует наличия SaveableEntity компонента!", this);
            }
        }

        public object CaptureState()
        {
            bool isActive = gameObject.activeSelf;
            
            if (enableDebugLogs)
            {
                Debug.Log($"GameObjectActivator CaptureState: {isActive} on {gameObject.name}");
            }
            
            return isActive;
        }

        public void RestoreState(object state)
        {
            bool shouldBeActive = JsonSaveHelper.ToBool(state);
            
            if (enableDebugLogs)
            {
                Debug.Log($"GameObjectActivator RestoreState: {shouldBeActive} on {gameObject.name}");
            }
            
            gameObject.SetActive(shouldBeActive);
        }

        /// <summary>
        /// Программно активировать объект
        /// </summary>
        public void Activate()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"GameObjectActivator manually activated: {gameObject.name}");
            }
            
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Программно деактивировать объект
        /// </summary>
        public void Deactivate()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"GameObjectActivator manually deactivated: {gameObject.name}");
            }
            
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Переключить состояние активности объекта
        /// </summary>
        public void Toggle()
        {
            bool newState = !gameObject.activeSelf;
            
            if (enableDebugLogs)
            {
                Debug.Log($"GameObjectActivator toggled to: {newState} on {gameObject.name}");
            }
            
            gameObject.SetActive(newState);
        }

        /// <summary>
        /// Сбросить объект к состоянию по умолчанию
        /// </summary>
        [ContextMenu("Reset To Default State")]
        public void ResetToDefaultState()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"GameObjectActivator reset to default: {defaultActiveState} on {gameObject.name}");
            }
            
            gameObject.SetActive(defaultActiveState);
        }
    }
}