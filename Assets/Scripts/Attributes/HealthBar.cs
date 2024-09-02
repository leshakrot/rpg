using UnityEngine;

namespace RPG.Attributes
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Health _healthComponent = null;
        [SerializeField] private RectTransform _foreground = null;
        [SerializeField] private Canvas _rootCanvas = null;

        private void Update()
        {
            if (Mathf.Approximately(_healthComponent.GetFraction(), 0) || Mathf.Approximately(_healthComponent.GetFraction(), 1))
            {
                _rootCanvas.enabled = false;
                return;
            }

            _rootCanvas.enabled = true;
            _foreground.localScale = new Vector3(_healthComponent.GetFraction(), 1, 1);
        }
    }
}
