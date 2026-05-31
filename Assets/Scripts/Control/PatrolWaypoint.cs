using UnityEngine;

namespace RPG.Control
{
    /// <summary>
    /// Скрипт для индивидуальной настройки конкретной точки патрулирования.
    /// Вешается на дочерние объекты внутри PatrolPath.
    /// </summary>
    public class PatrolWaypoint : MonoBehaviour
    {
        [Header("Ожидание (Dwell Time)")]
        [Tooltip("Переопределить стандартное время ожидания для этой точки?")]
        [SerializeField] private bool _overrideDwellTime = false;
        [SerializeField] private float _dwellTime = 3f;

        [Header("Скорость движения от этой точки")]
        [Tooltip("Переопределить скорость движения к следующей точке?")]
        [SerializeField] private bool _overrideSpeed = false;
        [Range(0, 1)]
        [SerializeField] private float _speedFraction = 0.5f;

        public bool OverrideDwellTime => _overrideDwellTime;
        public float DwellTime => _dwellTime;

        public bool OverrideSpeed => _overrideSpeed;
        public float SpeedFraction => _speedFraction;
    }
}