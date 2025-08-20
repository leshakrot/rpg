using UnityEngine;
using GameDevTV.Saving;

namespace RPG.Saving
{
    public class ObjectVisibilitySaver : MonoBehaviour, ISaveable
    {
        [SerializeField] private bool _isVisible = true;
        private bool _hasBeenInitialized = false;

        private void Awake()
        {
            // Устанавливаем начальное состояние только если состояние еще не было восстановлено
            if (!_hasBeenInitialized)
            {
                _isVisible = gameObject.activeSelf;
                _hasBeenInitialized = true;
            }
        }

        private void Start()
        {
            // Убеждаемся, что GameObject соответствует сохраненному состоянию
            UpdateGameObjectState();
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            UpdateGameObjectState();
        }

        public void ToggleVisibility()
        {
            SetVisible(!_isVisible);
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        private void UpdateGameObjectState()
        {
            if (gameObject.activeSelf != _isVisible)
            {
                gameObject.SetActive(_isVisible);
            }
        }

        public object CaptureState()
        {
            // Сохраняем текущее состояние видимости
            _isVisible = gameObject.activeSelf;
            return _isVisible;
        }

        public void RestoreState(object state)
        {
            if (state != null)
            {
                _isVisible = (bool)state;
                _hasBeenInitialized = true;
                // Применяем состояние сразу после восстановления
                UpdateGameObjectState();
            }
        }
    }
}