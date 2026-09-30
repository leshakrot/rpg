using RPG.Stats;
using TMPro;
using UnityEngine;

namespace RPG.Attributes
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Health _healthComponent = null;
        [SerializeField] private RectTransform _foreground = null;
        [SerializeField] private Canvas _rootCanvas = null;
        [SerializeField] private TextMeshProUGUI _level;

        // �������������� ��������� ��� �������� ���������
        private float _currentHealthFraction = 1f; // ������� ����� ������� ��������
        private float _targetHealthFraction = 1f; // ������� ����� ������� ��������
        private float _smoothSpeed = 5f; // �������� �������� ���������

        private BaseStats _baseStats;

        private void Start()
        {
            _baseStats = _healthComponent.gameObject.GetComponent<BaseStats>();
            if (_baseStats != null && _level != null)
            {
                _level.text = _baseStats.GetLevel().ToString();
            }
        }

        private void OnEnable()
        {
            if (_baseStats == null && _healthComponent != null)
            {
                _baseStats = _healthComponent.gameObject.GetComponent<BaseStats>();
            }

            if (_baseStats != null)
            {
                _baseStats.onLevelUp += UpdateLevelDisplay;
                _baseStats.onStatsRefreshed += UpdateLevelDisplay;
            }
        }

        private void OnDisable()
        {
            if (_baseStats != null)
            {
                _baseStats.onLevelUp -= UpdateLevelDisplay;
                _baseStats.onStatsRefreshed -= UpdateLevelDisplay;
            }
        }

        private void UpdateLevelDisplay()
        {
            if (_baseStats != null && _level != null)
            {
                _level.text = _baseStats.GetLevel().ToString();
            }
        }

        private void Update()
        {
            // ��������� ������� �������� ��������
            _targetHealthFraction = _healthComponent.GetFraction();

            // ������ �������� ������� �������� � ��������
            _currentHealthFraction = Mathf.Lerp(_currentHealthFraction, _targetHealthFraction, Time.deltaTime * _smoothSpeed);

            // ���� ������� ����� ������� � ������� ��������� ����, ��������� ������� ��������
            if (Mathf.Abs(_currentHealthFraction - _targetHealthFraction) < 0.001f)
            {
                _currentHealthFraction = _targetHealthFraction;
            }

            // ���������� ���������� ������
            if (Mathf.Approximately(_currentHealthFraction, 0) || Mathf.Approximately(_currentHealthFraction, 1))
            {
                _rootCanvas.enabled = false;
                return;
            }

            _rootCanvas.enabled = true;

            // ��������� ������� ��������� �����
            _foreground.localScale = new Vector3(_currentHealthFraction, 1, 1);

            // ��������� ������������ � �����, ������� "����������"
            ApplyForegroundTransparency();
        }

        private void ApplyForegroundTransparency()
        {
            // �������� ��������� Image ��� ��������� �����
            var foregroundImage = _foreground.GetComponent<UnityEngine.UI.Image>();
            if (foregroundImage == null) return;

            // ��������� ������� ����� ������� � ������� ���������
            float delta = _currentHealthFraction - _targetHealthFraction;

            // ������������� ������������ ��� "����������" �����
            Color color = foregroundImage.color;
            color.a = Mathf.Clamp01(1 - delta); // ������������ ������� �� �������
            foregroundImage.color = color;
        }
    }
}