using UnityEngine;
using GameDevTV.Saving;

namespace RPG.Saving
{
    public class ObjectVisibilitySaver : MonoBehaviour, ISaveable
    {
        [SerializeField] private bool _isVisible = true;
        
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private bool _hasBeenInitialized = false;

        private void Awake()
        {
            // Кэшируем компоненты один раз
            _renderers = GetComponentsInChildren<Renderer>();
            _colliders = GetComponentsInChildren<Collider>();
            
            _hasBeenInitialized = true;
            Debug.Log($"ObjectVisibilitySaver.Awake: {gameObject.name} initialized with {_renderers.Length} renderers and {_colliders.Length} colliders");
        }

        private void Start()
        {
            // Применяем сохраненное состояние при запуске
            ApplyVisibilityState();
            Debug.Log($"ObjectVisibilitySaver.Start: {gameObject.name} visibility state applied: {_isVisible}");
        }

        public void SetVisible(bool visible)
        {
            Debug.Log($"ObjectVisibilitySaver.SetVisible: {gameObject.name} setting visibility to: {visible}");
            _isVisible = visible;
            ApplyVisibilityState();
        }

        private void ApplyVisibilityState()
        {
            // Включаем/выключаем рендереры вместо самого объекта
            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                    renderer.enabled = _isVisible;
            }

            // Включаем/выключаем коллайдеры для взаимодействия
            foreach (var collider in _colliders)
            {
                if (collider != null)
                    collider.enabled = _isVisible;
            }
            
            Debug.Log($"ObjectVisibilitySaver.ApplyVisibilityState: {gameObject.name} applied visibility: {_isVisible}");
        }

        public object CaptureState()
        {
            Debug.Log($"ObjectVisibilitySaver.CaptureState: {gameObject.name} saving visibility state: {_isVisible}");
            return _isVisible;
        }

        public void RestoreState(object state)
        {
            bool newVisibility = JsonSaveHelper.ToBool(state);
            Debug.Log($"ObjectVisibilitySaver.RestoreState: {gameObject.name} restoring visibility state: {newVisibility}");
            
            _isVisible = newVisibility;
            ApplyVisibilityState();
        }
    }
}